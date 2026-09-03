using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.SupportAccess;

public static class SupportAccessErrors
{
    public static readonly OperationError InvalidRequest = new(
        "support_access.invalid_request", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "support_access.not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError Conflict = new(
        "support_access.conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "support_access.concurrency_conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError CurrentUserUnavailable = new(
        "support_access.current_user_unavailable", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Unauthorized);
}

