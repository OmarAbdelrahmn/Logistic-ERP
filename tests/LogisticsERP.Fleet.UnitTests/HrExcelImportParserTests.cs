using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Hr;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HrExcelImportParserTests
{
    [Fact]
    public void SampleShapeMapsSupportedColumnsAndSplitsMultipleLicenseTypes()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("المقيمين النشطين");
        string[] headers =
        [
            "رقم الاقامة", "الاسم", "الجنس", "الجنسية", "المهنة", "رقم الجواز",
            "تاريخ انتهاء الجواز", "تاريخ اصدار الاقامة", "تاريخ انتهاء الاقامة", "تاريخ الميلاد",
            "خارج المملكه", "رقم صاحب العمل", "تاريخ التعيين", "حالة الكفالة", "حالة الموظف",
            "الفرع / المدينة", "المهنة بالإقامة", "المسمى الوظيفي", "العمل الفعلي", "نوع الرخصة"
        ];
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        sheet.Cell(2, 1).Value = "٢٢٣٨٣٠٦١٧٥";
        sheet.Cell(2, 2).Value = "Test Rider";
        sheet.Cell(2, 3).Value = "ذكر";
        sheet.Cell(2, 4).Value = "سري لنكا";
        sheet.Cell(2, 5).Value = "سائق دراجة نارية";
        sheet.Cell(2, 6).Value = "P1000000";
        sheet.Cell(2, 7).Value = new DateTime(2036, 3, 6);
        sheet.Cell(2, 8).Value = new DateTime(2025, 1, 1);
        sheet.Cell(2, 9).Value = new DateTime(2027, 1, 1);
        sheet.Cell(2, 10).Value = new DateTime(1992, 3, 22);
        sheet.Cell(2, 11).Value = "لا";
        sheet.Cell(2, 12).Value = "7015658094";
        sheet.Cell(2, 13).Value = new DateTime(2026, 1, 1);
        sheet.Cell(2, 14).Value = "علي الكفالة";
        sheet.Cell(2, 15).Value = "Active";
        sheet.Cell(2, 16).Value = "جدة";
        sheet.Cell(2, 17).Value = "سائق دراجة نارية";
        sheet.Cell(2, 18).Value = "مندوب توصيل";
        sheet.Cell(2, 19).Value = "دباب";
        sheet.Cell(2, 20).Value = "نقل خفيف + دراجة نارية";
        using var stream = Save(workbook);

        var parsed = HrExcelImportParser.Parse(stream);

        Assert.Equal(1, parsed.TotalRows);
        var row = Assert.Single(parsed.Rows);
        Assert.Equal("2238306175", row.IqamaNo);
        Assert.Equal(new DateOnly(2025, 1, 1), row.ResidencyIssueDate);
        Assert.Equal(new DateOnly(2027, 1, 1), row.ResidencyExpiryDate);
        Assert.Equal(new DateOnly(1992, 3, 22), row.BirthDate);
        Assert.False(row.IsEmployee);
        Assert.Equal(["نقل خفيف", "دراجة نارية"], row.LicenseTypes);
        Assert.Contains("نوع الرخصة", parsed.ImportedColumns);
        Assert.Contains("رقم الجواز", parsed.IgnoredColumns);
        Assert.Contains("تاريخ انتهاء الجواز", parsed.IgnoredColumns);
        Assert.Empty(parsed.Issues);
    }

    [Fact]
    public void InvalidSupportedDateIsReportedAsARowError()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Employees");
        sheet.Cell(1, 1).Value = "رقم الاقامة";
        sheet.Cell(1, 2).Value = "الاسم";
        sheet.Cell(1, 3).Value = "تاريخ انتهاء الاقامة";
        sheet.Cell(2, 1).Value = "1234567890";
        sheet.Cell(2, 2).Value = "Test Employee";
        sheet.Cell(2, 3).Value = "not-a-date";
        using var stream = Save(workbook);

        var parsed = HrExcelImportParser.Parse(stream);

        Assert.Equal(1, parsed.TotalRows);
        Assert.Empty(parsed.Rows);
        var issue = Assert.Single(parsed.Issues);
        Assert.Equal("Error", issue.Severity);
        Assert.Contains("Residency expiry date", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LicenseCategoryResolutionUsesCatalogNamesAndKnownAliases()
    {
        DriverLicenseCategory[] categories =
        [
            new() { Id = DriverLicenseCategory.LightTransportId, Code = "LIGHT_TRANSPORT", NameAr = "نقل خفيف", NameEn = "Light Transport" },
            new() { Id = DriverLicenseCategory.MotorcycleId, Code = "MOTORCYCLE", NameAr = "دراجة نارية", NameEn = "Motorcycle" },
            new() { Code = "PRIVATE", NameAr = "خصوصي", NameEn = "Private" }
        ];

        Assert.Equal(
            DriverLicenseCategory.MotorcycleId,
            HrExcelImportService.ResolveLicenseCategory("دراجة الية", categories)!.Id);
        Assert.Equal("PRIVATE", HrExcelImportService.ResolveLicenseCategory("خصوصي", categories)!.Code);
        Assert.Null(HrExcelImportService.ResolveLicenseCategory("غير معروف", categories));
    }

    [Fact]
    public void AttachedSampleWorkbookMatchesTheSupportedShapeWhenAvailable()
    {
        var path = Environment.GetEnvironmentVariable("HR_IMPORT_SAMPLE_XLSX");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        using var stream = File.OpenRead(path);
        var parsed = HrExcelImportParser.Parse(stream);

        Assert.Equal(512, parsed.TotalRows);
        Assert.Equal(512, parsed.Rows.Count);
        Assert.Contains("نوع الرخصة", parsed.ImportedColumns);
        Assert.Empty(parsed.Issues);
    }

    private static MemoryStream Save(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
