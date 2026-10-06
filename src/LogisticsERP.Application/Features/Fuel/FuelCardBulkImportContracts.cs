using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fuel;

public interface IFuelCardBulkImportService
{
    Task<Result<FuelCardBulkImportResponse>> ImportAsync(
        Stream content,
        bool validateOnly,
        CancellationToken cancellationToken = default);
}

public sealed record FuelCardBulkImportResponse(
    bool ValidateOnly,
    bool CanImport,
    bool Imported,
    int TotalRows,
    int NewCards,
    int ExistingCards,
    IReadOnlyList<FuelCardBulkImportRow> Rows,
    IReadOnlyList<FuelCardBulkImportIssue> Issues);

public sealed record FuelCardBulkImportRow(
    int RowNumber,
    string CardNumber,
    string Sponsor70Number,
    Guid SponsorId,
    string SponsorNameAr,
    string CompanyName,
    string Provider,
    bool WillCreateCard,
    Guid OperatingCityId);

public sealed record FuelCardBulkImportIssue(int RowNumber, string? CardNumber, string Message);
