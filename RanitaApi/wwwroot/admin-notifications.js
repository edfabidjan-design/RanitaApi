const API_BASE_NOTIF = "https://ranitaapi-production.up.railway.app/api";

async function loadNavBadges() {
    try {
        const res = await fetch(API_BASE_NOTIF + "/orders");
        const orders = await res.json();
        const count = orders.filter(o => o.status === "En attente").length;

        // Supprimer anciens badges
        document.querySelectorAll(".nav-badge").forEach(b => b.remove());
        document.querySelectorAll(".nav-badge-wrap a").forEach(link => {
            link.parentNode.parentNode.insertBefore(link, link.parentNode);
            link.parentNode.remove();
        });

        if (count === 0) return;

        if (!document.getElementById("nav-badge-style")) {
            const style = document.createElement("style");
            style.id = "nav-badge-style";
            style.textContent = ".nav-badge-wrap { position: relative; display: inline-block; } .nav-badge { position: absolute; top: -10px; right: -12px; background: #ef4444; color: white; font-size: 11px; font-weight: 900; min-width: 20px; height: 20px; border-radius: 50px; display: inline-flex; align-items: center; justify-content: center; padding: 0 5px; z-index: 9999; pointer-events: none; }";
            document.head.appendChild(style);
        }

        document.querySelectorAll("header nav a").forEach(function (link) {
            if ((link.getAttribute("href") || "").indexOf("admin-orders") !== -1) {
                var wrap = document.createElement("span");
                wrap.className = "nav-badge-wrap";
                link.parentNode.insertBefore(wrap, link);
                wrap.appendChild(link);
                var badge = document.createElement("span");
                badge.className = "nav-badge";
                badge.textContent = count;
                wrap.appendChild(badge);
            }
        });

    } catch (e) {
        console.error("Badge erreur:", e);
    }
}

window.addEventListener("load", loadNavBadges);
setInterval(loadNavBadges, 30000);