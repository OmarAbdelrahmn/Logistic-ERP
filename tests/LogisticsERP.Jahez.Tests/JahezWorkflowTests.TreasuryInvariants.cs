using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Fact]
    public void TreasuryQueriesTranslateUsingSqlServerProvider()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedTreasury;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        IQueryable Query(string name, object[]? arguments = null) => (IQueryable)typeof(JahezWorkflowService)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(service, arguments)!;
        var attribution = Query("AttributionCandidates").ToQueryString();
        Assert.Contains("UNION ALL", attribution);
        Assert.Contains("Ownership", attribution);
        Assert.Contains("NOT EXISTS", Query("OutstandingCustodyPostings").ToQueryString());
        var postings = Query("FilterPostings", [TreasuryOctober, true]);
        var journal = Query("JournalRows", [postings]).ToQueryString();
        Assert.Contains("LEFT JOIN", journal); Assert.Contains("IqamaNo", journal);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task TreasuryTransferredDebtRetainsOriginSponsorAfterAccountSponsorChanges()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("treasury-debt");
        var originalSponsor = account.SponsorId!.Value;
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100, JahezOperationMode.SettleAndWithdraw, null, true), ct));
        var request = Success(await f.Service.CreateWorkflowRequestAsync("request", new(h.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: draft.Id), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", request.Request.Id, new(true, "اعتماد الترحيل"), ct));
        var debt = await f.Db.JahezTransferredDebts.SingleAsync(ct);
        var stored = await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct);
        stored.SponsorId = originalSponsor == Sponsor.ExpressGateId ? Sponsor.AlBawabaNextCompanyId : Sponsor.ExpressGateId;
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        Success(await f.Service.RepayTransferredDebtAsync("repay", debt.Id, new(JahezMoneyFund.Cashbox, "repayment", "سداد الدين الأصلي"), ct));
        var owner = await f.Db.Set<TreasuryOwnership>().SingleAsync(x => x.Source == TreasurySource.Debt && x.SourceId == debt.Id, ct);
        Assert.Equal(originalSponsor, owner.SponsorId);
        var movement = Assert.Single(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items);
        Assert.Equal(originalSponsor, movement.SponsorId);
        Assert.Equal(175, movement.Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TreasuryHandoverFailureRollsBackAllCompanyTransfers(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        await FundCustody(f, Sponsor.AlBawabaNextCompanyId, 100);
        await FundCustody(f, Sponsor.ExpressGateId, 100);
        var pending = Success(await f.Service.SubmitTreasuryHandoverAsync("submit", new(new(2026, 10, 6), "تسليم شركتين"), ct));
        // Simulate an independently discovered cash shortage after submission. The service must never partially confirm.
        var affected = pending.Lines[1];
        f.Db.Add(new TreasuryPosting { SubAccountId = affected.SubAccountId, SponsorId = affected.SponsorId,
            Amount = -10, Kind = TreasuryEntryKind.Adjustment, BatchId = Guid.NewGuid(), ActorUserId = f.User.UserId!.Value,
            OccurredAtUtc = f.Clock.UtcNow, Reference = "shortage-evidence", Reason = "اختبار فشل المطابقة" });
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        f.User.UserId = Guid.NewGuid();
        var confirmation = new TreasuryHandoverConfirm(pending.Lines.Select(l => new TreasuryCashCount(l.SubAccountId, l.Amount)).ToArray(),
            "استلام", Convert.ToBase64String(pending.Handover.RowVersion));
        Assert.Equal(ErrorType.Conflict, (await f.Service.ConfirmTreasuryHandoverAsync("confirm", pending.Handover.Id, confirmation, ct)).Error.Type);
        Assert.Equal(JahezCashboxHandoverStatus.Pending, (await f.Db.JahezCashboxHandovers.SingleAsync(x => x.Id == pending.Handover.Id, ct)).Status);
        Assert.False(await f.Db.Set<TreasuryPosting>().AnyAsync(p => p.CashboxHandoverId == pending.Handover.Id, ct));
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task TreasuryLegacyConfirmedHandoverIsNotReceivedTwice()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("treasury-legacy");
        Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 100, "fee"), ct));
        var old = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم قديم"), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", old.Id, CashboxConfirmation(old), ct));
        Success(await f.Service.BackfillTreasuryAsync("backfill", "ترحيل مثبت", ct));
        Assert.Equal(0, Success(await f.Service.BackfillTreasuryAsync("backfill-again", "ترحيل مثبت", ct)));
        foreach (var balance in Success(await f.Service.TreasuryBalancesAsync(ct)).Where(x => x.IsPosting))
            Success(await f.Service.VerifyTreasuryBalanceAsync($"verify-{balance.SubAccountId}", new(balance.SubAccountId, balance.Balance, "verified", "مطابقة فعلية"), ct));
        var ready = Success(await f.Service.TreasuryReadinessAsync(ct));
        Success(await f.Service.ActivateTreasuryAsync("activate", new(ready.RowVersion, "تفعيل"), ct));
        var preview = Success(await f.Service.PreviewTreasuryHandoverAsync(new(2026, 10, 6), ct));
        Assert.Equal(0, preview.PhysicalCash); Assert.Equal(0, preview.MovementCount);
        Assert.Equal(100, Success(await f.Service.TreasuryBalancesAsync(ct)).Where(x => x.IsPosting && x.Location == TreasuryLocation.Central).Sum(x => x.Balance));
    }

    [Fact]
    public async Task TreasuryConcurrentSqlConfirmationsPostOnlyOnce()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql: true); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f); await FundCustody(f, Sponsor.AlBawabaNextCompanyId, 100);
        var pending = Success(await f.Service.SubmitTreasuryHandoverAsync("submit", new(new(2026, 10, 6), "تسليم"), ct));
        f.User.UserId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(f.Db.Database.GetConnectionString())
            .AddInterceptors(new ApplicationPersistenceInterceptor(f.User, f.Clock)).Options;
        await using var first = new ApplicationDbContext(options, f.Clock);
        await using var second = new ApplicationDbContext(options, f.Clock);
        var a = new JahezWorkflowService(first, f.User, f.Permissions, f.Clock);
        var b = new JahezWorkflowService(second, f.User, f.Permissions, f.Clock);
        var request = new TreasuryHandoverConfirm(pending.Lines.Select(l => new TreasuryCashCount(l.SubAccountId, l.Amount)).ToArray(),
            "استلام", Convert.ToBase64String(pending.Handover.RowVersion));
        var results = await Task.WhenAll(a.ConfirmTreasuryHandoverAsync("first-confirm", pending.Handover.Id, request, ct),
            b.ConfirmTreasuryHandoverAsync("second-confirm", pending.Handover.Id, request, ct));
        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.IsFailure && x.Error.Type == ErrorType.Conflict);
        Assert.Equal(2, await f.Db.Set<TreasuryPosting>().CountAsync(p => p.CashboxHandoverId == pending.Handover.Id, ct));
    }
}
