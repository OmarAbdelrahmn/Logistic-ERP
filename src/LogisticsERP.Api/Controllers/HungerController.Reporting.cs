using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

public sealed partial class HungerController
{
    [HttpPost("finance/settlement-documents")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> RecordSettlement(HungerSettlementRequest request, CancellationToken ct)
    {
        var result = await settlements.RecordSettlementAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/settlement-documents")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> SettlementDocuments(Guid platformId, DateOnly month, Guid? accountId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await settlements.SettlementsAsync(platformId, month, accountId, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("quality/{sourceId:guid}/reallocate")]
    [RequirePermission(PermissionKeys.Hunger.QualityImport)]
    public async Task<IActionResult> ReallocateQuality(Guid sourceId, HungerReallocationRequest request, CancellationToken ct)
    {
        var result = await quality.ReallocateQualityAsync(sourceId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : NoContent();
    }
    [HttpGet("reports/quality/export")]
    [RequirePermission(PermissionKeys.Hunger.QualityExport)]
    public async Task<IActionResult> ExportQuality(DateOnly month, Guid? accountId, CancellationToken ct)
    {
        var result = await reporting.ExportQualityAsync(month, accountId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpGet("reports/operations/export")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceExport)]
    public async Task<IActionResult> ExportOperations(DateOnly from, DateOnly through, Guid? accountId, CancellationToken ct)
    {
        var result = await reporting.ExportOperationsAsync(from, through, accountId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpGet("reports/summary")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> Summary(DateOnly month, CancellationToken ct)
    {
        var result = await reporting.SummaryAsync(month, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("alert-rules")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> AlertRules(CancellationToken ct)
    {
        var result = await reporting.AlertRulesAsync(ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("alert-rules")]
    [RequirePermission(PermissionKeys.Hunger.AlertsManage)]
    public async Task<IActionResult> AddAlertRule(HungerAlertRuleRequest request, CancellationToken ct)
    {
        var result = await reporting.AddAlertRuleAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(new { ruleId = result.Value });
    }
}
