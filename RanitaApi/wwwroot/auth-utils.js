function authFetch(url, options = {}) {
    const token = localStorage.getItem("token");
    if (!token) {
        window.location.href = "login.html?redirect=" + encodeURIComponent(window.location.pathname);
        return Promise.reject("Non connecté");
    }
    return fetch(url, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${token}`,
            ...(options.headers || {})
        }
    }).then(res => {
        if (res.status === 401) {
            localStorage.removeItem("token");
            localStorage.removeItem("client");
            window.location.href = "login.html";
            return Promise.reject("Session expirée");
        }
        return res;
    });
}