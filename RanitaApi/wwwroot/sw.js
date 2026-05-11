const CACHE_NAME = "ranita-v1";
const URLS = ["/index.html", "/products.html", "/cart.html", "/products.css", "/header.css"];

self.addEventListener("install", e => {
    e.waitUntil(caches.open(CACHE_NAME).then(c => c.addAll(URLS)));
});

self.addEventListener("fetch", e => {
    e.respondWith(
        caches.match(e.request).then(r => r || fetch(e.request))
    );
});

// Notifications push admin
self.addEventListener('push', function (e) {
    const data = e.data ? e.data.json() : {};
    self.registration.showNotification(data.title || '🛒 Ranita', {
        body: data.body || 'Nouvelle notification',
        icon: '/images/logo.png',
        badge: '/images/logo.png',
        vibrate: [200, 100, 200]
    });
});