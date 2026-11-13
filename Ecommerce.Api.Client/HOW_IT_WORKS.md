# Ecommerce.Api.Client – How This Project Works

## Purpose
Reusable **typed HttpClient** library so console apps, web apps, and services can call Ecommerce.Api
without hand-writing URLs, headers, and JSON deserialization.

## Pieces

| Type | Role |
|------|------|
| `EcommerceApiOptions` | BaseUrl + ApiKey configuration |
| `EcommerceApiClient` | Methods per resource (GetProvincesAsync, CreateCityAsync, …) |
| `ApiResult<T>` | Success flag, status code, data or ErrorEnvelope |
| `Models/*` | DTOs mirroring API request/response shapes |
| `ServiceCollectionExtensions` | `AddEcommerceApiClient(...)` for DI |

## How a call works

1. Consumer registers the client with `services.AddEcommerceApiClient(opts => { ... }, acceptAnyServerCertificate: true)`.
2. `IHttpClientFactory` creates an `HttpClient` with BaseAddress and `X-Api-Key`.
3. `EcommerceApiClient` builds paths like `api/v1/provinces?page=1&pageSize=10`.
4. Optional `X-Idempotency-Key` on write methods.
5. Response is deserialized to `T` or `ErrorEnvelope` and wrapped in `ApiResult<T>`.

## Interconnection with other projects

- **Ecommerce.Api** is the server this client talks to.
- **Ecommerce.ConsoleClient** and **Ecommerce.WebClient** reference this project and inject `EcommerceApiClient`.

## Notes
- Set `acceptAnyServerCertificate: true` only for local HTTPS with the ASP.NET development certificate.
- Property names use camelCase to match the API’s JSON policy.
