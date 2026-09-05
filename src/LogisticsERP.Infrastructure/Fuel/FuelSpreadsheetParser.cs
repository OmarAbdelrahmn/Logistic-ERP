using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Domain.Fuel;

namespace LogisticsERP.Infrastructure.Fuel;

internal sealed record ParsedFuelRowError(
    int RowNumber,
    string? CardNumber,
    string Code,
    string Message);

internal sealed record ParsedFuelCardTotal(
    int FirstRowNumber,
    string CardNumber,
    string NormalizedCardNumber,
    FuelCardIdentifierType IdentifierType,
    string? PlateNumberText,
    string? FuelType,
    decimal TotalLiters,
    decimal TotalAmount,
    decimal? AmountBeforeTax,
    decimal? VatAmount,
    int? TransactionCount,
    DateTimeOffset? FirstTransactionAtUtc,
    DateTimeOffset? LastTransactionAtUtc);

internal sealed record ParsedFuelReport(
    FuelCardProvider Provider,
    DateOnly ReportMonth,
    DateTimeOffset? ReportThroughAtUtc,
    int SourceRows,
    IReadOnlyList<ParsedFuelCardTotal> Cards,
    IReadOnlyList<ParsedFuelRowError> Errors);

internal static class FuelSpreadsheetParser
{
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    public static ParsedFuelReport Parse(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new InvalidDataException("The fuel report stream must be seekable.");
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
        {
            LeaveOpen = true,
            FallbackEncoding = Encoding.GetEncoding(1256)
        });

        do
        {
            var rows = ReadWorksheet(reader);
            if (TryFindDetailedHeader(rows, out var detailedHeader))
            {
                return ParsePetroApp(rows, detailedHeader);
            }

            if (TryFindSummaryHeader(rows, out var summaryHeader))
            {
                return ParseSayaraApp(rows, summaryHeader);
            }
        } while (reader.NextResult());

