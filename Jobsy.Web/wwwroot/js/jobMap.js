window.jobMap = (function () {
    let map = null;
    let clusterGroup = null;
    let markersById = {};
    let defaultIcon = null;
    let highlightIcon = null;
    let originMarker = null;
    let travelRingLayers = [];
    let travelOptions = { maxMinutes: 30, transport: "Fiets", radiusKm: 15 };
    let activeClusterPopup = null;
    let openCallback = null;

    const SPEED_M_PER_MIN = {
        // Keep in sync with MockRoutingService SpeedsKmPerHour
        Fiets: 18.0 * 1000 / 60,
        Auto: 40.0 * 1000 / 60,
        OV: 25.0 * 1000 / 60,
        Lopend: 5.0 * 1000 / 60
    };

    const TRANSPORT_LABEL = {
        Fiets: "fietsen",
        Auto: "rijden",
        OV: "OV",
        Lopend: "lopen"
    };

    function createIcon(highlighted) {
        return L.divIcon({
            className: highlighted ? "job-marker job-marker--active" : "job-marker",
            html: "<span class=\"job-marker__pin\"></span>",
            iconSize: [28, 36],
            iconAnchor: [14, 34],
            popupAnchor: [0, -30]
        });
    }

    function createClusterIcon(cluster) {
        const count = cluster.getChildCount();
        let sizeClass = "job-cluster--sm";
        if (count >= 10) sizeClass = "job-cluster--lg";
        else if (count >= 4) sizeClass = "job-cluster--md";

        return L.divIcon({
            html: "<div><span>" + count + "</span></div>",
            className: "job-cluster " + sizeClass,
            iconSize: L.point(44, 44)
        });
    }

    function formatWage(wage) {
        if (wage == null || wage === "") return "";
        return Number(wage).toFixed(2).replace(".", ",");
    }

    function wageHtml(v) {
        if (v.wageLabel) {
            return "<span class=\"map-popup__wage map-popup__wage--masked\">" + escapeHtml(v.wageLabel) + "</span>";
        }
        if (Array.isArray(v.wageBands) && v.wageBands.length > 0) {
            const rows = v.wageBands.map(function (b) {
                return "<tr><th>" + escapeHtml(String(b.label || b.ageYears || "")) + "</th>" +
                    "<td>€ " + formatWage(b.hourlyRate) + "</td></tr>";
            }).join("");
            return "<table class=\"map-popup__wage-table\"><tbody>" + rows + "</tbody></table>";
        }
        if (v.wage == null || v.wage === "") {
            return "";
        }
        return "<span class=\"map-popup__wage\">€ " + formatWage(v.wage) + "</span>";
    }

    function travelHtml(v) {
        if (v.travelMinutes == null) return "";
        const transport = escapeHtml(String(v.transportLabel || TRANSPORT_LABEL[v.transport] || "reistijd"));
        return (
            "<p class=\"map-popup__travel\">" +
                "<span class=\"travel-badge\">± " + escapeHtml(String(v.travelMinutes)) + " min " + transport + "</span>" +
            "</p>"
        );
    }

    function buildPopupHtml(v) {
        const transports = Array.isArray(v.transport) ? v.transport : [];
        const workTypes = Array.isArray(v.workTypes) ? v.workTypes : [];
        const badges = workTypes.concat(transports)
            .map(function (t) {
                return "<span class=\"map-popup__badge\">" + escapeHtml(t) + "</span>";
            })
            .join("");

        const hasImage = !!v.imageUrl;
        const mediaClass = hasImage ? "map-popup__media" : "map-popup__media map-popup__media--logo-only";

        let mediaInner = "";
        if (hasImage) {
            mediaInner +=
                "<img class=\"map-popup__photo\" src=\"" + escapeAttr(v.imageUrl) + "\" alt=\"\" loading=\"lazy\" />";
        }

        if (v.logoUrl) {
            mediaInner +=
                "<img class=\"map-popup__logo\" src=\"" + escapeAttr(v.logoUrl) + "\" alt=\"" +
                escapeAttr(v.company) + " logo\" loading=\"lazy\" />";
        }

        const detailHref = "/vacancies/" + encodeURIComponent(v.id);

        return (
            "<div class=\"map-popup\">" +
                "<div class=\"" + mediaClass + "\">" + mediaInner + "</div>" +
                "<div class=\"map-popup__body\">" +
                    "<div class=\"map-popup__top\">" +
                        "<h3 class=\"map-popup__title\">" + escapeHtml(v.title) + "</h3>" +
                        wageHtml(v) +
                    "</div>" +
                    "<p class=\"map-popup__company\">" + escapeHtml(v.company) + "</p>" +
                    "<p class=\"map-popup__address\">" + escapeHtml(v.address || "") + "</p>" +
                    travelHtml(v) +
                    (badges ? "<div class=\"map-popup__badges\">" + badges + "</div>" : "") +
                    "<div class=\"map-popup__actions\">" +
                        "<a class=\"map-popup__cta\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">Bekijk vacature</a>" +
                    "</div>" +
                "</div>" +
            "</div>"
        );
    }

    function buildClusterListHtml(childMarkers) {
        const items = childMarkers
            .map(function (marker) {
                const v = marker.options.jobData;
                if (!v) return "";
                const wage = v.wageLabel
                    ? escapeHtml(v.wageLabel)
                    : (Array.isArray(v.wageBands) && v.wageBands.length
                        ? v.wageBands.map(function (b) {
                            return escapeHtml(String(b.label || b.ageYears || "")) + " € " + formatWage(b.hourlyRate);
                        }).join(" · ")
                        : (v.wage == null || v.wage === "" ? "" : ("€ " + formatWage(v.wage))));
                return (
                    "<button type=\"button\" class=\"cluster-list__item\" data-job-id=\"" + escapeAttr(v.id) + "\">" +
                        "<span class=\"cluster-list__title\">" + escapeHtml(v.title) + "</span>" +
                        "<span class=\"cluster-list__meta\">" +
                            "<span>" + escapeHtml(v.company) + "</span>" +
                            (wage ? "<strong>" + wage + "</strong>" : "") +
                        "</span>" +
                    "</button>"
                );
            })
            .join("");

        return (
            "<div class=\"cluster-list\">" +
                "<div class=\"cluster-list__header\">" +
                    "<strong>" + childMarkers.length + " vacatures op deze locatie</strong>" +
                "</div>" +
                "<div class=\"cluster-list__items\">" + items + "</div>" +
            "</div>"
        );
    }

    function notifyOpen(id) {
        if (openCallback) {
            try {
                openCallback.invokeMethodAsync("OnMapVacancyOpened", id);
            } catch {
                // ignore disposed circuit
            }
        }
    }

    function openClusterList(clusterLayer) {
        const childMarkers = clusterLayer.getAllChildMarkers();
        const latlng = clusterLayer.getLatLng();

        if (activeClusterPopup) {
            map.closePopup(activeClusterPopup);
        }

        activeClusterPopup = L.popup({
            className: "job-cluster-popup",
            maxWidth: 360,
            minWidth: 300,
            autoPanPadding: [40, 40]
        })
            .setLatLng(latlng)
            .setContent(buildClusterListHtml(childMarkers))
            .openOn(map);

        const popupEl = activeClusterPopup.getElement();
        if (!popupEl) {
            return;
        }

        popupEl.querySelectorAll(".cluster-list__item").forEach(function (btn) {
            btn.addEventListener("click", function (ev) {
                ev.preventDefault();
                ev.stopPropagation();
                const id = btn.getAttribute("data-job-id");
                map.closePopup(activeClusterPopup);
                activeClusterPopup = null;
                focus(id);
            });
        });
    }

    function bindPopupClicks(marker, v) {
        marker.on("popupopen", function () {
            notifyOpen(v.id);
            const el = marker.getPopup() && marker.getPopup().getElement();
            if (!el) return;
            const cta = el.querySelector(".map-popup__cta");
            if (cta && !cta.dataset.bound) {
                cta.dataset.bound = "1";
                cta.addEventListener("click", function () {
                    notifyOpen(v.id);
                });
            }
        });
    }

    function ringMinutes(maxMinutes) {
        const max = Math.max(5, Number(maxMinutes) || 30);
        if (max <= 10) return [max];
        if (max <= 20) return [Math.round(max / 2), max];
        const step = max <= 40 ? 10 : Math.max(10, Math.round(max / 3 / 5) * 5);
        const rings = [];
        for (let m = step; m < max; m += step) {
            rings.push(m);
        }
        rings.push(max);
        return rings.slice(-3);
    }

    function metersPerMinute(transport) {
        return SPEED_M_PER_MIN[transport] || SPEED_M_PER_MIN.Fiets;
    }

    function clearTravelRings() {
        travelRingLayers.forEach(function (layer) {
            if (map && layer) {
                map.removeLayer(layer);
            }
        });
        travelRingLayers = [];
    }

    function maxRingRadiusMeters() {
        const speed = metersPerMinute(travelOptions.transport || "Fiets");
        const travelMeters = (Number(travelOptions.maxMinutes) || 30) * speed;
        const radiusKm = Number(travelOptions.radiusKm);
        if (Number.isFinite(radiusKm) && radiusKm > 0) {
            return Math.min(travelMeters, radiusKm * 1000);
        }
        return travelMeters;
    }

    function ringRadiusForMinutes(mins) {
        const speed = metersPerMinute(travelOptions.transport || "Fiets");
        const uncapped = mins * speed;
        const cap = maxRingRadiusMeters();
        return Math.min(uncapped, cap);
    }

    function drawTravelRings(lat, lng) {
        clearTravelRings();
        if (!map) return;

        const transport = travelOptions.transport || "Fiets";
        const labelVerb = TRANSPORT_LABEL[transport] || "reistijd";
        const minutes = ringMinutes(travelOptions.maxMinutes);
        const cap = maxRingRadiusMeters();

        minutes.forEach(function (mins, index) {
            const radius = ringRadiusForMinutes(mins);
            if (radius < 40) return;

            // Skip intermediate rings that collapse to the same capped size
            if (index > 0 && radius >= cap - 1 && ringRadiusForMinutes(minutes[index - 1]) >= cap - 1) {
                return;
            }

            const circle = L.circle([lat, lng], {
                radius: radius,
                color: "#2fbf6b",
                weight: index === minutes.length - 1 ? 2 : 1.5,
                opacity: 0.7,
                fillColor: "#2fbf6b",
                fillOpacity: 0.08 + index * 0.03,
                interactive: false
            }).addTo(map);

            const labelLat = lat + (radius / 111320);
            const effectiveMins = Math.max(1, Math.round(radius / metersPerMinute(transport)));
            const label = L.marker([labelLat, lng], {
                interactive: false,
                keyboard: false,
                icon: L.divIcon({
                    className: "travel-ring-label",
                    html: "<span>" + effectiveMins + " min " + escapeHtml(labelVerb) + "</span>",
                    iconSize: [120, 22],
                    iconAnchor: [60, 11]
                })
            }).addTo(map);

            travelRingLayers.push(circle, label);
        });
    }

    function normalizeTravelOptions(options) {
        if (!options) return;
        if (options.maxMinutes != null) {
            travelOptions.maxMinutes = Number(options.maxMinutes) || travelOptions.maxMinutes;
        }
        if (options.transport) {
            travelOptions.transport = String(options.transport);
        }
        if (options.radiusKm != null) {
            const rk = Number(options.radiusKm);
            if (Number.isFinite(rk) && rk > 0) {
                travelOptions.radiusKm = rk;
            }
        }
    }

    function fitMapToContent(markerBounds) {
        if (!map) return;

        const points = Array.isArray(markerBounds) ? markerBounds.slice() : [];
        if (originMarker) {
            const ll = originMarker.getLatLng();
            points.push([ll.lat, ll.lng]);

            const ringM = maxRingRadiusMeters();
            if (ringM > 0) {
                const dLat = ringM / 111320;
                const dLng = ringM / (111320 * Math.cos((ll.lat * Math.PI) / 180) || 1);
                points.push([ll.lat + dLat, ll.lng]);
                points.push([ll.lat - dLat, ll.lng]);
                points.push([ll.lat, ll.lng + dLng]);
                points.push([ll.lat, ll.lng - dLng]);
            }
        }

        if (points.length > 0) {
            map.fitBounds(points, { padding: [48, 48], maxZoom: 13 });
        } else {
            map.setView([52.07, 4.28], 11);
        }
    }

    function init(elementId, vacancies, options) {
        if (typeof L === "undefined") {
            throw new Error("Leaflet (L) is not loaded");
        }
        if (typeof L.markerClusterGroup !== "function") {
            throw new Error("Leaflet.markercluster is not loaded");
        }

        const el = document.getElementById(elementId);
        if (!el) {
            throw new Error("Map element #" + elementId + " not found");
        }

        if (map) {
            dispose();
        }

        openCallback = options && options.dotNetRef ? options.dotNetRef : null;
        normalizeTravelOptions(options && options.travel);

        defaultIcon = createIcon(false);
        highlightIcon = createIcon(true);

        map = L.map(el, {
            zoomControl: true,
            scrollWheelZoom: true
        });

        // Light Carto basemap — CSS filter in app.css applies Jobsy green tint
        L.tileLayer("https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png", {
            maxZoom: 19,
            attribution: "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> &copy; <a href=\"https://carto.com/attributions\">CARTO</a>"
        }).addTo(map);

        clusterGroup = L.markerClusterGroup({
            showCoverageOnHover: false,
            zoomToBoundsOnClick: false,
            spiderfyOnMaxZoom: false,
            disableClusteringAtZoom: 16,
            maxClusterRadius: 60,
            iconCreateFunction: createClusterIcon
        });

        clusterGroup.on("clusterclick", function (e) {
            openClusterList(e.layer);
        });

        setVacancies(vacancies || []);

        map.addLayer(clusterGroup);

        if (options && options.origin) {
            setOrigin(options.origin.lat, options.origin.lng, options.travel);
        }

        [50, 200, 500].forEach(function (ms) {
            setTimeout(invalidate, ms);
        });

        window.addEventListener("resize", invalidate);
    }

    function setVacancies(vacancies) {
        if (!clusterGroup) return;

        clusterGroup.clearLayers();
        markersById = {};
        const bounds = [];

        (vacancies || []).forEach(function (v) {
            const lat = Number(v.lat);
            const lng = Number(v.lng);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
                return;
            }

            const marker = L.marker([lat, lng], {
                icon: defaultIcon,
                jobData: v
            });

            marker.bindPopup(buildPopupHtml(v), {
                className: "job-map-popup",
                maxWidth: 440,
                minWidth: 400,
                autoPanPadding: [48, 48]
            });

            marker.on("click", function () {
                highlight(v.id);
            });

            bindPopupClicks(marker, v);

            markersById[v.id] = marker;
            clusterGroup.addLayer(marker);
            bounds.push([lat, lng]);
        });

        if (bounds.length > 0 || originMarker) {
            fitMapToContent(bounds);
        } else {
            map.setView([52.07, 4.28], 11);
        }
    }

    function setOrigin(lat, lng, travel) {
        if (!map) return;
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) return;

        normalizeTravelOptions(travel);

        if (originMarker) {
            originMarker.setLatLng([la, ln]);
        } else {
            originMarker = L.circleMarker([la, ln], {
                radius: 9,
                color: "#0b6e4f",
                weight: 3,
                fillColor: "#2fbf6b",
                fillOpacity: 1
            }).addTo(map);
            originMarker.bindTooltip("Jouw locatie", { direction: "top" });
        }

        drawTravelRings(la, ln);
        const markerBounds = Object.keys(markersById).map(function (id) {
            const ll = markersById[id].getLatLng();
            return [ll.lat, ll.lng];
        });
        fitMapToContent(markerBounds);
    }

    function setTravelOptions(options) {
        normalizeTravelOptions(options);
        if (originMarker) {
            const ll = originMarker.getLatLng();
            drawTravelRings(ll.lat, ll.lng);
            const markerBounds = Object.keys(markersById).map(function (id) {
                const mll = markersById[id].getLatLng();
                return [mll.lat, mll.lng];
            });
            fitMapToContent(markerBounds);
        }
    }

    function clearOrigin() {
        clearTravelRings();
        if (originMarker && map) {
            map.removeLayer(originMarker);
            originMarker = null;
        }
    }

    function invalidate() {
        if (map) {
            map.invalidateSize({ animate: false });
        }
    }

    function highlight(id) {
        Object.keys(markersById).forEach(function (key) {
            markersById[key].setIcon(key === id ? highlightIcon : defaultIcon);
        });
    }

    function focus(id) {
        const marker = markersById[id];
        if (!marker || !map || !clusterGroup) {
            return;
        }

        highlight(id);

        clusterGroup.zoomToShowLayer(marker, function () {
            marker.openPopup();
        });
    }

    function dispose() {
        window.removeEventListener("resize", invalidate);
        activeClusterPopup = null;
        openCallback = null;
        clearTravelRings();
        originMarker = null;
        if (clusterGroup) {
            clusterGroup.clearLayers();
            clusterGroup = null;
        }
        if (map) {
            map.remove();
            map = null;
        }
        markersById = {};
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;");
    }

    function escapeAttr(value) {
        return escapeHtml(value).replaceAll("'", "&#39;");
    }

    return {
        init,
        setVacancies,
        setOrigin,
        setTravelOptions,
        clearOrigin,
        highlight,
        focus,
        dispose,
        invalidate
    };
})();
