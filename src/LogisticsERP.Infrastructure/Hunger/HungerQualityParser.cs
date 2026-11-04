using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Hunger;

namespace LogisticsERP.Infrastructure.Hunger;

internal static class HungerQualityParser
{
    internal const int MaxBytes = 10 * 1024 * 1024;
    internal const int MaxRows = 10000;
    internal static readonly string[] Headers = ["rider_id", "gross_orders", "completed_orders", "completed_orders_in_time",
        "total_verification_requests", "successful_verification_requests", "failed_orders_by_rider", "segment",
        "verification_success_rate", "on_time_delivery_score", "final_delivery_quality_score"];
    internal sealed record Parsed(IReadOnlyList<HungerQualityInput> Inputs, IReadOnlyList<HungerImportRow> Errors, string? FileError);
    private static string Key(string text) => new(text.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    internal static Parsed Parse(string fileName, byte[] content)
    {
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || content.Length is 0 or > MaxBytes)
            return new([], [], "ارفع ملف xlsx لا يتجاوز 10 ميجابايت.");
        try
        {
            using var stream = new MemoryStream(content);
            using var book = new XLWorkbook(stream);
            var sheet = book.Worksheet(1);
            var cols = sheet.Row(1).CellsUsed().ToLookup(c => Key(c.GetString()));
            var aliases = new Dictionary<string, string[]> { ["rider_id"] = ["rider_id", "Working ID", "Account ID", "Captain ID", "معرف هنقر", "رقم اليوزر", "معرّف السائق"], ["segment"] = ["segment", "rider_segment", "التصنيف"] };
            var mapping = new Dictionary<string, int>();
            foreach (var header in Headers)
            {
                var found = (aliases.GetValueOrDefault(header) ?? [header]).SelectMany(a => cols[Key(a)]).Distinct().ToArray();
                if (found.Length != 1) return new([], [], $"العمود {header} مفقود أو مكرر.");
                mapping[header] = found[0].Address.ColumnNumber;
            }
            var rows = sheet.RowsUsed().Skip(1).ToArray();
            if (rows.Length is 0 or > MaxRows) return new([], [], "يجب أن يحتوي الملف من 1 إلى 10000 صف.");
            var inputs = new List<HungerQualityInput>(); var errors = new List<HungerImportRow>();
            foreach (var row in rows)
            {
                var account = row.Cell(mapping["rider_id"]).GetFormattedString().Trim();
                try
                {
                    if (string.IsNullOrWhiteSpace(account) || account.Length > 150) throw new FormatException("معرف هنقر مطلوب كنص.");
                    long Count(string header)
                    {
                        var cell = row.Cell(mapping[header]);
                        if (cell.HasFormula || !decimal.TryParse(cell.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                            || number < 0 || number > int.MaxValue || decimal.Truncate(number) != number)
                            throw new FormatException($"{header}: يجب إدخال عدد صحيح غير سالب.");
                        return (long)number;
                    }
                    decimal Rate(string header)
                    {
                        var cell = row.Cell(mapping[header]); var text = cell.GetString().Trim();
                        if (cell.HasFormula) throw new FormatException($"{header}: يلزم إدخال قيمة فعلية.");
                        var percent = text.EndsWith('%');
                        if (!decimal.TryParse(text.TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                            throw new FormatException($"{header}: نسبة غير صالحة.");
                        if (percent || number > 1) number /= 100;
                        if (number is < 0 or > 1) throw new FormatException($"{header}: النسبة خارج النطاق.");
                        return decimal.Round(number, 6);
                    }
                    var segment = row.Cell(mapping["segment"]).GetString().Trim().ToUpperInvariant();
                    if (segment.Length is 0 or > 50) throw new FormatException("التصنيف مطلوب ولا يتجاوز 50 حرفًا.");
                    var counters = new HungerQualityCounters(Count("gross_orders"), Count("completed_orders"), Count("completed_orders_in_time"),
                        Count("total_verification_requests"), Count("successful_verification_requests"), Count("failed_orders_by_rider"));
                    inputs.Add(new(row.RowNumber(), account, counters, segment, Rate("verification_success_rate"), Rate("on_time_delivery_score"), Rate("final_delivery_quality_score")));
                }
                catch (FormatException e) { errors.Add(new(row.RowNumber(), account, "Rejected", e.Message)); }
            }
            return new(inputs, errors, null);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or System.Xml.XmlException or FormatException)
        { return new([], [], "تعذر قراءة ملف Excel أو بنيته غير صالحة."); }
    }
}
