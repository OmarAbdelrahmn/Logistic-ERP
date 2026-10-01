using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/housing")]
public sealed class HousingController(IHousingService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("stay-report")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> GetStayReport(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? housingId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetStayReportAsync(
            new HousingStayReportRequest(fromDate, toDate, housingId, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public Task<IActionResult> Create([FromBody] HousingUpsertRequest request, CancellationToken cancellationToken) => Upsert(null, request, cancellationToken);

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public Task<IActionResult> Update(Guid id, [FromBody] HousingUpsertRequest request, CancellationToken cancellationToken) => Upsert(id, request, cancellationToken);

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> Archive(Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/rooms")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> GetRooms(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetRoomsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/rooms")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CreateRoom(
        Guid id,
        [FromBody] HousingRoomUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpsertRoomAsync(id, null, request, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/rooms/{result.Value!.Id}", result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/floors")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> GetFloors(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetFloorsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/floors")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CreateFloor(Guid id, [FromBody] HousingFloorUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertFloorAsync(id, null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}/floors/{floorId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> UpdateFloor(Guid id, Guid floorId, [FromBody] HousingFloorUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertFloorAsync(id, floorId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("floors/{floorId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> DeleteFloor(Guid floorId, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.DeleteFloorAsync(floorId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPost("floors/{floorId:guid}/equipment")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CreateFloorEquipment(Guid floorId, [FromBody] HousingEquipmentUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertEquipmentAsync(floorId, null, null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("floors/{floorId:guid}/equipment/{equipmentId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> UpdateFloorEquipment(Guid floorId, Guid equipmentId, [FromBody] HousingEquipmentUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertEquipmentAsync(floorId, null, equipmentId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("equipment/{equipmentId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> DeleteEquipment(Guid equipmentId, CancellationToken cancellationToken)
    {
        var result = await service.DeleteEquipmentAsync(equipmentId, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/residents")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> Residents(Guid id, [FromQuery] bool currentOnly = false, CancellationToken cancellationToken = default)
    {
        var result = await service.GetResidentsAsync(id, currentOnly, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/residents")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> AssignResident(Guid id, [FromBody] AssignHousingResidentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AssignResidentAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("residence-periods/{periodId:guid}/close")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CloseResidence(Guid periodId, [FromBody] ClosePeriodRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CloseResidenceAsync(periodId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/supervisors")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> Supervisors(Guid id, [FromQuery] bool currentOnly = false, CancellationToken cancellationToken = default)
    {
        var result = await service.GetSupervisorsAsync(id, currentOnly, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/supervisors")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> AssignSupervisor(Guid id, [FromBody] AssignHousingSupervisorRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AssignSupervisorAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("supervisor-periods/{periodId:guid}/close")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CloseSupervisor(Guid periodId, [FromBody] ClosePeriodRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CloseSupervisorAsync(periodId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> Upsert(Guid? id, HousingUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
