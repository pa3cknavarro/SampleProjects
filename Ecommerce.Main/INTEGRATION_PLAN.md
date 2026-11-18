# Ecommerce.Main Integration Plan

## Audit Summary

- **Framework:** ASP.NET Core 8, Carter vertical slices, EF Core InMemory, API versioning `/api/v{version}/...`
- **Existing clients:** `Ecommerce.Api.Client` is a typed server-side .NET client. `Ecommerce.WebClient` is a Razor Pages sample.
- **React frontend:** No `Ecommerce.Main` project exists yet.
- **OpenAPI:** Swashbuckle Swagger is configured at `/swagger/v1/swagger.json` in Development.
- **Authentication:** Static `X-Api-Key: demo-api-key-12345` middleware. There is no JWT/session login flow.
- **Database:** InMemory database only; PostgreSQL is not currently implemented despite the integration brief.
- **CORS:** Not configured, so browser requests require an API-side CORS policy or a Vite development proxy.
- **Persistence:** API and idempotency state are process-local and demo-oriented.

## API Surface

The API exposes version 1 location, catalog, customer, order, cart, pricing, and user resources, including:

- Provinces, cities, and barangays
- Categories and products
- Unit of measures and unit prices
- Areas available
- Customer classes and customers
- Orders and order details
- Cart items
- Users

All list endpoints use `page`, `pageSize`, `search`, and commonly resource-specific filters. Mutations use JSON DTOs or entity payloads and static API-key authentication.

## Integration Mapping

| Frontend feature | API support | Implementation |
|---|---|---|
| Product browsing/search | `GET /api/v1/products` | React catalog page with pagination and search |
| Product details | `GET /api/v1/products/{id}` | React detail route |
| Categories | `GET /api/v1/categories` | Category filter and navigation |
| Shopping cart | `/api/v1/cart-items` | Cart API-backed feature |
| Checkout/orders | `POST /api/v1/orders` | Order form and confirmation |
| Location selection | Provinces/cities/barangays endpoints | Cascading address controls |
| Customer management | `/api/v1/customers` | Customer forms/listing |
| Demo security | Static API key middleware | Development-only proxy/header configuration |

No existing React mock or static frontend functionality can be mapped because `Ecommerce.Main` does not exist.

## Architecture

Create a Vite React TypeScript application under `Ecommerce.Main`:

1. Centralized typed API client
2. Feature API services
3. React hooks/context for loading and mutation state
4. Route pages for catalog, product detail, cart, checkout, and locations
5. Shared DTO, pagination, and error types
6. Vite proxy forwarding `/api` to the ASP.NET API
7. API CORS configuration as an alternative for direct browser access

## Initial Implementation Scope

- Scaffold React + TypeScript
- Configure environment-based API URLs and proxying
- Implement typed API errors and paginated responses
- Build catalog, product detail, cart, and checkout flows against real endpoints
- Build location and customer flows where practical
- Improve API CORS and Swagger metadata only where required for browser integration
- Add focused frontend and API tests
- Keep database, authentication, validation, and business rules in the API

## Implementation Progress

- Added `CORS` for explicit Vite development origins.
- Added a static API-key bypass for browser OPTIONS preflight.
- Seeded demo locations, category, products, unit of measure, customer class, customer, and user entities.
- Created the React storefront with catalog search, cart, checkout, and location selection.
- Added a centralized typed API client with timeout, cancellation, network, HTTP, and error-envelope handling.
- Added TanStack Query for products and cascading location server state.
- Added Zod validation for the order submission payload.
- Added Vite proxy support that injects the development API key server-side.
- Added Vitest and React Testing Library dependencies and an initial render test.
- Added xUnit integration-test project coverage for API authentication and pagination.
- Added Swagger generation command and contract documentation.
- Remaining: replace InMemory with PostgreSQL, replace static demo API key with a real authentication/authorization strategy, add full CRUD feature screens, and execute browser E2E tests.

## Current Risks and Limitations

- EF Core InMemory storage loses data on restart.
- Static API-key middleware is a demo mechanism, not user authentication.
- There is no role or permission authorization in the API.
- Cart state is browser-local and is not yet synchronized with `/api/v1/cart-items`.
- Product prices are currently demo UI constants because the API has no unit-price seed response integrated into the existing product DTO.
- The API has no PostgreSQL provider or migration configuration.
- Generated OpenAPI contracts require the API to be running during generation.
