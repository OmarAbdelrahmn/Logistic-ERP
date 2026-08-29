using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/hr-form-templates")]
public sealed class HrFormTemplatesController(IHrFormTemplateService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.HrForms.TemplatesRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetAllAsync(search, category, activeOnly, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.HrForms.TemplatesRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("by-code/{code}")]
    [RequirePermission(PermissionKeys.HrForms.TemplatesRead)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        var result = await service.GetByCodeAsync(code, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.HrForms.TemplatesManage)]
    public async Task<IActionResult> Create(
        [FromBody] HrFormTemplateCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Template.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.HrForms.TemplatesManage)]
    public async Task<IActionResult> UpdateMetadata(
        Guid id,
        [FromBody] HrFormTemplateMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateMetadataAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/versions")]
    [RequirePermission(PermissionKeys.HrForms.TemplatesRead)]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetVersionsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/versions")]
    [RequirePermission(PermissionKeys.HrForms.TemplatesManage)]
    public async Task<IActionResult> CreateVersion(
        Guid id,
        [FromBody] HrFormTemplateVersionCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateVersionAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/publish")]
    [RequirePermission(PermissionKeys.HrForms.TemplatesManage)]
    public async Task<IActionResult> Publish(
        Guid id,
        Guid versionId,
        [FromBody] HrFormTemplatePublishRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.PublishAsync(id, versionId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.HrForms.TemplatesManage)]
    public async Task<IActionResult> Archive(
        Guid id,
        [FromBody] ArchiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
