namespace JDB_Portfolio_API.Application.Common;

/// <summary>
/// The outcome of an application operation: either a value, or a typed
/// <see cref="Common.Error"/>.
///
/// <para>
/// The error used to be a string, which meant every failure looked identical and the
/// API had no way to choose a status code other than 502. <see cref="ErrorKind"/> is
/// what makes that choice possible.
/// </para>
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }

    public Error? Error { get; }

    public T? Value { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(Error error) =>
        new(false, default, error ?? throw new System.ArgumentNullException(nameof(error)));
}
