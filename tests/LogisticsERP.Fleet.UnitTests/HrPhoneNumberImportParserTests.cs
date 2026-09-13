using ClosedXML.Excel;
using LogisticsERP.Infrastructure.Hr;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HrPhoneNumberImportParserTests
{
    [Fact]
    public void HeaderRowAndArabicDigitsAreSupportedAndPhonesAreNormalized()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Phone numbers");
        sheet.Cell(1, 1).Value = "رقم الإقامة";
        sheet.Cell(1, 2).Value = "رقم الجوال";
        sheet.Cell(2, 1).Value = "١٢٣٤٥٦٧٨٩٠";
        sheet.Cell(2, 2).Value = "٠٥٥٥ ١٢٣ ٤٥٦";
        using var stream = Save(workbook);

        var parsed = HrPhoneNumberImportParser.Parse(stream);

        Assert.Equal(1, parsed.TotalRows);
        var row = Assert.Single(parsed.Rows);
        Assert.Equal(2, row.RowNumber);
        Assert.Equal("1234567890", row.IqamaNo);
        Assert.Equal("+966555123456", row.PhoneNumber);
        Assert.Empty(parsed.Issues);
    }

    [Fact]
    public void HeaderlessTwoColumnWorkbookIsSupported()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Phone numbers");
        sheet.Cell(1, 1).Value = "1234567890";
        sheet.Cell(1, 2).Value = "555123456";
        using var stream = Save(workbook);

        var parsed = HrPhoneNumberImportParser.Parse(stream);

        var row = Assert.Single(parsed.Rows);
        Assert.Equal(1, row.RowNumber);
        Assert.Equal("+966555123456", row.PhoneNumber);
    }

    [Fact]
    public void DuplicateIqamaAndInvalidPhoneAreReportedAsRowErrors()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Phone numbers");
        sheet.Cell(1, 1).Value = "Iqama number";
        sheet.Cell(1, 2).Value = "Phone number";
        sheet.Cell(2, 1).Value = "1234567890";
        sheet.Cell(2, 2).Value = "0555123456";
        sheet.Cell(3, 1).Value = "1234567890";
        sheet.Cell(3, 2).Value = "invalid";
        using var stream = Save(workbook);

        var parsed = HrPhoneNumberImportParser.Parse(stream);

        Assert.Equal(2, parsed.TotalRows);
        Assert.Single(parsed.Rows);
        Assert.Equal(2, parsed.Issues.Count);
        Assert.All(parsed.Issues, issue => Assert.Equal("Error", issue.Severity));
        Assert.Contains(parsed.Issues, issue => issue.Message.Contains("Duplicate", StringComparison.Ordinal));
        Assert.Contains(parsed.Issues, issue => issue.Message.Contains("second column", StringComparison.Ordinal));
    }

    private static MemoryStream Save(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
