using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Clients;

public sealed class PlatformAccountRegistration : AuditableEntity
{
    public Guid RegisteredEmployeeId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid ClientPlatformId { get; set; }
    public Guid ClientContractId { get; set; }
    public Guid? SponsorId { get; set; }
    public Guid OperatingCityId { get; set; }
    public PlatformRegistrationType RegistrationType { get; set; } = PlatformRegistrationType.Sponsored;
    public PlatformAccountRegistrationStatus Status { get; set; } = PlatformAccountRegistrationStatus.NotRequired;
    public string? StatusReason { get; set; }
    public DateTimeOffset? RequestedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public Guid? PlatformRiderAccountId { get; set; }
    public string? Notes { get; set; }
}
