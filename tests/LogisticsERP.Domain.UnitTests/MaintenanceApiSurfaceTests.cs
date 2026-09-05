using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Maintenance;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class MaintenanceApiSurfaceTests
{
    [Fact]
    public void PurchaseReceiptRequiresMultipartBillFile()
    {
        var method = typeof(MaintenanceInventoryController).GetMethod(nameof(MaintenanceInventoryController.PostReceipt));

        Assert.NotNull(method);
        Assert.Equal("receipts", Assert.Single(method!.GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.Contains("multipart/form-data", Assert.Single(method.GetCustomAttributes<ConsumesAttribute>()).ContentTypes);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(PermissionKeys.Inventory.ReceiptsManage, StringComparison.Ordinal) == true);
        var formParameter = Assert.Single(method.GetParameters(), parameter => parameter.ParameterType == typeof(PurchaseReceiptForm));
        Assert.NotNull(formParameter.GetCustomAttribute<FromFormAttribute>());
        Assert.Equal(typeof(IFormFile), typeof(PurchaseReceiptForm).GetProperty(nameof(PurchaseReceiptForm.BillFile))!.PropertyType);
        Assert.Equal(typeof(string), typeof(PurchaseReceiptForm).GetProperty(nameof(PurchaseReceiptForm.ReceiptJson))!.PropertyType);
    }

    [Fact]
    public void PurchaseReceiptPersistsOneProtectedAttachmentRecord()
    {
        Assert.Equal(typeof(Guid), typeof(PurchaseReceiptAttachment).GetProperty(nameof(PurchaseReceiptAttachment.PurchaseReceiptId))!.PropertyType);
        Assert.NotNull(typeof(PurchaseReceiptAttachment).GetProperty(nameof(PurchaseReceiptAttachment.StoragePath)));
        Assert.NotNull(typeof(PurchaseReceiptAttachment).GetProperty(nameof(PurchaseReceiptAttachment.Sha256Checksum)));
        Assert.NotNull(typeof(PurchaseReceiptAttachment).GetProperty(nameof(PurchaseReceiptAttachment.FileSizeBytes)));
    }

    [Fact]
    public void OilBarrelEndpointsExposeOpeningWarningAndControlledLoss()
    {
        AssertEndpointPermission(nameof(MaintenanceInventoryController.GetOilBarrels), PermissionKeys.Inventory.StockRead, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.GetOilBarrels), PermissionKeys.Inventory.CostLayersRead, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.OpenOilBarrel), PermissionKeys.Inventory.StockMove, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.RecordOilBarrelLoss), PermissionKeys.Inventory.StockAdjust, typeof(MaintenanceInventoryController));
        Assert.NotNull(typeof(OilBarrel).GetProperty(nameof(OilBarrel.RemainingLiters)));
        Assert.NotNull(typeof(OilBarrel).GetProperty(nameof(OilBarrel.UnitCostPerLiter)));
        Assert.NotNull(typeof(OilBarrel).GetProperty(nameof(OilBarrel.MaximumAllowedLossLiters)));
        Assert.NotNull(typeof(OilBarrel).GetProperty(nameof(OilBarrel.RecordedLossLiters)));
    }

    [Fact]
    public void ExternalWorkshopEndpointsHaveDedicatedFinancialPermissions()
    {
        AssertEndpointPermission(nameof(MaintenanceWorkOrdersController.PostPartSale), PermissionKeys.Maintenance.PartSalesManage, typeof(MaintenanceWorkOrdersController));
        AssertEndpointPermission(nameof(MaintenanceWorkOrdersController.PostCustomerLaborCharge), PermissionKeys.Maintenance.CustomerLaborChargesManage, typeof(MaintenanceWorkOrdersController));
        AssertEndpointPermission(nameof(MaintenanceWorkOrdersController.PostMechanicLaborPayment), PermissionKeys.Maintenance.MechanicLaborPaymentsManage, typeof(MaintenanceWorkOrdersController));
        AssertEndpointPermission(nameof(MaintenanceController.GetExternalProfit), PermissionKeys.Maintenance.ProfitReportsRead, typeof(MaintenanceController));
    }

    [Fact]
    public void AllMaintenanceAndInventoryPermissionsAreRegistered()
    {
        var permissions = (IEnumerable<string>)PermissionKeys.All;
        Assert.Contains(PermissionKeys.Maintenance.OilComplete, permissions);
        Assert.Contains(PermissionKeys.Maintenance.ProfitReportsRead, permissions);
        Assert.Contains(PermissionKeys.Inventory.CostLayersRead, permissions);
        Assert.Contains(PermissionKeys.Inventory.ReceiptsManage, permissions);
    }

    private static void AssertEndpointPermission(string methodName, string permission, Type controllerType)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }
}
