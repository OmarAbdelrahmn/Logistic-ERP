namespace LogisticsERP.Domain.Common;

public sealed class Address
{
    public string? BuildingNumber { get; set; }
    public string? Street { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? AdditionalNumber { get; set; }
}
