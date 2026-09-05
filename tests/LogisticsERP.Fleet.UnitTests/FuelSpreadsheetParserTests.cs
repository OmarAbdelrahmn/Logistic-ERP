using ClosedXML.Excel;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Domain.Fuel;
using LogisticsERP.Infrastructure.Fuel;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FuelSpreadsheetParserTests
{
    [Fact]
    public void PetroAppDetailedRowsAreSummedIntoOneMonthlyCardTotal()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Worksheet");
        string[] headers =
        [
            "رقم الفاتورة", "المركبة", "الرقم الداخلي", "نوع الوقود", "التكلفة",
            "التكلفة قبل الضريبة", "عدد اللترات", "التاريخ"
        ];
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        sheet.Cell(2, 1).Value = "INV-1";
        sheet.Cell(2, 2).Value = "ب  ب  و  ٨  ٣  ٥";
        sheet.Cell(2, 3).Value = "bw203";
        sheet.Cell(2, 4).Value = "91";
        sheet.Cell(2, 5).Value = 20m;
        sheet.Cell(2, 6).Value = 17.39m;
        sheet.Cell(2, 7).Value = 9.174m;
        sheet.Cell(2, 8).Value = new DateTime(2026, 8, 15, 10, 0, 0);
        sheet.Cell(3, 1).Value = "INV-2";
        sheet.Cell(3, 2).Value = "\u200Fب ب و ٨٣٥\u200E";
        sheet.Cell(3, 3).Value = "BW-203";
        sheet.Cell(3, 4).Value = "91";
        sheet.Cell(3, 5).Value = 50m;
        sheet.Cell(3, 6).Value = 43.48m;
        sheet.Cell(3, 7).Value = 22.936m;
        sheet.Cell(3, 8).Value = new DateTime(2026, 8, 31, 23, 0, 0);
        using var stream = Save(workbook);

        var report = FuelSpreadsheetParser.Parse(stream);

        Assert.Equal(FuelCardProvider.PetroApp, report.Provider);
        Assert.Equal(new DateOnly(2026, 8, 1), report.ReportMonth);
        var card = Assert.Single(report.Cards);
        Assert.Equal("BW203", card.NormalizedCardNumber);
        Assert.Equal(FuelCardIdentifierType.InternalNumber, card.IdentifierType);
        Assert.Equal(32.110m, card.TotalLiters);
        Assert.Equal(70m, card.TotalAmount);
        Assert.Equal(60.87m, card.AmountBeforeTax);
        Assert.Equal(9.13m, card.VatAmount);
        Assert.Equal(2, card.TransactionCount);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public void SayaraAppSummaryIsUsedAsThePeriodTotalWithoutDailyResumming()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Combined Data");
        sheet.Cell(1, 1).Value = "تقرير أستهلاك المركبة - 02/09/2026 05:56:34 م";
        sheet.Cell(3, 1).Value = "الرقم التسلسلي";
        sheet.Cell(3, 2).Value = "لوحة المركبة";
        sheet.Cell(3, 3).Value = "رقم المركبة الداخلي";
        sheet.Cell(3, 4).Value = "نوع الوقود";
        sheet.Cell(3, 5).Value = "الاستهلاك بالليتر";
        sheet.Cell(3, 6).Value = "التكلفة";
        sheet.Cell(4, 1).Value = 1;
        sheet.Cell(4, 2).Value = "\u200Fب  ب  و  ٨  ٣  ٥\u200E";
        sheet.Cell(4, 4).Value = "_91";
        sheet.Cell(4, 5).Value = 474.87m;
        sheet.Cell(4, 6).Value = 1035.22m;
        using var stream = Save(workbook);

        var report = FuelSpreadsheetParser.Parse(stream);

        Assert.Equal(FuelCardProvider.SayaraApp, report.Provider);
        Assert.Equal(new DateOnly(2026, 9, 1), report.ReportMonth);
        var card = Assert.Single(report.Cards);
        Assert.Equal(FuelCardIdentifierType.PlateNumber, card.IdentifierType);
        Assert.Equal(474.870m, card.TotalLiters);
        Assert.Equal(1035.22m, card.TotalAmount);
        Assert.Null(card.TransactionCount);
        Assert.Equal("91", card.FuelType);
        Assert.Equal(PlateNumberRules.CanonicalKey("ب ب و ٨٣٥"), card.NormalizedCardNumber);
    }

    [Theory]
    [InlineData("ب  ب  و  ٨  ٣  ٥")]
    [InlineData("\u200Fب ب و ٨٣٥\u200E")]
    [InlineData("ب-ب-و/۸۳۵")]
    public void PlateNormalizationHandlesRtlLtrMarksAndArabicPersianDigits(string plate)
    {
        Assert.Equal("835BBU", PlateNumberRules.CanonicalKey(plate));
        Assert.Equal("835BBU", FuelCardRules.NormalizeCardNumber(plate, FuelCardIdentifierType.PlateNumber));
    }

    [Fact]
    public void AttachedProviderSamplesMatchTheirKnownShapesWhenAvailable()
    {
        var petroPath = Environment.GetEnvironmentVariable("PETRO_APP_SAMPLE_XLSX");
        var sayaraPath = Environment.GetEnvironmentVariable("SAYARA_APP_SAMPLE_XLSX");
        if (string.IsNullOrWhiteSpace(petroPath) || string.IsNullOrWhiteSpace(sayaraPath))
        {
            return;
        }

        using var petroStream = File.OpenRead(petroPath);
        var petro = FuelSpreadsheetParser.Parse(petroStream);
        Assert.Equal(FuelCardProvider.PetroApp, petro.Provider);
        Assert.Equal(new DateOnly(2026, 8, 1), petro.ReportMonth);
        Assert.Equal(559, petro.Cards.Count);
        Assert.Equal(11_624, petro.SourceRows);
        Assert.Empty(petro.Errors);
        Assert.Equal(249_560.682m, petro.Cards.Sum(card => card.TotalLiters));
        Assert.Equal(544_060.18m, petro.Cards.Sum(card => card.TotalAmount));
        Assert.All(petro.Cards, card => Assert.True(card.TransactionCount > 0));

        using var sayaraStream = File.OpenRead(sayaraPath);
        var sayara = FuelSpreadsheetParser.Parse(sayaraStream);
        Assert.Equal(FuelCardProvider.SayaraApp, sayara.Provider);
        Assert.Equal(new DateOnly(2026, 9, 1), sayara.ReportMonth);
        Assert.Equal(73, sayara.Cards.Count);
        Assert.Equal(73, sayara.SourceRows);
        Assert.Empty(sayara.Errors);
        Assert.Equal(11_857.490m, sayara.Cards.Sum(card => card.TotalLiters));
        Assert.Equal(25_849.26m, sayara.Cards.Sum(card => card.TotalAmount));
        Assert.All(sayara.Cards, card => Assert.Null(card.TransactionCount));
    }

    private static MemoryStream Save(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
