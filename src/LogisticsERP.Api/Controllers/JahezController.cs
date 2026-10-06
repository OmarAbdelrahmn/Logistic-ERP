using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/jahez")]
public sealed class JahezController(IJahezService service, IJahezResponseMapper responseMapper) : ControllerBase
{
    [HttpPost("handovers")]
    [RequirePermission(PermissionKeys.Jahez.HandoversCreate)]
    public Task<IActionResult> Handover([FromHeader(Name = "Idempotency-Key")] string key, JahezHandoverRequest request, CancellationToken ct) => ToAction(service.HandoverAsync(key, request, ct));

    [HttpPost("legacy-adoptions")]
    [RequirePermission(PermissionKeys.Jahez.AdjustmentsCreate)]
    public Task<IActionResult> Adopt([FromHeader(Name = "Idempotency-Key")] string key, JahezLegacyAdoptionRequest request, CancellationToken ct) => ToAction(service.AdoptLegacyAsync(key, request, ct));

    [HttpPost("handovers/{id:guid}/close")]
    [RequirePermission(PermissionKeys.Jahez.HandoversDelete)]
    public Task<IActionResult> Close(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezCloseRequest request, CancellationToken ct) => ToAction(service.CloseAsync(key, id, request, ct));

    [HttpGet("handovers")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Handovers(Guid? accountId, Guid? riderId, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetHandoversAsync(accountId, riderId, page, pageSize, ct));

    [HttpGet("handovers/{id:guid}/balance")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Balance(Guid id, DateOnly through, CancellationToken ct) => ToAction(service.GetBalanceAsync(id, through, ct));

    [HttpGet("debts")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Debts(Guid? riderId, bool overdueOnly = false, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetDebtsAsync(riderId, overdueOnly, page, pageSize, ct));

    [HttpGet("ledger")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Ledger(Guid? riderId, Guid? handoverId, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetLedgerAsync(riderId, handoverId, page, pageSize, ct));

    [HttpGet("handovers/{id:guid}/fee")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Fee(Guid id, CancellationToken ct) => ToAction(service.GetFeeAsync(id, ct));

    [HttpGet("handovers/{id:guid}/settlements")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Settlements(Guid id, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetSettlementsAsync(id, page, pageSize, ct));

    [HttpGet("handovers/{id:guid}/earnings")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> EarningsHistory(Guid id, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetEarningsAsync(id, page, pageSize, ct));

    [HttpGet("handovers/{id:guid}/commission-policies")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Policies(Guid id, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetPoliciesAsync(id, page, pageSize, ct));

    [HttpPost("requests")]
    [RequirePermission(PermissionKeys.Jahez.RequestsCreate)]
    public Task<IActionResult> RequestApproval([FromHeader(Name = "Idempotency-Key")] string key, JahezApprovalCreateRequest request, CancellationToken ct) => ToAction(service.RequestApprovalAsync(key, request, ct));

    // The service checks the specific approval permission after resolving the request kind.
    [HttpPost("requests/{id:guid}/decision")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> DecideApproval(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezDecisionRequest request, CancellationToken ct) => ToAction(service.DecideApprovalAsync(key, id, request, ct));

    [HttpPost("requests/{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.Jahez.RequestsCreate)]
    public Task<IActionResult> CancelApproval(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezCancelRequest request, CancellationToken ct) => ToAction(service.CancelApprovalAsync(key, id, request.Reason, ct));

    [HttpGet("requests")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Requests(JahezApprovalStatus? status, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetApprovalsAsync(status, page, pageSize, ct));

    [HttpGet("requests/{id:guid}")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> GetRequest(Guid id, CancellationToken ct) => ToAction(service.GetApprovalAsync(id, ct));

    [HttpPost("earnings")]
    [RequirePermission(PermissionKeys.Jahez.EarningsCreate)]
    public Task<IActionResult> Earnings([FromHeader(Name = "Idempotency-Key")] string key, JahezEarningsRequest request, CancellationToken ct) => ToAction(service.RecordEarningsAsync(key, request, ct));

    [HttpPost("settlements")]
    [RequirePermission(PermissionKeys.Jahez.CollectionsCreate)]
    public Task<IActionResult> Payment([FromHeader(Name = "Idempotency-Key")] string key, JahezPaymentRequest request, CancellationToken ct) => ToAction(service.PayAsync(key, request, ct));

    [HttpPost("adjustments")]
    [RequirePermission(PermissionKeys.Jahez.AdjustmentsCreate)]
    public Task<IActionResult> Adjustment([FromHeader(Name = "Idempotency-Key")] string key, JahezLedgerAdjustmentRequest request, CancellationToken ct) => ToAction(service.AdjustAsync(key, request, ct));

    [HttpPost("imports")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(40 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 40 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Jahez.ImportsCreate)]
    public async Task<IActionResult> Upload([FromHeader(Name = "Idempotency-Key")] string key, [FromForm] JahezImportKind kind,
        [FromForm] List<IFormFile> files, [FromForm] Guid? replacesBatchId, [FromForm] string? correctionReason, CancellationToken ct)
    {
        if (files.Count is < 1 or > 10 || files.Any(f => f.Length is <= 0 or > 10 * 1024 * 1024) || files.Sum(f => f.Length) > 30 * 1024 * 1024)
            return Result.Failure<JahezImportPreview>(JahezErrors.Invalid("حجم أو عدد الملفات يتجاوز الحد المسموح.")).ToProblem(HttpContext);
        List<JahezUploadFile> uploads = [];
        foreach (var f in files)
        {
            await using var stream = new MemoryStream();
            await f.CopyToAsync(stream, ct);
            uploads.Add(new(f.FileName, stream.ToArray()));
        }
        return await ToAction(service.UploadAsync(key, new(kind, uploads, replacesBatchId, correctionReason), ct));
    }

    [HttpGet("imports/{id:guid}")]
    [RequirePermission(PermissionKeys.Jahez.ImportsRead)]
    public Task<IActionResult> ImportPreview(Guid id, CancellationToken ct) => ToAction(service.PreviewImportAsync(id, ct));

    [HttpGet("imports")]
    [RequirePermission(PermissionKeys.Jahez.ImportsRead)]
    public Task<IActionResult> Imports(int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetImportBatchesAsync(page, pageSize, ct));

    [HttpPost("imports/{id:guid}/commit")]
    [RequirePermission(PermissionKeys.Jahez.ImportsUpdate)]
    public Task<IActionResult> CommitImport(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezImportCommitRequest request, CancellationToken ct) => ToAction(service.CommitImportAsync(key, id, request, ct));

    [HttpGet("import-files/{id:guid}")]
    [RequirePermission(PermissionKeys.Jahez.ImportsRead)]
    public async Task<IActionResult> DownloadImportFile(Guid id, CancellationToken ct)
    {
        var result = await service.GetImportFileAsync(id, ct);
        return result.IsSuccess ? File(result.Value!.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Value.FileName) : result.ToProblem(HttpContext);
    }

    [HttpGet("dispatches")]
    [RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Dispatches(DateOnly from, DateOnly to, Guid? riderId, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetDispatchesAsync(from, to, riderId, page, pageSize, ct));

    [HttpGet("cashbox")]
    [RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> Cashbox(CancellationToken ct) => ToAction(service.GetCashboxAsync(ct));

    [HttpGet("cashbox/handovers")]
    [RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> CashboxHandovers(int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetCashboxHandoversAsync(page, pageSize, ct));

    [HttpGet("cashbox/entries")]
    [RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> CashboxEntries(Guid? cashboxHandoverId, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetCashboxEntriesAsync(cashboxHandoverId, page, pageSize, ct));

    [HttpPost("cashbox/handovers")]
    [RequirePermission(PermissionKeys.Jahez.CashboxSubmit)]
    public Task<IActionResult> SubmitCashbox([FromHeader(Name = "Idempotency-Key")] string key, JahezCashboxCreateRequest request, CancellationToken ct) => ToAction(service.SubmitCashboxAsync(key, request, ct));

    [HttpPost("cashbox/handovers/{id:guid}/confirm")]
    [RequirePermission(PermissionKeys.Jahez.CashboxConfirm)]
    public Task<IActionResult> ConfirmCashbox(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezAccountantConfirmRequest request, CancellationToken ct) => ToAction(service.ConfirmCashboxAsync(key, id, request, ct));

    [HttpPost("cashbox/handovers/{id:guid}/decision")]
    [RequirePermission(PermissionKeys.Jahez.CashboxApprove)]
    public Task<IActionResult> DecideCashbox(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezDecisionRequest request, CancellationToken ct) => ToAction(service.DecideCashboxAsync(key, id, request, ct));

    private async Task<IActionResult> ToAction<T>(Task<Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess
            ? Ok(await responseMapper.MapAsync(result.Value, HttpContext.RequestAborted))
            : result.ToProblem(HttpContext);
    }
}

public sealed record JahezCancelRequest(string Reason);
