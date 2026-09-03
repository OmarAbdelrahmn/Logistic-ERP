using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Telecom;

public interface IPhoneSimService
{
    Task<Result<PhoneSimPageResponse>> GetAllAsync(
        string? search,
        string? status,
        Guid? responsibleEmployeeId,
        Guid? riderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PhoneSimResponse>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PrivateFileDownload>> DownloadReceiptFormAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PhoneSimResponse>> CreateAsync(
        CreatePhoneSimRequest request,
        PrivateFileUpload receiptForm,
        CancellationToken cancellationToken = default);

    Task<Result<PhoneSimResponse>> UpdateAsync(
        Guid id,
        UpdatePhoneSimRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PhoneSimResponse>> ChangeResponsibleEmployeeAsync(
        Guid id,
        ChangePhoneSimResponsibleEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PhoneSimResponse>> ChangeStatusAsync(
        Guid id,
        ChangePhoneSimStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ArchiveAsync(
        Guid id,
        ArchivePhoneSimRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PhoneSimResponsibilityHistoryResponse>>> GetResponsibilityHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PhoneSimAssignmentResponse>>> GetAssignmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PhoneSimAssignmentResponse>> AssignAsync(
        Guid id,
        AssignPhoneSimRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PhoneSimAssignmentResponse>> CloseAssignmentAsync(
        Guid id,
        Guid assignmentId,
        ClosePhoneSimAssignmentRequest request,
        CancellationToken cancellationToken = default);
}
