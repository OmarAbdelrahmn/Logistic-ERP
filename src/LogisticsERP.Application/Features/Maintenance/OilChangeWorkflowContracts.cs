using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Maintenance;

public sealed record OilChangeEditRequest(
    DateTimeOffset PerformedAtUtc, long OdometerAtChange, Guid InventoryLocationId,
    Guid OilInventoryItemId, decimal? ConfiguredOilQuantityLiters, bool OilFilterChanged,
    Guid? OilFilterInventoryItemId, Guid? NextOilBarrelId, decimal LaborCost, decimal OtherCost,
    string? Notes, string Reason, string RowVersion, string? WorkOrderRowVersion,
    string? ExpectedEffectsHash = null, decimal? MileageBaselineKm = null);

public sealed record OilChangeCancelRequest(string Reason, string RowVersion,
    string? WorkOrderRowVersion = null, string? ExpectedEffectsHash = null, decimal? MileageBaselineKm = null);

public sealed record OilChangeWorkflowDetails(OilChangeReportResponse Operation,
    string? WorkOrderRowVersion, Guid InventoryLocationId,
    IReadOnlyList<MaintenanceMaterialUsageResponse> Materials,
    DateTimeOffset? CancelledAtUtc = null, Guid? CancelledByUserId = null, string? CancellationReason = null);

public sealed record OilChangeWorkflowEffects(bool Cancelled, bool MaterialsReplaced,
    decimal OilQuantityLiters, decimal OilCost, decimal FilterCost, decimal LaborCost, decimal OtherCost,
    decimal TotalCost, Guid? RiderProfileId, decimal? VehicleMileageKm,
    IReadOnlyList<OilChangeBarrelEffect> Barrels, MaintenanceWorkOrderStatus? WorkOrderStatus = null,
    decimal ReturnedAsWasteLiters = 0);

public sealed record OilChangeBarrelEffect(Guid BarrelId, decimal IssuedLiters, decimal ReversedLiters);

public sealed record OilChangeWorkflowResponse(Guid OperationId, Guid? RevisionId, bool Preview,
    string RowVersion, string? WorkOrderRowVersion, DateTimeOffset PostedAtUtc,
    string EffectsHash, OilChangeWorkflowEffects Effects);

public sealed record OilChangeRevisionResponse(Guid Id, int RevisionNumber, string Action,
    string Reason, Guid ActorUserId, DateTimeOffset PostedAtUtc, string BeforeJson,
    string AfterJson, string EffectsJson);

public sealed record OilChangeRevisionPage(IReadOnlyList<OilChangeRevisionResponse> Items,
    int Page, int PageSize, int TotalCount);

public static class OilChangeWorkflowErrors
{
    public static readonly OperationError Cancelled = new("maintenance.oil_change_cancelled", "تم إلغاء عملية الزيت.", ErrorType.Conflict);
    public static readonly OperationError PreviewChanged = new("maintenance.oil_change_preview_changed", "تغير أثر العملية؛ أعد المعاينة.", ErrorType.Conflict);
    public static readonly OperationError MissingMileageBasis = new("maintenance.oil_change_mileage_basis_required", "لا يمكن استنتاج أساس العداد؛ أدخل أساسًا موثقًا أو صحح سجل العداد أولاً.", ErrorType.Conflict, "mileageBaselineKm");
    public static readonly OperationError InvalidMileage = new("maintenance.oil_change_mileage_conflict", "التصحيح يتعارض مع تسلسل قراءات العداد.", ErrorType.Conflict);
    public static readonly OperationError BarrelRestore = new("maintenance.oil_change_barrel_restore_conflict", "يتطلب استرجاع الزيت تسوية البرميل المرتجع أو البرميل المفتوح المتعارض أولاً.", ErrorType.Conflict);
}
