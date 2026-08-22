using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/insurance")]
public sealed class InsuranceController(IComplianceService service) : ControllerBase
{
    [HttpGet("companies")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceRead)]
    public async Task<IActionResult> Companies(CancellationToken cancellationToken)
    {
        var result = await service.GetInsuranceCompaniesAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("companies")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> CreateCompany([FromBody] InsuranceCompanyUpsertRequest request, CancellationToken cancellationToken) => UpsertCompany(null, request, cancellationToken);

    [HttpPut("companies/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> UpdateCompany(Guid id, [FromBody] InsuranceCompanyUpsertRequest request, CancellationToken cancellationToken) => UpsertCompany(id, request, cancellationToken);

    [HttpGet("companies/{companyId:guid}/plans")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceRead)]
    public async Task<IActionResult> Plans(Guid companyId, CancellationToken cancellationToken)
    {
        var result = await service.GetInsurancePlansAsync(companyId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("companies/{companyId:guid}/plans")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> CreatePlan(Guid companyId, [FromBody] InsurancePlanUpsertRequest request, CancellationToken cancellationToken) => UpsertPlan(companyId, null, request, cancellationToken);

    [HttpPut("companies/{companyId:guid}/plans/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> UpdatePlan(Guid companyId, Guid id, [FromBody] InsurancePlanUpsertRequest request, CancellationToken cancellationToken) => UpsertPlan(companyId, id, request, cancellationToken);

    [HttpGet("policies")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceRead)]
    public async Task<IActionResult> Policies([FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetMedicalInsurancePoliciesAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("employees/{employeeId:guid}/policies")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> CreatePolicy(Guid employeeId, [FromBody] MedicalInsurancePolicyUpsertRequest request, CancellationToken cancellationToken) => UpsertPolicy(employeeId, null, request, cancellationToken);

    [HttpPut("employees/{employeeId:guid}/policies/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> UpdatePolicy(Guid employeeId, Guid id, [FromBody] MedicalInsurancePolicyUpsertRequest request, CancellationToken cancellationToken) => UpsertPolicy(employeeId, id, request, cancellationToken);

    [HttpPatch("companies/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> ArchiveCompany(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken) => Archive("insurance-company", id, request, cancellationToken);

    [HttpPatch("plans/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> ArchivePlan(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken) => Archive("insurance-plan", id, request, cancellationToken);

    [HttpPatch("policies/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.InsuranceManage)]
    public Task<IActionResult> ArchivePolicy(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken) => Archive("medical-policy", id, request, cancellationToken);

    private async Task<IActionResult> UpsertCompany(Guid? id, InsuranceCompanyUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertInsuranceCompanyAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
    private async Task<IActionResult> UpsertPlan(Guid companyId, Guid? id, InsurancePlanUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertInsurancePlanAsync(companyId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
    private async Task<IActionResult> UpsertPolicy(Guid employeeId, Guid? id, MedicalInsurancePolicyUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertMedicalInsurancePolicyAsync(employeeId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
    private async Task<IActionResult> Archive(string resource, Guid id, ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(resource, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
