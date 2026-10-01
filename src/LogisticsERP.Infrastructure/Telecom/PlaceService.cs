using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Telecom;
using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Telecom;

internal sealed class PlaceService(ApplicationDbContext dbContext) : IPlaceService
{
    private static readonly OperationError InvalidName = new(
        "place.invalid_name", "اسم المكان مطلوب ويجب ألا يتجاوز 200 حرف.", ErrorType.Validation, "name");
    private static readonly OperationError NotFound = new(
        "place.not_found", "المكان غير موجود.", ErrorType.NotFound, "id");
    private static readonly OperationError DuplicateName = new(
        "place.duplicate_name", "اسم المكان مستخدم بالفعل.", ErrorType.Conflict, "name");

    public async Task<Result<IReadOnlyList<PlaceResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var places = await dbContext.Places.AsNoTracking()
            .OrderBy(place => place.Name)
            .Select(place => new PlaceResponse(place.Id, place.Name))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlaceResponse>>(places);
    }

    public async Task<Result<PlaceResponse>> CreateAsync(SavePlaceRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return Result.Failure<PlaceResponse>(InvalidName);
        }
        if (await dbContext.Places.AnyAsync(place => place.Name == name, cancellationToken))
        {
            return Result.Failure<PlaceResponse>(DuplicateName);
        }

        var place = new Place { Name = name };
        dbContext.Places.Add(place);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<PlaceResponse>(DuplicateName);
        }
        return Result.Success(new PlaceResponse(place.Id, place.Name));
    }

    public async Task<Result<PlaceResponse>> UpdateAsync(Guid id, SavePlaceRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return Result.Failure<PlaceResponse>(InvalidName);
        }

        var place = await dbContext.Places.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (place is null)
        {
            return Result.Failure<PlaceResponse>(NotFound);
        }
        if (await dbContext.Places.AnyAsync(item => item.Id != id && item.Name == name, cancellationToken))
        {
            return Result.Failure<PlaceResponse>(DuplicateName);
        }

        place.Name = name;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<PlaceResponse>(DuplicateName);
        }
        return Result.Success(new PlaceResponse(place.Id, place.Name));
    }

    private static string? NormalizeName(string? name) =>
        string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200 ? null : name.Trim();
}
