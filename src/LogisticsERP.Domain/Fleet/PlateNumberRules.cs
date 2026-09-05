using System.Globalization;
using System.Text;

namespace LogisticsERP.Domain.Fleet;

public static class PlateNumberRules
{
    public static IEnumerable<string> BuildLookupKeys(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        var normalized = NormalizeCharacters(value);
        if (normalized.Length == 0)
        {
            yield break;
        }

        var variants = new HashSet<string>(StringComparer.Ordinal)
        {
            normalized,
            TransliterateArabic(normalized)
        };

        foreach (var variant in variants)
        {
            yield return variant;
            var digits = new string(variant.Where(char.IsDigit).ToArray());
            var letters = new string(variant.Where(char.IsLetter).ToArray());
            if (digits.Length > 0 && letters.Length > 0)
            {
                yield return digits + letters;
                yield return letters + digits;
            }
        }
    }

    public static string CanonicalKey(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = NormalizeCharacters(value);
        var transliterated = TransliterateArabic(normalized);
        var digits = new string(transliterated.Where(char.IsDigit).ToArray());
        var letters = new string(transliterated.Where(char.IsLetter).ToArray());
        return digits.Length > 0 && letters.Length > 0 ? digits + letters : transliterated;
    }

    public static string NormalizeCharacters(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark
                || !char.IsLetterOrDigit(character))
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
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                _ => char.ToUpperInvariant(character)
            });
        }

        return builder.ToString();
    }

    public static string TransliterateArabic(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                'ا' => 'A',
                'ب' => 'B',
                'ح' => 'J',
                'د' => 'D',
                'ر' => 'R',
                'س' => 'S',
                'ص' => 'X',
                'ط' => 'T',
                'ع' => 'E',
                'ق' => 'G',
                'ك' => 'K',
                'ل' => 'L',
                'م' => 'Z',
                'ن' => 'N',
                'ه' => 'H',
                'و' => 'U',
                'ي' => 'V',
                _ => character
            });
        }

        return builder.ToString();
    }
}
