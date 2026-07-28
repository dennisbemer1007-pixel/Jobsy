window.jobMap = (function () {
    let map = null;
    let clusterGroup = null;
    let markersById = {};
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

    function workTypeGlyph(workType) {
        const t = String(workType || "").toLowerCase();
        // Simple emoji glyphs for branch recognition on the map
        if (t.indexOf("horeca") >= 0) return "☕";
        if (t.indexOf("winkel") >= 0 || t.indexOf("retail") >= 0 || t.indexOf("supermarkt") >= 0) return "🛒";
        if (t.indexOf("logistiek") >= 0) return "📦";
        if (t.indexOf("zorg") >= 0) return "✚";
        if (t.indexOf("kantoor") >= 0) return "💼";
        if (t.indexOf("bouw") >= 0) return "🔧";
        if (t.indexOf("tuinbouw") >= 0) return "🌿";
        if (t.indexOf("schoonmaak") >= 0) return "✨";
        if (t.indexOf("productie") >= 0) return "🏭";
        return "●";
    }

    function createIcon(highlighted, workType) {
        const glyph = workTypeGlyph(workType);
        return L.divIcon({
            className: highlighted ? "job-marker job-marker--active" : "job-marker",
            html: "<span class=\"job-marker__glyph\" aria-hidden=\"true\">" + glyph + "</span>",
            iconSize: [34, 34],
            iconAnchor: [17, 17],
            popupAnchor: [0, -18]
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

    function isNarrowViewport() {
        return (window.innerWidth || 0) <= 768;
    }

    function jobPopupOptions(withWagePanel) {
        const vw = window.innerWidth || 360;
        const narrow = isNarrowViewport();
        // Keep room for vacancy + wage panel side-by-side, also on small screens.
        const maxWidth = narrow
            ? Math.max(260, Math.min(withWagePanel ? 380 : 300, vw - 20))
            : (withWagePanel ? 460 : 340);
        return {
            className: "job-map-popup" + (withWagePanel ? " job-map-popup--with-wages" : ""),
            maxWidth: maxWidth,
            minWidth: narrow
                ? Math.min(withWagePanel ? 300 : 240, maxWidth)
                : (withWagePanel ? 400 : 280),
            autoPanPadding: narrow ? [12, 56] : [36, 48],
            keepInView: true,
            closeOnClick: true,
            closeButton: true
        };
    }

    function clusterPopupOptions(withWagePanel) {
        const vw = window.innerWidth || 360;
        const vh = window.innerHeight || 640;
        const narrow = isNarrowViewport();
        // Wider card on phones; leave a little room for the external close button.
        const maxWidth = narrow
            ? Math.max(280, Math.min(withWagePanel ? 360 : 340, vw - 16))
            : (withWagePanel ? 420 : 360);
        return {
            className: "job-cluster-popup" + (withWagePanel ? " job-cluster-popup--with-wages" : ""),
            maxWidth: maxWidth,
            minWidth: narrow
                ? Math.min(withWagePanel ? 300 : 280, maxWidth)
                : (withWagePanel ? 360 : 300),
            // Extra padding so pager + external close stay inside the map on phones.
            autoPanPadding: narrow
                ? [18, Math.max(80, Math.round(vh * 0.09))]
                : [36, 48],
            keepInView: true
        };
    }

    function hasWageBands(v) {
        return Array.isArray(v.wageBands) && v.wageBands.length > 0;
    }

    function wageInlineHtml(v) {
        if (v.wageLabel) {
            return "<span class=\"map-popup__wage map-popup__wage--masked\">" + escapeHtml(v.wageLabel) + "</span>";
        }
        if (hasWageBands(v)) {
            // Per-age table lives in the side panel, not inline.
            return "";
        }
        if (v.wage == null || v.wage === "") {
            return "";
        }
        return "<span class=\"map-popup__wage\">€ " + formatWage(v.wage) + "</span>";
    }

    function wageTableRowsHtml(bands) {
        return bands.map(function (b) {
            return "<tr><th>" + escapeHtml(String(b.label || b.ageYears || "")) + "</th>" +
                "<td>€ " + formatWage(b.hourlyRate) + "</td></tr>";
        }).join("");
    }

    function wagePanelHtml(v) {
        if (!hasWageBands(v)) return "";
        return (
            "<aside class=\"map-popup__wage-panel\" aria-label=\"Uurlonen per leeftijd\">" +
                "<p class=\"map-popup__wage-panel-title\">Uurlonen</p>" +
                "<table class=\"map-popup__wage-table\"><tbody>" + wageTableRowsHtml(v.wageBands) + "</tbody></table>" +
            "</aside>"
        );
    }

    function clusterWageHtml(v) {
        if (v.wageLabel) {
            return "<span class=\"cluster-list__wage cluster-list__wage--masked\">" + escapeHtml(v.wageLabel) + "</span>";
        }
        // Cluster list stays compact: no per-age wage tables when age is unset.
        // Single resolved wage (age filter active) still shows inline.
        if (hasWageBands(v)) {
            return "";
        }
        if (v.wage == null || v.wage === "") {
            return "";
        }
        return "<span class=\"cluster-list__wage\">€ " + formatWage(v.wage) + "</span>";
    }

    function travelHtml(v) {
        if (v.travelMinutes == null) return "";
        const transport = escapeHtml(String(v.transportLabel || TRANSPORT_LABEL[v.transport] || "reistijd"));
        return (
            "<span class=\"map-popup__travel-inline\">± " + escapeHtml(String(v.travelMinutes)) + " min " + transport + "</span>"
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
        const rootClass = "map-popup" + (hasWageBands(v) ? " map-popup--with-wages" : "");

        return (
            "<div class=\"" + rootClass + "\">" +
                "<div class=\"map-popup__main\">" +
                    "<div class=\"" + mediaClass + "\">" + mediaInner + "</div>" +
                    "<div class=\"map-popup__body\">" +
                        "<div class=\"map-popup__top\">" +
                            "<h3 class=\"map-popup__title\">" + escapeHtml(v.title) + "</h3>" +
                            travelHtml(v) +
                        "</div>" +
                        "<p class=\"map-popup__company\">" + escapeHtml(v.company) + "</p>" +
                        "<p class=\"map-popup__address\">" + escapeHtml(v.address || "") + "</p>" +
                        wageInlineHtml(v) +
                        (badges ? "<div class=\"map-popup__badges\">" + badges + "</div>" : "") +
                        "<div class=\"map-popup__actions\">" +
                            "<a class=\"map-popup__cta\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">Bekijk vacature</a>" +
                        "</div>" +
                    "</div>" +
                "</div>" +
                wagePanelHtml(v) +
            "</div>"
        );
    }

    const CLUSTER_PAGE_SIZE = 1;

    function clusterJobsFromMarkers(childMarkers) {
        return childMarkers
            .map(function (marker) {
                return marker.options.jobData;
            })
            .filter(Boolean);
    }

    function buildClusterPagerHtml(current, pageCount) {
        if (pageCount <= 1) {
            return "";
        }

        return (
            "<div class=\"cluster-single__pager\" role=\"navigation\" aria-label=\"Vacatures op deze locatie\">" +
                "<button type=\"button\" class=\"cluster-single__nav\" data-cluster-page=\"" + (current - 1) + "\"" +
                    (current <= 1 ? " disabled" : "") + " aria-label=\"Vorige vacature\">‹</button>" +
                "<span class=\"cluster-single__page-status\">" + current + " / " + pageCount + "</span>" +
                "<button type=\"button\" class=\"cluster-single__nav\" data-cluster-page=\"" + (current + 1) + "\"" +
                    (current >= pageCount ? " disabled" : "") + " aria-label=\"Volgende vacature\">›</button>" +
            "</div>"
        );
    }

    function buildClusterSingleHtml(childMarkers, page) {
        const jobs = clusterJobsFromMarkers(childMarkers);
        const total = jobs.length;
        const pageCount = Math.max(1, Math.ceil(total / CLUSTER_PAGE_SIZE));
        const current = Math.min(Math.max(1, page || 1), pageCount);
        const job = jobs[current - 1];
        if (!job) {
            return "<div class=\"cluster-list\"><p>Geen vacatures</p></div>";
        }

        return (
            "<div class=\"cluster-single\">" +
                buildPopupHtml(job) +
                buildClusterPagerHtml(current, pageCount) +
            "</div>"
        );
    }

    function bindClusterPopupInteractions(popup, childMarkers) {
        const popupEl = popup.getElement();
        if (!popupEl) {
            return;
        }

        // Open vacancy detail immediately on CTA / main card click.
        popupEl.querySelectorAll(".map-popup__cta, .map-popup__main").forEach(function (el) {
            if (el.dataset.boundNav) {
                return;
            }
            el.dataset.boundNav = "1";
            el.addEventListener("click", function (ev) {
                const cta = el.classList.contains("map-popup__cta")
                    ? el
                    : el.querySelector(".map-popup__cta");
                const id = (cta && cta.getAttribute("data-job-id")) || null;
                if (!id) {
                    return;
                }
                if (!el.classList.contains("map-popup__cta")) {
                    if (ev.target.closest("a, button")) {
                        return;
                    }
                    ev.preventDefault();
                    window.location.href = "/vacancies/" + encodeURIComponent(id);
                }
                notifyOpen(id);
            });
        });

        popupEl.querySelectorAll("[data-cluster-page]").forEach(function (btn) {
            btn.addEventListener("click", function (ev) {
                ev.preventDefault();
                ev.stopPropagation();
                if (btn.disabled || btn.getAttribute("disabled") != null) {
                    return;
                }
                const nextPage = parseInt(btn.getAttribute("data-cluster-page") || "0", 10);
                const jobs = clusterJobsFromMarkers(childMarkers);
                const pageCount = Math.max(1, jobs.length);
                if (!nextPage || nextPage < 1 || nextPage > pageCount) {
                    return;
                }
                const job = jobs[nextPage - 1] || {};
                popup.setContent(buildClusterSingleHtml(childMarkers, nextPage));
                // Keep popup open on last/first page — only update content.
                if (typeof popup.update === "function") {
                    popup.update();
                }
                bindClusterPopupInteractions(popup, childMarkers);
            });
        });
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
        const jobs = clusterJobsFromMarkers(childMarkers);
        const first = jobs[0] || {};

        if (activeClusterPopup) {
            map.closePopup(activeClusterPopup);
        }

        // closeOnClick:true (default) — map clicks outside close the popup.
        // Pager buttons use stopPropagation so they stay usable inside the popup.
        const opts = Object.assign({}, clusterPopupOptions(hasWageBands(first)), {
            closeOnClick: true,
            autoClose: true,
            closeButton: true
        });

        activeClusterPopup = L.popup(opts)
            .setLatLng(latlng)
            .setContent(buildClusterSingleHtml(childMarkers, 1))
            .openOn(map);

        const opened = activeClusterPopup;
        opened.on("remove", function () {
            if (activeClusterPopup === opened) {
                activeClusterPopup = null;
            }
        });

        bindClusterPopupInteractions(activeClusterPopup, childMarkers);
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
                color: "#007bff",
                weight: index === minutes.length - 1 ? 2 : 1.5,
                opacity: 0.7,
                fillColor: "#007bff",
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

        map = L.map(el, {
            zoomControl: true,
            scrollWheelZoom: true,
            closePopupOnClick: true
        });

        // Light Carto basemap — CSS filter in app.css applies Jobsy blue tint
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
            // Prevent the same gesture from also firing map click → closing the new popup.
            if (e.originalEvent) {
                L.DomEvent.stopPropagation(e.originalEvent);
            }
            openClusterList(e.layer);
        });

        setVacancies(vacancies || []);

        map.addLayer(clusterGroup);

        if (options && options.origin) {
            setOrigin(options.origin.lat, options.origin.lng, options.travel);
        }

        addLocateControl();

        [50, 200, 500].forEach(function (ms) {
            setTimeout(invalidate, ms);
        });

        window.addEventListener("resize", invalidate);
    }

    function locateIconHtml() {
        return (
            "<svg class=\"job-map-locate__icon\" viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" aria-hidden=\"true\">" +
                "<circle cx=\"12\" cy=\"12\" r=\"3.2\" fill=\"currentColor\"/>" +
                "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" " +
                    "d=\"M12 3v2.5M12 18.5V21M3 12h2.5M18.5 12H21\"/>" +
                "<circle cx=\"12\" cy=\"12\" r=\"7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"/>" +
            "</svg>"
        );
    }

    function addLocateControl() {
        if (!map) return;

        const LocateControl = L.Control.extend({
            onAdd: function () {
                const bar = L.DomUtil.create("div", "leaflet-bar job-map-locate");
                const btn = L.DomUtil.create("a", "job-map-locate__btn", bar);
                btn.href = "#";
                btn.title = "Mijn locatie";
                btn.setAttribute("role", "button");
                btn.setAttribute("aria-label", "Mijn locatie");
                btn.innerHTML = locateIconHtml();

                L.DomEvent.disableClickPropagation(bar);
                L.DomEvent.on(btn, "click", L.DomEvent.stop)
                    .on(btn, "click", function () {
                        if (btn.classList.contains("is-busy")) return;
                        btn.classList.add("is-busy");
                        const done = function () {
                            btn.classList.remove("is-busy");
                        };

                        if (openCallback) {
                            openCallback.invokeMethodAsync("OnMapLocateClicked")
                                .then(done, done);
                        } else if (window.jobsyGeo && typeof window.jobsyGeo.requestLocation === "function") {
                            window.jobsyGeo.requestLocation()
                                .then(function (pos) {
                                    setOrigin(pos.lat, pos.lng, travelOptions);
                                })
                                .then(done, done);
                        } else {
                            done();
                        }
                    });

                return bar;
            }
        });

        new LocateControl({ position: "bottomright" }).addTo(map);
        syncLocateButton();
    }

    function syncLocateButton() {
        if (!map) return;
        const btn = map.getContainer().querySelector(".job-map-locate__btn");
        if (!btn) return;
        if (originMarker) {
            btn.classList.add("is-active");
        } else {
            btn.classList.remove("is-active");
        }
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

            const workType = Array.isArray(v.workTypes) && v.workTypes.length
                ? v.workTypes[0]
                : (v.workType || "");
            const marker = L.marker([lat, lng], {
                icon: createIcon(false, workType),
                jobData: v
            });

            marker.bindPopup(buildPopupHtml(v), jobPopupOptions(hasWageBands(v)));

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
                color: "#0056b3",
                weight: 3,
                fillColor: "#007bff",
                fillOpacity: 1
            }).addTo(map);
            originMarker.bindTooltip("Jouw locatie", { direction: "top" });
        }

        drawTravelRings(la, ln);
        syncLocateButton();
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
        syncLocateButton();
    }

    function invalidate() {
        if (map) {
            map.invalidateSize({ animate: false });
        }
    }

    function highlight(id) {
        Object.keys(markersById).forEach(function (key) {
            const marker = markersById[key];
            const data = marker.options.jobData || {};
            const workType = Array.isArray(data.workTypes) && data.workTypes.length
                ? data.workTypes[0]
                : (data.workType || "");
            marker.setIcon(createIcon(key === id, workType));
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
