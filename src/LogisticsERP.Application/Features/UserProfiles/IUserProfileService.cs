using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.UserProfiles;

public interface IUserProfileService
{
    Task<Result<UserProfileResponse>> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<Result<UserProfileResponse>> UpdatePreferencesAsync(
        UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserAuthorizationResponse>> GetAuthorizationAsync(
        CancellationToken cancellationToken = default);
}
