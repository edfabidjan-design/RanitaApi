const API_BASE_NOTIF = "https://ranitaapi-production.up.railway.app/api";

async function loadNavBadges() {
    try {
        // Commandes en attente
        const resOrders = await fetch(API_BASE_NOTIF + "/orders");
        const orders = await resOrders.json();
        const countOrders = orders.filter(o => o.status === "En attente").length;

        // Avis en attente
        let countReviews = 0;
        try {
            const resReviews = await fetch(API_BASE_NOTIF + "/reviews");
            const reviews = await resReviews.json();
            countReviews = reviews.filter(r => r.approuve === false).length;
        } catch (e) { }

        // Ajouter style une seule fois
        if (!document.getElementById("nav-badge-style")) {
            const style = document.createElement("style");
            style.id = "nav-badge-style";
            style.textContent = ".nav-badge-wrap { position: relative; display: inline-block; } .nav-badge { position: absolute; top: -10px; right: -12px; background: #ef4444; color: white; font-size: 11px; font-weight: 900; min-width: 20px; height: 20px; border-radius: 50px; display: inline-flex; align-items: center; justify-content: center; padding: 0 5px; z-index: 9999; pointer-events: none; }";
            document.head.appendChild(style);
        }

        // Mettre à jour ou créer les badges
        document.querySelectorAll("header nav a").forEach(function (link) {
            var href = link.getAttribute("href") || "";
            var count = 0;
            if (href.indexOf("admin-orders") !== -1) count = countOrders;
            if (href.indexOf("admin-reviews") !== -1) count = countReviews;

            var existingBadge = link.querySelector(".nav-badge") ||
                (link.parentElement.classList.contains("nav-badge-wrap") ? link.parentElement.querySelector(".nav-badge") : null);

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