# Ecommerce Solution

Vertical-slice ASP.NET Core API (Carter) + typed client + console + Razor web client.

## Projects
- **Ecommerce.Api** – API (locations, catalog, customers, orders)
- **Ecommerce.Api.Client** – typed HttpClient library
- **Ecommerce.ConsoleClient** – console sample
- **Ecommerce.WebClient** – Razor Pages sample

## Run API
```bash
cd Ecommerce.Api
dotnet run
```
Header required: `X-Api-Key: demo-api-key-12345`  
Swagger: `/swagger`  
Samples: `/samples/`

See `Ecommerce.Api/ARCHITECTURE.md` for details.
