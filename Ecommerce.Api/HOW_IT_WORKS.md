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
EF Core **InMemory** provider for demos. Swap `UseInMemoryDatabase` for SQL Server/PostgreSQL in production.
Idempotency store is also in-process memory (use Redis in production).

## Run
```bash
dotnet run
# Swagger: /swagger
# Header: X-Api-Key: demo-api-key-12345
```
