(function () {
    "use strict";

    function saveDataEnabled() {
        try {
            if (navigator.connection && navigator.connection.saveData) {
                return false;
            }
            var eff = navigator.connection && navigator.connection.effectiveType;
            if (eff === "2g" || eff === "slow-2g") {
                return false;
            }
        } catch (e) { }
        return true;
    }

    function idle(cb) {
        if (typeof requestIdleCallback === "function") {
            requestIdleCallback(function () { cb(); }, { timeout: 2500 });
        } else {
            setTimeout(cb, 1200);
        }
    }

    function warmCatalog() {
        if (!saveDataEnabled()) {
            return;
        }
        if (window.jobsyBanenkaartCatalog && typeof window.jobsyBanenkaartCatalog.prefetch === "function") {
            window.jobsyBanenkaartCatalog.prefetch().catch(function () { });
            return;
        }
        fetch("/api/banenkaart-client/all", {
            credentials: "same-origin",
            headers: { Accept: "application/json" }
        }).then(function (res) {
            if (!res.ok) {
                return null;
            }
            return res.text().then(function (body) {
                try {
                    sessionStorage.setItem("jobsy.bk.catalog.body.v1", body);
                    var etag = res.headers.get("ETag");
                    if (etag) {
                        sessionStorage.setItem("jobsy.bk.catalog.etag.v1", etag);
                    }
                    sessionStorage.setItem("jobsy.bk.catalog.at.v1", String(Date.now()));
                } catch (e) { }
            });
        }).catch(function () { });
    }

    function onLoad() {
        idle(warmCatalog);
    }

    if (document.readyState === "complete") {
        onLoad();
    } else {
        window.addEventListener("load", onLoad, { once: true });
    }
})();
