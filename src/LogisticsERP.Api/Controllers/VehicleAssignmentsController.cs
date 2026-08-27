using LogisticsERP.Api.ErrorHandling;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-assignments")]
public sealed class VehicleAssignmentsController(
    IFleetService service,
    ILogger<VehicleAssignmentsController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, string, Exception?> LogAssignmentFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1001, nameof(VehicleAssignmentsController)),
            "Vehicle assignment command failed. CorrelationId: {CorrelationId}");

    [HttpPost("take")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(32 * 1024 * 1024)]
    public async Task<IActionResult> Take([FromForm] VehicleAssignmentMultipartForm form, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var request = Deserialize<TakeVehicleRequest>(form.Metadata);
        if (request is null) return BadRequest();
        return await ExecuteAssignmentAsync(
            () => WithUploadsAsync(form.PromissoryFiles, uploads => service.TakeAsync(request, uploads, idempotencyKey ?? string.Empty, cancellationToken)),
            cancellationToken);
    }

    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] ReturnVehicleRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        return await ExecuteAssignmentAsync(async () =>
        {
            var result = await service.ReturnAsync(request, idempotencyKey ?? string.Empty, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
        }, cancellationToken);
    }

    [HttpPost("switch")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(32 * 1024 * 1024)]
    public async Task<IActionResult> Switch([FromForm] VehicleAssignmentMultipartForm form, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var request = Deserialize<SwitchVehicleRequest>(form.Metadata);
        if (request is null) return BadRequest();
        return await ExecuteAssignmentAsync(
            () => WithUploadsAsync(form.PromissoryFiles, uploads => service.SwitchAsync(request, uploads, idempotencyKey ?? string.Empty, cancellationToken)),
            cancellationToken);
    }

    [HttpPost("{assignmentId:guid}/renew-permission")]
    public async Task<IActionResult> Renew(Guid assignmentId, [FromBody] RenewVehiclePermissionRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        return await ExecuteAssignmentAsync(async () =>
        {
            var result = await service.RenewPermissionAsync(assignmentId, request, idempotencyKey ?? string.Empty, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
        }, cancellationToken);
    }

    private static T? Deserialize<T>(string metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return default;
        try { return JsonSerializer.Deserialize<T>(metadata, WebJsonOptions); }
        catch (JsonException) { return default; }
    }

    private async Task<IActionResult> WithUploadsAsync(List<IFormFile> files, Func<IReadOnlyList<PrivateFileUpload>, Task<Result<RiderVehicleAssignmentResponse>>> action)
    {
        var streams = new List<Stream>(files.Count);
        try
        {
            var uploads = files.Select(file =>
            {
                var stream = file.OpenReadStream(); streams.Add(stream);
                return new PrivateFileUpload(stream, file.FileName, file.ContentType, file.Length);
            }).ToArray();
            var result = await action(uploads);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
        }
        finally
        {
            foreach (var stream in streams) await stream.DisposeAsync();
        }
    }

    private async Task<IActionResult> ExecuteAssignmentAsync(Func<Task<IActionResult>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogAssignmentFailure(logger, HttpContext.TraceIdentifier, exception);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "fleet.assignment_unexpected_error",
                Detail = exception.Message,
                Type = "https://httpstatuses.io/500",
                Instance = HttpContext.Request.Path,
                Extensions =
                {
                    ["errorCode"] = "fleet.assignment_unexpected_error",
                    ["correlationId"] = HttpContext.TraceIdentifier,
                    ["exceptionType"] = exception.GetType().FullName,
                    ["exception"] = exception.ToString(),
                    ["innerException"] = exception.InnerException?.ToString()
                }
            };

            return StatusCode(StatusCodes.Status500InternalServerError, problem);
        }
    }
}

public sealed class VehicleAssignmentMultipartForm
{
    public string Metadata { get; init; } = string.Empty;
    public List<IFormFile> PromissoryFiles { get; init; } = [];
}

[ApiController]
[Route("api/riders/{riderProfileId:guid}/vehicle-timeline")]
public sealed class RiderVehicleTimelineController(IFleetService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid riderProfileId, CancellationToken cancellationToken)
    {
        var result = await service.GetRiderTimelineAsync(riderProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

[ApiController]
[Route("api/riders/{riderProfileId:guid}/promissory-files")]
public sealed class RiderPromissoryFilesController(IVehicleFileService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid riderProfileId, CancellationToken cancellationToken)
    {
        var result = await service.GetRiderPromissoryFilesAsync(riderProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{fileId:guid}/download")]
    public async Task<IActionResult> Download(Guid riderProfileId, Guid fileId, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadRiderPromissoryFileAsync(riderProfileId, fileId, versionId, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true) : result.ToProblem(HttpContext);
    }
}
