using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Platform;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Theory]
    [InlineData("CHEFZ", false)]
    [InlineData("THECHEFZ", false)]
    [InlineData("CHEFSZ", false)]
    [InlineData("SHIFTZ", false)]
    [InlineData("CHEFZ", true)]
    [InlineData("THECHEFZ", true)]
    [InlineData("CHEFSZ", true)]
    [InlineData("SHIFTZ", true)]
    public async Task TwoChefzAccountsUseBothSlotsRejectThirdAndCanBeReleasedEditedAndReplaced(string code, bool legacy)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        (await f.Db.ClientPlatforms.SingleAsync(ct)).Code = code;
        var sponsor = new Sponsor { RegistryNameAr = "كفيل الاختبار", EmployerIdentityNumber = "7000000001" };
        var firstOwner = new RiderProfile { EmployeeId = f.Account.RegisteredEmployeeId!.Value };
        f.Db.AddRange(sponsor, firstOwner);
        f.Account.SponsorId = sponsor.Id;
        f.Account.Status = PlatformRiderAccountStatus.Assigned;
        f.Account.RowVersion = [1]; f.Second.RowVersion = [2];
        var actual = await f.Db.RiderProfiles.SingleAsync(r => r.Id == f.Second.RiderProfileId, ct);
        (await f.Db.Employees.SingleAsync(e => e.Id == actual.EmployeeId, ct)).Status = EmployeeStatus.Active;
        (PlatformRiderAccount Account, RiderProfile Owner) AddAccount(string id)
        {
            var employee = new Employee { FullNameAr = id };
            var owner = new RiderProfile { EmployeeId = employee.Id };
            var account = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, RegisteredEmployeeId = employee.Id,
                OperatingCityId = f.Account.OperatingCityId, SponsorId = sponsor.Id, Code = id, ExternalAccountId = id, RowVersion = [1] };
            f.Db.AddRange(employee, owner, account);
            return (account, owner);
        }
        var second = AddAccount("CHEFZ-2");
        var third = AddAccount("CHEFZ-3");
        await f.Db.SaveChangesAsync(ct);
        var simple = new SimplePlatformService(f.Db, new CurrentUser(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        var operations = new PlatformOperationsService(f.Db, new CurrentUser(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        async Task<Result> Assign(PlatformRiderAccount account, DateOnly? from = null) => legacy
            ? await operations.AssignAccountAsync(new(actual.Id, f.Second.ClientContractId, account.Id,
                from ?? new(2026, 10, 19), "Active", "Take", null, null, false, null), ct)
            : await simple.AssignAccountAsync(account.Id, new(actual.Id, from ?? new(2026, 10, 19), "Take", false, null), ct);
        async Task<Result> ChangePayment(PlatformRiderAccount account, RiderProfile owner) => legacy
            ? await operations.UpsertAccountAsync(account.Id, new(account.ClientPlatformId, owner.EmployeeId, account.OperatingCityId, sponsor.Id,
                account.Code, account.ExternalAccountId, null, "Salary", "Assigned", null, null, null, null, null, null, "AQ=="), ct)
            : await simple.UpdateAccountAsync(account.Id, new(account.ClientPlatformId, account.OperatingCityId, sponsor.Id, owner.Id,
                account.Code, account.ExternalAccountId, null, "Salary", "Assigned", null, null, null, null, null, null, "AQ=="), ct);

        var taken = await Assign(second.Account);
        Assert.True(taken.IsSuccess, taken.Error.Description);
        var active = await f.Db.RiderClientAssignments.Where(a => a.RiderProfileId == actual.Id && a.EffectiveTo == null).OrderBy(a => a.RiderAccountSlot).ToArrayAsync(ct);
        Assert.Collection(active, a => Assert.Equal(1, a.RiderAccountSlot), a => Assert.Equal(2, a.RiderAccountSlot));
        Assert.Equal(PlatformRiderAccountStatus.Assigned, second.Account.Status);
        Assert.Equal(2, Value(await f.Service.GetAccountsAsync(1, 50, ct)).Summary.Taken);

        var rejected = await Assign(third.Account);
        Assert.Equal("platform.rider_account_limit_reached", rejected.Error.Code);
        var limits = Assert.IsType<PlatformAssignmentLimitErrorDetail>(rejected.Error.Details!["assignmentLimit"]);
        Assert.Equal(2, limits.MaximumActiveAccounts);
        Assert.Equal(2, limits.ActiveAccounts.Count);
        Assert.Empty(limits.AllowedPaymentModels);
        Assert.Equal(PlatformRiderAccountStatus.Available, third.Account.Status);
        Assert.Equal(3, await f.Db.RiderClientAssignments.CountAsync(ct));
        var edited = await ChangePayment(second.Account, second.Owner);
        Assert.True(edited.IsSuccess, edited.Error.Description);
        Assert.Equal("platform.rider_salary_account_limit_reached", (await ChangePayment(f.Account, firstOwner)).Error.Code);
        Assert.Equal(PlatformAccountPaymentModel.PayPerOrder, f.Account.PaymentModel);
        Assert.Equal(PlatformAccountPaymentModel.PayPerOrder, f.Second.PaymentModel);

        Result release = legacy
            ? await operations.CloseAssignmentAsync(f.Second.Id, new(new(2026, 10, 20), "Ended", "Return", "Ag=="), ct)
            : await simple.ReleaseAccountAsync(f.Account.Id, new(new(2026, 10, 20), "Ended", "Return", "Ag=="), ct);
        Assert.True(release.IsSuccess, release.Error.Description);
        Assert.Equal(PlatformRiderAccountStatus.Available, f.Account.Status);
        Assert.Equal(PlatformRiderAccountStatus.Assigned, second.Account.Status);
        Assert.Equal(1, Value(await f.Service.GetAccountsAsync(1, 50, ct)).Summary.Taken);
        var replacement = await Assign(third.Account, new(2026, 10, 21));
        Assert.True(replacement.IsSuccess, replacement.Error.Description);
        Assert.Equal(1, (await f.Db.RiderClientAssignments.SingleAsync(a => a.PlatformRiderAccountId == third.Account.Id && a.EffectiveTo == null, ct)).RiderAccountSlot);
        Assert.Equal(2, Value(await f.Service.GetAccountsAsync(1, 50, ct)).Summary.Taken);
    }

    [Theory]
    [InlineData("CHEFZ", "KEETA", false)]
    [InlineData("KEETA", "CHEFZ", false)]
    [InlineData("KEETA", "KEETA", false)]
    [InlineData("NINJA", "CHEFZ", false)]
    [InlineData("CHEFZ", "KEETA", true)]
    [InlineData("KEETA", "CHEFZ", true)]
    [InlineData("KEETA", "KEETA", true)]
    [InlineData("NINJA", "CHEFZ", true)]
    public async Task ChefzExceptionDoesNotPermitSecondGenericOrMixedPlatformAccount(string existingCode, string requestedCode, bool legacy)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var existingPlatform = await f.Db.ClientPlatforms.SingleAsync(ct);
        existingPlatform.Code = existingCode;
        var targetPlatform = existingPlatform;
        if (existingCode != requestedCode)
        {
            targetPlatform = new ClientPlatform { Code = requestedCode, NameAr = requestedCode, NameEn = requestedCode };
            f.Db.Add(targetPlatform);
        }
        var rider = await f.Db.RiderProfiles.SingleAsync(r => r.Id == f.Second.RiderProfileId, ct);
        (await f.Db.Employees.SingleAsync(e => e.Id == rider.EmployeeId, ct)).Status = EmployeeStatus.Active;
        var contract = new ClientContract { ClientPlatformId = targetPlatform.Id, Code = "NEW", DisplayNameAr = "اختبار", DisplayNameEn = "Test" };
        var account = new PlatformRiderAccount { ClientPlatformId = targetPlatform.Id, OperatingCityId = f.Account.OperatingCityId, Code = "NEW", ExternalAccountId = "NEW" };
        f.Db.AddRange(contract, account);
        await f.Db.SaveChangesAsync(ct);
        Result result = legacy
            ? await new PlatformOperationsService(f.Db, new CurrentUser(), new Clock(), null!).AssignAccountAsync(new(rider.Id, contract.Id,
                account.Id, new(2026, 10, 19), "Active", null, null, null, false, null), ct)
            : await new SimplePlatformService(f.Db, new CurrentUser(), new Clock(), null!).AssignAccountAsync(account.Id,
                new(rider.Id, new(2026, 10, 19), null, false, null), ct);
        Assert.Equal("platform.rider_account_limit_reached", result.Error.Code);
        var limits = Assert.IsType<PlatformAssignmentLimitErrorDetail>(result.Error.Details!["assignmentLimit"]);
        Assert.Equal(1, limits.MaximumActiveAccounts);
        Assert.Single(limits.ActiveAccounts);
        Assert.Empty(limits.AllowedPaymentModels);
        Assert.Equal(2, await f.Db.RiderClientAssignments.CountAsync(ct));
    }

    [Fact]
    public void ChefzAssignmentPolicySupportsAllAliasesAndPreservesSalaryAndGenericLimits()
    {
        var ppo = PlatformAccountPaymentModel.PayPerOrder;
        var salary = PlatformAccountPaymentModel.Salary;
        foreach (var code in new[] { "CHEFZ", "THECHEFZ", "CHEFSZ", "SHIFTZ" })
        {
            Assert.True(ChefzAccountAssignmentRules.IsChefzPlatform(code));
            Assert.Equal(PlatformAccountAssignmentDecision.Allowed, ChefzAccountAssignmentRules.Evaluate(true, [(code, ppo)], ppo));
            Assert.Equal(PlatformAccountAssignmentDecision.Allowed, ChefzAccountAssignmentRules.Evaluate(true, [(code, salary)], ppo));
            Assert.Equal(PlatformAccountAssignmentDecision.Allowed, ChefzAccountAssignmentRules.Evaluate(true, [(code, ppo)], salary));
            Assert.Equal(PlatformAccountAssignmentDecision.SalaryAccountLimitReached, ChefzAccountAssignmentRules.Evaluate(true, [(code, salary)], salary));
            Assert.Equal(PlatformAccountAssignmentDecision.AccountLimitReached, ChefzAccountAssignmentRules.Evaluate(true, [(code, ppo), ("SHIFTZ", ppo)], ppo));
            Assert.Equal(PlatformAccountAssignmentDecision.AccountLimitReached, ChefzAccountAssignmentRules.Evaluate(false, [(code, ppo)], ppo));
        }
        Assert.Equal(PlatformAccountAssignmentDecision.AccountLimitReached, ChefzAccountAssignmentRules.Evaluate(true, [("KEETA", ppo)], ppo));
        Assert.Equal(PlatformAccountAssignmentDecision.AccountLimitReached, PlatformAccountAssignmentPolicy.Evaluate([ppo], ppo));
    }

    [Fact]
    public void PlatformAssignmentContextQueryTranslatesForSqlServerWithExcludedAssignment()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer().Options);
        var sql = PlatformAssignmentPolicySupport.CurrentAssignments(db, Guid.NewGuid(), Guid.NewGuid()).ToQueryString();
        Assert.Contains("ClientPlatforms", sql, StringComparison.Ordinal);
        Assert.Contains("RiderAccountSlot", sql, StringComparison.Ordinal);
        Assert.Contains("EffectiveTo", sql, StringComparison.Ordinal);
        Assert.Contains("<>", sql, StringComparison.Ordinal);
    }
}
