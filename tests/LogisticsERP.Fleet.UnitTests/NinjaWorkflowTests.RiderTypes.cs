using LogisticsERP.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Theory]
    [InlineData("InternalCar", false)]
    [InlineData("ExternalCar", false)]
    [InlineData("InternalMotorcycle", false)]
    [InlineData("ExternalMotorcycle", false)]
    [InlineData("InternalCar", true)]
    [InlineData("ExternalCar", true)]
    [InlineData("InternalMotorcycle", true)]
    [InlineData("ExternalMotorcycle", true)]
    public async Task AssignmentSavesRiderTypeAndReturnsItInLifecycleAccountAndHistory(string riderType, bool sql)
    {
        Assert.SkipWhen(sql && Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER") is null,
            "Set NINJA_SQL_TEST_SERVER to verify rider types against disposable SQL Server databases.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var day = new DateOnly(2026, 10, 12);
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id,
            new(f.Rider.Id, day, "0501111111", "initial", RiderType: riderType, PaymentModel: "PayPerOrder"), ct));
        Assert.Equal(riderType, assigned.Assignment.RiderType);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(Enum.Parse<NinjaRiderType>(riderType), (await f.Db.RiderClientAssignments.SingleAsync(ct)).NinjaRiderType);
        Assert.Equal(riderType, Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).RiderType);
        Assert.Equal(riderType, Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items).RiderType);
        if (sql)
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => f.Db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [app].[RiderClientAssignments] SET [NinjaRiderType] = 'Invalid' WHERE [Id] = {assigned.Assignment.Id}", ct));
            Assert.Equal(547, error.Number);
        }

        var returned = Value(await f.Service.ReturnAsync(f.Account.Id,
            new(day, "0502222222", null, "Return", assigned.Assignment.RowVersion), ct));
        Assert.Equal(riderType, returned.Assignment.RiderType);
        Assert.Null(Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).RiderType);
        f.Clock.Now = f.Clock.Now.AddDays(1);
        var nextType = riderType == "ExternalMotorcycle" ? "InternalCar" : "ExternalMotorcycle";
        Value(await f.Service.AssignAsync(f.Account.Id,
            new(f.OwnerRider.Id, day.AddDays(1), "0503333333", RiderType: nextType, PaymentModel: "PayPerOrder"), ct));
        f.Db.ChangeTracker.Clear();
        var history = Value(await f.Simple.GetAccountAssignmentHistoryAsync(f.Account.Id, ct));
        Assert.Equal(riderType, Assert.Single(history, a => a.Id == assigned.Assignment.Id).RiderType);
        Assert.Equal(nextType, Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).RiderType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Car")]
    [InlineData("internalCar")]
    [InlineData("1")]
    [InlineData("InternalCar, ExternalCar")]
    public async Task MissingOrInvalidRiderTypeDoesNotSaveAssignmentPhoneOrPassword(string? riderType)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var result = await f.Service.AssignAsync(f.Account.Id,
            new(f.Rider.Id, new(2026, 10, 12), "0501111111", "initial", RiderType: riderType, PaymentModel: "PayPerOrder"), ct);
        Assert.True(result.IsFailure);
        Assert.Equal("ninja.invalid", result.Error.Code);
        await f.Db.SaveChangesAsync(ct);
        Assert.Empty(f.Db.RiderClientAssignments);
        Assert.Empty(f.Db.NinjaAccountDetails);
        Assert.Empty(f.Db.PlatformAccountCredentialVersions);
        Assert.Empty(f.Db.RiderAssignmentEvents);
        Assert.Equal(PlatformRiderAccountStatus.Available, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).Status);
    }

    [Fact]
    public async Task LegacyAssignmentsRemainReadableWithoutAnAssumedRiderType()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id,
            new(f.Rider.Id, new(2026, 10, 12), "0501111111", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var assignment = await f.Db.RiderClientAssignments.SingleAsync(ct);
        assignment.NinjaRiderType = null;
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        Assert.Null(Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).RiderType);
        Assert.Null(Assert.Single(Value(await f.Simple.GetAccountAssignmentHistoryAsync(f.Account.Id, ct))).RiderType);
        var returned = Value(await f.Service.ReturnAsync(f.Account.Id,
            new(new(2026, 10, 12), "0502222222", "return", "Return", assigned.Assignment.RowVersion), ct));
        Assert.Null(returned.Assignment.RiderType);
    }
}
