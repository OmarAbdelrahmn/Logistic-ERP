using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class AccidentPdfGeneratorTests
{
    [Fact]
    public void GenerateCreatesArabicFirstPdfDocument()
    {
        var generator = new AccidentPdfGenerator(
            Options.Create(new PdfGenerationOptions
            {
                QuestPdfLicense = "Community",
                ArabicFontFamily = "Arial"
            }),
            new TestHostEnvironment(AppContext.BaseDirectory));
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
            "الرياض / Riyadh",
            VehicleAccidentSeverity.Moderate,
            false,
            false,
            null,
            "لا يوجد / None",
            "تلف في الصدام الأمامي / Front bumper damage",
            "قيد المراجعة / Under review",
            "وصف تجريبي للحادث مع معلومات ثنائية اللغة. / Bilingual accident narrative.",
            "POL-123",
            "CLM-456",
            "شركة التأمين / Insurance Company",
            "INS-789",
            new DateTimeOffset(2026, 8, 22, 9, 0, 0, TimeSpan.Zero),
            [new AccidentPdfEvidence("scene.pdf", "application/pdf", new string('A', 64), null)]);

        var pdf = generator.Generate(snapshot);

        Assert.True(pdf.Length > 1_000);
        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }
}
