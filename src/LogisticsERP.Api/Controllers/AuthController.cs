using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Features.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            request,
            CreateClientContext(),
            cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem(HttpContext);
        }

        DisableResponseCaching();
        return Ok(result.Value);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RefreshAsync(
            request,
            CreateClientContext(),
            cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem(HttpContext);
        }

        DisableResponseCaching();
        return Ok(result.Value);
    }

    [Authorize(Policy = AuthenticationPolicies.AllowPasswordChangeRequired)]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await authenticationService.LogoutAsync(cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [Authorize(Policy = AuthenticationPolicies.AllowPasswordChangeRequired)]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await authenticationService.LogoutAllAsync(cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [Authorize(Policy = AuthenticationPolicies.AllowPasswordChangeRequired)]
    [HttpPost("change-password")]
    [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.ChangePasswordAsync(
            request,
            CreateClientContext(),
            cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem(HttpContext);
        }

        DisableResponseCaching();
        return Ok(result.Value);
    }

    [Authorize(Policy = AuthenticationPolicies.AllowPasswordChangeRequired)]
    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<UserSessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var result = await authenticationService.GetSessionsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [Authorize(Policy = AuthenticationPolicies.AllowPasswordChangeRequired)]
    [HttpPost("sessions/{sessionId:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RevokeSessionAsync(sessionId, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    private AuthenticationClientContext CreateClientContext() => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString());

    private void DisableResponseCaching()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }
}
