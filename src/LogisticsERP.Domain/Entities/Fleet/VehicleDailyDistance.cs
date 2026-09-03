using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehicleDailyDistance : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public DateOnly WorkDate { get; set; }
    public decimal? GpsDistanceKm { get; set; }
    public string? GpsPlateNumber { get; set; }
    public Guid? LastGpsImportId { get; set; }
    public DateTimeOffset? GpsImportedAtUtc { get; set; }
    public Guid? GpsImportedByUserId { get; set; }
    public long? ManualOdometerReading { get; set; }
    public long? ManualBaselineOdometerReading { get; set; }
    public decimal? ManualDistanceKm { get; set; }
    public DateTimeOffset? ManualEnteredAtUtc { get; set; }
    public Guid? ManualEnteredByUserId { get; set; }
    public string? ManualNotes { get; set; }
    public decimal AppliedDistanceKm { get; set; }
    public VehicleDailyDistanceSource AppliedSource { get; set; }
}

public sealed class VehicleDailyDistanceImport : HistoryEntity
{
    public DateOnly WorkDate { get; set; }
    public DateTimeOffset? PeriodStartUtc { get; set; }
    public DateTimeOffset? PeriodEndUtc { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Sha256Checksum { get; set; } = string.Empty;
    public int TotalVehicleRows { get; set; }
    public int GpsRows { get; set; }
    public int NoGpsRows { get; set; }
    public int MatchedRows { get; set; }
    public int CreatedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int UnmatchedRows { get; set; }
    public int InvalidRows { get; set; }
    public string RowErrorsJson { get; set; } = "[]";
}
