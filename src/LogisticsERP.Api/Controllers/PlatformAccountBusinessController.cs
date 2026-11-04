using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/platform-accounts")]
public sealed class PlatformAccountBusinessController(IPlatformAccountBusinessService service) : ControllerBase
{
    [HttpPost("{accountId:guid}/credentials/reveal")]
    [RequirePermission(PermissionKeys.PlatformFields.Reveal)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Reveal(Guid accountId, PlatformCredentialRevealRequest request, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await service.RevealCredentialAsync(accountId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("{accountId:guid}/business-context")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public async Task<IActionResult> Context(Guid accountId, DateOnly from, DateOnly through, CancellationToken ct)
    {
        var result = await service.ContextAsync(accountId, from, through, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPut("{accountId:guid}/business")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsUpdate)]
    public async Task<IActionResult> Update(Guid accountId, PlatformBusinessUpdate request, CancellationToken ct)
    {
        var result = await service.UpdateBusinessAsync(accountId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : NoContent();
    }
    [HttpPut("sim-cards/{simId:guid}/ownership")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsUpdate)]
    public async Task<IActionResult> SimOwnership(Guid simId, PlatformSimOwnershipUpdate request, CancellationToken ct)
    {
        var result = await service.UpdateSimOwnershipAsync(simId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : NoContent();
    }
}
