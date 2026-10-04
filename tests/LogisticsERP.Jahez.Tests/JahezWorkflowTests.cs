using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.SystemServices;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed class JahezWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FullWorkflowKeepsDebtOnPreviousRiderAndSeparatesCashboxSections(bool sql)
    {
        Assert.SkipUnless(!sql || OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1",
            "Set LOGISTICS_JAHEZ_SQL_TESTS=1 to run against a uniquely named disposable LocalDB database.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("456469");
        var initial = new JahezHandoverRequest(account.Id, f.RiderId, At(1), "تسليم الحساب", 80m);
        var handover = Success(await f.Service.HandoverAsync("new", initial, ct));
        var duplicate = Success(await f.Service.HandoverAsync("new", initial, ct));
        Assert.Equal(handover.Id, duplicate.Id);
        Assert.Single(await f.Db.Set<JahezAccountHandover>().ToArrayAsync(ct));
        Assert.True((await f.Service.HandoverAsync("new", initial with { InitialFeePayment = 90m }, ct)).IsFailure);
        var first = Success(await f.Service.GetBalanceAsync(handover.Id, new(2026, 10, 6), ct));
        Assert.Equal(120m, first.Fees);
        Assert.Equal(75m, first.UnpostedCommission);
        var imported = Success(await f.Service.UploadAsync("upload", new(JahezImportKind.Transactions,
            [new("transactions.xlsx", Transactions("456469", "10/2/2026 1:03:44\u202fAM", -502m, 0m, -500m, 2m))]), ct));
        Assert.Empty(imported.Issues);
        Success(await f.Service.CommitImportAsync("commit", imported.BatchId, new(), ct));
        Success(await f.Service.CommitImportAsync("commit-again", imported.BatchId, new(), ct));
        var payment = Success(await f.Service.PayAsync("pay", new(handover.Id, new(2026, 10, 6), 50m, 100m, 75m, true, "تصفية جزئية"), ct));
        Assert.Equal(225m, payment.FeePayment + payment.DebtPayment + payment.CommissionPayment);
        var balance = Success(await f.Service.GetBalanceAsync(handover.Id, new(2026, 10, 6), ct));
        Assert.Equal(70m, balance.Fees);
        Assert.Equal(400m, balance.PlatformDebt);
        Assert.Equal(0m, balance.PostedCommission);
        Assert.Equal(0m, balance.UnpostedCommission);
        Assert.Equal(f.Clock.GetUtcNow(), balance.ReminderAnchorAtUtc);

        var reset = Success(await f.Service.RequestApprovalAsync("reset", new(handover.Id, JahezApprovalKind.ResetAccount,
            "المندوب لم يحضر", EffectiveAtUtc: At(6, 20)), ct));
        Assert.True((await f.Service.DecideApprovalAsync("self", reset.Request.Id, new(true, "اعتماد"), ct)).IsFailure);
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideApprovalAsync("approve-reset", reset.Request.Id, new(true, "اعتماد نقل الدين"), ct));
        var resetCashbox = Success(await f.Service.GetCashboxAsync(ct));
        Assert.Equal(130m, resetCashbox.Fees);
        Assert.Equal(175m, resetCashbox.Settlements);
        var closed = Success(await f.Service.GetBalanceAsync(handover.Id, new(2026, 10, 6), ct));
        Assert.True(closed.DebtTransferred);
        Assert.Equal(470m, closed.TotalReceivable);
        var next = Success(await f.Service.HandoverAsync("next-rider", new(account.Id, f.OtherRiderId, At(6, 21), "مندوب جديد", 0), ct));
        Assert.NotEqual(handover.Id, next.Id);
        Assert.Equal(200m, Success(await f.Service.GetBalanceAsync(next.Id, new(2026, 10, 6), ct)).TotalReceivable);

        // A late imported transaction before the reset stays on the old handover.
        var late = Success(await f.Service.UploadAsync("late", new(JahezImportKind.Transactions,
            [new("late.xlsx", Transactions("456469", "10/3/2026 1:00:00 AM", -25m, 0m, -25m, 0m))]), ct));
        Success(await f.Service.CommitImportAsync("late-commit", late.BatchId, new(), ct));
        Assert.Equal(425m, Success(await f.Service.GetBalanceAsync(handover.Id, new(2026, 10, 6), ct)).PlatformDebt);
        Assert.Equal(0m, Success(await f.Service.GetBalanceAsync(next.Id, new(2026, 10, 6), ct)).PlatformDebt);

        var dispatch = Success(await f.Service.UploadAsync("dispatch", new(JahezImportKind.DailyDispatches,
            [new("dispatch.xlsx", Dispatches("456469", "06-10-2026", 14))]), ct));
        Assert.Contains(dispatch.Issues, x => x.Code == "assignment_ambiguous");
        Success(await f.Service.CommitImportAsync("dispatch-commit", dispatch.BatchId,
            new([new(dispatch.Rows[0].RowId, handover.Id, 10, "قبل الإغلاق"), new(dispatch.Rows[0].RowId, next.Id, 4, "بعد الاستلام")]), ct));
        var report = Success(await f.Service.GetDispatchesAsync(new(2026, 10, 6), new(2026, 10, 6), null, 1, 50, ct));
        Assert.Equal(14, report.Items.Sum(x => x.Count));
        Assert.Contains(report.Items, x => x.RiderProfileId == f.RiderId && x.Count == 10);
        Assert.Contains(report.Items, x => x.RiderProfileId == f.OtherRiderId && x.Count == 4);

        var transfer = Success(await f.Service.SubmitCashboxAsync("submit", new(new(2026, 10, 6), "التسليم اليومي"), ct));
        Assert.Equal(130m, transfer.FeeAmount);
        Assert.Equal(175m, transfer.SettlementAmount);
        f.User.UserId = Guid.NewGuid();
        Assert.True((await f.Service.ConfirmCashboxAsync("mismatch", transfer.Id, new(120m, 185m, "المبلغ"), ct)).IsFailure);
        Success(await f.Service.ConfirmCashboxAsync("confirm", transfer.Id, new(130m, 175m, "تم الاستلام"), ct));
        Assert.True((await f.Service.DecideCashboxAsync("accountant-approve", transfer.Id, new(true, "اعتماد"), ct)).IsFailure);
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideCashboxAsync("cashbox-approve", transfer.Id, new(true, "مطابق"), ct));
        var box = Success(await f.Service.GetCashboxAsync(ct));
        Assert.Equal(0m, box.Fees);
        Assert.Equal(0m, box.Settlements);
        Assert.Equal(495m, Success(await f.Service.GetBalanceAsync(handover.Id, new(2026, 10, 6), ct)).TotalReceivable);
        Assert.Contains(await f.Db.Notifications.ToArrayAsync(ct), x => x.EventType == "jahez.approval.decided");
    }

    [Fact]
    public async Task PercentagePolicyReplacesDailyChargeRequiresCompleteDetailsAndLocksPostedPeriod()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("100");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var request = Success(await f.Service.RequestApprovalAsync("percentage", new(h.Id, JahezApprovalKind.PercentageCommission,
            "أداء منخفض", FromDate: new(2026, 10, 2), ToDate: new(2026, 10, 5)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideApprovalAsync("approve", request.Request.Id, new(true, "موافق"), ct));
        Assert.False(Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct)).CommissionComplete);
        Assert.True((await f.Service.PayAsync("missing", new(h.Id, new(2026, 10, 6), 0, 0, 15, true, "تصفية"), ct)).IsFailure);
        var earnings = new JahezEarningsRequest(h.Id, new(2026, 10, 2), new(2026, 10, 5), 1000, 20, 200, 30, 10, 40, 50, 20, 10, "بيان الأرباح");
        Success(await f.Service.RecordEarningsAsync("earnings", earnings, ct));
        var balance = Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct));
        Assert.True(balance.CommissionComplete);
        Assert.Equal(144m, balance.UnpostedCommission); // (1000-20-200-30-10+40+50+20+10)*15% + 15
        Success(await f.Service.PayAsync("settle", new(h.Id, new(2026, 10, 6), 0, 0, 144, true, "تصفية"), ct));
        Assert.True((await f.Service.RecordEarningsAsync("rewrite", earnings, ct)).IsFailure);
        var retro = Success(await f.Service.RequestApprovalAsync("retro", new(h.Id, JahezApprovalKind.PercentageCommission,
            "طلب قديم", FromDate: new(2026, 10, 2), ToDate: new(2026, 10, 3)), ct));
        f.User.UserId = Guid.NewGuid();
        Assert.True((await f.Service.DecideApprovalAsync("retro-approve", retro.Request.Id, new(true, "اعتماد"), ct)).IsFailure);
    }

    [Fact]
    public async Task FreeSwitchPreservesOriginalFeesAndCommissionAndCreatesNoNewCharge()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var old = await f.Account("200"); var target = await f.Account("201");
        var h = Success(await f.Service.HandoverAsync("start", new(old.Id, f.RiderId, At(1), "استلام", 100), ct));
        var r = Success(await f.Service.RequestApprovalAsync("switch", new(h.Id, JahezApprovalKind.FreeSwitch, "ضعف الطلبات",
            TargetAccountId: target.Id, EffectiveAtUtc: At(4)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideApprovalAsync("approve", r.Request.Id, new(true, "تبديل مجاني"), ct));
        var next = (await f.Db.Set<JahezAccountHandover>().SingleAsync(x => x.PlatformRiderAccountId == target.Id, ct)).Id;
        Assert.Equal(0m, Success(await f.Service.GetBalanceAsync(next, new(2026, 10, 4), ct)).Fees);
        var previous = Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct));
        Assert.Equal(100m, previous.Fees);
        Assert.Equal(45m, previous.PostedCommission);
        Assert.Equal(0m, previous.UnpostedCommission);
        Assert.Equal(100m, Success(await f.Service.GetCashboxAsync(ct)).Fees);
    }

    [Fact]
    public async Task FeeExceptionCannotRefundCollectedFeesAndPartialSettlementChangesReminder()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("300");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 80), ct));
        var r = Success(await f.Service.RequestApprovalAsync("waive", new(h.Id, JahezApprovalKind.FeeException, "استثناء", WaiverAmount: 130), ct));
        f.User.UserId = Guid.NewGuid();
        Assert.True((await f.Service.DecideApprovalAsync("invalid", r.Request.Id, new(true, "اعتماد"), ct)).IsFailure);
        Assert.Equal(120m, Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct)).Fees);
        f.Clock.UtcNow = At(12);
        Assert.True(Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 12), ct)).IsOverdue);
        Success(await f.Service.PayAsync("fee-only", new(h.Id, new(2026, 10, 12), 20, 0, 0, false, "رسوم فقط"), ct));
        Assert.True(Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 12), ct)).IsOverdue);
        Success(await f.Service.PayAsync("partial-settlement", new(h.Id, new(2026, 10, 12), 10, 0, 0, true, "تصفية جزئية"), ct));
        Assert.False(Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 12), ct)).IsOverdue);
        Assert.Equal(90m, Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 12), ct)).Fees);
    }

    [Fact]
    public async Task ImportReplacementUsesReversalAndRepeatedUploadDoesNotDoubleDebt()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("400");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var file = new JahezUploadFile("source.xlsx", Transactions("400", "10/2/2026 1:00:00 AM", -100, 0, -100, 0));
        var upload = Success(await f.Service.UploadAsync("upload", new(JahezImportKind.Transactions, [file]), ct));
        Success(await f.Service.CommitImportAsync("commit", upload.BatchId, new(), ct));
        Assert.Equal(upload.BatchId, Success(await f.Service.UploadAsync("upload-again", new(JahezImportKind.Transactions, [file]), ct)).BatchId);
        var replacement = new JahezUploadFile("replacement.xlsx", Transactions("400", "10/2/2026 1:00:00 AM", -120, 0, -120, 0));
        var overlap = Success(await f.Service.UploadAsync("overlap", new(JahezImportKind.Transactions, [replacement]), ct));
        Assert.Contains(overlap.Issues, x => x.Code == "overlapping_period");
        Assert.True((await f.Service.CommitImportAsync("overlap-commit", overlap.BatchId, new(), ct)).IsFailure);
        // A correction is a distinct version with an explicit replacement reference.
        var corrected = new JahezUploadFile("corrected.xlsx", Transactions("400", "10/2/2026 1:00:01 AM", -120, 0, -120, 0));
        var correction = Success(await f.Service.UploadAsync("correction", new(JahezImportKind.Transactions, [corrected], upload.BatchId, "تصحيح تقرير جاهز"), ct));
        Success(await f.Service.CommitImportAsync("corrected-commit", correction.BatchId, new(), ct));
        Assert.Equal(120m, Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct)).PlatformDebt);
        Assert.Single(await f.Db.Set<JahezLedgerEntry>().Where(x => x.ReversesEntryId != null).ToArrayAsync(ct));
    }

    [Fact]
    public async Task CashboxSubmissionSnapshotsEntriesAndLaterReceiptsRemainForTheNextHandover()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("550");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 80), ct));
        var transfer = Success(await f.Service.SubmitCashboxAsync("submit", new(new(2026, 10, 6), "اليومية"), ct));
        Success(await f.Service.PayAsync("later", new(h.Id, new(2026, 10, 6), 20, 0, 0, false, "تحصيل بعد إنشاء التسليم"), ct));
        var pending = Success(await f.Service.GetCashboxAsync(ct));
        Assert.Equal(80m, pending.ReservedFees); Assert.Equal(20m, pending.AvailableFees);
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxAsync("confirm", transfer.Id, new(80, 0, "استلام"), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideCashboxAsync("approve", transfer.Id, new(true, "اعتماد"), ct));
        Assert.Equal(20m, Success(await f.Service.GetCashboxAsync(ct)).AvailableFees);
        Assert.Equal(100m, Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct)).Fees);
    }

    [Fact]
    public async Task PositiveNetAmountCreatesACreditWithoutACashboxPayout()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("560");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var batch = Success(await f.Service.UploadAsync("credit", new(JahezImportKind.Transactions,
            [new("credit.xlsx", Transactions("560", "10/2/2026 1:00:00 AM", 500, 0, 500, 0))]), ct));
        var summary = Assert.Single(batch.Accounts!);
        Assert.Equal(500m, summary.NetAmount); Assert.Equal(-500m, summary.PlatformDebtChange);
        Assert.Equal(new DateOnly(2026, 10, 2), summary.FromDate); Assert.False(summary.HasIssues);
        Success(await f.Service.CommitImportAsync("credit-commit", batch.BatchId, new(), ct));
        var balance = Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct));
        Assert.Equal(-500m, balance.PlatformDebt); Assert.Equal(-225m, balance.TotalReceivable);
        Assert.Empty(await f.Db.Set<JahezCashboxEntry>().ToArrayAsync(ct));
        Assert.True((await f.Service.PayAsync("no-debt", new(h.Id, new(2026, 10, 6), 0, 1, 0, true, "لا يوجد دين جاهز للتحصيل"), ct)).IsFailure);
    }

    [Fact]
    public async Task PermissionScopeIsCheckedBeforeMutationsAndCashboxRejectionReleasesReservation()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("500");
        f.Permissions.Denied = PermissionKeys.Jahez.HandoversManage;
        Assert.True((await f.Service.HandoverAsync("denied", new(a.Id, f.RiderId, At(1), "استلام", 80), ct)).IsFailure);
        Assert.Empty(await f.Db.Set<JahezAccountHandover>().ToArrayAsync(ct));
        f.Permissions.Denied = null;
        Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 80), ct));
        var box = Success(await f.Service.SubmitCashboxAsync("box", new(new(2026, 10, 6), "تسليم"), ct));
        Assert.Equal(80m, Success(await f.Service.GetCashboxAsync(ct)).ReservedFees);
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideCashboxAsync("reject", box.Id, new(false, "لم يستلم المحاسب"), ct));
        Assert.Equal(80m, Success(await f.Service.GetCashboxAsync(ct)).AvailableFees);
        Assert.All(f.Permissions.Scopes, x => Assert.Equal(f.PlatformId, x.TargetId));
    }

    [Fact]
    public async Task RemindersAreDeduplicatedResolveAfterPaymentAndRemainAfterReset()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("600");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"JahezIdentity_{Guid.NewGuid():N}", o => o.EnableNullChecks(false)).Options);
        identity.Users.Add(new ApplicationUser { Id = f.User.UserId!.Value, UserName = "collector", NormalizedUserName = "COLLECTOR",
            DisplayNameAr = "المسؤول", DisplayNameEn = "Collector", Status = UserAccountStatus.Active });
        await identity.SaveChangesAsync(ct);
        var reminders = new JahezReminderService(f.Db, identity, f.Permissions, f.Clock, f.Service);
        f.Clock.UtcNow = At(12);
        await reminders.RunAsync(ct); await reminders.RunAsync(ct);
        Assert.Single(await f.Db.Notifications.Where(x => x.EventType == "jahez.settlement.overdue").ToArrayAsync(ct));
        f.Permissions.RequiredScope = f.PlatformId;
        var feed = new NotificationService(f.Db, identity, f.User, f.Clock, f.Permissions);
        Assert.Single(Success(await feed.QueryAsync(new([PermissionKeys.Jahez.Read], false, 50, null), ct)).Items);
        f.Permissions.Denied = PermissionKeys.Jahez.Read;
        Assert.Empty(Success(await feed.QueryAsync(new([PermissionKeys.Jahez.Read], false, 50, null), ct)).Items);
        f.Permissions.Denied = null;
        var reset = Success(await f.Service.RequestApprovalAsync("reset", new(h.Id, JahezApprovalKind.ResetAccount,
            "المندوب غائب", EffectiveAtUtc: At(12)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideApprovalAsync("approve-reset", reset.Request.Id, new(true, "اعتماد"), ct));
        await reminders.RunAsync(ct);
        Assert.Single(await f.Db.Notifications.Where(x => x.EventType == "jahez.settlement.overdue" && x.ArchivedAtUtc == null).ToArrayAsync(ct));
        Success(await f.Service.PayAsync("pay", new(h.Id, new(2026, 10, 12), 1, 0, 0, true, "دفعة تصفية جزئية"), ct));
        await reminders.RunAsync(ct);
        Assert.Empty(await f.Db.Notifications.Where(x => x.EventType == "jahez.settlement.overdue" && x.ArchivedAtUtc == null).ToArrayAsync(ct));
        f.Clock.UtcNow = At(23);
        await reminders.RunAsync(ct);
        Assert.Equal(2, await f.Db.Set<JahezReminderState>().CountAsync(ct));
        Assert.Single(await f.Db.Notifications.Where(x => x.EventType == "jahez.settlement.overdue" && x.ArchivedAtUtc == null).ToArrayAsync(ct));
    }

    [Fact]
    public async Task ReplayedApprovalStillRequiresCurrentApprovalPermission()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("700");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var r = Success(await f.Service.RequestApprovalAsync("exception", new(h.Id, JahezApprovalKind.FeeException, "استثناء", WaiverAmount: 50), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideApprovalAsync("approve", r.Request.Id, new(true, "اعتماد"), ct));
        f.Permissions.Denied = PermissionKeys.Jahez.RequestsApprove;
        Assert.True((await f.Service.DecideApprovalAsync("approve", r.Request.Id, new(true, "اعتماد"), ct)).IsFailure);
    }

    [Fact]
    public async Task LateDebtReopensAResolvedReminderWithoutDuplicatingItsEpisode()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("750");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        Success(await f.Service.CloseAsync("close", h.Id, new(At(1, 12), "إغلاق في يوم الاستلام"), ct));
        Success(await f.Service.PayAsync("pay", new(h.Id, new(2026, 10, 6), 200, 0, 0, true, "تصفية الرسوم"), ct));
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"LateDebtIdentity_{Guid.NewGuid():N}", o => o.EnableNullChecks(false)).Options);
        identity.Users.Add(new ApplicationUser { Id = f.User.UserId!.Value, UserName = "collector", NormalizedUserName = "COLLECTOR",
            DisplayNameAr = "المسؤول", DisplayNameEn = "Collector", Status = UserAccountStatus.Active });
        await identity.SaveChangesAsync(ct);
        f.Clock.UtcNow = At(20);
        var reminders = new JahezReminderService(f.Db, identity, f.Permissions, f.Clock, f.Service);
        var first = Success(await f.Service.UploadAsync("late-1", new(JahezImportKind.Transactions,
            [new("late-1.xlsx", Transactions("750", "10/1/2026 10:00:00 AM", -50, 0, -50, 0))]), ct));
        Success(await f.Service.CommitImportAsync("late-1-commit", first.BatchId, new(), ct));
        await reminders.RunAsync(ct);
        Success(await f.Service.AdjustAsync("correction", new(h.Id, JahezLedgerBucket.PlatformDebt, -50, "تصحيح موثق"), ct));
        await reminders.RunAsync(ct);
        Assert.Empty(await f.Db.Notifications.Where(x => x.ArchivedAtUtc == null).ToArrayAsync(ct));
        var second = Success(await f.Service.UploadAsync("late-2", new(JahezImportKind.Transactions,
            [new("late-2.xlsx", Transactions("750", "10/1/2026 11:00:00 AM", -25, 0, -25, 0))]), ct));
        Success(await f.Service.CommitImportAsync("late-2-commit", second.BatchId, new(), ct));
        await reminders.RunAsync(ct); await reminders.RunAsync(ct);
        var notification = Assert.Single(await f.Db.Notifications.Where(x => x.ArchivedAtUtc == null).ToArrayAsync(ct));
        Assert.Contains("25.00", notification.BodyEn, StringComparison.Ordinal);
        Assert.Single(await f.Db.Set<JahezReminderState>().ToArrayAsync(ct));
    }

    [Fact]
    public async Task SqlConcurrentPaymentAndFailedSwitchAreAtomic()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable LOGISTICS_JAHEZ_SQL_TESTS with a working LocalDB to verify transactions and concurrency.");
        await using var f = await Fixture.Create(true); var ct = TestContext.Current.CancellationToken;
        var old = await f.Account("SQL-1"); var occupied = await f.Account("SQL-2");
        var h = Success(await f.Service.HandoverAsync("old", new(old.Id, f.RiderId, At(1), "استلام", 0), ct));
        var switchRequest = Success(await f.Service.RequestApprovalAsync("switch", new(h.Id, JahezApprovalKind.FreeSwitch,
            "طلب تبديل", TargetAccountId: occupied.Id, EffectiveAtUtc: At(3)), ct));
        Success(await f.Service.HandoverAsync("occupied", new(occupied.Id, f.OtherRiderId, At(1), "أصبح الحساب مستخدمًا", 0), ct));
        f.User.UserId = Guid.NewGuid();
        Assert.True((await f.Service.DecideApprovalAsync("failed-switch", switchRequest.Request.Id, new(true, "اعتماد"), ct)).IsFailure);
        Assert.Null((await f.Db.Set<JahezAccountHandover>().AsNoTracking().SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
        Assert.Equal(JahezApprovalStatus.Pending, (await f.Db.Set<JahezApprovalRequest>().AsNoTracking().SingleAsync(x => x.Id == switchRequest.Request.Id, ct)).Status);
        var connection = f.Db.Database.GetConnectionString()!;
        async Task<Result<JahezRiderSettlement>> Collect()
        {
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection,
                o => o.EnableRetryOnFailure()).AddInterceptors(new ApplicationPersistenceInterceptor(f.User, f.Clock)).Options, f.Clock);
            return await new JahezService(db, f.User, f.Permissions, f.Clock).PayAsync("same-payment",
                new(h.Id, new(2026, 10, 6), 10, 0, 0, false, "دفعة واحدة مع طلبين متزامنين"), ct);
        }
        var results = await Task.WhenAll(Collect(), Collect());
        Assert.Contains(results, x => x.IsSuccess);
        var replay = Success(await Collect());
        f.Db.ChangeTracker.Clear();
        Assert.Equal(1, await f.Db.Set<JahezRiderSettlement>().CountAsync(x => x.HandoverId == h.Id, ct));
        Assert.Equal(10m, Success(await f.Service.GetCashboxAsync(ct)).Fees);
        Assert.All(results.Where(x => x.IsSuccess), x => Assert.Equal(replay.Id, x.Value!.Id));
    }

    [Fact]
    public async Task EachAccountAccruesDailyWithoutOrdersAndLegacyAdoptionDoesNotInventHistoricalFees()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("800"); var b = await f.Account("801");
        var first = Success(await f.Service.HandoverAsync("first", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var second = Success(await f.Service.HandoverAsync("second", new(b.Id, f.RiderId, At(1), "استلام", 0), ct));
        Assert.Equal(75m, Success(await f.Service.GetBalanceAsync(first.Id, new(2026, 10, 6), ct)).UnpostedCommission);
        Assert.Equal(75m, Success(await f.Service.GetBalanceAsync(second.Id, new(2026, 10, 6), ct)).UnpostedCommission);
        var legacy = await f.Account("802");
        var contract = await f.Db.ClientContracts.FirstAsync(ct);
        var assignment = new RiderClientAssignment { RiderProfileId = f.OtherRiderId, PlatformRiderAccountId = legacy.Id,
            ClientContractId = contract.Id, EffectiveFrom = new(2026, 9, 1), Status = RiderAssignmentStatus.Active, AssignedByUserId = f.User.UserId!.Value };
        f.Db.Add(assignment); (await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == legacy.Id, ct)).Status = PlatformRiderAccountStatus.Assigned;
        await f.Db.SaveChangesAsync(ct);
        var adopted = Success(await f.Service.AdoptLegacyAsync("adopt", new(assignment.Id, new(2026, 10, 6), 500m, 70m, 30m, "أرصدة افتتاحية"), ct));
        var balance = Success(await f.Service.GetBalanceAsync(adopted.Id, new(2026, 10, 6), ct));
        Assert.Equal(500m, balance.PlatformDebt); Assert.Equal(70m, balance.Fees); Assert.Equal(30m, balance.PostedCommission);
        Assert.Equal(15m, balance.UnpostedCommission);
        Assert.True(adopted.IsLegacy);
    }

    [Fact]
    public async Task MalformedUnknownAndMultiDayImportsRemainUncommitted()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var unknown = Success(await f.Service.UploadAsync("unknown", new(JahezImportKind.Transactions,
            [new("unknown.xlsx", Transactions("not-known", "10/2/2026 1:00:00 AM", -1, 0, -1, 0))]), ct));
        Assert.Contains(unknown.Issues, x => x.Code == "unknown_account");
        Assert.True((await f.Service.CommitImportAsync("unknown-commit", unknown.BatchId, new(), ct)).IsFailure);
        Assert.Empty(await f.Db.Set<JahezTransaction>().ToArrayAsync(ct));
        var a = await f.Account("900");
        Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        using var w = new XLWorkbook(new MemoryStream(Dispatches("900", "02-10-2026", 14)));
        w.Worksheet(1).Cell(2, 3).Value = "03-10-2026";
        using var s = new MemoryStream(); w.SaveAs(s);
        var multi = Success(await f.Service.UploadAsync("multi", new(JahezImportKind.DailyDispatches, [new("multi.xlsx", s.ToArray())]), ct));
        Assert.Contains(multi.Issues, x => x.Code == "multi_day_report");
        var malformed = Success(await f.Service.UploadAsync("malformed", new(JahezImportKind.Transactions,
            [new("malformed.xlsx", Transactions("900", "instruction instead of a date", 0, 0, 0, 0))]), ct));
        Assert.Contains(malformed.Issues, x => x.Code == "parse_error");
        using var mixedWorkbook = new XLWorkbook(new MemoryStream(Transactions("900", "10/2/2026 1:00:00 AM", -1, 0, -1, 0, duplicate: true)));
        mixedWorkbook.Worksheet(1).Cell(3, 2).Value = "invalid date";
        using var mixedStream = new MemoryStream(); mixedWorkbook.SaveAs(mixedStream);
        var mixed = Success(await f.Service.UploadAsync("mixed", new(JahezImportKind.Transactions, [new("mixed.xlsx", mixedStream.ToArray())]), ct));
        var summary = Assert.Single(mixed.Accounts!);
        Assert.True(summary.HasIssues); Assert.Equal(1, summary.ValidRowCount); Assert.Equal(-1m, summary.NetAmount);
    }

    [Fact]
    public async Task PercentagePolicyCanBeSettledInConsecutiveSlicesWithoutDoubleCharging()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("1000");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var r = Success(await f.Service.RequestApprovalAsync("policy", new(h.Id, JahezApprovalKind.PercentageCommission,
            "نسبة للفترة", FromDate: new(2026, 10, 2), ToDate: new(2026, 10, 10)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideApprovalAsync("approve", r.Request.Id, new(true, "اعتماد"), ct));
        Success(await f.Service.RecordEarningsAsync("first-details", new(h.Id, new(2026, 10, 2), new(2026, 10, 3), 100, 0, 0, 0, 0, 0, 0, 0, 0, "الفترة الأولى"), ct));
        Success(await f.Service.PayAsync("first-payment", new(h.Id, new(2026, 10, 3), 0, 0, 15, true, "تصفية أولى"), ct));
        Success(await f.Service.RecordEarningsAsync("second-details", new(h.Id, new(2026, 10, 4), new(2026, 10, 6), 200, 0, 0, 0, 0, 0, 0, 0, 0, "الفترة الثانية"), ct));
        Assert.Equal(30m, Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct)).UnpostedCommission);
        Success(await f.Service.PayAsync("second-payment", new(h.Id, new(2026, 10, 6), 0, 0, 30, true, "تصفية ثانية"), ct));
        var accruals = await f.Db.Set<JahezLedgerEntry>().Where(x => x.Bucket == JahezLedgerBucket.Commission && x.Kind == JahezLedgerKind.Charge).ToArrayAsync(ct);
        Assert.Equal(45m, accruals.Sum(x => x.Amount));
        Assert.All(accruals, x => { Assert.NotNull(x.CalculationJson); Assert.Equal(x.Id, x.SourceId); Assert.NotNull(x.FromDate); Assert.NotNull(x.ThroughDate); });
    }

    [Fact]
    public async Task FailedPaymentDoesNotLeaveCommissionStagedAndIdenticalReplayKeepsAuditMetadata()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("1100");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        Assert.True((await f.Service.PayAsync("invalid", new(h.Id, new(2026, 10, 6), 0, 0, 1000, true, "تصفية غير صالحة"), ct)).IsFailure);
        Assert.Null((await f.Db.Set<JahezAccountHandover>().SingleAsync(x => x.Id == h.Id, ct)).CommissionPostedThrough);
        Assert.Empty(await f.Db.Set<JahezLedgerEntry>().Where(x => x.Bucket == JahezLedgerBucket.Commission).ToArrayAsync(ct));
        var request = new JahezPaymentRequest(h.Id, new(2026, 10, 6), 0, 0, 20, true, "دفعة جزئية");
        var first = Success(await f.Service.PayAsync("valid", request, ct));
        var replay = Success(await f.Service.PayAsync("valid", request, ct));
        Assert.Equal(first.CreatedAtUtc, replay.CreatedAtUtc); Assert.NotEqual(default, replay.CreatedAtUtc);
        Assert.Equal(first.CreatedByUserId, replay.CreatedByUserId);
        Assert.Equal(55m, Success(await f.Service.GetBalanceAsync(h.Id, new(2026, 10, 6), ct)).PostedCommission);
    }

    [Fact]
    public async Task DashboardSponsorIsIndependentInPrimaryAndCompatibilityApisAndJahezAssignmentsCannotBypassFees()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var sponsor = await f.Db.Sponsors.FirstAsync(ct);
        var city = await f.Db.OperatingCities.FirstAsync(ct);
        var dashboard = new Sponsor { RegistryNameAr = "كفيل الداشبورد", Status = CatalogStatus.Active };
        f.Db.Add(dashboard); await f.Db.SaveChangesAsync(ct);
        var simple = new SimplePlatformService(f.Db, f.User, f.Clock, new UnusedProtector());
        var compatibility = new PlatformOperationsService(f.Db, f.User, f.Clock, new UnusedProtector());
        var request = new SimplePlatformAccountUpsertRequest(f.PlatformId, city.Id, sponsor.Id, f.RiderId,
            "PRIMARY", "1200", null, "PayPerOrder", "Available", null, null, null, null, null, null, null);
        Assert.True((await simple.CreateAccountAsync(request, ct)).IsFailure);
        var primary = Success(await simple.CreateAccountAsync(request with { DashboardSponsorId = dashboard.Id }, ct));
        Assert.Equal(dashboard.Id, primary.DashboardSponsorId); Assert.Equal(sponsor.Id, primary.SponsorId);
        var employee = await f.Db.RiderProfiles.Where(x => x.Id == f.OtherRiderId).Select(x => x.EmployeeId).SingleAsync(ct);
        var compatibleRequest = new PlatformAccountUpsertRequest(f.PlatformId, employee, city.Id, sponsor.Id,
            "COMPAT", "1201", null, "PayPerOrder", "Available", null, null, null, null, null, null, null);
        Assert.True((await compatibility.UpsertAccountAsync(null, compatibleRequest, ct)).IsFailure);
        var compatible = Success(await compatibility.UpsertAccountAsync(null, compatibleRequest with { DashboardSponsorId = dashboard.Id }, ct));
        Assert.Equal(dashboard.Id, compatible.DashboardSponsorId); Assert.Equal(sponsor.Id, compatible.SponsorId);
        Assert.Equal(2, Success(await compatibility.GetAccountsAsync(f.PlatformId, null, dashboard.Id, ct)).Count);
        Assert.Empty(Success(await simple.GetAccountsAsync(null, f.PlatformId, null, null, null, null, null, null, false, false, sponsor.Id, ct)));
        var bypass = await simple.AssignAccountAsync(primary.Id, new(f.RiderId, new(2026, 10, 1), "تجاوز", false, null), ct);
        Assert.True(bypass.IsFailure); Assert.Equal(JahezErrors.UseJahezWorkflow.Code, bypass.Error.Code);
        Assert.Empty(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
        var h = Success(await f.Service.HandoverAsync("legitimate", new(primary.Id, f.RiderId, At(1), "استلام", 0), ct));
        var contract = await f.Db.ClientContracts.FirstAsync(ct);
        var bypassCompat = await compatibility.AssignAccountAsync(new(f.OtherRiderId, contract.Id, compatible.Id,
            new(2026, 10, 1), "Active", "تجاوز", null, null, false, null), ct);
        Assert.True(bypassCompat.IsFailure); Assert.Equal(JahezErrors.UseJahezWorkflow.Code, bypassCompat.Error.Code);
        var close = await compatibility.CloseAssignmentAsync(h.AssignmentId, new(new(2026, 10, 6), "Ended", "تجاوز", ""), ct);
        Assert.True(close.IsFailure); Assert.Equal(JahezErrors.UseJahezWorkflow.Code, close.Error.Code);
        Assert.Null((await f.Db.Set<JahezAccountHandover>().SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
    }

    [Fact]
    public async Task SuppliedReportsParseWithTheirOriginalPrecisionAndDailyCounts()
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var transactions = Path.Combine(downloads, "transactions-organization-24321-from-01_09_2026-to-30_09_2026 (3).xlsx");
        var dispatches = Path.Combine(downloads, "drivers-dispatches-23532-from-02_10_2026-to-02_10_2026.xlsx");
        Assert.SkipUnless(File.Exists(transactions) && File.Exists(dispatches), "The supplied source reports are not available on this machine.");
        var ct = TestContext.Current.CancellationToken;
        var movements = JahezSpreadsheetParser.Parse(await File.ReadAllBytesAsync(transactions, ct), Guid.NewGuid(), JahezImportKind.Transactions);
        var daily = JahezSpreadsheetParser.Parse(await File.ReadAllBytesAsync(dispatches, ct), Guid.NewGuid(), JahezImportKind.DailyDispatches);
        Assert.Equal(485, movements.Count); Assert.Equal(114, daily.Count);
        Assert.All(movements, x => Assert.Null(x.ParseError)); Assert.All(daily, x => Assert.Null(x.ParseError));
        Assert.Equal(-10188.144m, movements.Sum(x => x.NetAmount));
        Assert.Equal(1106, daily.Sum(x => x.Dispatches));
        Assert.Contains(movements, x => JahezRules.RiyadhDate(x.OccurredAtUtc) == new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void SqlModelHasFinancialUniquenessAndTranslatesCashboxAndTemporalQueries()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedJahezModel;Trusted_Connection=True").Options);
        var script = db.Database.GenerateCreateScript();
        Assert.Contains("[jahez].[JahezLedgerEntry]", script, StringComparison.Ordinal);
        Assert.Contains("[EndedAtUtc] IS NULL AND [IsDeleted] = 0", script, StringComparison.Ordinal);
        Assert.Contains("[ReversesEntryId] IS NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains("DashboardSponsorId", script, StringComparison.Ordinal);
        var grouped = (from e in db.Set<JahezCashboxEntry>()
            join h in db.Set<JahezCashboxHandover>() on e.CashboxHandoverId equals h.Id into handovers
            from h in handovers.DefaultIfEmpty()
            where h == null || h.Status != JahezCashboxHandoverStatus.Approved
            group e by new { e.Section, Reserved = e.CashboxHandoverId != null } into g
            select new { g.Key.Section, g.Key.Reserved, Amount = g.Sum(e => e.Amount) }).ToQueryString();
        Assert.Contains("GROUP BY", grouped, StringComparison.Ordinal);
        var overdue = db.Set<JahezAccountHandover>().Where(x => x.EndedAtUtc == null
            || db.Set<JahezLedgerEntry>().Where(l => l.HandoverId == x.Id).Sum(l => l.Amount) > 0).ToQueryString();
        Assert.Contains("SUM", overdue, StringComparison.Ordinal);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void SpreadsheetParserPreservesIdenticalTransactionsAndRiyadhBoundary()
    {
        var content = Transactions("456469", "10/1/2026 1:03:44\u202fAM", -22.006m, 0, -20.006m, 2, duplicate: true);
        var rows = JahezSpreadsheetParser.Parse(content, Guid.NewGuid(), JahezImportKind.Transactions);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Null(r.ParseError));
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 22, 3, 44, TimeSpan.Zero), rows[0].OccurredAtUtc);
        Assert.Equal(new DateOnly(2026, 10, 1), JahezRules.RiyadhDate(rows[0].OccurredAtUtc));
        Assert.Equal(-20.006m, rows[0].NetAmount);
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void ReminderStartsOnlyAfterTenRiyadhDays(int days, bool overdue) => Assert.Equal(overdue, JahezRules.IsOverdue(At(1), new DateOnly(2026, 10, 1).AddDays(days)));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-100, 0)]
    [InlineData(100, 15)]
    public void PercentageBasisCannotCreateANegativeCommission(decimal basis, decimal expected)
    {
        var statement = new JahezEarningsStatement { TotalDeliveryPrice = Math.Max(0, basis), TotalPenalties = Math.Max(0, -basis) };
        Assert.Equal(expected, JahezRules.PercentageCommission(statement));
    }

    private static DateTimeOffset At(int day, int hour = 8) => new(2026, 10, day, hour, 0, 0, TimeSpan.FromHours(3));
    private static T Success<T>(Result<T> result) { Assert.True(result.IsSuccess, result.Error.Description); return result.Value!; }

    internal static byte[] Transactions(string driver, string date, decimal delivery, decimal cash, decimal net, decimal adjustment, bool duplicate = false)
    {
        using var w = new XLWorkbook(); var s = w.AddWorksheet("SDP_Report");
        string[] headers = ["Driver ID", "Date", "Delivery Price", "Cash Amount", "Net Amount", "Driver Adjustment"];
        for (var i = 0; i < headers.Length; i++) s.Cell(1, i + 1).Value = headers[i];
        for (var r = 2; r <= (duplicate ? 3 : 2); r++)
        {
            s.Cell(r, 1).Value = driver; s.Cell(r, 2).Value = date; s.Cell(r, 3).Value = delivery;
            s.Cell(r, 4).Value = cash; s.Cell(r, 5).Value = net; s.Cell(r, 6).Value = adjustment;
        }
        using var stream = new MemoryStream(); w.SaveAs(stream); return stream.ToArray();
    }

    private static byte[] Dispatches(string driver, string date, int count)
    {
        using var w = new XLWorkbook(); var s = w.AddWorksheet("Delivery Insights Report");
        string[] headers = ["Driver ID", "From", "To", "Number Of Dispatches"];
        for (var i = 0; i < headers.Length; i++) s.Cell(1, i + 1).Value = headers[i];
        s.Cell(2, 1).Value = driver; s.Cell(2, 2).Value = date; s.Cell(2, 3).Value = date; s.Cell(2, 4).Value = count;
        using var stream = new MemoryStream(); w.SaveAs(stream); return stream.ToArray();
    }

    private sealed class Fixture(ApplicationDbContext db, MutableUser user, TestPermissions permissions, MutableClock clock, bool sql) : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; } = db;
        public MutableUser User { get; } = user;
        public TestPermissions Permissions { get; } = permissions;
        public MutableClock Clock { get; } = clock;
        public JahezService Service { get; } = new(db, user, permissions, clock);
        public Guid PlatformId { get; private set; }
        public Guid RiderId { get; private set; }
        public Guid OtherRiderId { get; private set; }

        public static async Task<Fixture> Create(bool sql = false)
        {
            var ct = TestContext.Current.CancellationToken;
            var name = $"LogisticsJahezTest_{Guid.NewGuid():N}";
            var user = new MutableUser(); var clock = new MutableClock { UtcNow = At(6, 23).ToUniversalTime() };
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (sql) builder.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={name};Trusted_Connection=True;TrustServerCertificate=True",
                o => o.MigrationsHistoryTable("__ApplicationMigrationsHistory", "migration"));
            else builder.UseInMemoryDatabase(name, o => o.EnableNullChecks(false));
            builder.AddInterceptors(new ApplicationPersistenceInterceptor(user, clock));
            var f = new Fixture(new ApplicationDbContext(builder.Options, clock), user, new TestPermissions(), clock, sql);
            if (sql) { await f.Db.Database.MigrateAsync(ct); Assert.False(f.Db.Database.HasPendingModelChanges()); }
            else await f.Db.Database.EnsureCreatedAsync(ct);
            var platform = await f.Db.ClientPlatforms.SingleOrDefaultAsync(x => x.Code == "JAHEZ", ct);
            if (platform is null) { platform = new ClientPlatform { Code = "JAHEZ", NameAr = "جاهز", NameEn = "Jahez", SupportedPaymentModels = SupportedPlatformPaymentModels.PayPerOrder }; f.Db.Add(platform); }
            f.PlatformId = platform.Id;
            f.RiderId = await f.AddRider("المندوب الفعلي"); f.OtherRiderId = await f.AddRider("المندوب التالي");
            await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
            return f;
        }

        private async Task<Guid> AddRider(string name)
        {
            var e = new Employee { FullNameAr = name, Status = EmployeeStatus.Active, EngagementType = EmployeeRelationshipType.OutsideRider };
            var r = new RiderProfile { EmployeeId = e.Id };
            Db.Add(e); Db.Add(r); await Db.SaveChangesAsync(TestContext.Current.CancellationToken); return r.Id;
        }

        public async Task<PlatformRiderAccount> Account(string external)
        {
            var owner = await AddRider("صاحب الحساب");
            var ownerEmployee = await Db.RiderProfiles.Where(x => x.Id == owner).Select(x => x.EmployeeId).SingleAsync(TestContext.Current.CancellationToken);
            var sponsor = await Db.Sponsors.FirstAsync(TestContext.Current.CancellationToken);
            var city = await Db.OperatingCities.FirstAsync(TestContext.Current.CancellationToken);
            var a = new PlatformRiderAccount { Code = $"J-{external}", ExternalAccountId = external, ClientPlatformId = PlatformId,
                RegisteredEmployeeId = ownerEmployee, SponsorId = sponsor.Id, DashboardSponsorId = sponsor.Id,
                OperatingCityId = city.Id, Status = PlatformRiderAccountStatus.Available };
            Db.Add(a); await Db.SaveChangesAsync(TestContext.Current.CancellationToken); Db.ChangeTracker.Clear(); return a;
        }

        public async ValueTask DisposeAsync()
        {
            if (sql) await Db.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
            await Db.DisposeAsync();
        }
    }

    private sealed class UnusedProtector : IPlatformCredentialProtector
    {
        public ProtectedPlatformCredential Protect(string value) => throw new NotSupportedException();
    }

    private sealed class MutableUser : ICurrentUser
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "jahez-tests";
    }
    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
    private sealed class TestPermissions : IPermissionChecker
    {
        public string? Denied { get; set; }
        public Guid? RequiredScope { get; set; }
        public List<PermissionScope> Scopes { get; } = [];
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default)
        {
            if (scope is not null) { lock (Scopes) Scopes.Add(scope); }
            return Task.FromResult(permissionKey != Denied && (!RequiredScope.HasValue || scope?.TargetId == RequiredScope.Value));
        }
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
