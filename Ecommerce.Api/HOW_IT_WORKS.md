# Ecommerce.Api – How This Project Works

## Purpose
HTTP API for ecommerce location hierarchy, catalog, customers, orders, and supporting entities.
Built with **vertical slices** and **Carter** modules instead of classic controllers.

## Request path (pipeline order)

1. **ExceptionHandlingMiddleware** – catches unhandled exceptions → `ErrorEnvelope` JSON
2. **HTTPS redirection**
3. **Rate limiter** – 100 requests / minute / IP → HTTP 429 + envelope
4. **ApiKeyAuthMiddleware** – requires header `X-Api-Key: demo-api-key-12345` (Swagger/samples skipped)
5. **IdempotencyMiddleware** – on POST/PUT/DELETE, header `X-Idempotency-Key` replays stored success responses
6. **Swagger / static files**
7. **Carter** – dispatches to feature modules under `Features/`

## Project layout

| Path | Role |
|------|------|
| `Program.cs` | DI registration, middleware pipeline, seed data |
| `Data/AppDbContext.cs` | EF Core model, composite keys, relationships |
| `Models/` | Entity classes (Province, Product, Order, User, …) |
| `Common/` | ErrorEnvelope, PaginatedResponse, HateoasLink, IdempotencyStore, constants |
| `Middleware/` | Auth, idempotency, exception handling |
| `Features/{Name}/` | One vertical slice per area: DTOs + `ICarterModule` endpoints |

## How features interconnect

- **Carter** scans assemblies for `ICarterModule` and calls `AddRoutes`.
- Handlers inject **AppDbContext** (scoped per request) and **HttpContext**.
- List endpoints use **PaginationParams** (`page`, `pageSize`, `search`, `sortBy`, `sortOrder`).
- Mutations validate FKs (e.g. city requires province) and return **201 / 400 / 404 / 409**.
- **OrderDetails** also adjust parent **Order** SubTotal/GrandTotal.

## Data store
EF Core uses PostgreSQL through Npgsql. Configure `ConnectionStrings__EcommerceDb` before starting the API; startup reports a clear configuration error if it is missing. Pending EF Core migrations are applied at startup. Data Protection keys are stored under the host's local application data directory by default and can be relocated with `DataProtection__KeyRingPath`; multi-instance deployments must share a persistent key ring to preserve cookie validation.

Restore the pinned EF CLI with `dotnet tool restore`. Create future schema changes with `dotnet ef migrations add <Name> --project Ecommerce.Api --output-dir Data/Migrations --context AppDbContext`. Set `ConnectionStrings__EcommerceDb` when applying migrations outside the application startup.

Registration and password-recovery challenges and idempotency records are still process-local. Notifications are persisted in PostgreSQL.

## Run
```bash
dotnet run
# Swagger: /swagger
# Header: X-Api-Key: demo-api-key-12345
```
