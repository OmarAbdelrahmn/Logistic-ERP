using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/platform-accounts")]
public sealed class PlatformAccountsController(ISimplePlatformService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public Task<IActionResult> GetAll(
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? platformId,
        [FromQuery] Guid? operatingCityId,
        [FromQuery] Guid? ownerRiderProfileId,
        [FromQuery] Guid? actualRiderProfileId,
        [FromQuery] string? status,
        [FromQuery] bool currentOnly = false,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        ToAction(service.GetAccountsAsync(
            accountId,
            platformId,
            operatingCityId,
            ownerRiderProfileId,
            actualRiderProfileId,
            status,
            currentOnly,
            includeArchived,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ToAction(service.GetAccountAsync(id, cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> Create(
        [FromBody] SimplePlatformAccountUpsertRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.CreateAccountAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] SimplePlatformAccountUpsertRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.UpdateAccountAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/assign")]
    [RequirePermission(PermissionKeys.Operations.PlatformAssignmentsManage)]
    public Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignSimplePlatformAccountRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.AssignAccountAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/release")]
    [RequirePermission(PermissionKeys.Operations.PlatformAssignmentsManage)]
    public Task<IActionResult> Release(
        Guid id,
        [FromBody] ReleaseSimplePlatformAccountRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.ReleaseAccountAsync(id, request, cancellationToken));

    [HttpGet("{id:guid}/assignment-history")]
    [RequirePermission(PermissionKeys.Operations.PlatformAssignmentsRead)]
    public Task<IActionResult> AssignmentHistory(Guid id, CancellationToken cancellationToken) =>
        ToAction(service.GetAccountAssignmentHistoryAsync(id, cancellationToken));

    [HttpGet("{id:guid}/credential-history")]
    [RequirePermission(PermissionKeys.Operations.PlatformCredentialsRead)]
    public Task<IActionResult> CredentialHistory(Guid id, CancellationToken cancellationToken) =>
        ToAction(service.GetCredentialHistoryAsync(id, cancellationToken));

    [HttpPost("{id:guid}/rotate-credential")]
    [RequirePermission(PermissionKeys.Operations.PlatformCredentialsRotate)]
    public Task<IActionResult> RotateCredential(
        Guid id,
        [FromBody] RotateSimplePlatformCredentialRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.RotateCredentialAsync(id, request, cancellationToken));

    private async Task<IActionResult> ToAction<T>(Task<LogisticsERP.Application.Common.Results.Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
