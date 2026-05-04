namespace API.Domain.Common;

public class ResultOf<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    
    public T? Value { get; }
    public string? Error { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    private ResultOf(
        bool isSuccess,
        T? value,
        string? error,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ValidationErrors = validationErrors;
    }

    // Successful result with a value
    public static ResultOf<T> Success(T value)
        => new ResultOf<T>(isSuccess: true, value: value, error: null);

    // Failure due to error
    public static ResultOf<T> Failure(string error)
        => new ResultOf<T>(isSuccess: false, value: default, error: error);

    // Failure due to validation errors
    public static ResultOf<T> ValidationFailure(
        IReadOnlyDictionary<string, string[]> validationErrors)
        => new ResultOf<T>(
            isSuccess: false,
            value: default,
            error: "Validation failed",
            validationErrors: validationErrors);
}