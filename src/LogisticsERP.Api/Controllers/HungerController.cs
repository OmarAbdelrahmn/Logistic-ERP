using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/hunger")]
public sealed partial class HungerController(IHungerQualityService quality, IHungerAccountsService accounts, IHungerOperationsService operations, IHungerFinanceService finance, IHungerReportingService reporting, IHungerSettlementService settlements) : ControllerBase
{
    [HttpPut("finance/configuration")]
    [RequirePermission(PermissionKeys.Hunger.FinanceConfigure)]
    public async Task<IActionResult> ConfigureFinance(HungerFinanceSetup request, CancellationToken ct)
    {
        var result = await finance.ConfigureFinanceAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : NoContent();
    }
    [HttpPost("finance/events")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> PostMoney(HungerMoneyRequest request, CancellationToken ct)
    {
        var result = await finance.PostMoneyAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/events")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> FinancialEvents(Guid platformId, Guid supervisorEmployeeId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await finance.FinanceReportAsync(platformId, supervisorEmployeeId, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/remittances")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> SubmitRemittance(HungerRemittanceRequest request, CancellationToken ct)
    {
        var result = await finance.SubmitRemittanceAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/remittances/{remittanceId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.RemittancesConfirm)]
    public async Task<IActionResult> ConfirmRemittance(Guid remittanceId, HungerRemittanceConfirmation request, CancellationToken ct)
    {
        var result = await finance.ConfirmRemittanceAsync(remittanceId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("accounts/{accountId:guid}/wallet-review")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> ReviewWallet(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct)
    {
        var result = await finance.ReviewWalletAsync(accountId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("wallet-reviews/{reviewId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> ConfirmWallet(Guid reviewId, CancellationToken ct)
    {
        var result = await finance.ConfirmWalletAsync(reviewId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("operations/template")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceImport)]
    public async Task<IActionResult> OperationTemplate(CancellationToken ct)
    {
        var result = await operations.OperationTemplateAsync(ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpPost("operations/imports/preview")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> PreviewOperations([FromForm] IFormFile file, [FromForm] DateOnly reportDate, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024) return BadRequest(new { message = "ارفع ملفًا بحد أقصى 10 ميجابايت." });
        using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
        var result = await operations.PreviewOperationsAsync(file.FileName, stream.ToArray(), reportDate, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("operations/imports/{previewId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceImport)]
    public async Task<IActionResult> ConfirmOperations(Guid previewId, CancellationToken ct)
    {
        var result = await operations.ConfirmOperationsAsync(previewId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("reports/operations")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> Operations(DateOnly from, DateOnly through, Guid? accountId, Guid? riderId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await operations.OperationsAsync(from, through, accountId, riderId, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPut("operations/{sourceId:guid}/manual")]
    [RequirePermission(PermissionKeys.Hunger.PerformanceImport)]
    public async Task<IActionResult> ManualOperations(Guid sourceId, HungerManualPerformanceRequest request, CancellationToken ct)
    {
        var result = await operations.ManualPerformanceAsync(sourceId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : NoContent();
    }
    [HttpGet("accounts")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> Accounts(string? disposition, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await accounts.AccountsAsync(disposition, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPut("accounts/{accountId:guid}/disposition")]
    [RequirePermission(PermissionKeys.Hunger.AccountsUpdate)]
    public async Task<IActionResult> Disposition(Guid accountId, HungerDispositionRequest request, CancellationToken ct)
    {
        var result = await accounts.DispositionAsync(accountId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : NoContent();
    }
    [HttpPost("accounts/imports/preview")]
    [RequirePermission(PermissionKeys.Hunger.AccountsImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> PreviewAccounts([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024) return BadRequest(new { message = "ارفع ملفًا بحد أقصى 10 ميجابايت." });
        using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
        var result = await accounts.PreviewAccountsAsync(file.FileName, stream.ToArray(), ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("accounts/imports/{previewId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.AccountsImport)]
    public async Task<IActionResult> ConfirmAccounts(Guid previewId, CancellationToken ct)
    {
        var result = await accounts.ConfirmAccountsAsync(previewId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("quality/template")]
    [RequirePermission(PermissionKeys.Hunger.QualityImport)]
    public async Task<IActionResult> QualityTemplate(CancellationToken ct)
    {
        var result = await quality.TemplateAsync(ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }
    [HttpPost("quality/imports/preview")]
    [RequirePermission(PermissionKeys.Hunger.QualityImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> PreviewQuality([FromForm] IFormFile file, [FromForm] DateOnly reportDate, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024) return BadRequest(new { message = "ارفع ملفًا بحد أقصى 10 ميجابايت." });
        using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
        var result = await quality.PreviewAsync(file.FileName, stream.ToArray(), reportDate, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("quality/imports/{previewId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.QualityImport)]
    public async Task<IActionResult> ConfirmQuality(Guid previewId, CancellationToken ct)
    {
        var result = await quality.ConfirmAsync(previewId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("quality/imports/{importId:guid}")]
    [RequirePermission(PermissionKeys.Hunger.QualityImport)]
    public async Task<IActionResult> QualityImportResult(Guid importId, CancellationToken ct)
    {
        var result = await quality.ImportResultAsync(importId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("reports/quality")]
    [RequirePermission(PermissionKeys.Hunger.Read)]
    public async Task<IActionResult> QualityReport(DateOnly month, Guid? accountId, Guid? riderId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await quality.ReportAsync(month, accountId, riderId, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
}
