using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fleet;

public static class FleetErrors
{
    public static readonly OperationError InvalidRequest = new("fleet.invalid_request", "The fleet request contains invalid or incomplete data.", ErrorType.Validation);
    public static readonly OperationError NotFound = new("fleet.not_found", "The requested fleet record was not found.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new("fleet.duplicate", "A vehicle or catalog record with the same unique value already exists.", ErrorType.Conflict);
    public static readonly OperationError Conflict = new("fleet.conflict", "The operation conflicts with the current vehicle or rider state.", ErrorType.Conflict);
    public static readonly OperationError VehicleUnavailable = new("fleet.vehicle_unavailable", "The vehicle is not available for assignment.", ErrorType.Conflict);
    public static readonly OperationError RiderUnavailable = new("fleet.rider_unavailable", "The rider is inactive or already has an active vehicle.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new("fleet.concurrency_conflict", "The record changed after it was loaded. Reload it and retry.", ErrorType.Conflict);
    public static readonly OperationError IdempotencyConflict = new("fleet.idempotency_conflict", "The idempotency key was already used for a different request.", ErrorType.Conflict);
    public static readonly OperationError IdempotencyRequired = new("fleet.idempotency_required", "An Idempotency-Key header is required.", ErrorType.Validation);
    public static readonly OperationError Forbidden = new("fleet.forbidden", "The current user cannot access this fleet record.", ErrorType.Forbidden);
    public static readonly OperationError CurrentUserUnavailable = new("fleet.current_user_unavailable", "The authenticated user could not be resolved.", ErrorType.Unauthorized);
    public static readonly OperationError InvalidFile = new("fleet.invalid_file", "The file is empty, too large, unsupported, or does not match its declared type.", ErrorType.Validation);
    public static readonly OperationError FileLimit = new("fleet.file_limit", "A rider can have at most three active promissory-note files.", ErrorType.Conflict);
    public static readonly OperationError FileMissing = new("fleet.file_missing", "The stored file could not be found.", ErrorType.NotFound);
    public static readonly OperationError InvalidState = new("fleet.invalid_state", "The requested transition is not valid for the current state.", ErrorType.Conflict);
    public static readonly OperationError OdometerDecreased = new("fleet.odometer_decreased", "The odometer cannot decrease without an authorized correction reason.", ErrorType.Conflict);
    public static readonly OperationError AccidentAssignmentMismatch = new("fleet.accident_assignment_mismatch", "The rider did not hold the vehicle at the reported accident time.", ErrorType.Conflict);
}
