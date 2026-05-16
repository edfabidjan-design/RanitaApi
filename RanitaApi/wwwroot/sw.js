const CACHE_NAME = "ranita-v20";
const URLS = ["/index.html", "/products.html", "/cart.html", "/products.css", "/header.css"];

self.addEventListener("install", e => {
    e.waitUntil(caches.open(CACHE_NAME).then(c => c.addAll(URLS)));
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
    // Ne jamais mettre en cache product-details.html
    if (e.request.url.includes("product-details.html")) {
        e.respondWith(fetch(e.request));
        return;
    }
    if (e.request.mode === "navigate") {
        e.respondWith(fetch(e.request).catch(() => caches.match(e.request)));
        return;
    }
    e.respondWith(caches.match(e.request).then(r => r || fetch(e.request)));
});

self.addEventListener('push', function (e) {
    const data = e.data ? e.data.json() : {};
    self.registration.showNotification(data.title || '🛒 Ranita', {
        body: data.body || 'Nouvelle notification',
        icon: '/images/logo.png',
        badge: '/images/logo.png',
        vibrate: [200, 100, 200]
    });
});