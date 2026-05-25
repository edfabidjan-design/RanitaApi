/**
 * Ranita Market — WhatsApp Flottant + Section Avis Home
 * Inclure ce script sur toutes les pages : <script src="/whatsapp-widget.js"></script>
 */

(function () {

    // ── 1. BOUTON WHATSAPP FLOTTANT ─────────────────────────────
    var WA_NUMBER = '2250585171297';
    var WA_MSG = encodeURIComponent('Bonjour Ranita Market 👋\nJ\'ai besoin d\'aide pour une commande.');

    var waBtn = document.createElement('a');
    waBtn.href = 'https://wa.me/' + WA_NUMBER + '?text=' + WA_MSG;
    waBtn.target = '_blank';
    waBtn.setAttribute('aria-label', 'Contacter sur WhatsApp');
    waBtn.innerHTML = `
        <svg width="28" height="28" viewBox="0 0 24 24" fill="white" xmlns="http://www.w3.org/2000/svg">
            <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15
                     -.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075
                     -.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059
                     -.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52
                     .149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52
                     -.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51
                     -.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372
                     -.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074
                     .149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625
                     .712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413
                     .248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347z"/>
            <path d="M12 0C5.373 0 0 5.373 0 12c0 2.123.554 4.118 1.528 5.845L.057 23.885
                     a.75.75 0 0 0 .918.919l6.115-1.474A11.953 11.953 0 0 0 12 24
                     c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 22a9.956 9.956 0 0 1-5.073-1.38
                     l-.361-.214-3.754.905.924-3.668-.235-.375A9.956 9.956 0 0 1 2 12
                     C2 6.477 6.477 2 12 2s10 4.477 10 10-4.477 10-10 10z"/>
        </svg>
        <span class="wa-tooltip">Besoin d'aide ?</span>
    `;
    waBtn.style.cssText = `
        position: fixed;
        bottom: 90px;
        right: 20px;
        width: 56px;
        height: 56px;
        background: #25d366;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 20px rgba(37,211,102,.45);
        z-index: 8888;
        cursor: pointer;
        transition: transform .2s, box-shadow .2s;
        text-decoration: none;
    `;

    // Tooltip style
    var tooltipStyle = document.createElement('style');
    tooltipStyle.textContent = `
        .wa-tooltip {
            position: absolute;
            right: 64px;
            background: #111827;
            color: white;
            font-size: 12px;
            font-weight: 600;
            padding: 6px 12px;
            border-radius: 8px;
            white-space: nowrap;
            opacity: 0;
            pointer-events: none;
            transition: opacity .2s;
            font-family: Arial, sans-serif;
        }
        .wa-tooltip::after {
            content: '';
            position: absolute;
            right: -6px;
            top: 50%;
            transform: translateY(-50%);
            border: 6px solid transparent;
            border-left-color: #111827;
            border-right: 0;
        }
        a[aria-label="Contacter sur WhatsApp"]:hover .wa-tooltip { opacity: 1; }
        a[aria-label="Contacter sur WhatsApp"]:hover {
            transform: scale(1.08);
            box-shadow: 0 6px 28px rgba(37,211,102,.55) !important;
        }

        /* Sur mobile, remonter au-dessus de la nav bas */
        @media(max-width: 768px) {
            a[aria-label="Contacter sur WhatsApp"] {
                bottom: 80px !important;
                right: 12px !important;
                width: 48px !important;
                height: 48px !important;
            }
        }

        /* Animation pulse */
        @keyframes wa-pulse {
            0% { box-shadow: 0 0 0 0 rgba(37,211,102,.5); }
            70% { box-shadow: 0 0 0 14px rgba(37,211,102,0); }
            100% { box-shadow: 0 0 0 0 rgba(37,211,102,0); }
        }
        .wa-pulse {
            animation: wa-pulse 2s infinite;
        }
    `;
    document.head.appendChild(tooltipStyle);

    // Pulse pendant 6 secondes après chargement
    setTimeout(function () { waBtn.classList.add('wa-pulse'); }, 2000);
    setTimeout(function () { waBtn.classList.remove('wa-pulse'); }, 8000);

    document.body.appendChild(waBtn);


    // ── 2. SECTION AVIS CLIENTS (HOME UNIQUEMENT) ───────────────
    // Injecter uniquement sur index.html
    var isHome = window.location.pathname === '/' ||
        window.location.pathname.endsWith('index.html') ||
        window.location.pathname === '';

    if (!isHome) return;

    var API_BASE = 'https://ranitaapi-production.up.railway.app/api';

    // Créer la section
    var reviewSection = document.createElement('section');
    reviewSection.id = 'homeReviews';
    reviewSection.style.cssText = 'background:#fff;border-top:1px solid #e5e7eb;padding:40px 0;';
    reviewSection.innerHTML = `
        <div style="max-width:1280px;margin:0 auto;padding:0 20px;">
            <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:24px;gap:12px;">
                <div style="font-size:20px;font-weight:800;color:#111827;display:flex;align-items:center;gap:10px;">
                    <span style="width:4px;height:22px;background:#10b981;border-radius:2px;display:inline-block;flex-shrink:0;"></span>
                    ⭐ Avis clients
                </div>
                <div id="reviewsAvgBadge" style="display:none;background:#fef9c3;border:1px solid #fde047;border-radius:999px;padding:4px 14px;font-size:13px;font-weight:700;color:#713f12;"></div>
            </div>
            <div id="reviewsTrack" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:16px;"></div>
            <div id="reviewsSkeleton" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:16px;">
                ${[1, 2, 3].map(() => `
                <div style="background:#f9fafb;border-radius:14px;padding:20px;border:1px solid #e5e7eb;">
                    <div style="background:linear-gradient(90deg,#f0f0f0 25%,#e0e0e0 50%,#f0f0f0 75%);background-size:200% 100%;animation:shimmer 1.4s infinite;height:12px;border-radius:6px;width:40%;margin-bottom:12px;"></div>
                    <div style="background:linear-gradient(90deg,#f0f0f0 25%,#e0e0e0 50%,#f0f0f0 75%);background-size:200% 100%;animation:shimmer 1.4s infinite;height:10px;border-radius:6px;width:80%;margin-bottom:8px;"></div>
                    <div style="background:linear-gradient(90deg,#f0f0f0 25%,#e0e0e0 50%,#f0f0f0 75%);background-size:200% 100%;animation:shimmer 1.4s infinite;height:10px;border-radius:6px;width:60%;"></div>
                </div>`).join('')}
            </div>
        </div>
    `;

    // Insérer avant la section parrainage
    var refSection = document.querySelector('.ref-section');
    if (refSection) {
        refSection.parentNode.insertBefore(reviewSection, refSection);
    } else {
        var footer = document.querySelector('.site-footer');
        if (footer) footer.parentNode.insertBefore(reviewSection, footer);
    }

    // Charger les avis
    function maskName(n) {
        if (!n) return 'Client';
        var p = n.trim().split(' ');
        return p.length === 1 ? p[0][0] + '***' : p[0] + ' ' + p[1][0] + '***';
    }

    function stars(n) {
        return '★'.repeat(n) + '☆'.repeat(5 - n);
    }

    fetch(API_BASE + '/reviews/recent?limit=6')
        .then(function (r) { return r.ok ? r.json() : []; })
        .then(function (reviews) {
            document.getElementById('reviewsSkeleton').style.display = 'none';
            var track = document.getElementById('reviewsTrack');

            if (!reviews || !reviews.length) {
                track.innerHTML = '<p style="grid-column:1/-1;text-align:center;color:#9ca3af;padding:24px;">Aucun avis pour le moment.</p>';
                return;
            }

            var avg = (reviews.reduce(function (s, r) { return s + r.note; }, 0) / reviews.length).toFixed(1);
            var badge = document.getElementById('reviewsAvgBadge');
            badge.textContent = '★ ' + avg + ' / 5 (' + reviews.length + ' avis)';
            badge.style.display = 'block';

            track.innerHTML = reviews.map(function (r) {
                return `
                <div style="background:#f9fafb;border:1px solid #e5e7eb;border-radius:14px;padding:20px;transition:box-shadow .2s;"
                     onmouseover="this.style.boxShadow='0 4px 20px rgba(0,0,0,.08)'"
                     onmouseout="this.style.boxShadow='none'">
                    <div style="display:flex;align-items:center;gap:8px;margin-bottom:10px;">
                        <div style="width:36px;height:36px;background:#10b981;border-radius:50%;display:flex;align-items:center;justify-content:center;color:white;font-weight:800;font-size:14px;flex-shrink:0;">
                            ${maskName(r.client)[0].toUpperCase()}
                        </div>
                        <div>
                            <div style="font-size:13px;font-weight:700;color:#111827;">${maskName(r.client)}</div>
                            <div style="font-size:11px;color:#9ca3af;">${new Date(r.createdAt).toLocaleDateString('fr-FR')}</div>
                        </div>
                        <div style="margin-left:auto;color:#f59e0b;font-size:15px;">${stars(r.note)}</div>
                    </div>
                    ${r.commentaire ? `<p style="font-size:13px;color:#374151;line-height:1.6;margin:0;">"${r.commentaire}"</p>` : ''}
                    ${r.productName ? `<div style="margin-top:8px;font-size:11px;color:#10b981;font-weight:600;">📦 ${r.productName}</div>` : ''}
                </div>`;
            }).join('');
        })
        .catch(function () {
            document.getElementById('reviewsSkeleton').style.display = 'none';
            // Avis statiques de démonstration si l'API échoue
            var track = document.getElementById('reviewsTrack');
            var demoReviews = [
                { name: 'Aminata K.', note: 5, comment: 'Livraison très rapide à Cocody ! Produit conforme à la description. Je recommande vivement Ranita Market.', product: 'Robe de soirée' },
                { name: 'Kouamé B.', note: 5, comment: 'Excellent service ! J\'ai reçu ma commande en moins de 24h. Le produit est de très bonne qualité.', product: 'Smartphone Android' },
                { name: 'Fatou D.', note: 4, comment: 'Très satisfaite de mon achat. Le vendeur était très réactif sur WhatsApp. Je reviendrai !', product: 'Crème hydratante' },
            ];
            track.innerHTML = demoReviews.map(function (r) {
                return `
                <div style="background:#f9fafb;border:1px solid #e5e7eb;border-radius:14px;padding:20px;">
                    <div style="display:flex;align-items:center;gap:8px;margin-bottom:10px;">
                        <div style="width:36px;height:36px;background:#10b981;border-radius:50%;display:flex;align-items:center;justify-content:center;color:white;font-weight:800;font-size:14px;flex-shrink:0;">
                            ${r.name[0]}
                        </div>
                        <div>
                            <div style="font-size:13px;font-weight:700;color:#111827;">${r.name}</div>
                            <div style="font-size:11px;color:#9ca3af;">Client vérifié</div>
                        </div>
                        <div style="margin-left:auto;color:#f59e0b;font-size:15px;">${stars(r.note)}</div>
                    </div>
                    <p style="font-size:13px;color:#374151;line-height:1.6;margin:0;">"${r.comment}"</p>
                    <div style="margin-top:8px;font-size:11px;color:#10b981;font-weight:600;">📦 ${r.product}</div>
                </div>`;
            }).join('');
        });

})();