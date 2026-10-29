using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/platform-accounts/{id:guid}/day-usage")]
public sealed class PlatformAccountDayUsageController(IPlatformAccountDayUsageService service) : ControllerBase
{
    [HttpGet("/api/platform-accounts/{id:guid}/assignment-context")]
    public async Task<IActionResult> AssignmentContext(Guid id, [FromQuery] DateOnly date, [FromQuery] Guid riderProfileId, CancellationToken ct)
    {
        var result = await service.AssignmentContextAsync(id, date, riderProfileId, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid id, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var result = await service.GetAsync(id, date, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut]
    public async Task<IActionResult> Update(Guid id, [FromQuery] DateOnly date,
        [FromBody] PlatformAccountDayUpdateRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, date, request, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
