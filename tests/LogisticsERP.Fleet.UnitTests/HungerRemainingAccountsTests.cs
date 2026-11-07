using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LogisticsERP.Infrastructure.Hunger;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class HungerQualityServiceTests
{
    private static async Task<byte[]> SeedFile(Fixture f, string external = "new-account", string owner = "1234567890", string phone = "0501234567", bool second = false)
    {
        var file = Value(await f.Service.AccountTemplateAsync(TestContext.Current.CancellationToken));
        using var book = new XLWorkbook(new MemoryStream(file.Content)); var sheet = book.Worksheet("Sheet1");
        sheet.Cell(2, 1).Value = external; sheet.Cell(2, 2).Value = owner; sheet.Cell(2, 3).Value = owner;
        sheet.Cell(2, 4).Value = phone; sheet.Cell(2, 5).Value = "Test"; sheet.Cell(2, 6).Value = "Registry";
        sheet.Cell(2, 7).Value = "Monthly"; sheet.Cell(2, 8).Value = "يعمل"; sheet.Cell(2, 9).Value = "Car";
        sheet.Cell(2, 13).Value = new DateTime(2026, 11, 1); sheet.Cell(2, 14).Value = 0; sheet.Cell(2, 15).Value = 0;
        if (second) { sheet.Row(2).CopyTo(sheet.Row(3)); sheet.Cell(3, 1).Value = "other-account"; }
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }

    private static HungerAccountImportConfirmation Resolve(int row, string action) => new([new(row, action)]);

    [Fact]
    public async Task HungerAccountImportCreatesWithoutPreexistingAccountAndReplaysDecisions()
    {
        await using var f = await Fixture.Create(); await Assign(f); var ct = TestContext.Current.CancellationToken;
        var file = await SeedFile(f);
        var preview = Value(await f.Service.PreviewAccountsAsync("accounts.xlsx", file, ct));
        Assert.Equal("New", Assert.Single(preview.Rows).Status);
        Assert.True((await f.Service.ConfirmAccountsAsync(preview.PreviewId, ct)).IsFailure);
        var result = Value(await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Create"), ct));
        Assert.Equal(1, result.ImportedAccounts); Assert.Equal(1, result.PendingAssignments);
        Assert.Equal(result.ImportId, Value(await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Create"), ct)).ImportId);
        Assert.True((await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Skip"), ct)).IsFailure);
        Assert.Equal(2, await f.Db.PlatformRiderAccounts.CountAsync(ct)); Assert.Single(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
        var accounts = Value(await f.Service.AccountsFilteredAsync(new() { ExternalAccountId = "new-account" }, 1, 10, ct));
        var account = Assert.Single(accounts.Items);
        Assert.Equal("Normal", account.OperationalDisposition);
        Assert.False(account.IsAssigned);
        Assert.Null(account.ReportedCurrentRider);
        var unchanged = Value(await f.Service.PreviewAccountsAsync("again.xlsx", file, ct));
        Assert.Equal("Unchanged", Assert.Single(unchanged.Rows).Status);
        var keep = Value(await f.Service.ConfirmAccountsAsync(unchanged.PreviewId, Resolve(2, "Keep"), ct)); Assert.Equal(0, keep.ImportedAccounts);
        Assert.Single(await f.Db.Set<PlatformAccountBusinessRevision>().ToArrayAsync(ct));
    }

    [Fact]
    public async Task HungerAccountImportShowsChangedFieldsAndRequiresOwnerSpecificResolution()
    {
        await using var f = await Fixture.Create(); await Assign(f); var ct = TestContext.Current.CancellationToken;
        var first = Value(await f.Service.PreviewAccountsAsync("seed.xlsx", await SeedFile(f), ct));
        Value(await f.Service.ConfirmAccountsAsync(first.PreviewId, Resolve(2, "Create"), ct));
        var changed = Value(await f.Service.PreviewAccountsAsync("changed.xlsx", await SeedFile(f, phone: "new-phone"), ct));
        Assert.Equal("ChangedField", Assert.Single(changed.Rows).Status);
        Assert.Contains(changed.Rows[0].Changes, c => c.Field == "PhoneNumber" && c.Existing == "0501234567" && c.Incoming == "new-phone");
        Value(await f.Service.ConfirmAccountsAsync(changed.PreviewId, Resolve(2, "Keep"), ct));
        var other = new Employee { FullNameAr = "Other owner", IqamaNo = "9876543210" }; f.Db.Add(other); await f.Db.SaveChangesAsync(ct);
        var conflict = Value(await f.Service.PreviewAccountsAsync("owner.xlsx", await SeedFile(f, owner: other.IqamaNo!), ct));
        Assert.Equal("ConflictingOwner", Assert.Single(conflict.Rows).Status);
        Assert.True((await f.Service.ConfirmAccountsAsync(conflict.PreviewId, Resolve(2, "Update"), ct)).IsFailure);
        Value(await f.Service.ConfirmAccountsAsync(conflict.PreviewId, Resolve(2, "ReplaceOwner"), ct));
        Assert.Equal(other.Id, (await f.Db.PlatformRiderAccounts.SingleAsync(a => a.ExternalAccountId == "new-account", ct)).RegisteredEmployeeId);
    }

    [Fact]
    public async Task HungerAccountImportRejectsMissingOrInvalidDecisionBeforeAnyWrites()
    {
        await using var f = await Fixture.Create(); await Assign(f); var ct = TestContext.Current.CancellationToken;
        var preview = Value(await f.Service.PreviewAccountsAsync("seed.xlsx", await SeedFile(f, second: true), ct));
        Assert.True((await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Create"), ct)).IsFailure);
        Assert.True((await f.Service.ConfirmAccountsAsync(preview.PreviewId, new([new(2, "Create"), new(3, "Update")]), ct)).IsFailure);
        Assert.Single(await f.Db.PlatformRiderAccounts.ToArrayAsync(ct)); Assert.Empty(await f.Db.Set<PlatformAccountBusinessRevision>().ToArrayAsync(ct));
        Value(await f.Service.ConfirmAccountsAsync(preview.PreviewId, new([new(2, "Create"), new(3, "Skip")]), ct));
        Assert.Equal(2, await f.Db.PlatformRiderAccounts.CountAsync(ct));
    }

    [Fact]
    public async Task HungerAccountImportRevalidatesRevisionAndPermissions()
    {
        await using var f = await Fixture.Create(); await Assign(f); var ct = TestContext.Current.CancellationToken;
        var file = await SeedFile(f, "test-account");
        var preview = Value(await f.Service.PreviewAccountsAsync("seed.xlsx", file, ct));
        var detail = await f.Db.Set<PlatformAccountBusinessDetails>().SingleAsync(ct); detail.RegistryIdentifier = "changed after preview"; await f.Db.SaveChangesAsync(ct);
        var stale = await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Update"), ct);
        Assert.True(stale.IsFailure); Assert.Equal("hunger.conflict", stale.Error.Code);
        f.Checker.OnlyPermission = PermissionKeys.Hunger.AccountsImport;
        Assert.True((await f.Service.PreviewAccountsAsync("seed.xlsx", file, ct)).IsFailure);
        Assert.True((await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Update"), ct)).IsFailure);
    }

    [Fact]
    public async Task HungerImportTemplatesCoverEverySupportedWorkbookAndRejectErrorCells()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        foreach (var template in new[] { Value(await f.Service.AccountTemplateAsync(ct)), Value(await f.Service.TemplateAsync(ct)), Value(await f.Service.OperationTemplateAsync(ct)) })
        {
            using var book = new XLWorkbook(new MemoryStream(template.Content)); Assert.Single(book.Worksheets);
            Assert.True(book.Worksheet(1).Row(1).CellsUsed().Count() >= 9);
        }
        using var source = new XLWorkbook(new MemoryStream(await SeedFile(f)));
        source.Worksheet(1).Cell(2, 2).Value = "#N/A";
        using var stream = new MemoryStream(); source.SaveAs(stream);
        Assert.True((await f.Service.PreviewAccountsAsync("seed.xlsx", stream.ToArray(), ct)).IsFailure);
    }

    [Fact]
    public async Task HungerReportExportsApplyTheSameRiderAndAccountFiltersBeforeExport()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var rider = Guid.NewGuid(); var operation = Operation(f.Account.Id, new(2026, 11, 1), 8, Share(rider, 20, 8));
        operation.CaptainClassification = "HS Gold"; f.Db.Add(operation); await f.Db.SaveChangesAsync(ct);
        var filter = new HungerReportExportFilter { RiderId = rider, Accounts = new() { ExternalAccountId = "test-account", OperatingCityNameAr = "Test" }, CaptainClassification = "HS Gold" };
        var matched = Value(await f.Service.ExportOperationsAsync(new(2026, 11, 1), new(2026, 11, 1), null, filter, ct));
        using var book = new XLWorkbook(new MemoryStream(matched.Content)); Assert.Equal(2, book.Worksheet(1).RowsUsed().Count());
        var empty = Value(await f.Service.ExportOperationsAsync(new(2026, 11, 1), new(2026, 11, 1), null, filter with { RiderId = Guid.NewGuid() }, ct));
        using var emptyBook = new XLWorkbook(new MemoryStream(empty.Content)); Assert.Single(emptyBook.Worksheet(1).RowsUsed());
        var quality = Value(await f.Service.ExportQualityAsync(new(2026, 11, 1), null, filter, ct));
        using var qualityBook = new XLWorkbook(new MemoryStream(quality.Content)); Assert.Single(qualityBook.Worksheet(1).RowsUsed());
        var query = new HungerPerformanceQuery(new(2026, 11, 1), new(2026, 11, 1)) { Accounts = new() { ExternalAccountId = "missing" } };
        Assert.Empty(Value(await f.Service.PerformanceAsync(query, ct)).Items);
    }

    [Fact]
    public async Task HungerMissingAccountWithUnknownOwnerIsExplicitAndCanOnlyBeSkipped()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var preview = Value(await f.Service.PreviewAccountsAsync("seed.xlsx", await SeedFile(f), ct));
        var row = Assert.Single(preview.Rows); Assert.Equal("MissingAccount", row.Status); Assert.NotNull(row.Error);
        Assert.True((await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Create"), ct)).IsFailure);
        Assert.Equal(0, Value(await f.Service.ConfirmAccountsAsync(preview.PreviewId, Resolve(2, "Skip"), ct)).ImportedAccounts);
    }

    [Fact]
    public async Task HungerDeferredRemindersIncludeDueAndOverdueWithoutActivatingAccounts()
    {
        await using var f = await Fixture.Create(); await Assign(f); var ct = TestContext.Current.CancellationToken;
        var d = await f.Db.Set<PlatformAccountBusinessDetails>().SingleAsync(ct);
        foreach (var day in new[] { 3, 4, 5 })
        {
            d.OperationalDisposition = "DeferredNextMonth"; d.DeferredUntil = new(2026, 11, day); await f.Db.SaveChangesAsync(ct);
            var due = Value(await f.Service.DeferredRemindersAsync(ct: ct));
            if (day == 5) Assert.Empty(due); else Assert.Equal(4 - day, Assert.Single(due).OverdueDays);
            Assert.Equal("DeferredNextMonth", d.OperationalDisposition);
        }
        f.Checker.Allowed = false; Assert.True((await f.Service.DeferredRemindersAsync(ct: ct)).IsFailure);
        d.DeferredUntil = new(2026, 11, 3); await f.Db.SaveChangesAsync(ct);
        var hook = new HungerDeferredReminderQuery(f.Db, new Clock(), new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Features:Hunger"] = "true" }).Build());
        Assert.Single(Value(await hook.FindDueAsync(f.Account.ClientPlatformId, ct: ct)));
        Assert.True((await hook.FindDueAsync(Guid.NewGuid(), ct: ct)).IsFailure);
        Assert.Equal("DeferredNextMonth", d.OperationalDisposition);
    }

    [Fact]
    public async Task HungerExportsAndVerificationIncludeEveryFilteredRowBeyondFirstPage()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var platformId = f.Account.ClientPlatformId;
        for (var i = 0; i < 220; i++) f.Db.Add(new PlatformRiderAccount { ClientPlatformId = platformId, OperatingCityId = f.Account.OperatingCityId, Code = "code" + i, ExternalAccountId = "match" + i });
        await f.Db.SaveChangesAsync(ct);
        var all = Value(await HungerAccountVerification.ReadAllAsync(f.Service, ct)); Assert.Equal(221, all.Count);
        var export = Value(await f.Service.ExportAccountsAsync(new() { ExternalAccountId = "match" }, "xlsx", ct));
        using var book = new XLWorkbook(new MemoryStream(export.Content)); Assert.Equal(221, book.Worksheet(1).RowsUsed().Count());
        var csv = Value(await f.Service.ExportAccountsAsync(new() { ExternalAccountId = "missing" }, "csv", ct));
        Assert.Single(System.Text.Encoding.UTF8.GetString(csv.Content).Trim().Split('\n'));
    }
}
