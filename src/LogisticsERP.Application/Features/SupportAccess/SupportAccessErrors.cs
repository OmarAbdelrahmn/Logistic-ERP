using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.SupportAccess;

public static class SupportAccessErrors
{
    public static readonly OperationError InvalidRequest = new(
        "support_access.invalid_request", "The support-access request is invalid.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "support_access.not_found", "The support-access grant was not found.", ErrorType.NotFound);
    public static readonly OperationError Conflict = new(
        "support_access.conflict", "The requested transition conflicts with the current grant state.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "support_access.concurrency_conflict", "The support-access grant changed after it was loaded.", ErrorType.Conflict);
    public static readonly OperationError CurrentUserUnavailable = new(
        "support_access.current_user_unavailable", "The authenticated user could not be resolved.", ErrorType.Unauthorized);
}

