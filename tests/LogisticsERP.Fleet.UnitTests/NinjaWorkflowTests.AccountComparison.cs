using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Ninja;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task AccountComparisonUsesRegisteredOwnerAndNeverChangesLifecycleOrCredentials()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var owner = await f.Db.Employees.SingleAsync(e => e.Id == f.OwnerRider.EmployeeId, ct);
        owner.IqamaNo = "2552431948"; owner.FullNameEn = "ACCOUNT OWNER";
        var actual = await f.Db.Employees.SingleAsync(e => e.Id == f.Rider.EmployeeId, ct);
        actual.IqamaNo = "2560597839"; actual.FullNameEn = "ACTUAL RIDER";
        await f.Db.SaveChangesAsync(ct);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "unchanged", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var beforeVersion = f.Account.RowVersion.ToArray(); var beforeUpdate = f.Account.UpdatedAtUtc;
        var clean = Value(await f.Service.CompareAccountsAsync("owners.xlsx",
            AccountSheet(["272578", " account   owner ", " RIYADH ", "٢٥٥٢٤٣١٩٤٨"]), ct));
        Assert.True(clean.ComparisonCompleted); Assert.Single(clean.MatchingAccounts); Assert.Empty(clean.DifferentAccounts);
        var mismatch = Value(await f.Service.CompareAccountsAsync("owners.xlsx",
            AccountSheet(["272578", "ACTUAL RIDER", "JEDDAH", "2560597839"]), ct));
        var differences = Assert.Single(mismatch.DifferentAccounts).Differences;
        Assert.Collection(differences, d => Assert.Equal("ownerNationalId", d.Field),
            d => Assert.Equal("ownerName", d.Field), d => Assert.Equal("city", d.Field));
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal(beforeVersion, f.Account.RowVersion); Assert.Equal(beforeUpdate, f.Account.UpdatedAtUtc);
        Assert.Equal("unchanged", f.Password()); Assert.Single(f.Db.PlatformAccountCredentialVersions);
        Assert.Single(f.Db.RiderClientAssignments); Assert.Empty(f.Db.Notifications); Assert.Empty(f.Db.NinjaDailyImports);
        Assert.Empty(f.Db.NinjaDailyPerformance); Assert.Empty(f.Db.NinjaDailyDeductions);
        Assert.Equal("2552431948", owner.IqamaNo); Assert.Equal("272578", f.Account.ExternalAccountId);
    }

    [Fact]
    public async Task ComparisonSeparatesMissingAccountsAndOffersOwnerBasedReviewCandidates()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var owner = await f.Db.Employees.SingleAsync(e => e.Id == f.OwnerRider.EmployeeId, ct);
        owner.IqamaNo = "2552431948"; owner.FullNameEn = "OWNER";
        f.Db.PlatformRiderAccounts.Add(new() { ClientPlatformId = f.Account.ClientPlatformId, ExternalAccountId = "888888",
            Code = "NINJA-ARCHIVED", OperatingCityId = OperatingCity.RiyadhId, Status = PlatformRiderAccountStatus.Archived });
        f.Db.PlatformRiderAccounts.Add(new() { ClientPlatformId = f.Account.ClientPlatformId, ExternalAccountId = "777777",
            Code = "NINJA-DELETED", OperatingCityId = OperatingCity.RiyadhId, IsDeleted = true });
        var other = new ClientPlatform { Code = "OTHER", NameAr = "أخرى", NameEn = "Other" };
        f.Db.ClientPlatforms.Add(other);
        f.Db.PlatformRiderAccounts.Add(new() { ClientPlatformId = other.Id, ExternalAccountId = "999999",
            Code = "OTHER-1", OperatingCityId = OperatingCity.RiyadhId });
        await f.Db.SaveChangesAsync(ct);
        var report = Value(await f.Service.CompareAccountsAsync("x.xlsx", AccountSheet(["295975", "OWNER", "Riyadh", "2552431948"]), ct));
        Assert.True(report.ComparisonCompleted); Assert.Equal(1, report.Summary.SheetAccounts); Assert.Equal(2, report.Summary.SystemAccounts);
        Assert.Equal(1, report.Summary.SheetOnlyAccounts); Assert.Equal(2, report.Summary.SystemOnlyAccounts);
        var onlySheet = Assert.Single(report.SheetOnlyAccounts);
        Assert.Equal(1, onlySheet.PossibleSystemAccountCount);
        Assert.Equal(f.Account.Id, Assert.Single(onlySheet.PossibleSystemAccounts).AccountId);
        Assert.Contains(report.SystemOnlyAccounts, a => a.CaptainId == "888888");
        Assert.DoesNotContain(report.SystemOnlyAccounts, a => a.CaptainId is "777777" or "999999");
    }

    [Fact]
    public async Task ComparisonRetainsAccountsWithMissingOwnerAndCityReferencesAndNormalizesIdentifiers()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        f.Account.RegisteredEmployeeId = null; f.Account.OperatingCityId = Guid.NewGuid();
        f.Account.ExternalAccountId = " ٠٠١٢٣٤ "; await f.Db.SaveChangesAsync(ct);
        var report = Value(await f.Service.CompareAccountsAsync("x.xlsx", AccountSheet(["001234", "OWNER", "Riyadh", "2552431948"]), ct));
        Assert.True(report.ComparisonCompleted); Assert.Empty(report.SystemOnlyAccounts); Assert.Empty(report.SheetOnlyAccounts);
        var changed = Assert.Single(report.DifferentAccounts); Assert.Equal(f.Account.Id, changed.System.AccountId);
        Assert.Equal(3, changed.Differences.Count); Assert.Null(changed.System.OwnerNationalId); Assert.Null(changed.System.CityNameEn);
    }

    [Fact]
    public async Task InvalidSheetDoesNotGenerateFalseMissingResultsAndReadPermissionIsEnforced()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var invalid = Value(await f.Service.CompareAccountsAsync("x.xlsx", AccountSheet(["bad-id", "OWNER", "Riyadh", "2552431948"]), ct));
        Assert.False(invalid.ComparisonCompleted); Assert.Single(invalid.Issues);
        Assert.Empty(invalid.SystemOnlyAccounts); Assert.Empty(invalid.SheetOnlyAccounts);
        f.Checker.Denied = PermissionKeys.Ninja.Read;
        var denied = await f.Service.CompareAccountsAsync("x.xlsx", AccountSheet(["272578", "OWNER", "Riyadh", "2552431948"]), ct);
        Assert.Equal("ninja.forbidden", denied.Error.Code);
        Assert.DoesNotContain(f.Db.ChangeTracker.Entries(), e => e.State is EntityState.Modified or EntityState.Added);
    }

    [Fact]
    public void AccountParserAcceptsProviderFootersAndRepeatedOwnersButRejectsInvalidAccountRows()
    {
        var ct = TestContext.Current.CancellationToken;
        var parsed = NinjaAccountSheetParser.Parse(AccountSheet(
            ["001234", "OWNER", "JEDDAH", "2552431948"], ["٠٠٥٦٧٨", "OWNER", "Riyadh", "2552431948"],
            ["الصفوف في كل صفحة", "", "", ""], ["200 من 200-1", "", "", ""]), ct);
        Assert.Empty(parsed.Issues); Assert.Equal(2, parsed.Rows.Count); Assert.Equal(2, parsed.IgnoredFooterRows);
        Assert.Equal("001234", parsed.Rows[0].CaptainId); Assert.Equal("005678", parsed.Rows[1].CaptainId);
        Assert.Single(NinjaAccountSheetParser.Parse(AccountSheet(["123", "OWNER", "Riyadh", "2552431948"],
            ["١٢٣", "OWNER", "Riyadh", "2552431948"]), ct).Issues);
        Assert.Single(NinjaAccountSheetParser.Parse(AccountSheet(["123", "OWNER", "Riyadh", "bad"]), ct).Issues);
        Assert.Single(NinjaAccountSheetParser.Parse(AccountSheet(["غير صالح", "", "", ""]), ct).Issues);
        Assert.Single(NinjaAccountSheetParser.Parse(AccountSheet(["الصفوف في كل صفحة", "", "", ""],
            ["123", "OWNER", "Riyadh", "2552431948"]), ct).Issues);
        using var book = new XLWorkbook(new MemoryStream(AccountSheet(["123", "OWNER", "Riyadh", "2552431948"])));
        book.Worksheet(1).Cell(2, 1).FormulaA1 = "123";
        using var stream = new MemoryStream(); book.SaveAs(stream);
        Assert.Single(NinjaAccountSheetParser.Parse(stream.ToArray(), ct).Issues);
        book.Worksheet(1).Cell(2, 1).FormulaA1 = "";
        book.Worksheet(1).Cell(2, 1).Value = 123;
        book.Worksheet(1).Cell(2, 1).Style.NumberFormat.Format = "000000";
        using var paddedStream = new MemoryStream(); book.SaveAs(paddedStream);
        Assert.Equal("000123", Assert.Single(NinjaAccountSheetParser.Parse(paddedStream.ToArray(), ct).Rows).CaptainId);
    }

    [Fact]
    public async Task ComparisonSqlIncludesScopedLeftJoinsAndBoundedOrdering()
    {
        await using var f = await Fixture.Create();
        await using var sql = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=TranslationOnly;Trusted_Connection=True;TrustServerCertificate=True").Options, f.Clock);
        var query = f.CreateService(sql).ComparisonAccounts([f.Account.ClientPlatformId]).Take(10001).ToQueryString();
        Assert.Contains("LEFT JOIN", query); Assert.Contains("RegisteredEmployeeId", query);
        Assert.Contains("TOP", query); Assert.Contains("NINJA", query);
        Assert.Contains("ORDER BY", query);
        Assert.DoesNotContain("RiderClientAssignments", query);
    }

    [Fact]
    public async Task AmbiguousNormalizedSystemIdentifiersStopComparisonInsteadOfSelectingAnAccount()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        f.Db.PlatformRiderAccounts.Add(new() { ClientPlatformId = f.Account.ClientPlatformId, ExternalAccountId = "٢٧٢٥٧٨",
            Code = "NINJA-NORMALIZED-COLLISION", OperatingCityId = OperatingCity.RiyadhId });
        await f.Db.SaveChangesAsync(ct);
        var report = Value(await f.Service.CompareAccountsAsync("x.xlsx", AccountSheet(["272578", "OWNER", "Riyadh", "2552431948"]), ct));
        Assert.False(report.ComparisonCompleted); Assert.Single(report.Issues); Assert.Equal(2, report.Summary.SystemAccounts);
        Assert.Empty(report.DifferentAccounts); Assert.Empty(report.MatchingAccounts); Assert.Empty(report.SystemOnlyAccounts);
    }

    [Fact]
    public void ProvidedAccountWorkbookParsesActualAccountsAndSkipsOnlyItsTwoFooters()
    {
        var path = Environment.GetEnvironmentVariable("NINJA_ACCOUNTS_SAMPLE");
        Assert.SkipUnless(path is not null, "Set NINJA_ACCOUNTS_SAMPLE to validate the supplied owner-account workbook.");
        using var file = new FileStream(path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var content = new MemoryStream(); file.CopyTo(content);
        var parsed = NinjaAccountSheetParser.Parse(content.ToArray(), TestContext.Current.CancellationToken);
        Assert.Empty(parsed.Issues); Assert.Equal(163, parsed.Rows.Count); Assert.Equal(2, parsed.IgnoredFooterRows);
        Assert.Equal("295975", parsed.Rows[0].CaptainId); Assert.Equal("2552431948", parsed.Rows[0].OwnerNationalId);
    }

    private static byte[] AccountSheet(params string[][] rows)
    {
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Accounts");
        string[] headers = ["المعرف", "الاسم", "المدينة", "رقم الهوية"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var n = 0; n < rows.Length; n++)
            for (var i = 0; i < rows[n].Length; i++) sheet.Cell(n + 2, i + 1).Value = rows[n][i];
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
}
