using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/jahez/v2")]
public sealed class JahezWorkflowController(IJahezWorkflowService service) : ControllerBase
{
    [HttpGet("settlement-reminders"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> SettlementReminders(int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        Respond(service.SettlementRemindersAsync(page, pageSize, ct));
    [HttpPost("fees/refunds/preview"), RequirePermission(JahezWorkflowPermissions.FeeApprove)]
    public Task<IActionResult> PreviewFeeRefund(JahezFeeRefundPreviewRequest request, CancellationToken ct) => Respond(service.PreviewFeeRefundAsync(request, ct));
    [HttpPost("fees/refunds"), RequirePermission(JahezWorkflowPermissions.FeeApprove)]
    public Task<IActionResult> RefundFee([FromHeader(Name = "Idempotency-Key")] string key, JahezFeeRefundCreate request, CancellationToken ct) => Respond(service.RefundFeeAsync(key, request, ct));
    [HttpGet("handovers/{handoverId:guid}/fee-refunds"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> FeeRefunds(Guid handoverId, CancellationToken ct) => Respond(service.FeeRefundsAsync(handoverId, ct));
    [HttpPost("handovers"), RequirePermission(PermissionKeys.Jahez.HandoversCreate)]
    public Task<IActionResult> Handover([FromHeader(Name = "Idempotency-Key")] string key, JahezHandoverRequest request, CancellationToken ct) => Respond(service.HandoverAsync(key, request, ct));
    [HttpPost("earnings"), RequirePermission(PermissionKeys.Jahez.EarningsCreate)]
    public Task<IActionResult> Earnings([FromHeader(Name = "Idempotency-Key")] string key, JahezEarningsRequest request, CancellationToken ct) => Respond(service.RecordEarningsAsync(key, request, ct));
    [HttpGet("administrative-debts"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> AdministrativeDebts(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.AdministrativeDebtsAsync(page, pageSize, ct));
    [HttpGet("imports"), RequirePermission(PermissionKeys.Jahez.ImportsRead)]
    public Task<IActionResult> Imports(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.ImportBatchesAsync(page, pageSize, ct));
    [HttpGet("imports/{id:guid}"), RequirePermission(PermissionKeys.Jahez.ImportsRead)]
    public Task<IActionResult> Import(Guid id, CancellationToken ct) => Respond(service.PreviewImportAsync(id, ct));
    [HttpPost("imports/{id:guid}/commit"), RequirePermission(PermissionKeys.Jahez.ImportsUpdate)]
    public Task<IActionResult> CommitImport(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezImportCommitRequest request, CancellationToken ct) => Respond(service.CommitImportAsync(key, id, request, ct));
    [HttpPost("imports"), Consumes("multipart/form-data"), RequestSizeLimit(40 * 1024 * 1024),
        RequestFormLimits(MultipartBodyLengthLimit = 40 * 1024 * 1024), RequirePermission(PermissionKeys.Jahez.ImportsCreate)]
    public async Task<IActionResult> Upload([FromHeader(Name = "Idempotency-Key")] string key, [FromForm] LogisticsERP.Domain.Entities.Jahez.JahezImportKind kind,
        [FromForm] List<IFormFile> files, [FromForm] Guid? replacesBatchId, [FromForm] string? correctionReason, CancellationToken ct)
    {
        if (files.Count is < 1 or > 10 || files.Any(f => f.Length is <= 0 or > 10 * 1024 * 1024) || files.Sum(f => f.Length) > 30 * 1024 * 1024)
            return Result.Failure<JahezImportPreview>(JahezErrors.Invalid("حجم أو عدد الملفات يتجاوز الحد المسموح.")).ToProblem(HttpContext);
        List<JahezUploadFile> uploads = [];
        foreach (var file in files)
        {
            await using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
            uploads.Add(new(file.FileName, stream.ToArray()));
        }
        return await Respond(service.UploadAsync(key, new(kind, uploads, replacesBatchId, correctionReason), ct));
    }
    [HttpGet("import-files/{id:guid}"), RequirePermission(PermissionKeys.Jahez.ImportsRead)]
    public async Task<IActionResult> ImportFile(Guid id, CancellationToken ct)
    {
        var result = await service.GetImportFileAsync(id, ct);
        return result.IsSuccess ? File(result.Value!.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Value.FileName) : result.ToProblem(HttpContext);
    }
    [HttpGet("accounts"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Accounts(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.AccountsV2Async(page, pageSize, ct));
    [HttpGet("accounts/{id:guid}"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Account(Guid id, int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.AccountDetailsV2Async(id, page, pageSize, ct));
    [HttpGet("statistics"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Statistics(CancellationToken ct) => Respond(service.StatisticsV2Async(ct));
    [HttpGet("reports/riders"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> RiderPerformanceReport([FromQuery, BindRequired] DateOnly startDate,
        [FromQuery, BindRequired] DateOnly endDate, Guid? riderId = null, Guid? accountId = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        Respond(service.RiderPerformanceReportAsync(startDate, endDate, riderId, accountId, page, pageSize, ct));
    [HttpGet("reports/profit"), RequirePermission(JahezTreasuryPermissions.ReportsRead)]
    public Task<IActionResult> ProfitReport(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default) =>
        Respond(service.ProfitReportAsync(fromDate, toDate, ct));
    [HttpPut("accounts/{id:guid}/status"), RequirePermission(JahezWorkflowPermissions.StatusUpdate)]
    public Task<IActionResult> Status(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezAccountStatusUpdate request, CancellationToken ct) => Respond(service.UpdateAccountStatusV2Async(key, id, request, ct));
    [HttpPost("operations/preview"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Preview(JahezOperationCreate request, CancellationToken ct) => Respond(service.PreviewOperationAsync(request, ct));
    [HttpPost("operations"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Create([FromHeader(Name = "Idempotency-Key")] string key, JahezOperationCreate request, CancellationToken ct) => Respond(service.CreateOperationAsync(key, request, ct));
    [HttpGet("operations"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Operations(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.OperationsV2Async(page, pageSize, ct));
    [HttpPost("operations/{id:guid}/execute"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Execute(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, CancellationToken ct) => Respond(service.ExecuteOperationAsync(key, id, ct));
    [HttpPut("operations/{id:guid}"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> UpdateOperation(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezOperationUpdate request, CancellationToken ct) => Respond(service.UpdateOperationAsync(key, id, request, ct));
    [HttpPut("operations/{id:guid}/payment-method"), RequirePermission(JahezWorkflowPermissions.Settle)]
    public Task<IActionResult> UpdatePaymentMethod(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezOperationPaymentMethodUpdate request, CancellationToken ct) => Respond(service.UpdateOperationPaymentMethodAsync(key, id, request, ct));
    [HttpPut("operations/{id:guid}/net"), RequirePermission(JahezWorkflowPermissions.AdjustmentApprove)]
    public Task<IActionResult> UpdateNet(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezOperationNetUpdate request, CancellationToken ct) => Respond(service.UpdateOperationNetAsync(key, id, request, ct));
    [HttpPut("operations/{id:guid}/commission-start"), RequirePermission(JahezWorkflowPermissions.AdjustmentApprove)]
    public Task<IActionResult> UpdateCommissionStart(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezOperationCommissionStartUpdate request, CancellationToken ct) => Respond(service.UpdateOperationCommissionStartAsync(key, id, request, ct));
    // Per-kind permissions are resolved by the service before idempotency replay.
    [HttpPost("requests"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> CreateRequest([FromHeader(Name = "Idempotency-Key")] string key, JahezWorkflowRequestCreate request, CancellationToken ct) => Respond(service.CreateWorkflowRequestAsync(key, request, ct));
    [HttpGet("requests"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Requests(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.RequestsV2Async(page, pageSize, ct));
    [HttpGet("requests/{id:guid}"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> RequestDetails(Guid id, CancellationToken ct) => Respond(service.RequestV2Async(id, ct));
    [HttpPost("requests/{id:guid}/decision"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Decision(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezWorkflowDecision request, CancellationToken ct) => Respond(service.DecideWorkflowRequestAsync(key, id, request, ct));
    [HttpPost("fees/receipts"), RequirePermission(JahezWorkflowPermissions.FeesRecord)]
    public Task<IActionResult> Fee([FromHeader(Name = "Idempotency-Key")] string key, JahezFeeRecord request, CancellationToken ct) => Respond(service.RecordFeeV2Async(key, request, ct));
    [HttpGet("transferred-debts"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> Debts(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.TransferredDebtsAsync(page, pageSize, ct));
    [HttpGet("riders/transferred-debts"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> RiderDebts(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.RidersWithTransferredDebtsAsync(page, pageSize, ct));
    [HttpGet("riders/{riderId:guid}/transferred-debts"), RequirePermission(PermissionKeys.Jahez.Read)]
    public Task<IActionResult> RiderDebtDetails(Guid riderId, int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.RiderTransferredDebtsAsync(riderId, page, pageSize, ct));
    [HttpPost("riders/{riderId:guid}/transferred-debts/repayment"), RequirePermission(JahezWorkflowPermissions.Repay)]
    public Task<IActionResult> RepayRiderDebts(Guid riderId, [FromHeader(Name = "Idempotency-Key")] string key,
        JahezRiderDebtRepayment request, CancellationToken ct) => Respond(service.RepayRiderTransferredDebtsAsync(key, riderId, request, ct));
    [HttpPost("transferred-debts/{id:guid}/repayment"), RequirePermission(JahezWorkflowPermissions.Repay)]
    public Task<IActionResult> Repay(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezMoneyRecord request, CancellationToken ct) => Respond(service.RepayTransferredDebtAsync(key, id, request, ct));
    [HttpPost("riders/{riderId:guid}/transferred-debts/partial-repayment"), RequirePermission(JahezWorkflowPermissions.Repay)]
    public Task<IActionResult> PartialRepayRiderDebts(Guid riderId, [FromHeader(Name = "Idempotency-Key")] string key,
        JahezRiderDebtPartialRepayment request, CancellationToken ct) => Respond(service.PartiallyRepayRiderTransferredDebtsAsync(key, riderId, request, ct));
    [HttpPut("riders/{riderId:guid}/transferred-debts/commission"), RequirePermission(JahezWorkflowPermissions.AdjustmentApprove)]
    public Task<IActionResult> UpdateRiderDebtCommission(Guid riderId, [FromHeader(Name = "Idempotency-Key")] string key,
        JahezRiderDebtCommissionUpdate request, CancellationToken ct) => Respond(service.UpdateRiderDebtCommissionAsync(key, riderId, request, ct));
    [HttpGet("money-movements"), RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> Movements(int page = 1, int pageSize = 50, DateOnly? fromDate = null,
        DateOnly? toDate = null, CancellationToken ct = default) => Respond(service.MovementsV2Async(page, pageSize, fromDate, toDate, ct));
    [HttpGet("funds"), RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> Funds(CancellationToken ct) => Respond(service.FundsV2Async(ct));
    [HttpGet("cashbox/handovers"), RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> CashboxHandovers(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.CashboxHandoversV2Async(page, pageSize, ct));
    [HttpGet("cashbox/handovers/{id:guid}"), RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> CashboxHandover(Guid id, CancellationToken ct) => Respond(service.CashboxHandoverV2Async(id, ct));
    [HttpPost("cashbox/handovers"), RequirePermission(PermissionKeys.Jahez.CashboxSubmit)]
    public Task<IActionResult> SubmitCashboxHandover([FromHeader(Name = "Idempotency-Key")] string key, JahezCashboxHandoverCreate request, CancellationToken ct) => Respond(service.SubmitCashboxHandoverV2Async(key, request, ct));
    [HttpPost("cashbox/handovers/{id:guid}/confirm"), RequirePermission(PermissionKeys.Jahez.CashboxConfirm)]
    public Task<IActionResult> ConfirmCashboxHandover(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezCashboxHandoverConfirm request, CancellationToken ct) => Respond(service.ConfirmCashboxHandoverV2Async(key, id, request, ct));
    [HttpPost("cashbox/handovers/{id:guid}/cancel"), RequirePermission(PermissionKeys.Jahez.CashboxSubmit)]
    public Task<IActionResult> CancelCashboxHandover(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezCashboxHandoverCancel request, CancellationToken ct) => Respond(service.CancelCashboxHandoverV2Async(key, id, request, ct));
    private async Task<IActionResult> Respond<T>(Task<Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
