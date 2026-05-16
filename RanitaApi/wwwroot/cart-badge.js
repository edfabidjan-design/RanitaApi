function updateCartBadge() {
    const cart = JSON.parse(localStorage.getItem('cart') || '[]');
    const total = cart.reduce((s, i) => s + (i.qty || i.quantity || 1), 0);

    const badge = document.getElementById('mobCartBadge');
    if (badge) {
        badge.textContent = total;
        badge.style.display = total > 0 ? 'flex' : 'none';
    }

    const count = document.getElementById('cartCount');
    if (count) count.textContent = total;
}


window.addEventListener('storage', updateCartBadge);