import react from '@vitejs/plugin-react'
// Resolves filesystem aliases consistently across this repository's split source folders.
import { fileURLToPath, URL } from 'node:url'
import path from 'node:path'
// Imports Vitest-aware config typing so test settings share the Vite config.
import { defineConfig } from 'vitest/config'

const projectRoot = fileURLToPath(new URL('.', import.meta.url))

// https://vite.dev/config/
export default defineConfig({
  // Resolves application aliases and transforms React components.
  plugins: [
    {
      name: 'ecommerce-main-path-alias',
      enforce: 'post',
      config(config) {
        const existingAliases = config.resolve?.alias
        const aliases = Array.isArray(existingAliases)
          ? existingAliases
          : Object.entries(existingAliases ?? {}).map(([find, replacement]) => ({ find, replacement }))
        config.resolve = {
          ...config.resolve,
          alias: [
            {
              find: /^@\/components\//,
              replacement: `${path.resolve(projectRoot, 'components')}${path.sep}`,
            },
            {
              find: /^@\//,
              replacement: `${path.resolve(projectRoot, 'src')}${path.sep}`,
            },
            ...aliases,
          ],
        }
      },
    },
    react(),
  ],
  // Configures API forwarding and test execution for the existing frontend.
  server: {
    // Serves the Vite frontend from the origin allowed by the API CORS policy.
    port: 5173,
    // Keeps the demo API key on the server side rather than exposing it in browser bundles.
    proxy: {
      // Forwards versioned API requests to the local ASP.NET Core process.
      '/api': {
        // Targets the API URL documented in the root README.
        target: 'http://127.0.0.1:5001',
        // Rewrites the Host header for the upstream API.
        changeOrigin: true,
        // Supplies the API's development-only key on server-proxied requests.
        headers: { 'X-Api-Key': 'demo-api-key-12345' },
      },
    },
  },
  // Configures the lightweight React component test environment.
  test: {
    // Provides browser-like DOM APIs for component tests.
    environment: 'jsdom',
    // Gives jsdom a non-opaque origin so browser storage APIs exist.
    environmentOptions: { jsdom: { url: 'http://localhost/' } },
    // Loads Testing Library matchers before the test files run.
    setupFiles: ['./src/test/setup.ts'],
  },
});
