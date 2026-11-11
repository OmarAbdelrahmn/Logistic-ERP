using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using Microsoft.AspNetCore.Mvc;
using LogisticsERP.Application.Abstractions.Files;

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
    [RequirePermission(PermissionKeys.Hunger.FundingExecute, PermissionKeys.Hunger.TopUpExecute, PermissionKeys.Hunger.Collection, PermissionKeys.Hunger.WithdrawalRequest, PermissionKeys.Hunger.RiderPayment)]
    public async Task<IActionResult> PostMoney(HungerMoneyRequest request, CancellationToken ct)
    {
        var result = await finance.PostMoneyAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/withdrawals/{withdrawalId:guid}/confirm-arrival")]
    [RequirePermission(PermissionKeys.Hunger.WithdrawalArrivalConfirm)]
    public async Task<IActionResult> ConfirmWithdrawalArrival(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct)
    {
        var result = await finance.ConfirmWithdrawalArrivalAsync(withdrawalId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/withdrawals/{withdrawalId:guid}/cancel")]
    [RequirePermission(PermissionKeys.Hunger.WithdrawalRequest)]
    public async Task<IActionResult> CancelWithdrawal(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct)
    {
        var result = await finance.CancelWithdrawalAsync(withdrawalId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/settlement-documents/{documentId:guid}/reconcile")]
    [RequirePermission(PermissionKeys.Hunger.FinanceManage)]
    public async Task<IActionResult> ReconcileAdjustment(Guid documentId, HungerAdjustmentReconciliation request, CancellationToken ct)
    {
        var result = await settlements.ReconcileAdjustmentAsync(documentId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/events")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead)]
    public async Task<IActionResult> FinancialEvents(Guid platformId, Guid supervisorEmployeeId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await finance.FinanceReportAsync(platformId, supervisorEmployeeId, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/remittances")]
    [RequirePermission(PermissionKeys.Hunger.RemittancesSubmit)]
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
    [HttpPost("finance/requests")]
    [RequirePermission(PermissionKeys.Hunger.FundingRequest, PermissionKeys.Hunger.TopUpRequest)]
    public async Task<IActionResult> RequestMoney(HungerMoneyRequest request, CancellationToken ct)
    {
        var result = await finance.RequestMoneyAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/requests/{requestId:guid}/execute")]
    [RequirePermission(PermissionKeys.Hunger.FundingExecute, PermissionKeys.Hunger.TopUpExecute)]
    public async Task<IActionResult> ExecuteMoneyRequest(Guid requestId, [FromBody] HungerMoneyExecution? execution = null, CancellationToken ct = default)
    {
        var result = await finance.ExecuteMoneyRequestAsync(requestId, execution, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/remittances/drafts")]
    [RequirePermission(PermissionKeys.Hunger.RemittancesSubmit)]
    public async Task<IActionResult> CreateRemittanceDraft(HungerRemittanceRequest request, CancellationToken ct)
    {
        var result = await finance.CreateRemittanceDraftAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/remittances/{remittanceId:guid}/submit")]
    [RequirePermission(PermissionKeys.Hunger.RemittancesSubmit)]
    public async Task<IActionResult> SubmitRemittanceDraft(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct)
    {
        var result = await finance.SubmitRemittanceDraftAsync(remittanceId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/remittances/{remittanceId:guid}/cancel")]
    [RequirePermission(PermissionKeys.Hunger.ReceiptsCancelReverse)]
    public async Task<IActionResult> CancelRemittance(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct)
    {
        var result = await finance.CancelRemittanceAsync(remittanceId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/receipt-confirmations/{confirmationId:guid}/reverse")]
    [RequirePermission(PermissionKeys.Hunger.ReceiptsCancelReverse)]
    public async Task<IActionResult> ReverseRemittanceConfirmation(Guid confirmationId, HungerRemittanceAction request, CancellationToken ct)
    {
        var result = await finance.ReverseRemittanceConfirmationAsync(confirmationId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/pending-collections")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead)]
    public async Task<IActionResult> PendingCollections(Guid platformId, Guid supervisorEmployeeId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await finance.PendingCollectionsAsync(platformId, supervisorEmployeeId, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/events/{eventId:guid}/seal")]
    [RequirePermission(PermissionKeys.Hunger.Seal)]
    public async Task<IActionResult> SealEvent(Guid eventId, HungerSealRequest request, CancellationToken ct)
    {
        var result = await finance.SealEventAsync(eventId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/events/{eventId:guid}/reject")]
    [RequirePermission(PermissionKeys.Hunger.Seal)]
    public async Task<IActionResult> RejectEvent(Guid eventId, HungerSealRejection request, CancellationToken ct)
    {
        var result = await finance.RejectEventAsync(eventId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/seal-queue")]
    [RequirePermission(PermissionKeys.Hunger.Seal, PermissionKeys.Hunger.FinanceRead)]
    public async Task<IActionResult> SealQueue(Guid platformId, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await finance.SealQueueAsync(platformId, status, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/evidence")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Hunger.WalletReview, PermissionKeys.Hunger.TopUpRequest, PermissionKeys.Hunger.TopUpExecute, PermissionKeys.Hunger.WithdrawalRequest)]
    public async Task<IActionResult> UploadEvidence([FromForm] HungerEvidenceForm form, CancellationToken ct)
    {
        await using var stream = form.File?.OpenReadStream();
        var image = form.File is { } file ? new PrivateFileUpload(stream!, file.FileName, file.ContentType, file.Length) : null;
        var result = await finance.UploadEvidenceAsync(new(form.PlatformId, form.AccountId, form.Purpose, form.ObservedOn), image, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/requests/{requestId:guid}/approve")]
    [RequirePermission(PermissionKeys.Hunger.FundingApprove)]
    public async Task<IActionResult> ApproveFunding(Guid requestId, HungerFundingDecision request, CancellationToken ct)
    {
        var result = await finance.DecideFundingAsync(requestId, true, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/requests/{requestId:guid}/reject")]
    [RequirePermission(PermissionKeys.Hunger.FundingApprove)]
    public async Task<IActionResult> RejectFunding(Guid requestId, HungerFundingDecision request, CancellationToken ct)
    {
        var result = await finance.DecideFundingAsync(requestId, false, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/custody-settlement")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead)]
    public async Task<IActionResult> CustodySettlement(Guid platformId, Guid supervisorEmployeeId, CancellationToken ct)
    {
        var result = await finance.CustodySettlementAsync(platformId, supervisorEmployeeId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/owner-collections/{receiptId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.OwnerCollectionsConfirm)]
    public async Task<IActionResult> ConfirmOwnerCollection(Guid receiptId, HungerOwnerCollectionConfirmation request, CancellationToken ct)
    {
        var result = await finance.ConfirmOwnerCollectionAsync(receiptId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/exceptions")]
    [RequirePermission(PermissionKeys.Hunger.ExceptionRequest)]
    public async Task<IActionResult> RequestException(HungerExceptionRequest request, CancellationToken ct)
    {
        var result = await finance.RequestExceptionAsync(request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/exceptions/{exceptionId:guid}/approve")]
    [RequirePermission(PermissionKeys.Hunger.ExceptionApprove)]
    public async Task<IActionResult> ApproveException(Guid exceptionId, HungerExceptionDecision request, CancellationToken ct)
    {
        var result = await finance.DecideExceptionAsync(exceptionId, true, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("finance/exceptions/{exceptionId:guid}/reject")]
    [RequirePermission(PermissionKeys.Hunger.ExceptionApprove)]
    public async Task<IActionResult> RejectException(Guid exceptionId, HungerExceptionDecision request, CancellationToken ct)
    {
        var result = await finance.DecideExceptionAsync(exceptionId, false, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpGet("finance/exceptions")]
    [RequirePermission(PermissionKeys.Hunger.FinanceRead, PermissionKeys.Hunger.ExceptionApprove)]
    public async Task<IActionResult> Exceptions(Guid platformId, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await finance.ExceptionsAsync(platformId, status, page, pageSize, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("accounts/{accountId:guid}/wallet-review")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Hunger.WalletReview)]
    public async Task<IActionResult> ReviewWalletImage(Guid accountId, [FromForm] HungerWalletReviewForm form, CancellationToken ct)
    {
        await using var stream = form.WalletEvidence?.OpenReadStream();
        var image = form.WalletEvidence is { } file
            ? new PrivateFileUpload(stream!, file.FileName, file.ContentType, file.Length) : null;
        HungerWalletReviewRequest request;
        try { request = form.ToRequest(await Request.ReadFormAsync(ct)); }
        catch (ArgumentException)
        {
            return Result.Failure<HungerWalletReviewResponse>(HungerErrors.Invalid("حقول إصدارات استخدام اليوم غير صالحة أو متعارضة؛ أعد تحميل سياق التكليف.")).ToProblem(HttpContext);
        }
        var result = await finance.ReviewWalletWithImageAsync(accountId, request, image, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("wallet-reviews/{reviewId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.WalletReview)]
    public async Task<IActionResult> ConfirmWallet(Guid reviewId, CancellationToken ct)
    {
        var result = await finance.ConfirmWalletAsync(reviewId, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    // Multipart variant: field `request` holds the JSON swap request, `walletEvidence` the screenshot.
    [HttpPost("accounts/{accountId:guid}/rider-swap/review")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Hunger.WalletReview)]
    public async Task<IActionResult> ReviewRiderSwapImage(Guid accountId, [FromForm] string? request, IFormFile? walletEvidence, CancellationToken ct)
    {
        HungerRiderSwapRequest? parsed = null;
        try
        {
            var options = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            if (!string.IsNullOrWhiteSpace(request)) parsed = System.Text.Json.JsonSerializer.Deserialize<HungerRiderSwapRequest>(request, options);
        }
        catch (System.Text.Json.JsonException) { }
        if (parsed is null) return Result.Failure<HungerRiderSwapReview>(HungerErrors.Invalid("حقل request يجب أن يحمل طلب التبديل بصيغة JSON.")).ToProblem(HttpContext);
        await using var stream = walletEvidence?.OpenReadStream();
        var image = walletEvidence is { } file ? new PrivateFileUpload(stream!, file.FileName, file.ContentType, file.Length) : null;
        var result = await finance.ReviewRiderSwapWithImageAsync(accountId, parsed, image, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("accounts/{accountId:guid}/rider-swap/review")]
    [Consumes("application/json")]
    [RequirePermission(PermissionKeys.Hunger.WalletReview)]
    public async Task<IActionResult> ReviewRiderSwap(Guid accountId, HungerRiderSwapRequest request, CancellationToken ct)
    {
        var result = await finance.ReviewRiderSwapAsync(accountId, request, ct);
        return result.IsFailure ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
    [HttpPost("rider-swaps/{swapId:guid}/confirm")]
    [RequirePermission(PermissionKeys.Hunger.WalletReview)]
    public async Task<IActionResult> ConfirmRiderSwap(Guid swapId, CancellationToken ct)
    {
        var result = await finance.ConfirmRiderSwapAsync(swapId, ct);
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
    public async Task<IActionResult> Accounts([FromQuery] HungerAccountFilter filter, string? disposition, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await accounts.AccountsFilteredAsync(filter with { OperationalDisposition = disposition ?? filter.OperationalDisposition }, page, pageSize, ct);
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
    public async Task<IActionResult> ConfirmAccounts(Guid previewId, HungerAccountImportConfirmation request, CancellationToken ct)
    {
        var result = await accounts.ConfirmAccountsAsync(previewId, request, ct);
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
