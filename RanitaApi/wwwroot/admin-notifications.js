const API_BASE = "https://ranitaapi-production.up.railway.app/api";

async function loadNavBadges() {
    try {
        const style = document.createElement("style");
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
                box-shadow: 0 2px 6px rgba(239,68,68,0.5);
                animation: pulse-badge 1.5s infinite;
                pointer-events: none;
            }
            @keyframes pulse-badge {
                0%, 100% { transform: scale(1); }
                50% { transform: scale(1.15); }
            }
        `;
        document.head.appendChild(style);

        const [ordersRes, reviewsRes] = await Promise.all([
            fetch(`${API_BASE}/orders`).catch(() => null),
            fetch(`${API_BASE}/reviews`).catch(() => null)
        ]);

        const orders = ordersRes ? await ordersRes.json() : [];
        const reviews = reviewsRes ? await reviewsRes.json() : [];

        const pendingOrders = orders.filter(o => o.status === "En attente").length;
        const pendingReviews = reviews.filter(r => !r.approved && !r.rejected).length;

        const nav = document.querySelector("header nav");
        if (!nav) return;

        nav.querySelectorAll("a").forEach(link => {
            const href =