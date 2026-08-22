namespace LogisticsERP.Application.Features.Hr;

public sealed record AddressRequest(
    string? BuildingNumber,
    string? Street,
    string? District,
    string? City,
    string? PostalCode,
    string? AdditionalNumber);

public sealed record AddressResponse(
    string? BuildingNumber,
    string? Street,
    string? District,
    string? City,
    string? PostalCode,
    string? AdditionalNumber);

public sealed record CatalogResponse(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    string Status,
    string RowVersion);

public sealed record ArchiveRequest(string Reason, string RowVersion);

public sealed record ClosePeriodRequest(DateOnly EffectiveTo, string Reason);
