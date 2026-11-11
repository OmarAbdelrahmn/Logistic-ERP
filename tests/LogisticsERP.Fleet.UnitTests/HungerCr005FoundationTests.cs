using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Files;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

// CR-005 phase 1: accountant seal, replacement of rejected rows, evidence files and exception approvals.
public sealed partial class HungerFinanceTests
{
    private static async Task<decimal> Custody(Fixture f) =>
        Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, TestContext.Current.CancellationToken)).CustodyBalance;

    [Fact]
    public async Task SealApprovesOnceWithAuditAndLeavesPostingsUntouched()
    {
        await using var f = await Fixture.Create(intercept: true); var ct = TestContext.Current.CancellationToken;
        var funding = Value(await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "fund", f.Bank.Id), ct));
        Assert.Equal("NotRequired", funding.ApprovalStatus);
        var advance = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 100, "advance"), ct));
        Assert.Equal("PendingSeal", advance.ApprovalStatus);
        var queue = Value(await f.Service.SealQueueAsync(f.Platform.Id, null, 1, 50, ct));
        Assert.Equal(advance.EventId, Assert.Single(queue.Items).Id);
        var postings = await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().CountAsync(ct);

        var accounts = new Dictionary<string, string> { ["debit"] = "112501", ["credit"] = "112306" };
        var first = Value(await f.Service.SealEventAsync(advance.EventId, new(accounts, "reviewed"), ct));
        var replay = Value(await f.Service.SealEventAsync(advance.EventId, new(accounts, "reviewed"), ct));
        Assert.Equal("Sealed", first.ApprovalStatus); Assert.Equal(first, replay);
        f.Db.ChangeTracker.Clear();
        var row = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == advance.EventId, ct);
        Assert.NotNull(row.ApprovedAtUtc); Assert.NotNull(row.ApprovedByUserId); Assert.Contains("112501", row.SealAccountsJson);
        Assert.Single(await f.Db.Set<HungerFinancialEvent>().Where(e => e.Kind == "Seal" && e.RelatedEventId == advance.EventId).ToArrayAsync(ct));
        Assert.Equal(postings, await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().CountAsync(ct));
        Assert.Empty(Value(await f.Service.SealQueueAsync(f.Platform.Id, null, 1, 50, ct)).Items);
        // A sealed row can no longer be rejected, and a row that never needed a seal cannot be sealed.
        Assert.True((await f.Service.RejectEventAsync(advance.EventId, new("late"), ct)).IsFailure);
        Assert.True((await f.Service.SealEventAsync(funding.EventId, new(), ct)).IsFailure);
    }

    [Fact]
    public async Task SealRequiresItsPermissionAndADifferentUserThanTheMaker()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "fund", f.Bank.Id), ct));
        var advance = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 100, "advance"), ct));
        var me = Value(await f.Service.RequestExceptionAsync(new(f.Platform.Id, "NonZeroHandover", f.Account.Id, null, 10, "who am I", "probe"), ct)).RequestedByUserId;

        // The legacy manage grant must not imply the checker role.
        f.Permissions.Allowed = [PermissionKeys.Hunger.FinanceManage, PermissionKeys.Hunger.TopUpExecute, PermissionKeys.Hunger.FinanceRead];
        Assert.Equal("hunger.forbidden", (await f.Service.SealEventAsync(advance.EventId, new(), ct)).Error.Code);
        Assert.Equal("hunger.forbidden", (await f.Service.RejectEventAsync(advance.EventId, new("no"), ct)).Error.Code);
        f.Permissions.Allowed = null;

        var row = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == advance.EventId, ct);
        row.CreatedByUserId = me; await f.Db.SaveChangesAsync(ct);
        var own = await f.Service.SealEventAsync(advance.EventId, new(), ct);
        Assert.True(own.IsFailure); Assert.Equal("hunger.conflict", own.Error.Code);
        f.Db.ChangeTracker.Clear();
        Assert.Equal("PendingSeal", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == advance.EventId, ct)).ApprovalStatus);
    }

    [Fact]
    public async Task RejectedTopUpKeepsItsEffectUntilReplacementIsSealed()
    {
        await using var f = await Fixture.Create(intercept: true); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "fund", f.Bank.Id), ct));
        var original = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 100, "advance"), ct));
        // Only a rejected row can be replaced.
        Assert.True((await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 150, "early") with { SupersedesEventId = original.EventId }, ct)).IsFailure);

        Assert.True((await f.Service.RejectEventAsync(original.EventId, new(" "), ct)).IsFailure);
        var rejected = Value(await f.Service.RejectEventAsync(original.EventId, new("wrong amount"), ct));
        Assert.Equal("Rejected", rejected.ApprovalStatus);
        Assert.Equal(rejected, Value(await f.Service.RejectEventAsync(original.EventId, new("wrong amount"), ct)));
        Assert.Equal(900, await Custody(f));

        // Custody counts what the rejected row will give back, and nothing more.
        Assert.True((await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 1001, "too-much") with { SupersedesEventId = original.EventId }, ct)).IsFailure);
        var replacement = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 150, "replacement") with { SupersedesEventId = original.EventId }, ct));
        Assert.Equal("AwaitingSeal", replacement.Status); Assert.Equal("PendingSeal", replacement.ApprovalStatus);
        Assert.Equal(900, await Custody(f));
        Assert.True((await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 120, "second") with { SupersedesEventId = original.EventId }, ct)).IsFailure);
        // A replacement cannot itself be the target of another replacement.
        Assert.True((await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 120, "chain") with { SupersedesEventId = replacement.EventId }, ct)).IsFailure);

        var sealedRow = Value(await f.Service.SealEventAsync(replacement.EventId, new(), ct));
        Assert.Equal("Posted", sealedRow.Status); Assert.Equal("Sealed", sealedRow.ApprovalStatus);
        Assert.Equal(sealedRow, Value(await f.Service.SealEventAsync(replacement.EventId, new(), ct)));
        f.Db.ChangeTracker.Clear();
        Assert.Equal(850, await Custody(f));
        var old = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == original.EventId, ct);
        Assert.Equal("Cancelled", old.ApprovalStatus); Assert.Equal("Cancelled", old.Status);
        var postings = await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct);
        Assert.Equal(0, postings.Sum(p => p.Amount));
        Assert.Equal(2, postings.Count(p => p.Kind == TreasuryEntryKind.Reversal && p.ReversesPostingId != null));
        Assert.Equal(2, postings.Count(p => p.SourceEventId == replacement.EventId));
    }

    [Fact]
    public async Task RejectingAReplacementLeavesTheOriginalEffectAndAllowsAnotherReplacement()
    {
        await using var f = await Fixture.Create(intercept: true); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "fund", f.Bank.Id), ct));
        var original = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 100, "advance"), ct));
        Value(await f.Service.RejectEventAsync(original.EventId, new("wrong amount"), ct));
        var bad = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 150, "bad") with { SupersedesEventId = original.EventId }, ct));
        Value(await f.Service.RejectEventAsync(bad.EventId, new("still wrong"), ct));
        Assert.Equal(900, await Custody(f));
        var good = Value(await f.Service.PostMoneyAsync(f.Request("TopUpCollected", 80, "good") with { SupersedesEventId = original.EventId }, ct));
        Value(await f.Service.SealEventAsync(good.EventId, new(), ct));
        Assert.Equal(920, await Custody(f));
        Assert.Equal(0, await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().SumAsync(p => p.Amount, ct));
    }

    [Fact]
    public async Task ApprovalStatusCannotBeChangedWithoutItsAuditEvent()
    {
        await using var f = await Fixture.Create(intercept: true); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "fund", f.Bank.Id), ct));
        var advance = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 100, "advance"), ct));
        f.Db.ChangeTracker.Clear();
        var row = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == advance.EventId, ct);
        row.ApprovalStatus = "Sealed"; row.ApprovedAtUtc = DateTimeOffset.UtcNow; row.ApprovedByUserId = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task ExceptionApprovalIsIdempotentAndNeedsASecondPerson()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var request = new HungerExceptionRequest(f.Platform.Id, "NonZeroHandover", f.Account.Id, null, -75.5m, "receiver accepts the balance", "ex-1");
        var created = Value(await f.Service.RequestExceptionAsync(request, ct));
        Assert.Equal("Requested", created.Status);
        Assert.Equal(created, Value(await f.Service.RequestExceptionAsync(request, ct)));
        Assert.True((await f.Service.RequestExceptionAsync(request with { Amount = 10 }, ct)).IsFailure);
        Assert.True((await f.Service.RequestExceptionAsync(request with { IdempotencyKey = "ex-2", Amount = 0 }, ct)).IsFailure);
        Assert.True((await f.Service.RequestExceptionAsync(request with { IdempotencyKey = "ex-3", AccountId = null }, ct)).IsFailure);
        Assert.True((await f.Service.RequestExceptionAsync(request with { IdempotencyKey = "ex-4", Kind = "CustodyIdentityMismatch" }, ct)).IsFailure);

        Assert.True((await f.Service.DecideExceptionAsync(created.ExceptionId, true, new(), ct)).IsFailure);
        var row = await f.Db.Set<HungerExceptionApproval>().SingleAsync(e => e.Id == created.ExceptionId, ct);
        row.RequestedByUserId = Guid.NewGuid(); await f.Db.SaveChangesAsync(ct);
        Assert.True((await f.Service.DecideExceptionAsync(created.ExceptionId, false, new(), ct)).IsFailure);
        f.Permissions.Allowed = [PermissionKeys.Hunger.FinanceManage, PermissionKeys.Hunger.ExceptionRequest];
        Assert.Equal("hunger.forbidden", (await f.Service.DecideExceptionAsync(created.ExceptionId, true, new(), ct)).Error.Code);
        f.Permissions.Allowed = null;
        var approved = Value(await f.Service.DecideExceptionAsync(created.ExceptionId, true, new("ok"), ct));
        Assert.Equal("Approved", approved.Status); Assert.NotNull(approved.DecidedByUserId);
        Assert.Equal(approved, Value(await f.Service.DecideExceptionAsync(created.ExceptionId, true, new("ok"), ct)));
        Assert.True((await f.Service.DecideExceptionAsync(created.ExceptionId, false, new("changed my mind"), ct)).IsFailure);
        var page = Value(await f.Service.ExceptionsAsync(f.Platform.Id, "Approved", 1, 50, ct));
        Assert.Equal(created.ExceptionId, Assert.Single(page.Items).ExceptionId);
        Assert.Empty(Value(await f.Service.ExceptionsAsync(f.Platform.Id, "Requested", 1, 50, ct)).Items);
    }

    [Fact]
    public async Task EvidenceUploadIssuesServerLinkAndRejectsInvalidInput()
    {
        var root = Path.Combine(Path.GetTempPath(), "HungerEvidence", Guid.NewGuid().ToString("N"));
        try
        {
            await using var f = await Fixture.Create(files: new PrivateFileStorage(new TestHostEnvironment(root)));
            var ct = TestContext.Current.CancellationToken;
            var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
            var upload = new HungerEvidenceUpload(f.Platform.Id, f.Account.Id, "WalletBefore", new(2026, 11, 1));
            PrivateFileUpload Png() => new(new MemoryStream(bytes), "wallet.png", "image/png", bytes.Length);

            var saved = Value(await f.Service.UploadEvidenceAsync(upload, Png(), ct));
            Assert.StartsWith($"/private/hunger/evidence/{f.Account.Id:D}/", saved.Link);
            Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(root, "wwwroot", saved.Link.TrimStart('/')), ct));
            var row = await f.Db.Set<HungerEvidenceFile>().SingleAsync(ct);
            Assert.Equal(saved.EvidenceFileId, row.Id); Assert.Null(row.FinancialEventId); Assert.Equal("WalletBefore", row.Purpose);

            Assert.True((await f.Service.UploadEvidenceAsync(upload, null, ct)).IsFailure);
            Assert.True((await f.Service.UploadEvidenceAsync(upload with { Purpose = "Selfie" }, Png(), ct)).IsFailure);
            Assert.True((await f.Service.UploadEvidenceAsync(upload with { ObservedOn = new(2026, 11, 5) }, Png(), ct)).IsFailure);
            Assert.True((await f.Service.UploadEvidenceAsync(upload with { AccountId = Guid.NewGuid() }, Png(), ct)).IsFailure);
            var text = "not an image"u8.ToArray();
            var spoofed = await f.Service.UploadEvidenceAsync(upload, new(new MemoryStream(text), "wallet.png", "image/png", text.Length), ct);
            Assert.Equal("files.invalid_file", spoofed.Error.Code);
            f.Permissions.Allowed = [PermissionKeys.Hunger.FinanceRead];
            Assert.Equal("hunger.forbidden", (await f.Service.UploadEvidenceAsync(upload, Png(), ct)).Error.Code);
            f.Db.ChangeTracker.Clear();
            Assert.Single(await f.Db.Set<HungerEvidenceFile>().ToArrayAsync(ct));
            Assert.Single(Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
