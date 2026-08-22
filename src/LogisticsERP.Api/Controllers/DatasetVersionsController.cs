using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.System;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/dataset-versions")]
public sealed class DatasetVersionsController(IDatasetVersionService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Reporting.ReportsRead)]
    public async Task<IActionResult> Get([FromQuery] string? moduleKey, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(moduleKey, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

