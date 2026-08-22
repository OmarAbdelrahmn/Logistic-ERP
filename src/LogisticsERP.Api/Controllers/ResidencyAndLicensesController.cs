using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/compliance")]
public sealed class ResidencyAndLicensesController(IComplianceService service) : ControllerBase
{
    [HttpGet("residency-permits")]
    [RequirePermission(PermissionKeys.Compliance.ResidencyRead)]
    public async Task<IActionResult> GetResidency([FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetResidencyPermitsAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("employees/{employeeId:guid}/residency-permits")]
    [RequirePermission(PermissionKeys.Compliance.ResidencyManage)]
    public Task<IActionResult> CreateResidency(Guid employeeId, [FromBody] ResidencyPermitUpsertRequest request, CancellationToken cancellationToken) => UpsertResidency(employeeId, null, request, cancellationToken);

    [HttpPut("employees/{employeeId:guid}/residency-permits/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.ResidencyManage)]
    public Task<IActionResult> UpdateResidency(Guid employeeId, Guid id, [FromBody] ResidencyPermitUpsertRequest request, CancellationToken cancellationToken) => UpsertResidency(employeeId, id, request, cancellationToken);

    [HttpGet("driver-licenses")]
    [RequirePermission(PermissionKeys.Compliance.LicensesRead)]
    public async Task<IActionResult> GetLicenses([FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetDriverLicensesAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("employees/{employeeId:guid}/driver-licenses")]
    [RequirePermission(PermissionKeys.Compliance.LicensesManage)]
    public Task<IActionResult> CreateLicense(Guid employeeId, [FromBody] DriverLicenseUpsertRequest request, CancellationToken cancellationToken) => UpsertLicense(employeeId, null, request, cancellationToken);

    [HttpPut("employees/{employeeId:guid}/driver-licenses/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.LicensesManage)]
    public Task<IActionResult> UpdateLicense(Guid employeeId, Guid id, [FromBody] DriverLicenseUpsertRequest request, CancellationToken cancellationToken) => UpsertLicense(employeeId, id, request, cancellationToken);

    [HttpPatch("residency-permits/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.ResidencyManage)]
    public Task<IActionResult> ArchiveResidency(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken) =>
        Archive("residency", id, request, cancellationToken);

    [HttpPatch("driver-licenses/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.LicensesManage)]
    public Task<IActionResult> ArchiveLicense(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken) =>
        Archive("license", id, request, cancellationToken);

    private async Task<IActionResult> Archive(string resource, Guid id, ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(resource, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> UpsertResidency(Guid employeeId, Guid? id, ResidencyPermitUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertResidencyPermitAsync(employeeId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> UpsertLicense(Guid employeeId, Guid? id, DriverLicenseUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertDriverLicenseAsync(employeeId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
