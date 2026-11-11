using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Files;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

// CR-005 phase 2: server-derived close, invoice-fed month close, mandatory screenshot, zero-or-approval hand-over.
public sealed partial class HungerFinanceTests
{
    private static readonly byte[] Pixel = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
    private static PrivateFileUpload PixelImage() => new(new MemoryStream(Pixel), "wallet.png", "image/png", Pixel.Length);
    private static string TempRoot() => Path.Combine(Path.GetTempPath(), "HungerCr005", Guid.NewGuid().ToString("N"));

    private static HungerSettlementRequest Invoice(Fixture f, string key, decimal riderBalance, decimal control, Guid? account = null) => new(
        f.Platform.Id, null, "Invoice", new(2026, 10, 1), new(2026, 10, 1), new(2026, 10, 31), new(2026, 11, 1), null, 115, "RiderLVL",
        [new("rider-1", "RiderLVL", account ?? f.Account.Id, null, 100, 15, null, "invoice line") { RiderBalance = riderBalance }],
        null, key, "invoice evidence", RiderBalanceControlTotal: control);

    private static async Task<Guid> ApprovedHandoverException(Fixture f, decimal amount, string key)
    {
        var ct = TestContext.Current.CancellationToken;
        var created = Value(await f.Service.RequestExceptionAsync(new(f.Platform.Id, "NonZeroHandover", f.Account.Id, null, amount, "receiver accepts", key), ct));
        var row = await f.Db.Set<HungerExceptionApproval>().SingleAsync(e => e.Id == created.ExceptionId, ct);
        row.RequestedByUserId = Guid.NewGuid(); await f.Db.SaveChangesAsync(ct);
        Value(await f.Service.DecideExceptionAsync(created.ExceptionId, true, new(), ct));
        return created.ExceptionId;
    }

