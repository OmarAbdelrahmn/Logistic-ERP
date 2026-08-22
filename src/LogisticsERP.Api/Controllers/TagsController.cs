using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Tags;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/tags")]
public sealed class TagsController(ITagService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Catalog.TagsRead)]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) => ToAction(service.GetAllAsync(cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionKeys.Catalog.TagsManage)]
    public Task<IActionResult> Create([FromBody] TagUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertAsync(null, request, cancellationToken));

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Catalog.TagsManage)]
    public Task<IActionResult> Update(Guid id, [FromBody] TagUpsertRequest request, CancellationToken cancellationToken) =>
        ToAction(service.UpsertAsync(id, request, cancellationToken));

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Catalog.TagsManage)]
    public async Task<IActionResult> Archive(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(id, request.Reason, request.RowVersion, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("assignments/{resource}/{resourceId:guid}")]
    [RequirePermission(PermissionKeys.Catalog.TagsRead)]
    public Task<IActionResult> GetAssignments(string resource, Guid resourceId, CancellationToken cancellationToken) =>
        ToAction(service.GetAssignmentsAsync(resource, resourceId, cancellationToken));

    [HttpPut("assignments/{resource}/{resourceId:guid}")]
    [RequirePermission(PermissionKeys.Catalog.TagsManage)]
    public Task<IActionResult> ReplaceAssignments(
        string resource,
        Guid resourceId,
        [FromBody] ReplaceTagAssignmentsRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.ReplaceAssignmentsAsync(resource, resourceId, request, cancellationToken));

    private async Task<IActionResult> ToAction<T>(Task<LogisticsERP.Application.Common.Results.Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

