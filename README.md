# Ecommerce Solution

Vertical-slice ASP.NET Core 8 API (Carter) with typed client library and sample apps.

## Projects

| Project | Description |
|---------|-------------|
| **Ecommerce.Api** | REST API – locations, catalog, customers, orders |
| **Ecommerce.Api.Client** | Typed `HttpClient` wrapper |
| **Ecommerce.ConsoleClient** | Console sample |
| **Ecommerce.WebClient** | Razor Pages sample |

## API features

- Vertical slices (Carter modules)
- API versioning (`/api/v1/...`)
- Pagination, search
- Rate limiting (100/min per IP)
- Idempotency (`X-Idempotency-Key`)
- API key auth (`X-Api-Key: demo-api-key-12345`)
- Error envelopes + HATEOAS links
- EF Core InMemory (demo data seeded)

## Quick start

```bash
git clone https://github.com/pa3cknavarro/SampleProjects.git
cd SampleProjects

# API
cd Ecommerce.Api
dotnet restore
dotnet run
# Swagger: https://localhost:<port>/swagger

# Console client (update BaseUrl port in Program.cs if needed)
cd ../Ecommerce.ConsoleClient
dotnet run

# Web client
cd ../Ecommerce.WebClient
dotnet run
```

## Main endpoints (v1)

- `GET/POST /api/v1/provinces`
- `GET/POST /api/v1/cities`
- `GET/POST /api/v1/barangays`
- `GET/POST /api/v1/categories`
- `GET/POST /api/v1/products`
- `GET/POST /api/v1/customers`
- `GET/POST /api/v1/orders`
- `GET/POST /api/v1/unit-of-measures`
- `GET/POST /api/v1/customer-classes`

See `Ecommerce.Api/ARCHITECTURE.md` for design notes.
