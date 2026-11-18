// Supplies React Testing Library helpers for mounting and querying the app.
import { render, screen } from '@testing-library/react'
// Supplies React Query's provider for isolated server-state tests.
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
// Imports Vitest test primitives and module mocking.
import { beforeEach, describe, expect, it, vi } from 'vitest'
// Imports the app component for the frontend smoke test.
import App from '../ApiStore'
// Imports the API module so its boundary can be mocked.
import * as apiModule from '../api'

vi.mock('../lib/AuthContext', () => ({
  useAuth: () => ({ user: null, logout: vi.fn() }),
}))

// Clears API mocks and uses realistic in-memory data for each test.
beforeEach(() => {
  vi.restoreAllMocks()
})

// Creates a query client isolated to one test render.
function renderApp() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

// Verifies the primary shop interface renders and exposes product data.
describe('Ecommerce Main', () => {
  // Verifies product rendering as the primary server-state integration behavior.
  it('renders products returned by the API', async () => {
    // Replaces the product network call with deterministic fixture data.
    vi.spyOn(apiModule.ecommerceApi, 'getProducts').mockResolvedValue({
      // Supplies one real-shaped product response item.
      items: [{
        // Uses a UUID-shaped product identifier.
        productId: '10000000-0000-0000-0000-000000000001',
        // Supplies the name that the test asserts is visible.
        productName: 'Cold Brew Coffee',
        // Supplies the API-shaped description.
        productDescription: 'Smooth cold brew.',
        // Supplies the category identifier.
        categoryId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
        // Marks the fixture product available.
        inStock: true,
      }],
      // Reports one matching product.
      totalCount: 1,
      // Reports the first response page.
      page: 1,
      // Reports the configured page size.
      pageSize: 12,
      // Reports one available page.
      totalPages: 1,
    })
    // Replaces province loading to keep this test isolated from location requests.
    vi.spyOn(apiModule.ecommerceApi, 'getProvinces').mockResolvedValue({
      // Provides an empty location list.
      items: [],
      // Reports no matching provinces.
      totalCount: 0,
      // Reports the first page.
      page: 1,
      // Reports the requested page size.
      pageSize: 100,
      // Reports no location pages.
      totalPages: 0,
    })
    // Mounts the application beneath an isolated React Query provider.
    renderApp()
    // Waits for the mocked product to reach the DOM and asserts its visibility.
    expect(await screen.findByText('Cold Brew Coffee')).toBeInTheDocument()
  })
})
