using Ecommerce.Api.Client.Models;

namespace Ecommerce.Api.Client.Services;

public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public ErrorEnvelope? Error { get; init; }

    public static ApiResult<T> Success(int status, T data) =>
        new() { IsSuccess = true, StatusCode = status, Data = data };

    public static ApiResult<T> Failure(int status, ErrorEnvelope? error) =>
        new() { IsSuccess = false, StatusCode = status, Error = error };
}
