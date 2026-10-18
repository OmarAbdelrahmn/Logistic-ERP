using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Ninja;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    private static readonly string[] AccountUpdateExpectedProblems = ["CityMismatch", "IqamaMismatch", "OwnerNotWorking", "NaqlBlocked", "SheetSuspended"];
    [Fact]
    public async Task AccountSheetSavesSeparateStatusAndProblemsWithoutChangingMainAccounts()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var owner = await f.Db.Employees.SingleAsync(e => e.Id == f.OwnerRider.EmployeeId, ct);
        owner.IqamaNo = "2000000001"; owner.Status = EmployeeStatus.Terminated; await f.Db.SaveChangesAsync(ct);
        var beforeVersion = f.Account.RowVersion.ToArray(); var beforeTime = f.Account.UpdatedAtUtc;
        var upload = Value(await f.Service.UploadAccountSheetAsync("check.xlsx", UpdateSheet(
            ["272578", "Owner", "JEDDAH", "2000000002", "NAQL_BLOCKED", "True"],
            ["999999", "Missing owner", "Riyadh", "2000000003", "ACTIVE", "False"]), "  Review October  ", ct));
        Assert.Equal("Review October", upload.Note); Assert.Equal(2, upload.Summary.TotalRows);
        Assert.Equal(1, upload.Summary.FoundRows); Assert.Equal(1, upload.Summary.NotFoundRows);
        Assert.Equal(2, upload.Summary.RowsWithProblems); Assert.Equal(1, upload.Summary.CityMismatchRows);
        Assert.Equal(1, upload.Summary.IqamaProblemRows); Assert.Equal(1, upload.Summary.NaqlBlockedRows); Assert.Equal(1, upload.Summary.SuspendedRows);
        Assert.Equal(64, upload.ContentHash.Length); Assert.Equal(f.User.UserId, upload.CreatedByUserId);
        var rows = Value(await f.Service.GetAccountSheetRowsAsync(upload.Id, null, null, false, 1, 50, ct));
        Assert.Equal(2, rows.TotalCount); Assert.Equal("Found", rows.Items[0].MatchStatus);
        Assert.Equal("NAQL_BLOCKED", rows.Items[0].SheetStatus); Assert.Equal(f.Account.Id, rows.Items[0].System!.AccountId);
        Assert.Equal("Available", rows.Items[0].System!.Status);
        Assert.Equal("2000000001", rows.Items[0].System!.OwnerIqamaNo); Assert.Equal("2000000002", rows.Items[0].SheetIqamaNo);
        Assert.All(AccountUpdateExpectedProblems,
            code => Assert.Contains(rows.Items[0].Problems, p => p.Code == code));
        Assert.Equal("NotFound", rows.Items[1].MatchStatus); Assert.Null(rows.Items[1].System);
        Assert.Equal("ACTIVE", rows.Items[1].SheetStatus); Assert.Contains(rows.Items[1].Problems, p => p.Code == "AccountNotFound");
        var missing = Value(await f.Service.GetAccountSheetRowsAsync(upload.Id, "NotFound", "ACTIVE", true, 1, 50, ct));
        Assert.Equal(1, missing.TotalCount); Assert.Equal("999999", Assert.Single(missing.Items).CaptainId);
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal("Available", account.Status); Assert.Equal("NAQL_BLOCKED", account.LatestSheetStatus);
        Assert.True(account.LatestSheetSuspended); Assert.Equal(upload.Id, account.LatestSheetUploadId);
        Assert.Equal(beforeVersion, f.Account.RowVersion); Assert.Equal(beforeTime, f.Account.UpdatedAtUtc);
        Assert.Equal(OperatingCity.RiyadhId, f.Account.OperatingCityId); Assert.Equal("2000000001", owner.IqamaNo);
        Assert.Empty(f.Db.RiderClientAssignments); Assert.Empty(f.Db.PlatformAccountCredentialVersions); Assert.Empty(f.Db.NinjaAccountDetails);
        Assert.Empty(f.Db.NinjaDailyPerformance); Assert.Empty(f.Db.NinjaDailyDeductions); Assert.Empty(f.Db.Notifications);
        Assert.Single(f.Db.NinjaAccountUpdateUploads); Assert.Equal(2, await f.Db.NinjaAccountUpdateRows.CountAsync(ct));
    }

    [Fact]
    public async Task NotesCanBeEditedClearedAndConcurrencyCheckedWhileSnapshotsRemainUnchanged()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var upload = Value(await f.Service.UploadAccountSheetAsync("check.xlsx", UpdateSheet(
            ["272578", "Owner", "Riyadh", "2000000001", "ACTIVE", "False"]), "First note", ct));
        var firstRow = await f.Db.NinjaAccountUpdateRows.SingleAsync(ct); var snapshot = firstRow.SystemSnapshotJson;
        var changed = Value(await f.Service.UpdateAccountSheetNoteAsync(upload.Id, new("  Follow up  ", upload.RowVersion), ct));
        Assert.Equal("Follow up", changed.Note); Assert.NotNull(changed.UpdatedAtUtc);
        Assert.True((await f.Service.UpdateAccountSheetNoteAsync(upload.Id, new("No", "not-a-token"), ct)).IsFailure);
        Assert.True((await f.Service.UpdateAccountSheetNoteAsync(upload.Id, new(new string('a', 4001), changed.RowVersion), ct)).IsFailure);
        var cleared = Value(await f.Service.UpdateAccountSheetNoteAsync(upload.Id, new(null, changed.RowVersion), ct));
        Assert.Null(cleared.Note); Assert.Equal(snapshot, firstRow.SystemSnapshotJson);
        var header = await f.Db.NinjaAccountUpdateUploads.SingleAsync(ct); header.RowVersion = [2]; await f.Db.SaveChangesAsync(ct);
        Assert.Equal("ninja.conflict", (await f.Service.UpdateAccountSheetNoteAsync(upload.Id, new("Stale", changed.RowVersion), ct)).Error.Code);
        firstRow.SheetStatus = "CHANGED";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task OldUploadsKeepTheirSnapshotsAndNewUploadsOnlyChangeLatestSheetReadFields()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var first = Value(await f.Service.UploadAccountSheetAsync("first.xlsx", UpdateSheet(
            ["272578", "Owner", "Riyadh", "2000000001", "ACTIVE", "False"]), null, ct));
        var original = Assert.Single(Value(await f.Service.GetAccountSheetRowsAsync(first.Id, null, null, false, 1, 50, ct)).Items);
        f.Account.OperatingCityId = OperatingCity.JeddahId; await f.Db.SaveChangesAsync(ct); f.Clock.Now = f.Clock.Now.AddHours(1);
        var second = Value(await f.Service.UploadAccountSheetAsync("second.xlsx", UpdateSheet(
            ["272578", "Owner", "JEDDAH", "2000000001", "NAQL_BLOCKED", "True"]), "Second", ct));
        var historical = Assert.Single(Value(await f.Service.GetAccountSheetRowsAsync(first.Id, null, null, false, 1, 50, ct)).Items);
        Assert.Equal(original.System, historical.System); Assert.Equal("ACTIVE", historical.SheetStatus);
        var listing = Value(await f.Service.GetAccountSheetUploadsAsync(1, 1, ct));
        Assert.Equal(2, listing.TotalCount); Assert.Equal(second.Id, Assert.Single(listing.Items).Id);
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal(second.Id, account.LatestSheetUploadId); Assert.Equal("NAQL_BLOCKED", account.LatestSheetStatus);
        Assert.Equal("Available", account.Status);
    }

    [Fact]
    public async Task OwnerMissingBadIqamaAndAmbiguousCaptainAreReportedWithoutFallbackMatching()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        f.Account.RegisteredEmployeeId = null;
        f.Db.PlatformRiderAccounts.Add(new() { ClientPlatformId = f.Account.ClientPlatformId, ExternalAccountId = " ٢٧٢٥٧٨ ",
            Code = "NINJA-DUP", OperatingCityId = OperatingCity.RiyadhId });
        await f.Db.SaveChangesAsync(ct);
        var upload = Value(await f.Service.UploadAccountSheetAsync("ambiguous.xlsx", UpdateSheet(
            ["272578", "Owner", "Riyadh", "invalid", "NAQL_BLOCKED", "False"]), null, ct));
        Assert.Equal(1, upload.Summary.AmbiguousRows); Assert.Equal(0, upload.Summary.FoundRows);
        var row = Assert.Single(Value(await f.Service.GetAccountSheetRowsAsync(upload.Id, "Ambiguous", null, false, 1, 50, ct)).Items);
        Assert.Null(row.System); Assert.Contains(row.Problems, p => p.Code == "SheetIqamaInvalid");
        Assert.Contains(row.Problems, p => p.Code == "AccountAmbiguous"); Assert.Contains(row.Problems, p => p.Code == "NaqlBlocked");
        Assert.Null(Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).LatestSheetStatus);
    }

    [Fact]
    public async Task InvalidOrUnauthorizedUploadsDoNotStageDataAndReadNotePermissionsAreSeparate()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var valid = UpdateSheet(["272578", "Owner", "Riyadh", "2000000001", "ACTIVE", "False"]);
        Assert.Equal("ninja.account_sheet_invalid", (await f.Service.UploadAccountSheetAsync("invalid.xlsx", UpdateSheet(
            ["272578", "Owner", "Riyadh", "2000000001", "ACTIVE", "False"],
            ["272578", "Owner", "Riyadh", "2000000001", "ACTIVE", "False"]), null, ct)).Error.Code);
        Assert.True((await f.Service.UploadAccountSheetAsync("check.csv", valid, null, ct)).IsFailure);
        Assert.True((await f.Service.UploadAccountSheetAsync("check.xlsx", valid, new string('a', 4001), ct)).IsFailure);
        f.Checker.Denied = PermissionKeys.Ninja.AccountsUpdate;
        Assert.Equal("ninja.forbidden", (await f.Service.UploadAccountSheetAsync("check.xlsx", valid, null, ct)).Error.Code);
        await f.Db.SaveChangesAsync(ct); Assert.Empty(f.Db.NinjaAccountUpdateUploads); Assert.Empty(f.Db.NinjaAccountUpdateRows);
        f.Checker.Denied = null;
        var upload = Value(await f.Service.UploadAccountSheetAsync("check.xlsx", valid, null, ct));
        f.Checker.Denied = PermissionKeys.Ninja.Read;
        Assert.True((await f.Service.GetAccountSheetUploadsAsync(1, 50, ct)).IsFailure);
        Assert.True((await f.Service.GetAccountSheetUploadAsync(upload.Id, ct)).IsFailure);
        Assert.True((await f.Service.GetAccountSheetRowsAsync(upload.Id, null, null, false, 1, 50, ct)).IsFailure);
        Value(await f.Service.UpdateAccountSheetNoteAsync(upload.Id, new("Update-only operator", upload.RowVersion), ct));
        f.Checker.Denied = PermissionKeys.Ninja.AccountsUpdate;
        Value(await f.Service.GetAccountSheetUploadAsync(upload.Id, ct));
        Assert.True((await f.Service.UpdateAccountSheetNoteAsync(upload.Id, new("Denied", upload.RowVersion), ct)).IsFailure);
        f.Checker.Denied = null;
        Assert.Equal("ninja.not_found", (await f.Service.GetAccountSheetUploadAsync(Guid.NewGuid(), ct)).Error.Code);
        Assert.True((await f.Service.GetAccountSheetUploadsAsync(0, 50, ct)).IsFailure);
        Assert.True((await f.Service.GetAccountSheetRowsAsync(upload.Id, "Bad", null, false, 1, 50, ct)).IsFailure);
        Assert.True((await f.Service.GetAccountSheetRowsAsync(upload.Id, null, null, false, 1, 101, ct)).IsFailure);
        var other = new ClientPlatform { Code = "OTHER", NameAr = "Other" }; f.Db.ClientPlatforms.Add(other);
        var header = await f.Db.NinjaAccountUpdateUploads.SingleAsync(ct); header.ClientPlatformId = other.Id; await f.Db.SaveChangesAsync(ct);
        Assert.Equal("ninja.forbidden", (await f.Service.GetAccountSheetUploadAsync(upload.Id, ct)).Error.Code);
    }

    [Fact]
    public void AccountUpdateParserSupportsNamedIdentifiersAndRejectsFormulaMalformedAndDuplicateHeaders()
    {
        var ct = TestContext.Current.CancellationToken;
        var content = UpdateSheet(["٠٠١٢٣٤", "Owner", "Riyadh", "٢٠٠٠٠٠٠٠٠١", "UNKNOWN_STATUS", "False"]);
        var parsed = NinjaAccountUpdateSheetParser.Parse(content, ct);
        Assert.Empty(parsed.Issues); Assert.Equal("001234", Assert.Single(parsed.Rows).CaptainId);
        Assert.Equal("2000000001", parsed.Rows[0].IqamaNo); Assert.False(parsed.Rows[0].Suspended);
        using var book = new XLWorkbook(new MemoryStream(content));
        book.Worksheet(1).Cell(2, 5).FormulaA1 = "1";
        using var stream = new MemoryStream(); book.SaveAs(stream);
        Assert.NotEmpty(NinjaAccountUpdateSheetParser.Parse(stream.ToArray(), ct).Issues);
        book.Worksheet(1).Cell(2, 5).Value = "ACTIVE"; book.Worksheet(1).Cell(1, 7).Value = "المعرف";
        using var duplicate = new MemoryStream(); book.SaveAs(duplicate);
        Assert.NotEmpty(NinjaAccountUpdateSheetParser.Parse(duplicate.ToArray(), ct).Issues);
        Assert.NotEmpty(NinjaAccountUpdateSheetParser.Parse([1, 2, 3], ct).Issues);
    }

    [Fact]
    public void SuppliedAccountUpdateExportParsesItsBlankIdHeaderStatusesFlagsAndFootersReadOnly()
    {
        var path = Environment.GetEnvironmentVariable("NINJA_ACCOUNT_UPDATES_SAMPLE");
        Assert.SkipUnless(path is not null, "Set NINJA_ACCOUNT_UPDATES_SAMPLE for the provided export.");
        using var file = new FileStream(path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var stream = new MemoryStream(); file.CopyTo(stream);
        var result = NinjaAccountUpdateSheetParser.Parse(stream.ToArray(), TestContext.Current.CancellationToken);
        Assert.Empty(result.Issues); Assert.Equal(163, result.Rows.Count); Assert.Equal(2, result.IgnoredFooterRows);
        Assert.Equal(84, result.Rows.Count(r => r.Status == "NAQL_BLOCKED")); Assert.Equal(79, result.Rows.Count(r => r.Status == "ACTIVE"));
        Assert.Equal(59, result.Rows.Count(r => r.Suspended == true)); Assert.Equal("295975", result.Rows[0].CaptainId);
    }

    private static byte[] UpdateSheet(params string[][] rows)
    {
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Accounts");
        string[] headers = ["المعرف", "الاسم", "المدينة", "رقم الهوية", "الحالة", "موقوف"];
        for (var c = 0; c < headers.Length; c++) sheet.Cell(1, c + 1).Value = headers[c];
        for (var r = 0; r < rows.Length; r++) for (var c = 0; c < rows[r].Length; c++) sheet.Cell(r + 2, c + 1).Value = rows[r][c];
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
}
