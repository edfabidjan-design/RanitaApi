// admin-auth.js — Ranita Admin

const PRESET_ROLES = {
    SuperAdmin: {
        label: '👑 Super Admin', color: '#7c3aed',
        pages: ['dashboard', 'products', 'categories', 'attributes', 'orders', 'clients', 'reviews', 'sellers', 'commissions', 'settings', 'users'],
        canEditOrders: true, canPayVendors: true, canDeleteProducts: true, canManageAdmins: true, canEditCommissions: true, readOnly: false
    },
    Analyste: {
        label: '📊 Analyste', color: '#6b7280',
        pages: ['dashboard', 'orders', 'clients', 'sellers', 'commissions'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: false, canManageAdmins: false, canEditCommissions: false, readOnly: true
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
    commissions: { label: 'Commissions', href: 'admin-commissions.html' },
    settings: { label: '⚙️ Paramètres', href: 'admin-settings.html' },
    settings: { label: '⚙️ Paramètres', href: 'admin-settings.html' },
    users: { label: '👑 Admins', href: 'admin-users.html' },
};

function parsePermissions(roleStr) {
    if (!roleStr) return PRESET_ROLES['Analyste'];
    if (roleStr.trim().startsWith('{')) {
        try {
            const c = JSON.parse(roleStr);
            const pages = c.pages || ['dashboard'];
            if (!pages.includes('dashboard')) pages.unshift('dashboard');
            return {
                label: c.label || '🔧 Personnalisé', color: c.color || '#0891b2', pages,
                canEditOrders: c.canEditOrders || false, canPayVendors: c.canPayVendors || false,
                canDeleteProducts: c.canDeleteProducts || false, canManageAdmins: c.canManageAdmins || false,
                canEditCommissions: c.canEditCommissions || false, readOnly: c.readOnly || false
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
        alert('Accès refusé.');
        window.location.href = 'admin-dashboard.html';
        return false;
    }
    return true;
}

function buildAdminNav(currentPage) {
    const perms = getPermissions();
    const pages = perms.pages.includes('dashboard') ? perms.pages : ['dashboard', ...perms.pages];

    // Nav desktop
    const navEl = document.getElementById('adminNav');
    if (navEl) {
        navEl.innerHTML = pages.filter(p => ALL_PAGES[p]).map(p => {
            const pg = ALL_PAGES[p];
            return `<a href="${pg.href}" ${p === currentPage ? 'class="active"' : ''}>${pg.label}</a>`;
        }).join('');
    }

    // Badge rôle
    const logo = document.querySelector('.admin-header .logo');
    if (logo && !document.querySelector('.role-badge')) {
        const badge = document.createElement('span');
        badge.className = 'role-badge';
        badge.style.cssText = `display:inline-block;margin-left:8px;padding:2px 8px;border-radius:20px;font-size:11px;font-weight:700;background:${perms.color}22;color:${perms.color};border:1px solid ${perms.color}44;vertical-align:middle;white-space:nowrap;`;
        badge.textContent = perms.label;
        logo.after(badge);
    }

    // Menu mobile — remplir les liens après chargement DOM
    const fillMobileNav = () => {
        const mobileNav = document.getElementById('mobileNav');
        if (!mobileNav) return;
        const linksDiv = mobileNav.querySelector('#mobileNavLinks');
        if (!linksDiv) return;
        linksDiv.innerHTML = pages.filter(p => ALL_PAGES[p]).map(p => {
            const pg = ALL_PAGES[p];
            const isActive = p === currentPage;
            return `<a href="${pg.href}" style="color:${isActive ? '#22c55e' : 'rgba(255,255,255,0.9)'};text-decoration:none;font-weight:600;font-size:16px;padding:16px;border-radius:10px;display:block;border-bottom:1px solid rgba(255,255,255,0.08);background:${isActive ? 'rgba(34,197,94,0.12)' : 'transparent'};">${pg.label}</a>`;
        }).join('');
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', fillMobileNav);
    } else {
        fillMobileNav();
    }
}

function toggleMobileMenu() {
    const menu = document.getElementById('mobileNav');
    const btn = document.getElementById('hamburgerBtn');
    if (!menu) return;
    const isOpen = menu.style.display === 'flex';
    if (isOpen) {
        menu.style.display = 'none';
        document.body.style.overflow = '';
        if (btn) btn.innerHTML = `<span style="display:block;width:22px;height:2px;background:white;border-radius:2px;"></span><span style="display:block;width:22px;height:2px;background:white;border-radius:2px;"></span><span style="display:block;width:22px;height:2px;background:white;border-radius:2px;"></span>`;
    } else {
        menu.style.display = 'flex';
        document.body.style.overflow = 'hidden';
        if (btn) btn.innerHTML = `<span style="display:block;width:22px;height:2px;background:white;border-radius:2px;transform:rotate(45deg) translate(5px,5px);"></span><span style="display:block;width:22px;height:2px;background:white;border-radius:2px;opacity:0;"></span><span style="display:block;width:22px;height:2px;background:white;border-radius:2px;transform:rotate(-45deg) translate(5px,-5px);"></span>`;
    }
}

function applyReadOnlyMode() {
    if (!getPermissions().readOnly) return;
    document.querySelectorAll('button').forEach(btn => {
        const onclick = btn.getAttribute('onclick') || '';
        const id = btn.id || '';
        if (!onclick.includes('logout') && id !== 'hamburgerBtn') btn.style.display = 'none';
    });
    const banner = document.createElement('div');
    banner.style.cssText = 'background:#fef3c7;color:#92400e;padding:10px 20px;font-size:13px;font-weight:700;text-align:center;';
    banner.textContent = '👁️ Mode lecture seule';
    const c = document.querySelector('.admin-container');
    if (c) c.insertBefore(banner, c.firstChild);
}

function adminLogout() {
    localStorage.removeItem('adminToken');
    localStorage.removeItem('adminRole');
    localStorage.removeItem('adminName');
    localStorage.removeItem('adminEmail');
    window.location.href = 'admin-login.html';
}