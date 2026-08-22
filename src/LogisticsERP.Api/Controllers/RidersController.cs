using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/riders")]
public sealed class RidersController(IWorkforceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workforce.RidersRead)]
    public async Task<IActionResult> GetAll([FromQuery] bool? outsideOnly, CancellationToken cancellationToken)
    {
        var result = await service.GetRidersAsync(outsideOnly, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("outside")]
    [RequirePermission(PermissionKeys.Workforce.RidersRead)]
    public async Task<IActionResult> GetOutsideRiders(CancellationToken cancellationToken)
    {
        var result = await service.GetRidersAsync(true, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("employee/{employeeId:guid}")]
    [RequirePermission(PermissionKeys.Workforce.RidersManage)]
    public async Task<IActionResult> Create(Guid employeeId, [FromBody] CreateRiderProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateRiderProfileAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{riderProfileId:guid}")]
    [RequirePermission(PermissionKeys.Workforce.RidersManage)]
    public async Task<IActionResult> Update(Guid riderProfileId, [FromBody] UpdateRiderProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateRiderProfileAsync(riderProfileId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

