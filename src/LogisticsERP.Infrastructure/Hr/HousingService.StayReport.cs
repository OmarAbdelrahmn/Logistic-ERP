using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class HousingService
{
    public async Task<Result<HousingStayReportResponse>> GetStayReportAsync(
        HousingStayReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromDate > request.ToDate)
            return Result.Failure<HousingStayReportResponse>(HrErrors.Invalid("toDate", "It cannot be before 'fromDate'."));
        if (request.Page < 1 || request.PageSize is < 1 or > 5000)
            return Result.Failure<HousingStayReportResponse>(HrErrors.InvalidRequest);

        var today = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System)
            .GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        var linkedQuery =
            from period in dbContext.HousingResidencePeriods.AsNoTracking()
            join room in dbContext.HousingRooms.IgnoreQueryFilters().AsNoTracking() on period.RoomId equals room.Id
            join housing in dbContext.Housing.IgnoreQueryFilters().AsNoTracking() on room.HousingId equals housing.Id
            join employee in dbContext.Employees.IgnoreQueryFilters().AsNoTracking() on period.EmployeeId equals employee.Id
            join floor in dbContext.HousingFloors.IgnoreQueryFilters().AsNoTracking() on room.FloorId equals floor.Id into floors
            from floor in floors.DefaultIfEmpty()
            join rider in dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking() on employee.Id equals rider.EmployeeId into riders
            from rider in riders.DefaultIfEmpty()
            where (request.HousingId == null || housing.Id == request.HousingId)
                && (request.FromDate == null || period.EffectiveTo == null || period.EffectiveTo >= request.FromDate)
                && (request.ToDate == null || period.EffectiveFrom <= request.ToDate)
            select new { Period = period, Room = room, Housing = housing, Employee = employee, Floor = floor, Rider = rider };

        // Name-only and unmatched occupants have no reliable entry/exit dates. They can only
        // be included when the requested window contains the current Riyadh date.
        var includeCurrentUnlinked = (request.FromDate == null || request.FromDate <= today)
            && (request.ToDate == null || request.ToDate >= today);
        var externalQuery =
            from person in dbContext.HousingExternalOccupants.AsNoTracking()
            join room in dbContext.HousingRooms.AsNoTracking() on person.RoomId equals room.Id
            join housing in dbContext.Housing.AsNoTracking() on room.HousingId equals housing.Id
            join floor in dbContext.HousingFloors.AsNoTracking() on room.FloorId equals floor.Id into floors
            from floor in floors.DefaultIfEmpty()
            where request.HousingId == null || housing.Id == request.HousingId
            select new { Person = person, Room = room, Housing = housing, Floor = floor };
        var pendingQuery =
            from person in dbContext.HousingPendingOccupants.AsNoTracking()
            join room in dbContext.HousingRooms.AsNoTracking() on person.RoomId equals room.Id
            join housing in dbContext.Housing.AsNoTracking() on room.HousingId equals housing.Id
            join floor in dbContext.HousingFloors.AsNoTracking() on room.FloorId equals floor.Id into floors
            from floor in floors.DefaultIfEmpty()
            where request.HousingId == null || housing.Id == request.HousingId
            select new { Person = person, Room = room, Housing = housing, Floor = floor };

        var linkedCount = await linkedQuery.CountAsync(cancellationToken);
        var externalCount = includeCurrentUnlinked ? await externalQuery.CountAsync(cancellationToken) : 0;
        var pendingCount = includeCurrentUnlinked ? await pendingQuery.CountAsync(cancellationToken) : 0;
        var items = new List<HousingStayReportRow>(request.PageSize);
        var skip = ((long)request.Page - 1) * request.PageSize;

        if (skip < linkedCount)
        {
            // SQL Server cannot translate ordering by properties of a constructed record.
            // Order and page entity columns before projecting the report row.
            var rows = await linkedQuery
                .OrderBy(item => item.Housing.Code).ThenBy(item => item.Room.Name)
                .ThenBy(item => item.Period.EffectiveFrom).ThenBy(item => item.Period.Id)
                .Skip((int)skip).Take(request.PageSize)
                .Select(item => new LinkedStayProjection(
                    item.Period.Id, item.Housing.Id, item.Housing.Code, item.Housing.NameAr, item.Housing.NameEn,
                    item.Room.Id, item.Room.Name, (Guid?)item.Room.FloorId,
                    item.Floor == null ? null : item.Floor.Name,
                    item.Employee.Id, item.Rider == null ? null : (Guid?)item.Rider.Id,
                    item.Employee.IqamaNo, item.Employee.FullNameAr, item.Employee.FullNameEn,
                    item.Period.EffectiveFrom, item.Period.EffectiveTo, item.Period.MoveInReason,
                    item.Period.MoveOutReason, item.Period.SourceReference))
                .ToArrayAsync(cancellationToken);
            items.AddRange(rows.Select(item => ToStayReportRow(item, request, today)));
            skip = 0;
        }
        else skip -= linkedCount;

        if (includeCurrentUnlinked && items.Count < request.PageSize)
        {
            if (skip < externalCount)
            {
                var rows = await externalQuery
                    .OrderBy(item => item.Housing.Code).ThenBy(item => item.Room.Name).ThenBy(item => item.Person.Id)
                    .Skip((int)skip).Take(request.PageSize - items.Count)
                    .Select(item => new UnlinkedStayProjection(
                        item.Person.Id, item.Housing.Id, item.Housing.Code, item.Housing.NameAr, item.Housing.NameEn,
                        item.Room.Id, item.Room.Name, (Guid?)item.Room.FloorId,
                        item.Floor == null ? null : item.Floor.Name,
                        item.Person.Name, null, null))
                    .ToArrayAsync(cancellationToken);
                items.AddRange(rows.Select(item => ToStayReportRow(item, "External")));
                skip = 0;
            }
            else skip -= externalCount;
        }

        if (includeCurrentUnlinked && items.Count < request.PageSize && skip < pendingCount)
        {
            var rows = await pendingQuery
                .OrderBy(item => item.Housing.Code).ThenBy(item => item.Room.Name).ThenBy(item => item.Person.Id)
                .Skip((int)skip).Take(request.PageSize - items.Count)
                .Select(item => new UnlinkedStayProjection(
                    item.Person.Id, item.Housing.Id, item.Housing.Code, item.Housing.NameAr, item.Housing.NameEn,
                    item.Room.Id, item.Room.Name, (Guid?)item.Room.FloorId,
                    item.Floor == null ? null : item.Floor.Name,
                    item.Person.Name, item.Person.IqamaNo, item.Person.SourceRow))
                .ToArrayAsync(cancellationToken);
            items.AddRange(rows.Select(item => ToStayReportRow(item, "PendingMatch")));
        }

        return Result.Success(new HousingStayReportResponse(request.FromDate, request.ToDate, today,
            request.Page, request.PageSize, linkedCount + externalCount + pendingCount, items));
    }

    private static HousingStayReportRow ToStayReportRow(
        LinkedStayProjection item, HousingStayReportRequest request, DateOnly today)
    {
        var (totalDays, selectedDays) = HousingStayReportDates.CountDays(
            item.MoveInDate, item.MoveOutDate, request.FromDate, request.ToDate, today);
        return new HousingStayReportRow(
            item.RecordId, item.RiderProfileId == null ? "Employee" : "Rider",
            item.HousingId, item.HousingCode, item.HousingNameAr, item.HousingNameEn,
            item.RoomId, item.RoomName, item.FloorId, item.FloorName,
            item.EmployeeId, item.RiderProfileId, item.IqamaNo,
            item.NameAr, item.NameEn, item.MoveInDate, item.MoveOutDate,
            item.MoveInDate <= today && (item.MoveOutDate == null || item.MoveOutDate >= today),
            totalDays, selectedDays, item.MoveInReason, item.MoveOutReason, item.SourceReference);
    }

    private static HousingStayReportRow ToStayReportRow(UnlinkedStayProjection item, string recordType) =>
        new(item.RecordId, recordType, item.HousingId, item.HousingCode,
            item.HousingNameAr, item.HousingNameEn, item.RoomId, item.RoomName,
            item.FloorId, item.FloorName, null, null, item.IqamaNo,
            item.NameAr, null, null, null, true, null, null, null, null,
            item.SourceRow is null ? null : $"Riyadh workbook row {item.SourceRow}");

    private sealed record LinkedStayProjection(
        Guid RecordId, Guid HousingId, string HousingCode, string HousingNameAr, string HousingNameEn,
        Guid RoomId, string RoomName, Guid? FloorId, string? FloorName,
        Guid EmployeeId, Guid? RiderProfileId, string? IqamaNo, string NameAr, string? NameEn,
        DateOnly MoveInDate, DateOnly? MoveOutDate, string? MoveInReason,
        string? MoveOutReason, string? SourceReference);

    private sealed record UnlinkedStayProjection(
        Guid RecordId, Guid HousingId, string HousingCode, string HousingNameAr, string HousingNameEn,
        Guid RoomId, string RoomName, Guid? FloorId, string? FloorName,
        string NameAr, string? IqamaNo, int? SourceRow);
}

internal static class HousingStayReportDates
{
    public static (int TotalDays, int SelectedDays) CountDays(
        DateOnly moveIn, DateOnly? moveOut, DateOnly? fromDate, DateOnly? toDate, DateOnly today)
    {
        var lastDay = moveOut is { } exit && exit < today ? exit : today;
        var totalDays = lastDay < moveIn ? 0 : lastDay.DayNumber - moveIn.DayNumber + 1;
        var selectedStart = fromDate is { } from && from > moveIn ? from : moveIn;
        var selectedEnd = toDate is { } to && to < lastDay ? to : lastDay;
        var selectedDays = selectedEnd < selectedStart ? 0 : selectedEnd.DayNumber - selectedStart.DayNumber + 1;
        return (totalDays, selectedDays);
    }
}
