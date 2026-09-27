using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class VehicleRiderPeriodReportService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider) : IVehicleRiderPeriodReportService
{
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    public async Task<Result<VehicleAssignmentsPeriodReport>> GetByVehicleAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        if (!ValidPeriod(fromDate, toDate))
            return Result.Failure<VehicleAssignmentsPeriodReport>(VehicleRiderPeriodReportErrors.InvalidPeriod);

        var (asOfUtc, vehicles, assignments) = await LoadAsync(fromDate, toDate, cancellationToken);
        var assignmentsByVehicle = assignments.GroupBy(x => x.VehicleId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var rows = vehicles.OrderBy(x => x.AssetNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id)
            .Select(vehicle =>
            {
                var items = assignmentsByVehicle.GetValueOrDefault(vehicle.Id) ?? [];
                return new VehicleAssignmentsPeriodRow(
                    vehicle.Id, vehicle.AssetNumber, vehicle.SerialNumber, vehicle.PlateNumberAr,
                    items.Sum(x => x.DaysInPeriod), items);
            }).ToArray();
        return Result.Success(new VehicleAssignmentsPeriodReport(fromDate, toDate, asOfUtc, rows));
    }

    public async Task<Result<RiderAssignmentsPeriodReport>> GetByRiderAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        if (!ValidPeriod(fromDate, toDate))
            return Result.Failure<RiderAssignmentsPeriodReport>(VehicleRiderPeriodReportErrors.InvalidPeriod);

        var (asOfUtc, _, assignments) = await LoadAsync(fromDate, toDate, cancellationToken);
        var rows = assignments.GroupBy(RiderKey, StringComparer.Ordinal)
            .Select(group =>
            {
                var items = group.OrderBy(x => x.PeriodStartedAtUtc).ThenBy(x => x.AssignmentId).ToArray();
                var primary = items.OrderByDescending(x => x.IsRealRider).First();
                return new RiderAssignmentsPeriodRow(
                    group.Key,
                    items.FirstOrDefault(x => x.IsRealRider)?.AssignedRiderProfileId,
                    primary.ActualRiderName,
                    primary.ActualRiderIqamaNo,
                    items.Sum(x => x.DaysInPeriod),
                    items);
            })
            .OrderBy(x => x.RiderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.RiderKey, StringComparer.Ordinal)
            .ToArray();
        return Result.Success(new RiderAssignmentsPeriodReport(fromDate, toDate, asOfUtc, rows));
    }

    private async Task<(DateTimeOffset AsOfUtc, VehicleLookup[] Vehicles,
        VehicleRiderPeriodAssignment[] Assignments)> LoadAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var asOfUtc = timeProvider.GetUtcNow();
        var reportStartUtc = StartOfDayUtc(fromDate);
        var reportEndUtc = StartOfDayUtc(toDate.AddDays(1));
        var vehicles = await dbContext.Vehicles.IgnoreQueryFilters().AsNoTracking()
            .Select(vehicle => new VehicleLookup(vehicle.Id, vehicle.AssetNumber,
                vehicle.SerialNumber, vehicle.PlateNumberAr))
            .ToArrayAsync(cancellationToken);
        var vehicleById = vehicles.ToDictionary(x => x.Id);
        var assignments = await dbContext.RiderVehicleAssignments.AsNoTracking()
            .Where(assignment => assignment.StartedAtUtc < reportEndUtc
                && (assignment.EndedAtUtc == null || assignment.EndedAtUtc > reportStartUtc))
            .OrderBy(assignment => assignment.StartedAtUtc)
            .ToArrayAsync(cancellationToken);
        var assignmentIds = assignments.Select(x => x.Id).ToArray();
        var profileIds = assignments.Select(x => x.RiderProfileId).Distinct().ToArray();
        var riders = await (
            from profile in dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking()
            join employee in dbContext.Employees.IgnoreQueryFilters().AsNoTracking()
                on profile.EmployeeId equals employee.Id
            where profileIds.Contains(profile.Id)
            select new AssignedRiderLookup(profile.Id, profile.EmployeeId,
                employee.FullNameAr, employee.IqamaNo))
            .ToDictionaryAsync(x => x.ProfileId, cancellationToken);
        var realRiders = await dbContext.RealRiders.IgnoreQueryFilters().AsNoTracking()
            .Where(x => assignmentIds.Contains(x.RiderVehicleAssignmentId))
            .ToDictionaryAsync(x => x.RiderVehicleAssignmentId, cancellationToken);

        var rows = new List<VehicleRiderPeriodAssignment>(assignments.Length);
        foreach (var assignment in assignments)
        {
            if (!vehicleById.TryGetValue(assignment.VehicleId, out var vehicle)) continue;
            var assignmentEndUtc = assignment.EndedAtUtc ?? asOfUtc;
            var periodStartUtc = assignment.StartedAtUtc > reportStartUtc
                ? assignment.StartedAtUtc : reportStartUtc;
            var periodEndUtc = assignmentEndUtc < reportEndUtc
                ? assignmentEndUtc : reportEndUtc;
            if (periodEndUtc <= periodStartUtc) continue;

            riders.TryGetValue(assignment.RiderProfileId, out var assignedRider);
            realRiders.TryGetValue(assignment.Id, out var realRider);
            rows.Add(new VehicleRiderPeriodAssignment(
                assignment.Id,
                vehicle.Id,
                vehicle.AssetNumber,
                vehicle.SerialNumber,
                vehicle.PlateNumberAr,
                assignment.RiderProfileId,
                assignedRider?.EmployeeId,
                assignedRider?.Name,
                assignedRider?.IqamaNo,
                assignment.IsRealRider,
                assignment.IsRealRider ? assignment.RiderProfileId : realRider?.Id,
                assignment.IsRealRider ? assignedRider?.Name : realRider?.Name,
                assignment.IsRealRider ? assignedRider?.IqamaNo : realRider?.IqamaNo,
                assignment.IsRealRider ? null : realRider?.RelationshipToAssignedRider,
                assignment.StartedAtUtc,
                assignment.EndedAtUtc,
                periodStartUtc,
                periodEndUtc,
                Days(periodStartUtc, periodEndUtc),
                Days(assignment.StartedAtUtc, assignmentEndUtc)));
        }
        return (asOfUtc, vehicles, rows.ToArray());
    }

    private static bool ValidPeriod(DateOnly fromDate, DateOnly toDate) =>
        fromDate != default && toDate != default && toDate >= fromDate && toDate < DateOnly.MaxValue;

    private static DateTimeOffset StartOfDayUtc(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), RiyadhOffset).ToUniversalTime();

    private static decimal Days(DateTimeOffset start, DateTimeOffset end) =>
        Math.Round((decimal)(end - start).Ticks / TimeSpan.TicksPerDay, 4);

    private static string RiderKey(VehicleRiderPeriodAssignment assignment)
    {
        if (!string.IsNullOrWhiteSpace(assignment.ActualRiderIqamaNo))
            return $"iqama:{assignment.ActualRiderIqamaNo}";
        return assignment.IsRealRider
            ? $"profile:{assignment.AssignedRiderProfileId:N}"
            : $"unidentified:{assignment.AssignmentId:N}";
    }

    private sealed record VehicleLookup(Guid Id, string AssetNumber, string? SerialNumber, string? PlateNumberAr);
    private sealed record AssignedRiderLookup(Guid ProfileId, Guid EmployeeId, string Name, string? IqamaNo);
}
