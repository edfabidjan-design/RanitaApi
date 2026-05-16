function updateCartBadge() {
    const cart = JSON.parse(localStorage.getItem('cart') || '[]');
    const total = cart.reduce((s, i) => s + (i.qty || i.quantity || 1), 0);
    ['mobCartBadge', 'cartBadgeHome', 'mobCartCount'].forEach(id => {
        const el = document.getElementById(id);
        if (el) { el.textContent = total; el.style.display = total > 0 ? 'flex' : 'none'; }
    });
    const cartCount = document.getElementById('cartCount');
    if (cartCount) cartCount.textContent = total;
}
document.addEventListener('DOMContentLoaded', updateCartBadge);
window.addEventListener('storage', updateCartBadge);
updateCartBadge();