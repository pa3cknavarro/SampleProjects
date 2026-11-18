# Ecommerce.Main API Integration

## Architecture

- `Ecommerce.Main` is a React application built with Vite and JavaScript/TypeScript.
- `Ecommerce.Api` provides the catalog, customer, order, and administration APIs.
- `src/api.ts` centralizes API requests and response types; `src/ApiStore.tsx` implements the API-backed storefront.
- The Vite development proxy forwards `/api` requests to the API and supplies the development API key. Browser user sessions use an HttpOnly cookie.
- The API uses ASP.NET Core Identity-style password hashing and cookie authentication. Registration verification and password recovery require email delivery outside Development.
- EF Core persists application data in PostgreSQL through Npgsql. Set `ConnectionStrings__EcommerceDb`; Development has a localhost fallback and other environments require an explicit connection string.
- TOTP MFA secrets are encrypted with ASP.NET Core Data Protection. Persist the key ring path configured by `DataProtection__KeyRingPath` when deploying or moving hosts.

## Feature mapping

| Frontend feature | API integration |
|---|---|
| Product browsing, search, details, and categories | Product and category endpoints |
| Shopping cart | Browser-local storage, scoped by user |
| Checkout and order history | Authenticated order endpoints |
| Customer account | Authenticated customer profile and password endpoints |
| Admin dashboard and reports | API-backed customer, product, order, and summary data |
| Admin entity management | Customers, products, categories, customer classes, units, prices, area availability, locations, and users |
| Login, registration, verification, and password recovery | API cookie-session endpoints |
| Two-factor authentication | Authenticator-based TOTP with hashed, one-time recovery codes |
| In-app notifications | Persistent order/account notifications with unread and mark-read actions |

## Limitations

- PostgreSQL supports shared/multi-instance data, but the process-local idempotency and credential challenge stores are not shared across instances.
- Startup applies committed EF Core migrations. Existing SQLite data is not imported automatically when changing providers.
- Registration/password-recovery challenges and idempotency records remain process-local.
- The development API key and seeded `admin` / `admin-local-only` account are local demo credentials, not production configuration.
- Cart state remains browser-local and is not synchronized to the API cart resource.
- The storefront uses the first configured unit price as the default unit; it does not yet let shoppers choose among multiple units.
- Notifications are in-app only; email, push, and SMS notification providers are not configured.
- Multi-instance deployments need a shared persistent Data Protection key ring, secrets, email delivery, and deployment-specific API URL settings.

## Validation

From `Ecommerce.Main`, run `npm run typecheck`, `npm run lint`, `npm test`, and `npm run build`. From the repository root, run `dotnet test Ecommerce.Api.Tests/Ecommerce.Api.Tests.csproj`.
