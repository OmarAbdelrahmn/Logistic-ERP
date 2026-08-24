using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/platforms")]
public sealed class PlatformsController(ISimplePlatformService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public Task<IActionResult> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        ToAction(service.GetPlatformsAsync(includeArchived, cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> Create(
        [FromBody] SimplePlatformUpsertRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.CreatePlatformAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] SimplePlatformUpsertRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.UpdatePlatformAsync(id, request, cancellationToken));

    private async Task<IActionResult> ToAction<T>(Task<LogisticsERP.Application.Common.Results.Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
