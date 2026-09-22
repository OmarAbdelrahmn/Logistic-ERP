using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingWarehouseModelTests
{
    [Fact]
    public void EachHousingHasOneActiveDefaultWarehouseAndUniqueItemNames()
    {
        using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HousingWarehouseModel;Trusted_Connection=True")
                .Options);

        var warehouse = dbContext.Model.FindEntityType(typeof(HousingWarehouse));
        Assert.NotNull(warehouse);
        var housingIndex = Assert.Single(warehouse!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(HousingWarehouse.HousingId)]));
        Assert.True(housingIndex.IsUnique);
        Assert.Equal("[IsDeleted] = 0", housingIndex.GetFilter());

        var item = dbContext.Model.FindEntityType(typeof(HousingWarehouseItem));
        Assert.NotNull(item);
        var nameIndex = Assert.Single(item!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(HousingWarehouseItem.WarehouseId),
                nameof(HousingWarehouseItem.NameAr)]));
        Assert.True(nameIndex.IsUnique);
        Assert.Equal("[IsDeleted] = 0", nameIndex.GetFilter());

        Assert.DoesNotContain(item.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType.Namespace?.Contains("Maintenance", StringComparison.Ordinal) == true);

        var balance = dbContext.Model.FindEntityType(typeof(HousingWarehouseItemBalance));
        Assert.NotNull(balance);
        var statusIndex = Assert.Single(balance!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(HousingWarehouseItemBalance.ItemId),
                nameof(HousingWarehouseItemBalance.Status)]));
        Assert.True(statusIndex.IsUnique);
        Assert.Equal("[IsDeleted] = 0", statusIndex.GetFilter());
    }

    [Fact]
    public async Task StatusTransferCreatesThenIncrementsDestinationBalanceWithoutDuplicatingItem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var housingId = Guid.CreateVersion7();
        var warehouseId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        dbContext.Housing.Add(new Housing
        {
            Id = housingId,
            Code = "HOU-TEST",
            NameAr = "سكن الاختبار",
            NameEn = "Test housing"
        });
        dbContext.HousingWarehouses.Add(new HousingWarehouse
        {
            Id = warehouseId,
            HousingId = housingId,
            RowVersion = [1]
        });
        dbContext.HousingWarehouseItems.Add(new HousingWarehouseItem
        {
            Id = itemId,
            WarehouseId = warehouseId,
            NameAr = "سرير",
            RowVersion = [2]
        });
        dbContext.HousingWarehouseItemBalances.Add(new HousingWarehouseItemBalance
        {
            ItemId = itemId,
            Status = HousingWarehouseItemStatus.Unused,
            Quantity = 10
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var service = new HousingWarehouseService(dbContext);
        var rowVersion = Convert.ToBase64String([2]);
        var first = await service.TransferStatusAsync(
            housingId,
            itemId,
            new TransferHousingWarehouseItemStatusRequest("Unused", "Used", 3, rowVersion),
            cancellationToken);
        var second = await service.TransferStatusAsync(
            housingId,
            itemId,
            new TransferHousingWarehouseItemStatusRequest("Unused", "Used", 2, rowVersion),
            cancellationToken);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(5, second.Value!.UnusedQuantity);
        Assert.Equal(5, second.Value.UsedQuantity);
        Assert.Equal(10, second.Value.TotalQuantity);
        Assert.Single(await dbContext.HousingWarehouseItems.ToArrayAsync(cancellationToken));
        Assert.Single(await dbContext.HousingWarehouseItemBalances
            .Where(balance => balance.ItemId == itemId && balance.Status == HousingWarehouseItemStatus.Used)
            .ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task HousingTransferCreatesThenReusesDestinationItemAndMovesOnlyUnusedQuantity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var sourceHousingId = Guid.CreateVersion7();
        var destinationHousingId = Guid.CreateVersion7();
        var sourceWarehouseId = Guid.CreateVersion7();
        var destinationWarehouseId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        dbContext.Housing.AddRange(
            new Housing { Id = sourceHousingId, Code = "HOU-SOURCE", NameAr = "سكن المصدر", NameEn = "Source" },
            new Housing { Id = destinationHousingId, Code = "HOU-DEST", NameAr = "سكن الوجهة", NameEn = "Destination" });
        dbContext.HousingWarehouses.AddRange(
            new HousingWarehouse { Id = sourceWarehouseId, HousingId = sourceHousingId, RowVersion = [1] },
            new HousingWarehouse { Id = destinationWarehouseId, HousingId = destinationHousingId, RowVersion = [2] });
        dbContext.HousingWarehouseItems.Add(new HousingWarehouseItem
        {
            Id = itemId,
            WarehouseId = sourceWarehouseId,
            NameAr = "ثلاجة",
            RowVersion = [3]
        });
        dbContext.HousingWarehouseItemBalances.AddRange(
            new HousingWarehouseItemBalance
            {
                ItemId = itemId,
                Status = HousingWarehouseItemStatus.Unused,
                Quantity = 10
            },
            new HousingWarehouseItemBalance
            {
                ItemId = itemId,
                Status = HousingWarehouseItemStatus.Used,
                Quantity = 2
            });
        await dbContext.SaveChangesAsync(cancellationToken);

        var service = new HousingWarehouseService(dbContext);
        var rowVersion = Convert.ToBase64String([3]);
        var first = await service.TransferUnusedToHousingAsync(
            sourceHousingId,
            itemId,
            new TransferHousingWarehouseItemToHousingRequest(destinationHousingId, 4, rowVersion),
            cancellationToken);
        var second = await service.TransferUnusedToHousingAsync(
            sourceHousingId,
            itemId,
            new TransferHousingWarehouseItemToHousingRequest(destinationHousingId, 1, rowVersion),
            cancellationToken);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(5, second.Value!.SourceItem.UnusedQuantity);
        Assert.Equal(2, second.Value.SourceItem.UsedQuantity);
        Assert.Equal(5, second.Value.DestinationItem.UnusedQuantity);
        Assert.Equal(0, second.Value.DestinationItem.UsedQuantity);
        Assert.Single(await dbContext.HousingWarehouseItems
            .Where(item => item.WarehouseId == destinationWarehouseId && item.NameAr == "ثلاجة")
            .ToArrayAsync(cancellationToken));
        Assert.Single(await dbContext.HousingWarehouseItemBalances
            .Where(balance => balance.ItemId == second.Value.DestinationItem.Id
                && balance.Status == HousingWarehouseItemStatus.Unused)
            .ToArrayAsync(cancellationToken));
    }
}
