using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/compliance/expiries")]
public sealed class EmployeeExpiryComplianceController(IEmployeeExpiryComplianceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? checkDate,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? riderProfileId,
        [FromQuery] string? sourceType,
        [FromQuery] string? dueStatus,
        [FromQuery] string? employeeStatus,
        [FromQuery] Guid? operatingCityId,
        [FromQuery] Guid? sponsorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetExpiriesAsync(new EmployeeExpiryComplianceQuery(
            checkDate, employeeId, riderProfileId, sourceType, dueStatus, employeeStatus,
            operatingCityId, sponsorId, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

[ApiController]
[Route("api/employees/{employeeId:guid}/compliance-expiries")]
public sealed class EmployeeComplianceExpiriesController(IEmployeeExpiryComplianceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public async Task<IActionResult> Get(Guid employeeId, [FromQuery] DateOnly? checkDate, CancellationToken cancellationToken = default)
    {
        var result = await service.GetEmployeeExpiriesAsync(employeeId, checkDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
