using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-platform-account-assignments")]
public sealed class VehiclePlatformAccountAssignmentsController(
    IVehiclePlatformAccountAssignmentService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? platformRiderAccountId,
        [FromQuery] Guid? platformId,
        [FromQuery] Guid? operatingCityId,
        [FromQuery] Guid? sponsorId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetAssignmentsAsync(
            vehicleId,
            platformRiderAccountId,
            platformId,
            operatingCityId,
            sponsorId,
            activeOnly,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("problems")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    public async Task<IActionResult> GetProblems(
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? platformRiderAccountId,
        [FromQuery] Guid? platformId,
        [FromQuery] Guid? operatingCityId,
        [FromQuery] Guid? sponsorId,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetProblemsAsync(
            vehicleId,
            platformRiderAccountId,
            platformId,
            operatingCityId,
            sponsorId,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAssignmentAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsManage)]
    public async Task<IActionResult> Approve(
        [FromBody] ApproveVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ApproveAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/close")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsManage)]
    public async Task<IActionResult> Close(
        Guid id,
        [FromBody] CloseVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CloseAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("switches")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    public async Task<IActionResult> GetSwitches(
        [FromQuery] bool pendingOnly = true,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetSwitchesAsync(pendingOnly, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("switches/{switchId:guid}")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    public async Task<IActionResult> GetSwitch(Guid switchId, CancellationToken cancellationToken)
    {
        var result = await service.GetSwitchAsync(switchId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/switch")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsManage)]
    public async Task<IActionResult> Switch(
        Guid id,
        [FromBody] SwitchVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SwitchAsync(id, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetSwitch), new { switchId = result.Value!.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPost("switches/{switchId:guid}/accept")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsManage)]
    public async Task<IActionResult> AcceptSwitch(
        Guid switchId,
        [FromBody] AcceptVehiclePlatformAccountSwitchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AcceptSwitchAsync(switchId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
