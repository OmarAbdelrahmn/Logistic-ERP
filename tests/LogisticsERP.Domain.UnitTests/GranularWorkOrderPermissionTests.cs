using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class GranularWorkOrderPermissionTests
{
    [Theory]
    [InlineData(nameof(MaintenanceWorkOrdersController.Get), PermissionKeys.Maintenance.WorkOrdersRead)]
    [InlineData(nameof(MaintenanceWorkOrdersController.GetOne), PermissionKeys.Maintenance.WorkOrdersRead)]
    [InlineData(nameof(MaintenanceWorkOrdersController.GetExternal), PermissionKeys.Maintenance.WorkOrdersRead)]
    [InlineData(nameof(MaintenanceWorkOrdersController.CreateCompany), PermissionKeys.Maintenance.WorkOrdersCreate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.CreateExternal), PermissionKeys.Maintenance.WorkOrdersCreate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.Start), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.Complete), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.Close), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.Cancel), PermissionKeys.Maintenance.WorkOrdersDelete)]
    [InlineData(nameof(MaintenanceWorkOrdersController.PostMaterial), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.ReverseMaterial), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.CompleteOilChange), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.PostPartSale), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.PostCustomerLaborCharge), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.PostMechanicLaborPayment), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.PostOtherFinancialEntry), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [InlineData(nameof(MaintenanceWorkOrdersController.PostCustomerPayment), PermissionKeys.Maintenance.WorkOrdersUpdate)]
    public void WorkOrderActionsRequireTheirOwnPermission(string action, string expectedKey)
    {
        var attributes = typeof(MaintenanceWorkOrdersController).GetMethod(action)!
            .GetCustomAttributes<RequirePermissionAttribute>().ToArray();
        var workOrderKeys = attributes.Select(x => x.Policy![AuthenticationPolicies.PermissionPrefix.Length..])
            .Where(x => x.StartsWith("maintenance.work_orders.", StringComparison.Ordinal));
        Assert.Equal([expectedKey], workOrderKeys);
    }

    [Theory]
    [InlineData(nameof(ImportController.ValidateExternalRiders))]
    [InlineData(nameof(ImportController.ImportExternalRiders))]
    public void MixedExternalRiderImportsRequireBothCreateAndUpdate(string action)
    {
        var keys = typeof(ImportController).GetMethod(action)!
            .GetCustomAttributes<RequirePermissionAttribute>()
            .Select(x => x.Policy![AuthenticationPolicies.PermissionPrefix.Length..]);
        Assert.Equal([PermissionKeys.Workforce.ExternalRidersCreate, PermissionKeys.Workforce.ExternalRidersUpdate], keys);
    }
}
