using System.Globalization;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class AccidentPdfGenerator : IAccidentPdfGenerator
{
    private readonly PdfGenerationOptions options;

    public AccidentPdfGenerator(IOptions<PdfGenerationOptions> configured, IHostEnvironment environment)
    {
        options = configured.Value;
        QuestPDF.Settings.License = options.QuestPdfLicense.ToLowerInvariant() switch
        {
            "community" => LicenseType.Community,
            "professional" => LicenseType.Professional,
            "enterprise" => LicenseType.Enterprise,
            _ => throw new InvalidOperationException("PdfGeneration:QuestPdfLicense must be Community, Professional, or Enterprise.")
        };
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = true;
        QuestPDF.Settings.UseEnvironmentFonts = true;
        if (!string.IsNullOrWhiteSpace(options.ArabicFontPath))
        {
            var path = Path.IsPathRooted(options.ArabicFontPath)
                ? options.ArabicFontPath
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.ArabicFontPath));
            if (!File.Exists(path)) throw new InvalidOperationException($"Configured Arabic PDF font was not found: {path}");
            using var stream = File.OpenRead(path);
            FontManager.RegisterFontWithCustomName(options.ArabicFontFamily, stream);
        }
    }

    public byte[] Generate(AccidentPdfSnapshot snapshot) => Document.Create(document =>
    {
        document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.PageColor(Colors.White);
            page.ContentFromRightToLeft();
            page.DefaultTextStyle(style => style.FontFamily(options.ArabicFontFamily).FontSize(10).FontColor(Colors.Grey.Darken3));
            page.Header().Element(container => Header(container, snapshot));
            page.Content().PaddingVertical(16).Column(column =>
            {
                column.Spacing(12);
                column.Item().Element(container => Summary(container, snapshot));
                column.Item().Element(container => Details(container, snapshot));
                column.Item().Element(container => Narrative(container, snapshot));
                column.Item().Element(container => Evidence(container, snapshot));
            });
            page.Footer().AlignCenter().DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Medium)).Text(text =>
            {
                text.Span("صفحة "); text.CurrentPageNumber(); text.Span(" من "); text.TotalPages();
                text.Span("  |  Page "); text.CurrentPageNumber(); text.Span(" of "); text.TotalPages();
            });
        });
    }).GeneratePdf();

    private static void Header(IContainer container, AccidentPdfSnapshot snapshot)
    {
        container.BorderBottom(2).BorderColor(Colors.Blue.Darken2).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("البوابة للخدمات اللوجستية").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                column.Item().ContentFromLeftToRight().Text("Al Bawaba Logistics - Vehicle Accident Report").FontSize(9).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(180).AlignLeft().Column(column =>
            {
                column.Item().Text("تقرير حادث مركبة").FontSize(15).Bold();
                column.Item().ContentFromLeftToRight().Text(snapshot.ReportNumber).FontSize(10).SemiBold();
            });
        });
    }

    private static void Summary(IContainer container, AccidentPdfSnapshot snapshot)
    {
        container.Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten2).Padding(12).Table(table =>
        {
            table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); });
            Cell(table, "رقم الحادث / Accident", snapshot.AccidentNumber);
            Cell(table, "التاريخ والوقت / Date & time", snapshot.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
            Cell(table, "الرايدر / Rider", $"{snapshot.RiderNameAr} / {snapshot.RiderNameEn}");
            Cell(table, "رقم الإقامة / Iqama", snapshot.IqamaNo ?? "-");
            Cell(table, "المركبة / Vehicle", snapshot.AssetNumber);
            Cell(table, "اللوحة / Plate", $"{snapshot.PlateNumberAr} / {snapshot.PlateNumberEn}");
            Cell(table, "الموقع / Location", snapshot.LocationDescription);
            Cell(table, "الخطورة / Severity", snapshot.Severity.ToString());
        });
    }

    private static void Details(IContainer container, AccidentPdfSnapshot snapshot)
    {
        container.Column(column =>
        {
            column.Item().Text("بيانات الحادث / Accident details").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); });
                Cell(table, "صالحة للقيادة / Drivable", snapshot.IsDrivable ? "نعم / Yes" : "لا / No");
                Cell(table, "إصابات / Injuries", snapshot.HasInjuries ? "نعم / Yes" : "لا / No");
                Cell(table, "رقم تقرير الشرطة / Police report", snapshot.PoliceReportNumber ?? "-");
                Cell(table, "مطالبة التأمين / Insurance claim", snapshot.InsuranceClaimNumber ?? "-");
                Cell(table, "شركة التأمين / Insurer", snapshot.InsuranceProvider ?? "-");
                Cell(table, "وثيقة التأمين / Policy", snapshot.InsurancePolicyNumber ?? "-");
                Cell(table, "تقييم الخطأ / Fault assessment", snapshot.FaultAssessment ?? "-");
                Cell(table, "الأطراف الأخرى / Third parties", snapshot.ThirdPartyDetails ?? "-");
            });
        });
    }

    private static void Narrative(IContainer container, AccidentPdfSnapshot snapshot)
    {
        container.Column(column =>
        {
            Section(column, "وصف الضرر / Damage description", snapshot.DamageDescription);
            Section(column, "تفاصيل الإصابات / Injury details", snapshot.InjuryDetails ?? "-");
            Section(column, "وصف الحادث / Narrative", snapshot.Narrative);
        });
    }

    private static void Evidence(IContainer container, AccidentPdfSnapshot snapshot)
    {
        container.Column(column =>
        {
            column.Item().Text("المرفقات / Evidence").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
            if (snapshot.Evidence.Count == 0)
            {
                column.Item().PaddingTop(5).Text("لا توجد مرفقات / No evidence attached.").Italic();
                return;
            }
            foreach (var evidence in snapshot.Evidence)
            {
                column.Item().PaddingTop(8).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(item =>
                {
                    item.Item().ContentFromLeftToRight().Text($"{evidence.OriginalFileName}  |  {evidence.ContentType}").SemiBold();
                    item.Item().ContentFromLeftToRight().Text($"SHA-256: {evidence.Sha256Checksum}").FontSize(7);
                    if (evidence.ImageBytes is { Length: > 0 }) item.Item().PaddingTop(6).MaxHeight(220).Image(evidence.ImageBytes).FitArea();
                });
            }
            column.Item().PaddingTop(14).ContentFromLeftToRight().Text($"Generated UTC: {snapshot.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}").FontSize(7);
        });
    }

    private static void Cell(TableDescriptor table, string label, string value)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(column =>
        {
            column.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
            column.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).SemiBold();
        });
    }

    private static void Section(ColumnDescriptor column, string title, string value)
    {
        column.Item().PaddingTop(8).Text(title).SemiBold().FontColor(Colors.Blue.Darken2);
        column.Item().PaddingTop(3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(value);
    }
}
