using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Chefz;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed record ChefzParsedRow(int RowNumber, string IdNumber, int TotalValidOffers,
    int AcceptedOffers, int DeclinedOffers, int MissedOffers, decimal AcceptanceRate);
internal sealed record ChefzParsedSheet(IReadOnlyList<ChefzParsedRow> Rows, IReadOnlyList<ChefzImportIssue> Issues);

internal static class ChefzSpreadsheetParser
{
    public const int MaxBytes = 10 * 1024 * 1024;
    private static readonly string[] Headers = ["Id Number", "Total Valid Offers", "Accepted Offers",
        "Declined Offers", "Missed Offers", "Acceptance Rate"];

    public static string NormalizeId(string value) => string.Concat(value.Trim().Select(c =>
        char.IsDigit(c) && char.GetNumericValue(c) is >= 0 and <= 9
            ? (char)('0' + (int)char.GetNumericValue(c)) : c));

    public static ChefzParsedSheet Parse(byte[] content)
    {
        var rows = new List<ChefzParsedRow>();
        var issues = new List<ChefzImportIssue>();
        if (content.Length is 0 or > MaxBytes)
            return new(rows, [new(0, null, "الملف فارغ أو يتجاوز 10 ميجابايت.")]);
        try
        {
            using var stream = new MemoryStream(content);
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                if (zip.Entries.Count > 2000 || zip.Entries.Sum(x => x.Length) > 40 * 1024 * 1024)
                    return new(rows, [new(0, null, "محتوى ملف Excel يتجاوز الحد المسموح.")]);
            }
            stream.Position = 0;
            using var workbook = new XLWorkbook(stream);
            var sheets = workbook.Worksheets.Where(s => Headers.All(h =>
                s.Row(1).CellsUsed().Any(c => string.Equals(c.GetString().Trim(), h, StringComparison.OrdinalIgnoreCase)))).ToArray();
            if (sheets.Length != 1)
                return new(rows, [new(1, null, "يجب وجود ورقة واحدة بعناوين Id Number وحقول الأداء الخمسة.")]);
            var sheet = sheets[0];
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (last > 10001 || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 200)
                return new(rows, [new(0, null, "الحد الأقصى 10000 صف و200 عمود.")]);
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in Headers)
            {
                var matches = sheet.Row(1).CellsUsed().Where(c => string.Equals(c.GetString().Trim(), header, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length != 1) return new(rows, [new(1, null, $"عنوان مكرر: {header}.")]);
                columns[header] = matches[0].Address.ColumnNumber;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var number = 2; number <= last; number++)
            {
                var row = sheet.Row(number);
                if (row.IsEmpty()) continue;
                string? id = null;
                try
                {
                    IXLCell Cell(string h) => row.Cell(columns[h]);
                    var idCell = Cell("Id Number");
                    id = NormalizeId(idCell.DataType == XLDataType.Number
                        ? idCell.GetDouble().ToString("0.################", CultureInfo.InvariantCulture) : idCell.GetString());
                    if (id.Length is 0 or > 100 || id.Contains('.') || !ids.Add(id))
                        throw new FormatException("رقم الهوية فارغ أو غير صالح أو مكرر في الملف.");
                    int Count(string h)
                    {
                        if (!decimal.TryParse(Cell(h).GetString().Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                            || value < 0 || value > int.MaxValue || decimal.Truncate(value) != value)
                            throw new FormatException($"قيمة غير صالحة في {h}.");
                        return (int)value;
                    }
                    var rateCell = Cell("Acceptance Rate");
                    var rateText = rateCell.GetString().Trim();
                    var percentText = rateText.EndsWith('%');
                    if (!decimal.TryParse(rateText.TrimEnd('%').Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
                        throw new FormatException("نسبة القبول غير صالحة.");
                    if (!percentText && rateCell.DataType == XLDataType.Number
                        && (rateCell.Style.NumberFormat.Format.Contains('%') || rateCell.Style.NumberFormat.NumberFormatId is 9 or 10)) rate *= 100;
                    if (rate is < 0 or > 100) throw new FormatException("نسبة القبول يجب أن تكون بين 0 و100.");
                    rows.Add(new(number, id, Count("Total Valid Offers"), Count("Accepted Offers"),
                        Count("Declined Offers"), Count("Missed Offers"), decimal.Round(rate, 2, MidpointRounding.AwayFromZero)));
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                { issues.Add(new(number, id, e.Message)); }
            }
            if (rows.Count == 0 && issues.Count == 0) issues.Add(new(0, null, "الملف لا يحتوي على بيانات أداء."));
        }
        catch (Exception e) when (e is InvalidDataException or IOException or ArgumentException or FormatException
            or System.Xml.XmlException or InvalidOperationException)
        { issues.Add(new(0, null, "تعذر قراءة ملف Excel؛ ارفع ملف xlsx صالحاً.")); }
        return new(rows, issues);
    }
}
