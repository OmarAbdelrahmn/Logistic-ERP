using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Telecom;

public static class PhoneSimErrors
{
    public static readonly OperationError InvalidRequest = new(
        "phone_sim.invalid_request",
        "The phone SIM request contains invalid or incomplete data.",
        ErrorType.Validation);

    public static readonly OperationError InvalidPhoneNumber = new(
        "phone_sim.invalid_phone_number",
        "The phone number is not a supported Saudi mobile or international E.164 number.",
        ErrorType.Validation,
        "phoneNumber");

    public static readonly OperationError InvalidIccid = new(
        "phone_sim.invalid_iccid",
        "The ICCID must contain 18 to 22 digits and start with 89.",
        ErrorType.Validation,
        "iccid");

    public static readonly OperationError InvalidStatus = new(
        "phone_sim.invalid_status",
        "The requested phone SIM status is invalid or cannot be set directly.",
        ErrorType.Validation,
        "status");

    public static readonly OperationError NotFound = new(
        "phone_sim.not_found",
        "The requested phone SIM card was not found.",
        ErrorType.NotFound,
        "id");

    public static readonly OperationError AssignmentNotFound = new(
        "phone_sim.assignment_not_found",
        "The requested phone SIM assignment was not found.",
        ErrorType.NotFound,
        "assignmentId");

    public static readonly OperationError ResponsibleEmployeeNotFound = new(
        "phone_sim.responsible_employee_not_found",
        "The selected responsible employee was not found.",
        ErrorType.NotFound,
        "responsibleEmployeeId");

    public static readonly OperationError ResponsibleEmployeeUnavailable = new(
        "phone_sim.responsible_employee_unavailable",
        "The selected responsible person must be an active internal employee.",
        ErrorType.Conflict,
        "responsibleEmployeeId");

    public static readonly OperationError RiderNotFound = new(
        "phone_sim.rider_not_found",
        "The selected rider profile was not found.",
        ErrorType.NotFound,
        "riderProfileId");

    public static readonly OperationError RiderUnavailable = new(
        "phone_sim.rider_unavailable",
        "The selected rider is not active or eligible to receive a phone SIM.",
        ErrorType.Conflict,
        "riderProfileId");

    public static readonly OperationError DuplicatePhoneNumber = new(
        "phone_sim.duplicate_phone_number",
        "Another active phone SIM card already uses this phone number.",
        ErrorType.Conflict,
        "phoneNumber");

    public static readonly OperationError DuplicateIccid = new(
        "phone_sim.duplicate_iccid",
        "Another active phone SIM card already uses this ICCID.",
        ErrorType.Conflict,
        "iccid");

    public static readonly OperationError ActiveAssignmentConflict = new(
        "phone_sim.active_assignment_conflict",
        "The operation is not allowed while the SIM has an active rider assignment.",
        ErrorType.Conflict);

    public static readonly OperationError AssignmentConflict = new(
        "phone_sim.assignment_conflict",
        "The assignment conflicts with the current SIM or rider state.",
        ErrorType.Conflict);

    public static readonly OperationError InvalidDateRange = new(
        "phone_sim.invalid_date_range",
        "The assignment dates are invalid. Future dates are not allowed and the end date cannot precede the start date.",
        ErrorType.Validation);

    public static readonly OperationError ConcurrencyConflict = new(
        "phone_sim.concurrency_conflict",
        "The record changed after it was loaded. Reload it and retry.",
        ErrorType.Conflict);

    public static readonly OperationError CurrentUserUnavailable = new(
        "phone_sim.current_user_unavailable",
        "The authenticated user could not be resolved.",
        ErrorType.Unauthorized);

    public static readonly OperationError PersistenceConflict = new(
        "phone_sim.persistence_conflict",
        "The operation could not be completed because the phone SIM state changed or conflicts with another record.",
        ErrorType.Conflict);
}
