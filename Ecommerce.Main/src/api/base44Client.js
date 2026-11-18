// Imports the Base44 SDK factory used by the existing application backend.
import { createClient } from '@base44/sdk'
// Imports runtime app configuration populated from Base44 URL parameters and environment.
import { appParams } from '@/lib/app-params'

// Creates one Base44 client using runtime-provided values without embedding credentials.
export const base44 = createClient({
  // Uses the app ID supplied by Base44 or an empty development fallback.
  appId: appParams.appId ?? '',
  // Uses a URL-provided user token when the host supplies one.
  token: appParams.token ?? undefined,
  // Uses a configured local or hosted Base44 API endpoint when available.
  serverUrl: appParams.appBaseUrl ?? undefined,
  // Keeps deployed function versions aligned with the host configuration.
  functionsVersion: appParams.functionsVersion ?? undefined,
})
