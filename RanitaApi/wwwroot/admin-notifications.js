const API_BASE_NOTIF = "https://ranitaapi-production.up.railway.app/api";

async function loadNavBadges() {
    try {
        const res = await fetch(`${API_BASE_NOTIF}/orders`);
        const orders = await res.json();
        const count = orders.filter(o => o.status === "En attente").length;

        console.log("Badges - commandes en attente:", count);

        if (count === 0) return;

        const style = document.createElement("style");
        style.textContent = `
            .nav-badge-wrap { position: relative; display: inline-block; }
            .nav-badge {
                position: absolute;
                top: -10px;
                right: -12px;
                background: #ef4444;
                color: white;
                font-size: 11px;
                font-weight: 900;
                min-width: 20px;
                height: 20px;
                border-radius: 50px;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                padding: 0 5px;
                box-shadow: 0 2px 8px rgba(239,68,68,0.6);
                z-index: 9999;
                pointer-events: none;
            }
        `;
        document.head.appendChild(style);

        document.querySelectorAll("header nav a").forEach(link => {
            if (link.getAttribute("href")?.includes("admin-orders")) {
                const wrap = document.createElement("span");
                wrap.className = "nav-badge-wrap";
                link.parentNode.insertBefore(wrap, link);
                wrap.appendChild(link);

                const badge = document.createElement("span");
                badge.className = "nav-badge";
                badge.textContent = count;
                wrap.appendChild(badge);