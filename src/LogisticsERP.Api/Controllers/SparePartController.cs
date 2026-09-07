using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Maintenance;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SparePartController(IMaintenanceService service) : ControllerBase
{
    [HttpPost("spare-parts")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersManage)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> PostUsages(
        [FromQuery] DateTime date,
        [FromBody] BatchSparePartUsageRequest request,
        CancellationToken cancellationToken)
    {
        if (date == default || request.Usages is null || request.Usages.Count == 0)
            return BadRequest(new { message = "The body must contain a non-empty usages array." });

        var usedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc));
        var result = await service.PostBatchSparePartUsageAsync(usedAtUtc, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
