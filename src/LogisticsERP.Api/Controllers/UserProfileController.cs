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

    [HttpPut("me/profile-image")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfileImage(
        [FromForm] UserProfileImageUploadForm request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest();
        }

        await using var stream = request.File.OpenReadStream();
        var result = await userProfileService.UpdateProfileImageAsync(
            new UserProfileImageUpload(stream, request.File.ContentType, request.File.Length),
            cancellationToken);
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

public sealed class UserProfileImageUploadForm
{
    public IFormFile File { get; init; } = null!;
}
