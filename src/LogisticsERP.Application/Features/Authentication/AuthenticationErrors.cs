using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Authentication;

public static class AuthenticationErrors
{
    public static readonly OperationError InvalidRequest = new(
        "Authentication.InvalidRequest",
        "The authentication request is invalid.",
        ErrorType.Validation);

    public static readonly OperationError InvalidCredentials = new(
        "Authentication.InvalidCredentials",
        "The login or password is incorrect.",
        ErrorType.Unauthorized);

    public static readonly OperationError AccountLocked = new(
        "Authentication.AccountLocked",
        "The account is temporarily locked. Try again later.",
        ErrorType.Forbidden);

    public static readonly OperationError AccountUnavailable = new(
        "Authentication.AccountUnavailable",
        "The account is not available for sign in.",
        ErrorType.Forbidden);

    public static readonly OperationError AccountNotConfirmed = new(
        "Authentication.AccountNotConfirmed",
        "The account must be confirmed before sign in.",
        ErrorType.Forbidden);

    public static readonly OperationError InvalidRefreshToken = new(
        "Authentication.InvalidRefreshToken",
        "The refresh token is invalid or expired.",
        ErrorType.Unauthorized);

    public static readonly OperationError CurrentUserUnavailable = new(
        "Authentication.CurrentUserUnavailable",
        "The current authenticated user is unavailable.",
        ErrorType.Unauthorized);

    public static readonly OperationError InvalidCurrentPassword = new(
        "Authentication.InvalidCurrentPassword",
        "The current password is incorrect.",
        ErrorType.Validation);

    public static readonly OperationError PasswordRejected = new(
        "Authentication.PasswordRejected",
        "The new password does not satisfy the password policy.",
        ErrorType.Validation);

    public static readonly OperationError SessionNotFound = new(
        "Authentication.SessionNotFound",
        "The session was not found.",
        ErrorType.NotFound);

    public static readonly OperationError ConcurrentRefresh = new(
        "Authentication.ConcurrentRefresh",
        "The refresh token has already been used. Sign in again.",
        ErrorType.Unauthorized);
}
