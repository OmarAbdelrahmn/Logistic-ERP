using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Infrastructure.Fleet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezTreasuryReportRenderer : IJahezTreasuryReportRenderer
{
    [GeneratedRegex(@"(?i)\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b")]
    private static partial Regex InternalIdentifier();

    private static string DisplayText(object? value) => InternalIdentifier().Replace(value?.ToString() ?? "-", "مرجع داخلي");
    private static string UserName(string? name) => string.IsNullOrWhiteSpace(name) || InternalIdentifier().IsMatch(name)
        ? "اسم المستخدم غير متاح" : name;
    private readonly string font;

    public JahezTreasuryReportRenderer(IOptions<PdfGenerationOptions> options, IHostEnvironment environment)
    {
        var settings = options.Value;
        QuestPDF.Settings.License = settings.QuestPdfLicense.ToLowerInvariant() switch
        {
            "community" => LicenseType.Community, "professional" => LicenseType.Professional,
            "enterprise" => LicenseType.Enterprise, _ => throw new InvalidOperationException("Configure PdfGeneration:QuestPdfLicense.")
        };
        font = settings.ArabicFontFamily;
        if (!string.IsNullOrWhiteSpace(settings.ArabicFontPath))
        {
            var path = Path.IsPathRooted(settings.ArabicFontPath) ? settings.ArabicFontPath : Path.Combine(environment.ContentRootPath, settings.ArabicFontPath);
            using var stream = File.OpenRead(path);
            FontManager.RegisterFontWithCustomName(font, stream);
        }
    }

    private sealed record ReportTable(string Name, string[] Headers, IReadOnlyList<object?[]> Rows);

    private static List<ReportTable> Tables(TreasuryReport report)
    {
        var tables = new List<ReportTable>();
        if (report.Accounts.Count > 0)
            tables.Add(new("الأرصدة", ["كود الحساب", "اسم الحساب", "حساب ترحيل", "أول المدة", "المقبوضات", "المدفوعات", "تحويل وارد", "تحويل صادر", "آخر المدة"],
                report.Accounts.Select(a => new object?[] { a.Code, a.NameAr, a.IsPosting ? "نعم" : "تجميعي", a.Opening, a.Receipts, a.Payments, a.TransfersIn, a.TransfersOut, a.Closing }).ToArray()));
        if (report.Movements.Count > 0)
            tables.Add(new("الحركات", ["التاريخ", "الحساب", "الكفيل", "نوع القيد", "المبلغ", "المرجع", "المندوب", "الإقامة", "منفذ الحركة"],
                report.Movements.Select(m => new object?[] { m.Posting.OccurredAtUtc.ToOffset(TimeSpan.FromHours(3)).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    m.AccountNameAr, m.SponsorNameAr, m.Posting.Kind switch
                    { LogisticsERP.Domain.Entities.Jahez.TreasuryEntryKind.Business => "قبض / صرف", LogisticsERP.Domain.Entities.Jahez.TreasuryEntryKind.Transfer => "تحويل داخلي",
                        LogisticsERP.Domain.Entities.Jahez.TreasuryEntryKind.Opening => "رصيد افتتاحي", LogisticsERP.Domain.Entities.Jahez.TreasuryEntryKind.Adjustment => "تسوية", _ => "عكس قيد" }, m.Posting.Amount, m.Posting.Reference,
                    m.DriverNameAr, m.IqamaNo, UserName(m.Posting.ActorNameAr) }).ToArray()));
        if (report.Fuel.Count > 0)
            tables.Add(new("بترو آب", ["الكفيل", "التحصيل", "التمويل المثبت", "التحصيل المخصص", "غير المخصص", "فرق التمويل", "رصيد البداية", "رصيد النهاية", "الاستهلاك المستورد", "الحالة"],
                report.Fuel.Select(f => new object?[] { f.SponsorNameAr, f.Collections, (object?)f.Funding ?? "غير متحقق", (object?)f.AllocatedCollections ?? "غير متحقق", (object?)f.UnallocatedCollections ?? "غير متحقق",
                    (object?)f.FundingDifference ?? "غير متحقق", (object?)f.OpeningStatement ?? "غير متحقق", (object?)f.ClosingStatement ?? "غير متحقق", (object?)f.ImportedConsumption ?? "غير متحقق",
                    f.Status switch { "Matched" => "متطابق", "Difference" => "فرق", _ => "غير متحقق" } }).ToArray()));
        if (report.Handover is { } h)
        {
            tables.Add(new("محضر التسليم", ["البيان", "القيمة"], new object?[][]
            {
                ["رقم المحضر", h.Handover.DocumentNumber ?? "محضر سابق بدون رقم مستند"],
                ["اليوم المحاسبي", h.Handover.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)],
                ["المشرف المسلم", UserName(h.Handover.RequestedByNameAr)], ["المحاسب المستلم", UserName(h.Handover.AccountantNameAr)],
                ["وقت التأكيد", h.Handover.ConfirmedAtUtc?.ToOffset(TimeSpan.FromHours(3)).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)],
                ["النقدية المستلمة", h.Handover.CashboxAmount], ["حركة البنك - غير نقدية", h.Handover.BankAmount],
                ["حركة بترو آب - غير نقدية", h.Handover.PetroAppAmount], ["الحالة", "مؤكد؛ أغلقت الحركات المدرجة فقط"],
                ["سبب التأكيد", h.Handover.ConfirmationReason]
            }));
            tables.Add(new("تفاصيل التسليم", ["الكفيل", "رقم الكفيل", "الحساب", "المبلغ", "عدد الحركات"],
                h.Lines.Select(l => new object?[] { l.SponsorNameAr, l.SponsorIdentifier, l.AccountNameAr, l.Amount, l.MovementCount }).ToArray()));
        }
        return tables;
    }

    public TreasuryExport Render(TreasuryReport report, string format)
    {
        var tables = Tables(report);
        var name = $"jahez-{report.ReportType}-{report.Filter.FromDate:yyyyMMdd}-{report.Filter.ToDate:yyyyMMdd}";
        if (format == "xlsx")
        {
            using var workbook = new XLWorkbook();
            foreach (var table in tables)
            {
                var sheet = workbook.AddWorksheet(table.Name); sheet.RightToLeft = true;
                for (var column = 0; column < table.Headers.Length; column++) sheet.Cell(1, column + 1).Value = table.Headers[column];
                for (var row = 0; row < table.Rows.Count; row++)
                    for (var column = 0; column < table.Headers.Length; column++)
                    {
                        var value = table.Rows[row][column];
                        var cell = sheet.Cell(row + 2, column + 1);
                        if (value is decimal money) { cell.Value = money; cell.Style.NumberFormat.Format = "#,##0.00"; }
                        else if (value is int count) cell.Value = count;
                        else cell.Value = DisplayText(value);
                    }
                sheet.Row(1).Style.Font.Bold = true; sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#163D49");
                sheet.Row(1).Style.Font.FontColor = XLColor.White; sheet.SheetView.FreezeRows(1);
                sheet.Columns().AdjustToContents(10, 42);
                sheet.Range(1, 1, Math.Max(1, table.Rows.Count + 1), table.Headers.Length).SetAutoFilter();
            }
            if (workbook.Worksheets.Count == 0) workbook.AddWorksheet("لا توجد بيانات").Cell(1, 1).Value = "لا توجد بيانات للفترة المحددة";
            using var stream = new MemoryStream(); workbook.SaveAs(stream);
            return new(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name + ".xlsx");
        }
        var pdf = Document.Create(document =>
        {
            foreach (var table in tables.Count == 0 ? [new ReportTable("لا توجد بيانات", ["البيان"], [])] : tables)
                document.Page(page =>
                {
                    page.Size(table.Headers.Length > 5 ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.Margin(24); page.ContentFromRightToLeft();
                    page.DefaultTextStyle(x => x.FontFamily(font).FontSize(8));
                    page.Header().Column(c =>
                    {
                        c.Item().Text("جاهز — التقارير المالية للخزائن").FontSize(16).Bold().FontColor("#163D49");
                        c.Item().Text($"{table.Name} | {report.Filter.FromDate:yyyy-MM-dd} — {report.Filter.ToDate:yyyy-MM-dd}");
                        c.Item().Text("المبالغ بالريال السعودي • تقرير حركة وأرصدة نقدية").FontSize(8);
                    });
                    page.Content().PaddingVertical(12).Table(t =>
                    {
                        t.ColumnsDefinition(columns => { foreach (var unused in table.Headers) columns.RelativeColumn(); });
                        t.Header(header => { foreach (var label in table.Headers) header.Cell().Background("#163D49").Padding(4).Text(label).FontColor(Colors.White).Bold(); });
                        foreach (var row in table.Rows)
                            foreach (var value in row)
                            {
                                var cell = t.Cell().BorderBottom(0.4f).BorderColor("#DCE5E8").Padding(4);
                                var label = value is decimal amount ? amount.ToString("N2", CultureInfo.InvariantCulture) : DisplayText(value);
                                if (label.All(c => c < 128)) cell = cell.ContentFromLeftToRight();
                                cell.Text(label);
                            }
                    });
                    page.Footer().AlignCenter().ContentFromLeftToRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
                });
        }).GeneratePdf();
        return new(pdf, "application/pdf", name + ".pdf");
    }
}
