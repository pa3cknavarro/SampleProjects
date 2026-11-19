# Ecommerce Solution

A versioned ASP.NET Core 8 e-commerce API and a React/TypeScript storefront in one .NET solution. `Ecommerce.Api` owns business rules, data access, validation, Swagger contracts, and authentication. `Ecommerce.Main` provides the browser storefront and communicates only through HTTP.

## Repository structure

| Project | Purpose |
|---|---|
| `Ecommerce.Api` | Carter vertical-slice REST API, EF Core PostgreSQL database, Swagger, rate limiting, idempotency, CORS, and demo seed data |
| `Ecommerce.Main` | Visual Studio JavaScript project (`.esproj`) for the React 18 storefront, with Vite, TypeScript, TanStack Query, Zod, and Vitest |
| `Ecommerce.Api.Client` | Reusable typed .NET `HttpClient` wrapper |
| `Ecommerce.ConsoleClient` | .NET console API client sample |
| `Ecommerce.WebClient` | ASP.NET Core Razor Pages API client sample |
| `Ecommerce.Api.Tests` | xUnit/WebApplicationFactory API integration tests |

## Architecture

```mermaid
flowchart LR
    User[Browser user] --> React[Ecommerce.Main React UI]
    React --> Query[TanStack Query]
    React --> Zod[Zod order validation]
    React --> Client[Centralized API client]
    Client --> Proxy[Vite server API bridge]
    Proxy --> Middleware[API key and idempotency middleware]
    Middleware --> Carter[Carter API endpoints]
    Carter --> EF[Entity Framework Core]
    EF --> Store[PostgreSQL database]
```

The API persists domain data in PostgreSQL through Npgsql. Configure `ConnectionStrings__EcommerceDb` before starting the API; startup reports a clear error if it is missing. Pending EF Core migrations run at startup. `DataProtection__KeyRingPath` configures the persistent key ring used to protect authentication cookies.

## Integrated features

- Versioned API routes under `/api/v1`
- Product search and pagination
- Product and category contract models
- Province, city, and barangay cascading selection
- Browser-local shopping cart
- Zod-validated order submission
- Idempotency header for order creation
- Structured API error envelopes
- Request timeout and cancellation handling
- TanStack Query caching and mutation invalidation
- Swagger/OpenAPI contract generation
- Responsive desktop and mobile storefront layout
- API-backed storefront available at `/store`
- Email/password registration, verification, login, password recovery, and cookie sessions
- Persistent in-app notifications
- Configurable unit prices and per-province product availability, enforced at checkout
- Administrator-only management endpoints for backend entities

The demo cart is browser-local and is not yet synchronized with the API's cart-item endpoints. The storefront defaults to the first configured unit price; shoppers cannot yet choose among multiple units.

## API authentication

Protected API routes require:

```http
X-Api-Key: demo-api-key-12345
```

This is a demo API credential, separate from user authentication. User sessions use an HttpOnly cookie and administrator endpoints enforce the Admin role. The Vite development bridge adds the demo header server-side; browser JavaScript should not contain the credential.

The seeded demo administrator password and API key are for local development only and must be replaced before production.

## Prerequisites

- .NET 8 SDK
- Node.js and npm
- Visual Studio 2022 with the Node.js development workload (to open `Ecommerce.Main` as a project)
- A modern browser

## Restore and build

Restore the complete solution and build all .NET projects:

```bash
dotnet restore Ecommerce.sln
dotnet build Ecommerce.sln
```

Open `Ecommerce.sln` in Visual Studio to load `Ecommerce.Main` alongside the .NET projects. Select `Ecommerce.Main` as the startup project to run its Vite development server; install its npm dependencies first as described below.

Install frontend dependencies and build the production bundle:

```bash
cd Ecommerce.Main
npm install
npm run build
```

## Run the application

Start the API in Development mode so Swagger is enabled:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Ecommerce.Api --urls http://127.0.0.1:5001
```

In a second terminal, start the frontend:

```bash
cd Ecommerce.Main
npm run dev -- --host 127.0.0.1
```

- Frontend: `http://127.0.0.1:5173`
- API: `http://127.0.0.1:5001`
- Swagger UI: `http://127.0.0.1:5001/swagger`
- OpenAPI JSON: `http://127.0.0.1:5001/swagger/v1/swagger.json`

## Frontend environment

Copy the example file:

```bash
cp Ecommerce.Main/.env.example Ecommerce.Main/.env.local
```

The current `VITE_ECOMMERCE_API_URL` default is `/api/v1`. Keep this same-origin so development requests pass through the Vite server bridge. Vite variables are public and must never contain private credentials or database secrets.

## Generate TypeScript API contracts

Start the API, then run:

```bash
cd Ecommerce.Main
npm run generate:api
```

The command generates `src/services/generated/ecommerce-api.d.ts` from the live Swagger document. Generated files are not manually edited; regenerate them after API contract changes.

The handwritten service at `src/api.ts` remains the runtime HTTP boundary until generated request functions are adopted throughout the application.

## Tests

Run backend integration tests:

```bash
dotnet test Ecommerce.sln
```

The xUnit tests verify product API responses, API-key rejection, and the registration, verification, password recovery, and cookie-session flows.

Run frontend tests:

```bash
cd Ecommerce.Main
npm test
```

The Vitest/Testing Library suite verifies the primary storefront render and API-product integration boundary.

## API documentation

Main API resources include:

- `/api/v1/products`
- `/api/v1/categories`
- `/api/v1/provinces`
- `/api/v1/cities`
- `/api/v1/barangays`
- `/api/v1/customers`
- `/api/v1/orders`
- `/api/v1/cart-items`
- `/api/v1/users`
- `/api/v1/unit-of-measures`
- `/api/v1/unit-prices`
- `/api/v1/customer-classes`
- `/api/v1/areas-available`

See `Ecommerce.Main/API_INTEGRATION.md` for endpoint status and mappings.

## Project documentation

- `Ecommerce.Main/HOW_IT_WORKS.md` — frontend and backend architecture
- `Ecommerce.Main/INTEGRATION_PLAN.md` — audit, progress, risks, and implementation boundaries
- `Ecommerce.Main/API_INTEGRATION.md` — endpoint mapping and integration status
- `Ecommerce.Api/ARCHITECTURE.md` — API architecture notes
- `Ecommerce.Api/HOW_IT_WORKS.md` — API behavior notes

## Production readiness and limitations

Before production:

- Configure and back up the PostgreSQL database.
- Share a persistent Data Protection key ring between API instances.
- Replace the static API key with secret management suitable for the deployment environment.
- Move registration/password-recovery challenges and idempotency records to persistent/shared storage.
- Configure and verify a production SMTP provider.
- Configure production CORS origins explicitly.
- Serve the API through HTTPS.
- Synchronize cart state with the API and authenticated user.
- Add complete frontend CRUD flows only for supported backend endpoints.
- Add backend integration, authorization, transaction, frontend end-to-end, and browser tests.
- Configure external email, push, or SMS delivery if notifications should reach users outside the app.

## Verification

Current verification results:

- `dotnet build Ecommerce.sln` succeeds with no warnings
- `dotnet test Ecommerce.sln` passes 3 API integration tests
- `npm test` passes the frontend Vitest suite
- `npm run build` produces the React production bundle successfully
