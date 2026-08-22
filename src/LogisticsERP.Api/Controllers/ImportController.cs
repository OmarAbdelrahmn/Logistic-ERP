using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/import")]
[RequestSizeLimit(20 * 1024 * 1024)]
[AllowAnonymous]
public sealed class ImportController(IHrExcelImportService service) : ControllerBase
{
    [HttpPost("employees-riders/validate")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> Validate([FromForm] HrExcelImportForm request, CancellationToken cancellationToken) =>
        Execute(request.File, validateOnly: true, cancellationToken);

    [HttpPost("employees-riders")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> Import([FromForm] HrExcelImportForm request, CancellationToken cancellationToken) =>
        Execute(request.File, validateOnly: false, cancellationToken);

    private async Task<IActionResult> Execute(IFormFile? file, bool validateOnly, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024
            || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "A non-empty .xlsx file up to 20 MB is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await service.ImportAsync(stream, Path.GetFileName(file.FileName), validateOnly, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

public sealed class HrExcelImportForm
{
    public IFormFile File { get; init; } = null!;
}
