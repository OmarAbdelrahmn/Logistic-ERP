namespace LogisticsERP.Application.Features.Telecom;

public sealed record PlaceResponse(Guid Id, string Name);

public sealed record SavePlaceRequest(string Name);
