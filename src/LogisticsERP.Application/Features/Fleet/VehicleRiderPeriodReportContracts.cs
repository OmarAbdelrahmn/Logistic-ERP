using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fleet;

public interface IVehicleRiderPeriodReportService
{
    Task<Result<VehicleAssignmentsPeriodReport>> GetByVehicleAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    Task<Result<RiderAssignmentsPeriodReport>> GetByRiderAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}

public sealed record VehicleAssignmentsPeriodReport(
    DateOnly FromDate, DateOnly ToDate, DateTimeOffset AsOfUtc,
    IReadOnlyList<VehicleAssignmentsPeriodRow> Vehicles);

public sealed record VehicleAssignmentsPeriodRow(
    Guid VehicleId, string AssetNumber, string? SerialNumber, string? PlateNumberAr,
    decimal TotalDaysAssignedInPeriod,
    IReadOnlyList<VehicleRiderPeriodAssignment> Assignments);

public sealed record RiderAssignmentsPeriodReport(
    DateOnly FromDate, DateOnly ToDate, DateTimeOffset AsOfUtc,
    IReadOnlyList<RiderAssignmentsPeriodRow> Riders);

public sealed record RiderAssignmentsPeriodRow(
    string RiderKey, Guid? RiderProfileId, string? RiderName, string? RiderIqamaNo,
    decimal TotalDaysWithVehiclesInPeriod,
    IReadOnlyList<VehicleRiderPeriodAssignment> Assignments);

public sealed record VehicleRiderPeriodAssignment(
    Guid AssignmentId,
    Guid VehicleId,
    string AssetNumber,
    string? SerialNumber,
    string? PlateNumberAr,
    Guid AssignedRiderProfileId,
    Guid? AssignedEmployeeId,
    string? AssignedRiderName,
    string? AssignedRiderIqamaNo,
    bool IsRealRider,
    Guid? ActualRiderId,
    string? ActualRiderName,
    string? ActualRiderIqamaNo,
    string? RelationshipToAssignedRider,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    DateTimeOffset PeriodStartedAtUtc,
    DateTimeOffset PeriodEndedAtUtc,
    decimal DaysInPeriod,
    decimal TotalAssignmentDays);

public static class VehicleRiderPeriodReportErrors
{
    public static readonly OperationError InvalidPeriod = new(
        "fleet.assignment_period.invalid_period",
        "حدد تاريخ بداية ونهاية صالحين، على أن لا يسبق تاريخ النهاية تاريخ البداية.",
        ErrorType.Validation,
        "toDate");
}