        throw new InvalidDataException("The workbook does not match a supported fuel report.");
    }

    private static List<WorksheetRow> ReadWorksheet(IExcelDataReader reader)
    {
        var rows = new List<WorksheetRow>();
        var rowNumber = 0;
        while (reader.Read())
        {
            rowNumber++;
            var values = new object?[reader.FieldCount];
            for (var index = 0; index < reader.FieldCount; index++)
            {
                values[index] = reader.GetValue(index);
            }

            rows.Add(new WorksheetRow(rowNumber, values));
        }

        return rows;
    }

    private static ParsedFuelReport ParsePetroApp(IReadOnlyList<WorksheetRow> rows, HeaderMap header)
    {
        var aggregates = new Dictionary<string, MutableCardTotal>(StringComparer.Ordinal);
        var errors = new List<ParsedFuelRowError>();
        var seenInvoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DateOnly? reportMonth = null;
        var sourceRows = 0;

        foreach (var row in rows.Where(x => x.Number > header.RowNumber))
        {
            var vehicle = TextAt(row, header.VehicleColumn);
            var internalNumber = TextAt(row, header.InternalNumberColumn);
            var cardNumber = IsMeaningful(internalNumber) ? internalNumber! : vehicle;
            var invoiceNumber = TextAt(row, header.InvoiceColumn);
            var litersValue = ValueAt(row, header.LitersColumn);
            var amountValue = ValueAt(row, header.AmountColumn);
            var dateValue = ValueAt(row, header.DateColumn);
            if (!IsMeaningful(cardNumber)
                && !IsMeaningful(invoiceNumber)
                && litersValue is null
                && amountValue is null
                && dateValue is null)
            {
                continue;
            }
            if ((!IsMeaningful(invoiceNumber) || !IsMeaningful(cardNumber)) && dateValue is null)
            {
                // Provider footer totals are not transaction rows.
                continue;
            }

            sourceRows++;
            if (!IsMeaningful(cardNumber))
            {
                errors.Add(new ParsedFuelRowError(row.Number, null, "missing_card_number", "رقم البطاقة أو اللوحة مفقود."));
                continue;
            }

            if (IsMeaningful(invoiceNumber) && !seenInvoices.Add(invoiceNumber!))
            {
                errors.Add(new ParsedFuelRowError(row.Number, cardNumber, "duplicate_invoice", "رقم الفاتورة مكرر داخل الملف."));
                continue;
            }

            if (!TryFuelAmounts(litersValue, amountValue, out var liters, out var amount))
            {
                errors.Add(new ParsedFuelRowError(row.Number, cardNumber, "invalid_amount", "تعذر قراءة اللترات أو التكلفة من الصف."));
                continue;
            }

            if (!TryTransactionDate(dateValue, out var occurredAtUtc))
            {
                errors.Add(new ParsedFuelRowError(row.Number, cardNumber, "invalid_date", "تعذر قراءة تاريخ حركة الوقود."));
                continue;
            }

            var rowMonth = new DateOnly(occurredAtUtc.ToOffset(RiyadhOffset).Year, occurredAtUtc.ToOffset(RiyadhOffset).Month, 1);
            reportMonth ??= rowMonth;
            if (reportMonth.Value != rowMonth)
            {
                throw new InvalidDataException("A detailed fuel report must contain one calendar month only.");
            }

            var identifierType = FuelCardRules.DetectIdentifierType(cardNumber!);
            var normalizedCardNumber = FuelCardRules.NormalizeCardNumber(cardNumber!, identifierType);
            if (!aggregates.TryGetValue(normalizedCardNumber, out var aggregate))
            {
                aggregate = new MutableCardTotal(
                    row.Number,
                    cardNumber!.Trim(),
                    normalizedCardNumber,
                    identifierType,
                    vehicle,
                    CleanFuelType(TextAt(row, header.FuelTypeColumn)));
                aggregates.Add(normalizedCardNumber, aggregate);
            }

            aggregate.TotalLiters += liters;
            aggregate.TotalAmount += amount;
            if (TryDecimal(ValueAt(row, header.AmountBeforeTaxColumn), out var amountBeforeTax) && amountBeforeTax >= 0)
            {
                aggregate.AmountBeforeTax = (aggregate.AmountBeforeTax ?? 0m) + amountBeforeTax;
            }
            aggregate.TransactionCount = (aggregate.TransactionCount ?? 0) + 1;
            aggregate.FirstTransactionAtUtc = Min(aggregate.FirstTransactionAtUtc, occurredAtUtc);
            aggregate.LastTransactionAtUtc = Max(aggregate.LastTransactionAtUtc, occurredAtUtc);
        }

        if (!reportMonth.HasValue || aggregates.Count == 0)
        {
            throw new InvalidDataException("The detailed fuel report contains no valid transaction rows.");
        }

        foreach (var aggregate in aggregates.Values.Where(x => x.AmountBeforeTax.HasValue))
        {
            aggregate.VatAmount = Math.Max(0m, aggregate.TotalAmount - aggregate.AmountBeforeTax!.Value);
        }

        return new ParsedFuelReport(
            FuelCardProvider.PetroApp,
            reportMonth.Value,
            aggregates.Values.Max(x => x.LastTransactionAtUtc),
            sourceRows,
            aggregates.Values.Select(x => x.ToRecord()).ToArray(),
            errors);
    }

    private static ParsedFuelReport ParseSayaraApp(IReadOnlyList<WorksheetRow> rows, HeaderMap header)
    {
        var reportDate = rows
            .Where(x => x.Number < header.RowNumber)
            .SelectMany(x => x.Values)
            .Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => TryReportDate(value!, out var parsed) ? parsed : (DateTimeOffset?)null)
            .FirstOrDefault(value => value.HasValue)
            ?? throw new InvalidDataException("The summary fuel report date is missing.");
        var localReportDate = reportDate.ToOffset(RiyadhOffset);
        var reportMonth = new DateOnly(localReportDate.Year, localReportDate.Month, 1);
        var aggregates = new Dictionary<string, MutableCardTotal>(StringComparer.Ordinal);
        var errors = new List<ParsedFuelRowError>();
        var sourceRows = 0;

        foreach (var row in rows.Where(x => x.Number > header.RowNumber))
        {
            var plate = TextAt(row, header.VehicleColumn);
            var internalNumber = TextAt(row, header.InternalNumberColumn);
            var cardNumber = IsMeaningful(internalNumber) ? internalNumber! : plate;
            var litersValue = ValueAt(row, header.LitersColumn);
            var amountValue = ValueAt(row, header.AmountColumn);
            if (!IsMeaningful(cardNumber) && litersValue is null && amountValue is null)
            {
                continue;
            }

            sourceRows++;
            if (!IsMeaningful(cardNumber))
            {
                errors.Add(new ParsedFuelRowError(row.Number, null, "missing_card_number", "رقم البطاقة أو اللوحة مفقود."));
                continue;
            }
            if (!TryFuelAmounts(litersValue, amountValue, out var liters, out var amount))
            {
                errors.Add(new ParsedFuelRowError(row.Number, cardNumber, "invalid_amount", "تعذر قراءة اللترات أو التكلفة من الصف."));
                continue;
            }

            var identifierType = FuelCardRules.DetectIdentifierType(cardNumber!);
            var normalizedCardNumber = FuelCardRules.NormalizeCardNumber(cardNumber!, identifierType);
            if (!aggregates.TryGetValue(normalizedCardNumber, out var aggregate))
            {
                aggregate = new MutableCardTotal(
                    row.Number,
                    cardNumber!.Trim(),
                    normalizedCardNumber,
                    identifierType,
                    plate,
                    CleanFuelType(TextAt(row, header.FuelTypeColumn)));
                aggregates.Add(normalizedCardNumber, aggregate);
            }

            aggregate.TotalLiters += liters;
            aggregate.TotalAmount += amount;
        }

        if (aggregates.Count == 0)
        {
            throw new InvalidDataException("The summary fuel report contains no valid card rows.");
        }

        return new ParsedFuelReport(
            FuelCardProvider.SayaraApp,
            reportMonth,
            reportDate,
            sourceRows,
            aggregates.Values.Select(x => x.ToRecord()).ToArray(),
            errors);
    }

    private static bool TryFindDetailedHeader(IReadOnlyList<WorksheetRow> rows, out HeaderMap header)
    {
        foreach (var row in rows.Take(20))
        {
            var values = row.Values.Select(value => CanonicalHeader(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)).ToArray();
            var invoice = Find(values, "رقمالفاتورة");
            var vehicle = Find(values, "المركبة");
            var liters = Find(values, "عدداللترات");
            var amount = Find(values, "التكلفة");
            var date = Find(values, "التاريخ");
            if (invoice >= 0 && vehicle >= 0 && liters >= 0 && amount >= 0 && date >= 0)
            {
                header = new HeaderMap(
                    row.Number, invoice, vehicle, Find(values, "الرقمالداخلي"), liters, amount,
                    Find(values, "التكلفةقبلالضريبة"), Find(values, "نوعالوقود"), date);
                return true;
            }
        }

        header = default;
        return false;
    }

    private static bool TryFindSummaryHeader(IReadOnlyList<WorksheetRow> rows, out HeaderMap header)
    {
        foreach (var row in rows.Take(20))
        {
            var values = row.Values.Select(value => CanonicalHeader(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)).ToArray();
            var vehicle = Find(values, "لوحةالمركبة");
            var liters = Find(values, "الاستهلاكبالليتر", "الاستهلاكباللتر");
            var amount = Find(values, "التكلفة");
            if (vehicle >= 0 && liters >= 0 && amount >= 0)
            {
                header = new HeaderMap(
                    row.Number, -1, vehicle, Find(values, "رقمالمركبةالداخلي"), liters, amount,
                    -1, Find(values, "نوعالوقود"), -1);
                return true;
            }
        }

        header = default;
        return false;
    }

    internal static bool TryReportDate(string value, out DateTimeOffset result)
    {
        result = default;
        var normalized = NormalizeText(value);
        var match = Regex.Match(
            normalized,
            @"(?<d>\d{1,2})/(?<m>\d{1,2})/(?<y>\d{4})(?:\s+(?<h>\d{1,2}):(?<min>\d{1,2})(?::(?<s>\d{1,2}))?\s*(?<ap>[صم])?)?",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        if (!match.Success)
        {
            return false;
        }

        var day = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
        var year = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
        var hour = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) : 0;
        var minute = match.Groups["min"].Success ? int.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture) : 0;
        var second = match.Groups["s"].Success ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;
        var amPm = match.Groups["ap"].Value;
        if (amPm == "م" && hour < 12) hour += 12;
        if (amPm == "ص" && hour == 12) hour = 0;

        try
        {
            result = new DateTimeOffset(year, month, day, hour, minute, second, RiyadhOffset).ToUniversalTime();
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryTransactionDate(object? value, out DateTimeOffset result)
    {
        if (value is DateTimeOffset offset)
        {
            result = offset.ToUniversalTime();
            return true;
        }
        if (value is DateTime dateTime)
        {
            result = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), RiyadhOffset).ToUniversalTime();
            return true;
        }

        var text = NormalizeText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        if (DateTime.TryParseExact(
            text,
            ["yyyy-MM-dd HH:mm:ss", "yyyy/M/d H:mm:ss", "d/M/yyyy H:mm:ss"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var parsed))
        {
            result = new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), RiyadhOffset).ToUniversalTime();
            return true;
        }

        result = default;
        return false;
    }

    private static bool TryDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case decimal decimalValue:
                result = decimalValue;
                return true;
            case double doubleValue when double.IsFinite(doubleValue):
                result = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
                return true;
            case float floatValue when float.IsFinite(floatValue):
                result = Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture);
                return true;
            case int intValue:
                result = intValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
        }

        var text = NormalizeText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
            .Replace("٬", string.Empty, StringComparison.Ordinal)
            .Replace('٫', '.')
            .Replace(',', '.');
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryFuelAmounts(object? litersValue, object? amountValue, out decimal liters, out decimal amount)
    {
        liters = 0m;
        if (!TryDecimal(amountValue, out amount) || amount < 0)
        {
            return false;
        }

        if (TryDecimal(litersValue, out liters))
        {
            return liters >= 0;
        }

        // Some detailed exports contain a zero-value authorization row with no liters.
        return litersValue is null && amount == 0m;
    }

    private static string NormalizeText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character switch
            {
                '\u0660' => '0', '\u0661' => '1', '\u0662' => '2', '\u0663' => '3', '\u0664' => '4',
                '\u0665' => '5', '\u0666' => '6', '\u0667' => '7', '\u0668' => '8', '\u0669' => '9',
                '\u06F0' => '0', '\u06F1' => '1', '\u06F2' => '2', '\u06F3' => '3', '\u06F4' => '4',
                '\u06F5' => '5', '\u06F6' => '6', '\u06F7' => '7', '\u06F8' => '8', '\u06F9' => '9',
                '\u00A0' or '\u202F' => ' ',
                _ => character
            });
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static string CanonicalHeader(string value) =>
        new(NormalizeText(value).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static int Find(string[] values, params string[] names)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (names.Contains(values[index], StringComparer.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static object? ValueAt(WorksheetRow row, int index) =>
        index >= 0 && index < row.Values.Length ? row.Values[index] : null;

    private static string? TextAt(WorksheetRow row, int index)
    {
        var text = Convert.ToString(ValueAt(row, index), CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static bool IsMeaningful(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim() is not "-" and not "_";

    private static string? CleanFuelType(string? value) =>
        IsMeaningful(value) ? value!.Trim().TrimStart('_') : null;

    private static DateTimeOffset Min(DateTimeOffset? current, DateTimeOffset value) =>
        !current.HasValue || value < current.Value ? value : current.Value;

    private static DateTimeOffset Max(DateTimeOffset? current, DateTimeOffset value) =>
        !current.HasValue || value > current.Value ? value : current.Value;

    private sealed record WorksheetRow(int Number, object?[] Values);

    private readonly record struct HeaderMap(
        int RowNumber,
        int InvoiceColumn,
        int VehicleColumn,
        int InternalNumberColumn,
        int LitersColumn,
        int AmountColumn,
        int AmountBeforeTaxColumn,
        int FuelTypeColumn,
        int DateColumn);

    private sealed class MutableCardTotal(
        int firstRowNumber,
        string cardNumber,
        string normalizedCardNumber,
        FuelCardIdentifierType identifierType,
        string? plateNumberText,
        string? fuelType)
    {
        public int FirstRowNumber { get; } = firstRowNumber;
        public string CardNumber { get; } = cardNumber;
        public string NormalizedCardNumber { get; } = normalizedCardNumber;
        public FuelCardIdentifierType IdentifierType { get; } = identifierType;
        public string? PlateNumberText { get; } = plateNumberText;
        public string? FuelType { get; } = fuelType;
        public decimal TotalLiters { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? AmountBeforeTax { get; set; }
        public decimal? VatAmount { get; set; }
        public int? TransactionCount { get; set; }
        public DateTimeOffset? FirstTransactionAtUtc { get; set; }
        public DateTimeOffset? LastTransactionAtUtc { get; set; }

        public ParsedFuelCardTotal ToRecord() => new(
            FirstRowNumber,
            CardNumber,
            NormalizedCardNumber,
            IdentifierType,
            PlateNumberText,
            FuelType,
            decimal.Round(TotalLiters, 3, MidpointRounding.AwayFromZero),
            decimal.Round(TotalAmount, 2, MidpointRounding.AwayFromZero),
            AmountBeforeTax.HasValue ? decimal.Round(AmountBeforeTax.Value, 2, MidpointRounding.AwayFromZero) : null,
            VatAmount.HasValue ? decimal.Round(VatAmount.Value, 2, MidpointRounding.AwayFromZero) : null,
            TransactionCount,
            FirstTransactionAtUtc,
            LastTransactionAtUtc);
    }
}
