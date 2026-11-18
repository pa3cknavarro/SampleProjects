// Adds DOM matchers used by React Testing Library assertions without modifying generated code.
import '@testing-library/jest-dom/vitest'

// Creates isolated in-memory browser storage for tests running without jsdom storage support.
const testStorage = new Map<string, string>()
// Defines the browser storage methods used by the API-backed storefront.
Object.defineProperty(globalThis, 'localStorage', {
  // Allows the test environment to replace storage during later setup if needed.
  configurable: true,
  // Provides deterministic storage methods without persisting test data on disk.
  value: {
    // Retrieves a stored value or returns null for an unset key.
    getItem: (key: string) => testStorage.get(key) ?? null,
    // Stores a value under the requested key.
    setItem: (key: string, value: string) => testStorage.set(key, value),
    // Removes one stored value.
    removeItem: (key: string) => testStorage.delete(key),
    // Clears all stored test values.
    clear: () => testStorage.clear(),
  },
})
