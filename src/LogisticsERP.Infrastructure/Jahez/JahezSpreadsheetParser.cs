using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Infrastructure.Jahez;

internal static class JahezSpreadsheetParser
{
    private static readonly string[] TransactionHeaders = ["Driver ID", "Date", "Delivery Price", "Cash Amount", "Net Amount", "Driver Adjustment"];
    private static readonly string[] DispatchHeaders = ["Driver ID", "From", "To", "Number Of Dispatches"];
    private static readonly string[] TransactionDateFormats = ["M/d/yyyy h:mm:ss tt", "M/d/yyyy H:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss"];
    private static readonly string[] DailyDateFormats = ["dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd/MM/yyyy"];

    public static IReadOnlyList<JahezImportRow> Parse(byte[] content, Guid fileId, JahezImportKind kind)
    {
        using var stream = new MemoryStream(content, false);
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            if (archive.Entries.Count > 5000 || archive.Entries.Sum(x => x.Length) > 100 * 1024 * 1024)
                throw new InvalidDataException("محتوى Excel يتجاوز الحد المسموح بعد فك الضغط.");
        }
        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        var sheetName = kind == JahezImportKind.Transactions ? "SDP_Report" : "Delivery Insights Report";
        if (!workbook.TryGetWorksheet(sheetName, out var sheet)) throw new InvalidDataException($"ورقة {sheetName} غير موجودة.");
        var headers = kind == JahezImportKind.Transactions ? TransactionHeaders : DispatchHeaders;
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in sheet.Row(1).CellsUsed())
        {
            if (c.HasFormula) throw new InvalidDataException("رؤوس الأعمدة يجب أن تكون نصوصًا.");
            if (!columns.TryAdd(c.GetString().Trim(), c.Address.ColumnNumber)) throw new InvalidDataException("رؤوس الأعمدة مكررة.");
        }
        if (headers.Any(h => !columns.ContainsKey(h))) throw new InvalidDataException("أعمدة التقرير المطلوبة غير مكتملة.");
        var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
        if (last > 50001) throw new InvalidDataException("أقصى حجم للملف 50000 صف.");
        List<JahezImportRow> rows = [];
        for (var number = 2; number <= last; number++)
        {
            var cells = headers.Select(h => sheet.Cell(number, columns[h])).ToArray();
            if (cells.All(c => c.IsEmpty())) continue;
            var row = new JahezImportRow { FileId = fileId, RowNumber = number,
                RawValuesJson = JsonSerializer.Serialize(cells.Select(c => c.HasFormula ? c.FormulaA1 : c.Value.ToString(CultureInfo.InvariantCulture)).ToArray()) };
            try
            {
                if (cells.Any(c => c.HasFormula)) throw new FormatException("أعمدة البيانات لا تقبل صيغ Excel؛ صدّر القيم الأصلية.");
                row.DriverId = cells[0].Value.ToString(CultureInfo.InvariantCulture).Trim();
                if (row.DriverId.Length is 0 or > 150) throw new FormatException("رقم الحساب الخارجي غير صالح.");
                var date = Date(cells[1], kind == JahezImportKind.Transactions ? TransactionDateFormats : DailyDateFormats);
                row.OccurredAtUtc = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), TimeSpan.FromHours(3)).ToUniversalTime();
                if (kind == JahezImportKind.Transactions)
                {
                    row.DeliveryPrice = Amount(cells[2]); row.CashAmount = Amount(cells[3]);
                    row.NetAmount = Amount(cells[4]); row.DriverAdjustment = Amount(cells[5]);
                }
                else
                {
                    row.ToDate = DateOnly.FromDateTime(Date(cells[2], DailyDateFormats));
                    var count = Amount(cells[3]);
                    if (count < 0 || count > int.MaxValue || count != decimal.Truncate(count)) throw new FormatException("عدد الطلبات يجب أن يكون عددًا صحيحًا غير سالب.");
                    row.Dispatches = (int)count;
                }
            }
            catch (FormatException exception) { row.ParseError = exception.Message; }
            rows.Add(row);
        }
        if (rows.Count == 0) throw new InvalidDataException("التقرير لا يحتوي بيانات.");
        return rows;
    }

    private static decimal Amount(IXLCell cell)
    {
        var value = cell.Value.ToString(CultureInfo.InvariantCulture).Trim();
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            || Math.Abs(amount) > 1_000_000_000m || Math.Round(amount, 6) != amount) throw new FormatException("المبلغ غير صالح أو تتجاوز دقته ست منازل عشرية.");
        return amount;
    }

    private static DateTime Date(IXLCell cell, string[] formats)
    {
        if (cell.DataType == XLDataType.DateTime)
        {
            var typed = cell.GetDateTime();
            if (typed.Year is < 2000 or > 2100) throw new FormatException("صيغة التاريخ غير صالحة.");
            return typed;
        }
        var text = cell.Value.ToString(CultureInfo.InvariantCulture).Replace('\u202f', ' ').Replace('\u00a0', ' ').Trim();
        if (!DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
            || date.Year is < 2000 or > 2100) throw new FormatException("صيغة التاريخ غير صالحة.");
        return date;
    }
}
