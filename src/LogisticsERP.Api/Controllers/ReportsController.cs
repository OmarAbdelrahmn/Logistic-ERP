using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IReportingService service) : ControllerBase
{
    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var result = await service.GetSystemDashboardAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("hr/dashboard")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    public async Task<IActionResult> HrDashboard(CancellationToken cancellationToken)
    {
        var result = await service.GetHrDashboardAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("people-compliance/dashboard")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    public async Task<IActionResult> PeopleComplianceDashboard(CancellationToken cancellationToken)
    {
        var result = await service.GetPeopleComplianceDashboardAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("fleet/dashboard")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    public async Task<IActionResult> FleetDashboard(CancellationToken cancellationToken)
    {
        var result = await service.GetFleetDashboardAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("operations/dashboard")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    public async Task<IActionResult> OperationsDashboard(CancellationToken cancellationToken)
    {
        var result = await service.GetOperationsDashboardAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("maintenance-inventory/dashboard")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    public async Task<IActionResult> MaintenanceInventoryDashboard(CancellationToken cancellationToken)
    {
        var result = await service.GetMaintenanceInventoryDashboardAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
