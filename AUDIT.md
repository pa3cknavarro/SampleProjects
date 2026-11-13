# Solution Audit Summary

## Build status
All four projects compile successfully on .NET 8.

## Bugs fixed in this pass
1. **ConsoleClient** used invalid URL `https://localhost:7xxx` → now uses `ECOMMERCE_API_BASE_URL` or `https://localhost:5001`.
2. **Client DI** had no first-class way to trust local HTTPS certs → `acceptAnyServerCertificate` flag on `AddEcommerceApiClient`.
3. **WebClient** registration simplified to use that flag in Development.

## Known limitations (by design / demo scope)
| Item | Impact | Production guidance |
|------|--------|---------------------|
| EF InMemory | Data lost on restart | Use SQL Server / PostgreSQL |
| In-process IdempotencyStore | Not shared across instances | Redis or DB with TTL |
| User passwords plain text | Insecure | Hash with ASP.NET Identity / bcrypt |
| Rate limit by IP only | Proxies share IP | Key by API key or user id |
| JSON WriteIndented | Slightly larger payloads | Disable in production |
| No integration test suite | Regressions possible | Add WebApplicationFactory tests |

## Performance notes
- List endpoints use `AsNoTracking()` (good for read-only queries).
- Pagination is applied after filters; ensure DB indexes on search columns when moving off InMemory.
- Order GET uses `Include(OrderDetail)` – fine for single orders; avoid unbounded includes on lists.
- HttpClient is factory-managed (socket reuse) in client/web/console.

## Security notes
- API key is static demo value – replace with secrets store + per-client keys or JWT.
- CORS not configured (same-site / server-side clients only).
- Dangerous certificate validation only for local Development clients.

## Feature coverage
Locations, catalog, customers, orders, areas-available, cart-items, unit-prices, users, order-details all expose Carter routes under `/api/v1/...`.
