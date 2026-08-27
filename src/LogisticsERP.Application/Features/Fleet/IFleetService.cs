using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Fleet;

public interface IFleetService
{
    Task<Result<IReadOnlyList<VehicleManufacturerResponse>>> GetManufacturersAsync(CancellationToken cancellationToken = default);
    Task<Result<VehicleManufacturerResponse>> UpsertManufacturerAsync(Guid? id, VehicleManufacturerRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleModelResponse>>> GetModelsAsync(Guid? manufacturerId, CancellationToken cancellationToken = default);
    Task<Result<VehicleModelResponse>> UpsertModelAsync(Guid? id, VehicleModelRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleSupplierResponse>>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<Result<VehicleSupplierResponse>> GetSupplierAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<VehicleSupplierResponse>> UpsertSupplierAsync(Guid? id, VehicleSupplierRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveSupplierAsync(Guid id, ArchiveFleetRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResponse<VehicleSummaryResponse>>> GetVehiclesAsync(string? search, string? status, Guid? operatingCityId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleLookupResponse>>> LookupVehiclesAsync(string? search, CancellationToken cancellationToken = default);
    Task<Result<VehicleDetailResponse>> GetVehicleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<VehicleDetailResponse>> UpsertVehicleAsync(Guid? id, VehicleUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleReadinessResponse>> GetReadinessAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<VehicleDetailResponse>> CorrectIdentityAsync(Guid id, VehicleIdentityCorrectionRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleIdentityCorrectionResponse>>> GetIdentityCorrectionHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<VehicleRegistrationTransitionResponse>> TransitionToPublicTransportAsync(Guid id, VehicleRegistrationTransitionRequest request, PrivateFileUpload istimara, PrivateFileUpload operationCard, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleRegistrationTransitionResponse>>> GetRegistrationTransitionHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> ArchiveVehicleAsync(Guid id, ArchiveFleetRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleDetailResponse>> RestoreVehicleAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default);
    Task<Result<VehicleDetailResponse>> ChangeAdministrativeStatusAsync(Guid id, string action, VehicleStatusCommandRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleStatusPeriodResponse>>> GetStatusHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<Result<VehicleOdometerReadingResponse>> RecordOdometerAsync(Guid vehicleId, OdometerReadingRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleOdometerReadingResponse>>> GetOdometerHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<Result<RiderVehicleAssignmentResponse>> TakeAsync(TakeVehicleRequest request, IReadOnlyList<PrivateFileUpload> promissoryFiles, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<RiderVehicleAssignmentResponse>> ReturnAsync(ReturnVehicleRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<RiderVehicleAssignmentResponse>> SwitchAsync(SwitchVehicleRequest request, IReadOnlyList<PrivateFileUpload> promissoryFiles, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<RiderVehicleAssignmentResponse>> RenewPermissionAsync(Guid assignmentId, RenewVehiclePermissionRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RiderVehicleTimelineResponse>>> GetVehicleTimelineAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RiderVehicleTimelineResponse>>> GetRiderTimelineAsync(Guid riderProfileId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleComplianceResponse>>> GetComplianceAsync(Guid vehicleId, string type, CancellationToken cancellationToken = default);
    Task<Result<VehicleComplianceResponse>> RenewRegistrationAsync(Guid vehicleId, VehicleRegistrationRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleComplianceResponse>> RenewInsuranceAsync(Guid vehicleId, VehicleInsuranceRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleComplianceResponse>> RenewInspectionAsync(Guid vehicleId, VehicleInspectionRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleComplianceDueResponse>>> GetComplianceDueAsync(DateOnly checkDate, CancellationToken cancellationToken = default);
    Task<Result<PagedResponse<VehicleIssueSummaryResponse>>> GetIssuesAsync(Guid? vehicleId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<VehicleIssueSummaryResponse>> CreateIssueAsync(CreateVehicleIssueRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<VehicleIssueSummaryResponse>> ActOnIssueAsync(Guid issueId, string action, VehicleIssueActionRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleIssueSummaryResponse>> ResolveIssueAsync(Guid issueId, ResolveVehicleIssueRequest request, CancellationToken cancellationToken = default);
}

public interface IVehicleFileService
{
    Task<Result<IReadOnlyList<VehicleAttachmentResponse>>> GetAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<Result<VehicleAttachmentResponse>> UploadSlotAsync(Guid vehicleId, VehicleFileKind kind, PrivateFileUpload file, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<VehicleAttachmentVersionResponse>>> GetVersionsAsync(Guid vehicleId, Guid attachmentId, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> DownloadAsync(Guid vehicleId, Guid attachmentId, Guid? versionId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RiderPromissoryFileResponse>>> GetRiderPromissoryFilesAsync(Guid riderProfileId, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> DownloadRiderPromissoryFileAsync(Guid riderProfileId, Guid fileId, Guid? versionId, CancellationToken cancellationToken = default);
}

public interface IVehicleAccidentService
{
    Task<Result<PagedResponse<VehicleAccidentSummaryResponse>>> GetAsync(Guid? vehicleId, Guid? riderProfileId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<VehicleAccidentDetailResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<VehicleAccidentDetailResponse>> CreateAsync(CreateVehicleAccidentRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<VehicleAccidentAttachmentResponse>> UploadEvidenceAsync(Guid accidentId, VehicleAccidentEvidenceType evidenceType, PrivateFileUpload file, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> DownloadEvidenceAsync(Guid accidentId, Guid attachmentId, CancellationToken cancellationToken = default);
    Task<Result<VehicleAccidentReportVersionResponse>> FinalizeAsync(Guid accidentId, AccidentActionRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleAccidentReportVersionResponse>> CorrectAsync(Guid accidentId, CorrectVehicleAccidentRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleAccidentDetailResponse>> CloseAsync(Guid accidentId, AccidentActionRequest request, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> DownloadReportAsync(Guid accidentId, Guid? reportVersionId, CancellationToken cancellationToken = default);
}
