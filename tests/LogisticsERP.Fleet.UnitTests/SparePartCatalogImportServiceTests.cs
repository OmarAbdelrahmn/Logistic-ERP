using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using LogisticsERP.Infrastructure.Maintenance;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class SparePartCatalogImportServiceTests
{
    [Fact]
    public async Task ValidationChecksWorkbookWithoutSavingEvenForAnonymousCaller()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        using var workbook = CreateWorkbook(("استوب خلفي بوكسر", "دباب"), ("مساعد امامى نيسان", "سيارة"));

        var result = await CreateService(dbContext).ImportAsync(workbook, "parts.xlsx", true, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.ValidateOnly);
        Assert.True(result.Value.CanImport);
        Assert.False(result.Value.Imported);
        Assert.Equal(2, result.Value.WouldCreateItems);
        Assert.Equal(0, result.Value.CreatedItems);
        Assert.All(result.Value.Rows, row => Assert.Equal("WouldCreate", row.Action));
        Assert.Empty(await dbContext.InventoryItems.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportsOnlyNamesAndExactVehicleCompatibility()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        using var workbook = CreateWorkbook(("استوب خلفي بوكسر", "دباب"), ("مساعد امامى نيسان", "سيارة"));

        var result = await CreateService(dbContext).ImportAsync(workbook, "parts.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Imported);
        Assert.Equal(2, result.Value.CreatedItems);
        var items = await dbContext.InventoryItems.ToArrayAsync(cancellationToken);
        Assert.Equal(2, items.Length);
        var motorcycle = Assert.Single(items, x => x.NameAr == "استوب خلفي بوكسر");
        Assert.Equal(InventoryItemType.SparePart, motorcycle.ItemType);
        Assert.Equal(InventoryItemVehicleCompatibility.For(VehicleType.Motorcycle), motorcycle.CompatibleVehicleTypesMask);
        Assert.Equal(motorcycle.NameAr, motorcycle.NameEn);
        Assert.StartsWith("SP-M-", motorcycle.Sku);
        Assert.Equal(InventoryUnitOfMeasure.Piece, motorcycle.BaseUnitOfMeasure);
        Assert.Null(motorcycle.Barcode);
        Assert.Equal(0m, motorcycle.MinimumStockLevel);
        var car = Assert.Single(items, x => x.NameAr == "مساعد امامى نيسان");
        Assert.Equal(InventoryItemVehicleCompatibility.For(VehicleType.Car), car.CompatibleVehicleTypesMask);
        Assert.StartsWith("SP-C-", car.Sku);
    }

    [Fact]
    public async Task RepeatedUploadSkipsExistingNamesForSameVehicleType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var service = CreateService(dbContext);
        using var firstWorkbook = CreateWorkbook(("فلتر زيت", "دباب"));
        using var secondWorkbook = CreateWorkbook(("فلتر زيت", "دباب"));

        var first = await service.ImportAsync(firstWorkbook, "parts.xlsx", false, cancellationToken);
        var second = await service.ImportAsync(secondWorkbook, "parts.xlsx", false, cancellationToken);

        Assert.True(first.IsSuccess, first.Error.Description);
        Assert.True(second.IsSuccess, second.Error.Description);
        Assert.Equal(0, second.Value!.CreatedItems);
        Assert.Equal(1, second.Value.AlreadyExistingItems);
        Assert.Single(await dbContext.InventoryItems.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task InvalidClassificationBlocksAllRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        using var workbook = CreateWorkbook(("فرامل سيارة", "سيارة"), ("قطعة شاحنة", "شاحنة"));

        var result = await CreateService(dbContext).ImportAsync(workbook, "parts.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanImport);
        Assert.False(result.Value.Imported);
        Assert.Equal(0, result.Value.CreatedItems);
        Assert.Equal("NotImported", Assert.Single(result.Value.Rows).Action);
        Assert.Contains(result.Value.Issues, issue => issue.Field == "vehicleType" && issue.RowNumber == 3);
        Assert.Empty(await dbContext.InventoryItems.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task DuplicateNameAndTypeWithinWorkbookBlocksAllRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        using var workbook = CreateWorkbook(("فلتر زيت", "دباب"), ("  فلتر   زيت ", "دباب"));

        var result = await CreateService(dbContext).ImportAsync(workbook, "parts.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanImport);
        Assert.Contains(result.Value.Issues, issue => issue.RowNumber == 3 && issue.Field == "nameAr");
        Assert.Empty(await dbContext.InventoryItems.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task ExistingNameWithOtherCompatibilityDoesNotBlockSpecificPart()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        dbContext.InventoryItems.Add(new InventoryItem
        {
            Sku = "EXISTING",
            NormalizedSku = "EXISTING",
            NameAr = "فلتر زيت",
            NameEn = "Oil filter",
            ItemType = InventoryItemType.SparePart,
            CompatibleVehicleTypesMask = InventoryItemVehicleCompatibility.AllVehicleTypesMask,
            BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece,
            PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Piece
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        using var workbook = CreateWorkbook(("فلتر زيت", "دباب"));

        var result = await CreateService(dbContext).ImportAsync(workbook, "parts.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(1, result.Value!.CreatedItems);
        Assert.Equal(2, await dbContext.InventoryItems.CountAsync(cancellationToken));
    }

    private static SparePartCatalogImportService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, NullLogger<SparePartCatalogImportService>.Instance);

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SparePartCatalogImport_{Guid.NewGuid():N}",
                options => options.EnableNullChecks(false))
            .Options,
        TimeProvider.System);

    private static MemoryStream CreateWorkbook(params (string? Name, string? Classification)[] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("قائمة الاصناف الشاملة");
        sheet.Cell(1, 1).Value = "اسم الصنف / القطعة";
        sheet.Cell(1, 2).Value = "التصنيف";
        for (var index = 0; index < rows.Length; index++)
        {
            sheet.Cell(index + 2, 1).Value = rows[index].Name ?? string.Empty;
            sheet.Cell(index + 2, 2).Value = rows[index].Classification ?? string.Empty;
        }
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

}
