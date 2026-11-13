# Ecommerce.WebClient – How This Project Works

## Purpose
Razor Pages UI that displays API data using **Ecommerce.Api.Client**.

## Flow

1. `Program.cs` binds `EcommerceApi` settings from `appsettings.json`.
2. Registers Razor Pages + `EcommerceApiClient` (accepts dev certs in Development).
3. Page models inject `EcommerceApiClient` and call the API in `OnGetAsync`.
4. The `.cshtml` view renders the result or an error message.

## Interconnection

```
Browser → Ecommerce.WebClient (Razor) → Ecommerce.Api.Client → Ecommerce.Api
```

## Configuration

Update `EcommerceApi:BaseUrl` in appsettings to the API HTTPS port.

## Run

```bash
cd Ecommerce.Api && dotnet run
cd Ecommerce.WebClient && dotnet run
```
