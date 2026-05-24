self.addEventListener('install', e => {
    self.skipWaiting();
});

self.addEventListener('activate', e => {
    e.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.map(k => caches.delete(k))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', e => {
    // Ne pas intercepter les requêtes cross-origin (Railway API)
    if (e.request.url.startsWith('http') &&
        !e.request.url.includes('ranita-shop.com')) {
        return; // Laisser passer sans intervention
    }
    // Pour les requêtes same-origin, aller directement au réseau
    e.respondWith(fetch(e.request));
});