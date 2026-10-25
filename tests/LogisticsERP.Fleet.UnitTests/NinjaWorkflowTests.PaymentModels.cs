using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task NinjaAccountReadsUseAssignmentPaymentSnapshotAndUnassignedAccountDefault()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var unassigned = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal("PayPerOrder", unassigned.PaymentModel);
        Assert.Null(unassigned.RiderType);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111",
            RiderType: "ExternalCar", PaymentModel: "Salary"), ct));
        // Simulate old account metadata that differs from the historical assignment snapshot.
        f.Account.PaymentModel = PlatformAccountPaymentModel.PayPerOrder;
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        var detail = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal("Salary", detail.PaymentModel);
        Assert.Equal("ExternalCar", detail.RiderType);
        Assert.Equal("Salary", Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items).PaymentModel);
    }

    [Theory]
    [InlineData("InternalCar", "PayPerOrder")]
    [InlineData("InternalCar", "Salary")]
    [InlineData("ExternalCar", "PayPerOrder")]
    [InlineData("ExternalCar", "Salary")]
    [InlineData("InternalMotorcycle", "PayPerOrder")]
    [InlineData("InternalMotorcycle", "Salary")]
    [InlineData("ExternalMotorcycle", "PayPerOrder")]
    [InlineData("ExternalMotorcycle", "Salary")]
    public async Task NinjaPaymentChoiceOverridesAccountDefaultAndPreservesBothChoicesInHistory(string riderType, string paymentModel)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var day = new DateOnly(2026, 10, 12);
        var otherModel = paymentModel == "Salary" ? "PayPerOrder" : "Salary";
        f.Account.PaymentModel = Enum.Parse<PlatformAccountPaymentModel>(otherModel);
        await f.Db.SaveChangesAsync(ct);
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id,
            new(f.Rider.Id, day, "0501111111", "initial", RiderType: riderType, PaymentModel: paymentModel), ct));
        Assert.Equal(paymentModel, assigned.Assignment.PaymentModel);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(Enum.Parse<PlatformAccountPaymentModel>(paymentModel), (await f.Db.RiderClientAssignments.SingleAsync(ct)).PaymentModel);
        Assert.Equal(Enum.Parse<PlatformAccountPaymentModel>(paymentModel), (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).PaymentModel);
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal(paymentModel, account.PaymentModel);
        Assert.Equal(riderType, account.RiderType);
        var listed = Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items);
        Assert.Equal(paymentModel, listed.PaymentModel);
        Assert.Equal(riderType, listed.RiderType);
        var returned = Value(await f.Service.ReturnAsync(f.Account.Id,
            new(day, "0502222222", null, "Return", assigned.Assignment.RowVersion), ct));
        Assert.Equal(paymentModel, returned.Assignment.PaymentModel);
        Assert.Equal(paymentModel, Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).PaymentModel);
        f.Clock.Now = f.Clock.Now.AddDays(1);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, day.AddDays(1), "0503333333",
            RiderType: "ExternalMotorcycle", PaymentModel: otherModel), ct));
        f.Db.ChangeTracker.Clear();
        var history = Value(await f.Simple.GetAccountAssignmentHistoryAsync(f.Account.Id, ct));
        var previous = Assert.Single(history, a => a.Id == assigned.Assignment.Id);
        Assert.Equal(paymentModel, previous.PaymentModel);
        Assert.Equal(riderType, previous.RiderType);
        Assert.Equal(otherModel, Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).PaymentModel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Commission")]
    [InlineData("salary")]
    [InlineData("2")]
    [InlineData("PayPerOrder, Salary")]
    public async Task InvalidPaymentChoiceDoesNotChangeAccountOrStageAssignmentCredentials(string? paymentModel)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var result = await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111",
            "initial", RiderType: "InternalCar", PaymentModel: paymentModel), ct);
        Assert.True(result.IsFailure);
        Assert.Equal("ninja.invalid", result.Error.Code);
        await f.Db.SaveChangesAsync(ct);
        Assert.Empty(f.Db.RiderClientAssignments);
        Assert.Empty(f.Db.NinjaAccountDetails);
        Assert.Empty(f.Db.PlatformAccountCredentialVersions);
        Assert.Equal(PlatformAccountPaymentModel.PayPerOrder, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).PaymentModel);
    }

    [Fact]
    public async Task UnsupportedPaymentChoiceDoesNotChangeAccountOrStageCredentials()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var platform = await f.Db.ClientPlatforms.SingleAsync(ct);
        platform.SupportedPaymentModels = SupportedPlatformPaymentModels.PayPerOrder;
        await f.Db.SaveChangesAsync(ct);
        var result = await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111",
            "initial", RiderType: "InternalCar", PaymentModel: "Salary"), ct);
        Assert.True(result.IsFailure);
        Assert.Equal("platform.payment_model_not_supported", result.Error.Code);
        await f.Db.SaveChangesAsync(ct);
        Assert.Empty(f.Db.RiderClientAssignments);
        Assert.Empty(f.Db.NinjaAccountDetails);
        Assert.Empty(f.Db.PlatformAccountCredentialVersions);
        Assert.Equal(PlatformAccountPaymentModel.PayPerOrder, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).PaymentModel);
    }

    [Theory]
    [InlineData("Salary")]
    [InlineData("PayPerOrder")]
    public async Task AssignmentLimitsUseSelectedPaymentModelInDecisionAndErrorDetails(string paymentModel)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var day = new DateOnly(2026, 10, 12);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, day, "0501111111", "initial",
            RiderType: "InternalCar", PaymentModel: "Salary"), ct));
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId,
            RegisteredEmployeeId = f.Account.RegisteredEmployeeId, OperatingCityId = f.Account.OperatingCityId,
            Code = "NINJA-2", ExternalAccountId = "SECOND", PaymentModel = paymentModel == "Salary"
                ? PlatformAccountPaymentModel.PayPerOrder : PlatformAccountPaymentModel.Salary };
        f.Db.Add(second);
        await f.Db.SaveChangesAsync(ct);
        var result = await f.Service.AssignAsync(second.Id, new(f.Rider.Id, day, "0502222222", "next",
            RiderType: "ExternalMotorcycle", PaymentModel: paymentModel), ct);
        Assert.True(result.IsFailure);
        var expected = paymentModel == "Salary" ? HrErrors.RiderSalaryAccountLimitReached : HrErrors.RiderAccountLimitReached;
        Assert.Equal(expected.Code, result.Error.Code);
        var details = Assert.IsType<PlatformAssignmentLimitErrorDetail>(result.Error.Details!["assignmentLimit"]);
        Assert.Equal(paymentModel, details.RequestedPaymentModel);
        await f.Db.SaveChangesAsync(ct);
        Assert.Single(f.Db.RiderClientAssignments);
        Assert.Single(f.Db.PlatformAccountCredentialVersions);
        Assert.Equal(PlatformRiderAccountStatus.Available, (await f.Db.PlatformRiderAccounts.SingleAsync(a => a.Id == second.Id, ct)).Status);
    }

    [Fact]
    public async Task GenericMetadataCannotRewriteActiveNinjaAssignmentPaymentChoice()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var sponsor = new Sponsor { RegistryNameAr = "Test sponsor", EmployerIdentityNumber = "TEST" };
        f.Db.Sponsors.Add(sponsor);
        f.Account.SponsorId = sponsor.Id;
        await f.Db.SaveChangesAsync(ct);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111",
            RiderType: "InternalCar", PaymentModel: "Salary"), ct));
        var token = Convert.ToBase64String(f.Account.RowVersion);
        var request = new SimplePlatformAccountUpsertRequest(f.Account.ClientPlatformId, f.Account.OperatingCityId,
            sponsor.Id, f.OwnerRider.Id, f.Account.Code, f.Account.ExternalAccountId, null, "PayPerOrder", "Assigned",
            null, null, null, null, null, null, token);
        Assert.Equal("ninja.use_workflow", (await f.Simple.UpdateAccountAsync(f.Account.Id, request, ct)).Error.Code);
        var legacy = new PlatformOperationsService(f.Db, f.User, f.Clock, f.Protector);
        var legacyRequest = new PlatformAccountUpsertRequest(f.Account.ClientPlatformId, f.Account.RegisteredEmployeeId,
            f.Account.OperatingCityId, sponsor.Id, f.Account.Code, f.Account.ExternalAccountId, null, "PayPerOrder",
            "Assigned", null, null, null, null, null, null, token);
        Assert.Equal("ninja.use_workflow", (await legacy.UpsertAccountAsync(f.Account.Id, legacyRequest, ct)).Error.Code);
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal(PlatformAccountPaymentModel.Salary, (await f.Db.RiderClientAssignments.SingleAsync(ct)).PaymentModel);
        Assert.Equal(PlatformAccountPaymentModel.Salary, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).PaymentModel);
    }
}
