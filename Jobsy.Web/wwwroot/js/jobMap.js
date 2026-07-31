window.jobMap = (function () {
    let map = null;
    let clusterGroup = null;
    let markersById = {};
    let originMarker = null;
    let travelRingLayers = [];
    let travelOptions = { maxMinutes: 30, transport: "Fiets", radiusKm: 15 };
    let activeClusterPopup = null;
    let openCallback = null;
    let outsideClickCloserBound = false;

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

    function jobPopupOptions() {
        const vw = window.innerWidth || 360;
        const narrow = isNarrowViewport();
        // Fixed width so every vacancy card matches (no content-driven resize).
        // Mobile uses a stacked Funda card at nearly full width.
        const width = narrow
            ? Math.max(280, Math.min(360, vw - 24))
            : 520;
        return {
            className: "job-map-popup",
            maxWidth: width,
            minWidth: width,
            autoPanPadding: narrow ? [12, 72] : [40, 56],
            keepInView: true,
            closeOnClick: true,
            closeButton: true
        };
    }

    function clusterPopupOptions() {
        // Same sizing/classes as the single-job popup; only add a cluster flag for the pager strip.
        const opts = jobPopupOptions();
        const narrow = isNarrowViewport();
        const vh = window.innerHeight || 640;
        opts.className = opts.className + " job-map-popup--cluster";
        // Slightly more vertical padding so the pager + close button stay on-screen.
        opts.autoPanPadding = narrow
            ? [12, Math.max(64, Math.round(vh * 0.08))]
            : [36, 56];
        return opts;
    }

    function applyPopupOptions(popup) {
        if (!popup) {
            return;
        }
        const opts = clusterPopupOptions();
        popup.options.className = opts.className;
        popup.options.maxWidth = opts.maxWidth;
        popup.options.minWidth = opts.minWidth;
        popup.options.autoPanPadding = opts.autoPanPadding;

        const el = typeof popup.getElement === "function" ? popup.getElement() : null;
        if (el) {
            el.classList.add("job-map-popup", "job-map-popup--cluster");
            el.classList.remove(
                "job-map-popup--with-wages",
                "job-cluster-popup",
                "job-cluster-popup--with-wages"
            );
        }
    }

    function hasWageBands(v) {
        return Array.isArray(v.wageBands) && v.wageBands.length > 0;
    }

    function wageInlineHtml(v) {
        if (v.wageLabel) {
            return "<p class=\"map-popup__wage map-popup__wage--masked\">" + escapeHtml(v.wageLabel) + "</p>";
        }
        // Only show a concrete rate when age is selected (or a single fixed wage).
        if (v.wage == null || v.wage === "") {
            return "";
        }
        return "<p class=\"map-popup__wage\">€ " + formatWage(v.wage) + " <span class=\"map-popup__wage-unit\">/uur</span></p>";
    }

    function wageTableRowsHtml(bands) {
        return bands.map(function (b) {
            return "<tr><th>" + escapeHtml(String(b.label || b.ageYears || "")) + "</th>" +
                "<td>€ " + formatWage(b.hourlyRate) + "</td></tr>";
        }).join("");
    }

    function wageInfoHtml(v) {
        if (!hasWageBands(v)) {
            return "";
        }
        return (
            "<button type=\"button\" class=\"map-popup__wage-info\" aria-expanded=\"false\" " +
                "aria-controls=\"wage-popover-" + escapeAttr(v.id) + "\" " +
                "aria-label=\"Uurlonen per leeftijd\">€</button>" +
            "<div id=\"wage-popover-" + escapeAttr(v.id) + "\" class=\"map-popup__wage-popover\" hidden>" +
                "<p class=\"map-popup__wage-popover-title\">Uurlonen</p>" +
                "<table class=\"map-popup__wage-table\"><tbody>" + wageTableRowsHtml(v.wageBands) + "</tbody></table>" +
            "</div>"
        );
    }

    function specIcon(kind) {
        if (kind === "travel") {
            return "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\">" +
                "<circle cx=\"12\" cy=\"12\" r=\"8.25\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                "<path d=\"M12 7.5v5l3.2 1.9\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>" +
                "</svg>";
        }
        if (kind === "work") {
            return "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\">" +
                "<rect x=\"4\" y=\"8\" width=\"16\" height=\"11\" rx=\"1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                "<path d=\"M9 8V6.8A1.8 1.8 0 0 1 10.8 5h2.4A1.8 1.8 0 0 1 15 6.8V8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
                "</svg>";
        }
        // transport
        return "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\">" +
            "<path d=\"M5 15.5 7.2 8.8A2 2 0 0 1 9.1 7.5h5.8a2 2 0 0 1 1.9 1.3L19 15.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\"/>" +
            "<circle cx=\"8\" cy=\"16.5\" r=\"1.6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<circle cx=\"16\" cy=\"16.5\" r=\"1.6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "</svg>";
    }

    function specsHtml(v) {
        const parts = [];
        if (v.travelMinutes != null) {
            const transport = String(v.transportLabel || TRANSPORT_LABEL[v.transport] || "reistijd");
            parts.push(
                "<span class=\"map-popup__spec\">" + specIcon("travel") +
                "<span>± " + escapeHtml(String(v.travelMinutes)) + " min " + escapeHtml(transport) + "</span></span>"
            );
        }

        const workTypes = Array.isArray(v.workTypes) ? v.workTypes : [];
        const primaryWork = workTypes[0] || v.workType || "";
        if (primaryWork) {
            parts.push(
                "<span class=\"map-popup__spec\">" + specIcon("work") +
                "<span>" + escapeHtml(String(primaryWork)) + "</span></span>"
            );
        }

        const transports = Array.isArray(v.transport) ? v.transport : [];
        if (transports.length) {
            parts.push(
                "<span class=\"map-popup__spec\">" + specIcon("transport") +
                "<span>" + escapeHtml(transports.join(", ")) + "</span></span>"
            );
        }

        if (!parts.length) {
            return "<div class=\"map-popup__specs map-popup__specs--empty\" aria-hidden=\"true\"></div>";
        }
        return "<div class=\"map-popup__specs\">" + parts.join("") + "</div>";
    }

    function mountWageControls(popupEl) {
        if (!popupEl) {
            return;
        }
        const content = popupEl.querySelector(".leaflet-popup-content");
        const btnInContent = content
            ? content.querySelector(".map-popup__wage-info")
            : null;
        const popoverInContent = content
            ? content.querySelector(".map-popup__wage-popover")
            : null;

        // Drop previously parked € controls (cluster page changes replace content only).
        Array.prototype.slice.call(popupEl.children).forEach(function (child) {
            if (!child.classList) {
                return;
            }
            const isWageChrome = child.classList.contains("map-popup__wage-info")
                || child.classList.contains("map-popup__wage-popover");
            if (isWageChrome && child !== btnInContent && child !== popoverInContent) {
                child.remove();
            }
        });

        // Park € control next to Leaflet's close button (outside the card).
        if (btnInContent) {
            popupEl.appendChild(btnInContent);
            popupEl.classList.add("job-map-popup--with-wages");
        } else {
            popupEl.classList.remove("job-map-popup--with-wages");
        }
        if (popoverInContent) {
            popupEl.appendChild(popoverInContent);
        }
    }

    function closeAllWagePopovers(root) {
        const scope = root || document;
        scope.querySelectorAll(".map-popup__wage-popover:not([hidden])").forEach(function (pop) {
            pop.setAttribute("hidden", "");
        });
        scope.querySelectorAll(".map-popup__wage-info.is-open").forEach(function (btn) {
            btn.classList.remove("is-open");
            btn.setAttribute("aria-expanded", "false");
        });
    }

    function bindWageInfoInteractions(popupEl) {
        if (!popupEl) {
            return;
        }

        mountWageControls(popupEl);

        popupEl.querySelectorAll(".map-popup__wage-info").forEach(function (btn) {
            if (btn.dataset.boundWageInfo) {
                return;
            }
            btn.dataset.boundWageInfo = "1";
            btn.addEventListener("click", function (ev) {
                // Keep the Leaflet vacancy popup open; only toggle the wage table.
                L.DomEvent.stop(ev);
                ev.preventDefault();
                const controlId = btn.getAttribute("aria-controls");
                const popover = controlId
                    ? popupEl.querySelector("#" + controlId)
                    : popupEl.querySelector(".map-popup__wage-popover");
                if (!popover) {
                    return;
                }
                const willOpen = popover.hasAttribute("hidden");
                closeAllWagePopovers(popupEl);
                if (willOpen) {
                    popover.removeAttribute("hidden");
                    btn.classList.add("is-open");
                    btn.setAttribute("aria-expanded", "true");
                }
            });
        });
    }

    function buildPopupHtml(v) {
        const workTypes = Array.isArray(v.workTypes) ? v.workTypes : [];
        const primaryWork = workTypes[0] || v.workType || "";
        const hasImage = !!v.imageUrl;
        const mediaClass = hasImage
            ? "map-popup__media"
            : "map-popup__media map-popup__media--logo-only";

        let mediaInner = "";
        if (v.highlighted || primaryWork) {
            const badgeText = v.highlighted ? "Top" : primaryWork;
            const badgeClass = v.highlighted
                ? "map-popup__badge"
                : "map-popup__badge map-popup__badge--soft";
            mediaInner +=
                "<span class=\"" + badgeClass + "\">" + escapeHtml(String(badgeText)) + "</span>";
        }

        if (hasImage) {
            mediaInner +=
                "<img class=\"map-popup__photo\" src=\"" + escapeAttr(v.imageUrl) + "\" alt=\"\" loading=\"lazy\" />";
        } else if (v.logoUrl) {
            mediaInner +=
                "<img class=\"map-popup__media-logo\" src=\"" + escapeAttr(v.logoUrl) + "\" alt=\"" +
                escapeAttr(v.company) + " logo\" loading=\"lazy\" />";
        }

        const detailHref = "/vacancies/" + encodeURIComponent(v.id);
        const applyHref = detailHref + "#apply";
        const wage = wageInlineHtml(v);

        return (
            "<div class=\"map-popup\">" +
                wageInfoHtml(v) +
                "<div class=\"map-popup__main\">" +
                    "<a class=\"" + mediaClass + "\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">" +
                        mediaInner +
                    "</a>" +
                    "<div class=\"map-popup__body\">" +
                        "<div class=\"map-popup__header\">" +
                            "<a class=\"map-popup__title map-popup__cta\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">" +
                                escapeHtml(v.title) +
                            "</a>" +
                            (v.address
                                ? "<p class=\"map-popup__address\">" + escapeHtml(v.address) + "</p>"
                                : "<p class=\"map-popup__address map-popup__address--empty\">&nbsp;</p>") +
                        "</div>" +
                        (wage || "<p class=\"map-popup__wage map-popup__wage--empty\">&nbsp;</p>") +
                        specsHtml(v) +
                        "<div class=\"map-popup__footer\">" +
                            "<a class=\"map-popup__company map-popup__cta\" href=\"" + detailHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">" +
                                escapeHtml(v.company) +
                            "</a>" +
                            "<a class=\"map-popup__apply map-popup__cta\" href=\"" + applyHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">Solliciteer</a>" +
                        "</div>" +
                    "</div>" +
                "</div>" +
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
            "<div class=\"map-popup__pager\" role=\"navigation\" aria-label=\"Vacatures op deze locatie\">" +
                "<button type=\"button\" class=\"map-popup__pager-nav\" data-cluster-page=\"" + (current - 1) + "\"" +
                    (current <= 1 ? " disabled" : "") + " aria-label=\"Vorige vacature\">‹</button>" +
                "<span class=\"map-popup__pager-status\">" + current + " van " + pageCount + "</span>" +
                "<button type=\"button\" class=\"map-popup__pager-nav\" data-cluster-page=\"" + (current + 1) + "\"" +
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
            return "<div class=\"map-popup\"><p class=\"map-popup__company\">Geen vacatures</p></div>";
        }

        // Funda-style: pager floats above the listing card.
        return buildClusterPagerHtml(current, pageCount) + buildPopupHtml(job);
    }

    function bindClusterPopupInteractions(popup, childMarkers) {
        const popupEl = popup.getElement();
        if (!popupEl) {
            return;
        }

        bindWageInfoInteractions(popupEl);

        // Match single-marker behaviour: detail links notify open.
        popupEl.querySelectorAll(".map-popup__cta, a.map-popup__media").forEach(function (cta) {
            if (cta.dataset.boundNav) {
                return;
            }
            cta.dataset.boundNav = "1";
            cta.addEventListener("click", function () {
                const id = cta.getAttribute("data-job-id");
                if (id) {
                    notifyOpen(id);
                }
            });
        });

        popupEl.querySelectorAll("[data-cluster-page]").forEach(function (btn) {
            btn.addEventListener("click", function (ev) {
                ev.preventDefault();
                // Stop both DOM and Leaflet bubbling so pager clicks never close the popup.
                L.DomEvent.stop(ev);
                if (btn.disabled || btn.getAttribute("disabled") != null) {
                    return;
                }
                const nextPage = parseInt(btn.getAttribute("data-cluster-page") || "0", 10);
                const jobs = clusterJobsFromMarkers(childMarkers);
                const pageCount = Math.max(1, jobs.length);
                if (!nextPage || nextPage < 1 || nextPage > pageCount) {
                    return;
                }
                applyPopupOptions(popup);
                popup.setContent(buildClusterSingleHtml(childMarkers, nextPage));
                // Keep popup open on last/first page — only update content.
                if (typeof popup.update === "function") {
                    popup.update();
                }
                bindClusterPopupInteractions(popup, childMarkers);
            });
        });
    }

    function eventTargetInsidePopup(ev, popup) {
        if (!popup || !ev) {
            return false;
        }
        const popupEl = typeof popup.getElement === "function" ? popup.getElement() : null;
        const target = ev.target || ev.srcElement;
        return !!(popupEl && target && popupEl.contains(target));
    }

    function eventTargetInsideWagePopover(ev) {
        const target = ev && (ev.target || ev.srcElement);
        return !!(target && target.closest &&
            target.closest(".map-popup__wage-info, .map-popup__wage-popover"));
    }

    function closePopupsIfClickOutside(ev) {
        if (!map) {
            return;
        }

        // Clicks outside the wage popover (even inside the vacancy card) dismiss the table only.
        if (!eventTargetInsideWagePopover(ev)) {
            closeAllWagePopovers(map.getContainer());
        }

        // Clicks inside any open popup (pager, CTA, content, close btn) must stay put.
        if (activeClusterPopup && eventTargetInsidePopup(ev, activeClusterPopup)) {
            return;
        }
        if (map._popup && eventTargetInsidePopup(ev, map._popup)) {
            return;
        }

        const target = ev.target || ev.srcElement;
        const onMarkerOrCluster = !!(target && target.closest &&
            target.closest(".leaflet-marker-icon, .marker-cluster, .job-cluster, .job-marker"));

        // Marker/cluster icons manage their own open/replace; don't force-close marker popups
        // here (would break click-to-toggle). Still dismiss the custom cluster list.
        if (onMarkerOrCluster) {
            if (activeClusterPopup) {
                map.closePopup(activeClusterPopup);
            }
            return;
        }

        if (activeClusterPopup) {
            map.closePopup(activeClusterPopup);
            return;
        }

        // Empty-map click: also close single vacancy popups (Leaflet preclick is unreliable
        // alongside MarkerCluster + bubblingMouseEvents:false).
        if (map._popup) {
            map.closePopup();
        }
    }

    function closeWagePopoverIfOutside(ev) {
        if (!eventTargetInsideWagePopover(ev)) {
            closeAllWagePopovers();
        }
    }

    function bindOutsideClickCloser() {
        if (!map || outsideClickCloserBound) {
            return;
        }
        outsideClickCloserBound = true;
        // Native capture: L.DomEvent.on's 4th arg is context, not useCapture.
        // Capture still runs when markers/clusters stop Leaflet click bubbling.
        map.getContainer().addEventListener("click", closePopupsIfClickOutside, true);
        document.addEventListener("click", closeWagePopoverIfOutside, true);
    }

    function unbindOutsideClickCloser() {
        document.removeEventListener("click", closeWagePopoverIfOutside, true);
        if (!map || !outsideClickCloserBound) {
            outsideClickCloserBound = false;
            return;
        }
        map.getContainer().removeEventListener("click", closePopupsIfClickOutside, true);
        outsideClickCloserBound = false;
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

        // closeOnClick stays true for Leaflet-native path; bindOutsideClickCloser is the
        // reliable fallback when MarkerCluster swallows map preclick/click bubbling.
        const opts = Object.assign({}, clusterPopupOptions(), {
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
            bindWageInfoInteractions(el);
            el.querySelectorAll(".map-popup__cta, a.map-popup__media").forEach(function (cta) {
                if (cta.dataset.bound) {
                    return;
                }
                cta.dataset.bound = "1";
                cta.addEventListener("click", function () {
                    notifyOpen(v.id);
                });
            });
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

        // Carto Voyager — vivid water/parks/roads without a washed-out light basemap
        L.tileLayer("https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png", {
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
            // Stop Leaflet bubbling (_stopped) — native-only stopPropagation is not enough.
            L.DomEvent.stopPropagation(e);
            if (e.originalEvent) {
                L.DomEvent.stopPropagation(e.originalEvent);
            }
            openClusterList(e.layer);
        });

        setVacancies(vacancies || []);

        map.addLayer(clusterGroup);
        bindOutsideClickCloser();

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

            marker.bindPopup(buildPopupHtml(v), jobPopupOptions());

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
            unbindOutsideClickCloser();
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
