using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Infrastructure.Ninja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    private static IConfiguration EvaluationConfig => new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["Features:NinjaEvaluation"] = "true" }).Build();
    private static NinjaEvaluationService Evaluation(Fixture f) => new(f.Db, f.User, f.Checker, f.Clock, EvaluationConfig);
    private static NinjaPerformanceImportService PerformanceImports(Fixture f) => new(f.Db, f.User, f.Checker, f.Clock, EvaluationConfig);
    private static byte[] PerformanceCsv(params (string Captain, DateOnly Date, int Orders, string Hours)[] rows)
    {
        var text = new StringBuilder("Captain ID,Total Orders,Total Working Hours,Date\n");
        foreach (var row in rows) text.Append(CultureInfo.InvariantCulture, $"{row.Captain},{row.Orders},{row.Hours},{row.Date:yyyy-MM-dd}\n");
        return Encoding.UTF8.GetBytes(text.ToString());
    }
    private static async Task SeptemberAssignment(Fixture f)
    {
        f.Clock.Now = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 9, 1), "0501111111", "initial", RiderType: "InternalMotorcycle", PaymentModel: "PayPerOrder"), TestContext.Current.CancellationToken));
        f.Clock.Now = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
    }
    private static async Task<NinjaPerformanceConfirmation> ImportCsv(Fixture f, byte[] csv)
    {
        var ct = TestContext.Current.CancellationToken; var imports = PerformanceImports(f);
        var preview = Value(await imports.PreviewAsync("performance.csv", csv, new("MultiDay"), ct));
        Assert.True(preview.CanConfirm, string.Join(";", preview.Rows.SelectMany(r => r.Issues)));
        return Value(await imports.ConfirmAsync(preview.PreviewId, "performance.csv", csv, ct));
    }

    [Fact]
    public async Task EvaluationSharedRidersHaveIndependentMoneyAndAccountComparison()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await SeptemberAssignment(f);
        var first = Assert.Single(f.Db.RiderClientAssignments);
        f.Clock.Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
        Value(await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 9, 10), "0501111111", "second", "Returned", Convert.ToBase64String(first.RowVersion)), ct));
        f.Clock.Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, new(2026, 9, 11), "0502222222", RiderType: "InternalMotorcycle", PaymentModel: "PayPerOrder"), ct));
        f.Clock.Now = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);
        var csv = PerformanceCsv(Enumerable.Range(1, 30).Select(day => ("272578", new DateOnly(2026, 9, day), day <= 10 ? 16 : 14, "8")).ToArray());
        var imported = await ImportCsv(f, csv); Assert.Equal(30, imported.ImportedRows);
        var report = Value(await Evaluation(f).EvaluateAsync(new() { Year = 2026, Month = 9 }, ct));
        Assert.Equal(2, report.Items.Count);
        var a = report.Items.Single(r => r.RiderProfileId == f.Rider.Id); var b = report.Items.Single(r => r.RiderProfileId == f.OwnerRider.Id);
        Assert.Equal(150m, a.PeriodTarget); Assert.Equal(300m, b.PeriodTarget);
        Assert.Equal(2010m, a.SuggestedMoney!.SuggestedAmount); Assert.Equal(3480m, b.SuggestedMoney!.SuggestedAmount);
        var comparison = Assert.Single(report.Summary.AccountComparisons);
        Assert.Equal(5540m, comparison.AccountSuggestedAmount); Assert.Equal(5490m, comparison.RiderSuggestedAmounts); Assert.Equal(50m, comparison.Difference);
        Assert.True(a.SuggestedMoney.Provisional); Assert.Null(a.SuggestedMoney.ActualInvoiceAmount);
        Assert.All(a.OtherEligibility, c => Assert.Equal("Unknown", c.Value));
        Assert.Empty(f.Db.JahezOperations); Assert.Empty(f.Db.JahezTransferredDebts);
    }
    [Fact]
    public async Task EvaluationMissingDaysDoNotStopLaterPerformanceAndRemainUnknown()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        await ImportCsv(f, PerformanceCsv(("272578", new(2026, 9, 1), 15, "8"), ("272578", new(2026, 9, 3), 30, "8")));
        var report = Value(await Evaluation(f).EvaluateAsync(new() { Year = 2026, Month = 9, AsOf = new(2026, 9, 3) }, ct));
        var row = Assert.Single(report.Items);
        Assert.Equal(45, row.Observed.Orders); Assert.Equal(45, row.Evaluated.Orders);
        Assert.Equal(16m, row.Evaluated.Hours); Assert.Equal(30m, row.Evaluated.Target);
        Assert.Equal(2m, row.Attendance.Qualified); Assert.Equal(28m, row.Attendance.Unknown);
        Assert.Equal(0, row.Attendance.ShortHoursDays);
        Assert.Equal(545m, row.SuggestedMoney!.SuggestedAmount); Assert.False(row.SuggestedMoney.AttendanceReduction);
        Assert.Equal(new DateOnly(2026, 9, 3), row.LatestCompleteDate); Assert.Equal(new DateOnly(2026, 9, 3), row.LatestImportedDate);
        Assert.Null(row.Forecast); Assert.NotNull(row.ContinuationScenario);
        Assert.Equal("Missing", row.Days.Single(d => d.Date.Day == 2).Coverage); Assert.Null(row.Days.Single(d => d.Date.Day == 2).Orders);
        Assert.Equal(30, row.Days.Count); Assert.Contains("MissingCoverage", row.Reasons);
        var account = Assert.Single(Value(await Evaluation(f).EvaluateAsync(new() { Year = 2026, Month = 9, View = "accounts", AsOf = new(2026, 9, 3) }, ct)).Items);
        Assert.Equal(row.Evaluated, account.Evaluated); Assert.Equal(row.Attendance, account.Attendance);
        var earlier = Assert.Single(Value(await Evaluation(f).EvaluateAsync(new() { Year = 2026, Month = 9, AsOf = new(2026, 9, 2) }, ct)).Items);
        Assert.Equal(15, earlier.Evaluated.Orders); Assert.Equal(1m, earlier.Attendance.Qualified);
    }
    [Fact]
    public async Task EvaluationRosterIncludesUnobservedRidersAndZeroOrdersAreNotAbsence()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        var service = Evaluation(f);
        var empty = Assert.Single(Value(await service.EvaluateAsync(new() { Year = 2026, Month = 9 }, ct)).Items);
        Assert.Null(empty.SuggestedMoney); Assert.Null(empty.Forecast); Assert.Equal("Unknown", empty.Attendance.Status);
        await ImportCsv(f, PerformanceCsv(("272578", new(2026, 9, 1), 0, "8")));
        var zero = Assert.Single(Value(await service.EvaluateAsync(new() { Year = 2026, Month = 9 }, ct)).Items);
        Assert.Equal("Qualified", zero.Days[0].HoursStatus); Assert.Equal(1, zero.Attendance.Qualified);
        Assert.Equal(0m, zero.SuggestedMoney!.SuggestedAmount);
    }
    [Fact]
    public async Task EvaluationSnapshotsStayImmutableAndFrozenResponsibilityNeedsReview()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        await ImportCsv(f, PerformanceCsv(("272578", new(2026, 9, 1), 15, "8")));
        var service = Evaluation(f); var query = new NinjaEvaluationQuery { Year = 2026, Month = 9 };
        var report = Value(await service.EvaluateAsync(query, ct));
        var frozen = Value(await service.SnapshotAsync(new(query, "Reviewed monitoring result", true, ExpectedInputRevision: report.InputRevision), ct));
        var request = new NinjaResponsibilityRequest(2026, 9, null, [], "Review schedule");
        Assert.Equal("ninja.frozen_review_required", (await service.SetResponsibilityAsync(f.Account.Id, request, ct)).Error.Code);
        var changed = Value(await service.SetResponsibilityAsync(f.Account.Id, request with { ReviewedSnapshotId = frozen.Id }, ct));
        Assert.Equal(1, changed.Revision);
        await ImportCsv(f, PerformanceCsv(("272578", new(2026, 9, 2), 30, "8")));
        var saved = Value(await service.GetSnapshotAsync(frozen.Id, ct));
        Assert.Equal(15, Assert.Single(saved.Report.Items).Observed.Orders);
        Assert.Equal(45, Assert.Single(Value(await service.EvaluateAsync(query, ct)).Items).Observed.Orders);
        Assert.True((await service.SetResponsibilityAsync(f.Account.Id, request with { ReviewedSnapshotId = frozen.Id }, ct)).IsFailure);
        var entity = await f.Db.Set<NinjaEvaluationSnapshot>().SingleAsync(ct);
        entity.Reason = "mutated";
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }
    [Fact]
    public async Task EvaluationImportIsAtomicIdempotentAndBoundToPreviewAndCurrentVersions()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        var service = PerformanceImports(f);
        var invalid = PerformanceCsv(("272578", new(2026, 9, 1), 15, "8"), ("999", new(2026, 9, 2), 20, "8"));
        var bad = Value(await service.PreviewAsync("performance.csv", invalid, new("MultiDay"), ct));
        Assert.False(bad.CanConfirm); Assert.True((await service.ConfirmAsync(bad.PreviewId, "performance.csv", invalid, ct)).IsFailure); Assert.Empty(f.Db.NinjaDailyPerformance);
        var csv = PerformanceCsv(("272578", new(2026, 9, 1), 15, "8"), ("272578", new(2026, 9, 2), 20, "8"));
        var preview = Value(await service.PreviewAsync("performance.csv", csv, new("MultiDay"), ct));
        Value(await Evaluation(f).SetResponsibilityAsync(f.Account.Id, new(2026, 9, null, [], "Schedule reviewed"), ct));
        Assert.Equal("ninja.preview_stale", (await service.ConfirmAsync(preview.PreviewId, "performance.csv", csv, ct)).Error.Code);
        Assert.Empty(f.Db.NinjaDailyPerformance);
        preview = Value(await service.PreviewAsync("performance.csv", csv, new("MultiDay"), ct));
        var confirmed = Value(await service.ConfirmAsync(preview.PreviewId, "performance.csv", csv, ct));
        var replay = Value(await service.ConfirmAsync(preview.PreviewId, "performance.csv", csv, ct));
        Assert.Equal(confirmed.ArtifactId, replay.ArtifactId); Assert.Equal(confirmed.ImportedRows, replay.ImportedRows);
        Assert.Equal(2, await f.Db.NinjaDailyPerformance.CountAsync(ct));
        var repeated = await ImportCsv(f, csv); Assert.Equal(0, repeated.ImportedRows); Assert.Equal(2, repeated.SkippedRows);
        Assert.Equal(1, await f.Db.Set<NinjaPerformanceArtifact>().CountAsync(a => a.Content.Length > 0, ct));
    }
    [Fact]
    public async Task EvaluationExportIncludesAllFilteredRowsAndPermissionsRemainIndependent()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        await ImportCsv(f, PerformanceCsv(("272578", new(2026, 9, 1), 15, "8")));
        var service = Evaluation(f); var query = new NinjaEvaluationQuery { Year = 2026, Month = 9, Page = 2, PageSize = 1 };
        Assert.Empty(Value(await service.EvaluateAsync(query, ct)).Items);
        var export = Value(await service.ExportAsync(query, ct));
        using var book = new XLWorkbook(new MemoryStream(export.Content));
        Assert.Equal(2, book.Worksheet("Suggestions").LastRowUsed()!.RowNumber());
        Assert.Equal(31, book.Worksheet("Daily").LastRowUsed()!.RowNumber());
        f.Checker.Granted = [PermissionKeys.Ninja.Read];
        Assert.True((await service.EvaluateAsync(query, ct)).IsSuccess);
        Assert.Equal("ninja.forbidden", (await service.ExportAsync(query, ct)).Error.Code);
        Assert.Equal("ninja.forbidden", (await PerformanceImports(f).PreviewAsync("x.csv", [], new(), ct)).Error.Code);
        Assert.Equal("ninja.forbidden", (await service.SetContextAsync(f.Account.Id, new(null, null, [], "Review"), ct)).Error.Code);
        var disabled = new NinjaEvaluationService(f.Db, f.User, f.Checker, f.Clock, new ConfigurationBuilder().Build());
        Assert.Equal("ninja.forbidden", (await disabled.EvaluateAsync(query, ct)).Error.Code);
    }
    [Fact]
    public void EvaluationCsvDurationDatesAndDuplicateValidationIsExplicit()
    {
        var today = new DateOnly(2026, 10, 26);
        var csv = Encoding.UTF8.GetBytes("Captain ID,Total Orders,Total Working Hours\n000123,15,08:30:00\n");
        var parsed = NinjaPerformanceParser.Parse("daily.csv", csv, new("Daily", today, HoursUnit: "Duration"), today, TestContext.Current.CancellationToken);
        Assert.Empty(parsed.Issues); Assert.Equal("000123", parsed.Rows[0].CaptainId); Assert.Equal(8.5m, parsed.Rows[0].Hours);
        Assert.NotEmpty(NinjaPerformanceParser.Parse("daily.csv", csv, new(), today, TestContext.Current.CancellationToken).Issues);
        var tooLong = Encoding.UTF8.GetBytes("Captain ID,Total Orders,Total Working Hours\n000123,15,24:00:01\n");
        Assert.NotEmpty(NinjaPerformanceParser.Parse("daily.csv", tooLong, new("Daily", today, HoursUnit: "Duration"), today, TestContext.Current.CancellationToken).Issues);
        var multiple = PerformanceCsv(("272578", today, 15, "8"), ("272578", today, 20, "8"));
        Assert.NotEmpty(NinjaPerformanceParser.Parse("monthly.csv", multiple, new("MultiDay"), today, TestContext.Current.CancellationToken).Issues);
        Assert.NotEmpty(NinjaPerformanceParser.Parse("monthly.csv", multiple, new("Daily", today), today, TestContext.Current.CancellationToken).Issues);
    }
}
