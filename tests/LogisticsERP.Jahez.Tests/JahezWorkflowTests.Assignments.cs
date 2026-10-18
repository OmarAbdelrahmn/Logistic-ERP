using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Platform;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    private static readonly string[] CombinedChefzCodes = ["CHEFZ", "THECHEFZ", "CHEFSZ", "SHIFTZ"];
    private static readonly bool[] CombinedLegacyPaths = [false, true];
    private static readonly int[] CombinedAssignmentCounts = [0, 1, 2];
    private static readonly int[] CombinedAssignmentSlots = [1, 2, 3];
    public static TheoryData<string, bool, int> CombinedAssignmentOrders => new(
        from code in CombinedChefzCodes
        from legacy in CombinedLegacyPaths
        from chefzBeforeJahez in CombinedAssignmentCounts
        select (code, legacy, chefzBeforeJahez));

    [Theory]
    [MemberData(nameof(CombinedAssignmentOrders))]
    public async Task RiderCanTakeTwoChefzPlusOneJahezInAnyOrderAndReplaceEither(string code, bool legacy, int chefzBeforeJahez)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var jahez = await f.Account("combined-jahez");
        var nextJahez = await f.Account("combined-jahez-next");
        var platform = new ClientPlatform { Code = code, NameAr = code, NameEn = code,
            SupportedPaymentModels = SupportedPlatformPaymentModels.PayPerOrder | SupportedPlatformPaymentModels.Salary };
        var contract = new ClientContract { ClientPlatformId = platform.Id, Code = "COMBINED",
            DisplayNameAr = code, DisplayNameEn = code, Status = ClientContractStatus.Active };
        f.Db.AddRange(platform, contract);
        await f.Db.SaveChangesAsync(ct);
        var chefz = new List<PlatformRiderAccount>();
        for (var i = 0; i < 3; i++)
        {
            var account = await f.Account($"combined-chefz-{i}");
            var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(a => a.Id == account.Id, ct);
            tracked.ClientPlatformId = platform.Id;
            await f.Db.SaveChangesAsync(ct);
            chefz.Add(tracked);
        }
        var simple = new SimplePlatformService(f.Db, f.User, f.Clock, new PlatformCredentialProtector(() => new byte[32]));
        var operations = new PlatformOperationsService(f.Db, f.User, f.Clock, new PlatformCredentialProtector(() => new byte[32]));
        async Task<Result> AssignChefz(int index) => legacy
            ? await operations.AssignAccountAsync(new(f.RiderId, contract.Id, chefz[index].Id, new(2026, 10, 1),
                "Active", "استلام", null, null, false, null), ct)
            : await simple.AssignAccountAsync(chefz[index].Id, new(f.RiderId, new(2026, 10, 1), "استلام", false, null), ct);
        async Task<Result> SetChefzSalary(int index)
        {
            var account = await f.Db.PlatformRiderAccounts.SingleAsync(a => a.Id == chefz[index].Id, ct);
            var owner = await f.Db.RiderProfiles.SingleAsync(r => r.EmployeeId == account.RegisteredEmployeeId, ct);
            account.RowVersion = [42];
            await f.Db.SaveChangesAsync(ct);
            return legacy
                ? await operations.UpsertAccountAsync(account.Id, new(account.ClientPlatformId, owner.EmployeeId,
                    account.OperatingCityId, account.SponsorId!.Value, account.Code, account.ExternalAccountId, null,
                    "Salary", "Assigned", null, null, null, null, null, null, "Kg=="), ct)
                : await simple.UpdateAccountAsync(account.Id, new(account.ClientPlatformId, account.OperatingCityId,
                    account.SponsorId!.Value, owner.Id, account.Code, account.ExternalAccountId, null,
                    "Salary", "Assigned", null, null, null, null, null, null, "Kg=="), ct);
        }

        for (var i = 0; i < chefzBeforeJahez; i++) Assert.True((await AssignChefz(i)).IsSuccess);
        if (chefzBeforeJahez == 2)
        {
            var fullChefz = await AssignChefz(2);
            Assert.Equal("platform.rider_account_limit_reached", fullChefz.Error.Code);
            Assert.Empty(Assert.IsType<PlatformAssignmentLimitErrorDetail>(fullChefz.Error.Details!["assignmentLimit"]).AllowedPaymentModels);
        }
        var h = Success(await f.Service.HandoverAsync("jahez", new(jahez.Id, f.RiderId, At(1), "استلام", 0), ct));
        // A second Jahez must fail even before the total reaches three.
        Assert.True((await f.Service.HandoverAsync("second-jahez", new(nextJahez.Id, f.RiderId, At(1), "استلام", 0), ct)).IsFailure);
        for (var i = chefzBeforeJahez; i < 2; i++) Assert.True((await AssignChefz(i)).IsSuccess);
        f.Db.ChangeTracker.Clear();
        var active = await f.Db.RiderClientAssignments.Where(a => a.RiderProfileId == f.RiderId && a.EffectiveTo == null)
            .OrderBy(a => a.RiderAccountSlot).ToArrayAsync(ct);
        Assert.Equal(CombinedAssignmentSlots, active.Select(a => a.RiderAccountSlot));
        var rejected = await AssignChefz(2);
        Assert.Equal("platform.rider_account_limit_reached", rejected.Error.Code);
        var limits = Assert.IsType<PlatformAssignmentLimitErrorDetail>(rejected.Error.Details!["assignmentLimit"]);
        Assert.Equal(3, limits.MaximumActiveAccounts);
        Assert.Empty(limits.AllowedPaymentModels);
        Assert.Equal(3, await f.Db.RiderClientAssignments.CountAsync(a => a.RiderProfileId == f.RiderId && a.EffectiveTo == null, ct));
        // Full combined capacity still permits a valid payment-model edit, with one Salary account globally.
        var salaryEdit = await SetChefzSalary(1);
        Assert.True(salaryEdit.IsSuccess, salaryEdit.Error.Description);
        Assert.Equal("platform.rider_salary_account_limit_reached", (await SetChefzSalary(0)).Error.Code);

        var released = await f.Db.RiderClientAssignments.SingleAsync(a => a.PlatformRiderAccountId == chefz[0].Id && a.EffectiveTo == null, ct);
        released.RowVersion = [42];
        await f.Db.SaveChangesAsync(ct);
        Result release = legacy
            ? await operations.CloseAssignmentAsync(released.Id, new(new(2026, 10, 6), "Ended", "إرجاع", "Kg=="), ct)
            : await simple.ReleaseAccountAsync(chefz[0].Id, new(new(2026, 10, 6), "Ended", "إرجاع", "Kg=="), ct);
        Assert.True(release.IsSuccess, release.Error.Description);
        Assert.True((await AssignChefz(2)).IsSuccess);
        Assert.Equal(released.RiderAccountSlot, (await f.Db.RiderClientAssignments.SingleAsync(a => a.PlatformRiderAccountId == chefz[2].Id, ct)).RiderAccountSlot);

        var change = Success(await f.Service.CreateWorkflowRequestAsync("switch", new(h.Id, JahezApprovalKind.FreeSwitch,
            "تبديل جاهز", TargetAccountId: nextJahez.Id, EffectiveAtUtc: At(6, 12)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve-switch", change.Request.Id, new(true, "اعتماد"), ct));
        var remaining = await f.Db.RiderClientAssignments.Where(a => a.RiderProfileId == f.RiderId && a.EffectiveTo == null).ToArrayAsync(ct);
        Assert.Equal(3, remaining.Length);
        Assert.Contains(remaining, a => a.PlatformRiderAccountId == nextJahez.Id);
        Assert.DoesNotContain(remaining, a => a.PlatformRiderAccountId == jahez.Id);
    }

    [Theory]
    [InlineData("CHEFZ", 0, 0, true)]
    [InlineData("CHEFZ", 1, 0, true)]
    [InlineData("CHEFZ", 2, 0, false)]
    [InlineData("CHEFZ", 0, 1, true)]
    [InlineData("CHEFZ", 1, 1, true)]
    [InlineData("CHEFZ", 2, 1, false)]
    [InlineData("JAHEZ", 0, 0, true)]
    [InlineData("JAHEZ", 1, 0, true)]
    [InlineData("JAHEZ", 2, 0, true)]
    [InlineData("JAHEZ", 0, 1, false)]
    [InlineData("JAHEZ", 1, 1, false)]
    [InlineData("JAHEZ", 2, 1, false)]
    public void CombinedAssignmentPolicyEnforcesEachPlatformLimit(string requested, int chefz, int jahez, bool allowed)
    {
        var ppo = PlatformAccountPaymentModel.PayPerOrder;
        var accounts = Enumerable.Repeat(((string?)"CHEFZ", ppo), chefz).Concat(Enumerable.Repeat(((string?)"JAHEZ", ppo), jahez));
        Assert.Equal(allowed ? PlatformAccountAssignmentDecision.Allowed : PlatformAccountAssignmentDecision.AccountLimitReached,
            ChefzAccountAssignmentRules.Evaluate(requested, accounts, ppo));
        Assert.Equal(PlatformAccountAssignmentDecision.AccountLimitReached,
            ChefzAccountAssignmentRules.Evaluate(requested, [("KEETA", ppo)], ppo));
        Assert.Equal(PlatformAccountAssignmentDecision.SalaryAccountLimitReached,
            ChefzAccountAssignmentRules.Evaluate(requested, [("CHEFZ", PlatformAccountPaymentModel.Salary)], PlatformAccountPaymentModel.Salary));
    }
}
