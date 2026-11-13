namespace Ecommerce.Api.Client.Services;

public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }

    public static ApiResult<T> Success(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Failure(int statusCode, string error) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };
}
