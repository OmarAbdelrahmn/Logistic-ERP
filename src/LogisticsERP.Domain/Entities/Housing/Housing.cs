using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class Housing : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public Guid CityId { get; set; }
    public Address Address { get; set; } = new();
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int TotalCapacity { get; set; }
    public string? ContactPhone { get; set; }
    public DateOnly? OpenedDate { get; set; }
    public DateOnly? ClosedDate { get; set; }
    public HousingStatus Status { get; set; } = HousingStatus.Draft;
    public string? StatusReason { get; set; }
    public string? Notes { get; set; }
}
