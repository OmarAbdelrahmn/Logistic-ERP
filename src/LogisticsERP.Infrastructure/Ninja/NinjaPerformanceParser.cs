using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Ninja;
using Microsoft.VisualBasic.FileIO;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed record NinjaPerformanceInput(int RowNumber, string CaptainId, DateOnly Date, int Orders, decimal Hours);
internal sealed record NinjaPerformanceParse(IReadOnlyList<NinjaPerformanceInput> Rows, IReadOnlyList<NinjaImportIssue> Issues);

internal static class NinjaPerformanceParser
{
    private static readonly string[] RequiredHeaders = ["Captain ID", "Total Orders", "Total Working Hours"];
    public static NinjaPerformanceParse Parse(string name, byte[] content, NinjaPerformanceImportOptions options, DateOnly today, CancellationToken ct)
    {
        var rows = new List<NinjaPerformanceInput>(); var issues = new List<NinjaImportIssue>();
        if (content.Length is 0 or > NinjaSpreadsheetParser.MaxBytes || string.IsNullOrWhiteSpace(name) || name.Length > 255
            || options.Mode is not ("Daily" or "MultiDay") || options.HoursUnit is not ("DecimalHours" or "Duration" or "ExcelDays")
            || options.Mode == "Daily" && (options.ReportDate is null || options.ReportDate == default(DateOnly) || options.ReportDate > today)
            || options.Mode == "MultiDay" && options.ReportDate is not null
            || options.DateFormat is not ("yyyy-MM-dd" or "dd/MM/yyyy" or "MM/dd/yyyy"))
            return new(rows, [new(0, null, "حدد نوع الملف ووحدة الساعات وتاريخاً صريحاً للملف اليومي. الحد 10 ميجابايت.")]);
        var seen = new HashSet<(string, DateOnly)>();
        void Add(int number, string id, string orders, string hours, string? dateText, DateOnly? excelDate = null)
        {
            id = NinjaSpreadsheetParser.NormalizeId(id);
            var date = options.ReportDate ?? excelDate ?? (DateOnly.TryParseExact(dateText, options.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ? parsedDate : default);
            if (id.Length is 0 or > 150 || id.Any(c => c is < '0' or > '9') || date == default || date > today)
                throw new FormatException("Captain ID أو تاريخ العمل غير صالح؛ تاريخ تنزيل الملف ليس تاريخ الأداء.");
            if (!int.TryParse(orders, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0)
                throw new FormatException("الطلبات عدد صحيح غير سالب.");
            var workingHours = Hours(hours, options.HoursUnit);
            if (workingHours is < 0 or > 24) throw new FormatException("ساعات اليوم بين 0 و24.");
            if (!seen.Add((id, date))) throw new FormatException("معرف وتاريخ مكرران داخل الملف.");
            rows.Add(new(number, id, date, count, decimal.Round(workingHours, 8, MidpointRounding.AwayFromZero)));
        }
        try
        {
            using var stream = new MemoryStream(content);
            if (name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                if (options.Delimiter is not ("," or ";" or "\t") || options.Encoding is not ("utf-8" or "windows-1256"))
                    throw new FormatException("حدد فاصلاً وترميزاً مدعومين صراحة.");
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var encoding = options.Encoding == "utf-8" ? new UTF8Encoding(false, true) : Encoding.GetEncoding(1256, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                using var parser = new TextFieldParser(stream, encoding, true) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
                parser.SetDelimiters(options.Delimiter);
                var headers = parser.ReadFields() ?? [];
                var columns = Columns(headers, options.Mode == "MultiDay");
                var number = 1;
                while (!parser.EndOfData)
                {
                    ct.ThrowIfCancellationRequested(); number++;
                    if (number > 10001) throw new FormatException("الحد 10000 صف.");
                    var fields = parser.ReadFields() ?? [];
                    if (fields.Length == 0 || fields.All(string.IsNullOrWhiteSpace)) continue;
                    try
                    {
                        if (fields.Length != headers.Length) throw new FormatException("عدد الأعمدة لا يطابق العناوين.");
                        Add(number, fields[columns["Captain ID"]], fields[columns["Total Orders"]], fields[columns["Total Working Hours"]],
                            columns.TryGetValue("Date", out var column) ? fields[column] : null);
                    }
                    catch (FormatException e) { issues.Add(new(number, null, e.Message)); }
                }
            }
            else if (name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
                    if (zip.Entries.Count > 2000 || zip.Entries.Sum(e => e.Length) > 40 * 1024 * 1024) throw new FormatException("محتوى Excel يتجاوز الحد.");
                stream.Position = 0;
                using var book = new XLWorkbook(stream);
                var sheets = book.Worksheets.Where(s => RequiredHeaders.All(h => s.Row(1).CellsUsed().Any(c => string.Equals(c.GetString().Trim(), h, StringComparison.OrdinalIgnoreCase)))).ToArray();
                if (sheets.Length != 1) throw new FormatException("يجب وجود ورقة واحدة بعناوين الأداء.");
                var sheet = sheets[0]; var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
                if (lastColumn > 200 || (sheet.LastRowUsed()?.RowNumber() ?? 0) > 10001) throw new FormatException("الحد 10000 صف و200 عمود.");
                var columns = Columns(Enumerable.Range(1, lastColumn).Select(i => sheet.Cell(1, i).GetString()).ToArray(), options.Mode == "MultiDay");
                for (var number = 2; number <= (sheet.LastRowUsed()?.RowNumber() ?? 1); number++)
                {
                    ct.ThrowIfCancellationRequested(); if (sheet.Row(number).IsEmpty()) continue;
                    try
                    {
                        if (columns.Values.Any(i => sheet.Cell(number, i + 1).HasFormula)) throw new FormatException("حقول الاستيراد قيم دون صيغ.");
                        string Value(string header) => sheet.Cell(number, columns[header] + 1).GetString().Trim();
                        var hourCell = sheet.Cell(number, columns["Total Working Hours"] + 1);
                        var hours = hourCell.DataType == XLDataType.TimeSpan
                            ? ((decimal)hourCell.GetTimeSpan().Ticks / TimeSpan.TicksPerHour).ToString(CultureInfo.InvariantCulture) : Value("Total Working Hours");
                        // A typed Excel duration is converted to the explicitly selected input unit before normalization.
                        if (hourCell.DataType == XLDataType.TimeSpan && options.HoursUnit == "Duration") hours = hourCell.GetTimeSpan().ToString("c", CultureInfo.InvariantCulture);
                        if (hourCell.DataType == XLDataType.TimeSpan && options.HoursUnit == "ExcelDays") hours = ((decimal)hourCell.GetTimeSpan().Ticks / TimeSpan.TicksPerDay).ToString(CultureInfo.InvariantCulture);
                        var dateCell = columns.TryGetValue("Date", out var dateColumn) ? sheet.Cell(number, dateColumn + 1) : null;
                        Add(number, Value("Captain ID"), Value("Total Orders"), hours, dateCell?.GetString().Trim(),
                            dateCell?.DataType == XLDataType.DateTime ? DateOnly.FromDateTime(dateCell.GetDateTime()) : null);
                    }
                    catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException) { issues.Add(new(number, null, e.Message)); }
                }
            }
            else throw new FormatException("ارفع ملف CSV أو XLSX.");
        }
        catch (Exception e) when (e is FormatException or InvalidDataException or IOException or ArgumentException or InvalidOperationException or System.Xml.XmlException or MalformedLineException)
        { issues.Add(new(0, null, e is FormatException ? e.Message : "تعذر قراءة الملف وفق الخيارات المحددة.")); }
        if (rows.Count == 0 && issues.Count == 0) issues.Add(new(0, null, "الملف لا يحتوي على بيانات."));
        return new(rows, issues);
    }

    private static Dictionary<string, int> Columns(string[] headers, bool multiple)
    {
        if (headers.Length > 200) throw new FormatException("الحد 200 عمود.");
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in multiple ? RequiredHeaders.Append("Date") : RequiredHeaders)
        {
            var indexes = headers.Select((h, i) => (Header: h.Trim().TrimStart('\uFEFF'), Index: i)).Where(x => string.Equals(x.Header, header, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (indexes.Length != 1) throw new FormatException($"عنوان مفقود أو مكرر: {header}.");
            map[header] = indexes[0].Index;
        }
        if (!multiple && headers.Any(h => string.Equals(h.Trim(), "Date", StringComparison.OrdinalIgnoreCase)))
            throw new FormatException("الملف يحتوي Date؛ اختر MultiDay لعدم إسناد تاريخ واحد لجميع الصفوف.");
        return map;
    }
    private static decimal Hours(string text, string unit)
    {
        if (unit == "Duration")
        {
            if (!TimeSpan.TryParseExact(text, "c", CultureInfo.InvariantCulture, out var time))
            {
                if (text == "24:00:00") return 24;
                throw new FormatException("المدة بصيغة HH:MM:SS.");
            }
            return (decimal)time.Ticks / TimeSpan.TicksPerHour;
        }
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            throw new FormatException("الساعات رقم عشري بوحدة واضحة.");
        return unit == "ExcelDays" ? value * 24 : value;
    }
}
