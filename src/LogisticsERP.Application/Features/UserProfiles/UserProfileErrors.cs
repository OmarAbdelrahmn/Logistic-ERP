using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.UserProfiles;

public static class UserProfileErrors
{
    public static readonly OperationError CurrentUserUnavailable = new(
        "UserProfile.CurrentUserUnavailable",
        "The current authenticated user is unavailable.",
        ErrorType.Unauthorized);

    public static readonly OperationError InvalidPreferences = new(
        "UserProfile.InvalidPreferences",
        "At least one valid user preference must be supplied.",
        ErrorType.Validation);

    public static readonly OperationError InvalidProfileImage = new(
        "UserProfile.InvalidProfileImage",
        "The profile image must be a JPEG, PNG, or WebP image no larger than 5 MB.",
        ErrorType.Validation);
}
