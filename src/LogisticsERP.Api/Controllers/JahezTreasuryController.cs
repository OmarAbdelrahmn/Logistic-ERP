using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/jahez/v3/treasury")]
public sealed class JahezTreasuryController(IJahezTreasuryService service) : ControllerBase
{
    [HttpGet("accounts"), RequirePermission(JahezTreasuryPermissions.Read)]
    public Task<IActionResult> Accounts(CancellationToken ct) => Respond(service.TreasuryAccountsAsync(ct));

    [HttpPost("accounts"), RequirePermission(JahezTreasuryPermissions.AccountsCreate)]
    public Task<IActionResult> CreateAccount([FromHeader(Name = "Idempotency-Key")] string key, TreasuryAccountCreate request, CancellationToken ct) =>
        Respond(service.CreateTreasuryAccountAsync(key, request, ct));

    [HttpPut("accounts/{id:guid}"), RequirePermission(JahezTreasuryPermissions.AccountsUpdate)]
    public Task<IActionResult> UpdateAccount(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, TreasuryAccountUpdate request, CancellationToken ct) =>
        Respond(service.UpdateTreasuryAccountAsync(key, id, request, ct));

    [HttpGet("balances"), RequirePermission(JahezTreasuryPermissions.Read)]
    public Task<IActionResult> Balances(CancellationToken ct) => Respond(service.TreasuryBalancesAsync(ct));

    [HttpGet("journal"), RequirePermission(JahezTreasuryPermissions.Read)]
    public Task<IActionResult> Journal([FromQuery] TreasuryFilter filter, CancellationToken ct) => Respond(service.TreasuryJournalAsync(filter, ct));

    [HttpPost("transfers"), RequirePermission(JahezTreasuryPermissions.TransfersCreate)]
    public Task<IActionResult> Transfer([FromHeader(Name = "Idempotency-Key")] string key, TreasuryTransferCreate request, CancellationToken ct) =>
        Respond(service.TransferTreasuryAsync(key, request, ct));

    // Service additionally checks EntriesCreate for business entries and Adjust for opening/adjustment entries.
    [HttpPost("cash-entries")]
    public Task<IActionResult> CashEntry([FromHeader(Name = "Idempotency-Key")] string key, TreasuryCashCreate request, CancellationToken ct) =>
        Respond(service.RecordTreasuryCashAsync(key, request, ct));

    [HttpPost("reversals"), RequirePermission(JahezTreasuryPermissions.Adjust)]
    public Task<IActionResult> Reverse([FromHeader(Name = "Idempotency-Key")] string key, TreasuryReverseCreate request, CancellationToken ct) =>
        Respond(service.ReverseTreasuryAsync(key, request, ct));

    [HttpGet("migration/readiness"), RequirePermission(JahezTreasuryPermissions.Adjust)]
    public Task<IActionResult> Readiness(CancellationToken ct) => Respond(service.TreasuryReadinessAsync(ct));

