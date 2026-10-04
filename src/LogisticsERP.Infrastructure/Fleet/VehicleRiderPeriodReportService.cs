using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class VehicleRiderPeriodReportService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider) : IVehicleRiderPeriodReportService
{
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);
    private static readonly TimeSpan BillingCutoff = TimeSpan.FromHours(14);

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
                var items = (assignmentsByVehicle.GetValueOrDefault(vehicle.Id) ?? [])
                    .Select(x => PriceAssignment(x, vehicle.VehicleType)).ToArray();
                return new VehicleAssignmentsPeriodRow(
                    vehicle.Id, vehicle.AssetNumber, vehicle.SerialNumber, vehicle.PlateNumberAr,
                    vehicle.SponsorId, vehicle.SponsorName,
                    items.Sum(x => x.DaysInPeriod), TotalCost(items), items);
            }).ToArray();
        return Result.Success(new VehicleAssignmentsPeriodReport(fromDate, toDate, asOfUtc, rows));
    }

    public async Task<Result<RiderAssignmentsPeriodReport>> GetByRiderAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        if (!ValidPeriod(fromDate, toDate))
            return Result.Failure<RiderAssignmentsPeriodReport>(VehicleRiderPeriodReportErrors.InvalidPeriod);

        var (asOfUtc, vehicles, assignments) = await LoadAsync(fromDate, toDate, cancellationToken);
        var vehicleById = vehicles.ToDictionary(x => x.Id);
        var rows = assignments.GroupBy(RiderKey, StringComparer.Ordinal)
            .Select(group =>
            {
                var items = group.OrderBy(x => x.PeriodStartedAtUtc).ThenBy(x => x.AssignmentId)
                    .Select(x => PriceAssignment(x, vehicleById[x.VehicleId].VehicleType)).ToArray();
                var primary = items.OrderByDescending(x => x.IsRealRider).First();
                return new RiderAssignmentsPeriodRow(
                    group.Key,
                    items.FirstOrDefault(x => x.IsRealRider)?.AssignedRiderProfileId,
                    primary.ActualRiderName,
                    primary.ActualRiderIqamaNo,
                    items.Sum(x => x.DaysInPeriod),
                    TotalCost(items),
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
        var vehicles = await (
            from vehicle in dbContext.Vehicles.IgnoreQueryFilters().AsNoTracking()
            join sponsor in dbContext.Sponsors.IgnoreQueryFilters().AsNoTracking()
                on vehicle.SponsorId equals sponsor.Id into sponsors
            from sponsor in sponsors.DefaultIfEmpty()
            select new VehicleLookup(vehicle.Id, vehicle.AssetNumber,
                vehicle.SerialNumber, vehicle.PlateNumberAr, vehicle.VehicleType,
                vehicle.SponsorId, sponsor == null ? null : sponsor.RegistryNameAr))
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
                Days(assignment.StartedAtUtc, assignmentEndUtc, fromDate, toDate),
                Days(assignment.StartedAtUtc, assignmentEndUtc)));
        }
        return (asOfUtc, vehicles, rows.ToArray());
    }

    private static bool ValidPeriod(DateOnly fromDate, DateOnly toDate) =>
        fromDate != default && toDate != default && toDate >= fromDate && toDate < DateOnly.MaxValue;

    private static DateTimeOffset StartOfDayUtc(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), RiyadhOffset).ToUniversalTime();

    private static decimal Days(
        DateTimeOffset start, DateTimeOffset end, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var firstDay = BillingDayBoundary(start);
        var endDayExclusive = BillingDayBoundary(end);
        if (fromDate.HasValue) firstDay = Math.Max(firstDay, fromDate.Value.DayNumber);
        if (toDate.HasValue) endDayExclusive = Math.Min(endDayExclusive, toDate.Value.DayNumber + 1);
        return Math.Max(0, endDayExclusive - firstDay);
    }

    private static int BillingDayBoundary(DateTimeOffset timestamp)
    {
        var local = timestamp.ToOffset(RiyadhOffset);
        // Pickup after 14:00 starts billing tomorrow; return after 14:00 includes today.
        return DateOnly.FromDateTime(local.DateTime).DayNumber
            + (local.TimeOfDay > BillingCutoff ? 1 : 0);
    }

    private static RiderVehiclePeriodAssignment PriceAssignment(
        VehicleRiderPeriodAssignment assignment, VehicleType vehicleType)
    {
        decimal? monthlyCostSar = vehicleType switch
        {
            VehicleType.Car => 1800m,
            VehicleType.Motorcycle => 800m,
            _ => null
        };
        var dailyCostSar = monthlyCostSar / 30m;
        var costInPeriodSar = dailyCostSar.HasValue
            ? Math.Round(assignment.DaysInPeriod * dailyCostSar.Value,
                2, MidpointRounding.AwayFromZero)
            : (decimal?)null;
        return new RiderVehiclePeriodAssignment(
            assignment, vehicleType, monthlyCostSar, dailyCostSar, costInPeriodSar);
    }

    private static decimal? TotalCost(IReadOnlyList<RiderVehiclePeriodAssignment> assignments) =>
        assignments.All(x => x.CostInPeriodSar.HasValue)
            ? assignments.Sum(x => x.CostInPeriodSar!.Value) : null;

    private static string RiderKey(VehicleRiderPeriodAssignment assignment)
    {
        if (!string.IsNullOrWhiteSpace(assignment.ActualRiderIqamaNo))
            return $"iqama:{assignment.ActualRiderIqamaNo}";
        return assignment.IsRealRider
            ? $"profile:{assignment.AssignedRiderProfileId:N}"
            : $"unidentified:{assignment.AssignmentId:N}";
    }

    private sealed record VehicleLookup(
        Guid Id, string AssetNumber, string? SerialNumber, string? PlateNumberAr, VehicleType VehicleType,
        Guid? SponsorId, string? SponsorName);
    private sealed record AssignedRiderLookup(Guid ProfileId, Guid EmployeeId, string Name, string? IqamaNo);
}
