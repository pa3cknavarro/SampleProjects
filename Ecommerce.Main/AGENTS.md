# Ecommerce.Main

## Project context

This is the React storefront and admin interface for `Ecommerce.Api`. Preserve the existing Vite, TypeScript, and centralized API-client patterns.

## Key files

- `src/`: frontend routes, API client, auth context, and pages.
- `components/`: shared storefront and admin components.
- `vite.config.js`: Vite aliases, API development proxy, and test configuration.
- `.env.local`: local-only environment values; never commit secrets.

## Development

- Use the `Ecommerce.Api` service for authentication and data operations; do not add a separate frontend backend.
- Keep credentials out of browser bundles and Vite environment variables.
- Surface API errors to the user rather than silently replacing failed requests with empty data.
- Run the relevant checks from `package.json` before finishing code changes.
