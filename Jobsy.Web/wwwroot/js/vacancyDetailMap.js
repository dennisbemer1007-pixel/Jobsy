window.vacancyDetailMap = (function () {
    let map = null;
    let marker = null;
    let currentLat = null;
    let currentLng = null;
    let popupHtml = "";
    let markerTitle = "Locatie";

    const TRAVEL_MODE = {
        Fiets: "bicycling",
        Auto: "driving",
        OV: "transit",
        Lopend: "walking"
    };

    function createMarkerElement() {
        const el = document.createElement("div");
        el.className = "vacancy-detail-marker";
        el.innerHTML =
            "<img class=\"vacancy-detail-marker__img\" src=\"/images/brand/lobsy.png?v=20260731-eyes\" alt=\"\" width=\"48\" height=\"48\" />";
        return el;
    }

    function readCoord(options, camel, pascal) {
        const raw = options && (options[camel] != null ? options[camel] : options[pascal]);
        if (raw == null || raw === "") {
            return NaN;
        }
        const n = typeof raw === "number" ? raw : Number(String(raw).trim().replace(",", "."));
        return n;
    }

    function restoreMarker() {
        if (!map || !Number.isFinite(currentLat) || !Number.isFinite(currentLng)) {
            return;
        }
        if (marker) {
            marker.remove();
            marker = null;
        }
        marker = new maplibregl.Marker({
            element: createMarkerElement(),
            anchor: "bottom",
            pitchAlignment: "viewport",
            rotationAlignment: "viewport"
        })
            .setLngLat([currentLng, currentLat])
            .addTo(map);
        marker.getElement().setAttribute("title", markerTitle);
        if (popupHtml) {
            marker.setPopup(new maplibregl.Popup({
                className: "vacancy-detail-map-popup",
                closeButton: true,
                maxWidth: "280px",
                offset: 18
            }).setHTML(popupHtml));
        }
    }

    function init(elementId, options) {
        if (typeof maplibregl === "undefined") {
            throw new Error("MapLibre GL JS (maplibregl) is not loaded");
        }
        if (!window.jobsyMapLibre) {
            throw new Error("jobsyMapLibre is not loaded");
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
        let useLat = lat;
        let useLng = lng;
        if (Math.abs(lat) <= 90 && Math.abs(lng) <= 180) {
            if (lat > 2 && lat < 10 && lng > 49 && lng < 55) {
                useLat = lng;
                useLng = lat;
            }
        }

        if (map) {
            dispose();
        }

        currentLat = useLat;
        currentLng = useLng;

        if (el.clientHeight < 40) {
            el.style.position = "absolute";
            el.style.inset = "0";
            el.style.width = "100%";
            el.style.height = "100%";
        }

        map = window.jobsyMapLibre.createMap(el, {
            center: [useLng, useLat],
            zoom: 15,
            scrollZoom: false,
            touchZoomRotate: false,
            styleKey: "liberty",
            controls: false
        });

        const address = options && options.address ? String(options.address) : "";
        markerTitle = (options && options.title) || "Locatie";
        const company = (options && options.company) || "";
        popupHtml = address
            ? "<strong>" + escapeHtml(company) + "</strong><br>" + escapeHtml(address)
            : "";

        map._jobsyOnStyleRestored = function () {
            restoreMarker();
            recenter();
        };

        map.on("load", function () {
            restoreMarker();
            invalidate();
            recenter();
        });

        map.once("idle", function () {
            invalidate();
            recenter();
        });

        [50, 150, 300, 600, 1200].forEach(function (ms) {
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
        map.jumpTo({
            center: [currentLng, currentLat],
            zoom: map.getZoom() || 15
        });
        if (marker) {
            marker.setLngLat([currentLng, currentLat]);
        }
    }

    function onResize() {
        invalidate();
        recenter();
    }

    function invalidate() {
        if (map) {
            map.resize();
        }
    }

    function dispose() {
        window.removeEventListener("resize", onResize);
        if (marker) {
            marker.remove();
            marker = null;
        }
        currentLat = null;
        currentLng = null;
        popupHtml = "";
        markerTitle = "Locatie";
        if (map) {
            map.remove();
            map = null;
        }
    }

    function travelMode(transport) {
        return TRAVEL_MODE[String(transport || "")] || TRAVEL_MODE.Fiets;
    }

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
