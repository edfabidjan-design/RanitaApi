// Redirection PWA vers homepage au relancement
(function () {
    if (window.matchMedia('(display-mode: standalone)').matches) {
        var t = localStorage.getItem('_pwa_ts');
        var now = Date.now();
        if (!t || (now - parseInt(t)) > 10000) {
            localStorage.setItem('_pwa_ts', now);
            if (window.location.pathname !== '/index.html' &&
                window.location.pathname !== '/') {
                window.location.replace('/index.html');
            }
        }
        localStorage.setItem('_pwa_ts', now);
    }
})();

let deferredPrompt;

window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    deferredPrompt = e;

    if (!sessionStorage.getItem('installDismissed')) {
        setTimeout(() => {
            if (!document.getElementById('installBanner')) {
                const banner = document.createElement('div');
                banner.id = 'installBanner';
                banner.innerHTML = `
                    <div style="display:flex;align-items:center;justify-content:space-between;max-width:600px;margin:0 auto;">
                        <div style="display:flex;align-items:center;gap:12px;">
                            <img src="/images/logo.png" style="width:40px;height:40px;border-radius:8px;">
                            <div>
                                <div style="font-weight:700;font-size:15px;">📱 Installer Ranita Market</div>
                                <div style="font-size:12px;color:#9ca3af;">Accès rapide depuis votre écran d'accueil</div>
                            </div>
                        </div>
                        <div style="display:flex;gap:8px;">
                            <button onclick="installApp()" style="background:#10b981;color:white;border:none;padding:10px 18px;border-radius:10px;font-weight:700;cursor:pointer;font-size:14px;">Installer</button>
                            <button onclick="dismissBanner()" style="background:#374151;color:white;border:none;padding:10px 14px;border-radius:10px;cursor:pointer;font-size:14px;">✕</button>
                        </div>
                    </div>
                `;
                banner.style.cssText = 'position:fixed;bottom:0;left:0;right:0;background:#111827;color:white;padding:16px 20px;z-index:9999;box-shadow:0 -4px 20px rgba(0,0,0,0.3);';
                document.body.appendChild(banner);
            }
        }, 3000);
    }
});

async function installApp() {
    if (!deferredPrompt) return;
    deferredPrompt.prompt();
    const result = await deferredPrompt.userChoice;
    deferredPrompt = null;
    document.getElementById('installBanner').style.display = 'none';
}

function dismissBanner() {
    document.getElementById('installBanner').style.display = 'none';
    sessionStorage.setItem('installDismissed', '1');
}

window.addEventListener('appinstalled', () => {
    const b = document.getElementById('installBanner');
    if (b) b.style.display = 'none';
});