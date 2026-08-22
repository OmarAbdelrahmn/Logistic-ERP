using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Company;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/company-profile")]
public sealed class CompanyProfileController(ICompanyProfileService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Catalog.CompanyProfileRead)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut]
    [RequirePermission(PermissionKeys.Catalog.CompanyProfileManage)]
    public async Task<IActionResult> Update([FromBody] UpdateCompanyProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

