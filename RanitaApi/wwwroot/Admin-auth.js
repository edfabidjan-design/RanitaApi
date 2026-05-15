// ══════════════════════════════════════════════════════════════
// admin-auth.js — Gestion centralisée des rôles admin Ranita
// À inclure dans le <head> de TOUTES les pages admin
// ══════════════════════════════════════════════════════════════

const ROLES = {
    SuperAdmin: { label: '👑 Super Admin', color: '#7c3aed' },
    GestionnaireCommandes: { label: '🧾 Gest. Commandes', color: '#2563eb' },
    GestionnaireVendeurs: { label: '🏪 Gest. Vendeurs', color: '#059669' },
    GestionnaireProduits: { label: '📦 Gest. Produits', color: '#0891b2' },
    GestionnairePaiements: { label: '💸 Gest. Paiements', color: '#65a30d' },
    GestionnaireClients: { label: '👥 Gest. Clients', color: '#d97706' },
    ModerateurAvis: { label: '⭐ Modérateur Avis', color: '#f59e0b' },
    GestionnaireLivraisons: { label: '🚚 Gest. Livraisons', color: '#6366f1' },
    GestionnaireParametres: { label: '⚙️ Gest. Paramètres', color: '#64748b' },
    Analyste: { label: '📊 Analyste', color: '#6b7280' }
};

const PERMISSIONS = {
    SuperAdmin: {
        pages: ['dashboard', 'products', 'categories', 'attributes', 'orders',
            'clients', 'reviews', 'sellers', 'commissions', 'users', 'deliveries'],
        canEditOrders: true, canPayVendors: true, canDeleteProducts: true,
        canManageAdmins: true, canEditCommissions: true, readOnly: false
    },
    GestionnaireCommandes: {
        pages: ['dashboard', 'orders', 'deliveries'],
        canEditOrders: true, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: false, readOnly: false
    },
    GestionnaireVendeurs: {
        pages: ['dashboard', 'sellers'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: false, readOnly: false
    },
    GestionnaireProduits: {
        pages: ['dashboard', 'products', 'categories', 'attributes', 'sellers'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: true,
        canManageAdmins: false, canEditCommissions: false, readOnly: false
    },
    GestionnairePaiements: {
        pages: ['dashboard', 'sellers', 'commissions'],
        canEditOrders: false, canPayVendors: true, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: true, readOnly: false
    },
    GestionnaireClients: {
        pages: ['dashboard', 'clients', 'orders'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: false, readOnly: false
    },
    ModerateurAvis: {
        pages: ['dashboard', 'reviews'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: false, readOnly: false
    },
    GestionnaireLivraisons: {
        pages: ['dashboard', 'orders', 'deliveries'],
        canEditOrders: true, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: false, readOnly: false
    },
    GestionnaireParametres: {
        pages: ['dashboard', 'categories', 'attributes', 'commissions'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: true, readOnly: false
    },
    Analyste: {
        pages: ['dashboard', 'orders', 'clients', 'sellers', 'commissions'],
        canEditOrders: false, canPayVendors: false, canDeleteProducts: false,
        canManageAdmins: false, canEditCommissions: false, readOnly: true
    }
};

const PAGE_NAV = {
    dashboard: { href: 'admin-dashboard.html', label: 'Dashboard' },
    products: { href: 'admin-products.html', label: 'Produits' },
    categories: { href: 'admin-categories.html', label: 'Catégories' },
    attributes: { href: 'admin-attributes.html', label: 'Attributs' },
    orders: { href: 'admin-orders.html', label: 'Commandes' },
    clients: { href: 'admin-clients.html', label: 'Clients' },
    reviews: { href: 'admin-reviews.html', label: '⭐ Avis' },
    sellers: { href: 'admin-sellers.html', label: 'Vendeurs' },
    commissions: { href: 'admin-commissions.html', label: '⚙️ Commissions' },
    users: { href: 'admin-users.html', label: '👑 Admins' },
};

function getAdminInfo() {
    return {
        token: localStorage.getItem('adminToken'),
        role: localStorage.getItem('adminRole') || 'Analyste',
        name: localStorage.getItem('adminName') || 'Admin',
        email: localStorage.getItem('adminEmail') || ''
    };
}

function getPermissions() {
    const { role } = getAdminInfo();
    return PERMISSIONS[role] || PERMISSIONS['Analyste'];
}

function can(action) {
    return getPermissions()[action] === true;
}

function hasPageAccess(page) {
    return getPermissions().pages.includes(page);
}

function checkPageAccess(currentPage) {
    const { token } = getAdminInfo();
    if (!token) { window.location.href = 'admin-login.html'; return false; }
    if (!hasPageAccess(currentPage)) {
        alert('Accès refusé. Vous n\'avez pas les permissions nécessaires.');
        window.location.href = 'admin-dashboard.html';
        return false;
    }
    return true;
}

function buildAdminNav(currentPage) {
    const { role } = getAdminInfo();
    const perms = getPermissions();
    const roleInfo = ROLES[role] || ROLES['Analyste'];

    const navEl = document.querySelector('.admin-header nav') ||
        document.getElementById('adminNav');
    if (!navEl) return;

    navEl.innerHTML = perms.pages
        .filter(p => PAGE_NAV[p])
        .map(p => {
            const nav = PAGE_NAV[p];
            const active = p === currentPage ? 'class="active"' : '';
            return `<a href="${nav.href}" ${active}>${nav.label}</a>`;
        }).join('');

    // Badge rôle
    const logo = document.querySelector('.admin-header .logo');
    if (logo && !logo.nextElementSibling?.classList?.contains('role-badge')) {
        const badge = document.createElement('span');
        badge.className = 'role-badge';
        badge.style.cssText = `display:inline-block;margin-left:10px;padding:3px 10px;border-radius:20px;font-size:11px;font-weight:700;background:${roleInfo.color}22;color:${roleInfo.color};border:1px solid ${roleInfo.color}44;`;
        badge.textContent = roleInfo.label;
        logo.after(badge);
    }
}

function applyReadOnlyMode() {
    if (!getPermissions().readOnly) return;
    document.querySelectorAll('button').forEach(btn => {
        if (!btn.onclick?.toString().includes('logout') && !btn.classList.contains('pbtn')) {
            btn.style.display = 'none';
        }
    });
    const banner = document.createElement('div');
    banner.style.cssText = 'background:#fef3c7;color:#92400e;padding:10px 20px;font-size:13px;font-weight:700;text-align:center;border-bottom:1px solid #fde68a;';
    banner.textContent = '👁️ Mode lecture seule — vous n\'avez pas les droits de modification';
    document.body.insertBefore(banner, document.body.firstChild);
}

function adminLogout() {
    localStorage.removeItem('adminToken');
    localStorage.removeItem('adminRole');
    localStorage.removeItem('adminName');
    localStorage.removeItem('adminEmail');
    window.location.href = 'admin-login.html';
}