using ClosedXML.Excel;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    private static readonly TreasuryFilter TreasuryOctober = new(new(2026, 10, 1), new(2026, 10, 31));

    private static async Task EnableTreasury(Fixture f)
    {
        var ct = TestContext.Current.CancellationToken;
        foreach (var a in await f.Db.Set<TreasurySubAccount>().Where(x => x.IsPosting && x.IsActive).ToArrayAsync(ct))
            Success(await f.Service.VerifyTreasuryBalanceAsync($"verify-{a.Id}", new(a.Id, 0, "physical-count", "تمت مطابقة الرصيد الصفري"), ct));
        var ready = Success(await f.Service.TreasuryReadinessAsync(ct));
        Assert.Empty(ready.UnverifiedAccounts);
        Success(await f.Service.ActivateTreasuryAsync("activate", new(ready.RowVersion, "تفعيل الخزينة بعد المطابقة"), ct));
    }

    private static async Task<TreasurySubAccount> TreasuryAccount(Fixture f, Guid sponsor, TreasuryLocation location, TreasuryPurpose purpose = TreasuryPurpose.Settlement) =>
        await f.Db.Set<TreasurySubAccount>().AsNoTracking().SingleAsync(x => x.SponsorId == sponsor && x.Location == location && x.IsDefault
            && (x.Purpose == purpose || x.Purpose == TreasuryPurpose.Combined), TestContext.Current.CancellationToken);

    private static async Task FundCustody(Fixture f, Guid sponsor, decimal amount)
    {
        var ct = TestContext.Current.CancellationToken;
        var central = await TreasuryAccount(f, sponsor, TreasuryLocation.Central);
        var custody = await TreasuryAccount(f, sponsor, TreasuryLocation.Supervisor);
        Success(await f.Service.RecordTreasuryCashAsync($"central-{Guid.NewGuid()}", new(central.Id, amount, TreasuryEntryKind.Business, "capital", "تمويل فعلي موثق"), ct));
        Success(await f.Service.TransferTreasuryAsync($"fund-{Guid.NewGuid()}", new(central.Id, custody.Id, amount, "custody-funding", "تمويل عهدة التشغيل"), ct));
    }

    [Fact]
    public async Task TreasuryUsesAccountSponsorAndPreservesFeeOwnerAfterSponsorChange()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("treasury-ownership");
        var original = account.SponsorId!.Value;
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "تسليم الحساب", 0), ct));
        Success(await f.Service.RecordFeeV2Async("fee-one", new(h.Id, 50, JahezMoneyFund.Cashbox, "fee-1", "دفعة رسوم"), ct));
        var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct);
        tracked.SponsorId = original == Sponsor.ExpressGateId ? Sponsor.AlBawabaNextCompanyId : Sponsor.ExpressGateId;
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        Success(await f.Service.RecordFeeV2Async("fee-two", new(h.Id, 150, JahezMoneyFund.Cashbox, "fee-2", "استكمال الرسوم"), ct));
        var movements = Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items;
        Assert.Equal(2, movements.Count);
        Assert.All(movements, m => { Assert.Equal(original, m.SponsorId); Assert.NotNull(m.SponsorIdentifier); Assert.NotNull(m.TreasurySubAccountId); });
        var feeCustody = await TreasuryAccount(f, original, TreasuryLocation.Supervisor, TreasuryPurpose.AccountFee);
        Assert.Equal(200, Success(await f.Service.TreasuryBalancesAsync(ct)).Single(x => x.SubAccountId == feeCustody.Id).Balance);
        Assert.All(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct), p => Assert.Equal(original, p.SponsorId));
        var report = Success(await f.Service.TreasuryReportAsync("movements", TreasuryOctober, ct: ct));
        Assert.All(report.Movements, row => Assert.Equal("المندوب الفعلي", row.DriverNameAr));
    }

    [Fact]
    public async Task TreasuryRejectsSponsorChangedAfterDraftAndRequiresCompanyFunding()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("treasury-funding");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var preview = Success(await f.Service.PreviewOperationAsync(Settle(h.Id, -100), ct));
        Assert.Equal(account.SponsorId, preview.SponsorId);
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, -100), ct));
        var failed = await f.Service.ExecuteOperationAsync("execute", draft.Id, ct);
        Assert.Equal(ErrorType.Conflict, failed.Error.Type);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Empty(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
        await FundCustody(f, account.SponsorId!.Value, 100);
        var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct);
        tracked.SponsorId = account.SponsorId == Sponsor.ExpressGateId ? Sponsor.AlBawabaNextCompanyId : Sponsor.ExpressGateId;
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        Assert.Equal(ErrorType.Conflict, (await f.Service.ExecuteOperationAsync("execute", draft.Id, ct)).Error.Type);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
    }

    [Fact]
    public async Task TreasuryHandoverIncludesFundingFeesAndOnlySnapshotCash()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("treasury-handover"); var sponsor = account.SponsorId!.Value;
        await FundCustody(f, sponsor, 100);
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 50, "initial-fee"), ct));
        var operation = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, -100), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", operation.Id, ct)); // pays 25; 75 custody remains
        var pending = Success(await f.Service.SubmitTreasuryHandoverAsync("submit", new(new(2026, 10, 6), "تسليم نهاية اليوم"), ct));
        Assert.Equal(125, pending.Handover.CashboxAmount);
        Assert.StartsWith("JH-", pending.Handover.DocumentNumber);
        Success(await f.Service.RecordFeeV2Async("later", new(h.Id, 10, JahezMoneyFund.Cashbox, "later", "قبض بعد الإرسال"), ct));
        var counts = pending.Lines.Select(l => new TreasuryCashCount(l.SubAccountId, l.Amount)).ToArray();
        var confirm = new TreasuryHandoverConfirm(counts, "استلام نقدي مطابق", Convert.ToBase64String(pending.Handover.RowVersion));
        Assert.Equal(ErrorType.Forbidden, (await f.Service.ConfirmTreasuryHandoverAsync("confirm", pending.Handover.Id, confirm, ct)).Error.Type);
        f.User.UserId = Guid.NewGuid();
        Assert.Equal(ErrorType.Validation, (await f.Service.ConfirmTreasuryHandoverAsync("bad-count", pending.Handover.Id,
            confirm with { CashCounts = counts.Select((x, i) => x with { Amount = x.Amount + (i == 0 ? 1 : -1) }).ToArray() }, ct)).Error.Type);
        var done = Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm", pending.Handover.Id, confirm, ct));
        Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm", pending.Handover.Id, confirm, ct));
        Assert.Equal(JahezCashboxHandoverStatus.Confirmed, done.Handover.Status);
        var balances = Success(await f.Service.TreasuryBalancesAsync(ct));
        Assert.Equal(10, balances.Where(x => x.IsPosting && x.Location == TreasuryLocation.Supervisor).Sum(x => x.Balance));
        Assert.Equal(125, balances.Where(x => x.IsPosting && x.Location == TreasuryLocation.Central).Sum(x => x.Balance));
        Assert.Equal(2, await f.Db.Set<TreasuryPosting>().Where(x => x.CashboxHandoverId == pending.Handover.Id).Select(x => x.BatchId).Distinct().CountAsync(ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.SubmitCashboxHandoverV2Async("old", new(new(2026, 10, 6), "قديم"), ct)).Error.Type);
        var report = Success(await f.Service.TreasuryReportAsync("handover", TreasuryOctober, pending.Handover.Id, ct));
        Assert.Equal(125, report.Handover!.Handover.CashboxAmount);
    }

    [Fact]
    public async Task TreasuryMigratedMovementCorrectionStaysSynchronizedBeforeActivation()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("legacy-correction-before-activation");
        var session = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(session.Id, -100), ct));
        var executed = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        Assert.Empty(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
        var custody = await TreasuryAccount(f, account.SponsorId!.Value, TreasuryLocation.Supervisor);
        var bank = await TreasuryAccount(f, account.SponsorId.Value, TreasuryLocation.Bank);
        Success(await f.Service.RecordTreasuryCashAsync("historical-custody", new(custody.Id, 100, TreasuryEntryKind.Adjustment,
            "verified-custody-reference", "إثبات تمويل تاريخي ضمن بيانات الاختبار"), ct));
        Success(await f.Service.RecordTreasuryCashAsync("historical-bank", new(bank.Id, 100, TreasuryEntryKind.Opening,
            "verified-bank-reference", "رصيد افتتاحي موثق ضمن بيانات الاختبار"), ct));
        Success(await f.Service.BackfillTreasuryAsync("backfill", "إعادة بناء تاريخ موثق", ct));
        var original = await f.Db.Set<TreasuryPosting>().AsNoTracking().SingleAsync(p => p.MoneyMovementId != null, ct);
        Assert.True(original.Amount < 0);
        Assert.False(Success(await f.Service.TreasuryReadinessAsync(ct)).IsActive);

        Success(await f.Service.UpdateOperationPaymentMethodAsync("correct", executed.Id,
            new(JahezMoneyFund.Bank, "bank-receipt-correction", "تصحيح جهة القبض قبل التفعيل", Convert.ToBase64String(executed.RowVersion)), ct));

        var entries = await f.Db.Set<TreasuryPosting>().AsNoTracking().ToArrayAsync(ct);
        Assert.Equal(5, entries.Length);
        Assert.Single(entries, p => p.ReversesPostingId == original.Id && p.Amount == -original.Amount);
        var balances = Success(await f.Service.TreasuryBalancesAsync(ct));
        Assert.Equal(100, balances.Single(x => x.SubAccountId == original.SubAccountId).Balance);
        Assert.Equal(100 + original.Amount, balances.Single(x => x.SubAccountId == bank.Id).Balance);
        Assert.False(Success(await f.Service.TreasuryReadinessAsync(ct)).IsActive);
    }

    [Fact]
    public async Task TreasuryCorrectionsAppendReversalsAndCannotSpendFeesOrOtherSponsorsCash()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("treasury-correction"); var sponsor = account.SponsorId!.Value;
        var other = sponsor == Sponsor.ExpressGateId ? Sponsor.AlBawabaNextCompanyId : Sponsor.ExpressGateId;
        await FundCustody(f, other, 300);
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 200, "fee"), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, -100), ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.ExecuteOperationAsync("execute", draft.Id, ct)).Error.Type);
        await FundCustody(f, sponsor, 100);
        var paid = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var bank = await TreasuryAccount(f, sponsor, TreasuryLocation.Bank);
        Success(await f.Service.RecordTreasuryCashAsync("bank-open", new(bank.Id, 100, TreasuryEntryKind.Adjustment, "bank-proof", "رصيد البنك المثبت"), ct));
        Success(await f.Service.UpdateOperationPaymentMethodAsync("change", paid.Id,
            new(JahezMoneyFund.Bank, "bank-transfer", "تصحيح الجهة", Convert.ToBase64String(paid.RowVersion)), ct));
        var entries = await f.Db.Set<TreasuryPosting>().Where(x => x.MoneyMovementId != null || x.Kind == TreasuryEntryKind.Reversal).ToArrayAsync(ct);
        Assert.Single(entries, x => x.Kind == TreasuryEntryKind.Reversal);
        Assert.Equal(3, entries.Count(x => x.Kind == TreasuryEntryKind.Business));
        var balances = Success(await f.Service.TreasuryBalancesAsync(ct));
        Assert.Equal(75, balances.Single(x => x.SubAccountId == bank.Id).Balance);
        var wrongFrom = await TreasuryAccount(f, other, TreasuryLocation.Central);
        Assert.Equal(ErrorType.Validation, (await f.Service.TransferTreasuryAsync("cross", new(wrongFrom.Id, bank.Id, 1, "cross", "تحويل"), ct)).Error.Type);
        var report = Success(await f.Service.TreasuryReportAsync("subaccounts", TreasuryOctober, ct: ct));
        Assert.All(report.Accounts, a => Assert.Equal(a.Closing, a.Opening + a.Receipts - a.Payments + a.TransfersIn - a.TransfersOut));
    }

    [Fact]
    public async Task TreasuryFundingReservedByPendingHandoverAndCancellationReleasesIt()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var sponsor = Sponsor.AlBawabaNextCompanyId;
        await FundCustody(f, sponsor, 100);
        var pending = Success(await f.Service.SubmitTreasuryHandoverAsync("submit", new(new(2026, 10, 6), "تسليم التمويل غير المستخدم"), ct));
        Assert.Equal(100, pending.Handover.CashboxAmount);
        var funding = await f.Db.Set<TreasuryPosting>().FirstAsync(x => x.Kind == TreasuryEntryKind.Transfer && x.Amount > 0, ct);
        Assert.Equal(ErrorType.Conflict, (await f.Service.ReverseTreasuryAsync("reverse", new(funding.Id, "reverse", "عكس التمويل"), ct)).Error.Type);
        Success(await f.Service.CancelTreasuryHandoverAsync("cancel", pending.Handover.Id,
            new("إلغاء وإعادة المطابقة", Convert.ToBase64String(pending.Handover.RowVersion)), ct));
        Success(await f.Service.ReverseTreasuryAsync("reverse", new(funding.Id, "reverse", "عكس التمويل"), ct));
        var custody = await TreasuryAccount(f, sponsor, TreasuryLocation.Supervisor);
        Assert.Equal(0, Success(await f.Service.TreasuryBalancesAsync(ct)).Single(x => x.SubAccountId == custody.Id).Balance);
    }

    [Fact]
    public async Task TreasuryPetroEvidenceIsIndependentAndMissingEvidenceIsUnverified()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var report = Success(await f.Service.TreasuryReportAsync("petroapp", TreasuryOctober, ct: ct));
        Assert.All(report.Fuel, row => { Assert.Equal("Unverified", row.Status); Assert.Null(row.Funding); Assert.Null(row.ClosingStatement); });
        var sponsor = Sponsor.AlBawabaNextCompanyId;
        var proof = new PetroEvidenceCreate(sponsor, PetroEvidenceKind.Funding, new(2026, 10, 6), 100, 100, "proof-1", "إثبات التمويل");
        var first = Success(await f.Service.RecordPetroEvidenceAsync("proof", proof, ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.RecordPetroEvidenceAsync("duplicate", proof, ct)).Error.Type);
        Success(await f.Service.RecordPetroEvidenceAsync("replacement", proof with { Amount = 90, Reference = "proof-2", ReplacesId = first.Id }, ct));
        report = Success(await f.Service.TreasuryReportAsync("petroapp", TreasuryOctober, ct: ct));
        Assert.Equal(90, report.Fuel.Single(x => x.SponsorId == sponsor).Funding);
        Assert.Empty(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
    }

    [Fact]
    public async Task TreasuryHistoricalBackfillIsRepeatableAndActivationRequiresActualBalances()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("treasury-history");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 100, "fee"), ct));
        Assert.Empty(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
        Assert.Equal(1, Success(await f.Service.BackfillTreasuryAsync("backfill", "إعادة بناء التاريخ", ct)));
        Assert.Equal(0, Success(await f.Service.BackfillTreasuryAsync("backfill-again", "إعادة بناء التاريخ", ct)));
        Assert.Single(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
        var ready = Success(await f.Service.TreasuryReadinessAsync(ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.ActivateTreasuryAsync("activate", new(ready.RowVersion, "تفعيل"), ct)).Error.Type);
        var custody = await TreasuryAccount(f, account.SponsorId!.Value, TreasuryLocation.Supervisor, TreasuryPurpose.AccountFee);
        Assert.Equal(ErrorType.Conflict, (await f.Service.VerifyTreasuryBalanceAsync("wrong", new(custody.Id, 90, "count", "فرق فعلي"), ct)).Error.Type);
        Success(await f.Service.VerifyTreasuryBalanceAsync("right", new(custody.Id, 100, "count", "مطابقة"), ct));
        Assert.NotEqual(Guid.Empty, h.Id);
    }

    [Fact]
    public async Task TreasuryReadAndWritePermissionsAreCheckedOnReplay()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        f.Permissions.Denied = JahezTreasuryPermissions.Read;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.TreasuryBalancesAsync(ct)).Error.Type);
        f.Permissions.Denied = null;
        var account = await TreasuryAccount(f, Sponsor.AlBawabaNextCompanyId, TreasuryLocation.Central);
        var request = new TreasuryCashCreate(account.Id, 100, TreasuryEntryKind.Opening, "opening-proof", "رصيد افتتاحي مثبت");
        Success(await f.Service.RecordTreasuryCashAsync("opening", request, ct));
        f.Permissions.Denied = JahezTreasuryPermissions.Adjust;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.RecordTreasuryCashAsync("opening", request, ct)).Error.Type);
        Assert.Single(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TreasurySqlConstraintsAndAtomicHandover(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        await FundCustody(f, Sponsor.AlBawabaNextCompanyId, 100);
        var pending = Success(await f.Service.SubmitTreasuryHandoverAsync("submit", new(new(2026, 10, 6), "تسليم"), ct));
        f.User.UserId = Guid.NewGuid();
        var confirmation = new TreasuryHandoverConfirm(pending.Lines.Select(l => new TreasuryCashCount(l.SubAccountId, l.Amount)).ToArray(),
            "استلام", Convert.ToBase64String(pending.Handover.RowVersion));
        Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm", pending.Handover.Id, confirmation, ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.ConfirmTreasuryHandoverAsync("other-confirm", pending.Handover.Id, confirmation, ct)).Error.Type);
        var posting = await f.Db.Set<TreasuryPosting>().FirstAsync(ct); posting.Amount += 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct)); f.Db.ChangeTracker.Clear();
        Assert.Equal(100, (await f.Db.Set<TreasuryPosting>().Where(x => x.CashboxHandoverId == pending.Handover.Id && x.Amount > 0).SumAsync(x => x.Amount, ct)));
        if (sql)
        {
            Assert.False(f.Db.Database.HasPendingModelChanges());
            f.Db.Add(new TreasuryPosting { SubAccountId = pending.Lines[0].SubAccountId, SponsorId = Sponsor.AlBawabaNextCompanyId,
                Amount = 0, Kind = TreasuryEntryKind.Business, Reference = "invalid", Reason = "invalid" });
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync(ct));
        }
    }

    private sealed class TreasuryTestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TreasuryTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public async Task TreasuryExcelAndPdfUseSameReportData()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"TreasuryReportIdentity_{Guid.NewGuid():N}", x => x.EnableNullChecks(false)).Options);
        var accountantId = Guid.NewGuid();
        identity.Users.AddRange(new ApplicationUser { Id = f.User.UserId!.Value, DisplayNameAr = "مشرف جاهز الميداني" },
            new ApplicationUser { Id = accountantId, DisplayNameAr = "محاسب الخزينة" });
        await identity.SaveChangesAsync(ct);
        var renderer = new JahezTreasuryReportRenderer(Options.Create(new PdfGenerationOptions { QuestPdfLicense = "Community", ArabicFontFamily = "Arial" }), new TreasuryTestEnvironment());
        var service = new JahezWorkflowService(f.Db, f.User, f.Permissions, f.Clock, renderer, treasuryIdentity: identity);
        var central = await TreasuryAccount(f, Sponsor.AlBawabaNextCompanyId, TreasuryLocation.Central);
        var custody = await TreasuryAccount(f, Sponsor.AlBawabaNextCompanyId, TreasuryLocation.Supervisor);
        Success(await service.RecordTreasuryCashAsync("central-report", new(central.Id, 123.45m, TreasuryEntryKind.Business, "capital", "تمويل مثبت"), ct));
        Success(await service.TransferTreasuryAsync("fund-report", new(central.Id, custody.Id, 123.45m, "custody-funding", "تمويل التشغيل"), ct));
        var json = Success(await service.TreasuryReportAsync("movements", TreasuryOctober, ct: ct));
        var excel = Success(await service.ExportTreasuryReportAsync("movements", "xlsx", TreasuryOctober, ct: ct));
        using (var workbook = new XLWorkbook(new MemoryStream(excel.Content)))
        {
            Assert.Equal(json.Accounts[0].Closing, workbook.Worksheet("الأرصدة").Cell(2, 9).GetValue<decimal>());
            Assert.Equal(json.TotalMovements + 1, workbook.Worksheet("الحركات").LastRowUsed()!.RowNumber());
            Assert.All(workbook.Worksheet("الحركات").RowsUsed().Skip(1), row => Assert.Equal("مشرف جاهز الميداني", row.Cell(9).GetString()));
            Assert.DoesNotContain(workbook.Worksheets.SelectMany(s => s.CellsUsed()), c => Guid.TryParse(c.GetString(), out _));
        }
        var pdf = Success(await service.ExportTreasuryReportAsync("movements", "pdf", TreasuryOctober, ct: ct));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Content, 0, 4));
        if (Environment.GetEnvironmentVariable("LOGISTICS_TREASURY_RENDER_QA") == "1")
        {
            var folder = Path.GetFullPath("../../../../../outputs/jahez-treasury-qa", AppContext.BaseDirectory);
            Directory.CreateDirectory(folder);
            await File.WriteAllBytesAsync(Path.Combine(folder, "treasury-movements.pdf"), pdf.Content, ct);
            await File.WriteAllBytesAsync(Path.Combine(folder, "treasury-movements.xlsx"), excel.Content, ct);
        }
        var pending = Success(await service.SubmitTreasuryHandoverAsync("submit-report", new(new(2026, 10, 6), "تسليم لاختبار التقرير"), ct));
        Assert.Equal("مشرف جاهز الميداني", pending.Handover.RequestedByNameAr);
        f.User.UserId = accountantId;
        var confirmed = Success(await service.ConfirmTreasuryHandoverAsync("confirm-report", pending.Handover.Id,
            new(pending.Lines.Select(l => new TreasuryCashCount(l.SubAccountId, l.Amount)).ToArray(), "مطابقة واستلام", Convert.ToBase64String(pending.Handover.RowVersion)), ct));
        Assert.Equal("محاسب الخزينة", confirmed.Handover.AccountantNameAr);
        (await identity.Users.SingleAsync(x => x.Id == accountantId, ct)).DisplayNameAr = "اسم محدث لاحقاً";
        await identity.SaveChangesAsync(ct);
        Assert.Equal("محاسب الخزينة", Success(await service.TreasuryHandoverAsync(pending.Handover.Id, ct)).Handover.AccountantNameAr);
        var handoverReport = Success(await service.TreasuryReportAsync("handover", TreasuryOctober, pending.Handover.Id, ct));
        using (var workbook = new XLWorkbook(new MemoryStream(renderer.Render(handoverReport, "xlsx").Content)))
        {
            Assert.Equal("مشرف جاهز الميداني", workbook.Worksheet("محضر التسليم").Cell(4, 2).GetString());
            Assert.Equal("محاسب الخزينة", workbook.Worksheet("محضر التسليم").Cell(5, 2).GetString());
        }
        // Old snapshots and supporting references can contain GUIDs. Neither export format may print them.
        handoverReport.Handover!.Handover.RequestedByNameAr = Guid.NewGuid().ToString();
        handoverReport.Handover.Handover.AccountantNameAr = null;
        handoverReport.Handover.Handover.DocumentNumber = null;
        handoverReport.Handover.Handover.ConfirmationReason = $"legacy:{Guid.NewGuid()}";
        using (var workbook = new XLWorkbook(new MemoryStream(renderer.Render(handoverReport, "xlsx").Content)))
        {
            Assert.Equal("اسم المستخدم غير متاح", workbook.Worksheet("محضر التسليم").Cell(4, 2).GetString());
            Assert.Equal("اسم المستخدم غير متاح", workbook.Worksheet("محضر التسليم").Cell(5, 2).GetString());
            Assert.DoesNotContain(workbook.Worksheets.SelectMany(s => s.CellsUsed()), c =>
                System.Text.RegularExpressions.Regex.IsMatch(c.GetString(), @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
        foreach (var type in new[] { "subaccounts", "trading", "petroapp", "handover" })
        {
            var rendered = Success(await service.ExportTreasuryReportAsync(type, "pdf", TreasuryOctober,
                type == "handover" ? pending.Handover.Id : null, ct));
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(rendered.Content, 0, 4));
            var spreadsheet = Success(await service.ExportTreasuryReportAsync(type, "xlsx", TreasuryOctober,
                type == "handover" ? pending.Handover.Id : null, ct));
            using var workbook = new XLWorkbook(new MemoryStream(spreadsheet.Content));
            Assert.True(workbook.Worksheets.Count > 0);
            if (Environment.GetEnvironmentVariable("LOGISTICS_TREASURY_RENDER_QA") == "1")
            {
                var folder = Path.GetFullPath("../../../../../outputs/jahez-treasury-qa", AppContext.BaseDirectory);
                await File.WriteAllBytesAsync(Path.Combine(folder, $"treasury-{type}.pdf"), rendered.Content, ct);
            }
        }
    }
}
