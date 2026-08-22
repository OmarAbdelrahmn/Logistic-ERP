using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Tags;

public static class TagErrors
{
    public static readonly OperationError InvalidRequest = new(
        "tags.invalid_request", "The tag request is invalid or not applicable to the resource.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "tags.not_found", "The requested tag or resource was not found.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new(
        "tags.duplicate", "A tag with the same code already exists.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "tags.concurrency_conflict", "The resource changed after it was loaded.", ErrorType.Conflict);
}

