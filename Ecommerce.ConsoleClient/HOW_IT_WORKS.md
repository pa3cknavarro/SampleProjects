# Ecommerce.ConsoleClient – How This Project Works

## Purpose
Command-line demo that exercises the API through **Ecommerce.Api.Client**.

## Flow

1. Read `ECOMMERCE_API_BASE_URL` and `ECOMMERCE_API_KEY` (or defaults).
2. Build a `ServiceCollection`, register `EcommerceApiClient` with a handler that trusts local HTTPS certs.
3. Resolve `EcommerceApiClient` and run sample operations (list/create provinces, cities, barangays).

## Interconnection

```
ConsoleClient → Ecommerce.Api.Client (HttpClient) → Ecommerce.Api (HTTPS)
```

## Run

```bash
cd Ecommerce.Api && dotnet run
export ECOMMERCE_API_BASE_URL=https://localhost:PORT
cd Ecommerce.ConsoleClient && dotnet run
```
