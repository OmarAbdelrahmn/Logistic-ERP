using System.Text.Json;
using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaEvaluationService
{
    public async Task<Result<NinjaEvaluationFile>> ExportAsync(NinjaEvaluationQuery query, CancellationToken ct = default)
    {
        if ((await Platforms(PermissionKeys.Ninja.EvaluationExport, ct)).Length == 0) return Result.Failure<NinjaEvaluationFile>(NinjaErrors.Forbidden);
        var result = await Report(query, true, ct);
        if (result.IsFailure) return Result.Failure<NinjaEvaluationFile>(result.Error);
        var report = result.Value!;
        using var book = new XLWorkbook(); var summary = book.AddWorksheet("Suggestions"); var daily = book.AddWorksheet("Daily");
        string[] headers = ["Key", "Rider", "Captain IDs", "Observed orders", "Observed hours", "Evaluated orders", "Cutoff", "Target", "Gap", "Order suggestion", "Hours-adjusted suggestion", "Projected orders", "Projected suggestion", "Attendance", "Reasons"];
        for (var i = 0; i < headers.Length; i++) summary.Cell(1, i + 1).Value = headers[i];
        string[] dailyHeaders = ["Key", "Date", "Account ID", "Captain ID", "Assignment ID", "Rider ID", "Pricing", "Responsibility", "Orders", "Hours", "Target", "Gap", "Hours status", "Coverage", "Source ID", "Import ID", "Source row", "Origin"];
        for (var i = 0; i < dailyHeaders.Length; i++) daily.Cell(1, i + 1).Value = dailyHeaders[i];
        var rowNumber = 2; var dayNumber = 2;
        foreach (var row in report.Items)
        {
            ct.ThrowIfCancellationRequested();
            // XLCell.Value writes text values, never FormulaA1, including untrusted names beginning with '='.
            summary.Cell(rowNumber, 1).Value = row.Key; summary.Cell(rowNumber, 2).Value = row.RiderName ?? string.Empty;
            summary.Cell(rowNumber, 3).Value = string.Join(", ", row.CaptainIds);
            summary.Cell(rowNumber, 4).Value = row.Observed.Orders; summary.Cell(rowNumber, 5).Value = row.Observed.Hours;
            summary.Cell(rowNumber, 6).Value = row.Evaluated.Orders; summary.Cell(rowNumber, 7).Value = row.LatestCompleteDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
            Set(summary.Cell(rowNumber, 8), row.Evaluated.Target); Set(summary.Cell(rowNumber, 9), row.Evaluated.Gap);
            Set(summary.Cell(rowNumber, 10), row.SuggestedMoney?.OrderAmount); Set(summary.Cell(rowNumber, 11), row.SuggestedMoney?.SuggestedAmount);
            Set(summary.Cell(rowNumber, 12), row.Forecast?.ProjectedOrders); Set(summary.Cell(rowNumber, 13), row.Forecast?.SuggestedMoney?.SuggestedAmount);
            summary.Cell(rowNumber, 14).Value = row.Attendance.Status; summary.Cell(rowNumber++, 15).Value = string.Join("; ", row.Reasons);
            foreach (var day in row.Days)
            {
                daily.Cell(dayNumber, 1).Value = row.Key; daily.Cell(dayNumber, 2).Value = day.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                daily.Cell(dayNumber, 3).Value = day.AccountId.ToString(); daily.Cell(dayNumber, 4).Value = day.CaptainId;
                daily.Cell(dayNumber, 5).Value = day.AssignmentId?.ToString() ?? string.Empty; daily.Cell(dayNumber, 6).Value = day.RiderProfileId?.ToString() ?? string.Empty;
                daily.Cell(dayNumber, 7).Value = day.PricingType ?? "Unknown"; Set(daily.Cell(dayNumber, 8), day.Responsibility);
                Set(daily.Cell(dayNumber, 9), day.Orders); Set(daily.Cell(dayNumber, 10), day.Hours); Set(daily.Cell(dayNumber, 11), day.Target); Set(daily.Cell(dayNumber, 12), day.Gap);
                daily.Cell(dayNumber, 13).Value = day.HoursStatus; daily.Cell(dayNumber, 14).Value = day.Coverage;
                daily.Cell(dayNumber, 15).Value = day.SourceId?.ToString() ?? string.Empty; daily.Cell(dayNumber, 16).Value = day.ImportId?.ToString() ?? string.Empty;
                Set(daily.Cell(dayNumber, 17), day.SourceRowNumber); daily.Cell(dayNumber++, 18).Value = day.AllocationOrigin;
            }
        }
        var metadata = book.AddWorksheet("Metadata"); metadata.Cell(1, 1).Value = "SUGGESTIONS ONLY — Awaiting Ninja monthly invoice";
        metadata.Cell(2, 1).Value = "Policy"; metadata.Cell(2, 2).Value = report.PolicyVersion;
        metadata.Cell(3, 1).Value = "Input revision"; metadata.Cell(3, 2).Value = report.InputRevision;
        metadata.Cell(4, 1).Value = "Query"; metadata.Cell(4, 2).Value = JsonSerializer.Serialize(query, Json);
        metadata.Cell(5, 1).Value = "Summary"; metadata.Cell(5, 2).Value = JsonSerializer.Serialize(report.Summary, Json);
        metadata.Cell(6, 1).Value = "Computed UTC"; metadata.Cell(6, 2).Value = report.ComputedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        metadata.Cell(7, 1).Value = "Basis"; metadata.Cell(7, 2).Value = "Completed orders and working hours only. Acceptance, timing, uniform and leave compliance unknown.";
        foreach (var sheet in book.Worksheets) { sheet.Row(1).Style.Font.Bold = true; sheet.SheetView.FreezeRows(1); }
        summary.Columns(1, 15).Width = 22; daily.Columns(1, 18).Width = 20; metadata.Column(1).Width = 24; metadata.Column(2).Width = 90;
        using var stream = new MemoryStream(); book.SaveAs(stream);
        return Result.Success(new NinjaEvaluationFile(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Ninja-suggestions-{query.Year:D4}-{query.Month:D2}-{query.View}.xlsx"));
    }
    private static void Set(IXLCell cell, decimal? value) { if (value.HasValue) cell.Value = value.Value; else cell.Value = "Unknown"; }
}
