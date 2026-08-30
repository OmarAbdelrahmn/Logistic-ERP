using System.Reflection;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehiclePlatformAccountAssignmentQueryTests
{
    [Fact]
    public void OrderedDetailedAssignmentListTranslatesToSqlServer()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer()
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new VehiclePlatformAccountAssignmentService(context, null!);
        var method = typeof(VehiclePlatformAccountAssignmentService).GetMethod(
            "CreateProjectionQuery",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var query = Assert.IsAssignableFrom<IQueryable>(method!.Invoke(service, [true, null, true]));
        var sql = query.ToQueryString();

        Assert.Contains("VehiclePlatformAccountAssignments", sql, StringComparison.Ordinal);
        Assert.Contains("PlatformRiderAccounts", sql, StringComparison.Ordinal);
        Assert.Contains("Vehicles", sql, StringComparison.Ordinal);
        Assert.Contains("VehicleRegistrations", sql, StringComparison.Ordinal);
        Assert.Contains("ClientPlatforms", sql, StringComparison.Ordinal);
        Assert.Contains("Sponsors", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE", sql, StringComparison.Ordinal);
        Assert.Contains("[v].[Id]", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
        Assert.Contains("[v].[ApprovedAtUtc] DESC", sql, StringComparison.Ordinal);
        Assert.Contains("[v].[Id] DESC", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingSwitchListTranslatesToSqlServer()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer()
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new VehiclePlatformAccountAssignmentService(context, null!);
        var method = typeof(VehiclePlatformAccountAssignmentService).GetMethod(
            "CreateSwitchProjectionQuery",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var query = Assert.IsAssignableFrom<IQueryable>(method!.Invoke(
            service,
            [null, VehiclePlatformAccountSwitchStatus.Pending, true]));
        var sql = query.ToQueryString();

        Assert.Contains("VehiclePlatformAccountSwitches", sql, StringComparison.Ordinal);
        Assert.Contains("Vehicles", sql, StringComparison.Ordinal);
        Assert.Contains("PlatformRiderAccounts", sql, StringComparison.Ordinal);
        Assert.Contains("VehicleRegistrations", sql, StringComparison.Ordinal);
        Assert.Contains("@status_Value", sql, StringComparison.Ordinal);
        Assert.Contains("[Status] = @", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
        Assert.Contains("[RequestedAtUtc]", sql, StringComparison.Ordinal);
    }
}
