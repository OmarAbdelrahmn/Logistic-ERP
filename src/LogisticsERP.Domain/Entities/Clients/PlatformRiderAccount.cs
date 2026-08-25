using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Clients;

public sealed class PlatformRiderAccount : AuditableEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid? RegisteredEmployeeId { get; set; }
    public Guid OperatingCityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ExternalAccountId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public PlatformAccountPaymentModel PaymentModel { get; set; } = PlatformAccountPaymentModel.PayPerOrder;
    public PlatformRiderAccountStatus Status { get; set; } = PlatformRiderAccountStatus.Available;
    public string? StatusReason { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? OwnershipNotes { get; set; }
    public string? OperationalNotes { get; set; }
}
