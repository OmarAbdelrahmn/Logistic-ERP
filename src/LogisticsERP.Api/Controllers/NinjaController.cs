using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/ninja")]
public sealed class NinjaController(INinjaService service) : ControllerBase
{
    [HttpPost("accounts/sheet-updates")]
    [RequirePermission(PermissionKeys.Ninja.AccountsUpdate)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadAccountSheet([FromForm] IFormFile file, [FromForm] string? note, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024)
            return Result.Failure(NinjaErrors.Invalid("الحد الأقصى للملف 10 ميجابايت.")).ToProblem(HttpContext);
        using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);
        return await ToAction(service.UploadAccountSheetAsync(file.FileName, content.ToArray(), note, ct));
    }

    [HttpGet("accounts/sheet-updates")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> AccountSheetUploads(int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        ToAction(service.GetAccountSheetUploadsAsync(page, pageSize, ct));

    [HttpGet("accounts/sheet-updates/{uploadId:guid}")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> AccountSheetUpload(Guid uploadId, CancellationToken ct) => ToAction(service.GetAccountSheetUploadAsync(uploadId, ct));

    [HttpGet("accounts/sheet-updates/{uploadId:guid}/rows")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> AccountSheetRows(Guid uploadId, string? matchStatus, string? sheetStatus, bool problemsOnly = false,
        int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        ToAction(service.GetAccountSheetRowsAsync(uploadId, matchStatus, sheetStatus, problemsOnly, page, pageSize, ct));

    [HttpPut("accounts/sheet-updates/{uploadId:guid}/note")]
    [RequirePermission(PermissionKeys.Ninja.AccountsUpdate)]
    public Task<IActionResult> AccountSheetNote(Guid uploadId, NinjaAccountUpdateNoteRequest request, CancellationToken ct) =>
        ToAction(service.UpdateAccountSheetNoteAsync(uploadId, request, ct));

    [HttpPost("accounts/compare")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> CompareAccounts([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024)
            return Result.Failure(NinjaErrors.Invalid("الحد الأقصى للملف 10 ميجابايت.")).ToProblem(HttpContext);
        using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);
        return await ToAction(service.CompareAccountsAsync(file.FileName, content.ToArray(), ct));
    }

    [HttpGet("accounts")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Accounts(int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetAccountsAsync(page, pageSize, ct));

    [HttpGet("accounts/{accountId:guid}")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Account(Guid accountId, CancellationToken ct) => ToAction(service.GetAccountAsync(accountId, ct));

    [HttpPut("accounts/{accountId:guid}/status")]
    [RequirePermission(PermissionKeys.Ninja.AccountsUpdate)]
    public Task<IActionResult> SetAccountStatus(Guid accountId, NinjaAccountStatusRequest request, CancellationToken ct) =>
        ToAction(service.SetAccountStatusAsync(accountId, request, ct));

    [HttpGet("reports/performance")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> PerformanceReport(DateOnly from, DateOnly to, Guid? accountId, Guid? riderId, CancellationToken ct) =>
        ToAction(service.GetPerformanceReportAsync(from, to, accountId, riderId, ct));

    [HttpGet("reports/daily")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> DailyReport(DateOnly from, DateOnly to, Guid? accountId, Guid? riderId,
        int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        ToAction(service.GetDailyReportAsync(from, to, accountId, riderId, page, pageSize, ct));

    [HttpGet("reports/riders")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> RiderReport(DateOnly from, DateOnly to, Guid? accountId, Guid? riderId,
        int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        ToAction(service.GetRiderReportAsync(from, to, accountId, riderId, page, pageSize, ct));

    [HttpPost("accounts/{accountId:guid}/assign")]
    [RequirePermission(PermissionKeys.Ninja.AssignmentsCreate)]
    public Task<IActionResult> Assign(Guid accountId, NinjaAssignRequest request, CancellationToken ct) => ToAction(service.AssignAsync(accountId, request, ct));

    [HttpPost("accounts/{accountId:guid}/return")]
    [RequirePermission(PermissionKeys.Ninja.AssignmentsDelete)]
    public Task<IActionResult> Return(Guid accountId, NinjaReturnRequest request, CancellationToken ct) => ToAction(service.ReturnAsync(accountId, request, ct));

    [HttpPut("accounts/{accountId:guid}/credentials")]
    [RequirePermission(PermissionKeys.Ninja.AccountsUpdate)]
    public Task<IActionResult> Credentials(Guid accountId, NinjaCredentialRequest request, CancellationToken ct) => ToAction(service.UpdateCredentialsAsync(accountId, request, ct));

    [HttpPut("accounts/{accountId:guid}/assignments/{assignmentId:guid}/dates")]
    [RequirePermission(PermissionKeys.Ninja.AssignmentDatesUpdate)]
    public Task<IActionResult> UpdateAssignmentDates(Guid accountId, Guid assignmentId, NinjaAssignmentDatesRequest request, CancellationToken ct) =>
        ToAction(service.UpdateAssignmentDatesAsync(accountId, assignmentId, request, ct));

    [HttpPost("performance/import")]
    [RequirePermission(PermissionKeys.Ninja.PerformanceImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public Task<IActionResult> ImportPerformance([FromForm] DateOnly reportDate, [FromForm] IFormFile file, CancellationToken ct) => Import(false, reportDate, file, ct);

    [HttpPost("deductions/import")]
    [RequirePermission(PermissionKeys.Ninja.DeductionsImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public Task<IActionResult> ImportDeductions([FromForm] DateOnly reportDate, [FromForm] IFormFile file, CancellationToken ct) => Import(true, reportDate, file, ct);

    [HttpGet("performance")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> Performance(DateOnly from, DateOnly to, Guid? accountId, Guid? riderId, int page = 1, int pageSize = 50,
        CancellationToken ct = default) => ToAction(service.GetPerformanceAsync(from, to, accountId, riderId, page, pageSize, ct));

    [HttpGet("deductions")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> Deductions(DateOnly from, DateOnly to, Guid? accountId, Guid? riderId, int page = 1, int pageSize = 50,
        CancellationToken ct = default) => ToAction(service.GetDeductionsAsync(from, to, accountId, riderId, page, pageSize, ct));

    private async Task<IActionResult> Import(bool deductions, DateOnly date, IFormFile file, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024)
            return Result.Failure(NinjaErrors.Invalid("الحد الأقصى للملف 10 ميجابايت.")).ToProblem(HttpContext);
        using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);
        return await ToAction(service.ImportAsync(deductions, date, file.FileName, content.ToArray(), ct));
    }
    private async Task<IActionResult> ToAction<T>(Task<Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
