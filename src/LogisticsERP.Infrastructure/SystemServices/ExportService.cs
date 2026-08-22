using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.SystemServices;

internal sealed class ExportService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IConfiguration configuration,
    TimeProvider timeProvider) : IExportService
{
    private static readonly HashSet<string> AllowedReports = new(StringComparer.OrdinalIgnoreCase)
    {
        "employees", "riders", "housing", "platform-accounts", "leave-requests", "notifications", "audit"
    };

    public async Task<Result<IReadOnlyList<ExportJobResponse>>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure<IReadOnlyList<ExportJobResponse>>(SystemErrors.CurrentUserUnavailable);
        var rows = await dbContext.ExportJobs.AsNoTracking().Where(item => item.RequestedByUserId == userId)
            .OrderByDescending(item => item.RequestedAtUtc).Take(200).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ExportJobResponse>>(rows.Select(ToResponse).ToArray());
    }

    public async Task<Result<ExportJobResponse>> CreateAsync(CreateExportRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || currentUser.AuthorizationVersion is not { } authorizationVersion
            || !AllowedReports.Contains(request.ReportType)
            || request.ReportVersion < 1
            || !Enum.TryParse<ExportFormat>(request.Format, true, out var format)
            || request.IncludesSensitiveValues && string.IsNullOrWhiteSpace(request.SensitiveExportReason)
            || !IsJsonObject(request.FilterSnapshotJson))
            return Result.Failure<ExportJobResponse>(SystemErrors.InvalidRequest);
        var now = timeProvider.GetUtcNow();
        var item = new ExportJob
        {
            RequestedByUserId = userId,
            ReportType = request.ReportType.Trim().ToLowerInvariant(),
            ReportVersion = request.ReportVersion,
            ScopeSnapshotJson = JsonSerializer.Serialize(new { UserId = userId, AuthorizationVersion = authorizationVersion }),
            FilterSnapshotJson = NormalizeJson(request.FilterSnapshotJson),
            Format = format,
            IncludesSensitiveValues = request.IncludesSensitiveValues,
            SensitiveExportReason = HrServiceSupport.TrimOrNull(request.SensitiveExportReason),
            Status = ExportStatus.Pending,
            RequestedAtUtc = now
        };
        dbContext.ExportJobs.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(item));
    }

    public async Task<Result<ExportJobResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await FindOwnedAsync(id, cancellationToken);
        return result is null ? Result.Failure<ExportJobResponse>(SystemErrors.NotFound) : Result.Success(ToResponse(result));
    }

    public async Task<Result<ExportArtifactResponse>> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await FindOwnedAsync(id, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (item is null) return Result.Failure<ExportArtifactResponse>(SystemErrors.NotFound);
        if (item.Status != ExportStatus.Completed || item.ArtifactExpiresAtUtc <= now
            || string.IsNullOrWhiteSpace(item.ArtifactPath) || item.ArtifactSizeBytes is not { } length)
            return Result.Failure<ExportArtifactResponse>(SystemErrors.ArtifactUnavailable);
        var fullPath = ResolveArtifactPath(item.ArtifactPath);
        if (fullPath is null || !File.Exists(fullPath)) return Result.Failure<ExportArtifactResponse>(SystemErrors.ArtifactUnavailable);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var extension = item.Format == ExportFormat.Excel ? "xlsx" : "csv";
        var contentType = item.Format == ExportFormat.Excel
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv; charset=utf-8";
        return Result.Success(new ExportArtifactResponse(stream, contentType, $"{item.ReportType}-{item.Id:N}.{extension}", length));
    }

    public async Task<Result> CancelAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default)
    {
        var item = await FindOwnedAsync(id, cancellationToken);
        if (item is null) return Result.Failure(SystemErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, rowVersion)) return Result.Failure(SystemErrors.ConcurrencyConflict);
        if (item.Status != ExportStatus.Pending) return Result.Failure(SystemErrors.Conflict);
        item.Status = ExportStatus.Cancelled;
        item.ErrorCode = "cancelled_by_user";
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<ExportJob?> FindOwnedAsync(Guid id, CancellationToken cancellationToken) =>
        currentUser.UserId is not { } userId ? null : await dbContext.ExportJobs.SingleOrDefaultAsync(
            item => item.Id == id && item.RequestedByUserId == userId, cancellationToken);

    private string? ResolveArtifactPath(string storagePath)
    {
        if (Path.IsPathRooted(storagePath)) return null;
        var configured = configuration["Export:ArtifactRoot"];
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), ".data", "exports")
            : configured);
        var fullPath = Path.GetFullPath(Path.Combine(root, storagePath.Replace('/', Path.DirectorySeparatorChar)));
        return fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }

    private static bool IsJsonObject(string value)
    {
        try { using var document = JsonDocument.Parse(value); return document.RootElement.ValueKind == JsonValueKind.Object; }
        catch (JsonException) { return false; }
    }

    private static string NormalizeJson(string value)
    {
        using var document = JsonDocument.Parse(value);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static ExportJobResponse ToResponse(ExportJob item) => new(
        item.Id, item.ReportType, item.ReportVersion, item.Format.ToString(), item.IncludesSensitiveValues,
        item.Status.ToString(), item.ProgressPercentage, item.RequestedAtUtc, item.StartedAtUtc,
        item.CompletedAtUtc, item.ArtifactSizeBytes, item.ArtifactExpiresAtUtc, item.ErrorCode,
        HrServiceSupport.EncodeRowVersion(item.RowVersion));
}
