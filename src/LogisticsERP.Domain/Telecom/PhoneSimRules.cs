using System.Text;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Telecom;

public static class PhoneSimRules
{
    private const int MinimumE164Digits = 8;
    private const int MaximumE164Digits = 15;
    private const int MinimumIccidDigits = 18;
    private const int MaximumIccidDigits = 22;

    public static string NormalizePhoneNumber(string phoneNumber)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        if (TryNormalizePhoneNumber(phoneNumber, out var normalizedPhoneNumber))
        {
            return normalizedPhoneNumber;
        }

        throw new ArgumentException(
            "The phone number must be a valid Saudi mobile number or an international E.164 number.",
            nameof(phoneNumber));
    }

    public static bool TryNormalizePhoneNumber(string? phoneNumber, out string normalizedPhoneNumber)
    {
        normalizedPhoneNumber = string.Empty;
        if (string.IsNullOrWhiteSpace(phoneNumber)
            || !TryRemoveFormatting(phoneNumber, allowLeadingPlus: true, out var compact))
        {
            return false;
        }

        if (compact.StartsWith("00", StringComparison.Ordinal))
        {
            compact = $"+{compact[2..]}";
        }
        else if (compact.Length == 10 && compact.StartsWith("05", StringComparison.Ordinal))
        {
            compact = $"+966{compact[1..]}";
        }
        else if (compact.Length == 9 && compact[0] == '5')
        {
            compact = $"+966{compact}";
        }
        else if (compact.Length == 12 && compact.StartsWith("966", StringComparison.Ordinal))
        {
            compact = $"+{compact}";
        }

        if (compact.Length < MinimumE164Digits + 1
            || compact.Length > MaximumE164Digits + 1
            || compact[0] != '+'
            || compact[1] == '0'
            || compact.AsSpan(1).ContainsAnyExceptInRange('0', '9'))
        {
            return false;
        }

        if (compact.StartsWith("+966", StringComparison.Ordinal)
            && (compact.Length != 13 || !compact.StartsWith("+9665", StringComparison.Ordinal)))
        {
            return false;
        }

        normalizedPhoneNumber = compact;
        return true;
    }

    public static string? NormalizeIccid(string? iccid)
    {
        if (string.IsNullOrWhiteSpace(iccid))
        {
            return null;
        }

        if (!TryRemoveFormatting(iccid, allowLeadingPlus: false, out var normalizedIccid)
            || normalizedIccid.Length is < MinimumIccidDigits or > MaximumIccidDigits
            || !normalizedIccid.StartsWith("89", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The ICCID must contain 18 to 22 digits and start with 89.",
                nameof(iccid));
        }

        return normalizedIccid;
    }

    public static bool CanAssign(PhoneSimStatus status, bool hasCurrentAssignment) =>
        status == PhoneSimStatus.Available && !hasCurrentAssignment;

    public static PhoneSimStatus GetStatusAfterAssignment(
        PhoneSimStatus status,
        bool hasCurrentAssignment)
    {
        if (!CanAssign(status, hasCurrentAssignment))
        {
            throw new InvalidOperationException(
                "Only an available SIM without a current rider assignment can be assigned.");
        }

        return PhoneSimStatus.Assigned;
    }

    public static PhoneSimStatus GetStatusAfterRelease(PhoneSimStatus status) =>
        status == PhoneSimStatus.Assigned ? PhoneSimStatus.Available : status;

    public static PhoneSimStatus DeriveStatus(
        PhoneSimStatus directStatus,
        bool hasCurrentAssignment)
    {
        if (!Enum.IsDefined(directStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(directStatus));
        }

        return directStatus switch
        {
            PhoneSimStatus.Available when hasCurrentAssignment => PhoneSimStatus.Assigned,
            PhoneSimStatus.Assigned when !hasCurrentAssignment => PhoneSimStatus.Available,
            _ => directStatus
        };
    }

    public static bool CanSetStatusDirectly(PhoneSimStatus status) =>
        Enum.IsDefined(status) && status != PhoneSimStatus.Assigned;

    private static bool TryRemoveFormatting(
        string value,
        bool allowLeadingPlus,
        out string compact)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            if (TryGetAsciiDigit(character, out var digit))
            {
                builder.Append(digit);
                continue;
            }

            if (allowLeadingPlus && character == '+' && builder.Length == 0)
            {
                builder.Append(character);
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '(' or ')' or '.')
            {
                continue;
            }

            compact = string.Empty;
            return false;
        }

        compact = builder.ToString();
        return compact.Length > 0;
    }

    private static bool TryGetAsciiDigit(char character, out char digit)
    {
        if (character is >= '0' and <= '9')
        {
            digit = character;
            return true;
        }

        if (character is >= '\u0660' and <= '\u0669')
        {
            digit = (char)('0' + character - '\u0660');
            return true;
        }

        if (character is >= '\u06f0' and <= '\u06f9')
        {
            digit = (char)('0' + character - '\u06f0');
            return true;
        }

        digit = default;
        return false;
    }
}
