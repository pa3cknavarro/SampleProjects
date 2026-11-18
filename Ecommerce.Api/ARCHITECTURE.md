# Ecommerce API Architecture

## Style
Vertical Slice Architecture with Carter modules. Each feature owns its endpoints and DTOs under `Features/{Name}`.

## Cross-cutting
- **Auth**: `X-Api-Key` header (`demo-api-key-12345`) via `ApiKeyAuthMiddleware`
- **Versioning**: URL segment `/api/v{version}` (default v1)
- **Pagination**: `page`, `pageSize`, `search` query params; responses use `PaginatedResponse<T>`
- **Rate limiting**: fixed window 100 req/min per IP
- **Idempotency**: `X-Idempotency-Key` on write methods
- **Errors**: `ErrorEnvelope` JSON for failures
- **HATEOAS**: links on selected resources
- **Data**: EF Core PostgreSQL database (`AppDbContext`); configure `ConnectionStrings__EcommerceDb`
- **User sessions**: HttpOnly cookies; TOTP secrets are Data Protection encrypted, recovery codes are stored as hashes
- **Notifications**: user-scoped rows persisted in PostgreSQL

## Run
```bash
cd Ecommerce.Api
dotnet run
```
Swagger at `/swagger`. Samples under `/samples/` (if present).
