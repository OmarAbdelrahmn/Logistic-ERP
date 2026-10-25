using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task GenericMetadataUpdatesCannotBypassNinjaStatusControlsOrSoftDeleteAnArchivedAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var sponsor = new Sponsor { RegistryNameAr = "Test sponsor", EmployerIdentityNumber = "TEST" };
        f.Db.Sponsors.Add(sponsor); f.Account.SponsorId = sponsor.Id;
        await f.Db.SaveChangesAsync(ct);
        var initial = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        var archived = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Archived", initial.RowVersion, "Admin choice"), ct));
        var request = new SimplePlatformAccountUpsertRequest(f.Account.ClientPlatformId, f.Account.OperatingCityId,
            sponsor.Id, f.OwnerRider.Id, f.Account.Code, f.Account.ExternalAccountId, null, "PayPerOrder", "Archived",
            "Admin choice", null, null, null, "Metadata edit", null, archived.RowVersion);
        Value(await f.Simple.UpdateAccountAsync(f.Account.Id, request, ct));
        Assert.False(f.Account.IsDeleted); Assert.Equal(PlatformRiderAccountStatus.Archived, f.Account.Status);
        Assert.Equal("ninja.use_workflow", (await f.Simple.UpdateAccountAsync(f.Account.Id, request with { Status = "Available" }, ct)).Error.Code);
        Assert.Equal("ninja.use_workflow", (await f.Simple.UpdateAccountAsync(f.Account.Id, request with { StatusReason = null }, ct)).Error.Code);
        var legacy = new PlatformOperationsService(f.Db, f.User, f.Clock, f.Protector);
        var legacyRequest = new PlatformAccountUpsertRequest(f.Account.ClientPlatformId, f.Account.RegisteredEmployeeId,
            f.Account.OperatingCityId, sponsor.Id, f.Account.Code, f.Account.ExternalAccountId, null, "PayPerOrder",
            "Available", null, null, null, null, null, null, archived.RowVersion);
        Assert.Equal("ninja.use_workflow", (await legacy.UpsertAccountAsync(f.Account.Id, legacyRequest, ct)).Error.Code);
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal(PlatformRiderAccountStatus.Archived, f.Account.Status); Assert.False(f.Account.IsDeleted);
    }

    [Fact]
    public async Task AccountReadsKeepOwnerActualRiderAndLoginPhonesSeparateAndHandleMissingPeople()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var owner = await f.Db.Employees.SingleAsync(e => e.Id == f.Account.RegisteredEmployeeId, ct);
        var employee = await f.Db.Employees.SingleAsync(e => e.Id == f.Rider.EmployeeId, ct);
        owner.PrimaryPhone = "0501111111"; owner.IqamaNo = "2000000001"; owner.FullNameEn = "Owner";
        employee.PrimaryPhone = "0502222222"; employee.IqamaNo = "2000000002"; employee.FullNameEn = "Rider";
        await f.Db.SaveChangesAsync(ct);
        var empty = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal("Owner", empty.Owner!.NameEn); Assert.Equal(owner.Id, empty.Owner.EmployeeId);
        Assert.Null(empty.ActualRider); Assert.Null(empty.ActualRiderProfileId); Assert.Null(empty.PhoneNumber);
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0503333333", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var account = Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items);
        Assert.Equal(empty.Owner, account.Owner); Assert.Equal("Rider", account.ActualRider!.NameEn);
        Assert.Equal("2000000001", account.Owner!.IqamaNo); Assert.Equal("0501111111", account.Owner.PhoneNumber);
        Assert.Equal("2000000002", account.ActualRider.IqamaNo); Assert.Equal("0502222222", account.ActualRider.PhoneNumber);
        Assert.Equal("0503333333", account.PhoneNumber); Assert.Equal(f.Rider.Id, account.ActualRiderProfileId);
        Assert.Equal(assigned.Assignment.Id, account.CurrentAssignmentId);
        Assert.Equal(assigned.Assignment.RowVersion, account.AssignmentRowVersion);
        Assert.Equal(f.Account.OperatingCityId, account.OperatingCityId); Assert.Equal(f.Account.Code, account.Code);
        employee.IsDeleted = true; owner.IsDeleted = true; await f.Db.SaveChangesAsync(ct);
        var missing = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Null(missing.Owner); Assert.Null(missing.ActualRider); Assert.Equal(f.Rider.Id, missing.ActualRiderProfileId);
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Retired")]
    [InlineData("Archived")]
    public async Task ManualStatusSurvivesReturnAndCanBeRestored(string status)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var changed = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new(status, assigned.Credentials.RowVersion, "  Admin choice  "), ct));
        Assert.Equal(status, changed.Status); Assert.Equal("Admin choice", changed.StatusReason);
        Assert.Equal(f.Rider.Id, changed.ActualRiderProfileId); Assert.False(f.Account.IsDeleted);
        Value(await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 12), "0502222222", null, "Return", assigned.Assignment.RowVersion), ct));
        var returned = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal(status, returned.Status); Assert.Equal("Admin choice", returned.StatusReason); Assert.Null(returned.ActualRider);
        Assert.Equal(changed.Owner, returned.Owner); Assert.Single(f.Db.RiderClientAssignments);
        Assert.True((await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct)).IsFailure);
        var active = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Active", returned.RowVersion), ct));
        Assert.Equal("Available", active.Status); Assert.Null(active.StatusReason);
    }

    [Fact]
    public async Task StatusValidationPermissionsConcurrencyAndDebtBlockPreventMutations()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var token = assigned.Credentials.RowVersion;
        Assert.True((await f.Service.SetAccountStatusAsync(f.Account.Id, new("Assigned", token), ct)).IsFailure);
        Assert.True((await f.Service.SetAccountStatusAsync(f.Account.Id, new("Suspended", "invalid"), ct)).IsFailure);
        Assert.True((await f.Service.SetAccountStatusAsync(f.Account.Id, new("Suspended", token, new string('x', 501)), ct)).IsFailure);
        f.Checker.Denied = PermissionKeys.Ninja.AccountsUpdate;
        Assert.Equal("ninja.forbidden", (await f.Service.SetAccountStatusAsync(f.Account.Id, new("Suspended", token), ct)).Error.Code);
        Assert.Equal(PlatformRiderAccountStatus.Assigned, f.Account.Status);
        f.Checker.Denied = null;
        var suspended = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Suspended", token), ct));
        var active = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Active", suspended.RowVersion), ct));
        Assert.Equal("Assigned", active.Status);
        f.Db.Set<JahezTransferredDebt>().Add(new() { OperationId = Guid.NewGuid(), RiderProfileId = f.Rider.Id, Amount = 10 });
        await f.Db.SaveChangesAsync(ct);
        var blocked = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Suspended", active.RowVersion), ct));
        Assert.True(blocked.IsOperationallyBlocked);
        Assert.Equal("rider.jahez_debt_block", (await f.Service.SetAccountStatusAsync(f.Account.Id, new("Active", blocked.RowVersion), ct)).Error.Code);
        Assert.Equal(PlatformRiderAccountStatus.Suspended, f.Account.Status);
        f.Checker.Denied = PermissionKeys.Ninja.Read;
        Assert.True((await f.Service.GetAccountAsync(f.Account.Id, ct)).IsFailure);
    }
}
