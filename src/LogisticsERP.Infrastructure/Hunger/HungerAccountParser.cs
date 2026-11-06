using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Hunger;

namespace LogisticsERP.Infrastructure.Hunger;

internal static class HungerAccountParser
{
    internal static (HungerAccountSeed[] Rows, string? Error) Parse(string fileName, byte[] content)
    {
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || content.Length is 0 or > HungerQualityParser.MaxBytes) return ([], "ارفع ملف xlsx بحد أقصى 10 ميجابايت.");
        try
        {
            using var book = new XLWorkbook(new MemoryStream(content));
            var sheet = book.Worksheets.FirstOrDefault(s => s.Name == "Sheet1");
            if (sheet is null) return ([], "ورقة Sheet1 مطلوبة؛ Sheet2 للمقارنة فقط.");
            var headers = sheet.Row(1).CellsUsed().ToLookup(c => c.GetString().Trim());
            if (headers.Any(h => h.Count() != 1)) return ([], "Duplicate column headers.");
            string[] required = ["رقم اليوزر", "رقم هوية اليوزر", "رقم الهوية1", "مدينة اليوزر", "حالة اليوزر"];
            if (required.Any(h => headers[h].Count() != 1)) return ([], "عناوين التأسيس المطلوبة مفقودة أو مكررة.");
            var rows = sheet.RowsUsed().Skip(1).ToArray();
            if (rows.Length is 0 or > HungerQualityParser.MaxRows) return ([], "عدد صفوف الملف غير صالح.");
            var result = new List<HungerAccountSeed>();
            foreach (var row in rows)
            {
                string Text(string name)
                {
                    var col = headers[name].SingleOrDefault(); if (col is null) return string.Empty;
                    var cell = row.Cell(col.Address.ColumnNumber);
                    if (cell.HasFormula || cell.DataType == XLDataType.Error || cell.GetString().Trim() == "#N/A")
                        throw new FormatException("Invalid source cell.");
                    return cell.GetString().Trim();
                }
                decimal? Money(string name)
                {
                    var text = Text(name);
                    if (text.Length == 0) return null;
                    if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || decimal.Round(value, 2) != value || Math.Abs(value) > 1000000000m)
                        throw new FormatException("Invalid source amount.");
                    return value;
                }
                var code = Text("رقم اليوزر");
                if (code.Length is 0 or > 150) return ([], $"Invalid account identifier at row {row.RowNumber()}.");
                DateOnly? date = null;
                if (headers["تاريخ الإستلام1"].SingleOrDefault() is { } dc)
                {
                    var cell = row.Cell(dc.Address.ColumnNumber);
                    if (!cell.IsEmpty())
                    {
                        if (cell.HasFormula || cell.DataType == XLDataType.Error || !cell.TryGetValue<DateTime>(out var dt) || dt.Year < 2000)
                            return ([], $"Invalid assignment date at row {row.RowNumber()}.");
                        date = DateOnly.FromDateTime(dt);
                    }
                }
                var ownership = Text("نوع الشريحة") switch { "شركة" or "شريحة شركة" => "Company", "خاصة" or "شريحة خاصة" => "Private", _ => "Unknown" };
                var orders = Money("استلام عدد الطلبات1");
                if (orders.HasValue && (orders < 0 || orders > int.MaxValue || decimal.Truncate(orders.Value) != orders))
                    return ([], $"Invalid opening orders at row {row.RowNumber()}.");
                result.Add(new(row.RowNumber(), code, Text("رقم هوية اليوزر"), Text("رقم الهوية1"), Text("رقم الهاتف"), Text("مدينة اليوزر"),
                    Text("سجل اليوزر"), Text("نظام التسوية"), Text("حالة اليوزر"), Text("نوع مركبة اليوزر"), ownership,
                    Text("نظام المحاسبة"), Text("نوع المركبة1"), date, Money("استلام المحفظة1"),
                    orders is >= 0 and <= int.MaxValue && decimal.Truncate(orders.Value) == orders ? (int)orders.Value : null));
            }
            return (result.ToArray(), result.Count == 0 ? "لا توجد حسابات في Sheet1." : null);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or FormatException or System.Xml.XmlException)
        { return ([], "تعذر قراءة بنية ملف التأسيس."); }
    }
}
