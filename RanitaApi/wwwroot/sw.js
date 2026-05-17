// Service Worker désactivé complètement
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
    // Toujours aller sur le réseau, jamais le cache
    e.respondWith(fetch(e.request));
});
