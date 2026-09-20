using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed record ParsedVehicleImportRow(
    int RowNumber,
    string PlateNumber,
    string RegistrationType,
    string Manufacturer,
    string Model,
    int? ModelYear,
    string SerialNumber,
    string ChassisNumber,
    string Color,
    string City,
    string OwnerNumber,
    string UserNumber);

internal sealed record ParsedVehicleImportWorkbook(
    string Worksheet,
    int TotalRows,
    IReadOnlyList<ParsedVehicleImportRow> Rows,
    IReadOnlyList<LogisticsERP.Application.Features.Fleet.VehicleImportIssue> Issues);

internal static partial class VehicleSpreadsheetParser
{
    private const string Plate = "plate";
    private const string RegistrationType = "registrationType";
    private const string Manufacturer = "manufacturer";
    private const string Model = "model";
    private const string ModelYear = "modelYear";
    private const string SerialNumber = "serialNumber";
    private const string ChassisNumber = "chassisNumber";
    private const string Color = "color";
    private const string City = "city";
    private const string OwnerNumber = "ownerNumber";
    private const string UserNumber = "userNumber";

    private static readonly Dictionary<string, string> HeaderAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["رقماللوحه"] = Plate,
            ["نوعتسجيلالاستماره"] = RegistrationType,
            ["نوعالتسجيل"] = RegistrationType,
            ["الماركه"] = Manufacturer,
            ["الطراز"] = Model,
            ["سنهالصنع"] = ModelYear,
            ["الرقمالتسلسلي"] = SerialNumber,
            ["رقمالهيكل"] = ChassisNumber,
            ["اللونالاساسي"] = Color,
            ["المدينه"] = City,
            ["رقمهويهالمالك"] = OwnerNumber,
            ["رقمالسجلاوالمستخدم"] = UserNumber,
            ["رقمالسجلاوالمستعمل"] = UserNumber
        };

    private static readonly string[] RequiredFields =
    [
        Plate, RegistrationType, Manufacturer, Model, ModelYear, SerialNumber,
        ChassisNumber, Color, City, OwnerNumber, UserNumber
    ];

    public static ParsedVehicleImportWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The workbook does not contain a worksheet.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidDataException("The worksheet is empty.");
        var headerRow = usedRange.FirstRow();
        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in headerRow.Cells())
        {
            var normalized = NormalizeKey(CellText(cell));
            if (HeaderAliases.TryGetValue(normalized, out var field) && !headers.ContainsKey(field))
            {
                headers[field] = cell.Address.ColumnNumber;
            }
        }

        var missing = RequiredFields.Where(field => !headers.ContainsKey(field)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException($"Missing required vehicle columns: {string.Join(", ", missing)}");
        }

        var rows = new List<ParsedVehicleImportRow>();
        var issues = new List<LogisticsERP.Application.Features.Fleet.VehicleImportIssue>();
        var totalRows = 0;
        foreach (var row in worksheet.Rows(headerRow.RowNumber() + 1, usedRange.LastRow().RowNumber()))
        {
            var values = RequiredFields.ToDictionary(field => field, field => Value(row, headers, field), StringComparer.Ordinal);
            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            totalRows++;
            var plate = CollapseWhitespace(values[Plate]);
            var rowHasError = false;
            foreach (var field in RequiredFields.Where(field => field != Color))
            {
                if (string.IsNullOrWhiteSpace(values[field]))
                {
                    issues.Add(new(row.RowNumber(), EmptyToNull(plate), "Error", field, "القيمة مطلوبة."));
                    rowHasError = true;
                }
            }

            int? year = null;
            if (!string.IsNullOrWhiteSpace(values[ModelYear]))
            {
                var normalizedYear = NormalizeDigits(values[ModelYear]);
                if (!int.TryParse(normalizedYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYear)
                    || parsedYear is < 1950 or > 2200)
                {
                    issues.Add(new(row.RowNumber(), EmptyToNull(plate), "Error", ModelYear, "سنة الصنع يجب أن تكون بين 1950 و2200."));
                    rowHasError = true;
                }
                else
                {
                    year = parsedYear;
                }
            }

            if (rowHasError)
            {
                continue;
            }

            rows.Add(new ParsedVehicleImportRow(
                row.RowNumber(),
                plate,
                CollapseWhitespace(values[RegistrationType]),
                CollapseWhitespace(values[Manufacturer]),
                CollapseWhitespace(values[Model]),
                year,
                NormalizeDigits(CollapseWhitespace(values[SerialNumber])),
                CollapseWhitespace(values[ChassisNumber]).ToUpperInvariant(),
                CollapseWhitespace(values[Color]),
                CollapseWhitespace(values[City]),
                DigitsOnly(values[OwnerNumber]),
                DigitsOnly(values[UserNumber])));
        }

        if (totalRows == 0)
        {
            throw new InvalidDataException("The workbook does not contain vehicle rows.");
        }

        return new ParsedVehicleImportWorkbook(worksheet.Name, totalRows, rows, issues);
    }

    internal static string NormalizeKey(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var mapped = character switch
            {
                '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1',
                '\u0662' or '\u06F2' => '2', '\u0663' or '\u06F3' => '3',
                '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
                '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7',
                '\u0668' or '\u06F8' => '8', '\u0669' or '\u06F9' => '9',
                '\u0622' or '\u0623' or '\u0625' => '\u0627',
                '\u0649' => '\u064A',
                '\u0629' => '\u0647',
                '\u0640' => '\0',
                _ => character
            };
            if (mapped != '\0' && char.IsLetterOrDigit(mapped))
            {
                builder.Append(mapped);
            }
        }

        return builder.ToString();
    }

    private static string Value(IXLRow row, Dictionary<string, int> headers, string field) =>
        CellText(row.Cell(headers[field]));

    private static string CellText(IXLCell cell) => cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();

    private static string CollapseWhitespace(string value) => WhitespaceRegex().Replace(value.Trim(), " ");

    private static string NormalizeDigits(string value) => string.Concat(value.Select(character => character switch
    {
        '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1',
        '\u0662' or '\u06F2' => '2', '\u0663' or '\u06F3' => '3',
        '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
        '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7',
        '\u0668' or '\u06F8' => '8', '\u0669' or '\u06F9' => '9',
        _ => character
    }));

    private static string DigitsOnly(string value) => new(NormalizeDigits(value).Where(char.IsAsciiDigit).ToArray());
    private static string? EmptyToNull(string value) => value.Length == 0 ? null : value;

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
