namespace LogisticsERP.Application.Features.Fuel;

public sealed record FuelCardPageResponse(
    IReadOnlyList<FuelCardResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record FuelCardResponse(
    Guid Id,
    string Provider,
    string ProviderNameAr,
    string IdentifierType,
    string CardNumber,
    string NormalizedCardNumber,
    string? PlateNumberText,
    FuelCardCurrentRiderResponse? CurrentRider,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);

public sealed record FuelCardCurrentRiderResponse(
    Guid AssignmentId,
    Guid RiderProfileId,
    Guid EmployeeId,
    string RiderNameAr,
    string? RiderNameEn,
    DateOnly EffectiveFrom,
    string RowVersion);

public sealed record CreateFuelCardRequest(
    string Provider,
    string CardNumber,
    string? PlateNumberText,
    string? Notes);

public sealed record AssignFuelCardRiderRequest(
    Guid RiderProfileId,
    DateOnly EffectiveFrom,
    string Reason,
    string? Notes);

public sealed record StopFuelCardRiderRequest(
    DateOnly EffectiveTo,
    string Reason,
    string RowVersion);

public sealed record FuelCardAssignmentResponse(
    Guid Id,
    Guid FuelCardId,
    string CardNumber,
    Guid RiderProfileId,
    Guid EmployeeId,
    string RiderNameAr,
    string? RiderNameEn,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string AssignmentReason,
    string? EndReason,
    string? Notes,
    Guid AssignedByUserId,
    Guid? ClosedByUserId,
    string RowVersion);

public sealed record FuelMonthlyUsagePageResponse(
    IReadOnlyList<FuelMonthlyUsageResponse> Items,
    DateOnly Month,
    int Page,
    int PageSize,
    int TotalCount,
    decimal TotalLiters,
    decimal TotalAmount);

public sealed record FuelMonthlyUsageResponse(
    Guid Id,
    Guid FuelCardId,
    string Provider,
    string ProviderNameAr,
    string CardNumber,
    string? PlateNumberText,
    DateOnly ReportMonth,
    Guid RiderProfileId,
    Guid EmployeeId,
    string RiderNameAr,
    string? RiderNameEn,
    decimal TotalLiters,
    decimal TotalAmount,
    decimal? AmountBeforeTax,
    decimal? VatAmount,
    int? TransactionCount,
    string? FuelType,
    DateTimeOffset? FirstTransactionAtUtc,
    DateTimeOffset? LastTransactionAtUtc,
    DateTimeOffset? ReportThroughAtUtc,
    Guid LastImportId,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);

public sealed record FuelImportRowError(
    int RowNumber,
    string? CardNumber,
    string Code,
    string Message);

public sealed record FuelImportResponse(
    Guid ImportId,
    string Provider,
    string ProviderNameAr,
    DateOnly ReportMonth,
    DateTimeOffset? ReportThroughAtUtc,
    string OriginalFileName,
    string Sha256Checksum,
    int SourceRows,
    int CardRows,
    int CreatedCards,
    int CreatedMonthlyRecords,
    int UpdatedMonthlyRecords,
    int UnassignedCards,
    int InvalidRows,
    IReadOnlyList<FuelImportRowError> Errors,
    DateTimeOffset ImportedAtUtc);

public sealed record FuelImportHistoryResponse(
    Guid Id,
    string Provider,
    string ProviderNameAr,
    DateOnly ReportMonth,
    DateTimeOffset? ReportThroughAtUtc,
    string OriginalFileName,
    string Sha256Checksum,
    int SourceRows,
    int CardRows,
    int CreatedCards,
    int CreatedMonthlyRecords,
    int UpdatedMonthlyRecords,
    int UnassignedCards,
    int InvalidRows,
    DateTimeOffset ImportedAtUtc,
    Guid? ImportedByUserId);
