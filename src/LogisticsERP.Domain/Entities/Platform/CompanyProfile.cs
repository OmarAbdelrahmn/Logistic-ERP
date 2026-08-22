using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Platform;

public sealed class CompanyProfile : AuditableEntity
{
    public static readonly Guid FixedId = Guid.Parse("019c18d5-62e1-7000-8000-000000000001");

    public string Code { get; set; } = string.Empty;
    public string LegalNameAr { get; set; } = string.Empty;
    public string LegalNameEn { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string DisplayNameEn { get; set; } = string.Empty;
    public string? CommercialRegistrationNumber { get; set; }
    public string? UnifiedNationalNumber { get; set; }
    public string? VatNumber { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public Address Address { get; set; } = new();
    public string? LogoAssetKey { get; set; }
    public string DefaultLocale { get; set; } = "ar";
    public string TimeZoneId { get; set; } = "Asia/Riyadh";
    public long NextEmployeeSequence { get; set; } = 1;
    public CompanyStatus Status { get; set; } = CompanyStatus.Setup;
    public string? SuspensionReason { get; set; }
    public DateTimeOffset? SuspendedAtUtc { get; set; }
    public Guid? SuspendedByUserId { get; set; }
}
