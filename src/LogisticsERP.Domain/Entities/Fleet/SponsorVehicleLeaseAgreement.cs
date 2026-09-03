using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class SponsorVehicleLeaseAgreement : TemporalPeriodEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid LessorSponsorId { get; set; }
    public Guid LesseeSponsorId { get; set; }
    public DateOnly? AgreementDate { get; set; }
    public string? AgreementReference { get; set; }
    public string? EndReason { get; set; }
    public string? Notes { get; set; }
}

public sealed class SponsorVehicleLeaseAgreementVehicle : HistoryEntity
{
    public Guid SponsorVehicleLeaseAgreementId { get; set; }
    public Guid VehicleId { get; set; }
}
