using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.UserManagement;

public static class UserManagementErrors
{
    public static readonly OperationError CurrentUserUnavailable = new("UserManagement.CurrentUserUnavailable", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Unauthorized);
    public static readonly OperationError NotFound = new("UserManagement.NotFound", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError InvalidRequest = new("UserManagement.InvalidRequest", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);
    public static readonly OperationError Duplicate = new("UserManagement.Duplicate", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError Conflict = new("UserManagement.Conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new("UserManagement.ConcurrencyConflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError PasswordRejected = new("UserManagement.PasswordRejected", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);
    public static readonly OperationError ProtectedAccount = new("UserManagement.ProtectedAccount", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Forbidden);
    public static readonly OperationError SelfSecurityChange = new("UserManagement.SelfSecurityChange", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Forbidden);
}
