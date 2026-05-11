self.addEventListener('push', function (e) {
    const data = e.data ? e.data.json() : {};
    self.registration.showNotification(data.title || '🛒 Ranita', {
        body: data.body || 'Nouvelle notification',
        icon: '/images/logo.png',
        badge: '/images/logo.png',
        vibrate: [200, 100, 200]
    });
});