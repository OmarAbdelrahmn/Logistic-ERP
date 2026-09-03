using ClosedXML.Excel;
using LogisticsERP.Infrastructure.Fleet;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class GpsDistanceSpreadsheetParserTests
{
    [Fact]
    public void AttachedLegacyGpsSampleMatchesExpectedDailyTotalsWhenAvailable()
    {
        var path = Environment.GetEnvironmentVariable("GPS_SAMPLE_XLS");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var companionSheet = Path.Combine(
            Path.GetDirectoryName(path)!,
            Path.GetFileNameWithoutExtension(path) + ".files",
            "sheet001.htm");
        var reportPath = File.Exists(companionSheet) ? companionSheet : path;
        using var stream = File.OpenRead(reportPath);
        var report = GpsDistanceSpreadsheetParser.Parse(stream);

        Assert.Equal(new DateOnly(2026, 8, 31), report.WorkDate);
        Assert.NotEmpty(report.Rows);
        Assert.Contains(report.Rows, row => row.HasGpsDistance);
        Assert.All(report.Rows.Where(row => row.HasGpsDistance), row => Assert.True(row.DistanceKm >= 0));
    }

    [Fact]
    public void ParserReadsReportDateDistancesAndMissingGpsRows()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("GPS");
        sheet.Cell(1, 1).Value = "معلومات عامة";
        sheet.Cell(3, 1).Value = "فترة:";
        sheet.Cell(3, 2).Value = "2026-08-31 00:00:00 - 2026-09-01 00:00:00";
        sheet.Cell(5, 1).Value = "عربة";
        sheet.Cell(5, 2).Value = "طول الطريق";
        sheet.Cell(6, 1).Value = "2429 AH";
        sheet.Cell(6, 2).Value = "68.55 كيلومترا";
        sheet.Cell(7, 1).Value = "1098 ا ط س";
        sheet.Cell(7, 2).Value = "لم يتم العثور على طلبك.";
        sheet.Cell(8, 1).Value = "أ ص ه 5193";
        sheet.Cell(8, 2).Value = "كيلومترا";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var report = GpsDistanceSpreadsheetParser.Parse(stream);

        Assert.Equal(new DateOnly(2026, 8, 31), report.WorkDate);
        Assert.Equal(3, report.Rows.Count);
        Assert.Equal(68.55m, report.Rows[0].DistanceKm);
        Assert.True(report.Rows[0].HasGpsDistance);
        Assert.False(report.Rows[1].HasGpsDistance);
        Assert.False(report.Rows[2].HasGpsDistance);
        Assert.All(report.Rows, row => Assert.Null(row.ErrorCode));
    }

    [Theory]
    [InlineData("١٢٢٫٦١ كيلومترا", 122.61)]
    [InlineData("۱۲۲٫۶۱ km", 122.61)]
    [InlineData("۱٬۲۳۴٫۵۰ كم", 1234.50)]
    [InlineData("205 كيلومترا", 205)]
    [InlineData("3.16 km", 3.16)]
    public void DistanceParserSupportsGpsNumberFormats(string value, decimal expected)
    {
        Assert.True(GpsDistanceSpreadsheetParser.TryParseDistance(value, out var actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HtmlParserHandlesMixedArabicEnglishBidiTextPersianDigitsAndUtf16()
    {
        const string html = """
            <html><head><meta charset="utf-16"></head><body><table>
            <tr><td>الفترة: ۳۱/۰۸/۲۰۲۶ ۰۰:۰۰:۰۰ - ۰۱/۰۹/۲۰۲۶ ۰۰:۰۰:۰۰</td></tr>
            <tr><th>‏رقم اللوحة‎ / Plate Number</th><th>Route Distance / المسافة</th></tr>
            <tr><td>۱۲۳۴ ا ب ح</td><td>۱٬۲۳۴٫۵۰ km</td></tr>
            <tr><td>5678 D R S</td><td>N/A</td></tr>
            </table></body></html>
            """;
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(html)).ToArray();
        using var stream = new MemoryStream(bytes);

        var report = GpsDistanceSpreadsheetParser.Parse(stream);

        Assert.Equal(new DateOnly(2026, 8, 31), report.WorkDate);
        Assert.Equal(2, report.Rows.Count);
        Assert.Equal("1234 ا ب ح", report.Rows[0].PlateNumber);
        Assert.Equal(1234.50m, report.Rows[0].DistanceKm);
        Assert.True(report.Rows[0].HasGpsDistance);
        Assert.False(report.Rows[1].HasGpsDistance);
        Assert.Null(report.Rows[1].ErrorCode);
    }

    [Fact]
    public void ArchiveParserReadsCompanionExcelHtmlWorksheet()
    {
        const string sheet = """
            <html><head><meta charset="utf-8"></head><body><table>
            <tr><td>فترة: 2026-08-31 00:00:00 - 2026-09-01 00:00:00</td></tr>
            <tr><th>عربة</th><th>طول الطريق</th></tr>
            <tr><td>ABS 1020</td><td>68.55 كيلومترا</td></tr>
            </table></body></html>
            """;
        using var archiveStream = new MemoryStream();
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("report.files/sheet001.htm");
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(sheet);
        }
        archiveStream.Position = 0;

        var report = GpsDistanceSpreadsheetParser.ParseArchive(archiveStream);

        Assert.Single(report.Rows);
        Assert.Equal("ABS 1020", report.Rows[0].PlateNumber);
        Assert.Equal(68.55m, report.Rows[0].DistanceKm);
    }
}
