// Service Worker désactivé - se supprime automatiquement
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', e => {
    e.waitUntil(
        caches.keys().then(keys => Promise.all(keys.map(k => caches.delete(k))))
        .then(() => self.clients.claim())
        .then(() => self.clients.matchAll().then(clients => clients.forEach(c => c.postMessage('reload'))))
    );
});
self.addEventListener('fetch', e => e.respondWith(fetch(e.request)));
