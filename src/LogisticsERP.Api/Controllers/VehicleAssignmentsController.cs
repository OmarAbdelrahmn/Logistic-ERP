using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-assignments")]
public sealed class VehicleAssignmentsController(IFleetService service) : ControllerBase
{
    [HttpPost("take")]
    public async Task<IActionResult> Take([FromBody] TakeVehicleRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.TakeAsync(request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] ReturnVehicleRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.ReturnAsync(request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("switch")]
    public async Task<IActionResult> Switch([FromBody] SwitchVehicleRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.SwitchAsync(request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{assignmentId:guid}/renew-permission")]
    public async Task<IActionResult> Renew(Guid assignmentId, [FromBody] RenewVehiclePermissionRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.RenewPermissionAsync(assignmentId, request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

[ApiController]
[Route("api/riders/{riderProfileId:guid}/vehicle-timeline")]
public sealed class RiderVehicleTimelineController(IFleetService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid riderProfileId, CancellationToken cancellationToken)
    {
        var result = await service.GetRiderTimelineAsync(riderProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
