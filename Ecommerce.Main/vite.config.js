import base44 from "@base44/vite-plugin"
import react from '@vitejs/plugin-react'
// Resolves filesystem aliases consistently across this repository's split source folders.
import { fileURLToPath, URL } from 'node:url'
// Imports Vitest-aware config typing so test settings share the Vite config.
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  // Maps shared UI and application modules to their actual project locations.
  resolve: {
    // Checks the more specific component alias before the general source alias.
    alias: [
      // Resolves shared UI components stored beside src rather than inside src.
      { find: '@/components', replacement: fileURLToPath(new URL('./components', import.meta.url)) },
      // Resolves application libraries, pages, and feature modules under src.
      { find: '@', replacement: fileURLToPath(new URL('./src', import.meta.url)) },
    ],
  },
  // Preserves the existing Base44 and React development integrations.
  plugins: [
    base44({
      // Support for legacy code that imports the base44 SDK with @/integrations, @/entities, etc.
      // can be removed if the code has been updated to use the new SDK imports from @base44/sdk
      legacySDKImports: process.env.BASE44_LEGACY_SDK_IMPORTS === 'true',
      hmrNotifier: true,
      navigationNotifier: true,
      analyticsTracker: true,
      visualEditAgent: true
    }),
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
