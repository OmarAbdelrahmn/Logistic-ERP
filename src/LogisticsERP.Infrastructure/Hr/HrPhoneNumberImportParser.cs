using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Telecom;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed record ParsedHrPhoneNumberWorkbook(
    string Worksheet,
    int TotalRows,
    IReadOnlyList<ParsedHrPhoneNumberRow> Rows,
    IReadOnlyList<HrExcelImportIssue> Issues);

internal sealed record ParsedHrPhoneNumberRow(
    int RowNumber,
    string IqamaNo,
    string PhoneNumber);

internal static class HrPhoneNumberImportParser
{
    private const int MaximumRows = 5_000;

    private static readonly HashSet<string> IqamaHeaders = new(StringComparer.Ordinal)
    {
        "iqama",
        "iqamano",
        "iqamanumber",
        "residencyid",
        "residencypermitnumber",
        "رقمالاقامه",
        "رقمالهويه"
    };

    private static readonly HashSet<string> PhoneHeaders = new(StringComparer.Ordinal)
    {
        "phone",
        "phonenumber",
        "mobile",
        "mobilenumber",
        "primaryphone",
        "الجوال",
        "رقمالجوال",
        "رقمالهاتف"
    };

    public static ParsedHrPhoneNumberWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        var used = worksheet?.RangeUsed(XLCellsUsedOptions.Contents);
        if (worksheet is null || used is null)
        {
            throw new InvalidDataException("The workbook does not contain a populated worksheet.");
        }

        var firstUsedRow = used.FirstRow().RowNumber();
        var hasHeader = IsHeaderRow(worksheet.Row(firstUsedRow));
        var firstDataRow = hasHeader ? firstUsedRow + 1 : firstUsedRow;
        if (used.LastRow().RowNumber() - firstDataRow + 1 > MaximumRows)
        {
            throw new InvalidDataException($"The workbook contains more than {MaximumRows} data rows.");
        }

        var issues = new List<HrExcelImportIssue>();
        var rows = new List<ParsedHrPhoneNumberRow>();
        var iqamas = new HashSet<string>(StringComparer.Ordinal);
        var totalRows = 0;

        for (var rowNumber = firstDataRow; rowNumber <= used.LastRow().RowNumber(); rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var iqamaValue = CellText(row.Cell(1));
            var phoneValue = CellText(row.Cell(2));
            if (iqamaValue.Length == 0 && phoneValue.Length == 0)
            {
                continue;
            }

            totalRows++;
            var iqama = NormalizeDigits(iqamaValue);
            var rowHasError = false;
            if (iqama.Length != 10 || !iqama.All(char.IsAsciiDigit))
            {
                issues.Add(new(rowNumber, NullIfEmpty(iqama), "Error", "The first column must contain a 10-digit Iqama number."));
                rowHasError = true;
            }
            else if (!iqamas.Add(iqama))
            {
                issues.Add(new(rowNumber, iqama, "Error", "Duplicate Iqama number in the workbook."));
                rowHasError = true;
            }

            if (!PhoneSimRules.TryNormalizePhoneNumber(phoneValue, out var phoneNumber))
            {
                issues.Add(new(
                    rowNumber,
                    NullIfEmpty(iqama),
                    "Error",
                    "The second column must contain a valid Saudi mobile number or international E.164 phone number."));
                rowHasError = true;
            }

            if (!rowHasError)
            {
                rows.Add(new ParsedHrPhoneNumberRow(rowNumber, iqama, phoneNumber));
            }
        }

        if (totalRows == 0)
        {
            throw new InvalidDataException("The workbook does not contain any phone-number rows.");
        }

        return new ParsedHrPhoneNumberWorkbook(worksheet.Name, totalRows, rows, issues);
    }

    private static bool IsHeaderRow(IXLRow row) =>
        IqamaHeaders.Contains(HrExcelImportParser.NormalizeKey(CellText(row.Cell(1))))
        && PhoneHeaders.Contains(HrExcelImportParser.NormalizeKey(CellText(row.Cell(2))));

    private static string CellText(IXLCell cell) =>
        cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();

    private static string NormalizeDigits(string value) => string.Concat(value.Trim().Select(character => character switch
    {
        '\u0660' or '\u06F0' => '0',
        '\u0661' or '\u06F1' => '1',
        '\u0662' or '\u06F2' => '2',
        '\u0663' or '\u06F3' => '3',
        '\u0664' or '\u06F4' => '4',
        '\u0665' or '\u06F5' => '5',
        '\u0666' or '\u06F6' => '6',
        '\u0667' or '\u06F7' => '7',
        '\u0668' or '\u06F8' => '8',
        '\u0669' or '\u06F9' => '9',
        _ => character
    }));

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
