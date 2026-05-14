const API_BASE_NOTIF = "https://ranitaapi-production.up.railway.app/api";

async function loadNavBadges() {
    try {
        const currentPage = window.location.href;

        // ── Commandes ──
        let countOrders = 0;
        try {
            const resOrders = await fetch(API_BASE_NOTIF + "/orders");
            const orders = await resOrders.json();
            const total = orders.filter(o =>
                o.status === "En attente" ||
                o.status === "Confirmée par vendeur" ||
                o.status === "Indisponible vendeur"
            ).length;

            if (currentPage.indexOf('admin-orders') !== -1) {
                localStorage.setItem('badge-orders-seen', total);
                countOrders = 0;
            } else {
                const lastSeen = parseInt(localStorage.getItem('badge-orders-seen') || '0');
                countOrders = total > lastSeen ? total - lastSeen : 0;
            }
        } catch (e) { }

        // ── Avis en attente ──
        let countReviews = 0;
        try {
            const resReviews = await fetch(API_BASE_NOTIF + "/reviews");
            const reviews = await resReviews.json();
            const total = reviews.filter(r => r.approuve === false).length;

            if (currentPage.indexOf('admin-reviews') !== -1) {
                localStorage.setItem('badge-reviews-seen', total);
                countReviews = 0;
            } else {
                const lastSeen = parseInt(localStorage.getItem('badge-reviews-seen') || '0');
                countReviews = total > lastSeen ? total - lastSeen : 0;
            }
        } catch (e) { }

        // ── Vendeurs en attente ──
        let countVendors = 0;
        let countProducts = 0;
        try {
            const resVendors = await fetch(API_BASE_NOTIF + "/admin/sellers?status=Pending");
            const vendors = await resVendors.json();
            countVendors = vendors.length;
        } catch (e) { }
        try {
            const resProducts = await fetch(API_BASE_NOTIF + "/admin/sellers/products?status=Pending");
            const products = await resProducts.json();
            countProducts = products.length;
        } catch (e) { }

        const totalVendors = countVendors + countProducts;
        let displayVendors = 0;
        if (currentPage.indexOf('admin-sellers') !== -1) {
            localStorage.setItem('badge-vendors-seen', totalVendors);
            displayVendors = 0;
        } else {
            const lastSeen = parseInt(localStorage.getItem('badge-vendors-seen') || '0');
            displayVendors = totalVendors > lastSeen ? totalVendors - lastSeen : 0;
        }

        // ── Clients inscrits aujourd'hui ──
        let countClients = 0;
        try {
            const resClients = await fetch(API_BASE_NOTIF + "/clients");
            const clients = await resClients.json();
            const today = new Date().toDateString();
            const todayClients = clients.filter(c => new Date(c.createdAt).toDateString() === today).length;

            if (currentPage.indexOf('admin-clients') !== -1) {
                localStorage.setItem('badge-clients-seen', todayClients);
                countClients = 0;
            } else {
                const lastSeen = parseInt(localStorage.getItem('badge-clients-seen') || '0');
                countClients = todayClients > lastSeen ? todayClients - lastSeen : 0;
            }
        } catch (e) { }

        // ── Style badge ──
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

        // ── Mettre à jour les badges nav ──
        document.querySelectorAll("header nav a").forEach(function (link) {
            var href = link.getAttribute("href") || "";
            var count = 0;

            if (href.indexOf("admin-orders") !== -1) count = countOrders;
            if (href.indexOf("admin-reviews") !== -1) count = countReviews;
            if (href.indexOf("admin-sellers") !== -1) count = displayVendors;
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
setInterval(loadNavBadges, 20000);

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