using ClosedXML.Excel;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HrEmployeeStatusImportParserTests
{
    [Fact]
    public void HeaderRowAndArabicDigitsAreSupported()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Statuses");
        sheet.Cell(1, 1).Value = "رقم الإقامة";
        sheet.Cell(1, 2).Value = "حالة الموظف";
        sheet.Cell(2, 1).Value = "١٢٣٤٥٦٧٨٩٠";
        sheet.Cell(2, 2).Value = "٣";
        using var stream = Save(workbook);

        var parsed = HrEmployeeStatusImportParser.Parse(stream);

        Assert.Equal(1, parsed.TotalRows);
        var row = Assert.Single(parsed.Rows);
        Assert.Equal(2, row.RowNumber);
        Assert.Equal("1234567890", row.IqamaNo);
        Assert.Equal(EmployeeStatus.Active, row.Status);
        Assert.Empty(parsed.Issues);
    }

    [Fact]
    public void HeaderlessTwoColumnWorkbookIsSupported()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Statuses");
        sheet.Cell(1, 1).Value = "1234567890";
        sheet.Cell(1, 2).Value = 5;
        using var stream = Save(workbook);

        var row = Assert.Single(HrEmployeeStatusImportParser.Parse(stream).Rows);

        Assert.Equal(1, row.RowNumber);
        Assert.Equal(EmployeeStatus.OnLeave, row.Status);
    }

    [Fact]
    public void DuplicateIqamaAndOutOfRangeStatusAreReportedAsRowErrors()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Statuses");
        sheet.Cell(1, 1).Value = "Iqama number";
        sheet.Cell(1, 2).Value = "EmployeeStatus";
        sheet.Cell(2, 1).Value = "1234567890";
        sheet.Cell(2, 2).Value = 3;
        sheet.Cell(3, 1).Value = "1234567890";
        sheet.Cell(3, 2).Value = 11;
        using var stream = Save(workbook);

        var parsed = HrEmployeeStatusImportParser.Parse(stream);

        Assert.Equal(2, parsed.TotalRows);
        Assert.Single(parsed.Rows);
        Assert.Equal(2, parsed.Issues.Count);
        Assert.All(parsed.Issues, issue => Assert.Equal("Error", issue.Severity));
        Assert.Contains(parsed.Issues, issue => issue.Message.Contains("Duplicate", StringComparison.Ordinal));
        Assert.Contains(parsed.Issues, issue => issue.Message.Contains("1 through 10", StringComparison.Ordinal));
    }

    private static MemoryStream Save(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
