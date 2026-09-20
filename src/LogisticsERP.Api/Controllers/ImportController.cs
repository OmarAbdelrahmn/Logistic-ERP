using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/import")]
[RequestSizeLimit(20 * 1024 * 1024)]

public sealed class ImportController(
    IHrExcelImportService service,
    IVehicleImportValidationService vehicleValidationService,
    IVehicleImportService vehicleImportService,
    IVehiclePurchaseSupplierImportService vehiclePurchaseSupplierImportService,
    IVehicleRiderAssignmentImportService vehicleRiderAssignmentImportService) : ControllerBase
{
    [HttpPost("employees-riders/validate")]
    [Consumes("multipart/form-data")]
    //[RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    [AllowAnonymous]
    public Task<IActionResult> Validate([FromForm] HrExcelImportForm request, CancellationToken cancellationToken) =>
        Execute(request.File, validateOnly: true, cancellationToken);

    [HttpPost("employees-riders")]
    [Consumes("multipart/form-data")]
    //[RequirePermission(PermissionKeys.Workforce.EmployeesCreate)]
    //[RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    [AllowAnonymous]
    public Task<IActionResult> Import([FromForm] HrExcelImportForm request, CancellationToken cancellationToken) =>
        Execute(request.File, validateOnly: false, cancellationToken);

    [HttpPost("employees-riders/phone-numbers/validate")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ValidatePhoneNumbers(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecutePhoneNumberUpdate(request.File, validateOnly: true, cancellationToken);

    [HttpPost("employees-riders/phone-numbers")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> UpdatePhoneNumbers(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecutePhoneNumberUpdate(request.File, validateOnly: false, cancellationToken);

    [HttpPost("employees-riders/statuses/validate")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ValidateStatuses(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecuteStatusUpdate(request.File, validateOnly: true, cancellationToken);

    [HttpPost("employees-riders/statuses")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> UpdateStatuses(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecuteStatusUpdate(request.File, validateOnly: false, cancellationToken);

    [HttpPost("vehicles/validate")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ValidateVehicles(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecuteVehicleImport(request.File, vehicleValidationService.ValidateAsync, cancellationToken);

    [HttpPost("vehicles")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ImportVehicles(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecuteVehicleImport(request.File, vehicleImportService.ImportAsync, cancellationToken);

    [HttpPost("vehicles/purchase-suppliers/validate")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ValidatePurchaseSuppliers(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecutePurchaseSupplierImport(request.File, validateOnly: true, cancellationToken);

    [HttpPost("vehicles/purchase-suppliers")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ImportPurchaseSuppliers(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecutePurchaseSupplierImport(request.File, validateOnly: false, cancellationToken);

    [HttpPost("vehicle-rider-assignments/validate")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ValidateVehicleRiderAssignments(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecuteVehicleRiderAssignmentImport(request.File, validateOnly: true, cancellationToken);

    [HttpPost("vehicle-rider-assignments")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public Task<IActionResult> ImportVehicleRiderAssignments(
        [FromForm] HrExcelImportForm request,
        CancellationToken cancellationToken) =>
        ExecuteVehicleRiderAssignmentImport(request.File, validateOnly: false, cancellationToken);

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

    private async Task<IActionResult> ExecutePhoneNumberUpdate(
        IFormFile? file,
        bool validateOnly,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024
            || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "A non-empty .xlsx file up to 20 MB is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await service.UpdatePhoneNumbersAsync(
            stream,
            Path.GetFileName(file.FileName),
            validateOnly,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> ExecuteStatusUpdate(
        IFormFile? file,
        bool validateOnly,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024
            || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "A non-empty .xlsx file up to 20 MB is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await service.UpdateStatusesAsync(
            stream,
            Path.GetFileName(file.FileName),
            validateOnly,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> ExecuteVehicleImport(
        IFormFile? file,
        Func<Stream, string, CancellationToken, Task<Result<VehicleImportResponse>>> action,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024
            || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "A non-empty .xlsx file up to 20 MB is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await action(stream, Path.GetFileName(file.FileName), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> ExecutePurchaseSupplierImport(
        IFormFile? file,
        bool validateOnly,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024
            || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "A non-empty .xlsx file up to 20 MB is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await vehiclePurchaseSupplierImportService.ImportAsync(
            stream,
            Path.GetFileName(file.FileName),
            validateOnly,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> ExecuteVehicleRiderAssignmentImport(
        IFormFile? file,
        bool validateOnly,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024
            || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "A non-empty .xlsx file up to 20 MB is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await vehicleRiderAssignmentImportService.ImportAsync(
            stream,
            Path.GetFileName(file.FileName),
            validateOnly,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

public sealed class HrExcelImportForm
{
    public IFormFile File { get; init; } = null!;
}
