// ══════════════════════════════════════════════════════════════
// admin-auth.js — Permissions personnalisées + Menu mobile
// ══════════════════════════════════════════════════════════════

const PRESET_ROLES = {
    SuperAdmin: {
        label: '👑 Super Admin', color: '#7c3aed',
        pages: ['dashboard', 'products', 'categories', 'attributes', 'orders',
            'clients', 'reviews', 'sellers', 'commissions', 'users'],
        canEditOrders: true, canPayVendors: true, canDeleteProducts: true,
        canManageAdmins: true, canEditCommissions: true, readOnly: false
    },
    Analyste: {
        label: '📊 Analyste', color: '#6b7280',
        pages: ['dashboard', 'orders', 'clients', 'sellers', 'commissions'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: false, readOnly: true
    }
};

const ALL_PAGES = {
    dashboard: { label: 'Dashboard', href: 'admin-dashboard.html' },
    products: { label: 'Produits', href: 'admin-products.html' },
    categories: { label: 'Catégories', href: 'admin-categories.html' },
    attributes: { label: 'Attributs', href: 'admin-attributes.html' },
    orders: { label: 'Commandes', href: 'admin-orders.html' },
    clients: { label: 'Clients', href: 'admin-clients.html' },
    reviews: { label: '⭐ Avis', href: 'admin-reviews.html' },
    sellers: { label: 'Vendeurs', href: 'admin-sellers.html' },
    commissions: { label: '⚙️ Commissions', href: 'admin-commissions.html' },
    users: { label: '👑 Admins', href: 'admin-users.html' },
};

function parsePermissions(roleStr) {
    if (!roleStr) return PRESET_ROLES['Analyste'];
    if (roleStr.trim().startsWith('{')) {
        try {
            const custom = JSON.parse(roleStr);
            const pages = custom.pages || ['dashboard'];
            if (!pages.includes('dashboard')) pages.unshift('dashboard');
            return {
                label: custom.label || '🔧 Personnalisé', color: custom.color || '#0891b2', pages,
                canEditOrders: custom.canEditOrders || false, canPayVendors: custom.canPayVendors || false,
                canDeleteProducts: custom.canDeleteProducts || false, canManageAdmins: custom.canManageAdmins || false,
                canEditCommissions: custom.canEditCommissions || false, readOnly: custom.readOnly || false,
            };
        } catch (e) { return PRESET_ROLES['Analyste']; }
    }
    return PRESET_ROLES[roleStr] || PRESET_ROLES['Analyste'];
}

function getAdminInfo() {
    return {
        token: localStorage.getItem('adminToken'),
        role: localStorage.getItem('adminRole') || 'Analyste',
        name: localStorage.getItem('adminName') || 'Admin',
        email: localStorage.getItem('adminEmail') || ''
    };
}

function getPermissions() { return parsePermissions(getAdminInfo().role); }
function can(action) { return getPermissions()[action] === true; }

function hasPageAccess(page) {
    if (page === 'dashboard') return true;
    return getPermissions().pages.includes(page);
}

function checkPageAccess(currentPage) {
    const { token } = getAdminInfo();
    if (!token) { window.location.href = 'admin-login.html'; return false; }
    if (currentPage === 'dashboard') return true;
    if (!hasPageAccess(currentPage)) {
        alert('Accès refusé. Vous n\'avez pas les permissions nécessaires.');
        window.location.href = 'admin-dashboard.html';
        return false;
    }
    return true;
}

function buildAdminNav(currentPage) {
    const perms = getPermissions();
    const pages = perms.pages.includes('dashboard') ? perms.pages : ['dashboard', ...perms.pages];

    // ── Nav desktop ──────────────────────────────────────────
    const navEl = document.querySelector('.admin-header nav') || document.getElementById('adminNav');
    if (navEl) {
        navEl.innerHTML = pages.filter(p => ALL_PAGES[p]).map(p => {
            const page = ALL_PAGES[p];
            const active = p === currentPage ? 'class="active"' : '';
            return `<a href="${page.href}" ${active}>${page.label}</a>`;
        }).join('');
    }

    // ── Badge rôle ───────────────────────────────────────────
    const logo = document.querySelector('.admin-header .logo');
    if (logo && !document.querySelector('.role-badge')) {
        const badge = document.createElement('span');
        badge.className = 'role-badge';
        badge.style.cssText = `display:inline-block;margin-left:8px;padding:2px 8px;border-radius:20px;font-size:11px;font-weight:700;background:${perms.color}22;color:${perms.color};border:1px solid ${perms.color}44;vertical-align:middle;`;
        badge.textContent = perms.label;
        logo.after(badge);
    }

    // ── Hamburger + menu mobile (auto-injecté) ───────────────
    const header = document.querySelector('.admin-header');
    if (!header || document.querySelector('.hamburger')) return;

    // Bouton hamburger
    const hamburger = document.createElement('button');
    hamburger.className = 'hamburger';
    hamburger.setAttribute('aria-label', 'Menu');
    hamburger.style.cssText = 'display:none;flex-direction:column;gap:5px;cursor:pointer;padding:6px;border-radius:6px;background:rgba(255,255,255,0.1);border:none;margin-left:auto;';
    hamburger.innerHTML = `
        <span style="display:block;width:22px;height:2px;background:white;border-radius:2px;transition:all 0.3s;"></span>
        <span style="display:block;width:22px;height:2px;background:white;border-radius:2px;transition:all 0.3s;"></span>
        <span style="display:block;width:22px;height:2px;background:white;border-radius:2px;transition:all 0.3s;"></span>`;
    header.appendChild(hamburger);

    // Menu mobile fullscreen
    const mobileNav = document.createElement('div');
    mobileNav.id = 'mobileNav';
    mobileNav.style.cssText = `
        display:none;position:fixed;top:60px;left:0;right:0;bottom:0;
        background:#111827;z-index:999;overflow-y:auto;padding:16px;
        flex-direction:column;gap:4px;`;
    mobileNav.innerHTML = pages.filter(p => ALL_PAGES[p]).map(p => {
        const page = ALL_PAGES[p];
        const isActive = p === currentPage;
        return `<a href="${page.href}" style="color:${isActive ? '#22c55e' : 'rgba(255,255,255,0.85)'};text-decoration:none;font-weight:600;font-size:16px;padding:14px 16px;border-radius:10px;display:block;border-bottom:1px solid rgba(255,255,255,0.06);background:${isActive ? 'rgba(34,197,94,0.1)' : 'transparent'};">${page.label}</a>`;
    }).join('') + `
        <div style="margin-top:auto;padding-top:16px;border-top:1px solid rgba(255,255,255,0.1);">
            <button onclick="adminLogout()" style="width:100%;background:#ef4444;color:white;border:none;padding:14px;border-radius:10px;font-weight:700;font-size:15px;cursor:pointer;">🚪 Déconnexion</button>
        </div>`;
    document.body.appendChild(mobileNav);

    // Toggle
    hamburger.addEventListener('click', () => {
        const isOpen = mobileNav.style.display === 'flex';
        mobileNav.style.display = isOpen ? 'none' : 'flex';
        document.body.style.overflow = isOpen ? '' : 'hidden';
        // Animer hamburger → X
        const spans = hamburger.querySelectorAll('span');
        if (!isOpen) {
            spans[0].style.transform = 'rotate(45deg) translate(5px, 5px)';
            spans[1].style.opacity = '0';
            spans[2].style.transform = 'rotate(-45deg) translate(5px, -5px)';
        } else {
            spans[0].style.transform = ''; spans[1].style.opacity = '1'; spans[2].style.transform = '';
        }
    });

    // Fermer sur clic lien
    mobileNav.querySelectorAll('a').forEach(a => {
        a.addEventListener('click', () => {
            mobileNav.style.display = 'none';
            document.body.style.overflow = '';
        });
    });

    // Responsive
    const checkSize = () => {
        const isMobile = window.innerWidth <= 900;
        hamburger.style.display = isMobile ? 'flex' : 'none';
        if (!isMobile) {
            mobileNav.style.display = 'none';
            document.body.style.overflow = '';
            const spans = hamburger.querySelectorAll('span');
            spans[0].style.transform = ''; spans[1].style.opacity = '1'; spans[2].style.transform = '';
        }
    };
    checkSize();
    window.addEventListener('resize', checkSize);
}

function applyReadOnlyMode() {
    if (!getPermissions().readOnly) return;
    document.querySelectorAll('button').forEach(btn => {
        const onclick = btn.getAttribute('onclick') || '';
        const cls = btn.className || '';
        if (!onclick.includes('logout') && !cls.includes('pbtn') && !cls.includes('hamburger')) {
            btn.style.display = 'none';
        }
    });
    const banner = document.createElement('div');
    banner.style.cssText = 'background:#fef3c7;color:#92400e;padding:10px 20px;font-size:13px;font-weight:700;text-align:center;border-bottom:1px solid #fde68a;position:sticky;top:60px;z-index:998;';
    banner.textContent = '👁️ Mode lecture seule — vous n\'avez pas les droits de modification';
    const container = document.querySelector('.admin-container');
    if (container) container.insertBefore(banner, container.firstChild);
    else document.body.insertBefore(banner, document.body.children[1]);
}

function adminLogout() {
    localStorage.removeItem('adminToken');
    localStorage.removeItem('adminRole');
    localStorage.removeItem('adminName');
    localStorage.removeItem('adminEmail');
    window.location.href = 'admin-login.html';
}