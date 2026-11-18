const CACHE_VERSION = 'farmfur-v1';
const STATIC_CACHE = `${CACHE_VERSION}-static`;

// Install: activate immediately
self.addEventListener('install', () => {
  self.skipWaiting();
});

// Activate: clean old caches and claim clients
self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      const keys = await caches.keys();
      await Promise.all(
        keys.filter((key) => !key.startsWith(CACHE_VERSION)).map((key) => caches.delete(key))
      );
      await self.clients.claim();
    })()
  );
});

// Fetch: route by request type
self.addEventListener('fetch', (event) => {
  const { request } = event;

  // Only handle GET requests
  if (request.method !== 'GET') return;

  const url = new URL(request.url);

  // Skip cross-origin (API servers, CDNs, auth providers)
  if (url.origin !== self.location.origin) return;

  // Skip API and auth routes — never cache dynamic/private data
  if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/auth/')) return;

  // Navigation requests: network-first, fall back to cached app shell
  if (request.mode === 'navigate') {
    event.respondWith(
      (async () => {
        try {
          const response = await fetch(request);
          const cache = await caches.open(STATIC_CACHE);
          cache.put(request, response.clone());
          return response;
        } catch {
          const cached = await caches.match(request);
          if (cached) return cached;
          const cache = await caches.open(STATIC_CACHE);
          return (await cache.match('/')) || Response.error();
        }
      })()
    );
    return;
  }

  // Static assets: stale-while-revalidate (updates propagate on next visit)
  event.respondWith(
    (async () => {
      const cache = await caches.open(STATIC_CACHE);
      const cached = await cache.match(request);
      const networkFetch = fetch(request)
        .then((response) => {
          if (response.ok) cache.put(request, response.clone());
          return response;
        })
        .catch(() => cached);
      return cached || networkFetch;
    })()
  );
});
