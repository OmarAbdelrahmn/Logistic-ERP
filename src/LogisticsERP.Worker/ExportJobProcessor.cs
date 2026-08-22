using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Worker;

internal sealed partial class ExportJobProcessor(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<ExportJobProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextAsync(stoppingToken);
                if (!processed)
                {
                    await ExpireArtifactsAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogWorkerLoopFailed(logger, exception);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var candidateId = await dbContext.ExportJobs.AsNoTracking()
            .Where(item => item.Status == ExportStatus.Pending)
            .OrderBy(item => item.RequestedAtUtc)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (candidateId is null) return false;

        var now = timeProvider.GetUtcNow();
        var claimed = await dbContext.ExportJobs
            .Where(item => item.Id == candidateId && item.Status == ExportStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, ExportStatus.Running)
                .SetProperty(item => item.ProgressPercentage, 5)
                .SetProperty(item => item.StartedAtUtc, now), cancellationToken);
        if (claimed == 0) return true;

        var job = await dbContext.ExportJobs.SingleAsync(item => item.Id == candidateId, cancellationToken);
        try
        {
            var table = await BuildReportAsync(dbContext, job, cancellationToken);
            job.ProgressPercentage = 70;
            await dbContext.SaveChangesAsync(cancellationToken);

            var extension = job.Format == ExportFormat.Excel ? "xlsx" : "csv";
            var relativePath = $"{job.RequestedByUserId:N}/{job.Id:N}/{job.ReportType}.{extension}";
            var fullPath = ResolveArtifactPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            if (job.Format == ExportFormat.Excel)
                WriteExcel(fullPath, job.ReportType, table);
            else
                await WriteCsvAsync(fullPath, table, cancellationToken);

            await using var stream = File.OpenRead(fullPath);
            var checksum = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
            var info = new FileInfo(fullPath);
            job.ArtifactPath = relativePath;
            job.ArtifactChecksum = checksum;
            job.ArtifactSizeBytes = info.Length;
            job.ArtifactExpiresAtUtc = timeProvider.GetUtcNow().AddHours(24);
            job.Status = ExportStatus.Completed;
            job.ProgressPercentage = 100;
            job.CompletedAtUtc = timeProvider.GetUtcNow();
            job.ErrorCode = null;
            job.ErrorDetails = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            LogExportJobFailed(logger, exception, job.Id);
            job.Status = ExportStatus.Failed;
            job.ErrorCode = "export_generation_failed";
            job.ErrorDetails = exception.GetType().Name;
            job.CompletedAtUtc = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    private async Task ExpireArtifactsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        var expired = await dbContext.ExportJobs.Where(item => item.Status == ExportStatus.Completed
            && item.ArtifactExpiresAtUtc <= now).Take(100).ToListAsync(cancellationToken);
        foreach (var item in expired)
        {
            if (!string.IsNullOrWhiteSpace(item.ArtifactPath))
            {
                try { File.Delete(ResolveArtifactPath(item.ArtifactPath)); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            item.Status = ExportStatus.Expired;
        }
        if (expired.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string[][]> BuildReportAsync(
        ApplicationDbContext dbContext,
        ExportJob job,
        CancellationToken cancellationToken)
    {
        switch (job.ReportType)
        {
            case "employees":
            {
                var rows = await dbContext.Employees.AsNoTracking().OrderBy(item => item.EmployeeNumber).ToArrayAsync(cancellationToken);
                return Prepend(["EmployeeNumber", "NameAr", "NameEn", "Relationship", "Status", "HireDate"],
                    rows.Select(item => new[] { item.EmployeeNumber, item.FullNameAr, item.FullNameEn ?? "", item.CurrentRelationshipType?.ToString() ?? "", item.CurrentStatus.ToString(), item.HireDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "" }));
            }
            case "riders":
            {
                var rows = await (from rider in dbContext.RiderProfiles.AsNoTracking()
                                  join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                                  orderby employee.EmployeeNumber
                                  select new { rider, employee }).ToArrayAsync(cancellationToken);
                return Prepend(["EmployeeNumber", "NameAr", "RiderStatus", "PreferredCityId"],
                    rows.Select(row => new[] { row.employee.EmployeeNumber, row.employee.FullNameAr, row.rider.Status.ToString(), row.rider.PreferredCityId?.ToString("D") ?? "" }));
            }
            case "housing":
            {
                var rows = await dbContext.Housing.AsNoTracking().OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
                return Prepend(["Code", "NameAr", "NameEn", "Capacity", "Status"],
                    rows.Select(item => new[] { item.Code, item.NameAr, item.NameEn, item.TotalCapacity.ToString(CultureInfo.InvariantCulture), item.Status.ToString() }));
            }
            case "platform-accounts":
            {
                var rows = await dbContext.PlatformRiderAccounts.AsNoTracking().OrderBy(item => item.Code).ToArrayAsync(cancellationToken);
                return Prepend(["Code", "ExternalAccountId", "ClientPlatformId", "ClientContractId", "Status"],
                    rows.Select(item => new[] { item.Code, item.ExternalAccountId, item.ClientPlatformId.ToString(), item.ClientContractId.ToString(), item.Status.ToString() }));
            }
            case "leave-requests":
            {
                var rows = await dbContext.LeaveRequests.AsNoTracking().OrderByDescending(item => item.StartDate).ToArrayAsync(cancellationToken);
                return Prepend(["RequestNumber", "EmployeeId", "StartDate", "EndDate", "Status", "HrStatus"],
                    rows.Select(item => new[] { item.RequestNumber, item.EmployeeId.ToString("D"), item.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), item.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), item.Status.ToString(), item.HrStatus.ToString() }));
            }
            case "notifications":
            {
                var rows = await dbContext.Notifications.AsNoTracking().Where(item => item.RecipientUserId == job.RequestedByUserId)
                    .OrderByDescending(item => item.VisibleAtUtc).ToArrayAsync(cancellationToken);
                return Prepend(["EventType", "Severity", "TitleAr", "VisibleAtUtc", "ReadAtUtc"],
                    rows.Select(item => new[] { item.EventType, item.Severity.ToString(), item.TitleAr, item.VisibleAtUtc.ToString("O", CultureInfo.InvariantCulture), item.ReadAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "" }));
            }
            case "audit":
            {
                var rows = await dbContext.AuditEntries.AsNoTracking().OrderByDescending(item => item.Sequence).Take(10000).ToArrayAsync(cancellationToken);
                return Prepend(["Sequence", "ActorUserId", "Action", "EntityType", "EntityId", "OccurredAtUtc", "CorrelationId"],
                    rows.Select(item => new[] { item.Sequence.ToString(CultureInfo.InvariantCulture), item.ActorUserId?.ToString("D") ?? "", item.Action, item.EntityType, item.EntityId?.ToString("D") ?? "", item.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture), item.CorrelationId }));
            }
            default:
                throw new InvalidOperationException("The export report type is not supported.");
        }
    }

    private static string[][] Prepend(string[] header, IEnumerable<string[]> rows) => [header, .. rows];

    private static async Task WriteCsvAsync(string path, string[][] table, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(true));
        foreach (var row in table)
        {
            await writer.WriteLineAsync(string.Join(',', row.Select(EscapeCsv)).AsMemory(), cancellationToken);
        }
    }

    private static void WriteExcel(string path, string sheetName, string[][] table)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName[..Math.Min(sheetName.Length, 31)]);
        for (var row = 0; row < table.Length; row++)
        for (var column = 0; column < table[row].Length; column++)
            worksheet.Cell(row + 1, column + 1).Value = table[row][column];
        if (table.Length > 0)
        {
            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.SheetView.FreezeRows(1);
            worksheet.ColumnsUsed().AdjustToContents(1, 80);
        }
        workbook.SaveAs(path);
    }

    private static string EscapeCsv(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private string ResolveArtifactPath(string relativePath)
    {
        var configured = configuration["Export:ArtifactRoot"];
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), ".data", "exports")
            : configured);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The export artifact path escaped its configured root.");
        return fullPath;
    }

    [LoggerMessage(1, LogLevel.Error, "The export worker loop failed.")]
    private static partial void LogWorkerLoopFailed(ILogger logger, Exception exception);

    [LoggerMessage(2, LogLevel.Error, "Export job {ExportJobId} failed.")]
    private static partial void LogExportJobFailed(ILogger logger, Exception exception, Guid exportJobId);
}
