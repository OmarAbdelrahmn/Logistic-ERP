using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Common;

namespace LogisticsERP.Infrastructure.Hr;

internal static class HrServiceSupport
{
    public static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    public static string NormalizeText(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        var previousWasSpace = false;
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                }
                previousWasSpace = true;
                continue;
            }

            previousWasSpace = false;
            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }

    public static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

    public static string EncodeRowVersion(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static bool MatchesRowVersion(byte[] current, string? supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied))
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(current, Convert.FromBase64String(supplied));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static Address ToAddress(AddressRequest? request) => request is null
        ? new Address()
        : new Address
        {
            BuildingNumber = TrimOrNull(request.BuildingNumber),
            Street = TrimOrNull(request.Street),
            District = TrimOrNull(request.District),
            City = TrimOrNull(request.City),
            PostalCode = TrimOrNull(request.PostalCode),
            AdditionalNumber = TrimOrNull(request.AdditionalNumber)
        };

    public static Address? ToNullableAddress(AddressRequest? request) =>
        request is null ? null : ToAddress(request);

    public static AddressResponse ToAddressResponse(Address address) => new(
        address.BuildingNumber,
        address.Street,
        address.District,
        address.City,
        address.PostalCode,
        address.AdditionalNumber);

    public static AddressResponse? ToNullableAddressResponse(Address? address) =>
        address is null ? null : ToAddressResponse(address);

    public static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string MaskLastFour(string? lastFour) => string.IsNullOrEmpty(lastFour) ? string.Empty : $"••••{lastFour}";

    public static string LastFour(string value)
    {
        var normalized = NormalizeIdentifier(value);
        return normalized.Length <= 4 ? normalized : normalized[^4..];
    }

    public static string NormalizeIdentifier(string value) =>
        new(value.Trim().Normalize(NormalizationForm.FormKC).Where(character => !char.IsWhiteSpace(character)).ToArray());
}

internal interface ISensitiveValueProtector
{
    byte[] Protect(string value);
    string Unprotect(byte[] value);
    string CreateLookupHash(string value);
}

internal sealed record ProtectedPlatformCredential(byte[] Ciphertext, byte[] Nonce, byte[] AuthenticationTag);

internal interface IPlatformCredentialProtector
{
    ProtectedPlatformCredential Protect(string value);
}

internal sealed class PlatformCredentialProtector : IPlatformCredentialProtector
{
    private readonly byte[] encryptionKey;

    public PlatformCredentialProtector(byte[] masterKey)
    {
        encryptionKey = HMACSHA256.HashData(masterKey, "LogisticsERP.PlatformCredential.v1"u8.ToArray());
    }

    public ProtectedPlatformCredential Protect(string value)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new AesGcm(encryptionKey, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        CryptographicOperations.ZeroMemory(plaintext);
        return new ProtectedPlatformCredential(ciphertext, nonce, tag);
    }
}

internal sealed class SensitiveValueProtector : ISensitiveValueProtector
{
    private const byte FormatVersion = 1;
    private readonly byte[] encryptionKey;
    private readonly byte[] lookupKey;

    public SensitiveValueProtector(byte[] masterKey)
    {
        encryptionKey = HMACSHA256.HashData(masterKey, "LogisticsERP.FieldEncryption.v1"u8.ToArray());
        lookupKey = HMACSHA256.HashData(masterKey, "LogisticsERP.LookupHash.v1"u8.ToArray());
    }

    public byte[] Protect(string value)
    {
        var plaintext = Encoding.UTF8.GetBytes(HrServiceSupport.NormalizeIdentifier(value));
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new AesGcm(encryptionKey, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
        payload[0] = FormatVersion;
        nonce.CopyTo(payload, 1);
        tag.CopyTo(payload, 1 + nonce.Length);
        ciphertext.CopyTo(payload, 1 + nonce.Length + tag.Length);
        CryptographicOperations.ZeroMemory(plaintext);
        return payload;
    }

    public string CreateLookupHash(string value)
    {
        var normalized = Encoding.UTF8.GetBytes(HrServiceSupport.NormalizeIdentifier(value));
        var hash = HMACSHA256.HashData(lookupKey, normalized);
        CryptographicOperations.ZeroMemory(normalized);
        return Convert.ToHexString(hash);
    }

    public string Unprotect(byte[] value)
    {
        if (value.Length < 1 + AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize || value[0] != FormatVersion)
        {
            throw new CryptographicException("Invalid protected value.");
        }
        var nonceLength = AesGcm.NonceByteSizes.MaxSize;
        var tagLength = AesGcm.TagByteSizes.MaxSize;
        var ciphertextLength = value.Length - 1 - nonceLength - tagLength;
        var plaintext = new byte[ciphertextLength];
        using var aes = new AesGcm(encryptionKey, tagLength);
        aes.Decrypt(value.AsSpan(1, nonceLength), value.AsSpan(1 + nonceLength, tagLength), value.AsSpan(1 + nonceLength + tagLength), plaintext);
        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
