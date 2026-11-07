using System.Text.Json;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class HungerQualityServiceTests
{
    private static readonly JsonSerializerOptions PerformanceTestJson = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task HistoricalSourceRidersRemainInDistinctCountsWithoutCurrentAssignments()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        f.Db.Add(Operation(f.Account.Id, new(2026, 11, 1), 8, Share(Guid.NewGuid(), 8, 3), Share(Guid.NewGuid(), 12, 5)));
        await f.Db.SaveChangesAsync(ct);
        var account = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 1)), ct));
        var riders = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 1), GroupBy: "rider"), ct));
        Assert.Equal(1, account.DistinctAccounts); Assert.Equal(2, account.DistinctRiders); Assert.Equal(2, riders.DistinctRiders);
        Assert.Equal(account.CompletedOrders, riders.CompletedOrders); Assert.Equal(account.WorkingHours, riders.WorkingHours);
    }
    [Fact]
    public async Task ActiveAccountShowsOnlyTheRiderFromItsCurrentAssignment()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var employee = new Employee { FullNameAr = "Reported rider", IqamaNo = "1234567890" };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var import = new HungerImportPreview { Kind = "Accounts", CreatedAtUtc = new Clock().GetUtcNow(), ResultJson = "{}" };
        f.Db.AddRange(employee, rider, import, new PlatformAccountBusinessDetails { PlatformRiderAccountId = f.Account.Id,
            OperationalDisposition = "Normal", SourceStatus = "يعمل", SourceActualRiderProfileId = rider.Id,
            SourceActualIqama = employee.IqamaNo, SourceAssignmentFrom = new(2026, 11, 1), SourceReference = $"Import:{import.Id}/2" });
        await f.Db.SaveChangesAsync(ct);
        var item = await Item(f);
        Assert.Equal("Normal", item.OperationalDisposition);
        Assert.Equal("Unknown", item.MonthlyUsageStatus);
        Assert.Equal(rider.Id, item.SourceActualRider?.RiderProfileId);
        Assert.Null(item.ActualRider);
        Assert.Null(item.ReportedCurrentRider);
        Assert.Equal("Unassigned", item.ReportedAssignmentStatus);
        Assert.Equal("None", item.CurrentRiderSource);

        var assignedEmployee = new Employee { FullNameAr = "Assigned rider", IqamaNo = "9876543210" };
        var assignedRider = new RiderProfile { EmployeeId = assignedEmployee.Id };
        f.Db.AddRange(assignedEmployee, assignedRider, new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id,
            RiderProfileId = assignedRider.Id, EffectiveFrom = new(2026, 11, 1), Status = RiderAssignmentStatus.Active });
        await f.Db.SaveChangesAsync(ct);

        item = await Item(f);
        Assert.Equal(assignedRider.Id, item.ActualRider?.RiderProfileId);
        Assert.Equal(item.ActualRider, item.ReportedCurrentRider);
        Assert.Equal("Assigned", item.ReportedAssignmentStatus);
        Assert.Equal("Assignment", item.CurrentRiderSource);
        Assert.Equal("Unknown", item.MonthlyUsageStatus);
    }
    [Fact]
    public async Task MonthlyNoShowInvoiceReconcilesSeveralDaysWithinItsObservedShiftCount()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var sourceRequest = new HungerSettlementRequest(f.Account.ClientPlatformId, null, "Invoice", new(2026, 11, 1), new(2026, 11, 1), new(2026, 11, 3),
            new(2026, 11, 3), null, -60, "Breakdown", [new("monthly-no-show", "Breakdown", f.Account.Id, null, -60, 0, null, "invoice count") { NoShowCount = 3 }], null, "monthly", "invoice");
        var invoice = new HungerSettlementDocument { ClientPlatformId = f.Account.ClientPlatformId, Kind = "Invoice", Status = "Matched", ChargeFrom = new(2026, 11, 1), ChargeThrough = new(2026, 11, 3), DataJson = JsonSerializer.Serialize(sourceRequest, PerformanceTestJson) };
        f.Db.Add(invoice);
        for (var day = 1; day <= 3; day++)
        {
            var operation = Operation(f.Account.Id, new(2026, 11, day), 8); operation.NoShowCount = day == 2 ? 2 : 1;
            f.Db.AddRange(operation, new HungerPotentialFine { DailyOperationId = operation.Id, NoShowCount = operation.NoShowCount.Value });
        }
        await f.Db.SaveChangesAsync(ct);
        var rows = Value(await f.Service.PotentialFinesAsync(new(2026, 11, 1), new(2026, 11, 3), ct));
        Assert.True((await f.Service.ResolvePotentialFineAsync(rows[0].Id, new("Invoiced", invoice.Id, "monthly-no-show", "day 1", rows[0].RowVersion), ct)).IsSuccess);
        Assert.True((await f.Service.ResolvePotentialFineAsync(rows[1].Id, new("Invoiced", invoice.Id, "monthly-no-show", "day 2", rows[1].RowVersion), ct)).IsSuccess);
        Assert.True((await f.Service.ResolvePotentialFineAsync(rows[2].Id, new("Invoiced", invoice.Id, "monthly-no-show", "over capacity", rows[2].RowVersion), ct)).IsFailure);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
    }
    [Fact]
    public async Task FutureEffectiveTargetVersionParticipatesInMonthEndForecast()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await Assign(f);
        foreach (var (day, target) in new[] { (1, 300), (10, 600) })
            Value(await f.Service.AddAlertRuleAsync(new(f.Account.ClientPlatformId, "Target", target, "Info", new(2026, 11, day), true, "dated contract")
                { Kind = "Target", Metric = "CompletedOrders", Period = "Month", Operator = "GreaterThanOrEqual" }, ct));
        f.Db.AddRange(Operation(f.Account.Id, new(2026, 11, 1), 8), Operation(f.Account.Id, new(2026, 11, 2), 8)); await f.Db.SaveChangesAsync(ct);
        var row = Assert.Single(Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 2)), ct)).Items);
        Assert.Equal(510, row.Target); Assert.Equal(600, row.Forecast);
        Assert.Equal(2, row.Catalog.Count(e => e.Kind == "Target"));
    }

    [Fact]
    public async Task ExcelTypedDurationUsesExplicitDurationMapping()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        using var book = new XLWorkbook(new MemoryStream(OperationsFile("08:30:00", "Duration")));
        book.Worksheet(1).Cell(2, 3).Value = TimeSpan.FromHours(8.5);
        book.Worksheet(1).Cell(2, 3).Style.NumberFormat.Format = "[h]:mm:ss";
        using var stream = new MemoryStream(); book.SaveAs(stream);
        var preview = Value(await f.Service.PreviewOperationsAsync("ops.xlsx", stream.ToArray(), new(2026, 11, 1), ct));
        Assert.True(preview.CanConfirm);
        Value(await f.Service.ConfirmOperationsAsync(preview.PreviewId, ct));
        Assert.Equal(8.5m, Assert.Single(await f.Db.Set<HungerDailyOperation>().ToArrayAsync(ct)).ActualWorkingHours);
    }
    [Fact]
    public async Task MonthEndRunRateUsesCompleteScheduledDaysAndExcludesTodaysPartialData()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await Assign(f);
        Value(await f.Service.AddAlertRuleAsync(new(f.Account.ClientPlatformId, "Target", 300, "Info", new(2026, 11, 1), true, "approved target")
            { Kind = "Target", Period = "Month", Metric = "CompletedOrders", Operator = "GreaterThanOrEqual" }, ct));
        for (var day = 1; day <= 4; day++)
        { var operation = Operation(f.Account.Id, new(2026, 11, day), 8); operation.AcceptedOrders = 25; f.Db.Add(operation); }
        await f.Db.SaveChangesAsync(ct);
        var row = Assert.Single(Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 30)), ct)).Items);
        Assert.Equal(80, row.CompletedOrders); Assert.Equal(100, row.AcceptedOrders); Assert.Equal(60, row.ForecastObservedOrders);
        Assert.Equal(3, row.ElapsedDays); Assert.Equal(27, row.RemainingDays); Assert.Equal(600, row.Forecast);
        var partialRange = Assert.Single(Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 2)), ct)).Items);
        Assert.Equal(600, partialRange.Forecast); Assert.Equal(28, partialRange.RemainingDays); Assert.Equal(300, partialRange.Target);
        Assert.Equal(240m / 27, row.RequiredPace); Assert.Equal(new DateOnly(2026, 11, 3), row.CoverageThrough);
    }

    [Fact]
    public async Task OneRiderOnTwoAccountsHasOneWorkedDayAndIndependentAccountTotals()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; var rider = await Assign(f);
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, OperatingCityId = f.Account.OperatingCityId, ExternalAccountId = "second" };
        f.Db.AddRange(second, new RiderClientAssignment { PlatformRiderAccountId = second.Id, RiderProfileId = rider.Id, Status = RiderAssignmentStatus.Active, EffectiveFrom = new(2026, 11, 1) },
            Operation(f.Account.Id, new(2026, 11, 1), 4, Share(rider.Id, 20, 4)), Operation(second.Id, new(2026, 11, 1), 4, Share(rider.Id, 20, 4)));
        await f.Db.SaveChangesAsync(ct);
        var report = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 1), GroupBy: "rider"), ct));
        var row = Assert.Single(report.Items); Assert.Equal(1, row.WorkedDays); Assert.Equal(1, row.QualifiedDays);
        Assert.Equal(40, row.CompletedOrders); Assert.Equal(8, row.WorkingHours); Assert.Equal(1, report.DistinctRiders); Assert.Equal(2, report.DistinctAccounts);
    }

    [Theory]
    [InlineData("0.5", "FractionOfDay", 12)]
    [InlineData("0.3541666666666667", "FractionOfDay", 8.5)]
    [InlineData("8.5", "DecimalHours", 8.5)]
    public async Task ExcelHoursImportUsesDeclaredUnit(string value, string unit, decimal expected)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var preview = Value(await f.Service.PreviewOperationsAsync("ops.xlsx", OperationsFile(value, unit), new(2026, 11, 1), ct));
        Value(await f.Service.ConfirmOperationsAsync(preview.PreviewId, ct));
        var row = Assert.Single(await f.Db.Set<HungerDailyOperation>().ToArrayAsync(ct)); Assert.Equal(expected, row.ActualWorkingHours); Assert.Equal(unit, row.HoursUnit);
    }

    [Fact]
    public async Task NoShowCanReconcileOnlyToMatchedCanonicalInvoiceEvidenceWithoutPostingDebt()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var operation = Operation(f.Account.Id, new(2026, 11, 1), 8); operation.NoShowCount = 3;
        var fine = new HungerPotentialFine { DailyOperationId = operation.Id, NoShowCount = 3 };
        var invoiceRequest = new HungerSettlementRequest(f.Account.ClientPlatformId, null, "Invoice", new(2026, 11, 1), new(2026, 11, 1), new(2026, 11, 3),
            new(2026, 11, 3), null, -60, "Breakdown", [new("no-show-3", "Breakdown", f.Account.Id, null, -60, 0, null, "invoice line")], null, "invoice", "invoice evidence");
        var invoice = new HungerSettlementDocument { ClientPlatformId = f.Account.ClientPlatformId, Kind = "Invoice", Status = "NeedsReview", ChargeFrom = new(2026, 11, 1), ChargeThrough = new(2026, 11, 3),
            DataJson = JsonSerializer.Serialize(invoiceRequest, PerformanceTestJson) };
        var wrongRiderRequest = invoiceRequest with { Lines = [new("no-show-3", "Breakdown", f.Account.Id, Guid.NewGuid(), -60, 0, null, "other rider")] };
        var wrongRiderInvoice = new HungerSettlementDocument { ClientPlatformId = f.Account.ClientPlatformId, Kind = "Invoice", Status = "Matched", ChargeFrom = new(2026, 11, 1), ChargeThrough = new(2026, 11, 3), DataJson = JsonSerializer.Serialize(wrongRiderRequest, PerformanceTestJson) };
        f.Db.AddRange(operation, fine, invoice, wrongRiderInvoice); await f.Db.SaveChangesAsync(ct);
        var row = Assert.Single(Value(await f.Service.PotentialFinesAsync(new(2026, 11, 1), new(2026, 11, 3), ct)));
        Assert.True((await f.Service.ResolvePotentialFineAsync(row.Id, new("Invoiced", invoice.Id, "no-show-3", "verified", row.RowVersion), ct)).IsFailure);
        Assert.True((await f.Service.ResolvePotentialFineAsync(row.Id, new("Invoiced", wrongRiderInvoice.Id, "no-show-3", "wrong rider", row.RowVersion), ct)).IsFailure);
        invoice.Status = "Matched"; f.Db.Update(invoice); await f.Db.SaveChangesAsync(ct);
        Assert.True((await f.Service.ResolvePotentialFineAsync(row.Id, new("Invoiced", invoice.Id, "missing", "verified", row.RowVersion), ct)).IsFailure);
        Assert.True((await f.Service.ResolvePotentialFineAsync(row.Id, new("Invoiced", invoice.Id, "no-show-3", "verified", row.RowVersion), ct)).IsSuccess);
        Assert.Equal("Invoiced", Assert.Single(Value(await f.Service.PotentialFinesAsync(new(2026, 11, 1), new(2026, 11, 3), ct))).Status);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
    }

    [Fact]
    public async Task MonthlyVehicleAndSponsorDimensionsFilterBeforePagingAndExportAllResults()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; var rider = await Assign(f);
        var sponsor = Guid.NewGuid(); f.Account.SponsorId = sponsor; f.Db.Update(f.Account);
        var vehicle = new Vehicle { PlateNumberAr = "PERF", OwnershipType = VehicleOwnershipType.Owned };
        f.Db.AddRange(vehicle, new RiderVehicleAssignment { VehicleId = vehicle.Id, RiderProfileId = rider.Id, StartedAtUtc = At(1), EndedAtUtc = At(2), Status = RiderVehicleAssignmentStatus.Completed },
            Operation(f.Account.Id, new(2026, 11, 1), 8), Operation(f.Account.Id, new(2026, 11, 2), 10));
        await f.Db.SaveChangesAsync(ct);
        var query = new HungerPerformanceQuery(new(2026, 11, 1), new(2026, 11, 2), SponsorId: sponsor, ActualVehicle: "Mixed", PageSize: 1);
        var row = Assert.Single(Value(await f.Service.PerformanceAsync(query, ct)).Items);
        Assert.Equal("Mixed", row.ActualVehicle); Assert.Equal(sponsor.ToString(), row.Sponsorship);
        Assert.Empty(Value(await f.Service.PerformanceAsync(query with { ActualVehicle = "Company" }, ct)).Items);
        var file = Value(await f.Service.ExportPerformanceAsync(query with { Period = "day", ActualVehicle = null }, ct));
        using var book = new XLWorkbook(new MemoryStream(file.Content));
        Assert.Equal(3, book.Worksheet("Performance").RowsUsed().Count());
        f.Checker.Allowed = false;
        Assert.True((await f.Service.PerformanceAsync(query, ct)).IsFailure); Assert.True((await f.Service.ExportPerformanceAsync(query, ct)).IsFailure);
    }

    [Fact]
    public async Task CatalogVersionsAndCityOverridesRetainHistoricalBehavior()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.AddAlertRuleAsync(new(f.Account.ClientPlatformId, "HoursCritical", 8, "High", new(2026, 11, 1), true, "v1"), ct));
        Value(await f.Service.AddAlertRuleAsync(new(f.Account.ClientPlatformId, "HoursCritical", 8, "High", new(2026, 11, 2), false, "city exemption") { OperatingCityId = f.Account.OperatingCityId }, ct));
        f.Db.AddRange(Operation(f.Account.Id, new(2026, 11, 1), 7), Operation(f.Account.Id, new(2026, 11, 2), 7)); await f.Db.SaveChangesAsync(ct);
        var report = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 2), Period: "day"), ct));
        Assert.Contains(report.Items[0].Alerts, a => a.StartsWith("HoursCritical:", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Items[1].Alerts, a => a.StartsWith("HoursCritical:", StringComparison.Ordinal));
        Assert.Equal(2, Value(await f.Service.AlertRulesAsync(ct)).Count);
    }

    [Fact]
    public async Task ObservedWorkUsesBusinessDateRatherThanImportCreationOrAssignmentPresence()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await Assign(f);
        Assert.Equal("Unknown", (await Item(f)).MonthlyUsageStatus);
        f.Db.Add(Operation(f.Account.Id, new(2026, 10, 31), 8)); await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Unknown", (await Item(f)).MonthlyUsageStatus);
        f.Db.Add(Operation(f.Account.Id, new(2026, 11, 1), 0)); await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Unknown", (await Item(f)).MonthlyUsageStatus);
        f.Db.Add(Operation(f.Account.Id, new(2026, 11, 2), 1)); await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Used", (await Item(f)).MonthlyUsageStatus);
    }

    [Theory]
    [InlineData("08:30", "Duration", 8.5)]
    [InlineData("08:30:00", "Duration", 8.5)]
    [InlineData("8.5", "DecimalHours", 8.5)]
    [InlineData("0.5", "FractionOfDay", 12)]
    [InlineData("0.3541666666666667", "FractionOfDay", 8.5)]
    [InlineData("24:00", "Duration", 24)]
    public void HoursUnitsAreExplicit(string value, string unit, decimal expected) => Assert.Equal(expected, HungerPerformanceRules.Hours(value, unit));

    [Theory]
    [InlineData("25", "DecimalHours")]
    [InlineData("08:30", "DecimalHours")]
    [InlineData("8.5", "unknown")]
    [InlineData("2", "FractionOfDay")]
    public void InvalidHoursAreRejected(string value, string unit) => Assert.Throws<FormatException>(() => HungerPerformanceRules.Hours(value, unit));

    [Fact]
    public void ForecastHandlesZeroElapsedZeroRemainingAndGaps()
    {
        Assert.Equal((null, (decimal?)5), HungerPerformanceRules.Forecast(0, 10, 0, 2, true));
        Assert.Equal(((decimal?)20, null), HungerPerformanceRules.Forecast(20, 10, 2, 0, true));
        Assert.Equal(((decimal?)60, (decimal?)10), HungerPerformanceRules.Forecast(20, 40, 1, 2, true));
        Assert.Equal((null, (decimal?)null), HungerPerformanceRules.Forecast(20, 40, 1, 2, false));
    }

    private static byte[] OperationsFile(string? hours, string unit = "DecimalHours", int noShow = 0)
    {
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Operations");
        string[] headers = ["Captain ID", "CompletedOrders", "ActualWorkingHours", "RejectedOrders", "CancelledOrders", "NoShowCount", "HoursUnit", "AcceptedOrders", "StackedOrders"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Cell(2, 1).Value = "test-account"; sheet.Cell(2, 2).Value = 20;
        if (hours != null) sheet.Cell(2, 3).Value = hours;
        sheet.Cell(2, 4).Value = 2; sheet.Cell(2, 5).Value = 1; sheet.Cell(2, 6).Value = noShow;
        sheet.Cell(2, 7).Value = unit; sheet.Cell(2, 8).Value = 25; sheet.Cell(2, 9).Value = 4;
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }

    [Fact]
    public async Task DurationImportOptionalMetricsAndNoShowAreIdempotentAndDoNotCreateDebt()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var preview = Value(await f.Service.PreviewOperationsAsync("ops.xlsx", OperationsFile("08:30:00", "Duration", 3), new(2026, 11, 1), ct));
        Assert.True(preview.CanConfirm);
        Value(await f.Service.ConfirmOperationsAsync(preview.PreviewId, ct)); Value(await f.Service.ConfirmOperationsAsync(preview.PreviewId, ct));
        var operation = Assert.Single(await f.Db.Set<HungerDailyOperation>().ToArrayAsync(ct));
        Assert.Equal(8.5m, operation.ActualWorkingHours); Assert.Equal(25, operation.AcceptedOrders); Assert.Equal(4, operation.StackedOrders);
        var export = Value(await f.Service.ExportOperationsAsync(new(2026, 11, 1), new(2026, 11, 1), null, ct));
        using var book = new XLWorkbook(new MemoryStream(export.Content)); var sheet = book.Worksheet("Operations");
        Assert.Equal("AcceptedOrders", sheet.Cell(1, 13).GetString()); Assert.Equal("StackedOrders", sheet.Cell(1, 14).GetString()); Assert.Equal("HoursUnit", sheet.Cell(1, 15).GetString());
        Assert.Equal(25, sheet.Cell(2, 13).GetValue<int>()); Assert.Equal(4, sheet.Cell(2, 14).GetValue<int>()); Assert.Equal("Duration", sheet.Cell(2, 15).GetString());
        var fine = Assert.Single(Value(await f.Service.PotentialFinesAsync(new(2026, 11, 1), new(2026, 11, 2), ct)));
        Assert.Equal("Potential", fine.Status); Assert.Equal(3, fine.NoShowCount);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
        Assert.True((await f.Service.ResolvePotentialFineAsync(fine.Id, new("Invoiced", Guid.NewGuid(), "wrong", "evidence", fine.RowVersion), ct)).IsFailure);
        Assert.True((await f.Service.ResolvePotentialFineAsync(fine.Id, new("Waived", null, null, "approved waiver", fine.RowVersion), ct)).IsSuccess);
        Assert.True((await f.Service.ResolvePotentialFineAsync(fine.Id, new("Invoiced", Guid.NewGuid(), "wrong", "evidence", fine.RowVersion), ct)).IsFailure);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
    }

    [Fact]
    public async Task MissingHoursStayUnknownAndManualComparisonCanBeCleared()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var preview = Value(await f.Service.PreviewOperationsAsync("ops.xlsx", OperationsFile(null), new(2026, 11, 1), ct));
        Value(await f.Service.ConfirmOperationsAsync(preview.PreviewId, ct));
        var row = Assert.Single(Value(await f.Service.OperationsAsync(new(2026, 11, 1), new(2026, 11, 1), null, null, 1, 50, ct)).Items);
        Assert.Null(row.ActualWorkingHours); Assert.Contains("MissingHoursCoverage", row.Alerts); Assert.DoesNotContain("HoursCritical", row.Alerts);
        foreach (var manual in new int?[] { 18, 22, null })
        {
            Assert.True((await f.Service.ManualPerformanceAsync(row.Id, new(manual, null, "comparison", row.RowVersion), ct)).IsSuccess);
            row = Assert.Single(Value(await f.Service.OperationsAsync(new(2026, 11, 1), new(2026, 11, 1), null, null, 1, 50, ct)).Items);
            Assert.Equal(manual.HasValue ? 20 - manual : null, row.OrdersDifference);
        }
    }

    private static HungerDailyOperation Operation(Guid account, DateOnly day, decimal? hours, params PlatformDayShare[] shares) => new()
    {
        PlatformRiderAccountId = account, ReportDate = day, ImportId = Guid.NewGuid(), CompletedOrders = 20, ActualWorkingHours = hours,
        AllocationsJson = JsonSerializer.Serialize(shares, PerformanceTestJson)
    };
    private static PlatformDayShare Share(Guid rider, decimal orders, decimal hours) => new(Guid.NewGuid(), rider, 1,
        new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = orders, [PlatformDayMetric.WorkingHours] = hours });

    [Fact]
    public async Task RiderGroupingUsesSharesAndDistinctDaysWithoutDuplicatingAccountTotals()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        f.Db.AddRange(new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id, RiderProfileId = first, Status = RiderAssignmentStatus.Active, EffectiveFrom = new(2026, 11, 1), EffectiveTo = new(2026, 11, 2) },
            new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id, RiderProfileId = second, Status = RiderAssignmentStatus.Active, EffectiveFrom = new(2026, 11, 1), EffectiveTo = new(2026, 11, 2) },
            Operation(f.Account.Id, new(2026, 11, 1), 8, Share(first, 8, 3), Share(second, 12, 5)));
        await f.Db.SaveChangesAsync(ct);
        var account = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 2)), ct));
        var riders = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 2), GroupBy: "rider"), ct));
        Assert.Equal(20, account.CompletedOrders); Assert.Equal(20, riders.CompletedOrders); Assert.Equal(8, riders.WorkingHours);
        Assert.Equal(1, account.DistinctAccounts); Assert.Equal(2, account.DistinctRiders);
        Assert.Equal(1, Assert.Single(account.Items).QualifiedDays); Assert.All(riders.Items, r => { Assert.Equal(1, r.WorkedDays); Assert.Equal(0, r.QualifiedDays); Assert.Equal(1, r.UnknownDays); Assert.Equal(0, r.RemainingDays); });
        var horizontal = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 2), Layout: "horizontal", PageSize: 1), ct));
        Assert.Equal(account.CompletedOrders, horizontal.CompletedOrders);
        var daily = Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 2), Period: "day", PageSize: 1), ct));
        Assert.Equal(2, daily.TotalRecords); Assert.Equal(20, daily.CompletedOrders);
    }

    [Fact]
    public async Task PlannedAssignmentsExcludedAndMissingMandatoryDayIsUnknown()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        f.Db.AddRange(new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id, RiderProfileId = Guid.NewGuid(), Status = RiderAssignmentStatus.Planned, EffectiveFrom = new(2026, 10, 1) },
            new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id, RiderProfileId = Guid.NewGuid(), Status = RiderAssignmentStatus.Ended, EffectiveFrom = new(2026, 10, 29), EffectiveTo = new(2026, 10, 31) });
        await f.Db.SaveChangesAsync(ct);
        var report = Value(await f.Service.PerformanceAsync(new(new(2026, 10, 29), new(2026, 10, 31)), ct));
        Assert.Equal(1, report.DistinctRiders); var row = Assert.Single(report.Items);
        Assert.Equal(3, row.UnknownDays); Assert.Equal(0, row.WorkedDays); Assert.Equal(3, row.Alerts.Count(a => a.StartsWith("MandatoryDayCoverageUnknown", StringComparison.Ordinal)));
        Assert.DoesNotContain(row.Alerts, a => a.StartsWith("MandatoryDayNotWorked", StringComparison.Ordinal));
        var summary = Value(await f.Service.SummaryAsync(new(2026, 10, 1), ct)); Assert.Equal(1, summary.ActualRiders);
    }

    [Fact]
    public async Task CatalogHonorsCityEffectiveDatesAndTargetsAndSuppressesForecastOnGaps()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.AddAlertRuleAsync(new(f.Account.ClientPlatformId, "LowOrders", 30, "Warning", new(2026, 11, 1), true, "contract")
            { Metric = "CompletedOrders", OperatingCityId = f.Account.OperatingCityId, EffectiveThrough = new(2026, 11, 1) }, ct));
        Value(await f.Service.AddAlertRuleAsync(new(f.Account.ClientPlatformId, "OrderTarget", 300, "Info", new(2026, 11, 1), true, "contract")
            { Metric = "CompletedOrders", Kind = "Target", Period = "Month", Operator = "GreaterThanOrEqual" }, ct));
        f.Db.AddRange(new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id, RiderProfileId = Guid.NewGuid(), Status = RiderAssignmentStatus.Active, EffectiveFrom = new(2026, 11, 1) },
            Operation(f.Account.Id, new(2026, 11, 1), 8), Operation(f.Account.Id, new(2026, 11, 2), 10));
        await f.Db.SaveChangesAsync(ct);
        var row = Assert.Single(Value(await f.Service.PerformanceAsync(new(new(2026, 11, 1), new(2026, 11, 30)), ct)).Items);
        Assert.Equal(300, row.Target); Assert.Equal(260, row.Deficit); Assert.Null(row.Forecast);
        Assert.Single(row.Catalog, e => e.Code == "LowOrders"); Assert.Contains(row.Alerts, a => a.StartsWith("LowOrders:", StringComparison.Ordinal));
        Assert.DoesNotContain(row.Catalog, e => e.Code == "LowOrders" && e.Period == new DateOnly(2026, 11, 2));
    }

    [Fact]
    public async Task ThresholdsCompareUnroundedHours()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        f.Db.AddRange(Operation(f.Account.Id, new(2026, 11, 1), 8 - 1m / 3600), Operation(f.Account.Id, new(2026, 11, 2), 8), Operation(f.Account.Id, new(2026, 11, 3), 10));
        await f.Db.SaveChangesAsync(ct);
        var rows = Value(await f.Service.OperationsAsync(new(2026, 11, 1), new(2026, 11, 3), null, null, 1, 50, ct)).Items;
        Assert.Contains("HoursCritical", rows[0].Alerts); Assert.Equal("High", Assert.Single(rows[0].AlertDetails).Severity);
        Assert.Contains("HoursFollowUp", rows[1].Alerts); Assert.Empty(rows[2].Alerts);
    }
}
