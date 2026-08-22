using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Company;

public static class CompanyErrors
{
    public static readonly OperationError InvalidRequest = new(
        "company.invalid_request", "The company profile request is invalid.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "company.not_found", "The company profile was not found.", ErrorType.NotFound);
    public static readonly OperationError ConcurrencyConflict = new(
        "company.concurrency_conflict", "The company profile changed after it was loaded.", ErrorType.Conflict);
    public static readonly OperationError CurrentUserUnavailable = new(
        "company.current_user_unavailable", "The authenticated user could not be resolved.", ErrorType.Unauthorized);
}

