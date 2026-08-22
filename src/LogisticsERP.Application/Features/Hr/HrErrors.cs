using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public static class HrErrors
{
    public static readonly OperationError InvalidRequest = new(
        "hr.invalid_request", "The HR request contains invalid or incomplete data.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "hr.not_found", "The requested HR record was not found.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new(
        "hr.duplicate", "A record with the same unique value already exists.", ErrorType.Conflict);
    public static readonly OperationError Conflict = new(
        "hr.conflict", "The operation conflicts with the current record state.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "hr.concurrency_conflict", "The record changed after it was loaded. Reload it and retry.", ErrorType.Conflict);
    public static readonly OperationError CurrentUserUnavailable = new(
        "hr.current_user_unavailable", "The authenticated user could not be resolved.", ErrorType.Unauthorized);
    public static readonly OperationError InvalidFile = new(
        "documents.invalid_file", "The file is empty, too large, or has an unsupported type.", ErrorType.Validation);
    public static readonly OperationError FileMissing = new(
        "documents.file_missing", "The stored file could not be found.", ErrorType.NotFound);
    public static readonly OperationError CapacityExceeded = new(
        "housing.capacity_exceeded", "The housing capacity would be exceeded. Supply an approved override reason.", ErrorType.Conflict);
}

