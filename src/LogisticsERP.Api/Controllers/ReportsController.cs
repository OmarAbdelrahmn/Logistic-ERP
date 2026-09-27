using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Application.Features.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(
    IReportingService service,
    IVehicleRiderPeriodReportService vehicleRiderPeriodReportService) : ControllerBase
{
    [HttpGet("fleet/vehicle-assignments")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    [RequirePermission(PermissionKeys.Workforce.RidersRead)]
    public async Task<IActionResult> VehicleAssignments(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var result = await vehicleRiderPeriodReportService.GetByVehicleAsync(fromDate, toDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("fleet/rider-assignments")]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    [RequirePermission(PermissionKeys.Workforce.RidersRead)]
    public async Task<IActionResult> RiderAssignments(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var result = await vehicleRiderPeriodReportService.GetByRiderAsync(fromDate, toDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

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
