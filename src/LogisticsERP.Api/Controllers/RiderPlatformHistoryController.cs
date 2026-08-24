using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/riders/{riderProfileId:guid}/platform-history")]
public sealed class RiderPlatformHistoryController(ISimplePlatformService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Operations.PlatformAssignmentsRead)]
    public async Task<IActionResult> Get(Guid riderProfileId, CancellationToken cancellationToken)
    {
        var result = await service.GetRiderPlatformHistoryAsync(riderProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
