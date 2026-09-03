using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Telecom;

public static class PhoneSimErrors
{
    public static readonly OperationError InvalidRequest = new(
        "phone_sim.invalid_request",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);

    public static readonly OperationError InvalidPhoneNumber = new(
        "phone_sim.invalid_phone_number",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "phoneNumber");

    public static readonly OperationError InvalidIccid = new(
        "phone_sim.invalid_iccid",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "iccid");

    public static readonly OperationError InvalidStatus = new(
        "phone_sim.invalid_status",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation,
        "status");

    public static readonly OperationError NotFound = new(
        "phone_sim.not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "id");

    public static readonly OperationError ReceiptFormNotFound = new(
        "phone_sim.receipt_form_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "id");

    public static readonly OperationError AssignmentNotFound = new(
        "phone_sim.assignment_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "assignmentId");

    public static readonly OperationError ResponsibleEmployeeNotFound = new(
        "phone_sim.responsible_employee_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "responsibleEmployeeId");

    public static readonly OperationError ResponsibleEmployeeUnavailable = new(
        "phone_sim.responsible_employee_unavailable",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "responsibleEmployeeId");

    public static readonly OperationError RiderNotFound = new(
        "phone_sim.rider_not_found",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.NotFound,
        "riderProfileId");

    public static readonly OperationError RiderUnavailable = new(
        "phone_sim.rider_unavailable",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "riderProfileId");

    public static readonly OperationError DuplicatePhoneNumber = new(
        "phone_sim.duplicate_phone_number",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "phoneNumber");

    public static readonly OperationError DuplicateIccid = new(
        "phone_sim.duplicate_iccid",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict,
        "iccid");

    public static readonly OperationError ActiveAssignmentConflict = new(
        "phone_sim.active_assignment_conflict",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict);

    public static readonly OperationError AssignmentConflict = new(
        "phone_sim.assignment_conflict",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict);

    public static readonly OperationError InvalidDateRange = new(
        "phone_sim.invalid_date_range",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Validation);

    public static readonly OperationError ConcurrencyConflict = new(
        "phone_sim.concurrency_conflict",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict);

    public static readonly OperationError CurrentUserUnavailable = new(
        "phone_sim.current_user_unavailable",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Unauthorized);

    public static readonly OperationError PersistenceConflict = new(
        "phone_sim.persistence_conflict",
        "تعذر تنفيذ العملية المطلوبة.",
        ErrorType.Conflict);
}
