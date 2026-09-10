using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-issues")]
public sealed class VehicleIssuesController(IFleetService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Fleet.IssuesRead)]
    public async Task<IActionResult> Get([FromQuery] Guid? vehicleId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await service.GetIssuesAsync(vehicleId, status, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/evidence")]
    [RequirePermission(PermissionKeys.Fleet.IssuesRead)]
    public async Task<IActionResult> GetEvidence(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetIssueEvidenceAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/evidence/{evidenceId:guid}/download")]
    [RequirePermission(PermissionKeys.Fleet.IssuesRead)]
    public async Task<IActionResult> DownloadEvidence(Guid id, Guid evidenceId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadIssueEvidenceAsync(id, evidenceId, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Fleet.IssuesManage)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleIssueRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.CreateIssueAsync(request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/{operation:regex(^(review|close|reject)$)}")]
    [RequirePermission(PermissionKeys.Fleet.IssuesManage)]
    public async Task<IActionResult> Act(Guid id, string operation, [FromBody] VehicleIssueActionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ActOnIssueAsync(id, operation, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.Fleet.IssuesManage)]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveVehicleIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ResolveIssueAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
