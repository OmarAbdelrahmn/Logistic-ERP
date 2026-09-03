namespace LogisticsERP.Application.Features.Telecom;

public sealed record PhoneSimPageResponse(
    IReadOnlyList<PhoneSimResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PhoneSimResponse(
    Guid Id,
    string PhoneNumber,
    string? Iccid,
    string? CarrierName,
    string Status,
    string? StatusReason,
    Guid ResponsibleEmployeeId,
    string ResponsibleEmployeeNameAr,
    string? ResponsibleEmployeeNameEn,
    PhoneSimCurrentRiderResponse? CurrentRider,
    string? Notes,
    PhoneSimReceiptFormResponse? ReceiptForm,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);

public sealed record PhoneSimReceiptFormResponse(
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Sha256Checksum);

public sealed record PhoneSimCurrentRiderResponse(
    Guid AssignmentId,
    Guid RiderProfileId,
    Guid EmployeeId,
    string FullNameAr,
    string? FullNameEn,
    DateOnly EffectiveFrom,
    string RowVersion);

public sealed record CreatePhoneSimRequest(
    string PhoneNumber,
    string? Iccid,
    string? CarrierName,
    Guid ResponsibleEmployeeId,
    string? Notes);

public sealed record UpdatePhoneSimRequest(
    string PhoneNumber,
    string? Iccid,
    string? CarrierName,
    string? Notes,
    string RowVersion);

public sealed record ChangePhoneSimResponsibleEmployeeRequest(
    Guid ResponsibleEmployeeId,
    string Reason,
    string RowVersion);

public sealed record ChangePhoneSimStatusRequest(
    string Status,
    string Reason,
    string RowVersion);

public sealed record ArchivePhoneSimRequest(
    string Reason,
    string RowVersion);

public sealed record AssignPhoneSimRequest(
    Guid RiderProfileId,
    DateOnly EffectiveFrom,
    string Reason,
    string? Notes,
    string RowVersion);

public sealed record ClosePhoneSimAssignmentRequest(
    DateOnly EffectiveTo,
    string Reason,
    string RowVersion);

public sealed record PhoneSimResponsibilityHistoryResponse(
    Guid Id,
    Guid PhoneSimCardId,
    Guid? PreviousResponsibleEmployeeId,
    string? PreviousResponsibleEmployeeNameAr,
    string? PreviousResponsibleEmployeeNameEn,
    Guid ResponsibleEmployeeId,
    string ResponsibleEmployeeNameAr,
    string? ResponsibleEmployeeNameEn,
    DateTimeOffset ChangedAtUtc,
    Guid ChangedByUserId,
    string Reason);

public sealed record PhoneSimAssignmentResponse(
    Guid Id,
    Guid PhoneSimCardId,
    string PhoneNumber,
    Guid RiderProfileId,
    Guid EmployeeId,
    string RiderNameAr,
    string? RiderNameEn,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? AssignmentReason,
    string? EndReason,
    string? Notes,
    Guid AssignedByUserId,
    Guid? ClosedByUserId,
    string RowVersion);
