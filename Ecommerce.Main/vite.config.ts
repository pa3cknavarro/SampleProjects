// Imports the Vite-compatible Vitest configuration type.
import { defineConfig } from 'vitest/config'
// Imports the React transform plugin used by Vite.
import react from '@vitejs/plugin-react'
// Imports Node's HTTP client for same-origin server-side API forwarding.
import http from 'node:http'

// Defines development server and test behavior for Ecommerce.Main.
export default defineConfig({
  // Enables the React transform and custom API middleware.
  plugins: [
    // Configures JSX and Fast Refresh support.
    react(),
    // Adds a server-only API bridge so browser code does not receive secrets.
    {
      // Names the plugin for diagnostics.
      name: 'ecommerce-api-proxy',
      // Registers middleware when the Vite development server starts.
      configureServer(server) {
        // Routes only /api requests through the ASP.NET Core backend.
        server.middlewares.use('/api', async (request, response) => {
          // Resolves the browser-relative path into a request URL.
          const url = new URL(request.url ?? '/', 'http://127.0.0.1:5173')
          // Builds the local ASP.NET Core upstream URL.
          const upstream = `http://127.0.0.1:5001${url.pathname}${url.search}`
          try {
            // Parses the upstream URL into HTTP request components.
            const upstreamUrl = new URL(upstream)
            // Opens an HTTP request that keeps the API key server-side.
            const upstreamRequest = http.request({
              // Uses plain HTTP for the documented local development target.
              protocol: upstreamUrl.protocol,
              // Targets the local API host.
              hostname: upstreamUrl.hostname,
              // Uses the API's local port.
              port: upstreamUrl.port,
              // Preserves the versioned path and query parameters.
              path: `${upstreamUrl.pathname}${upstreamUrl.search}`,
              // Forwards the browser HTTP method.
              method: request.method,
              // Supplies API JSON and authentication headers.
              headers: {
                // Declares the request content type.
                'Content-Type': 'application/json',
                // Injects the demo API key outside browser JavaScript.
                'X-Api-Key': 'demo-api-key-12345',
              },
            // Pipes the API response back to the browser.
            }, upstreamResponse => {
              // Mirrors the backend status code.
              response.statusCode = upstreamResponse.statusCode ?? 502
              // Mirrors the JSON content type or uses JSON as a fallback.
              response.setHeader(
                'Content-Type',
                upstreamResponse.headers['content-type']?.toString() ?? 'application/json',
              )
              // Streams the upstream body into the browser response.
              upstreamResponse.pipe(response)
            })
            // Converts upstream connection failures into a controlled gateway error.
            upstreamRequest.on('error', () => {
              // Marks the response as a bad gateway.
              response.statusCode = 502
              // Sends a normalized JSON proxy error.
              response.end(JSON.stringify({
                // Reports the gateway status.
                status: 502,
                // Names the API proxy failure.
                title: 'API unavailable',
                // Gives the UI a safe detail string.
                detail: 'The Ecommerce API proxy could not reach the backend.',
              }))
            })
            // Streams browser request content into the backend request.
            request.pipe(upstreamRequest)
          } catch {
            // Handles exceptions before the upstream request is sent.
            response.statusCode = 502
            // Sends the same normalized gateway error.
            response.end(JSON.stringify({
              // Reports the gateway status.
              status: 502,
              // Names the API proxy failure.
              title: 'API unavailable',
              // Gives the UI a safe detail string.
              detail: 'The Ecommerce API proxy could not reach the backend.',
            }))
          }
        })
      },
    },
  ],
  // Configures the frontend test runner.
  test: {
    // Uses a browser-like DOM for React component tests.
    environment: 'jsdom',
    // Loads shared test matchers before test files execute.
    setupFiles: ['./src/test/setup.ts'],
  },
  // Configures the development server listener.
  server: {
    // Uses the fixed frontend port allowed by the API CORS policy.
    // Aligns the frontend origin with the API's explicit CORS policy.
    port: 5173,
  },
})
