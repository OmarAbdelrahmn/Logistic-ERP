using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "vehicle-accident-report.pdf"));
var environment = new SmokeEnvironment { ContentRootPath = Directory.GetCurrentDirectory() };
var generator = new AccidentPdfGenerator(
    Options.Create(new PdfGenerationOptions { QuestPdfLicense = "Community", ArabicFontFamily = "Arial" }),
    environment);
var snapshot = new AccidentPdfSnapshot(
    "ACC-20260822-0001-V1",
    "ACC-20260822-0001",
    new DateTimeOffset(2026, 8, 22, 8, 30, 0, TimeSpan.Zero),
    "محمد أحمد",
    "Mohammed Ahmed",
    "EMP-1001",
    "VEH-1001",
    "أ ب ج ١٢٣٤",
    "ABC 1234",
    "الرياض، المملكة العربية السعودية / Riyadh, Saudi Arabia",
    VehicleAccidentSeverity.Moderate,
    false,
    false,
    null,
    "لا يوجد / None",
    "تلف في الصدام الأمامي والمصباح الأيسر / Front bumper and left headlight damage",
    "قيد المراجعة / Under review",
    "وقع الحادث أثناء قيادة الرايدر للمركبة في الرياض. / The accident occurred while the rider was driving in Riyadh.",
    "POL-12345",
    "CLM-98765",
    "شركة التأمين النموذجية / Example Insurance",
    "INS-2026-789",
    new DateTimeOffset(2026, 8, 22, 9, 0, 0, TimeSpan.Zero),
    [new AccidentPdfEvidence("police-report.pdf", "application/pdf", new string('A', 64), null)]);

await File.WriteAllBytesAsync(outputPath, generator.Generate(snapshot));
Console.WriteLine(outputPath);

internal sealed class SmokeEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "FleetPdfSmoke";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
