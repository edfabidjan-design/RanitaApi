const API_BASE = "https://ranitaapi-production.up.railway.app/api";

async function loadNavBadges() {
    try {
        // Commandes en attente
        const ordersRes = await fetch(`${API_BASE}/orders`);
        const orders = await ordersRes.json();
        const pendingOrders = orders.filter(o => o.status === "En attente").length;

        // Avis (optionnel, ignore si erreur)
        let pendingReviews = 0;
        try {
            const reviewsRes = await fetch(`${API_BASE}/reviews`);
            const reviews = await reviewsRes.json();
            pendingReviews = reviews.filter(r => !r.approved && !r.rejected).length;
        } catch (e) { }

        // Styles
        if (!document.getElementById("nav-badge-style")) {
            const style = document.createElement("style");
            style.id = "nav-badge-style";
            style.textContent = `
                .nav-badge-wrap { position: relative; display: inline-block; }
                .nav-badge {
                    position: absolute;
                    top: -8px;
                    right: -10px;
                    background: #ef4444;
                    color: white;
                    font-size: 10px;
                    font-weight: 800;
                    min-width: 18px;
                    height: 18px;
                    border-radius: 999px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    padding: 0 4px;
                    line-height: 1;
                    box-shadow: 0 2px 6px rgba(239,68,68,0.5);
                    animation: pulse-badge 1.5s infinite;
                    pointer-events: none;
                    z-index: 999;
                }
                @keyframes pulse-badge {
                    0%, 100% { transform: scale(1); }
                    50% { transform: scale(1.2); }
                }
            `;
            document.head.appendChild(style);
        }

        const nav = document.querySelector("header nav");
        if (!nav) return;

        nav.querySelectorAll("a").forEach(link => {
            const href = link.getAttribute("href") || "";
            let count = 0;
            if (href.includes("admin-orders")) count = pendingOrders;
            if (href.includes("admin-reviews")) count = pendingReviews;

            if (count > 0 && !link.parentElement.classList.contains("nav-badge-wrap")) {
                const wrap = document.createElement("span");
                wrap.className = "nav-badge-wrap";
                link.parentNode.insertBefore(wrap, link);
                wrap.appendChild(link);

                const badge = document.createElement("span");
                badge.className = "nav-badge";
                badge.textContent = count > 99 ? "99+" : count;
                wrap.appendChild(badge);
            }
        });

    } catch (e) {
        console.error("Badges nav erreur:", e);
    }
}

// Attendre que la page soit complètement chargée
window.addEventListener("load", loadNavBadges);