    [Fact]
    public async Task TypedCloseAndAdjustmentsAreIgnoredInFavourOfSettlementDocuments()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.RecordSettlementAsync(Close(f, new(2026, 10, 1), "close"), ct));
        var preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f, 999, 777) with { ClosingEvidence = null, AdjustmentEvidence = null }, ct));
        Assert.Equal(20, preview.ClosingBalance); Assert.Equal(80, preview.CurrentPeriodBalance);
        Assert.Equal(0, preview.ProvenAdjustments); Assert.Equal("Manual", preview.ClosingSource); Assert.False(preview.RequiresException);
    }

    [Fact]
    public async Task MatchedInvoiceFeedsMonthCloseAndZeroesAccountsItDoesNotList()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var other = new PlatformRiderAccount { ClientPlatformId = f.Platform.Id, Code = "HS2", ExternalAccountId = "2" };
        f.Db.AddRange(other, new RiderClientAssignment { PlatformRiderAccountId = other.Id, RiderProfileId = f.Rider.Id,
            EffectiveFrom = new(2026, 10, 1), EffectiveTo = new(2026, 10, 20), Status = RiderAssignmentStatus.Ended, RowVersion = [1] });
        var mine = await f.Db.RiderClientAssignments.SingleAsync(ct); mine.EffectiveFrom = new(2026, 10, 1);
        await f.Db.SaveChangesAsync(ct);

        Assert.True((await f.Service.RecordSettlementAsync(Invoice(f, "negative", -1, -1), ct)).IsFailure);
        Assert.True((await f.Service.RecordSettlementAsync(Invoice(f, "no-control", 30, 30) with { RiderBalanceControlTotal = null }, ct)).IsFailure);
        Assert.True((await f.Service.RecordSettlementAsync(Invoice(f, "partial", 30, 30) with { ChargeThrough = new(2026, 10, 15) }, ct)).IsFailure);

        // A control mismatch keeps the invoice out of the wallet entirely.
        var mismatch = Value(await f.Service.RecordSettlementAsync(Invoice(f, "mismatch", 30, 31), ct));
        Assert.Equal("NeedsReview", mismatch.Status); Assert.Equal(0, mismatch.GeneratedCloses);
        var unknown = Value(await f.Service.ReviewWalletAsync(other.Id, Delivery(f), ct));
        Assert.False(unknown.CanConfirm); Assert.Equal("Unknown", unknown.ClosingSource);

        var invoice = Value(await f.Service.RecordSettlementAsync(Invoice(f, "invoice", 30, 30), ct));
        Assert.Equal("Matched", invoice.Status); Assert.Equal(1, invoice.GeneratedCloses); Assert.Null(invoice.CloseConflictAccountIds);
        var replay = Value(await f.Service.RecordSettlementAsync(Invoice(f, "invoice", 30, 30), ct));
        Assert.Equal(invoice.DocumentId, replay.DocumentId); Assert.Equal(0, replay.GeneratedCloses);
        var close = await f.Db.Set<HungerSettlementDocument>().SingleAsync(d => d.Kind == "MonthClose", ct);
        Assert.Equal(-30, close.WalletBalance); Assert.Equal(invoice.DocumentId, close.InvoiceDocumentId);
        Assert.Equal(f.Account.Id, close.PlatformRiderAccountId); Assert.Equal(new DateOnly(2026, 10, 31), close.ChargeThrough);

        var listed = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f), ct));
        Assert.Equal(-30, listed.ClosingBalance); Assert.Equal(130, listed.CurrentPeriodBalance); Assert.Equal("Invoice", listed.ClosingSource);
        var absent = Value(await f.Service.ReviewWalletAsync(other.Id, Delivery(f), ct));
        Assert.Equal(0, absent.ClosingBalance); Assert.True(absent.CanConfirm); Assert.Equal("InvoiceAbsent", absent.ClosingSource);
    }

    [Fact]
    public async Task InvoiceNeverOverwritesAnExistingMonthCloseAndReportsTheConflict()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.RecordSettlementAsync(Close(f, new(2026, 10, 1), "manual-close"), ct));
        var invoice = Value(await f.Service.RecordSettlementAsync(Invoice(f, "invoice", 30, 30), ct));
        Assert.Equal("Matched", invoice.Status); Assert.Equal(0, invoice.GeneratedCloses);
        Assert.Equal([f.Account.Id], invoice.CloseConflictAccountIds);
        Assert.Equal([f.Account.Id], Value(await f.Service.RecordSettlementAsync(Invoice(f, "invoice", 30, 30), ct)).CloseConflictAccountIds);
        Assert.Equal(20, (await f.Db.Set<HungerSettlementDocument>().SingleAsync(d => d.Kind == "MonthClose", ct)).WalletBalance);
        var preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f), ct));
        Assert.Equal(20, preview.ClosingBalance); Assert.Equal("Manual", preview.ClosingSource);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-40)]
    public async Task HandoverNeedsScreenshotAndZeroBalanceOrMatchingApprovedException(decimal balance)
    {
        var root = TempRoot();
        try
        {
            await using var f = await Fixture.Create(files: new PrivateFileStorage(new TestHostEnvironment(root)), cr005: true);
            var ct = TestContext.Current.CancellationToken;
            f.Db.RemoveRange(await f.Db.RiderClientAssignments.ToArrayAsync(ct)); await f.Db.SaveChangesAsync(ct);
            var request = Delivery(f) with { MobileBalance = balance };
            Assert.True((await f.Service.ReviewWalletAsync(f.Account.Id, request, ct)).IsFailure);
            Assert.True((await f.Service.ReviewWalletWithImageAsync(f.Account.Id, request, null, ct)).IsFailure);
            Assert.Empty(await f.Db.Set<HungerWalletReview>().ToArrayAsync(ct));

            var review = Value(await f.Service.ReviewWalletWithImageAsync(f.Account.Id, request, PixelImage(), ct));
            Assert.Equal(balance != 0, review.RequiresException); Assert.NotNull(review.WalletEvidence);
            if (balance != 0)
            {
                var blocked = await f.Service.ConfirmWalletAsync(review.ReviewId, ct);
                Assert.True(blocked.IsFailure); Assert.Equal("hunger.conflict", blocked.Error.Code);
                // An approval for a different amount does not cover this hand-over.
                var wrong = await ApprovedHandoverException(f, balance + 10, "wrong");
                review = Value(await f.Service.ReviewWalletWithImageAsync(f.Account.Id, request with { ExceptionApprovalId = wrong }, PixelImage(), ct));
                Assert.True((await f.Service.ConfirmWalletAsync(review.ReviewId, ct)).IsFailure);
                Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
                var approval = await ApprovedHandoverException(f, balance, "right");
                review = Value(await f.Service.ReviewWalletWithImageAsync(f.Account.Id, request with { ExceptionApprovalId = approval }, PixelImage(), ct));
            }
            var confirmed = Value(await f.Service.ConfirmWalletAsync(review.ReviewId, ct));
            Assert.Equal(-balance, confirmed.RiderBalanceChange);
            f.Db.ChangeTracker.Clear();
            var entry = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == confirmed.EventId, ct);
            Assert.Equal(review.WalletEvidence, entry.EvidenceReference); Assert.Equal("PendingSeal", entry.ApprovalStatus);
            var approvals = await f.Db.Set<HungerExceptionApproval>().ToArrayAsync(ct);
            if (balance == 0) { Assert.Empty(approvals); Assert.Null(entry.ExceptionApprovalId); }
            else
            {
                var used = Assert.Single(approvals, a => a.Status == "Consumed");
                Assert.Equal(entry.Id, used.ConsumedByEventId); Assert.Equal(used.Id, entry.ExceptionApprovalId);
                Assert.Single(approvals, a => a.Status == "Approved");
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(100, false)]
    [InlineData(-100, true)]
    public async Task SwapSharesOneScreenshotAndNeedsExceptionOnlyWhenTheReceiverInheritsABalance(decimal balance, bool needsException)
    {
        var root = TempRoot();
        try
        {
            await using var f = await Fixture.Create(intercept: true, relational: true, files: new PrivateFileStorage(new TestHostEnvironment(root)), cr005: true);
            var ct = TestContext.Current.CancellationToken;
            var employee = new Employee { FullNameAr = "New rider", Status = EmployeeStatus.Active };
            var rider = new RiderProfile { EmployeeId = employee.Id }; f.Db.AddRange(employee, rider); await f.Db.SaveChangesAsync(ct);
            Assert.True((await f.Service.ReviewRiderSwapAsync(f.Account.Id, Swap(f, rider.Id, balance), ct)).IsFailure);
            Assert.True((await f.Service.ReviewRiderSwapWithImageAsync(f.Account.Id, Swap(f, rider.Id, balance), null, ct)).IsFailure);

            var review = Value(await f.Service.ReviewRiderSwapWithImageAsync(f.Account.Id, Swap(f, rider.Id, balance), PixelImage(), ct));
            Assert.False(review.Return.RequiresException); Assert.Equal(needsException, review.Handover.RequiresException);
            Assert.StartsWith($"/private/hunger/wallets/{f.Account.Id:D}/", review.Return.WalletEvidence);
            Assert.Equal(review.Return.WalletEvidence, review.Handover.WalletEvidence);
            if (needsException)
            {
                Assert.True((await f.Service.ConfirmRiderSwapAsync(review.SwapId, ct)).IsFailure);
                f.Db.ChangeTracker.Clear();
                Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
                Assert.Null((await f.Db.RiderClientAssignments.SingleAsync(ct)).EffectiveTo);
                var approval = await ApprovedHandoverException(f, balance, "swap");
                f.Db.ChangeTracker.Clear();
                review = Value(await f.Service.ReviewRiderSwapWithImageAsync(f.Account.Id,
                    Swap(f, rider.Id, balance) with { HandoverExceptionApprovalId = approval }, PixelImage(), ct));
            }
            Value(await f.Service.ConfirmRiderSwapAsync(review.SwapId, ct));
            f.Db.ChangeTracker.Clear();
            var events = await f.Db.Set<HungerFinancialEvent>().Where(e => e.Kind == "WalletReturn" || e.Kind == "WalletDelivery").ToArrayAsync(ct);
            Assert.Equal(2, events.Length);
            Assert.All(events, e => Assert.Equal(review.Return.WalletEvidence, e.EvidenceReference));
            Assert.Equal(needsException, events.Single(e => e.Kind == "WalletDelivery").ExceptionApprovalId.HasValue);
            Assert.Null(events.Single(e => e.Kind == "WalletReturn").ExceptionApprovalId);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
