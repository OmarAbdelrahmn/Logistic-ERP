using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fuel;

public sealed class FuelCard : AuditableEntity
{
    public FuelCardProvider Provider { get; set; }
    public FuelCardIdentifierType IdentifierType { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string NormalizedCardNumber { get; set; } = string.Empty;
    public string? PlateNumberText { get; set; }
    public string? NormalizedPlateNumber { get; set; }
    public string? Notes { get; set; }
}

public sealed class FuelCardRiderAssignment : TemporalPeriodEntity
{
    public Guid FuelCardId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public string AssignmentReason { get; set; } = string.Empty;
    public string? EndReason { get; set; }
    public string? Notes { get; set; }
}

public sealed class FuelCardMonthlyUsage : AuditableEntity
{
    public Guid FuelCardId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly ReportMonth { get; set; }
    public decimal TotalLiters { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? AmountBeforeTax { get; set; }
    public decimal? VatAmount { get; set; }
    public int? TransactionCount { get; set; }
    public string? FuelType { get; set; }
    public string? SourcePlateNumber { get; set; }
    public string? NormalizedSourcePlateNumber { get; set; }
    public DateTimeOffset? FirstTransactionAtUtc { get; set; }
    public DateTimeOffset? LastTransactionAtUtc { get; set; }
    public DateTimeOffset? ReportThroughAtUtc { get; set; }
    public Guid LastImportId { get; set; }
}

public sealed class FuelCardImport : HistoryEntity
{
    public FuelCardProvider Provider { get; set; }
    public DateOnly ReportMonth { get; set; }
    public DateTimeOffset? ReportThroughAtUtc { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Sha256Checksum { get; set; } = string.Empty;
    public int SourceRows { get; set; }
    public int CardRows { get; set; }
    public int CreatedCards { get; set; }
    public int CreatedMonthlyRecords { get; set; }
    public int UpdatedMonthlyRecords { get; set; }
    public int UnassignedCards { get; set; }
    public int InvalidRows { get; set; }
    public string RowErrorsJson { get; set; } = "[]";
}
