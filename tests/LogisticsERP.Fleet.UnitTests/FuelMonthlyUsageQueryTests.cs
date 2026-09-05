using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Infrastructure.Fuel;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FuelMonthlyUsageQueryTests
{
    [Fact]
    public async Task HostedMonthlyUsageQueryExecutesWhenConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGISTICS_INTEGRATION_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        var service = new FuelCardService(
            dbContext,
            new DiagnosticCurrentUser(),
            new AllowPermissionChecker(),
            TimeProvider.System);

        var result = await service.GetMonthlyUsageAsync(
            new DateOnly(2026, 9, 1),
            null,
            null,
            null,
            1,
            100,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task HostedAssignmentResponseQueryExecutesWhenConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGISTICS_INTEGRATION_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        var latestAssignment = await dbContext.FuelCardRiderAssignments
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.FuelCardId, x.Id })
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        if (latestAssignment is null)
        {
            return;
        }
        var service = new FuelCardService(
            dbContext,
            new DiagnosticCurrentUser(),
            new AllowPermissionChecker(),
            TimeProvider.System);

        var result = await service.GetAssignmentAsync(
            latestAssignment.FuelCardId,
            latestAssignment.Id,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(latestAssignment.Id, result.Value!.Id);
    }

    private sealed class DiagnosticCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.Parse("019c18d5-62e1-7000-c000-000000000001");
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "fuel-monthly-diagnostic";
    }

    private sealed class AllowPermissionChecker : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(
            Guid userId,
            long authorizationVersion,
            string permissionKey,
            PermissionScope? scope = null,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void InvalidateUser(Guid userId, long authorizationVersion)
        {
        }
    }
}
