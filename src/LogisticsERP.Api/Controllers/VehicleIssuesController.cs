using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-issues")]
public sealed class VehicleIssuesController(IFleetService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? vehicleId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await service.GetIssuesAsync(vehicleId, status, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleIssueRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.CreateIssueAsync(request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/{operation:regex(^(review|close|reject)$)}")]
    public async Task<IActionResult> Act(Guid id, string operation, [FromBody] VehicleIssueActionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ActOnIssueAsync(id, operation, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveVehicleIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ResolveIssueAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
