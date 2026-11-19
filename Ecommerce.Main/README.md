# Ecommerce.Main

`Ecommerce.Main` is the React storefront and admin UI for `Ecommerce.Api`. The frontend uses Vite, TypeScript, TanStack Query, and a centralized HTTP client. User authentication is handled by the API using an HttpOnly cookie; the Vite development proxy adds the demo API key server-side.

## Prerequisites

- Node.js and npm
- .NET 8 SDK

## Run locally

Start the API from the repository root:

```powershell
dotnet run --project Ecommerce.Api --urls http://127.0.0.1:5001
```

In another terminal:

```powershell
cd Ecommerce.Main
npm install
npm run dev
```

Open the Vite URL, normally `http://localhost:5173`. The development proxy forwards `/api` requests to `http://127.0.0.1:5001`.

The Development API seed includes `demo` / `demo` for a customer and `admin` / `admin-local-only` for administrator access. These local-only credentials must be changed before deployment.

## Build and checks

```powershell
npm run typecheck
npm run lint
npm test
npm run build
```

## Configuration and limitations

`VITE_ECOMMERCE_API_URL` defaults to `/api/v1`. Keep it same-origin in production and never put private credentials in Vite environment variables. The API uses PostgreSQL; set `ConnectionStrings__EcommerceDb` before starting it. Startup applies EF Core migrations. Share a persistent `DataProtection__KeyRingPath` between API instances. Registration verification and password recovery require SMTP outside Development; their challenge tokens are currently held in process memory. Notifications are in-app only, and the cart remains browser-local.
