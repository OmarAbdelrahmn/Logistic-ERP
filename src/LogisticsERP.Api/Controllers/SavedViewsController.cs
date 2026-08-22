using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Features.System;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/saved-views")]
public sealed class SavedViewsController(ISavedViewService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] string? moduleKey, CancellationToken cancellationToken)
    {
        var result = await service.GetMineAsync(moduleKey, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavedViewUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavedViewUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string rowVersion, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, rowVersion, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}

