using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.UserManagement;

public static class UserManagementErrors
{
    public static readonly OperationError CurrentUserUnavailable = new("UserManagement.CurrentUserUnavailable", "The current authenticated user is unavailable.", ErrorType.Unauthorized);
    public static readonly OperationError NotFound = new("UserManagement.NotFound", "The requested user or authorization item was not found.", ErrorType.NotFound);
    public static readonly OperationError InvalidRequest = new("UserManagement.InvalidRequest", "The user-management request is invalid.", ErrorType.Validation);
    public static readonly OperationError Duplicate = new("UserManagement.Duplicate", "The username, email, employee, role, or permission assignment already exists.", ErrorType.Conflict);
    public static readonly OperationError Conflict = new("UserManagement.Conflict", "The operation conflicts with the current security state.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new("UserManagement.ConcurrencyConflict", "The user was changed by another operation. Reload it and try again.", ErrorType.Conflict);
    public static readonly OperationError PasswordRejected = new("UserManagement.PasswordRejected", "The password does not satisfy the configured password policy.", ErrorType.Validation);
    public static readonly OperationError ProtectedAccount = new("UserManagement.ProtectedAccount", "The protected development account cannot be changed through user management.", ErrorType.Forbidden);
    public static readonly OperationError SelfSecurityChange = new("UserManagement.SelfSecurityChange", "Use the personal profile and password endpoints for your own account; another administrator must change your security access.", ErrorType.Forbidden);
}
