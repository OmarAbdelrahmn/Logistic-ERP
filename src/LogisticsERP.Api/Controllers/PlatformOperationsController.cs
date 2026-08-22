using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/platform-operations")]
public sealed class PlatformOperationsController(IPlatformOperationsService service) : ControllerBase
{
    [HttpGet("platforms")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public Task<IActionResult> Platforms(CancellationToken cancellationToken) => ToAction(service.GetPlatformsAsync(cancellationToken));

    [HttpPost("platforms")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> CreatePlatform([FromBody] ClientPlatformUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertPlatformAsync(null, request, cancellationToken));

    [HttpPut("platforms/{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> UpdatePlatform(Guid id, [FromBody] ClientPlatformUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertPlatformAsync(id, request, cancellationToken));

    [HttpGet("contracts")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public Task<IActionResult> Contracts([FromQuery] Guid? platformId, CancellationToken cancellationToken) => ToAction(service.GetContractsAsync(platformId, cancellationToken));

    [HttpPost("contracts")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> CreateContract([FromBody] ClientContractUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertContractAsync(null, request, cancellationToken));

    [HttpPut("contracts/{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> UpdateContract(Guid id, [FromBody] ClientContractUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertContractAsync(id, request, cancellationToken));

    [HttpGet("accounts")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public Task<IActionResult> Accounts([FromQuery] Guid? platformId, CancellationToken cancellationToken) => ToAction(service.GetAccountsAsync(platformId, cancellationToken));

    [HttpPost("accounts")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> CreateAccount([FromBody] PlatformAccountUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertAccountAsync(null, request, cancellationToken));

    [HttpPut("accounts/{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> UpdateAccount(Guid id, [FromBody] PlatformAccountUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertAccountAsync(id, request, cancellationToken));

    [HttpGet("accounts/{id:guid}/credentials/versions")]
    [RequirePermission(PermissionKeys.Operations.PlatformCredentialsRead)]
    public Task<IActionResult> CredentialVersions(Guid id, CancellationToken cancellationToken) =>
        ToAction(service.GetCredentialVersionsAsync(id, cancellationToken));

    [HttpPost("accounts/{id:guid}/credentials/rotations")]
    [RequirePermission(PermissionKeys.Operations.PlatformCredentialsRotate)]
    public Task<IActionResult> RotateCredential(
        Guid id,
        [FromBody] RotatePlatformCredentialRequest request,
        CancellationToken cancellationToken) =>
        ToAction(service.RotateCredentialAsync(id, request, cancellationToken));

    [HttpGet("registrations")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsRead)]
    public Task<IActionResult> Registrations([FromQuery] Guid? riderProfileId, CancellationToken cancellationToken) => ToAction(service.GetRegistrationsAsync(riderProfileId, cancellationToken));

    [HttpPost("registrations")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> CreateRegistration([FromBody] PlatformRegistrationUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertRegistrationAsync(null, request, cancellationToken));

    [HttpPut("registrations/{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public Task<IActionResult> UpdateRegistration(Guid id, [FromBody] PlatformRegistrationUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertRegistrationAsync(id, request, cancellationToken));

    [HttpGet("assignments")]
    [RequirePermission(PermissionKeys.Operations.PlatformAssignmentsRead)]
    public Task<IActionResult> Assignments([FromQuery] Guid? riderProfileId, [FromQuery] bool currentOnly = false, CancellationToken cancellationToken = default) => ToAction(service.GetAssignmentsAsync(riderProfileId, currentOnly, cancellationToken));

    [HttpPost("assignments")]
    [RequirePermission(PermissionKeys.Operations.PlatformAssignmentsManage)]
    public Task<IActionResult> AssignAccount([FromBody] AssignPlatformAccountRequest request, CancellationToken cancellationToken) => ToAction(service.AssignAccountAsync(request, cancellationToken));

    [HttpPost("assignments/{id:guid}/close")]
    [RequirePermission(PermissionKeys.Operations.PlatformAssignmentsManage)]
    public Task<IActionResult> CloseAssignment(Guid id, [FromBody] ClosePlatformAssignmentRequest request, CancellationToken cancellationToken) => ToAction(service.CloseAssignmentAsync(id, request, cancellationToken));

    [HttpPatch("{resource}/{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Operations.PlatformAccountsManage)]
    public async Task<IActionResult> Archive(string resource, Guid id, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(resource, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> ToAction<T>(Task<LogisticsERP.Application.Common.Results.Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
