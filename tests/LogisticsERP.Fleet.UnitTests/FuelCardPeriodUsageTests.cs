using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fuel;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FuelCardPeriodUsageTests
{
    [Fact]
    public async Task ReturnsEachRiderAssignmentsAndMonthlyCostsForTouchedMonths()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var firstEmployee = new Employee { FullNameAr = "رايدر أول", FullNameEn = "First rider" };
        var secondEmployee = new Employee { FullNameAr = "رايدر ثاني", FullNameEn = "Second rider" };
        var firstRider = new RiderProfile { EmployeeId = firstEmployee.Id };
        var secondRider = new RiderProfile { EmployeeId = secondEmployee.Id };
        var card = new FuelCard
        {
            Provider = FuelCardProvider.PetroApp, CardNumber = "BW101", NormalizedCardNumber = "BW101",
            IdentifierType = FuelCardIdentifierType.InternalNumber
        };
        db.AddRange(firstEmployee, secondEmployee, firstRider, secondRider, card);
        db.FuelCardRiderAssignments.AddRange(
            new FuelCardRiderAssignment
            {
                FuelCardId = card.Id, RiderProfileId = firstRider.Id, EmployeeId = firstEmployee.Id,
                EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = new DateOnly(2026, 2, 28)
            },
            new FuelCardRiderAssignment
            {
                FuelCardId = card.Id, RiderProfileId = secondRider.Id, EmployeeId = secondEmployee.Id,
                EffectiveFrom = new DateOnly(2026, 3, 1)
            });
        db.FuelCardMonthlyUsages.AddRange(
            Usage(card.Id, firstRider.Id, firstEmployee.Id, 1, 10m, 100m),
            Usage(card.Id, firstRider.Id, firstEmployee.Id, 2, 5m, 50m),
            Usage(card.Id, secondRider.Id, secondEmployee.Id, 3, 7m, 75m));
        await db.SaveChangesAsync(ct);

        var result = await Service(db).GetPeriodUsageAsync(
            new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 10), null, null, 1, 50, ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Value!.UsageMonthFrom);
        Assert.Equal(new DateOnly(2026, 3, 1), result.Value.UsageMonthTo);
        Assert.Equal(225m, result.Value.TotalAmount);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(225m, item.TotalAmount);
        Assert.Equal(22m, item.TotalLiters);
        Assert.Equal(2, item.Riders.Count);
        var first = item.Riders[0];
        Assert.Equal(firstRider.Id, first.RiderProfileId);
        Assert.Equal(150m, first.TotalAmount);
        Assert.Equal([100m, 50m], first.UsageMonths.Select(month => month.TotalAmount));
        Assert.Equal(new DateOnly(2026, 1, 15), Assert.Single(first.Assignments).From);
        Assert.Equal(new DateOnly(2026, 2, 28), first.Assignments[0].To);
        var second = item.Riders[1];
        Assert.Equal(secondRider.Id, second.RiderProfileId);
        Assert.Equal(75m, second.TotalAmount);
        Assert.Equal(new DateOnly(2026, 3, 1), Assert.Single(second.Assignments).From);
        Assert.Equal(new DateOnly(2026, 3, 10), second.Assignments[0].To);
        Assert.Null(second.Assignments[0].EffectiveTo);
    }

    [Fact]
    public async Task RejectsInvalidOrOverlongPeriods()
    {
        await using var db = CreateContext();
        var service = Service(db);
        foreach (var (start, end) in new[]
        {
            (new DateOnly(2026, 3, 1), new DateOnly(2026, 2, 1)),
            (new DateOnly(2023, 1, 1), new DateOnly(2026, 1, 1))
        })
        {
            var result = await service.GetPeriodUsageAsync(
                start, end, null, null, 1, 50, TestContext.Current.CancellationToken);
            Assert.Equal(FuelErrors.InvalidReportPeriod.Code, result.Error.Code);
        }
    }

    [Fact]
    public async Task PaginatesCardsWhileKeepingTotalsForAllMatchingCards()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var employee = new Employee { FullNameAr = "رايدر" };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var first = new FuelCard
        {
            Provider = FuelCardProvider.PetroApp, CardNumber = "BW101", NormalizedCardNumber = "BW101"
        };
        var second = new FuelCard
        {
            Provider = FuelCardProvider.PetroApp, CardNumber = "BW102", NormalizedCardNumber = "BW102"
        };
        db.AddRange(employee, rider, first, second,
            Usage(first.Id, rider.Id, employee.Id, 1, 1m, 10m),
            Usage(second.Id, rider.Id, employee.Id, 1, 2m, 20m));
        await db.SaveChangesAsync(ct);
        var service = Service(db);
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 31);

        var firstPage = await service.GetPeriodUsageAsync(start, end, null, null, 1, 1, ct);
        var secondPage = await service.GetPeriodUsageAsync(start, end, null, null, 2, 1, ct);
        var filtered = await service.GetPeriodUsageAsync(start, end, null, "BW102", 1, 1, ct);

        Assert.Equal(2, firstPage.Value!.TotalCount);
        Assert.Equal(30m, firstPage.Value.TotalAmount);
        Assert.Equal(first.Id, Assert.Single(firstPage.Value.Items).FuelCardId);
        Assert.Equal(second.Id, Assert.Single(secondPage.Value!.Items).FuelCardId);
        Assert.Equal(1, filtered.Value!.TotalCount);
        Assert.Equal(20m, filtered.Value.TotalAmount);
    }

    [Fact]
    public async Task SqlServerPeriodQueryExecutesWhenConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGISTICS_INTEGRATION_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options);

        var result = await Service(db).GetPeriodUsageAsync(
            new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 5), null, null, 1, 2,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.NotNull(result.Value);
    }

    private static FuelCardMonthlyUsage Usage(Guid cardId, Guid riderId, Guid employeeId,
        int month, decimal liters, decimal amount) => new()
    {
        FuelCardId = cardId, RiderProfileId = riderId, EmployeeId = employeeId,
        ReportMonth = new DateOnly(2026, month, 1), TotalLiters = liters, TotalAmount = amount
    };

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FuelCardPeriodUsage_{Guid.NewGuid():N}", options => options.EnableNullChecks(false))
            .Options);

    private static FuelCardService Service(ApplicationDbContext db) =>
        new(db, new TestCurrentUser(), new AllowPermissionChecker(), TimeProvider.System);

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class AllowPermissionChecker : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
