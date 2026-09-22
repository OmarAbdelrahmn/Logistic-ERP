using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class HousingWarehouseApiSurfaceTests
{
    public static TheoryData<string, Type, string, string> Endpoints => new()
    {
        { nameof(HousingWarehouseController.Get), typeof(HttpGetAttribute), "", PermissionKeys.Operations.HousingRead },
        { nameof(HousingWarehouseController.GetItems), typeof(HttpGetAttribute), "items", PermissionKeys.Operations.HousingRead },
        { nameof(HousingWarehouseController.GetItem), typeof(HttpGetAttribute), "items/{itemId:guid}", PermissionKeys.Operations.HousingRead },
        { nameof(HousingWarehouseController.CreateItem), typeof(HttpPostAttribute), "items", PermissionKeys.Operations.HousingManage },
        { nameof(HousingWarehouseController.UpdateItem), typeof(HttpPutAttribute), "items/{itemId:guid}", PermissionKeys.Operations.HousingManage },
        { nameof(HousingWarehouseController.SetStatusQuantity), typeof(HttpPatchAttribute), "items/{itemId:guid}/statuses/{status}/quantity", PermissionKeys.Operations.HousingManage },
        { nameof(HousingWarehouseController.TransferStatus), typeof(HttpPostAttribute), "items/{itemId:guid}/status-transfers", PermissionKeys.Operations.HousingManage },
        { nameof(HousingWarehouseController.TransferUnusedToHousing), typeof(HttpPostAttribute), "items/{itemId:guid}/housing-transfers", PermissionKeys.Operations.HousingManage },
        { nameof(HousingWarehouseController.ArchiveItem), typeof(HttpDeleteAttribute), "items/{itemId:guid}", PermissionKeys.Operations.HousingManage }
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(
        string methodName,
        Type verbType,
        string route,
        string permission)
    {
        var method = typeof(HousingWarehouseController).GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(route, verb.Template ?? string.Empty);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void WarehouseItemsAreIndependentFromMaintenanceInventory()
    {
        var itemType = typeof(HousingWarehouseItem);
        Assert.NotNull(itemType.GetProperty(nameof(HousingWarehouseItem.WarehouseId)));
        Assert.NotNull(itemType.GetProperty(nameof(HousingWarehouseItem.NameAr)));
        Assert.Null(itemType.GetProperty("Code"));
        Assert.Null(itemType.GetProperty("NameEn"));
        Assert.Null(itemType.GetProperty("Unit"));
        Assert.Null(itemType.GetProperty("InventoryItemId"));
        Assert.Null(itemType.GetProperty("SparePartId"));
        Assert.Null(itemType.GetProperty("ReceiptId"));
        Assert.NotNull(typeof(HousingWarehouseItemBalance).GetProperty(nameof(HousingWarehouseItemBalance.Status)));
    }

    [Theory]
    [InlineData("Unused")]
    [InlineData("Used")]
    [InlineData("Damaged")]
    public void ContractSupportsRequiredItemStatuses(string status)
    {
        Assert.True(Enum.TryParse<HousingWarehouseItemStatus>(status, out var parsed));
        Assert.True(Enum.IsDefined(parsed));
    }

    [Fact]
    public void CreateContractContainsOnlySimplifiedItemFields()
    {
        var properties = typeof(CreateHousingWarehouseItemRequest).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["NameAr", "Quantity", "Status", "Notes"], properties);
        Assert.DoesNotContain("Code", properties);
        Assert.DoesNotContain("NameEn", properties);
        Assert.DoesNotContain("Unit", properties);
    }
}
