using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Chefz;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed record ChefzOrderRow(int RowNumber, string DriverId, DateOnly Date, string OrderId,
    decimal DeliveryFees, decimal Tips, decimal Compensation, decimal Bonuses, decimal Deductions, decimal CashCollected = 0);
internal sealed record ChefzOrderSheet(IReadOnlyList<ChefzOrderRow> Rows, IReadOnlyList<ChefzImportIssue> Issues);

internal static class ChefzOrderSpreadsheetParser
{
    public const int MaxRows = 50000;
    private static readonly string[] Required = ["Date", "Driver ID", "Driver Delivery Fees",
        "Deduction Amount", "Driver Compensate", "Driver Tip"];
    private static readonly string[] Optional = ["Chef Order ID", "Bonus Amount", "Cash Collected By Driver"];

    private static string Header(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static ChefzOrderSheet Parse(byte[] content, CancellationToken ct = default)
    {
        var rows = new List<ChefzOrderRow>();
        var issues = new List<ChefzImportIssue>();
        if (content.Length is 0 or > ChefzSpreadsheetParser.MaxBytes)
            return new(rows, [new(0, null, "الملف فارغ أو يتجاوز 10 ميجابايت.")]);
        try
        {
            using var stream = new MemoryStream(content);
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                if (zip.Entries.Count > 2000 || zip.Entries.Sum(e => e.Length) > 40 * 1024 * 1024)
                    return new(rows, [new(0, null, "محتوى ملف Excel يتجاوز الحد المسموح.")]);
            }
            stream.Position = 0;
            using var book = new XLWorkbook(stream);
            var sheets = book.Worksheets.Where(s => Required.All(h => s.Row(1).CellsUsed()
                .Any(c => string.Equals(Header(c.GetString()), h, StringComparison.OrdinalIgnoreCase)))).ToArray();
            if (sheets.Length != 1)
                return new(rows, [new(1, null, "يلزم وجود ورقة واحدة بعناوين Date وDriver ID وDriver Delivery Fees وDeduction Amount وDriver Compensate وDriver Tip.")]);
            var sheet = sheets[0];
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            if (last > MaxRows + 1 || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 200)
                return new(rows, [new(0, null, "الحد الأقصى 50000 صف و200 عمود.")]);
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in Required.Concat(Optional))
            {
                var matches = sheet.Row(1).CellsUsed().Where(c => string.Equals(Header(c.GetString()), h, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length > 1) return new(rows, [new(1, null, $"عنوان مكرر: {h}.")]);
                if (matches.Length == 1) columns[h] = matches[0].Address.ColumnNumber;
            }
            var orderIds = new HashSet<(string DriverId, string OrderId)>();
            for (var n = 2; n <= last; n++)
            {
                ct.ThrowIfCancellationRequested();
                var row = sheet.Row(n);
                if (row.IsEmpty()) continue;
                string? id = null;
                try
                {
                    IXLCell? Cell(string h) => columns.TryGetValue(h, out var col) ? row.Cell(col) : null;
                    string Text(string h)
                    {
                        var cell = Cell(h);
                        if (cell is null || cell.IsEmpty()) return "";
                        if (cell.HasFormula) throw new FormatException($"لا يسمح بمعادلات في {h}؛ ارفع قيم التقرير الأصلية.");
                        return cell.DataType == XLDataType.Number
                            ? cell.GetDouble().ToString("0.################", CultureInfo.InvariantCulture)
                            : cell.GetString().Trim();
                    }
                    id = ChefzSpreadsheetParser.NormalizeId(Text("Driver ID"));
                    if (id.Length is 0 or > 100 || !id.All(c => c is >= '0' and <= '9'))
                        throw new FormatException("Driver ID يجب أن يكون رقماً صحيحاً.");
                    var dateCell = Cell("Date")!;
                    if (dateCell.HasFormula) throw new FormatException("لا يسمح بمعادلات في Date.");
                    DateOnly date;
                    if (dateCell.DataType == XLDataType.DateTime) date = DateOnly.FromDateTime(dateCell.GetDateTime());
                    else if (!DateOnly.TryParseExact(Text("Date"), ["yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "d/M/yyyy"],
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                        throw new FormatException("Date يجب أن يكون تاريخ Excel أو yyyy-MM-dd أو dd/MM/yyyy.");
                    if (date == default) throw new FormatException("تاريخ الطلب غير صالح.");
                    decimal Money(string h)
                    {
                        var text = Text(h);
                        if (text.Length == 0) return 0;
                        if (!decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowParentheses,
                            CultureInfo.InvariantCulture, out var value) || Math.Abs(value) > 1000000000m
                            || decimal.Round(value, 2) != value)
                            throw new FormatException($"قيمة غير صالحة في {h}؛ أدخل مبلغاً بحد أقصى منزلتين عشريتين.");
                        return value;
                    }
                    var orderId = Text("Chef Order ID");
                    if (orderId.Length > 100) throw new FormatException("Chef Order ID طويل جداً.");
                    var delivery = Money("Driver Delivery Fees"); var tips = Money("Driver Tip");
                    var compensation = Money("Driver Compensate"); var bonus = Money("Bonus Amount");
                    var collectedCash = Math.Abs(Money("Cash Collected By Driver"));
                    var deduction = Math.Abs(Money("Deduction Amount")) + collectedCash;
                    if (orderId.Length != 0 && !orderIds.Add((id, orderId)))
                        throw new FormatException("طلب مكرر لنفس Driver ID في الملف؛ لم تتم مضاعفة المبلغ.");
                    rows.Add(new(n, id, date, orderId, delivery, tips, compensation, bonus, deduction, collectedCash));
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                { issues.Add(new(n, id, e.Message)); }
            }
            if (rows.Count == 0 && issues.Count == 0) issues.Add(new(0, null, "لا توجد بيانات طلبات في الملف."));
        }
        catch (Exception e) when (e is InvalidDataException or IOException or ArgumentException or FormatException
            or System.Xml.XmlException or InvalidOperationException)
        { issues.Add(new(0, null, "تعذر قراءة ملف Excel؛ ارفع ملف xlsx صالحاً.")); }
        return new(rows, issues);
    }
}
