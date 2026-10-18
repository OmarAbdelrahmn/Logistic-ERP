using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Ninja;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed record NinjaParsedAccountSheet(IReadOnlyList<NinjaAccountSheetRow> Rows,
    IReadOnlyList<NinjaImportIssue> Issues, int IgnoredFooterRows);

internal static partial class NinjaAccountSheetParser
{
    private static readonly string[] Headers = ["المعرف", "الاسم", "المدينة", "رقم الهوية"];
    public const int MaxAccounts = 10000;

    public static NinjaParsedAccountSheet Parse(byte[] content, CancellationToken ct = default)
    {
        var rows = new List<NinjaAccountSheetRow>();
        var issues = new List<NinjaImportIssue>();
        var ignored = 0;
        if (content.Length is 0 or > NinjaSpreadsheetParser.MaxBytes)
            return new(rows, [new(0, null, "الملف فارغ أو يتجاوز 10 ميجابايت.")], ignored);
        try
        {
            using var stream = new MemoryStream(content);
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
                if (zip.Entries.Count > 2000 || zip.Entries.Sum(x => x.Length) > 40 * 1024 * 1024)
                    return new(rows, [new(0, null, "محتوى ملف Excel يتجاوز الحد المسموح.")], ignored);
            stream.Position = 0;
            using var book = new XLWorkbook(stream);
            bool Matches(IXLCell c, string h) => string.Equals(c.GetString().Trim(), h, StringComparison.OrdinalIgnoreCase);
            var sheets = book.Worksheets.Where(s => Headers.All(h => s.Row(1).CellsUsed().Any(c => Matches(c, h)))).ToArray();
            if (sheets.Length != 1)
                return new(rows, [new(1, null, $"يجب وجود ورقة واحدة بعناوين: {string.Join(", ", Headers)}.")], ignored);
            var sheet = sheets[0];
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (last > MaxAccounts + 21 || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 200)
                return new(rows, [new(0, null, "الحد الأقصى 10000 حساب و200 عمود." )], ignored);
            var columns = new int[Headers.Length];
            for (var i = 0; i < Headers.Length; i++)
            {
                var matches = sheet.Row(1).CellsUsed().Where(c => Matches(c, Headers[i])).ToArray();
                if (matches.Length != 1) return new(rows, [new(1, null, $"عنوان مكرر: {Headers[i]}.")], ignored);
                columns[i] = matches[0].Address.ColumnNumber;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var n = 2; n <= last; n++)
            {
                ct.ThrowIfCancellationRequested();
                var row = sheet.Row(n);
                if (row.IsEmpty()) continue;
                string? id = null;
                try
                {
                    var cells = columns.Select(c => row.Cell(c)).ToArray();
                    if (cells.Any(c => c.HasFormula)) throw new FormatException("حقول المقارنة يجب أن تحتوي على قيم دون صيغ.");
                    var first = NinjaSpreadsheetParser.NormalizeId(cells[0].GetString());
                    // These two footer forms occur in the provider export. Never ignore an arbitrary invalid ID.
                    if (cells.Skip(1).All(c => c.IsEmpty()) &&
                        (first == "الصفوف في كل صفحة" || FooterRange().IsMatch(first)))
                    { ignored++; continue; }
                    if (ignored > 0) throw new FormatException("يجب أن تكون صفوف تذييل التقرير بعد جميع الحسابات.");
                    id = ReadIdentifier(cells[0]);
                    if (id.Length is 0 or > 150 || id.Any(c => c is < '0' or > '9') || !ids.Add(id))
                        throw new FormatException("المعرف فارغ أو غير صالح أو مكرر.");
                    var name = cells[1].GetString().Trim(); var city = cells[2].GetString().Trim();
                    var nationalId = ReadIdentifier(cells[3]);
                    if (name.Length is 0 or > 200 || city.Length is 0 or > 200)
                        throw new FormatException("الاسم والمدينة مطلوبان وبحد أقصى 200 حرف لكل حقل.");
                    if (nationalId.Length != 10 || nationalId.Any(c => c is < '0' or > '9'))
                        throw new FormatException("رقم هوية صاحب الحساب يجب أن يتكون من 10 أرقام.");
                    rows.Add(new(n, id, name, city, nationalId));
                    if (rows.Count > MaxAccounts) return new([], [new(0, null, "الحد الأقصى 10000 حساب.")], ignored);
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                { issues.Add(new(n, id, e.Message)); }
            }
            if (rows.Count == 0 && issues.Count == 0) issues.Add(new(0, null, "الملف لا يحتوي على حسابات."));
        }
        catch (Exception e) when (e is InvalidDataException or IOException or ArgumentException or FormatException
            or System.Xml.XmlException or InvalidOperationException)
        { issues.Add(new(0, null, "تعذر قراءة ملف Excel؛ ارفع ملف xlsx صالحاً.")); }
        return new(rows, issues, ignored);
    }

    internal static string ReadIdentifier(IXLCell cell)
    {
        if (cell.DataType != XLDataType.Number) return NinjaSpreadsheetParser.NormalizeId(cell.GetString());
        var value = cell.GetDouble();
        if (!double.IsFinite(value) || value < 0 || value >= 1e15 || Math.Truncate(value) != value)
            throw new FormatException("المعرفات الرقمية يجب أن تكون أعداداً صحيحة؛ احفظ المعرفات الطويلة كنص.");
        // Respect a zero-only number format, which is often used for leading-zero identifiers.
        var format = cell.Style.NumberFormat.Format;
        var text = format.Length > 0 && format.All(c => c == '0')
            ? value.ToString(format, CultureInfo.InvariantCulture) : value.ToString("0", CultureInfo.InvariantCulture);
        return NinjaSpreadsheetParser.NormalizeId(text);
    }

    [GeneratedRegex(@"^\d+\s+من\s+\d+\s*[-–]\s*\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex FooterRange();
}
