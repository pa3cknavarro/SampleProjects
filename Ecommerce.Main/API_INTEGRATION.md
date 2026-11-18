# API INTEGRATION

## Base Configuration

- API version: `v1`
- Base route: `/api/v1`
- Authentication: development-only `X-Api-Key` middleware
- Pagination: `page`, `pageSize`, `search`
- Error envelope: `traceId`, `status`, `title`, `detail`

## Endpoint Mapping

| Module | Method | Route | Frontend use | Status |
|---|---|---|---|---|
| Products | GET | `/api/v1/products` | Product search and catalog | Integrated |
| Products | GET | `/api/v1/products/{id}` | Direct product contract | Service available |
| Categories | GET | `/api/v1/categories` | Category contract | Service available |
| Provinces | GET | `/api/v1/provinces` | Cascading location selector | Integrated |
| Cities | GET | `/api/v1/cities` | Cascading location selector | Integrated |
| Barangays | GET | `/api/v1/barangays` | Cascading location selector | Integrated |
| Orders | POST | `/api/v1/orders` | Checkout order creation | Integrated |
| Orders | GET | `/api/v1/orders` | Not displayed by current UI | Service pending |
| Orders | GET | `/api/v1/orders/{id}` | Not displayed by current UI | Service pending |
| Cart items | GET/POST/PUT/DELETE | `/api/v1/cart-items` | Cart UI uses localStorage demo state | Pending backend identity/cart alignment |
| Customers | GET/POST | `/api/v1/customers` | Demo order uses existing seed customer | Pending full UI |
| Users | GET/POST/PUT/DELETE | `/api/v1/users` | No login implementation exists in API | Pending backend auth |
| Unit measures/prices | GET/POST/etc. | `/api/v1/unit-of-measures`, `/api/v1/unit-prices` | No seeded unit-price UI contract | Pending |
| Customer classes | GET/POST/etc. | `/api/v1/customer-classes` | Administrative functionality | Pending |
| Areas available | GET/POST/etc. | `/api/v1/areas-available` | Inventory location functionality | Pending |
| Provinces/cities/barangays | POST/PUT/DELETE | Related routes | Administrative functionality | Pending |
| Categories/products | POST/DELETE/PUT | Related routes | Not exposed in current customer UI | Pending |

## Shared Contracts

`Ecommerce.Main/src/api.ts` contains handwritten TypeScript contracts because the repository is new and no existing generated client existed. Repeatable Swagger generation is configured through `npm run generate:api` when the API is running.

Generated files should be stored at `Ecommerce.Main/src/services/generated/` and must not be edited manually.

## Error and Transaction Behavior

Frontend and backend use JSON error envelopes. Mutations use `X-Idempotency-Key` for order creation. HTTP 429 is handled as an API error through the centralized client. The backend should remain authoritative for inventory and monetary calculations.
