window.vacancyDetailMap = (function () {
    let map = null;
    let marker = null;

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
                "<img class=\"vacancy-detail-marker__img\" src=\"/images/brand/lobsy.png\" alt=\"\" width=\"48\" height=\"48\" />",
            iconSize: [48, 52],
            iconAnchor: [24, 50],
            popupAnchor: [0, -44]
        });
    }

    function init(elementId, options) {
        if (typeof L === "undefined") {
            throw new Error("Leaflet (L) is not loaded");
        }

        const el = document.getElementById(elementId);
        if (!el) {
            throw new Error("Map element #" + elementId + " not found");
        }

        const lat = Number(options && options.lat);
        const lng = Number(options && options.lng);
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            throw new Error("Invalid vacancy coordinates");
        }

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

        marker = L.marker([lat, lng], {
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

        map.setView([lat, lng], 15);

        [50, 200, 400].forEach(function (ms) {
            setTimeout(invalidate, ms);
        });

        window.addEventListener("resize", invalidate);
    }

    function invalidate() {
        if (map) {
            map.invalidateSize({ animate: false });
        }
    }

    function dispose() {
        window.removeEventListener("resize", invalidate);
        marker = null;
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
        const destLat = Number(options && options.destLat);
        const destLng = Number(options && options.destLng);
        if (!Number.isFinite(destLat) || !Number.isFinite(destLng)) {
            return { opened: false };
        }

        const mode = travelMode(options && options.transport);
        let url =
            "https://www.google.com/maps/dir/?api=1" +
            "&destination=" + encodeURIComponent(destLat + "," + destLng) +
            "&travelmode=" + encodeURIComponent(mode);

        const originLatRaw = options && options.originLat;
        const originLngRaw = options && options.originLng;
        if (originLatRaw != null && originLngRaw != null) {
            const originLat = Number(originLatRaw);
            const originLng = Number(originLngRaw);
            if (Number.isFinite(originLat) && Number.isFinite(originLng)) {
                url += "&origin=" + encodeURIComponent(originLat + "," + originLng);
            }
        }

        const win = window.open(url, "_blank", "noopener,noreferrer");
        return { opened: !!win };
    }

    /** Opens Google Street View at the company location. */
    function openStreetView(options) {
        const lat = Number(options && options.lat);
        const lng = Number(options && options.lng);
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return { opened: false };
        }

        const url =
            "https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=" +
            encodeURIComponent(lat + "," + lng);

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
