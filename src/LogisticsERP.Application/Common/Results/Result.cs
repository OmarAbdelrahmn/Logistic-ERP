namespace LogisticsERP.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, OperationError error)
    {
        if (isSuccess && error != OperationError.None || !isSuccess && error == OperationError.None)
        {
            throw new ArgumentException("Result success state and error are inconsistent.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public OperationError Error { get; }

    public static Result Success() => new(true, OperationError.None);
    public static Result Failure(OperationError error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, OperationError.None);
    public static Result<TValue> Failure<TValue>(OperationError error) => new(default, false, error);
}

public sealed class Result<TValue> : Result
{
    internal Result(TValue? value, bool isSuccess, OperationError error) : base(isSuccess, error)
    {
        Value = value;
    }

    public TValue? Value { get; }
}
