window.vacancyDetailMap = (function () {
    let map = null;
    let marker = null;
    let currentLat = null;
    let currentLng = null;

    const TRAVEL_MODE = {
        Fiets: "bicycling",
        Auto: "driving",
        OV: "transit",
        Lopend: "walking"
    };

    function createLobsyIcon() {
        return L.divIcon({
            className: "vacancy-detail-marker",
            html:
                "<img class=\"vacancy-detail-marker__img\" src=\"/images/brand/lobsy.png?v=20260731-eyes\" alt=\"\" width=\"48\" height=\"48\" />",
            iconSize: [48, 52],
            iconAnchor: [24, 50],
            popupAnchor: [0, -44]
        });
    }

    function readCoord(options, camel, pascal) {
        const raw = options && (options[camel] != null ? options[camel] : options[pascal]);
        if (raw == null || raw === "") {
            return NaN;
        }
        // Support invariant strings ("52.07") and numbers.
        const n = typeof raw === "number" ? raw : Number(String(raw).trim().replace(",", "."));
        return n;
    }

    function init(elementId, options) {
        if (typeof L === "undefined") {
            throw new Error("Leaflet (L) is not loaded");
        }

        const el = document.getElementById(elementId);
        if (!el) {
            throw new Error("Map element #" + elementId + " not found");
        }

        const lat = readCoord(options, "lat", "Lat");
        const lng = readCoord(options, "lng", "Lng");
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            throw new Error("Invalid vacancy coordinates");
        }
        // Guard against swapped lat/lng for NL/BE-ish data (lng around 3–8, lat around 50–54).
        let useLat = lat;
        let useLng = lng;
        if (Math.abs(lat) <= 90 && Math.abs(lng) <= 180) {
            // If values look swapped for the Low Countries, correct them.
            if (lat > 2 && lat < 10 && lng > 49 && lng < 55) {
                useLat = lng;
                useLng = lat;
            }
        }

        currentLat = useLat;
        currentLng = useLng;

        if (map) {
            dispose();
        }

        map = L.map(el, {
            zoomControl: true,
            scrollWheelZoom: false,
            dragging: !L.Browser.mobile
        });

        L.tileLayer("https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png", {
            maxZoom: 19,
            attribution:
                "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> " +
                "&copy; <a href=\"https://carto.com/attributions\">CARTO</a>"
        }).addTo(map);

        marker = L.marker([useLat, useLng], {
            icon: createLobsyIcon(),
            title: (options && options.title) || "Locatie"
        }).addTo(map);

        const address = options && options.address ? String(options.address) : "";
        if (address) {
            marker.bindPopup(
                "<strong>" + escapeHtml((options && options.company) || "") + "</strong>" +
                (address ? "<br>" + escapeHtml(address) : ""),
                { className: "vacancy-detail-map-popup" }
            );
        }

        map.setView([useLat, useLng], 15);

        [50, 200, 400, 800].forEach(function (ms) {
            setTimeout(function () {
                invalidate();
                recenter();
            }, ms);
        });

        window.addEventListener("resize", onResize);
    }

    function recenter() {
        if (!map || !Number.isFinite(currentLat) || !Number.isFinite(currentLng)) {
            return;
        }
        map.setView([currentLat, currentLng], map.getZoom() || 15, { animate: false });
        if (marker) {
            marker.setLatLng([currentLat, currentLng]);
        }
    }

    function onResize() {
        invalidate();
        recenter();
    }

    function invalidate() {
        if (map) {
            map.invalidateSize({ animate: false });
        }
    }

    function dispose() {
        window.removeEventListener("resize", onResize);
        marker = null;
        currentLat = null;
        currentLng = null;
        if (map) {
            map.remove();
            map = null;
        }
    }

    function travelMode(transport) {
        return TRAVEL_MODE[String(transport || "")] || TRAVEL_MODE.Fiets;
    }

    /**
     * Opens Google Maps directions for the selected transport mode.
     * Uses stored/passed origin when available; otherwise destination-only.
     */
    function openRoute(options) {
        const destLat = readCoord(options, "destLat", "DestLat");
        const destLng = readCoord(options, "destLng", "DestLng");
        const lat = Number.isFinite(destLat) ? destLat : currentLat;
        const lng = Number.isFinite(destLng) ? destLng : currentLng;
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return { opened: false };
        }

        const mode = travelMode(options && options.transport);
        let url =
            "https://www.google.com/maps/dir/?api=1" +
            "&destination=" + encodeURIComponent(lat + "," + lng) +
            "&travelmode=" + encodeURIComponent(mode);

        const originLat = readCoord(options, "originLat", "OriginLat");
        const originLng = readCoord(options, "originLng", "OriginLng");
        if (Number.isFinite(originLat) && Number.isFinite(originLng)) {
            url += "&origin=" + encodeURIComponent(originLat + "," + originLng);
        }

        const win = window.open(url, "_blank", "noopener,noreferrer");
        return { opened: !!win };
    }

    /** Opens Google Street View at the company location. */
    function openStreetView(options) {
        const lat = readCoord(options, "lat", "Lat");
        const lng = readCoord(options, "lng", "Lng");
        const useLat = Number.isFinite(lat) ? lat : currentLat;
        const useLng = Number.isFinite(lng) ? lng : currentLng;
        if (!Number.isFinite(useLat) || !Number.isFinite(useLng)) {
            return { opened: false };
        }

        const url =
            "https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=" +
            encodeURIComponent(useLat + "," + useLng);

        const win = window.open(url, "_blank", "noopener,noreferrer");
        return { opened: !!win };
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;");
    }

    return {
        init,
        dispose,
        invalidate,
        openRoute,
        openStreetView
    };
})();
