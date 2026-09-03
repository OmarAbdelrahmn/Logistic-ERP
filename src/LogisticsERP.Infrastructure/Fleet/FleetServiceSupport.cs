using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.Persistence;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class FleetServiceSupport(
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider)
{
    public Guid? UserId => currentUser.UserId;
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public async Task<bool> HasPermissionAsync(string permissionKey, Guid? housingId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || currentUser.AuthorizationVersion is not { } version) return false;
        return await permissionChecker.HasPermissionAsync(userId, version, permissionKey, null, cancellationToken);
    }

    public async Task<bool> HasVehiclePermissionAsync(Vehicle vehicle, string permissionKey, CancellationToken cancellationToken)
    {
        return await HasPermissionAsync(permissionKey, null, cancellationToken);
    }

    public static string NormalizeIdentifier(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character) || character is '-' or '_' or '/') continue;
            builder.Append(character switch
            {
                '\u0660' => '0', '\u0661' => '1', '\u0662' => '2', '\u0663' => '3', '\u0664' => '4',
                '\u0665' => '5', '\u0666' => '6', '\u0667' => '7', '\u0668' => '8', '\u0669' => '9',
                _ => char.ToUpperInvariant(character)
            });
        }
        return builder.ToString();
    }

    public static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static string EncodeRowVersion(byte[] value) => Convert.ToBase64String(value);

    public static bool MatchesRowVersion(byte[] current, string? supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied)) return false;
        try { return CryptographicOperations.FixedTimeEquals(current, Convert.FromBase64String(supplied)); }
        catch (FormatException) { return false; }
    }

    public static string HashRequest<T>(T request)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public static VehicleComplianceDueStatus DueStatus(DateOnly? expiry, DateOnly checkDate, int alertDays = 30) =>
        VehicleComplianceStatusCalculator.Calculate(expiry, checkDate, alertDays);

    public static string NewVehicleAssetNumber(Guid id) => $"VEH-{id:N}"[..12].ToUpperInvariant();

    public static string NewIssueNumber(Guid id) => $"ISS-{id.ToString("N")[^16..]}".ToUpperInvariant();

    public static string NewNumber(string prefix, DateTimeOffset now, Guid id) => $"{prefix}-{now:yyyyMMdd}-{id:N}"[..(prefix.Length + 18)].ToUpperInvariant();
}
