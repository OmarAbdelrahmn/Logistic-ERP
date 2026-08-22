using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/sponsors")]
public sealed class SponsorsController(IWorkforceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workforce.SponsorsRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetSponsorsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Workforce.SponsorsRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetSponsorAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Workforce.SponsorsManage)]
    public Task<IActionResult> Create([FromBody] SponsorUpsertRequest request, CancellationToken cancellationToken) => Upsert(null, request, cancellationToken);

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Workforce.SponsorsManage)]
    public Task<IActionResult> Update(Guid id, [FromBody] SponsorUpsertRequest request, CancellationToken cancellationToken) => Upsert(id, request, cancellationToken);

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Workforce.SponsorsManage)]
    public async Task<IActionResult> Archive(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveSponsorAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> Upsert(Guid? id, SponsorUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertSponsorAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

