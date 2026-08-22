using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.System;

public static class SystemErrors
{
    public static readonly OperationError InvalidRequest = new(
        "system.invalid_request", "The system request is invalid.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "system.not_found", "The requested system record was not found.", ErrorType.NotFound);
    public static readonly OperationError Forbidden = new(
        "system.forbidden", "The requested record does not belong to the authenticated user.", ErrorType.Forbidden);
    public static readonly OperationError Conflict = new(
        "system.conflict", "The operation conflicts with the current record state.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "system.concurrency_conflict", "The record changed after it was loaded.", ErrorType.Conflict);
    public static readonly OperationError CurrentUserUnavailable = new(
        "system.current_user_unavailable", "The authenticated user could not be resolved.", ErrorType.Unauthorized);
    public static readonly OperationError ArtifactUnavailable = new(
        "exports.artifact_unavailable", "The export artifact is unavailable or has expired.", ErrorType.NotFound);
}

