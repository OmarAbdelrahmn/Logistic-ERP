using System.IO.Compression;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Ninja;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed record NinjaParsedAccountUpdateRow(int SourceRowNumber, string CaptainId, string OwnerName,
    string City, string IqamaNo, string Status, bool? Suspended);
internal sealed record NinjaParsedAccountUpdateSheet(string SheetName, IReadOnlyList<NinjaParsedAccountUpdateRow> Rows,
    IReadOnlyList<NinjaImportIssue> Issues, int IgnoredFooterRows);

internal static partial class NinjaAccountUpdateSheetParser
{
    private static readonly string[] Required = ["الاسم", "المدينة", "رقم الهوية", "الحالة"];
    private static bool Header(IXLCell cell, string value) => cell.GetString().Trim() == value;

    private static bool ProviderBlankIdHeader(IXLWorksheet sheet) => sheet.Cell(1, 1).IsEmpty()
        && Header(sheet.Cell(1, 2), "الصورة") && Header(sheet.Cell(1, 3), "الاسم")
        && Header(sheet.Cell(1, 4), "المدينة") && Header(sheet.Cell(1, 5), "رقم الهوية")
        && Header(sheet.Cell(1, 6), "البريد الإلكتروني") && Header(sheet.Cell(1, 7), "الحالة")
        && Header(sheet.Cell(1, 8), "موقوف");

    public static NinjaParsedAccountUpdateSheet Parse(byte[] content, CancellationToken ct = default)
    {
        var rows = new List<NinjaParsedAccountUpdateRow>(); var issues = new List<NinjaImportIssue>();
        var ignored = 0; var sheetName = "";
        NinjaParsedAccountUpdateSheet Failed(int row, string message) => new(sheetName, [], [new(row, null, message)], ignored);
        if (content.Length is 0 or > NinjaSpreadsheetParser.MaxBytes) return Failed(0, "الملف فارغ أو يتجاوز 10 ميجابايت.");
        try
        {
            using var stream = new MemoryStream(content);
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
                if (zip.Entries.Count > 2000 || zip.Entries.Sum(e => e.Length) > 40 * 1024 * 1024)
                    return Failed(0, "محتوى ملف Excel يتجاوز الحد المسموح.");
            stream.Position = 0;
            using var book = new XLWorkbook(stream);
            var sheets = book.Worksheets.Where(s => Required.All(h => s.Row(1).CellsUsed().Any(c => Header(c, h)))
                && (s.Row(1).CellsUsed().Any(c => Header(c, "المعرف")) || ProviderBlankIdHeader(s))).ToArray();
            if (sheets.Length != 1) return Failed(1, "يجب وجود ورقة واحدة بعناوين المعرف، الاسم، المدينة، رقم الهوية، الحالة. يُقبل تصدير نينجا الأصلي ذو عنوان المعرف الفارغ.");
            var sheet = sheets[0]; sheetName = sheet.Name;
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (last > NinjaAccountSheetParser.MaxAccounts + 21 || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 200)
                return Failed(0, "الحد الأقصى 10000 حساب و200 عمود.");
            var columns = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var h in Required.Concat(["المعرف", "موقوف"]))
            {
                var cells = sheet.Row(1).CellsUsed().Where(c => Header(c, h)).ToArray();
                if (cells.Length > 1) return Failed(1, $"عنوان مكرر: {h}.");
                columns[h] = cells.Length == 1 ? cells[0].Address.ColumnNumber : h == "المعرف" ? 1 : 0;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var n = 2; n <= last; n++)
            {
                ct.ThrowIfCancellationRequested(); var row = sheet.Row(n); if (row.IsEmpty()) continue;
                string? id = null;
                try
                {
                    var cells = columns.Values.Where(c => c > 0).Select(c => row.Cell(c)).ToArray();
                    if (cells.Any(c => c.HasFormula)) throw new FormatException("حقول تحديث الحسابات يجب أن تحتوي على قيم دون صيغ.");
                    var idCell = row.Cell(columns["المعرف"]);
                    var first = NinjaSpreadsheetParser.NormalizeId(idCell.GetString());
                    if (row.CellsUsed().All(c => c.Address.ColumnNumber == columns["المعرف"])
                        && (first == "الصفوف في كل صفحة" || FooterRange().IsMatch(first))) { ignored++; continue; }
                    if (ignored > 0) throw new FormatException("يجب أن تكون صفوف التذييل بعد جميع الحسابات.");
                    id = NinjaAccountSheetParser.ReadIdentifier(idCell);
                    if (id.Length is 0 or > 150 || id.Any(c => c is < '0' or > '9') || !ids.Add(id))
                        throw new FormatException("المعرف فارغ أو غير صالح أو مكرر.");
                    var name = row.Cell(columns["الاسم"]).GetString().Trim();
                    var city = row.Cell(columns["المدينة"]).GetString().Trim();
                    var iqama = NinjaAccountSheetParser.ReadIdentifier(row.Cell(columns["رقم الهوية"]));
                    var status = row.Cell(columns["الحالة"]).GetString().Trim();
                    if (name.Length > 200 || city.Length > 200 || iqama.Length > 100 || status.Length > 100)
                        throw new FormatException("الاسم والمدينة بحد أقصى 200 حرف، والهوية والحالة بحد أقصى 100 حرف.");
                    bool? suspended = null;
                    if (columns["موقوف"] > 0)
                    {
                        var value = row.Cell(columns["موقوف"]).GetString().Trim();
                        if (value.Length > 0) suspended = value.ToUpperInvariant() switch
                        {
                            "TRUE" or "1" or "نعم" => true, "FALSE" or "0" or "لا" => false,
                            _ => throw new FormatException("قيمة موقوف يجب أن تكون True/False أو نعم/لا أو 1/0.")
                        };
                    }
                    rows.Add(new(n, id, name, city, iqama, status, suspended));
                    if (rows.Count > NinjaAccountSheetParser.MaxAccounts) return Failed(0, "الحد الأقصى 10000 حساب.");
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                { issues.Add(new(n, id, e.Message)); }
            }
            if (rows.Count == 0 && issues.Count == 0) return Failed(0, "الملف لا يحتوي على حسابات.");
        }
        catch (Exception e) when (e is InvalidDataException or IOException or ArgumentException or FormatException
            or System.Xml.XmlException or InvalidOperationException)
        { return Failed(0, "تعذر قراءة ملف Excel؛ ارفع ملف xlsx صالحاً."); }
        return new(sheetName, rows, issues, ignored);
    }

    [GeneratedRegex(@"^\d+\s+من\s+\d+\s*[-–]\s*\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex FooterRange();
}
