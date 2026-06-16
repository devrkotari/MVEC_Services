namespace Mvec.BuildingBlocks.Common;

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error NotFound(string msg) => new("not_found", msg);
    public static Error Validation(string msg) => new("validation", msg);
    public static Error Conflict(string msg) => new("conflict", msg);
}

public class Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }
    protected Result(bool ok, Error error) { IsSuccess = ok; Error = error; }
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error e) => new(false, e);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }
    private Result(T value) : base(true, Error.None) => Value = value;
    private Result(Error e) : base(false, e) => Value = default;
    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error e) => new(e);
}
