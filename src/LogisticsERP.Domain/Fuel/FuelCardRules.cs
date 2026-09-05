using System.Globalization;
using System.Text;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;

namespace LogisticsERP.Domain.Fuel;

public static class FuelCardRules
{
    public static string NormalizeCardNumber(string value, FuelCardIdentifierType identifierType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = identifierType == FuelCardIdentifierType.PlateNumber
            ? PlateNumberRules.CanonicalKey(value)
            : NormalizeInternalNumber(value);
        if (normalized.Length is < 2 or > 100)
        {
            throw new ArgumentException("The fuel-card number is invalid.", nameof(value));
        }

        return normalized;
    }

    public static FuelCardIdentifierType DetectIdentifierType(string value)
    {
        var internalNumber = NormalizeInternalNumber(value);
        return internalNumber.StartsWith("BW", StringComparison.Ordinal)
            && internalNumber.AsSpan(2).Length > 0
            && internalNumber.AsSpan(2).ContainsAnyExceptInRange('0', '9') == false
                ? FuelCardIdentifierType.InternalNumber
                : FuelCardIdentifierType.PlateNumber;
    }

    public static DateOnly MonthStart(DateOnly value) => new(value.Year, value.Month, 1);

    public static DateOnly MonthEnd(DateOnly value) => MonthStart(value).AddMonths(1).AddDays(-1);

    public static bool PeriodTouchesMonth(DateOnly from, DateOnly? to, DateOnly month) =>
        from <= MonthEnd(month) && (!to.HasValue || to.Value >= MonthStart(month));

    public static bool CanUseRiderForMonth(Guid riderProfileId, IEnumerable<Guid> existingRiderProfileIds)
    {
        ArgumentNullException.ThrowIfNull(existingRiderProfileIds);
        return existingRiderProfileIds.All(existingRiderProfileId => existingRiderProfileId == riderProfileId);
    }

    private static string NormalizeInternalNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark
                || char.IsWhiteSpace(character) || character is '-' or '_' or '/')
            {
                continue;
            }

            builder.Append(character switch
            {
                '\u0660' => '0',
                '\u0661' => '1',
                '\u0662' => '2',
                '\u0663' => '3',
                '\u0664' => '4',
                '\u0665' => '5',
                '\u0666' => '6',
                '\u0667' => '7',
                '\u0668' => '8',
                '\u0669' => '9',
                '\u06F0' => '0',
                '\u06F1' => '1',
                '\u06F2' => '2',
                '\u06F3' => '3',
                '\u06F4' => '4',
                '\u06F5' => '5',
                '\u06F6' => '6',
                '\u06F7' => '7',
                '\u06F8' => '8',
                '\u06F9' => '9',
                _ => char.ToUpperInvariant(character)
            });
        }

        return builder.ToString();
    }
}
