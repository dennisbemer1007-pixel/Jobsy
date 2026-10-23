(function () {
    "use strict";

    if (window.jobsyGeo && typeof window.jobsyGeo.requestLocation === "function") {
        return;
    }

    var STORAGE_KEY = "jobsy.origin";

    function getStoredOrigin() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) {
                return null;
            }
            var parsed = JSON.parse(raw);
            var lat = Number(parsed.lat);
            var lng = Number(parsed.lng);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
                return null;
            }
            var label = typeof parsed.label === "string" && parsed.label.trim()
                ? parsed.label.trim()
                : null;
            return { lat: lat, lng: lng, label: label };
        } catch (e) {
            return null;
        }
    }

    function setStoredOrigin(lat, lng, label) {
        var payload = {
            lat: Number(lat),
            lng: Number(lng),
            at: new Date().toISOString()
        };
        if (typeof label === "string" && label.trim()) {
            payload.label = label.trim();
        }
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
        } catch (eSet) { }
    }

    function requestLocation() {
        return new Promise(function (resolve, reject) {
            if (!window.isSecureContext && location.hostname !== "localhost" && location.hostname !== "127.0.0.1") {
                reject(new Error("Locatie delen vereist een beveiligde verbinding (HTTPS)."));
                return;
            }
            if (!navigator.geolocation) {
                reject(new Error("Geolocation niet beschikbaar in deze browser."));
                return;
            }
            navigator.geolocation.getCurrentPosition(
                function (pos) {
                    var lat = pos.coords.latitude;
                    var lng = pos.coords.longitude;
                    setStoredOrigin(lat, lng);
                    resolve({ lat: lat, lng: lng, Lat: lat, Lng: lng });
                },
                function (err) {
                    var message = "Locatie geweigerd.";
                    if (err) {
                        if (err.code === 1) {
                            message = "Locatietoegang geweigerd. Sta locatie toe in je browser.";
                        } else if (err.code === 2) {
                            message = "Locatie kon niet worden bepaald.";
                        } else if (err.code === 3) {
                            message = "Locatie ophalen duurde te lang.";
                        } else if (err.message) {
                            message = err.message;
                        }
                    }
                    reject(new Error(message));
                },
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
            );
        });
    }

    window.jobsyGeo = {
        getStoredOrigin: getStoredOrigin,
        setStoredOrigin: setStoredOrigin,
        requestLocation: requestLocation,
        whenReady: function () { return Promise.resolve(true); }
    };
})();
