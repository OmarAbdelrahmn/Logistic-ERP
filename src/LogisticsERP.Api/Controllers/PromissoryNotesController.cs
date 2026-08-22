using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/promissory-notes")]
public sealed class PromissoryNotesController(IComplianceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Compliance.PromissoryNotesRead)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetPromissoryNotesAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("employee/{employeeId:guid}")]
    [RequirePermission(PermissionKeys.Compliance.PromissoryNotesManage)]
    public Task<IActionResult> Create(Guid employeeId, [FromBody] PromissoryNoteUpsertRequest request, CancellationToken cancellationToken) => Upsert(employeeId, null, request, cancellationToken);

    [HttpPut("employee/{employeeId:guid}/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.PromissoryNotesManage)]
    public Task<IActionResult> Update(Guid employeeId, Guid id, [FromBody] PromissoryNoteUpsertRequest request, CancellationToken cancellationToken) => Upsert(employeeId, id, request, cancellationToken);

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Compliance.PromissoryNotesManage)]
    public async Task<IActionResult> Archive(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync("promissory-note", id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> Upsert(Guid employeeId, Guid? id, PromissoryNoteUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertPromissoryNoteAsync(employeeId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
