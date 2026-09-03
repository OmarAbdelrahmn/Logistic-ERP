using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.System;

public static class SystemErrors
{
    public static readonly OperationError InvalidRequest = new(
        "system.invalid_request", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "system.not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError Forbidden = new(
        "system.forbidden", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Forbidden);
    public static readonly OperationError Conflict = new(
        "system.conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "system.concurrency_conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError CurrentUserUnavailable = new(
        "system.current_user_unavailable", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Unauthorized);
    public static readonly OperationError ArtifactUnavailable = new(
        "exports.artifact_unavailable", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
}

