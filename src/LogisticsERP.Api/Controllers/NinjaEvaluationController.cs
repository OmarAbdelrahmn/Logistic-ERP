using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/ninja")]
public sealed class NinjaEvaluationController(INinjaEvaluationService evaluation, INinjaPerformanceImportService imports) : ControllerBase
{
    [HttpGet("evaluation")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> Evaluate([FromQuery] NinjaEvaluationQuery query, CancellationToken ct) => Respond(evaluation.EvaluateAsync(query, ct));

    [HttpGet("evaluation/accounts/{id:guid}/days/{date}")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> Day(Guid id, DateOnly date, CancellationToken ct) => Respond(evaluation.DayAsync(id, date, ct));

    [HttpGet("evaluation/policy")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> Policy(CancellationToken ct) => Respond(evaluation.PolicyAsync(ct));

    [HttpGet("evaluation/accounts/{id:guid}/context")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> Context(Guid id, CancellationToken ct) => Respond(evaluation.ContextAsync(id, ct));

    [HttpPut("evaluation/accounts/{id:guid}/context")]
    [RequirePermission(PermissionKeys.Ninja.EvaluationManage)]
    public Task<IActionResult> SetContext(Guid id, NinjaEvaluationContextRequest request, CancellationToken ct) => Respond(evaluation.SetContextAsync(id, request, ct));

    [HttpGet("evaluation/accounts/{id:guid}/responsibility")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> Responsibility(Guid id, int year, int month, CancellationToken ct) => Respond(evaluation.ResponsibilityAsync(id, year, month, ct));

    [HttpPut("evaluation/accounts/{id:guid}/responsibility")]
    [RequirePermission(PermissionKeys.Ninja.EvaluationManage)]
    public Task<IActionResult> SetResponsibility(Guid id, NinjaResponsibilityRequest request, CancellationToken ct) => Respond(evaluation.SetResponsibilityAsync(id, request, ct));

    [HttpGet("evaluation/export")]
    [RequirePermission(PermissionKeys.Ninja.EvaluationExport)]
    public Task<IActionResult> Export([FromQuery] NinjaEvaluationQuery query, CancellationToken ct) => Download(evaluation.ExportAsync(query, ct));

    [HttpPost("evaluation/snapshots")]
    [RequirePermission(PermissionKeys.Ninja.EvaluationManage)]
    public Task<IActionResult> Snapshot(NinjaSnapshotRequest request, CancellationToken ct) => Respond(evaluation.SnapshotAsync(request, ct));

    [HttpGet("evaluation/snapshots/{id:guid}")]
    [RequirePermission(PermissionKeys.Ninja.Read)]
    public Task<IActionResult> GetSnapshot(Guid id, CancellationToken ct) => Respond(evaluation.GetSnapshotAsync(id, ct));

    [HttpGet("performance/imports/template")]
    [RequirePermission(PermissionKeys.Ninja.PerformanceImport)]
    public Task<IActionResult> Template(string mode = "Daily", CancellationToken ct = default) => Download(imports.TemplateAsync(mode, ct));

    [HttpPost("performance/imports/preview")]
    [RequirePermission(PermissionKeys.Ninja.PerformanceImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Preview([FromForm] IFormFile file, [FromForm] NinjaPerformanceImportOptions options, CancellationToken ct)
    {
        if (file.Length is <= 0 or > 10 * 1024 * 1024) return Result.Failure(NinjaErrors.Invalid("الحد 10 ميجابايت.")).ToProblem(HttpContext);
        using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
        return await Respond(imports.PreviewAsync(file.FileName, stream.ToArray(), options, ct));
    }
    [HttpPost("performance/imports/{id:guid}/confirm")]
    [RequirePermission(PermissionKeys.Ninja.PerformanceImport)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Confirm(Guid id, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > 10 * 1024 * 1024) return Result.Failure(NinjaErrors.Invalid("الحد 10 ميجابايت.")).ToProblem(HttpContext);
        using var stream = new MemoryStream(); await file.CopyToAsync(stream, ct);
        return await Respond(imports.ConfirmAsync(id, file.FileName, stream.ToArray(), ct));
    }
    private async Task<IActionResult> Respond<T>(Task<Result<T>> task)
    {
        var result = await task; return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
    private async Task<IActionResult> Download(Task<Result<NinjaEvaluationFile>> task)
    {
        var result = await task; return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.FileName) : result.ToProblem(HttpContext);
    }
}
