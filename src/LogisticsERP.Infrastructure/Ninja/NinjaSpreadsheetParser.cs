using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Ninja;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed record NinjaParsedRow(int RowNumber, string CaptainId, int TotalOrders, decimal WorkingHours, decimal CollectedAmount, decimal Deductions);
internal sealed record NinjaParsedSheet(IReadOnlyList<NinjaParsedRow> Rows, IReadOnlyList<NinjaImportIssue> Issues);

internal static class NinjaSpreadsheetParser
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public static string NormalizeId(string value) => string.Concat(value.Trim().Select(c =>
        char.IsDigit(c) && char.GetNumericValue(c) is >= 0 and <= 9 ? (char)('0' + (int)char.GetNumericValue(c)) : c));

    public static NinjaParsedSheet Parse(byte[] content, bool deductions, CancellationToken ct = default)
    {
        string[] headers = deductions ? ["Captain ID", "labels.TotalCollectedAmountAtDelivery", "TOTAL DEDUCTIONS"]
            : ["Captain ID", "Total Orders", "Total Working Hours"];
        var rows = new List<NinjaParsedRow>(); var issues = new List<NinjaImportIssue>();
        if (content.Length is 0 or > MaxBytes) return new(rows, [new(0, null, "الملف فارغ أو يتجاوز 10 ميجابايت.")]);
        try
        {
            using var stream = new MemoryStream(content);
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
                if (zip.Entries.Count > 2000 || zip.Entries.Sum(x => x.Length) > 40 * 1024 * 1024)
                    return new(rows, [new(0, null, "محتوى ملف Excel يتجاوز الحد المسموح.")]);
            stream.Position = 0;
            using var book = new XLWorkbook(stream);
            bool Matches(IXLCell c, string h) => string.Equals(c.GetString().Trim(), h, StringComparison.OrdinalIgnoreCase);
            var sheets = book.Worksheets.Where(s => headers.All(h => s.Row(1).CellsUsed().Any(c => Matches(c, h)))).ToArray();
            if (sheets.Length != 1) return new(rows, [new(1, null, $"يجب وجود ورقة واحدة بعناوين: {string.Join(", ", headers)}.")]);
            var sheet = sheets[0]; var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (last > 10001 || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 200)
                return new(rows, [new(0, null, "الحد الأقصى 10000 صف و200 عمود.")]);
            var columns = new Dictionary<string, int>();
            foreach (var h in headers)
            {
                var matches = sheet.Row(1).CellsUsed().Where(c => Matches(c, h)).ToArray();
                if (matches.Length != 1) return new(rows, [new(1, null, $"عنوان مكرر: {h}.")]);
                columns[h] = matches[0].Address.ColumnNumber;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var n = 2; n <= last; n++)
            {
                ct.ThrowIfCancellationRequested();
                var row = sheet.Row(n); if (row.IsEmpty()) continue;
                string? id = null;
                try
                {
                    IXLCell Cell(string h) => row.Cell(columns[h]);
                    if (headers.Any(h => Cell(h).HasFormula)) throw new FormatException("حقول الاستيراد يجب أن تحتوي على قيم دون صيغ.");
                    var cell = Cell("Captain ID");
                    id = NormalizeId(cell.DataType == XLDataType.Number ? cell.GetDouble().ToString("0.################", CultureInfo.InvariantCulture) : cell.GetString());
                    if (id.Length is 0 or > 150 || id.Any(c => c is < '0' or > '9') || !ids.Add(id))
                        throw new FormatException("Captain ID فارغ أو غير صالح أو مكرر.");
                    decimal Number(string h)
                    {
                        if (!decimal.TryParse(Cell(h).GetString().Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                            throw new FormatException($"قيمة غير صالحة في {h}.");
                        return value;
                    }
                    if (deductions)
                    {
                        var collected = Number(headers[1]); var amount = Number(headers[2]);
                        if (collected < 0 || collected > 9999999999999999.99m || Math.Abs(amount) > 9999999999999999.99m
                            || decimal.Round(collected, 2) != collected || decimal.Round(amount, 2) != amount)
                            throw new FormatException("المبالغ يجب أن تكون صالحة بدقة منزلتين؛ المبلغ المحصل لا يكون سالباً.");
                        rows.Add(new(n, id, 0, 0, collected, amount));
                    }
                    else
                    {
                        var orders = Number(headers[1]); var hours = Number(headers[2]);
                        if (orders < 0 || orders > int.MaxValue || decimal.Truncate(orders) != orders || hours is < 0 or > 24)
                            throw new FormatException("الطلبات عدد صحيح غير سالب وساعات العمل بين 0 و24.");
                        rows.Add(new(n, id, (int)orders, decimal.Round(hours, 8, MidpointRounding.AwayFromZero), 0, 0));
                    }
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                { issues.Add(new(n, id, e.Message)); }
            }
            if (rows.Count == 0 && issues.Count == 0) issues.Add(new(0, null, "الملف لا يحتوي على بيانات."));
        }
        catch (Exception e) when (e is InvalidDataException or IOException or ArgumentException or FormatException or System.Xml.XmlException or InvalidOperationException)
        { issues.Add(new(0, null, "تعذر قراءة ملف Excel؛ ارفع ملف xlsx صالحاً.")); }
        return new(rows, issues);
    }
}
