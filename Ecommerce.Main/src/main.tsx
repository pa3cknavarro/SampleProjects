// Enables React concurrent rendering safeguards for development and production.
import { StrictMode } from 'react'
// Provides the DOM entry point for mounting the application.
import { createRoot } from 'react-dom/client'
// Supplies the React Query cache used for server state.
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
// Imports the application component responsible for feature routing and views.
import App from './App'
// Loads the responsive global styling sheet.
import './styles.css'

// Creates one shared query cache for products, locations, and mutations.
const queryClient = new QueryClient({
  // Applies reasonable stale and retry defaults consistently across API queries.
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1 },
  },
})

// Finds the HTML mount point and renders the application beneath it.
createRoot(document.getElementById('root')!).render(
  // Runs lifecycle checks in development and wraps the full provider tree.
  <StrictMode>
    {/* Shares cached server state with every feature component. */}
    <QueryClientProvider client={queryClient}>
      {/* Renders the main e-commerce experience. */}
      <App />
    </QueryClientProvider>
  </StrictMode>,
)
