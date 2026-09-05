using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Maintenance;

public sealed class MaintenanceWorkOrder : AuditableEntity
{
    public string WorkOrderNumber { get; set; } = string.Empty;
    public MaintenanceServiceSubjectType ServiceSubjectType { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? VehicleIssueId { get; set; }
    public Guid MaintenanceLocationId { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public MaintenanceWorkOrderStatus Status { get; set; } = MaintenanceWorkOrderStatus.Open;
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public long? OdometerAtOpen { get; set; }
    public long? OdometerAtCompletion { get; set; }
    public string? Diagnosis { get; set; }
    public string? WorkPerformed { get; set; }
    public string? QualityCheckNotes { get; set; }
    public Guid OpenedByUserId { get; set; }
    public Guid? AssignedTechnicianUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public Guid? RiderVehicleAssignmentId { get; set; }
    public Guid? AttributedRiderProfileId { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal ActualOtherCost { get; set; }
    public decimal ActualTotalCost { get; set; }
    public string? Notes { get; set; }
}

public sealed class ExternalVehicleSnapshot : HistoryEntity
{
    public Guid MaintenanceWorkOrderId { get; set; }
    public string? PlateOrReference { get; set; }
    public VehicleType? VehicleType { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
}

public sealed class MaintenanceMaterialUsage : HistoryEntity
{
    public Guid MaintenanceWorkOrderId { get; set; }
    public Guid InventoryItemId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public MaintenanceUsageType UsageType { get; set; }
    public MaintenanceUsageDirection Direction { get; set; } = MaintenanceUsageDirection.Issue;
    public decimal Quantity { get; set; }
    public InventoryUnitOfMeasure UnitOfMeasure { get; set; }
    public decimal TotalCost { get; set; }
    public Guid StockMovementId { get; set; }
    public Guid StockMovementLineId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? RiderVehicleAssignmentId { get; set; }
    public Guid? RiderProfileId { get; set; }
    public InventoryAttributionStatus AttributionStatus { get; set; }
    public DateTimeOffset UsedAtUtc { get; set; }
    public Guid UsedByUserId { get; set; }
    public string? Notes { get; set; }
    public Guid? ReversalOfUsageId { get; set; }
}

public sealed class MaintenanceLaborEntry : HistoryEntity
{
    public Guid MaintenanceWorkOrderId { get; set; }
    public Guid? TechnicianUserId { get; set; }
    public string? ExternalTechnicianName { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset EndedAtUtc { get; set; }
    public decimal Hours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class MaintenancePlan : AuditableEntity
{
    public static readonly Guid CarOilPlanId = Guid.Parse("019d77f0-0000-7000-8000-000000000005");
    public static readonly Guid MotorcycleOilPlanId = Guid.Parse("019d77f0-0000-7000-8000-000000000006");

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public Guid? VehicleModelId { get; set; }
    public VehicleType? VehicleType { get; set; }
    public MaintenanceTriggerType TriggerType { get; set; }
    public int? IntervalDays { get; set; }
    public long? IntervalKilometers { get; set; }
    public long? ReminderAfterKilometers { get; set; }
    public long? MaximumAfterKilometers { get; set; }
    public int? AlertDaysBefore { get; set; }
    public long? AlertKilometersBefore { get; set; }
    public Guid? InventoryItemId { get; set; }
    public decimal? DefaultOilQuantityLiters { get; set; }
    public string? ChecklistJson { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}

public sealed class VehicleMaintenanceSchedule : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public Guid MaintenancePlanId { get; set; }
    public Guid? LastCompletedWorkOrderId { get; set; }
    public DateTimeOffset? LastCompletedAtUtc { get; set; }
    public long? LastCompletedOdometer { get; set; }
    public DateOnly? NextDueOn { get; set; }
    public long? ReminderFromOdometer { get; set; }
    public long? MaximumDueOdometer { get; set; }
    public MaintenanceDueStatus ComputedStatus { get; set; }
    public DateTimeOffset ComputedAtUtc { get; set; }
}

public sealed class OilChangeOperation : HistoryEntity
{
    public Guid MaintenanceWorkOrderId { get; set; }
    public DateTimeOffset PerformedAtUtc { get; set; }
    public long OdometerAtChange { get; set; }
    public VehicleType VehicleTypeSnapshot { get; set; }
    public Guid OilInventoryItemId { get; set; }
    public decimal OilQuantityLiters { get; set; }
    public Guid OilMaterialUsageId { get; set; }
    public decimal OilCost { get; set; }
    public bool OilFilterChanged { get; set; }
    public Guid? OilFilterInventoryItemId { get; set; }
    public Guid? OilFilterMaterialUsageId { get; set; }
    public decimal OilFilterCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string? Notes { get; set; }
}

public sealed class VehicleExpense : HistoryEntity
{
    public Guid VehicleId { get; set; }
    public Guid? RiderVehicleAssignmentId { get; set; }
    public Guid? RiderProfileId { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid SourceEntityId { get; set; }
    public DateOnly OccurredOn { get; set; }
    public decimal AmountBeforeTax { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public string Description { get; set; } = string.Empty;
    public Guid? ReversalOfExpenseId { get; set; }
}

public sealed class ExternalPartSaleLine : HistoryEntity
{
    public Guid MaintenanceWorkOrderId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal SellingUnitPriceBeforeTax { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public Guid MaintenanceMaterialUsageId { get; set; }
    public decimal InventoryCost { get; set; }
    public decimal PartsGrossProfit { get; set; }
}

public sealed class ExternalMaintenanceFinancialEntry : HistoryEntity
{
    public Guid MaintenanceWorkOrderId { get; set; }
    public ExternalFinancialEntryType EntryType { get; set; }
    public ExternalFinancialSourceType SourceType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public decimal AmountBeforeTax { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public string Description { get; set; } = string.Empty;
    public Guid RecordedByUserId { get; set; }
    public Guid? MechanicEmployeeId { get; set; }
    public string? ExternalMechanicName { get; set; }
    public Guid? ReversalOfEntryId { get; set; }
}

public sealed class ExternalCustomerPayment : HistoryEntity
{
    public Guid MaintenanceWorkOrderId { get; set; }
    public DateTimeOffset PaidAtUtc { get; set; }
    public decimal Amount { get; set; }
    public WorkshopPaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public Guid RecordedByUserId { get; set; }
    public Guid? ReversalOfPaymentId { get; set; }
}
