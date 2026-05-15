// ══════════════════════════════════════════════════════════════
// admin-auth.js — Permissions personnalisées par admin
// ══════════════════════════════════════════════════════════════

// Rôles prédéfinis (pour compatibilité + SuperAdmin)
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

// Toutes les pages disponibles
const ALL_PAGES = {
    dashboard: { label: '📊 Dashboard', href: 'admin-dashboard.html' },
    products: { label: '📦 Produits', href: 'admin-products.html' },
    categories: { label: '📁 Catégories', href: 'admin-categories.html' },
    attributes: { label: '🏷️ Attributs', href: 'admin-attributes.html' },
    orders: { label: '🧾 Commandes', href: 'admin-orders.html' },
    clients: { label: '👥 Clients', href: 'admin-clients.html' },
    reviews: { label: '⭐ Avis', href: 'admin-reviews.html' },
    sellers: { label: '🏪 Vendeurs', href: 'admin-sellers.html' },
    commissions: { label: '⚙️ Commissions', href: 'admin-commissions.html' },
    users: { label: '👑 Admins', href: 'admin-users.html' },
};

// Toutes les actions disponibles
const ALL_ACTIONS = {
    canEditOrders: { label: '✏️ Modifier statuts commandes' },
    canPayVendors: { label: '💸 Payer les vendeurs' },
    canDeleteProducts: { label: '🗑️ Supprimer des produits' },
    canManageAdmins: { label: '👑 Gérer les admins' },
    canEditCommissions: { label: '⚙️ Modifier les commissions' },
    readOnly: { label: '👁️ Mode lecture seule uniquement' },
};

// ── PARSER LES PERMISSIONS ────────────────────────────────────
// Role peut être:
// 1. "SuperAdmin" → preset
// 2. "Analyste" → preset
// 3. JSON string → permissions custom
function parsePermissions(roleStr) {
    if (!roleStr) return PRESET_ROLES['Analyste'];

    // Essayer de parser en JSON (permissions custom)
    if (roleStr.startsWith('{')) {
        try {
            const custom = JSON.parse(roleStr);
            return {
                label: custom.label || '🔧 Custom',
                color: custom.color || '#6b7280',
                pages: custom.pages || ['dashboard'],
                canEditOrders: custom.canEditOrders || false,
                canPayVendors: custom.canPayVendors || false,
                canDeleteProducts: custom.canDeleteProducts || false,
                canManageAdmins: custom.canManageAdmins || false,
                canEditCommissions: custom.canEditCommissions || false,
                readOnly: custom.readOnly || false,
            };
        } catch (e) {
            return PRESET_ROLES['Analyste'];
        }
    }

    // Rôle prédéfini
    return PRESET_ROLES[roleStr] || PRESET_ROLES['Analyste'];
}

// ── FONCTIONS UTILITAIRES ─────────────────────────────────────
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
    return parsePermissions(role);
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
    const perms = getPermissions();
    const navEl = document.querySelector('.admin-header nav') ||
        document.getElementById('adminNav');
    if (!navEl) return;

    navEl.innerHTML = perms.pages
        .filter(p => ALL_PAGES[p])
        .map(p => {
            const page = ALL_PAGES[p];
            const active = p === currentPage ? 'class="active"' : '';
            return `<a href="${page.href}" ${active}>${page.label}</a>`;
        }).join('');

    // Badge rôle
    const logo = document.querySelector('.admin-header .logo');
    if (logo && !logo.nextElementSibling?.classList?.contains('role-badge')) {
        const badge = document.createElement('span');
        badge.className = 'role-badge';
        badge.style.cssText = `display:inline-block;margin-left:10px;padding:3px 10px;border-radius:20px;font-size:11px;font-weight:700;background:${perms.color}22;color:${perms.color};border:1px solid ${perms.color}44;`;
        badge.textContent = perms.label;
        logo.after(badge);
    }
}

function applyReadOnlyMode() {
    if (!getPermissions().readOnly) return;
    document.querySelectorAll('button').forEach(btn => {
        const onclick = btn.getAttribute('onclick') || '';
        if (!onclick.includes('logout') && !btn.classList.contains('pbtn')) {
            btn.style.display = 'none';
        }
    });
    const banner = document.createElement('div');
    banner.style.cssText = 'background:#fef3c7;color:#92400e;padding:10px 20px;font-size:13px;font-weight:700;text-align:center;border-bottom:1px solid #fde68a;position:sticky;top:0;z-index:999;';
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