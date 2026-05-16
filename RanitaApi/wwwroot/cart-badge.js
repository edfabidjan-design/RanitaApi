function updateCartBadge() {
    const cart = JSON.parse(localStorage.getItem('cart') || '[]');
    const total = cart.reduce((s, i) => s + (i.qty || i.quantity || 1), 0);
    ['mobCartBadge', 'cartBadgeHome', 'mobCartCount', 'cartCount'].forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            el.textContent = total;
            if (id === 'mobCartBadge') {
                el.style.display = total > 0 ? 'flex' : 'none';
            }
        }
    });
}
document.addEventListener('DOMContentLoaded', updateCartBadge);
window.addEventListener('storage', updateCartBadge);
updateCartBadge();