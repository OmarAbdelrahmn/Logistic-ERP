using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class HousingWarehouseService(ApplicationDbContext dbContext) : IHousingWarehouseService
{
    public async Task<Result<HousingWarehouseResponse>> GetWarehouseAsync(
        Guid housingId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from warehouse in dbContext.HousingWarehouses.AsNoTracking()
            join housing in dbContext.Housing.AsNoTracking() on warehouse.HousingId equals housing.Id
            where warehouse.HousingId == housingId
            select new
            {
                Warehouse = warehouse,
                Housing = housing,
                ItemCount = dbContext.HousingWarehouseItems.Count(item => item.WarehouseId == warehouse.Id),
                TotalQuantity = dbContext.HousingWarehouseItemBalances
                    .Where(balance => dbContext.HousingWarehouseItems.Any(item =>
                        item.Id == balance.ItemId && item.WarehouseId == warehouse.Id))
                    .Sum(balance => (decimal?)balance.Quantity) ?? 0m
            }).SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return await HousingExistsAsync(housingId, cancellationToken)
                ? Result.Failure<HousingWarehouseResponse>(HrErrors.WarehouseNotFound)
                : Result.Failure<HousingWarehouseResponse>(HrErrors.HousingNotFound);
        }

        return Result.Success(new HousingWarehouseResponse(
            row.Warehouse.Id,
            row.Housing.Id,
            row.Housing.Code,
            row.Housing.NameAr,
            row.Housing.NameEn,
            row.Warehouse.NameAr,
            row.Warehouse.NameEn,
            row.Warehouse.IsDefault,
            row.ItemCount,
            row.TotalQuantity,
            HrServiceSupport.EncodeRowVersion(row.Warehouse.RowVersion)));
    }

    public async Task<Result<IReadOnlyList<HousingWarehouseItemResponse>>> GetItemsAsync(
        Guid housingId,
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var warehouseResult = await GetWarehouseEntityAsync(housingId, cancellationToken);
        if (!warehouseResult.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<HousingWarehouseItemResponse>>(warehouseResult.Error!);
        }

        HousingWarehouseItemStatus? parsedStatus = null;
        if (HrServiceSupport.HasText(status))
        {
            if (!TryParseStatus(status, out var value))
            {
                return Result.Failure<IReadOnlyList<HousingWarehouseItemResponse>>(
                    HrErrors.Invalid("status", "Use Unused, Used, or Damaged."));
            }

            parsedStatus = value;
        }

        var query = dbContext.HousingWarehouseItems.AsNoTracking()
            .Where(item => item.WarehouseId == warehouseResult.Value!.Id);

        if (HrServiceSupport.HasText(search))
        {
            var term = search!.Trim();
            query = query.Where(item => item.NameAr.Contains(term));
        }

        if (parsedStatus.HasValue)
        {
            query = query.Where(item => dbContext.HousingWarehouseItemBalances.Any(balance =>
                balance.ItemId == item.Id
                && balance.Status == parsedStatus.Value
                && balance.Quantity > 0));
        }

        var items = await query.OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        var balances = await GetBalancesAsync(items.Select(item => item.Id), cancellationToken);

        return Result.Success<IReadOnlyList<HousingWarehouseItemResponse>>(
            items.Select(item => ToResponse(item, housingId, balances.GetValueOrDefault(item.Id, []))).ToArray());
    }

    public async Task<Result<HousingWarehouseItemResponse>> GetItemAsync(
        Guid housingId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var itemResult = await GetItemEntityAsync(housingId, itemId, tracking: false, cancellationToken);
        if (!itemResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItemResponse>(itemResult.Error!);
        }

        var balances = await dbContext.HousingWarehouseItemBalances.AsNoTracking()
            .Where(balance => balance.ItemId == itemId)
            .ToArrayAsync(cancellationToken);
        return Result.Success(ToResponse(itemResult.Value!, housingId, balances));
    }

    public async Task<Result<HousingWarehouseItemResponse>> CreateItemAsync(
        Guid housingId,
        CreateHousingWarehouseItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateNameAndNotes(request.NameAr, request.Notes);
        if (validation is not null)
        {
            return Result.Failure<HousingWarehouseItemResponse>(validation);
        }

        if (request.Quantity < 0)
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("quantity", "It must be zero or greater."));
        }

        if (!TryParseStatus(request.Status, out var status))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("status", "Use Unused, Used, or Damaged."));
        }

        var warehouseResult = await GetWarehouseEntityAsync(housingId, cancellationToken);
        if (!warehouseResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItemResponse>(warehouseResult.Error!);
        }

        var name = request.NameAr.Trim();
        if (await dbContext.HousingWarehouseItems.AnyAsync(
                item => item.WarehouseId == warehouseResult.Value!.Id && item.NameAr == name,
                cancellationToken))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.WarehouseItemNameDuplicate);
        }

        var item = new HousingWarehouseItem
        {
            WarehouseId = warehouseResult.Value!.Id,
            NameAr = name,
            Notes = HrServiceSupport.TrimOrNull(request.Notes)
        };
        var balance = new HousingWarehouseItemBalance
        {
            ItemId = item.Id,
            Status = status,
            Quantity = request.Quantity
        };
        dbContext.HousingWarehouseItems.Add(item);
        dbContext.HousingWarehouseItemBalances.Add(balance);

        var saveResult = await SaveItemChangesAsync(cancellationToken);
        if (saveResult is not null)
        {
            return Result.Failure<HousingWarehouseItemResponse>(saveResult);
        }

        return Result.Success(ToResponse(item, housingId, [balance]));
    }

    public async Task<Result<HousingWarehouseItemResponse>> UpdateItemAsync(
        Guid housingId,
        Guid itemId,
        UpdateHousingWarehouseItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateNameAndNotes(request.NameAr, request.Notes);
        if (validation is not null)
        {
            return Result.Failure<HousingWarehouseItemResponse>(validation);
        }

        var itemResult = await GetItemEntityAsync(housingId, itemId, tracking: true, cancellationToken);
        if (!itemResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItemResponse>(itemResult.Error!);
        }

        var item = itemResult.Value!;
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.ConcurrencyConflict);
        }

        var name = request.NameAr.Trim();
        if (await dbContext.HousingWarehouseItems.AnyAsync(
                candidate => candidate.WarehouseId == item.WarehouseId
                    && candidate.Id != item.Id
                    && candidate.NameAr == name,
                cancellationToken))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.WarehouseItemNameDuplicate);
        }

        item.NameAr = name;
        item.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        var saveResult = await SaveItemChangesAsync(cancellationToken);
        if (saveResult is not null)
        {
            return Result.Failure<HousingWarehouseItemResponse>(saveResult);
        }

        var balances = await dbContext.HousingWarehouseItemBalances.AsNoTracking()
            .Where(balance => balance.ItemId == item.Id)
            .ToArrayAsync(cancellationToken);
        return Result.Success(ToResponse(item, housingId, balances));
    }

    public async Task<Result<HousingWarehouseItemResponse>> SetStatusQuantityAsync(
        Guid housingId,
        Guid itemId,
        string status,
        SetHousingWarehouseItemStatusQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(status, out var parsedStatus))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("status", "Use Unused, Used, or Damaged."));
        }

        if (request.Quantity < 0)
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("quantity", "It must be zero or greater."));
        }

        var itemResult = await GetItemEntityAsync(housingId, itemId, tracking: true, cancellationToken);
        if (!itemResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItemResponse>(itemResult.Error!);
        }

        var item = itemResult.Value!;
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.ConcurrencyConflict);
        }

        var balance = await dbContext.HousingWarehouseItemBalances.SingleOrDefaultAsync(
            candidate => candidate.ItemId == item.Id && candidate.Status == parsedStatus,
            cancellationToken);
        if (balance is null)
        {
            balance = new HousingWarehouseItemBalance
            {
                ItemId = item.Id,
                Status = parsedStatus
            };
            dbContext.HousingWarehouseItemBalances.Add(balance);
        }

        balance.Quantity = request.Quantity;
        Touch(item);
        var saveResult = await SaveItemChangesAsync(cancellationToken);
        if (saveResult is not null)
        {
            return Result.Failure<HousingWarehouseItemResponse>(saveResult);
        }

        return await GetItemAsync(housingId, itemId, cancellationToken);
    }

    public async Task<Result<HousingWarehouseItemResponse>> TransferStatusAsync(
        Guid housingId,
        Guid itemId,
        TransferHousingWarehouseItemStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(request.FromStatus, out var fromStatus))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("fromStatus", "Use Unused, Used, or Damaged."));
        }

        if (!TryParseStatus(request.ToStatus, out var toStatus))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("toStatus", "Use Unused, Used, or Damaged."));
        }

        if (fromStatus == toStatus)
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("toStatus", "It must differ from fromStatus."));
        }

        if (request.Quantity <= 0)
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.Invalid("quantity", "It must be greater than zero."));
        }

        var itemResult = await GetItemEntityAsync(housingId, itemId, tracking: true, cancellationToken);
        if (!itemResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItemResponse>(itemResult.Error!);
        }

        var item = itemResult.Value!;
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.ConcurrencyConflict);
        }

        var balances = await dbContext.HousingWarehouseItemBalances
            .Where(balance => balance.ItemId == item.Id && (balance.Status == fromStatus || balance.Status == toStatus))
            .ToArrayAsync(cancellationToken);
        var source = balances.SingleOrDefault(balance => balance.Status == fromStatus);
        if (source is null || source.Quantity < request.Quantity)
        {
            return Result.Failure<HousingWarehouseItemResponse>(HrErrors.WarehouseItemInsufficientQuantity);
        }

        var destination = balances.SingleOrDefault(balance => balance.Status == toStatus);
        if (destination is null)
        {
            destination = new HousingWarehouseItemBalance
            {
                ItemId = item.Id,
                Status = toStatus
            };
            dbContext.HousingWarehouseItemBalances.Add(destination);
        }

        source.Quantity -= request.Quantity;
        destination.Quantity += request.Quantity;
        Touch(item);
        var saveResult = await SaveItemChangesAsync(cancellationToken);
        if (saveResult is not null)
        {
            return Result.Failure<HousingWarehouseItemResponse>(saveResult);
        }

        return await GetItemAsync(housingId, itemId, cancellationToken);
    }

    public async Task<Result<HousingWarehouseItemHousingTransferResponse>> TransferUnusedToHousingAsync(
        Guid housingId,
        Guid itemId,
        TransferHousingWarehouseItemToHousingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.DestinationHousingId == housingId)
        {
            return Result.Failure<HousingWarehouseItemHousingTransferResponse>(
                HrErrors.Invalid("destinationHousingId", "It must differ from the source housing."));
        }

        if (request.Quantity <= 0)
        {
            return Result.Failure<HousingWarehouseItemHousingTransferResponse>(
                HrErrors.Invalid("quantity", "It must be greater than zero."));
        }

        var sourceItemResult = await GetItemEntityAsync(housingId, itemId, tracking: true, cancellationToken);
        if (!sourceItemResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItemHousingTransferResponse>(sourceItemResult.Error);
        }

        var sourceItem = sourceItemResult.Value!;
        if (!HrServiceSupport.MatchesRowVersion(sourceItem.RowVersion, request.RowVersion))
        {
            return Result.Failure<HousingWarehouseItemHousingTransferResponse>(HrErrors.ConcurrencyConflict);
        }

        var destinationWarehouseResult = await GetWarehouseEntityAsync(request.DestinationHousingId, cancellationToken);
        if (!destinationWarehouseResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItemHousingTransferResponse>(destinationWarehouseResult.Error);
        }

        var sourceBalance = await dbContext.HousingWarehouseItemBalances.SingleOrDefaultAsync(
            balance => balance.ItemId == sourceItem.Id && balance.Status == HousingWarehouseItemStatus.Unused,
            cancellationToken);
        if (sourceBalance is null || sourceBalance.Quantity < request.Quantity)
        {
            return Result.Failure<HousingWarehouseItemHousingTransferResponse>(HrErrors.WarehouseItemInsufficientQuantity);
        }

        var destinationWarehouse = destinationWarehouseResult.Value!;
        var destinationItem = await dbContext.HousingWarehouseItems.SingleOrDefaultAsync(
            candidate => candidate.WarehouseId == destinationWarehouse.Id && candidate.NameAr == sourceItem.NameAr,
            cancellationToken);
        if (destinationItem is null)
        {
            destinationItem = new HousingWarehouseItem
            {
                WarehouseId = destinationWarehouse.Id,
                NameAr = sourceItem.NameAr,
                Notes = sourceItem.Notes
            };
            dbContext.HousingWarehouseItems.Add(destinationItem);
        }

        var destinationBalance = await dbContext.HousingWarehouseItemBalances.SingleOrDefaultAsync(
            balance => balance.ItemId == destinationItem.Id && balance.Status == HousingWarehouseItemStatus.Unused,
            cancellationToken);
        if (destinationBalance is null)
        {
            destinationBalance = new HousingWarehouseItemBalance
            {
                ItemId = destinationItem.Id,
                Status = HousingWarehouseItemStatus.Unused
            };
            dbContext.HousingWarehouseItemBalances.Add(destinationBalance);
        }

        sourceBalance.Quantity -= request.Quantity;
        destinationBalance.Quantity += request.Quantity;
        Touch(sourceItem);
        if (dbContext.Entry(destinationItem).State != EntityState.Added)
        {
            Touch(destinationItem);
        }

        var saveResult = await SaveItemChangesAsync(cancellationToken);
        if (saveResult is not null)
        {
            return Result.Failure<HousingWarehouseItemHousingTransferResponse>(saveResult);
        }

        var balances = await GetBalancesAsync([sourceItem.Id, destinationItem.Id], cancellationToken);
        return Result.Success(new HousingWarehouseItemHousingTransferResponse(
            housingId,
            request.DestinationHousingId,
            request.Quantity,
            ToResponse(sourceItem, housingId, balances.GetValueOrDefault(sourceItem.Id, [])),
            ToResponse(destinationItem, request.DestinationHousingId, balances.GetValueOrDefault(destinationItem.Id, []))));
    }

    public async Task<Result> ArchiveItemAsync(
        Guid housingId,
        Guid itemId,
        ArchiveHousingWarehouseItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure(HrErrors.Required("reason"));
        }

        var itemResult = await GetItemEntityAsync(housingId, itemId, tracking: true, cancellationToken);
        if (!itemResult.IsSuccess)
        {
            return Result.Failure(itemResult.Error!);
        }

        var item = itemResult.Value!;
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
        {
            return Result.Failure(HrErrors.ConcurrencyConflict);
        }

        item.IsDeleted = true;
        item.DeletionReason = request.Reason.Trim();
        var saveResult = await SaveItemChangesAsync(cancellationToken);
        return saveResult is null ? Result.Success() : Result.Failure(saveResult);
    }

    private async Task<Result<HousingWarehouse>> GetWarehouseEntityAsync(
        Guid housingId,
        CancellationToken cancellationToken)
    {
        var warehouse = await dbContext.HousingWarehouses.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.HousingId == housingId, cancellationToken);
        if (warehouse is not null)
        {
            return Result.Success(warehouse);
        }

        return await HousingExistsAsync(housingId, cancellationToken)
            ? Result.Failure<HousingWarehouse>(HrErrors.WarehouseNotFound)
            : Result.Failure<HousingWarehouse>(HrErrors.HousingNotFound);
    }

    private async Task<Result<HousingWarehouseItem>> GetItemEntityAsync(
        Guid housingId,
        Guid itemId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var warehouseResult = await GetWarehouseEntityAsync(housingId, cancellationToken);
        if (!warehouseResult.IsSuccess)
        {
            return Result.Failure<HousingWarehouseItem>(warehouseResult.Error!);
        }

        var query = tracking ? dbContext.HousingWarehouseItems : dbContext.HousingWarehouseItems.AsNoTracking();
        var item = await query.SingleOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.WarehouseId == warehouseResult.Value!.Id,
            cancellationToken);
        return item is null
            ? Result.Failure<HousingWarehouseItem>(HrErrors.WarehouseItemNotFound)
            : Result.Success(item);
    }

    private async Task<Dictionary<Guid, IReadOnlyList<HousingWarehouseItemBalance>>> GetBalancesAsync(
        IEnumerable<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        var ids = itemIds.ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var balances = await dbContext.HousingWarehouseItemBalances.AsNoTracking()
            .Where(balance => ids.Contains(balance.ItemId))
            .ToArrayAsync(cancellationToken);
        return balances
            .GroupBy(balance => balance.ItemId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<HousingWarehouseItemBalance>)group.ToArray());
    }

    private Task<bool> HousingExistsAsync(Guid housingId, CancellationToken cancellationToken) =>
        dbContext.Housing.AsNoTracking().AnyAsync(housing => housing.Id == housingId, cancellationToken);

    private async Task<OperationError?> SaveItemChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return HrErrors.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
            && sqlException.Message.Contains("IX_HousingWarehouseItems_WarehouseId_NameAr", StringComparison.Ordinal))
        {
            return HrErrors.WarehouseItemNameDuplicate;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
            && sqlException.Message.Contains("IX_HousingWarehouseItemBalances_ItemId_Status", StringComparison.Ordinal))
        {
            return HrErrors.ConcurrencyConflict;
        }
    }

    private static OperationError? ValidateNameAndNotes(string? nameAr, string? notes)
    {
        if (!HrServiceSupport.HasText(nameAr)) return HrErrors.Required("nameAr");
        if (nameAr!.Trim().Length > 200) return HrErrors.Invalid("nameAr", "It cannot exceed 200 characters.");
        if (notes?.Length > 2000) return HrErrors.Invalid("notes", "It cannot exceed 2000 characters.");
        return null;
    }

    private static bool TryParseStatus(string? value, out HousingWarehouseItemStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private static void Touch(HousingWarehouseItem item) => item.UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static HousingWarehouseItemResponse ToResponse(
        HousingWarehouseItem item,
        Guid housingId,
        IReadOnlyList<HousingWarehouseItemBalance> balances)
    {
        var unused = balances.Where(balance => balance.Status == HousingWarehouseItemStatus.Unused).Sum(balance => balance.Quantity);
        var used = balances.Where(balance => balance.Status == HousingWarehouseItemStatus.Used).Sum(balance => balance.Quantity);
        var damaged = balances.Where(balance => balance.Status == HousingWarehouseItemStatus.Damaged).Sum(balance => balance.Quantity);
        return new HousingWarehouseItemResponse(
            item.Id,
            item.WarehouseId,
            housingId,
            item.NameAr,
            unused + used + damaged,
            unused,
            used,
            damaged,
            item.Notes,
            HrServiceSupport.EncodeRowVersion(item.RowVersion));
    }
}
