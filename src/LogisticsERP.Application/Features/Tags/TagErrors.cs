using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Tags;

public static class TagErrors
{
    public static readonly OperationError InvalidRequest = new(
        "tags.invalid_request", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Validation);
    public static readonly OperationError NotFound = new(
        "tags.not_found", "تعذر تنفيذ العملية المطلوبة.", ErrorType.NotFound);
    public static readonly OperationError Duplicate = new(
        "tags.duplicate", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new(
        "tags.concurrency_conflict", "تعذر تنفيذ العملية المطلوبة.", ErrorType.Conflict);
}

