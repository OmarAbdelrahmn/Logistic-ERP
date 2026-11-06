using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

public sealed partial class HungerController
{
    [HttpGet("accounts/deferred-reminders")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> DeferredReminders(DateOnly? dueThrough, CancellationToken ct)
    {
        var result = await accounts.DeferredRemindersAsync(dueThrough, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("accounts/template")]
    [RequirePermission(PermissionKeys.Hunger.AccountsImport)]
    public async Task<IActionResult> AccountTemplate(CancellationToken ct)
    {
        var result = await accounts.AccountTemplateAsync(ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpGet("accounts/export")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceExport)]
    public async Task<IActionResult> ExportAccounts([FromQuery] HungerAccountFilter filter, string format = "xlsx", CancellationToken ct = default)
    {
        var result = await accounts.ExportAccountsAsync(filter, format, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpGet("wallets")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead)]
    public async Task<IActionResult> Wallets([FromQuery] HungerWalletFilter filter, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await finance.WalletsAsync(filter, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("wallets/export")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead)]
    public async Task<IActionResult> ExportWallets([FromQuery] HungerWalletFilter filter, string format = "xlsx", CancellationToken ct = default)
    {
        var result = await finance.ExportWalletsAsync(filter, format, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpGet("accounts/{accountId:guid}/wallet-history")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead)]
    public async Task<IActionResult> WalletHistory(Guid accountId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await finance.WalletHistoryAsync(accountId, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("reports/performance/export")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceExport)]
    public async Task<IActionResult> ExportPerformance([FromQuery] HungerPerformanceQuery query, CancellationToken ct)
    {
        var result = await reporting.ExportPerformanceAsync(query, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpGet("reports/performance")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> Performance([FromQuery] HungerPerformanceQuery query, CancellationToken ct)
    {
        var result = await reporting.PerformanceAsync(query, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("potential-fines")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> PotentialFines(DateOnly from, DateOnly through, CancellationToken ct)
    {
        var result = await reporting.PotentialFinesAsync(from, through, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("potential-fines/{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> ResolvePotentialFine(Guid id, HungerPotentialFineResolution request, CancellationToken ct)
    {
        var result = await reporting.ResolvePotentialFineAsync(id, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : NoContent();
    }
    [HttpPost("finance/settlement-documents")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> RecordSettlement(HungerSettlementRequest request, CancellationToken ct)
    {
        var result = await settlements.RecordSettlementAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/settlement-documents")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead)]
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
    public async Task<IActionResult> ExportQuality(DateOnly month, Guid? accountId, [FromQuery] HungerReportExportFilter filter, CancellationToken ct)
    {
        var result = await reporting.ExportQualityAsync(month, accountId, filter, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpGet("reports/operations/export")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceExport)]
    public async Task<IActionResult> ExportOperations(DateOnly from, DateOnly through, Guid? accountId, [FromQuery] HungerReportExportFilter filter, CancellationToken ct)
    {
        var result = await reporting.ExportOperationsAsync(from, through, accountId, filter, ct);
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
