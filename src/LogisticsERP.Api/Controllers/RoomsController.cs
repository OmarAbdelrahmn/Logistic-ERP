using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(IHousingService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetRoomAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] HousingRoomUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpsertRoomAsync(null, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> Archive(
        Guid id,
        [FromBody] ArchiveHousingRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ArchiveRoomAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/occupants/employees")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> AssignEmployee(
        Guid id,
        [FromBody] AssignRoomEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AssignEmployeeToRoomAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/occupants/riders")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> AssignRider(
        Guid id,
        [FromBody] AssignRoomRiderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AssignRiderToRoomAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("occupants/{occupancyPeriodId:guid}/move")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> MoveOccupant(
        Guid occupancyPeriodId,
        [FromBody] MoveRoomOccupantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.MoveOccupantAsync(occupancyPeriodId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("occupants/{occupancyPeriodId:guid}/remove")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> RemoveOccupant(
        Guid occupancyPeriodId,
        [FromBody] RemoveRoomOccupantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.RemoveOccupantAsync(occupancyPeriodId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
