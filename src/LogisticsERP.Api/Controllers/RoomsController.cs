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

    [HttpPost("{id:guid}/occupants/iqama")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> AssignByIqama(Guid id, [FromBody] AssignRoomByIqamaRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AssignByIqamaToRoomAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/equipment")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CreateEquipment(Guid id, [FromBody] HousingEquipmentUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertEquipmentAsync(null, id, null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}/equipment/{equipmentId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> UpdateEquipment(Guid id, Guid equipmentId, [FromBody] HousingEquipmentUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertEquipmentAsync(null, id, equipmentId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/occupants/external")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CreateExternalOccupant(Guid id, [FromBody] HousingExternalOccupantUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertExternalOccupantAsync(null, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("occupants/external/{occupantId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> UpdateExternalOccupant(Guid occupantId, [FromBody] HousingExternalOccupantUpsertRequest request, CancellationToken cancellationToken)
    {
        if (request.RoomId is not { } roomId) return BadRequest("roomId is required.");
        var result = await service.UpsertExternalOccupantAsync(occupantId, roomId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("occupants/external/{occupantId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> DeleteExternalOccupant(Guid occupantId, CancellationToken cancellationToken)
    {
        var result = await service.DeleteExternalOccupantAsync(occupantId, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPost("occupants/pending/{pendingId:guid}/resolve")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> ResolvePendingOccupant(Guid pendingId, [FromBody] ResolvePendingOccupantRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ResolvePendingOccupantAsync(pendingId, request.EffectiveFrom, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("occupants/pending/{pendingId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> DeletePendingOccupant(Guid pendingId, CancellationToken cancellationToken)
    {
        var result = await service.DeletePendingOccupantAsync(pendingId, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
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
