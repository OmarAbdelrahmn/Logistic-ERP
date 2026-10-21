using System.Reflection;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Fact]
    public async Task V2HostedReadOnlyQueries()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_HOSTED_READS") == "1", "Opt-in read-only hosted verification.");
        var ct = TestContext.Current.CancellationToken;
        var workspace = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        using var config = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(workspace, "src/LogisticsERP.Api/appsettings.json"), ct));
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__LogisticsDatabase")
            ?? config.RootElement.GetProperty("ConnectionStrings").GetProperty("LogisticsDatabase").GetString()!;
        var target = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        Assert.Equal("db67927.public.databaseasp.net", target.DataSource); Assert.Equal("db67927", target.InitialCatalog);
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__ApplicationMigrationsHistory", "migration")).Options);
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), TimeProvider.System);
        var accounts = Success(await service.AccountsV2Async(1, 100, ct));
        var stats = Success(await service.StatisticsV2Async(ct));
        Assert.Equal(accounts.TotalCount, stats.TotalAccounts);
        Success(await service.OperationsV2Async(1, 50, ct)); Success(await service.RequestsV2Async(1, 50, ct));
        Success(await service.TransferredDebtsAsync(1, 50, ct)); Success(await service.MovementsV2Async(1, 50, ct: ct));
        Success(await service.AdministrativeDebtsAsync(1, 50, ct)); Success(await service.ImportBatchesAsync(1, 50, ct));
        var funds = Success(await service.FundsV2Async(ct));
        if (accounts.Items.Count > 0) Success(await service.AccountDetailsV2Async(accounts.Items[0].AccountId, 1, 50, ct));
        Assert.False(db.ChangeTracker.HasChanges()); Assert.False(db.Database.HasPendingModelChanges());
        var report = new { VerifiedAtUtc = DateTimeOffset.UtcNow, Server = target.DataSource, Database = target.InitialCatalog,
            Accounts = accounts.TotalCount, ReturnedAccounts = accounts.Items.Count, StatisticsAccounts = stats.TotalAccounts,
            stats.UnassignedAccounts, stats.BlockedAccounts, stats.MissingPasswordAccounts, Funds = funds,
            ReadOnlyQueriesSucceeded = true, ApplicationModelMatchesSnapshot = true };
        await File.WriteAllTextAsync(Path.Combine(workspace, "outputs/jahez-v2-hosted-read-verification.json"), System.Text.Json.JsonSerializer.Serialize(report), ct);
    }
    [Theory]
    [InlineData(100, 50, 150, JahezMoneyDirection.Receipt)]
    [InlineData(-100, 50, -50, JahezMoneyDirection.Disbursement)]
    [InlineData(-50, 100, 50, JahezMoneyDirection.Receipt)]
    [InlineData(-50, 50, 0, JahezMoneyDirection.None)]
    public void V2PaperFormula(decimal net, decimal commission, decimal expected, JahezMoneyDirection direction)
    {
        Assert.Equal(expected, JahezSettlementRules.Remaining(net, commission));
        Assert.Equal(direction, JahezSettlementRules.Direction(expected));
        Assert.Equal(1.01m, JahezSettlementRules.Remaining(1.005m, 0));
    }

    [Theory]
    [InlineData(JahezMoneyFund.Cashbox, JahezMoneyDirection.Receipt, JahezMoneyPurpose.Settlement, true)]
    [InlineData(JahezMoneyFund.Cashbox, JahezMoneyDirection.Disbursement, JahezMoneyPurpose.Settlement, true)]
    [InlineData(JahezMoneyFund.Bank, JahezMoneyDirection.Receipt, JahezMoneyPurpose.Settlement, false)]
    [InlineData(JahezMoneyFund.Bank, JahezMoneyDirection.Disbursement, JahezMoneyPurpose.Settlement, true)]
    [InlineData(JahezMoneyFund.PetroApp, JahezMoneyDirection.Receipt, JahezMoneyPurpose.Settlement, true)]
    [InlineData(JahezMoneyFund.PetroApp, JahezMoneyDirection.Receipt, JahezMoneyPurpose.Fuel, true)]
    [InlineData(JahezMoneyFund.PetroApp, JahezMoneyDirection.Receipt, JahezMoneyPurpose.AccountFee, false)]
    [InlineData(JahezMoneyFund.PetroApp, JahezMoneyDirection.Receipt, JahezMoneyPurpose.TransferredDebtRepayment, false)]
    [InlineData(JahezMoneyFund.PetroApp, JahezMoneyDirection.Disbursement, JahezMoneyPurpose.Settlement, false)]
    [InlineData(JahezMoneyFund.PetroApp, JahezMoneyDirection.Disbursement, JahezMoneyPurpose.Fuel, false)]
    public void V2MoneyRouting(JahezMoneyFund fund, JahezMoneyDirection direction, JahezMoneyPurpose purpose, bool allowed) =>
        Assert.Equal(allowed, JahezSettlementRules.Allows(fund, direction, purpose));

    private static JahezOperationCreate Settle(Guid h, decimal net, JahezOperationMode mode = JahezOperationMode.Settle,
        JahezMoneyFund? fund = JahezMoneyFund.Cashbox, bool transfer = false) =>
        new(h, mode, new(2026, 10, 6), At(6, 12), net, "تصفية من داشبورد جاهز", fund,
            JahezMoneyPurpose.Settlement, transfer, fund.HasValue ? "actual-payment-1" : null);

    [Theory]
    [InlineData(JahezOperationMode.Settle, false)]
    [InlineData(JahezOperationMode.SettleAndWithdraw, false)]
    [InlineData(JahezOperationMode.Settle, true)]
    [InlineData(JahezOperationMode.SettleAndWithdraw, true)]
    public async Task V2PetroAppSettlementReceiptIsOfferedAndExecuted(JahezOperationMode mode, bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("v2-petroapp-settlement");
        var handover = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var input = Settle(handover.Id, -5.01m, mode, JahezMoneyFund.PetroApp);
        var preview = Success(await f.Service.PreviewOperationAsync(input, ct));
        Assert.Equal(69.99m, preview.Remaining);
        Assert.Equal(JahezMoneyDirection.Receipt, preview.Direction);
        Assert.Contains(JahezMoneyFund.PetroApp, preview.AllowedFunds);

        var draft = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        var executed = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        Assert.Equal(JahezOperationStatus.Executed, executed.Status);
        Assert.Equal(JahezOperationPaymentStatus.Paid, executed.PaymentStatus);
        Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Equal(draft.Id, movement.OperationId);
        Assert.Equal(JahezMoneyFund.PetroApp, movement.Fund);
        Assert.Equal(JahezMoneyPurpose.Settlement, movement.Purpose);
        Assert.Equal(JahezMoneyDirection.Receipt, movement.Direction);
        Assert.Equal(69.99m, movement.Amount);
        var funds = Success(await f.Service.FundsV2Async(ct));
        Assert.Equal(69.99m, funds.Single(x => x.Fund == JahezMoneyFund.PetroApp).Balance);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2SignedSettlementIgnoresAdministrativeDebtAndCannotRepeat(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("v2-signed");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var input = Settle(h.Id, -125);
        var before = Success(await f.Service.PreviewOperationAsync(input, ct));
        Assert.Equal(75, before.Commission);
        Assert.Equal(-50, before.Remaining);
        f.Db.Add(new JahezLedgerEntry { HandoverId = h.Id, Bucket = JahezLedgerBucket.PlatformDebt, Kind = JahezLedgerKind.Adjustment, Amount = 999999, SourceId = Guid.NewGuid(), Reason = "للمتابعة" }); await f.Db.SaveChangesAsync(ct);
        var after = Success(await f.Service.PreviewOperationAsync(input, ct));
        Assert.Equal(before.Net, after.Net); Assert.Equal(before.Commission, after.Commission); Assert.Equal(before.Remaining, after.Remaining);
        var draft = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        var executed = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        Assert.Equal(JahezOperationStatus.Executed, executed.Status);
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Equal(JahezMoneyDirection.Disbursement, movement.Direction);
        Assert.Equal(50, movement.Amount);
        Assert.Null((await f.Db.Set<JahezAccountSession>().SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
        Assert.Equal(999999, await f.Db.Set<JahezLedgerEntry>().Where(x => x.HandoverId == h.Id && x.Bucket == JahezLedgerBucket.PlatformDebt).SumAsync(x => x.Amount, ct));
        Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.True((await f.Service.ExecuteOperationAsync("second", draft.Id, ct)).IsFailure);
        Assert.True((await f.Service.CreateOperationAsync("overlap", input, ct)).IsFailure);
        var funds = Success(await f.Service.FundsV2Async(ct));
        Assert.Equal(-50, funds.Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
    }

    [Fact]
    public async Task V2ReviewDefersEveryEffectRejectsThenApprovesModifiedValues()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("v2-review");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var o = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100, JahezOperationMode.SettleAndWithdraw), ct));
        var r = Success(await f.Service.CreateWorkflowRequestAsync("request", new(h.Id, JahezApprovalKind.Adjustment, "تصحيح",
            OperationId: o.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedNet: -125), ct));
        Assert.True((await f.Service.ExecuteOperationAsync("blocked", o.Id, ct)).IsFailure);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Empty(await f.Db.JahezTransferredDebts.ToArrayAsync(ct));
        Assert.Null((await f.Db.Set<JahezAccountSession>().SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
        Assert.True((await f.Service.DecideWorkflowRequestAsync("self", r.Request.Id, new(true, "ذاتي"), ct)).IsFailure);
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("reject", r.Request.Id, new(false, "رفض"), ct));
        var ready = await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct);
        Assert.Equal(JahezOperationStatus.Ready, ready.Status); Assert.Equal(100, ready.Net);
        f.User.UserId = Guid.NewGuid();
        var second = Success(await f.Service.CreateWorkflowRequestAsync("request2", new(h.Id, JahezApprovalKind.Adjustment, "تصحيح",
            OperationId: o.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedNet: -125), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", second.Request.Id, new(true, "اعتماد"), ct));
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        var done = Success(await f.Service.ExecuteOperationAsync("execute", o.Id, ct));
        Assert.Equal(-50, done.Remaining);
        Assert.NotNull((await f.Db.Set<JahezAccountSession>().SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
        Assert.Equal(PlatformRiderAccountStatus.Available, (await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == a.Id, ct)).Status);
        Assert.True((await f.Service.CreateWorkflowRequestAsync("executed-edit", new(h.Id, JahezApprovalKind.Adjustment, "تغيير",
            OperationId: o.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement), ct)).IsFailure);
    }

    [Fact]
    public async Task V2WithdrawDefersFinanceAndAllowsReassignmentAndOldRiderSettlement()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("v2-defer");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var withdraw = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 0, JahezOperationMode.Withdraw, null), ct));
        Success(await f.Service.ExecuteOperationAsync("withdraw", withdraw.Id, ct));
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Null((await f.Db.Set<JahezAccountSession>().SingleAsync(x => x.Id == h.Id, ct)).CommissionPostedThrough);
        f.Clock.UtcNow = At(10, 23).ToUniversalTime();
        var next = Success(await f.Service.HandoverAsync("next", new(a.Id, f.OtherRiderId, At(7), "استلام آخر", 0), ct));
        var input = Settle(h.Id, -75, fund: null);
        var preview = Success(await f.Service.PreviewOperationAsync(input, ct));
        Assert.Equal(75, preview.Commission); Assert.Equal(0, preview.Remaining);
        var settle = Success(await f.Service.CreateOperationAsync("settle", input, ct));
        Success(await f.Service.ExecuteOperationAsync("execute", settle.Id, ct));
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Null((await f.Db.Set<JahezAccountSession>().SingleAsync(x => x.Id == next.Id, ct)).EndedAtUtc);
    }

    [Fact]
    public async Task V2TransferNeedsApprovalWithoutBlockingOperationsAndFullRepaymentIsIdempotent()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("v2-transfer");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var o = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100, JahezOperationMode.SettleAndWithdraw, null, true), ct));
        Assert.True((await f.Service.ExecuteOperationAsync("direct", o.Id, ct)).IsFailure);
        var request = Success(await f.Service.CreateWorkflowRequestAsync("request", new(h.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: o.Id), ct));
        Assert.Empty(await f.Db.JahezTransferredDebts.ToArrayAsync(ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", request.Request.Id, new(true, "اعتماد"), ct));
        var debt = Assert.Single(await f.Db.JahezTransferredDebts.ToArrayAsync(ct)); Assert.Equal(175, debt.Amount);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        var ninja = new ClientPlatform { Code = "NINJA", NameAr = "نينجا", NameEn = "Ninja" };
        f.Db.Add(ninja); await f.Db.SaveChangesAsync(ct);
        Assert.False(await RiderOperationalBlock.IsBlocked(f.Db, f.RiderId, ninja.Id, ct));
        Assert.False(await RiderOperationalBlock.IsBlocked(f.Db, f.OtherRiderId, ninja.Id, ct));
        var keeta = new ClientPlatform { Code = "KEETA", NameAr = "كيتا", NameEn = "Keeta" };
        var otherAccount = await f.Account("v2-keeta");
        var storedAccount = await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == otherAccount.Id, ct);
        storedAccount.ClientPlatformId = keeta.Id;
        var contract = new ClientContract { ClientPlatformId = keeta.Id, Code = "KEETA", DisplayNameAr = "كيتا",
            DisplayNameEn = "Keeta", Status = ClientContractStatus.Active };
        f.Db.Add(keeta); f.Db.Add(contract); await f.Db.SaveChangesAsync(ct);
        var simple = new SimplePlatformService(f.Db, f.User, f.Clock, new UnusedProtector());
        var assigned = Success(await simple.AssignAccountAsync(otherAccount.Id, new(f.RiderId, new(2026, 10, 6), "تشغيل", false, null), ct));
        Assert.False(await RiderOperationalBlock.IsAccountBlocked(f.Db, otherAccount.Id, ct));
        Assert.Empty(await RiderOperationalBlock.BlockedAssignmentIds(f.Db,
            await f.Db.RiderClientAssignments.Select(x => x.Id).ToArrayAsync(ct), ct));
        var money = new JahezMoneyRecord(JahezMoneyFund.Cashbox, "receipt-175", "سداد كامل");
        f.Permissions.Denied = JahezWorkflowPermissions.Receive;
        Assert.True((await f.Service.RepayTransferredDebtAsync("denied", debt.Id, money, ct)).IsFailure);
        Assert.False(await RiderOperationalBlock.IsBlocked(f.Db, f.RiderId, ninja.Id, ct));
        f.Permissions.Denied = null;
        Success(await f.Service.RepayTransferredDebtAsync("repay", debt.Id, money, ct));
        Assert.False(await RiderOperationalBlock.IsBlocked(f.Db, f.RiderId, ninja.Id, ct));
        Assert.Equal(175, Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct)).Amount);
        Success(await f.Service.RepayTransferredDebtAsync("repay", debt.Id, money, ct));
        Assert.True((await f.Service.RepayTransferredDebtAsync("again", debt.Id, money, ct)).IsFailure);
    }

    [Fact]
    public async Task V2FeeWaiverAndPartialFeeSwitchKeepFinancialDomainsSeparate()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var old = await f.Account("v2-switch-old"); var next = await f.Account("v2-switch-new");
        var h = Success(await f.Service.HandoverAsync("start", new(old.Id, f.RiderId, At(1), "استلام", 0), ct));
        var waiver = Success(await f.Service.CreateWorkflowRequestAsync("waiver", new(h.Id, JahezApprovalKind.FeeException, "إعفاء", WaiverAmount: 100), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("waive", waiver.Request.Id, new(true, "اعتماد"), ct));
        Success(await f.Service.RecordFeeV2Async("fee", new(h.Id, 100, JahezMoneyFund.Cashbox, "fee-100", "قبض الباقي"), ct));
        var preview = Success(await f.Service.PreviewOperationAsync(Settle(h.Id, 0), ct));
        Assert.Equal(75, preview.Remaining); // Neither fee nor fee receipt is part of settlement.
        var change = Success(await f.Service.CreateWorkflowRequestAsync("switch", new(h.Id, JahezApprovalKind.FreeSwitch,
            "تبديل برسوم 60", TargetAccountId: next.Id, WaiverAmount: 140, EffectiveAtUtc: At(6, 12)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("switch-approve", change.Request.Id, new(true, "اعتماد"), ct));
        var newHandover = await f.Db.Set<JahezAccountSession>().SingleAsync(x => x.PlatformRiderAccountId == next.Id, ct);
        Assert.Equal(140, (await f.Db.Set<JahezAccountFee>().SingleAsync(x => x.HandoverId == newHandover.Id, ct)).WaivedAmount);
        Assert.Empty(await f.Db.JahezTransferredDebts.ToArrayAsync(ct));
        Assert.Empty(await f.Db.JahezTransferredDebts.ToArrayAsync(ct));
        Assert.Equal(100, Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct)).Amount);
        var deferred = Assert.Single(await f.Db.JahezOperations.ToArrayAsync(ct));
        Assert.Equal(JahezOperationMode.Withdraw, deferred.Mode); Assert.Equal(JahezOperationPaymentStatus.Deferred, deferred.PaymentStatus);
    }

    [Fact]
    public async Task V2AdministrativeAdjustmentDoesNotConsumeCommissionOrCreateMoneyOrBlock()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("v2-info");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var request = Success(await f.Service.CreateWorkflowRequestAsync("adjust", new(h.Id, JahezApprovalKind.Adjustment,
            "معلومة إدارية", AdjustmentTarget: JahezAdjustmentTarget.AdministrativeDebt, DebtAdjustment: 12345), ct));
        f.User.UserId = Guid.NewGuid(); f.Permissions.Denied = JahezWorkflowPermissions.DebtAdjust;
        Assert.True((await f.Service.DecideWorkflowRequestAsync("denied", request.Request.Id, new(true, "اعتماد"), ct)).IsFailure);
        f.Permissions.Denied = null;
        Success(await f.Service.DecideWorkflowRequestAsync("approve", request.Request.Id, new(true, "اعتماد", ApprovedDebtAdjustment: 12000), ct));
        Assert.Equal(12000, await f.Db.Set<JahezLedgerEntry>().Where(x => x.Bucket == JahezLedgerBucket.PlatformDebt).SumAsync(x => x.Amount, ct));
        Assert.Equal(75, Success(await f.Service.PreviewOperationAsync(Settle(h.Id, 0), ct)).Remaining);
        Assert.Empty(await f.Db.JahezTransferredDebts.ToArrayAsync(ct)); Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Null((await f.Db.Set<JahezAccountSession>().SingleAsync(x => x.Id == h.Id, ct)).CommissionPostedThrough);
    }

    [Fact]
    public async Task V2PercentagePolicyRequiresEarningsAndDraftCanRefreshAfterDataArrives()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("v2-percentage");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var r = Success(await f.Service.CreateWorkflowRequestAsync("policy", new(h.Id, JahezApprovalKind.PercentageCommission,
            "عمولة نسبة", FromDate: new(2026, 10, 2), ToDate: new(2026, 10, 6)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", r.Request.Id, new(true, "اعتماد"), ct));
        var input = Settle(h.Id, 100);
        Assert.False(Success(await f.Service.PreviewOperationAsync(input, ct)).CommissionComplete);
        var draft = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        Assert.True((await f.Service.ExecuteOperationAsync("incomplete", draft.Id, ct)).IsFailure);
        Success(await f.Service.RecordEarningsAsync("earnings", new(h.Id, new(2026, 10, 2), new(2026, 10, 6),
            1000, 0, 0, 0, 0, 0, 0, 0, 0, "بيانات الفترة"), ct));
        var refreshed = Success(await f.Service.UpdateOperationAsync("refresh", draft.Id, new(input, Convert.ToBase64String(draft.RowVersion)), ct));
        Assert.Equal(150, refreshed.Commission); Assert.Equal(250, refreshed.Remaining);
        Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        Assert.Equal(250, Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct)).Amount);
    }

    [Fact]
    public async Task V2YesterdayOrdersUseCommittedReplacementReportsAndKeepZeroDifferentFromMissing()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("v2-orders"); var empty = await f.Account("v2-no-data");
        Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "استلام", 0), ct));
        var first = Success(await f.Service.UploadAsync("upload", new(JahezImportKind.DailyDispatches,
            [new("orders.xlsx", Dispatches(a.ExternalAccountId, "2026-10-05", 8))]), ct));
        Success(await f.Service.CommitImportAsync("commit", first.BatchId, new(), ct));
        var before = Success(await f.Service.AccountsV2Async(1, 100, ct));
        Assert.Equal(8, before.Items.Single(x => x.AccountId == a.Id).YesterdayOrders);
        Assert.Equal(new DateOnly(2026, 10, 5), before.Items.Single(x => x.AccountId == a.Id).LastOrderDate);
        var replacement = Success(await f.Service.UploadAsync("replace", new(JahezImportKind.DailyDispatches,
            [new("corrected.xlsx", Dispatches(a.ExternalAccountId, "2026-10-05", 0))], first.BatchId, "تصحيح"), ct));
        Success(await f.Service.CommitImportAsync("commit2", replacement.BatchId, new(), ct));
        var after = Success(await f.Service.AccountsV2Async(1, 100, ct));
        Assert.Equal(0, after.Items.Single(x => x.AccountId == a.Id).YesterdayOrders);
        Assert.Null(after.Items.Single(x => x.AccountId == a.Id).LastOrderDate);
        Assert.Null(after.Items.Single(x => x.AccountId == empty.Id).YesterdayOrders);
        Assert.Equal(0, Success(await f.Service.StatisticsV2Async(ct)).YesterdayOrders);
    }

    [Fact]
    public async Task V2AccountsIncludeUnassignedAndDistinguishOwnerFromActualRider()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var free = await f.Account("v2-free"); var taken = await f.Account("v2-taken");
        Success(await f.Service.HandoverAsync("start", new(taken.Id, f.RiderId, At(1), "استلام", 0), ct));
        var accounts = Success(await f.Service.AccountsV2Async(1, 100, ct));
        Assert.Equal(2, accounts.TotalCount);
        var available = accounts.Items.Single(x => x.AccountId == free.Id);
        Assert.False(available.IsAssigned); Assert.Null(available.ActualRider); Assert.Null(available.YesterdayOrders);
        Assert.Null(available.LastOrderDate); Assert.False(available.HasPassword);
        var assigned = accounts.Items.Single(x => x.AccountId == taken.Id);
        Assert.True(assigned.IsAssigned); Assert.Equal(f.RiderId, assigned.ActualRider!.RiderProfileId);
        Assert.NotEqual(assigned.Owner!.EmployeeId, assigned.ActualRider.EmployeeId);
        var stats = Success(await f.Service.StatisticsV2Async(ct));
        Assert.Equal(2, stats.TotalAccounts); Assert.Equal(1, stats.UnassignedAccounts); Assert.Equal(2, stats.MissingPasswordAccounts);
        Assert.Null(stats.YesterdayOrders);
        Success(await f.Service.AccountDetailsV2Async(taken.Id, 1, 50, ct));
    }

    [Fact]
    public void V2SqlQueriesTranslateAndModelMatchesMigration()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedV2;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var query = (IQueryable<JahezAccountRow>)typeof(JahezWorkflowService).GetMethod("WorkflowAccounts", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, new object?[] { null, 0, 50 })!;
        Assert.Contains("SELECT", query.ToQueryString());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains("MoneyMovements", db.Database.GenerateCreateScript());
    }
}
