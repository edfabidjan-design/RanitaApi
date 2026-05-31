const API_BASE_NOTIF = "https://ranitaapi-production.up.railway.app/api";

(function setupBeforeUnload() {
    const currentPage = window.location.href;
    window.addEventListener('beforeunload', async () => {
        if (currentPage.indexOf('admin-reviews') !== -1) {
            try {
                const res = await fetch(API_BASE_NOTIF + "/reviews");
                const reviews = await res.json();
                localStorage.setItem('badge-reviews-seen', reviews.filter(r => !r.approuve).length);
            } catch (e) { }
        }
        if (currentPage.indexOf('admin-clients') !== -1) {
            try {
                const res = await fetch(API_BASE_NOTIF + "/clients", { headers: { 'Authorization': 'Bearer ' + (getAdminInfo()?.token || '') } });
                const clients = await res.json();
                const today = new Date().toDateString();
                localStorage.setItem('badge-clients-seen', clients.filter(c => new Date(c.createdAt).toDateString() === today).length);
            } catch (e) { }
        }
    });
})();

async function loadNavBadges() {
    const { token } = getAdminInfo();
    if (!token) { window.location.href = 'admin-login.html'; return; }
    const headers = { 'Authorization': 'Bearer ' + token };

    try {
        // ── Commandes ──
        let countOrders = 0;
        try {
            const res = await fetch(API_BASE_NOTIF + "/orders", { headers });
            if (res.status === 401) { window.location.href = 'admin-login.html'; return; }
            const orders = await res.json();
            countOrders = orders.filter(o =>
                o.status === "En attente" || o.status === "Remboursement demandé"
            ).length;
        } catch (e) { }

        // ── Avis ──
        let countReviews = 0;
        try {
            const res = await fetch(API_BASE_NOTIF + "/reviews");
            const reviews = await res.json();
            const total = reviews.filter(r => r.approuve === false).length;
            const lastSeen = parseInt(localStorage.getItem('badge-reviews-seen') || '0');
            countReviews = total > lastSeen ? total - lastSeen : 0;
        } catch (e) { }

        // ── Vendeurs ──
        let displayVendors = 0;
        try {
            const token = getAdminInfo()?.token || '';
            const headers = { 'Authorization': 'Bearer ' + token };

            // Vendeurs en attente
            const r1 = await fetch(API_BASE_NOTIF + "/admin/sellers", { headers });
            const sellers = await r1.json();
            const pendingSellers = Array.isArray(sellers) ? sellers.filter(s => s.status === 'Pending').length : 0;

            // Produits à valider
            const r2 = await fetch(API_BASE_NOTIF + "/admin/sellers/products", { headers });
            const products = await r2.json();
            const pendingProducts = Array.isArray(products) ? products.filter(p => p.approvalStatus === 'Pending').length : 0;

            // Paiements en attente
            const r3 = await fetch(API_BASE_NOTIF + "/admin/sellers/payouts", { headers });
            const payouts = await r3.json();
            const pendingPayouts = Array.isArray(payouts) ? payouts.filter(p => p.status === 'Pending' && p.netAmount > 0).length : 0; const pendingPayouts = Array.isArray(payouts) ? payouts.filter(p => p.status === 'Pending').length : 0;

            displayVendors = pendingSellers + pendingProducts + pendingPayouts;
        } catch (e) { }

        // ── Clients ──
        let countClients = 0;
        try {
            const res = await fetch(API_BASE_NOTIF + "/clients", { headers });
            const clients = await res.json();
            const today = new Date().toDateString();
            const total = clients.filter(c => new Date(c.createdAt).toDateString() === today).length;
            const lastSeen = parseInt(localStorage.getItem('badge-clients-seen') || '0');
            countClients = total > lastSeen ? total - lastSeen : 0;
        } catch (e) { }

        // ── Style ──
        if (!document.getElementById("nav-badge-style")) {
            const style = document.createElement("style");
            style.id = "nav-badge-style";
            style.textContent = `
                .nav-badge-wrap { position: relative; display: inline-block; }
                .nav-badge {
                    position: absolute; top: -10px; right: -12px;
                    background: #ef4444; color: white;
                    font-size: 11px; font-weight: 900;
                    min-width: 20px; height: 20px; border-radius: 50px;
                    display: inline-flex; align-items: center; justify-content: center;
                    padding: 0 5px; z-index: 9999; pointer-events: none;
                }
            `;
            document.head.appendChild(style);
        }

        // ── Badges nav ──
        document.querySelectorAll("header nav a").forEach(function (link) {
            var href = link.getAttribute("href") || "";
            var count = 0;
            if (href.indexOf("admin-orders") !== -1) count = countOrders;
            if (href.indexOf("admin-reviews") !== -1) count = countReviews;
            if (href.indexOf("admin-sellers") !== -1) count = displayVendors;
            if (href.indexOf("admin-clients") !== -1) count = countClients;

            var existingBadge = link.querySelector(".nav-badge") ||
                (link.parentElement.classList.contains("nav-badge-wrap")
                    ? link.parentElement.querySelector(".nav-badge") : null);

            if (existingBadge) {
                if (count === 0) existingBadge.remove();
                else existingBadge.textContent = count;
            } else if (count > 0) {
                if (!link.parentElement.classList.contains("nav-badge-wrap")) {
                    var wrap = document.createElement("span");
                    wrap.className = "nav-badge-wrap";
                    link.parentNode.insertBefore(wrap, link);
                    wrap.appendChild(link);
                }
                var badge = document.createElement("span");
                badge.className = "nav-badge";
                badge.textContent = count;
                link.parentElement.appendChild(badge);
            }
        });

    } catch (e) {
        console.error("Badge erreur:", e);
    }
}

window.addEventListener("load", loadNavBadges);
setInterval(loadNavBadges, 15000);

// ── Push notifications admin ──
async function registerPush() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return;
    try {
        const reg = await navigator.serviceWorker.register('/sw.js');
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') return;
        const existing = await reg.pushManager.getSubscription();
        if (existing) return;
        const sub = await reg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: 'BK0OMo2QWE4SuKh0RTa6yvHfpkBXcPzL5sZkaJe3nNLesXQjRDhMzyimA8UNBCGvB9AOYpv_Q0RQrmgmA9YdNdY'
        });
        await fetch('https://ranitaapi-production.up.railway.app/api/notifications/subscribe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                endpoint: sub.endpoint,
                p256dh: btoa(String.fromCharCode(...new Uint8Array(sub.getKey('p256dh')))),
                auth: btoa(String.fromCharCode(...new Uint8Array(sub.getKey('auth'))))
            })
        });
        console.log('Push admin enregistré !');
    } catch (e) { console.error('Push error:', e); }
}

window.addEventListener('load', registerPush);