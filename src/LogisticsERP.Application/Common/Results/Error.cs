namespace LogisticsERP.Application.Common.Results;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Failure = 6
}

public sealed record OperationError(
    string Code,
    string Description,
    ErrorType Type,
    string? Field = null,
    IReadOnlyDictionary<string, object?>? Details = null)
{
    public static readonly OperationError None = new(string.Empty, string.Empty, ErrorType.None);
}
