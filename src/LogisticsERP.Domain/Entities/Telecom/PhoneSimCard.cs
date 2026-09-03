using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Telecom;

public sealed class PhoneSimCard : AuditableEntity
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string NormalizedPhoneNumber { get; set; } = string.Empty;
    public string? Iccid { get; set; }
    public string? NormalizedIccid { get; set; }
    public string? CarrierName { get; set; }
    public Guid ResponsibleEmployeeId { get; set; }
    public PhoneSimStatus Status { get; set; } = PhoneSimStatus.Available;
    public string? StatusReason { get; set; }
    public string? Notes { get; set; }
    public string? ReceiptFormOriginalFileName { get; set; }
    public string? ReceiptFormStoredFileName { get; set; }
    public string? ReceiptFormContentType { get; set; }
    public long? ReceiptFormSizeBytes { get; set; }
    public string? ReceiptFormSha256Checksum { get; set; }
    public string? ReceiptFormStoragePath { get; set; }
}
