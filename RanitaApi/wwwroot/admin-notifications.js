const API_BASE_NOTIF = "https://ranitaapi-production.up.railway.app/api";

async function loadNavBadges() {
    try {
        // ── Commandes ──
        const resOrders = await fetch(API_BASE_NOTIF + "/orders");
        const orders = await resOrders.json();

        // En attente + Confirmée par vendeur + Indisponible vendeur = nécessite action admin
        const countOrders = orders.filter(o =>
            o.status === "En attente" ||
            o.status === "Confirmée par vendeur" ||
            o.status === "Indisponible vendeur"
        ).length;

        // ── Avis en attente ──
        let countReviews = 0;
        try {
            const resReviews = await fetch(API_BASE_NOTIF + "/reviews");
            const reviews = await resReviews.json();
            countReviews = reviews.filter(r => r.approuve === false).length;
        } catch (e) { }

        // ── Vendeurs en attente ──
        let countVendors = 0;
        try {
            const resVendors = await fetch(API_BASE_NOTIF + "/admin/sellers?status=Pending");
            const vendors = await resVendors.json();
            countVendors = vendors.length;
        } catch (e) { }

        // ── Produits vendeurs en attente ──
        let countProducts = 0;
        try {
            const resProducts = await fetch(API_BASE_NOTIF + "/admin/sellers/products?status=Pending");
            const products = await resProducts.json();
            countProducts = products.length;
        } catch (e) { }

        // ── Clients inscrits aujourd'hui ──
        let countClients = 0;
        try {
            const resClients = await fetch(API_BASE_NOTIF + "/clients");
            const clients = await resClients.json();
            const today = new Date().toDateString();
            countClients = clients.filter(c => new Date(c.createdAt).toDateString() === today).length;
        } catch (e) { }

        // Ajouter style une seule fois
        if (!document.getElementById("nav-badge-style")) {
            const style = document.createElement("style");
            style.id = "nav-badge-style";
            style.textContent = `
                .nav-badge-wrap { position: relative; display: inline-block; }
                .nav-badge {
                    position: absolute; top: -10px; right: -12px;
                    background: #ef4444; color: white;
                    font-size: 11px; font-weight: 900;
                    min-width: 20px; height: 20px;
                    border-radius: 50px;
                    display: inline-flex; align-items: center; justify-content: center;
                    padding: 0 5px; z-index: 9999; pointer-events: none;
                }
            `;
            document.head.appendChild(style);
        }

        // Mettre à jour les badges nav
        document.querySelectorAll("header nav a").forEach(function (link) {
            var href = link.getAttribute("href") || "";
            var count = 0;

            if (href.indexOf("admin-orders") !== -1) count = countOrders;
            if (href.indexOf("admin-reviews") !== -1) count = countReviews;
            if (href.indexOf("admin-sellers") !== -1) count = countVendors + countProducts;
            if (href.indexOf("admin-clients") !== -1) count = countClients;

            var existingBadge = link.querySelector(".nav-badge") ||
                (link.parentElement.classList.contains("nav-badge-wrap")
                    ? link.parentElement.querySelector(".nav-badge")
                    : null);

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
setInterval(loadNavBadges, 20000); // toutes les 20 secondes

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
    } catch (e) {
        console.error('Push error:', e);
    }
}

window.addEventListener('load', registerPush);