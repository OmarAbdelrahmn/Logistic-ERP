using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/chefz")]
public sealed class ChefzController(IChefzService service) : ControllerBase
{
    [HttpPut("assignments/{assignmentId:guid}/start-date")]
    [RequirePermission(PermissionKeys.Chefz.AccountsUpdate)]
    public Task<IActionResult> UpdateAssignmentStartDate(Guid assignmentId, ChefzAssignmentStartDateRequest request, CancellationToken ct) =>
        ToAction(service.UpdateAssignmentStartDateAsync(assignmentId, request, ct));

    [HttpPut("accounts/{accountId:guid}/status")]
    [RequirePermission(PermissionKeys.Chefz.AccountsUpdate)]
    public Task<IActionResult> SetAccountStatus(Guid accountId, ChefzAccountStatusRequest request, CancellationToken ct) =>
        ToAction(service.SetAccountStatusAsync(accountId, request, ct));

    [HttpGet("settings/performance")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Settings(CancellationToken ct) => ToAction(service.GetSettingsAsync(ct));

    [HttpPut("settings/performance")]
    [RequirePermission(PermissionKeys.Chefz.AccountsUpdate)]
    public Task<IActionResult> SaveSettings(ChefzSettingsRequest request, CancellationToken ct) => ToAction(service.SaveSettingsAsync(request, ct));

    [HttpGet("sponsor-statistics")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Statistics(int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetStatisticsAsync(page, pageSize, ct));

    [HttpGet("sponsor-statistics/{sponsorId:guid}")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> StatisticsBySponsor(Guid sponsorId, CancellationToken ct) => ToAction(service.GetStatisticsAsync(sponsorId, ct));

    [HttpPost("sponsor-statistics/{sponsorId:guid}")]
    [RequirePermission(PermissionKeys.Chefz.AccountsUpdate)]
    public Task<IActionResult> CreateStatistics(Guid sponsorId, ChefzStatisticsRequest request, CancellationToken ct) => ToAction(service.CreateStatisticsAsync(sponsorId, request, ct));

    [HttpPut("sponsor-statistics/{sponsorId:guid}")]
    [RequirePermission(PermissionKeys.Chefz.AccountsUpdate)]
    public Task<IActionResult> UpdateStatistics(Guid sponsorId, ChefzStatisticsRequest request, CancellationToken ct) => ToAction(service.UpdateStatisticsAsync(sponsorId, request, ct));

    [HttpDelete("sponsor-statistics/{sponsorId:guid}")]
    [RequirePermission(PermissionKeys.Chefz.AccountsUpdate)]
    public async Task<IActionResult> DeleteStatistics(Guid sponsorId, [FromQuery] string rowVersion, CancellationToken ct)
    {
        var result = await service.DeleteStatisticsAsync(sponsorId, rowVersion, ct);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("reports/riders")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> RiderReport(int year, int month, int half, int halves, Guid? accountId, Guid? riderId,
        CancellationToken ct) => ToAction(service.GetRiderReportAsync(accountId, riderId, year, month, half, halves, ct));

    [HttpGet("reports/management")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> ManagementReport(DateOnly reportDate, Guid? sponsorId, CancellationToken ct) => ToAction(service.GetManagementReportAsync(reportDate, sponsorId, ct));

    [HttpGet("reports/riders/all")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> AllRidersReport(int year, int month, int half, int halves = 1,
        CancellationToken ct = default) => ToAction(service.GetAllRidersReportAsync(year, month, half, halves, ct));

    [HttpGet("reminders")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Reminders(int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetRemindersAsync(page, pageSize, ct));

    [HttpGet("accounts")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Accounts(int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetAccountsAsync(page, pageSize, ct));

    [HttpGet("accounts/{accountId:guid}")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Account(Guid accountId, CancellationToken ct) => ToAction(service.GetAccountAsync(accountId, ct));

    [HttpPut("accounts/{accountId:guid}")]
    [RequirePermission(PermissionKeys.Chefz.AccountsUpdate)]
    public Task<IActionResult> SaveAccount(Guid accountId, ChefzAccountDetailsRequest request, CancellationToken ct) => ToAction(service.SaveAccountAsync(accountId, request, ct));

    [HttpPut("accounts/{accountId:guid}/password")]
    [RequirePermission(PermissionKeys.Operations.PlatformCredentialsRotate)]
    public async Task<IActionResult> SetPassword(Guid accountId, ChefzPasswordRequest request, CancellationToken ct)
    {
        var result = await service.SetPasswordAsync(accountId, request, ct);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("accounts/{accountId:guid}/password")]
    [RequirePermission(PermissionKeys.Operations.PlatformCredentialsRead)]
    [Produces("application/json")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Password(Guid accountId, CancellationToken ct) => ToAction(service.GetPasswordAsync(accountId, ct));

    [HttpPost("settlements/preview")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Preview(ChefzSettlementRequest request, CancellationToken ct) => ToAction(service.PreviewSettlementAsync(request, ct));

    [HttpPost("settlements")]
    [RequirePermission(PermissionKeys.Chefz.SettlementsCreate)]
    public Task<IActionResult> SaveSettlement(ChefzSettlementRequest request, CancellationToken ct) => ToAction(service.SaveSettlementAsync(request, ct));

    [HttpGet("settlements")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Settlements(Guid? accountId, Guid? riderId, int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetSettlementsAsync(accountId, riderId, page, pageSize, ct));

    [HttpPost("performance/import")]
    [RequirePermission(PermissionKeys.Chefz.PerformanceImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromForm] DateOnly reportDate, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024)
            return Result.Failure(ChefzErrors.Invalid("الحد الأقصى للملف 10 ميجابايت.")).ToProblem(HttpContext);
        using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);
        return await ToAction(service.ImportPerformanceAsync(reportDate, file.FileName, content.ToArray(), ct));
    }

    [HttpPost("settlements/orders/preview")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public Task<IActionResult> PreviewOrderSettlements([FromForm] IFormFile file, CancellationToken ct) => ProcessOrderFile(file, true, ct);

    [HttpPost("settlements/orders/import")]
    [RequirePermission(PermissionKeys.Chefz.SettlementsCreate)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public Task<IActionResult> ImportOrderSettlements([FromForm] IFormFile file, CancellationToken ct) => ProcessOrderFile(file, false, ct);

    [HttpGet("settlements/orders/imports/{importId:guid}")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> OrderSettlementImport(Guid importId, CancellationToken ct) => ToAction(service.GetOrderSettlementImportAsync(importId, ct));

    private async Task<IActionResult> ProcessOrderFile(IFormFile file, bool preview, CancellationToken ct)
    {
        if (file.Length is 0 or > 10 * 1024 * 1024)
            return Result.Failure(ChefzErrors.Invalid("الحد الأقصى للملف 10 ميجابايت.")).ToProblem(HttpContext);
        using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);
        return await ToAction(service.ProcessOrderSettlementsAsync(file.FileName, content.ToArray(), preview, ct));
    }

    [HttpGet("performance")]
    [RequirePermission(PermissionKeys.Chefz.Read)]
    public Task<IActionResult> Performance(DateOnly from, DateOnly to, Guid? accountId, Guid? riderId,
        int page = 1, int pageSize = 50, CancellationToken ct = default) => ToAction(service.GetPerformanceAsync(accountId, riderId, from, to, page, pageSize, ct));

    private async Task<IActionResult> ToAction<T>(Task<Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
