using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.UserProfiles;

public static class UserProfileErrors
{
    public static readonly OperationError CurrentUserUnavailable = new(
        "UserProfile.CurrentUserUnavailable",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Unauthorized);

    public static readonly OperationError InvalidPreferences = new(
        "UserProfile.InvalidPreferences",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);

    public static readonly OperationError InvalidProfileImage = new(
        "UserProfile.InvalidProfileImage",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);
}
