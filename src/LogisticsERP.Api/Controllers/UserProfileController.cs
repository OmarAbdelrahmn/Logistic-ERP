using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Features.UserProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthenticationPolicies.AllowPasswordChangeRequired)]
[Route("api/user-profile")]
public sealed class UserProfileController(IUserProfileService userProfileService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var result = await userProfileService.GetCurrentAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("me/preferences")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userProfileService.UpdatePreferencesAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("me/authorization")]
    [ProducesResponseType<UserAuthorizationResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthorization(CancellationToken cancellationToken)
    {
        var result = await userProfileService.GetAuthorizationAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
