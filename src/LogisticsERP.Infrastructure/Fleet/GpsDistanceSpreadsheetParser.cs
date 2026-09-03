using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed record ParsedGpsDistanceRow(
    int RowNumber,
    string PlateNumber,
    decimal? DistanceKm,
    bool HasGpsDistance,
    string? ErrorCode,
    string? ErrorMessage);

internal sealed record ParsedGpsDistanceReport(
    DateOnly WorkDate,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc,
    IReadOnlyList<ParsedGpsDistanceRow> Rows);

internal sealed class GpsFramesetMissingSheetException()
    : Exception("The Excel HTML frameset does not contain its companion sheet file.");

internal static partial class GpsDistanceSpreadsheetParser
{
    private static readonly CultureInfo SaudiCulture = CultureInfo.GetCultureInfo("ar-SA");

    public static ParsedGpsDistanceReport Parse(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new InvalidDataException("The GPS report stream must be seekable.");
        }

        var originalPosition = stream.Position;
        Span<byte> prefix = stackalloc byte[256];
        var read = stream.Read(prefix);
        stream.Position = originalPosition;
        return LooksLikeHtml(prefix[..read])
            ? ParseHtmlExport(stream)
            : ParseWorkbook(stream);
    }

    public static ParsedGpsDistanceReport ParseArchive(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sheet = archive.Entries
            .Where(entry => entry.Length > 0 && entry.Length <= 10 * 1024 * 1024)
            .Where(entry => Path.GetExtension(entry.Name).Equals(".htm", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(entry.Name).Equals(".html", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.Name.StartsWith("sheet", StringComparison.OrdinalIgnoreCase))
            .ThenBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (sheet is null)
        {
            throw new InvalidDataException("The GPS archive does not contain an HTML worksheet.");
        }

        using var entryStream = sheet.Open();
        using var worksheet = new MemoryStream((int)sheet.Length);
        entryStream.CopyTo(worksheet);
        worksheet.Position = 0;
        return Parse(worksheet);
    }

    private static ParsedGpsDistanceReport ParseWorkbook(Stream stream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
        {
            LeaveOpen = true,
            FallbackEncoding = Encoding.GetEncoding(1256)
        });

        DateTimeOffset? periodStart = null;
        DateTimeOffset? periodEnd = null;
        var rows = new List<ParsedGpsDistanceRow>();
        var headerFound = false;
        var plateColumn = -1;
        var distanceColumn = -1;
        var rowNumber = 0;

        do
        {
            while (reader.Read())
            {
                rowNumber++;
                var values = Enumerable.Range(0, reader.FieldCount)
                    .Select(index => Convert.ToString(reader.GetValue(index), SaudiCulture)?.Trim() ?? string.Empty)
                    .ToArray();

                if (!periodStart.HasValue)
                {
                    var periodText = string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)));
                    if (TryParsePeriod(periodText, out var parsedStart, out var parsedEnd))
                    {
                        periodStart = parsedStart;
                        periodEnd = parsedEnd;
                    }
                }

                if (!headerFound)
                {
                    plateColumn = Array.FindIndex(values, IsVehicleHeader);
                    distanceColumn = Array.FindIndex(values, IsDistanceHeader);
                    headerFound = plateColumn >= 0 && distanceColumn >= 0;
                    continue;
                }

                var plate = ValueAt(values, plateColumn);
                var distanceText = ValueAt(values, distanceColumn);
                if (string.IsNullOrWhiteSpace(plate))
                {
                    continue;
                }

                if (TryParseDistance(distanceText, out var distance))
                {
                    rows.Add(new ParsedGpsDistanceRow(rowNumber, plate, distance, true, null, null));
                    continue;
                }

                if (IsNoGpsValue(distanceText))
                {
                    rows.Add(new ParsedGpsDistanceRow(rowNumber, plate, null, false, null, null));
                    continue;
                }

                rows.Add(new ParsedGpsDistanceRow(
                    rowNumber,
                    plate,
                    null,
                    false,
                    "invalid_distance",
                    "تعذر قراءة مسافة GPS من الصف."));
            }
        } while (reader.NextResult() && !headerFound);

        if (!headerFound || !periodStart.HasValue || rows.Count == 0)
        {
            throw new InvalidDataException("The workbook does not contain the expected GPS report structure.");
        }

        return new ParsedGpsDistanceReport(
            DateOnly.FromDateTime(periodStart.Value.ToOffset(TimeSpan.FromHours(3)).DateTime),
            periodStart,
            periodEnd,
            rows);
    }

    private static ParsedGpsDistanceReport ParseHtmlExport(Stream stream)
    {
        var html = ReadHtml(stream);
        if (html.Contains("<frameset", StringComparison.OrdinalIgnoreCase)
            || html.Contains("<frame ", StringComparison.OrdinalIgnoreCase)
                && html.Contains("sheet001.htm", StringComparison.OrdinalIgnoreCase))
        {
            throw new GpsFramesetMissingSheetException();
        }

        if (!TryParsePeriod(html, out var periodStart, out var periodEnd))
        {
            throw new InvalidDataException("The HTML export does not contain a valid report period.");
        }

        var rows = new List<ParsedGpsDistanceRow>();
        var headerFound = false;
        var plateColumn = -1;
        var distanceColumn = -1;
        var reportRowNumber = 0;
        foreach (Match rowMatch in HtmlRowRegex().Matches(html))
        {
            reportRowNumber++;
            var cells = HtmlCellRegex().Matches(rowMatch.Groups[1].Value)
                .Select(match => NormalizeText(WebUtility.HtmlDecode(
                    HtmlTagRegex().Replace(match.Groups[1].Value, string.Empty))))
                .ToArray();
            if (cells.Length == 0)
            {
                continue;
            }

            if (!headerFound)
            {
                plateColumn = Array.FindIndex(cells, IsVehicleHeader);
                distanceColumn = Array.FindIndex(cells, IsDistanceHeader);
                headerFound = plateColumn >= 0 && distanceColumn >= 0;
                continue;
            }

            var plate = ValueAt(cells, plateColumn);
            var distanceText = ValueAt(cells, distanceColumn);
            if (string.IsNullOrWhiteSpace(plate))
            {
                continue;
            }

            if (TryParseDistance(distanceText, out var distance))
            {
                rows.Add(new ParsedGpsDistanceRow(reportRowNumber, plate, distance, true, null, null));
            }
            else if (IsNoGpsValue(distanceText))
            {
                rows.Add(new ParsedGpsDistanceRow(reportRowNumber, plate, null, false, null, null));
            }
            else
            {
                rows.Add(new ParsedGpsDistanceRow(
                    reportRowNumber,
                    plate,
                    null,
                    false,
                    "invalid_distance",
                    "تعذر قراءة مسافة GPS من الصف."));
            }
        }

        if (!headerFound || rows.Count == 0)
        {
            throw new InvalidDataException("The HTML export does not contain the expected GPS table.");
        }

        return new ParsedGpsDistanceReport(
            DateOnly.FromDateTime(periodStart.ToOffset(TimeSpan.FromHours(3)).DateTime),
            periodStart,
            periodEnd,
            rows);
    }

    internal static bool TryParseDistance(string value, out decimal distance)
    {
        distance = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = DistanceNumberRegex().Match(NormalizeText(value));
        var number = match.Success ? NormalizeNumber(match.Value) : string.Empty;
        return match.Success
            && decimal.TryParse(
                number,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out distance)
            && distance >= 0;
    }

    private static bool TryParsePeriod(string value, out DateTimeOffset start, out DateTimeOffset end)
    {
        start = default;
        end = default;
        var matches = PeriodDateRegex().Matches(NormalizeText(value));
        if (matches.Count < 2
            || !TryParseReportDateTime(matches[0].Value, out var startLocal)
            || !TryParseReportDateTime(matches[1].Value, out var endLocal))
        {
            return false;
        }

        var riyadhOffset = TimeSpan.FromHours(3);
        start = new DateTimeOffset(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), riyadhOffset).ToUniversalTime();
        end = new DateTimeOffset(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), riyadhOffset).ToUniversalTime();
        return end > start;
    }

    private static bool IsVehicleHeader(string value)
    {
        var header = CanonicalHeader(value);
        return header is "عربة" or "العربة" or "مركبة" or "المركبة" or "رقمالمركبة"
            or "رقماللوحة" or "لوحة" or "vehicle" or "vehiclenumber" or "platenumber" or "plate"
            || header.Contains("vehicle", StringComparison.Ordinal)
            || header.Contains("plate", StringComparison.Ordinal)
            || header.Contains("رقمالمركبة", StringComparison.Ordinal)
            || header.Contains("رقماللوحة", StringComparison.Ordinal);
    }

    private static bool IsDistanceHeader(string value)
    {
        var header = CanonicalHeader(value);
        return header.Contains("طولالطريق", StringComparison.Ordinal)
            || header.Contains("المسافة", StringComparison.Ordinal)
            || header.Contains("مسافة", StringComparison.Ordinal)
            || header.Contains("distance", StringComparison.Ordinal)
            || header.Contains("routelength", StringComparison.Ordinal)
            || header.Contains("mileage", StringComparison.Ordinal);
    }

    private static bool IsNoGpsValue(string value)
    {
        var normalized = NormalizeText(value);
        var compact = CanonicalHeader(normalized);
        return string.IsNullOrWhiteSpace(normalized)
            || compact.Contains("لميتمالعثور", StringComparison.Ordinal)
            || compact.Contains("لاتوجدبيانات", StringComparison.Ordinal)
            || compact is "كيلومترا" or "كيلومتر" or "km" or "kms" or "na";
    }

    private static string ValueAt(string[] values, int index) =>
        index >= 0 && index < values.Length ? values[index] : string.Empty;

    private static bool LooksLikeHtml(ReadOnlySpan<byte> prefix)
    {
        if (prefix.Length >= 3 && prefix[0] == 0xEF && prefix[1] == 0xBB && prefix[2] == 0xBF)
        {
            prefix = prefix[3..];
        }

        if (prefix.Length >= 2 && prefix[0] == 0xFF && prefix[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(prefix[2..]).TrimStart().StartsWith('<');
        }

        if (prefix.Length >= 2 && prefix[0] == 0xFE && prefix[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(prefix[2..]).TrimStart().StartsWith('<');
        }

        return Encoding.ASCII.GetString(prefix).TrimStart().StartsWith('<');
    }

    private static string ReadHtml(Stream stream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var originalPosition = stream.Position;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        stream.Position = originalPosition;
        var bytes = buffer.ToArray();

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        var prefix = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 2048));
        var charset = HtmlCharsetRegex().Match(prefix).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(charset))
        {
            try
            {
                return Encoding.GetEncoding(charset).GetString(bytes).TrimStart('\uFEFF');
            }
            catch (ArgumentException)
            {
                // Fall through to UTF-8/Windows-1256 detection.
            }
        }

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1256).GetString(bytes).TrimStart('\uFEFF');
        }
    }

    private static bool TryParseReportDateTime(string value, out DateTime result)
    {
        result = default;
        var match = ReportDatePartsRegex().Match(value);
        if (!match.Success)
        {
            return false;
        }

        var first = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var second = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var third = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var yearFirst = match.Groups[1].Value.Length == 4;
        var year = yearFirst ? first : third;
        var month = second;
        var day = yearFirst ? third : first;
        var hour = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
        var secondValue = int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);

        try
        {
            result = new DateTime(year, month, day, hour, minute, secondValue, DateTimeKind.Unspecified);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string NormalizeNumber(string value)
    {
        var compact = new string(value.Where(character => !char.IsWhiteSpace(character)).ToArray())
            .Replace("٬", string.Empty, StringComparison.Ordinal)
            .Replace('٫', '.')
            .Replace(',', '.');
        var decimalIndex = compact.LastIndexOf('.');
        if (decimalIndex < 0)
        {
            return compact;
        }

        var integerPart = compact[..decimalIndex].Replace(".", string.Empty, StringComparison.Ordinal);
        var fractionalPart = compact[(decimalIndex + 1)..];
        return integerPart + "." + fractionalPart;
    }

    private static string CanonicalHeader(string value) =>
        new(NormalizeText(value).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

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

        return WhitespaceRegex().Replace(builder.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"[0-9]+(?:[\s\u00A0\u202F\.,٫٬][0-9]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex DistanceNumberRegex();

    [GeneratedRegex(@"(?:[0-9]{4}[-\./][0-9]{1,2}[-\./][0-9]{1,2}|[0-9]{1,2}[-\./][0-9]{1,2}[-\./][0-9]{4})[\sT]+[0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PeriodDateRegex();

    [GeneratedRegex(@"^([0-9]{1,4})[-\./]([0-9]{1,2})[-\./]([0-9]{1,4})[\sT]+([0-9]{1,2}):([0-9]{1,2}):([0-9]{1,2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReportDatePartsRegex();

    [GeneratedRegex(@"charset\s*=\s*[""']?\s*([A-Za-z0-9_\-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlCharsetRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"<tr\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex HtmlRowRegex();

    [GeneratedRegex(@"<t[dh]\b[^>]*>(.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex HtmlCellRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex HtmlTagRegex();
}
