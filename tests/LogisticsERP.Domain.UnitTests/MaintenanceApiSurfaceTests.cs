using System.Reflection;
using System.Text.Json.Serialization;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
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
        AssertEndpointPermission(nameof(MaintenanceWorkOrdersController.GetExternal), PermissionKeys.Maintenance.ExternalJobsRead, typeof(MaintenanceWorkOrdersController));
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
        Assert.Contains(PermissionKeys.Inventory.SupplyRequestsSubmit, permissions);
        Assert.Contains(PermissionKeys.Inventory.SupplyRequestsRead, permissions);
        Assert.Contains(PermissionKeys.Inventory.SupplyRequestsApprove, permissions);
    }

    [Fact]
    public void SupplyRequestWorkflowSeparatesSubmissionFromWarehouseIssuance()
    {
        AssertEndpointPermission(nameof(MaintenanceInventoryController.CreateRiderSupplyRequest), PermissionKeys.Inventory.SupplyRequestsSubmit, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.GetOwnSupplyRequest), PermissionKeys.Inventory.SupplyRequestsSubmit, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.GetSupplyRequests), PermissionKeys.Inventory.SupplyRequestsRead, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.ApproveAndIssueSupplyRequest), PermissionKeys.Inventory.SupplyRequestsApprove, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.ApproveAndIssueSupplyRequest), PermissionKeys.Inventory.StockMove, typeof(MaintenanceInventoryController));
        AssertEndpointPermission(nameof(MaintenanceInventoryController.RejectSupplyRequest), PermissionKeys.Inventory.SupplyRequestsApprove, typeof(MaintenanceInventoryController));
        Assert.Equal("supply-requests/{id:guid}/approve-and-issue", Assert.Single(typeof(MaintenanceInventoryController)
            .GetMethod(nameof(MaintenanceInventoryController.ApproveAndIssueSupplyRequest))!.GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    [Fact]
    public void MaintenanceWorkOrderCanCarryOneDetailedSupplyRequest()
    {
        Assert.Equal(typeof(MaintenanceSupplyRequestInput), typeof(CreateMaintenanceWorkOrderRequest)
            .GetProperty(nameof(CreateMaintenanceWorkOrderRequest.SupplyRequest))!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<InventorySupplyRequestLineInput>), typeof(MaintenanceSupplyRequestInput)
            .GetProperty(nameof(MaintenanceSupplyRequestInput.Lines))!.PropertyType);
        Assert.NotNull(typeof(InventorySupplyRequest).GetProperty(nameof(InventorySupplyRequest.VehicleId)));
        Assert.NotNull(typeof(InventorySupplyRequest).GetProperty(nameof(InventorySupplyRequest.RiderProfileId)));
        Assert.Equal(InventorySupplyRequestStatus.PendingWarehouseApproval, new InventorySupplyRequest().Status);
    }

    [Fact]
    public void WorkOrderCreationDoesNotAcceptUserEnteredCostAndSupportsOneStepOilRequest()
    {
        Assert.Null(typeof(CreateMaintenanceWorkOrderRequest).GetProperty("EstimatedCost"));
        Assert.Equal(typeof(OilChangeSupplyRequestInput), typeof(CreateMaintenanceWorkOrderRequest)
            .GetProperty(nameof(CreateMaintenanceWorkOrderRequest.OilChange))!.PropertyType);
        Assert.NotNull(typeof(InventorySupplyDecisionRequest).GetProperty(nameof(InventorySupplyDecisionRequest.NextOilBarrelId)));
        var laborCost = typeof(MaintenanceWorkOrderResponse).GetProperty(nameof(MaintenanceWorkOrderResponse.ActualLaborCost))!;
        Assert.Equal(typeof(decimal?), laborCost.PropertyType);
        Assert.Equal(JsonIgnoreCondition.WhenWritingNull, laborCost.GetCustomAttribute<JsonIgnoreAttribute>()!.Condition);
    }

    [Fact]
    public void BatchSparePartUsageEndpointUsesTheRequestedRouteAndStockPermissions()
    {
        var method = typeof(SparePartController).GetMethod(nameof(SparePartController.PostUsages));

        Assert.NotNull(method);
        Assert.Equal("spare-parts", Assert.Single(method!.GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.Equal("api/[controller]", Assert.Single(typeof(SparePartController).GetCustomAttributes<RouteAttribute>()).Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(PermissionKeys.Maintenance.WorkOrdersManage, StringComparison.Ordinal) == true);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(PermissionKeys.Inventory.StockMove, StringComparison.Ordinal) == true);
        Assert.Equal(typeof(IReadOnlyList<BatchSparePartUsageLineRequest>), typeof(BatchSparePartUsageRequest).GetProperty(nameof(BatchSparePartUsageRequest.Usages))!.PropertyType);
    }

    private static void AssertEndpointPermission(string methodName, string permission, Type controllerType)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }
}
