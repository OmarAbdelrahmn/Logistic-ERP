using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeDriverLicense : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid DriverLicenseCategoryId { get; set; }
    public byte[]? LicenseNumberCiphertext { get; set; }
    public string? LicenseNumberLookupHash { get; set; }
    public string? LicenseNumberLastFour { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DriverLicenseBookingStatus BookingStatus { get; set; } = DriverLicenseBookingStatus.Unknown;
    public DriverLicenseIssuanceStatus IssuanceStatus { get; set; } = DriverLicenseIssuanceStatus.NotStarted;
    public DriverLicenseStatus LicenseStatus { get; set; } = DriverLicenseStatus.Application;
    public Guid? PreviousLicenseId { get; set; }
    public bool IsCurrent { get; set; }
    public Guid? EmployeeDocumentId { get; set; }
    public string? Notes { get; set; }
}