    [HttpGet("migration/attribution-review"), RequirePermission(JahezTreasuryPermissions.Adjust)]
    public Task<IActionResult> AttributionReview(int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        Respond(service.TreasuryAttributionReviewAsync(page, pageSize, ct));

    [HttpPost("migration/attributions"), RequirePermission(JahezTreasuryPermissions.Adjust)]
    public Task<IActionResult> Attribute([FromHeader(Name = "Idempotency-Key")] string key, TreasuryAttributionCreate request, CancellationToken ct) =>
        Respond(service.AttributeTreasuryAsync(key, request, ct));

    [HttpPost("migration/backfill"), RequirePermission(JahezTreasuryPermissions.Adjust)]
    public Task<IActionResult> Backfill([FromHeader(Name = "Idempotency-Key")] string key, TreasuryBackfillRequest request, CancellationToken ct) =>
        Respond(service.BackfillTreasuryAsync(key, request.Reason, ct));

    [HttpPost("migration/balance-verifications"), RequirePermission(JahezTreasuryPermissions.Adjust)]
    public Task<IActionResult> Verify([FromHeader(Name = "Idempotency-Key")] string key, TreasuryVerificationCreate request, CancellationToken ct) =>
        Respond(service.VerifyTreasuryBalanceAsync(key, request, ct));

    [HttpPost("migration/activate"), RequirePermission(JahezTreasuryPermissions.Adjust)]
    public Task<IActionResult> Activate([FromHeader(Name = "Idempotency-Key")] string key, TreasuryActivationCreate request, CancellationToken ct) =>
        Respond(service.ActivateTreasuryAsync(key, request, ct));

    [HttpGet("handovers/preview"), RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> PreviewHandover(DateOnly businessDate, CancellationToken ct) => Respond(service.PreviewTreasuryHandoverAsync(businessDate, ct));

    [HttpGet("handovers"), RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> Handovers(int page = 1, int pageSize = 50, CancellationToken ct = default) => Respond(service.TreasuryHandoversAsync(page, pageSize, ct));

    [HttpGet("handovers/{id:guid}"), RequirePermission(PermissionKeys.Jahez.CashboxRead)]
    public Task<IActionResult> Handover(Guid id, CancellationToken ct) => Respond(service.TreasuryHandoverAsync(id, ct));

    [HttpPost("handovers"), RequirePermission(PermissionKeys.Jahez.CashboxSubmit)]
    public Task<IActionResult> SubmitHandover([FromHeader(Name = "Idempotency-Key")] string key, JahezCashboxHandoverCreate request, CancellationToken ct) =>
        Respond(service.SubmitTreasuryHandoverAsync(key, request, ct));

    [HttpPost("handovers/{id:guid}/confirm"), RequirePermission(PermissionKeys.Jahez.CashboxConfirm)]
    public Task<IActionResult> ConfirmHandover(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, TreasuryHandoverConfirm request, CancellationToken ct) =>
        Respond(service.ConfirmTreasuryHandoverAsync(key, id, request, ct));

    [HttpPost("handovers/{id:guid}/cancel"), RequirePermission(PermissionKeys.Jahez.CashboxSubmit)]
    public Task<IActionResult> CancelHandover(Guid id, [FromHeader(Name = "Idempotency-Key")] string key, JahezCashboxHandoverCancel request, CancellationToken ct) =>
        Respond(service.CancelTreasuryHandoverAsync(key, id, request, ct));

    [HttpGet("petroapp/evidence"), RequirePermission(JahezTreasuryPermissions.Read)]
    public Task<IActionResult> Evidence([FromQuery] TreasuryFilter filter, CancellationToken ct) => Respond(service.PetroEvidenceAsync(filter, ct));

    [HttpPost("petroapp/evidence"), RequirePermission(JahezTreasuryPermissions.EvidenceCreate)]
    public Task<IActionResult> AddEvidence([FromHeader(Name = "Idempotency-Key")] string key, PetroEvidenceCreate request, CancellationToken ct) =>
        Respond(service.RecordPetroEvidenceAsync(key, request, ct));

    [HttpGet("reports/{type}"), RequirePermission(JahezTreasuryPermissions.ReportsRead)]
    public Task<IActionResult> Report(string type, [FromQuery] TreasuryFilter filter, Guid? handoverId, CancellationToken ct) =>
        Respond(service.TreasuryReportAsync(type, filter, handoverId, ct));

    [HttpGet("reports/{type}/export"), RequirePermission(JahezTreasuryPermissions.ReportsExport)]
    public async Task<IActionResult> Export(string type, string format, [FromQuery] TreasuryFilter filter, Guid? handoverId, CancellationToken ct)
    {
        var result = await service.ExportTreasuryReportAsync(type, format, filter, handoverId, ct);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.FileName) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> Respond<T>(Task<Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
