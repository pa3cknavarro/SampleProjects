// Imports the client-side ErrorEnvelope used by the Failure factory.
using Ecommerce.Api.Client.Models;

// Namespace for the HTTP client and its result type.
namespace Ecommerce.Api.Client.Services;

// Generic result of one HTTP call. T is the success payload. init-only setters
// mean the properties can be set in an object initializer and not changed afterward.
public class ApiResult<T>
{
    // True when the call returned a success status and a body was deserialized into Data.
    public bool IsSuccess { get; init; }

    // The HTTP status code, whether the call succeeded or failed.
    public int StatusCode { get; init; }

    // Deserialized success body. Null on failure or when the body was empty.
    public T? Data { get; init; }

    // Deserialized error body. Null on success.
    public ErrorEnvelope? Error { get; init; }

    // Factory for a successful call. The expression-bodied member returns a new result
    // with IsSuccess true, the given status, and the given data. Error stays null.
    public static ApiResult<T> Success(int status, T data) =>
        new() { IsSuccess = true, StatusCode = status, Data = data };

    // Factory for a failed call. IsSuccess is false, Data stays null, and error may
    // itself be null if the body was not a valid envelope.
    public static ApiResult<T> Failure(int status, ErrorEnvelope? error) =>
        new() { IsSuccess = false, StatusCode = status, Error = error };
}
// Ends the result type.
