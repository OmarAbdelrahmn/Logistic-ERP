using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Telecom;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/places")]
public sealed class PlacesController(IPlaceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> Create([FromBody] SavePlaceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePlaceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
