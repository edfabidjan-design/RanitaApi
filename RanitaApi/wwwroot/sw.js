const CACHE_NAME = "ranita-v50";
const STATIC = ["/products.css", "/header.css", "/cart-badge.js"];

self.addEventListener("install", e => {
    e.waitUntil(caches.open(CACHE_NAME).then(c => c.addAll(STATIC)));
    self.skipWaiting();
});

self.addEventListener("activate", e => {
    e.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
        )
    );
    self.clients.claim();
});

self.addEventListener("fetch", e => {
    const url = e.request.url;
    // Ne JAMAIS mettre en cache les HTML — toujours depuis le réseau
    if (e.request.mode === "navigate" || url.endsWith(".html")) {
        e.respondWith(
            fetch(e.request).catch(() => caches.match("/index.html"))
        );
        return;
    }
    // Ne jamais mettre en cache les appels API
    if (url.includes("/api/")) {
        e.respondWith(fetch(e.request));
        return;
    }
    // CSS/JS depuis cache
    e.respondWith(caches.match(e.request).then(r => r || fetch(e.request)));
});

self.addEventListener('push', function(e) {
    const data = e.data ? e.data.json() : {};
    self.registration.showNotification(data.title || '🛒 Ranita', {
        body: data.body || 'Nouvelle notification',
        icon: '/images/logo.png',
        badge: '/images/logo.png',
        vibrate: [200, 100, 200]
    });
});
/* cache bust Sun May 17 14:07:45 UTC 2026 */
