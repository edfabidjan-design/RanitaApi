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

// Ne rien intercepter — laisser tout passer au réseau
self.addEventListener('fetch', e => {
    // Pas de e.respondWith() = le navigateur gère normalement
});