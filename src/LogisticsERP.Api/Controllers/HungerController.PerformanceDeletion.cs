using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LogisticsERP.Api.Controllers;

public sealed partial class HungerController
{
    [HttpDelete("performance"), RequirePermission(PermissionKeys.Hunger.PerformanceDelete)]
    public async Task<IActionResult> DeletePerformance([FromQuery, BindRequired] DateOnly reportDate, [FromQuery] string reason,
        [FromQuery] Guid? sponsorId = null, CancellationToken ct = default)
    {
        if (Request.Query.TryGetValue("sponsorId", out var suppliedSponsor) && string.IsNullOrWhiteSpace(suppliedSponsor))
            return Result.Failure<HungerPerformanceDeletionResponse>(HungerErrors.Invalid("حدد الكفيل أو احذف المعامل لحذف أداء اليوم لجميع الحسابات.")).ToProblem(HttpContext);
        var result = await operations.DeletePerformanceAsync(reportDate, sponsorId, reason, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
