using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/hr-catalogs")]
public sealed class HrCatalogsController(IHrCatalogService service) : ControllerBase
{
    [HttpGet("global-cities")]
    [RequirePermission(PermissionKeys.Catalog.OperatingCitiesRead)]
    public Task<IActionResult> GlobalCities(CancellationToken cancellationToken) => ToAction(service.GetGlobalCitiesAsync(cancellationToken));

    [HttpPost("global-cities")]
    [RequirePermission(PermissionKeys.Catalog.OperatingCitiesManage)]
    public Task<IActionResult> CreateGlobalCity([FromBody] GlobalCityUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertGlobalCityAsync(null, request, cancellationToken));

    [HttpPut("global-cities/{id:guid}")]
    [RequirePermission(PermissionKeys.Catalog.OperatingCitiesManage)]
    public Task<IActionResult> UpdateGlobalCity(Guid id, [FromBody] GlobalCityUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertGlobalCityAsync(id, request, cancellationToken));

    [HttpGet("job-titles")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public Task<IActionResult> JobTitles(CancellationToken cancellationToken) => ToAction(service.GetJobTitlesAsync(cancellationToken));

    [HttpPost("job-titles")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public Task<IActionResult> CreateJobTitle([FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertJobTitleAsync(null, request, cancellationToken));

    [HttpPut("job-titles/{id:guid}")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public Task<IActionResult> UpdateJobTitle(Guid id, [FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertJobTitleAsync(id, request, cancellationToken));

    [HttpPut("job-titles/{id:guid}/operational-work-types")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public async Task<IActionResult> SetWorkTypes(Guid id, [FromBody] SetJobTitleWorkTypesRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SetJobTitleWorkTypesAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("residency-professions")]
    [RequirePermission(PermissionKeys.Compliance.ResidencyRead)]
    public Task<IActionResult> ResidencyProfessions(CancellationToken cancellationToken) => ToAction(service.GetResidencyProfessionsAsync(cancellationToken));

    [HttpPost("residency-professions")]
    [RequirePermission(PermissionKeys.Compliance.ResidencyManage)]
    public Task<IActionResult> CreateResidencyProfession([FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertResidencyProfessionAsync(null, request, cancellationToken));

    [HttpPut("residency-professions/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.ResidencyManage)]
    public Task<IActionResult> UpdateResidencyProfession(Guid id, [FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertResidencyProfessionAsync(id, request, cancellationToken));

    [HttpGet("operational-work-types")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public Task<IActionResult> OperationalWorkTypes(CancellationToken cancellationToken) => ToAction(service.GetOperationalWorkTypesAsync(cancellationToken));

    [HttpPost("operational-work-types")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public Task<IActionResult> CreateOperationalWorkType([FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertOperationalWorkTypeAsync(null, request, cancellationToken));

    [HttpPut("operational-work-types/{id:guid}")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public Task<IActionResult> UpdateOperationalWorkType(Guid id, [FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertOperationalWorkTypeAsync(id, request, cancellationToken));

    [HttpGet("driver-license-categories")]
    [RequirePermission(PermissionKeys.Compliance.LicensesRead)]
    public Task<IActionResult> DriverLicenseCategories(CancellationToken cancellationToken) => ToAction(service.GetDriverLicenseCategoriesAsync(cancellationToken));

    [HttpPost("driver-license-categories")]
    [RequirePermission(PermissionKeys.Compliance.LicensesManage)]
    public Task<IActionResult> CreateDriverLicenseCategory([FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertDriverLicenseCategoryAsync(null, request, cancellationToken));

    [HttpPut("driver-license-categories/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.LicensesManage)]
    public Task<IActionResult> UpdateDriverLicenseCategory(Guid id, [FromBody] CatalogUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertDriverLicenseCategoryAsync(id, request, cancellationToken));

    [HttpGet("document-types")]
    [RequirePermission(PermissionKeys.Documents.Read)]
    public Task<IActionResult> DocumentTypes(CancellationToken cancellationToken) => ToAction(service.GetDocumentTypesAsync(cancellationToken));

    [HttpPost("document-types")]
    [RequirePermission(PermissionKeys.Documents.CatalogManage)]
    public Task<IActionResult> CreateDocumentType([FromBody] DocumentTypeUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertDocumentTypeAsync(null, request, cancellationToken));

    [HttpPut("document-types/{id:guid}")]
    [RequirePermission(PermissionKeys.Documents.CatalogManage)]
    public Task<IActionResult> UpdateDocumentType(Guid id, [FromBody] DocumentTypeUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertDocumentTypeAsync(id, request, cancellationToken));

    [HttpGet("document-requirements")]
    [RequirePermission(PermissionKeys.Documents.Read)]
    public Task<IActionResult> DocumentRequirements([FromQuery] Guid? documentTypeId, CancellationToken cancellationToken) =>
        ToAction(service.GetDocumentRequirementsAsync(documentTypeId, cancellationToken));

    [HttpPost("document-requirements")]
    [RequirePermission(PermissionKeys.Documents.CatalogManage)]
    public Task<IActionResult> CreateDocumentRequirement([FromBody] DocumentRequirementUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertDocumentRequirementAsync(null, request, cancellationToken));

    [HttpPut("document-requirements/{id:guid}")]
    [RequirePermission(PermissionKeys.Documents.CatalogManage)]
    public Task<IActionResult> UpdateDocumentRequirement(Guid id, [FromBody] DocumentRequirementUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertDocumentRequirementAsync(id, request, cancellationToken));

    [HttpGet("operating-cities")]
    [RequirePermission(PermissionKeys.Catalog.OperatingCitiesRead)]
    public Task<IActionResult> OperatingCities(CancellationToken cancellationToken) => ToAction(service.GetOperatingCitiesAsync(cancellationToken));

    [HttpPost("operating-cities")]
    [RequirePermission(PermissionKeys.Catalog.OperatingCitiesManage)]
    public Task<IActionResult> CreateOperatingCity([FromBody] OperatingCityUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertOperatingCityAsync(null, request, cancellationToken));

    [HttpPut("operating-cities/{id:guid}")]
    [RequirePermission(PermissionKeys.Catalog.OperatingCitiesManage)]
    public Task<IActionResult> UpdateOperatingCity(Guid id, [FromBody] OperatingCityUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertOperatingCityAsync(id, request, cancellationToken));

    private async Task<IActionResult> ToAction<T>(Task<LogisticsERP.Application.Common.Results.Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
