using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.System;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/exports")]
[RequirePermission(PermissionKeys.Reporting.ExportsCreate)]
public sealed class ExportsController(IExportService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await service.GetMineAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExportRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? AcceptedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsync(id, cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true)
            : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] string rowVersion, CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(id, rowVersion, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}

