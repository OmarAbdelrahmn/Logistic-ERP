using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class OperationCardReadDiagnosticTests
{
    [Fact]
    public async Task ExistingOperationCardCanBeReadAndSerializedFromSqlServer()
    {
        var configPath = Environment.GetEnvironmentVariable("OPERATION_CARD_DIAGNOSTIC_CONFIG");
        Assert.SkipUnless(configPath is not null, "Set OPERATION_CARD_DIAGNOSTIC_CONFIG to run this read-only database diagnostic.");
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath!, TestContext.Current.CancellationToken));
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__LogisticsDatabase")
            ?? config.RootElement.GetProperty("ConnectionStrings").GetProperty("LogisticsDatabase").GetString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var db = new ApplicationDbContext(options);
        var vehicleId = Guid.Parse("01a0aed7-0d9a-74dc-9a1e-010305b2407e");
        var expected = await db.VehicleOperationCards.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId).ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(expected);
        await using var identityDb = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(connection).Options);
        var service = new FleetService(db, identityDb,
            new FleetServiceSupport(new DiagnosticUser(), new PermitDiagnosticRead(), TimeProvider.System),
            new UnusedStorage());
        var history = await service.GetComplianceAsync(vehicleId, "operation-cards", TestContext.Current.CancellationToken);
        Assert.True(history.IsSuccess, history.Error.Description);
        Assert.Equal(expected.Length, history.Value!.Count);
        var current = Assert.Single(history.Value, item => item.IsCurrent);
        Assert.Equal("38-00048011", current.Number);
        Assert.Equal("الهيئة العامة للنقل", current.Issuer);
        Assert.Equal(new DateOnly(2025, 10, 27), current.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 10, 29), current.ExpiryDate);

        using var serialized = JsonDocument.Parse(JsonSerializer.Serialize(current, JsonSerializerOptions.Web));
        Assert.Equal("38-00048011", serialized.RootElement.GetProperty("number").GetString());
        Assert.Equal("2025-10-27", serialized.RootElement.GetProperty("effectiveFrom").GetString());
        Assert.True(serialized.RootElement.TryGetProperty("issuer", out _));
        Assert.True(serialized.RootElement.TryGetProperty("dueStatus", out _));
        Assert.False(serialized.RootElement.TryGetProperty("cardNumber", out _));
        Assert.False(serialized.RootElement.TryGetProperty("issuingAuthority", out _));
        Assert.False(serialized.RootElement.TryGetProperty("issueDate", out _));
        Assert.False(serialized.RootElement.TryGetProperty("status", out _));
        Assert.False(serialized.RootElement.TryGetProperty("notes", out _));
    }

    private sealed class DiagnosticUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "operation-card-read-diagnostic";
    }

    private sealed class PermitDiagnosticRead : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(permissionKey == PermissionKeys.Fleet.ComplianceRead);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }

    private sealed class UnusedStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file,
            long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType,
            string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) => throw new NotSupportedException();
    }
}
