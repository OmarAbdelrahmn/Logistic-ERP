using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/riders/{riderProfileId:guid}")]
public sealed class RiderCardsController(IComplianceService service) : ControllerBase
{
    [HttpGet("cards")]
    [RequirePermission(PermissionKeys.Compliance.RiderCardsRead)]
    public async Task<IActionResult> GetCards(Guid riderProfileId, CancellationToken cancellationToken)
    {
        var result = await service.GetRiderCardsAsync(riderProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("cards")]
    [RequirePermission(PermissionKeys.Compliance.RiderCardsManage)]
    public Task<IActionResult> CreateCard(Guid riderProfileId, [FromBody] RiderCardUpsertRequest request, CancellationToken cancellationToken) => UpsertCard(riderProfileId, null, request, cancellationToken);

    [HttpPut("cards/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.RiderCardsManage)]
    public Task<IActionResult> UpdateCard(Guid riderProfileId, Guid id, [FromBody] RiderCardUpsertRequest request, CancellationToken cancellationToken) => UpsertCard(riderProfileId, id, request, cancellationToken);

    [HttpGet("health-cards")]
    [RequirePermission(PermissionKeys.Compliance.HealthCardsRead)]
    public async Task<IActionResult> GetHealthCards(Guid riderProfileId, CancellationToken cancellationToken)
    {
        var result = await service.GetHealthCardsAsync(riderProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("health-cards")]
    [RequirePermission(PermissionKeys.Compliance.HealthCardsManage)]
    public Task<IActionResult> CreateHealthCard(Guid riderProfileId, [FromBody] HealthCardUpsertRequest request, CancellationToken cancellationToken) => UpsertHealthCard(riderProfileId, null, request, cancellationToken);

    [HttpPut("health-cards/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.HealthCardsManage)]
    public Task<IActionResult> UpdateHealthCard(Guid riderProfileId, Guid id, [FromBody] HealthCardUpsertRequest request, CancellationToken cancellationToken) => UpsertHealthCard(riderProfileId, id, request, cancellationToken);

    [HttpPatch("cards/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.RiderCardsManage)]
    public Task<IActionResult> ArchiveCard(Guid riderProfileId, Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken) => Archive("rider-card", id, request, cancellationToken);

    [HttpPatch("health-cards/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.HealthCardsManage)]
    public Task<IActionResult> ArchiveHealthCard(Guid riderProfileId, Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken) => Archive("health-card", id, request, cancellationToken);

    private async Task<IActionResult> UpsertCard(Guid riderProfileId, Guid? id, RiderCardUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertRiderCardAsync(riderProfileId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> UpsertHealthCard(Guid riderProfileId, Guid? id, HealthCardUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertHealthCardAsync(riderProfileId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> Archive(string resource, Guid id, ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(resource, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
