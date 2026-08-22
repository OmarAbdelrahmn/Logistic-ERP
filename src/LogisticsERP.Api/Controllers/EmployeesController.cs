using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/employees")]
[AllowAnonymous]
public sealed class EmployeesController(IWorkforceService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetEmployeesAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{employeeId:guid}")]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetEmployeeAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateEmployeeAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { employeeId = result.Value!.Employee.Id }, result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{employeeId:guid}")]
    public async Task<IActionResult> Update(Guid employeeId, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateEmployeeAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{employeeId:guid}/status-transitions")]
    public async Task<IActionResult> ChangeStatus(Guid employeeId, [FromBody] ChangeEmployeeStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeStatusAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{employeeId:guid}/relationship-transitions")]
    public async Task<IActionResult> ChangeRelationship(Guid employeeId, [FromBody] ChangeEmployeeRelationshipRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeRelationshipAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{employeeId:guid}/sponsored-details")]
    public async Task<IActionResult> UpdateSponsoredDetails(Guid employeeId, [FromBody] SponsoredInternalDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateSponsoredDetailsAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{employeeId:guid}/outside-rider-details")]
    public async Task<IActionResult> UpdateOutsideRiderDetails(Guid employeeId, [FromBody] OutsideRiderDetailsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateOutsideRiderDetailsAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{employeeId:guid}/operational-assignments")]
    public async Task<IActionResult> AssignOperationalWork(Guid employeeId, [FromBody] AssignOperationalWorkRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AssignOperationalWorkAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{employeeId:guid}/sponsorships")]
    public async Task<IActionResult> GetSponsorships(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetSponsorshipHistoryAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{employeeId:guid}/sponsorships")]
    public async Task<IActionResult> ChangeSponsorship(Guid employeeId, [FromBody] ChangeSponsorshipRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeSponsorshipAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{employeeId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid employeeId, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveEmployeeAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
