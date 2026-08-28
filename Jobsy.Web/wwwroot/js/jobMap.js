window.jobMap = (function () {
    let map = null;
    let clusterGroup = null;
    let markersById = {};
    let originMarker = null;
    let travelRingLayers = [];
    let travelRingGeo = null;
    let travelOptions = { maxMinutes: 30, transport: "Fiets", radiusKm: 15 };
    let activeClusterPopup = null;
    let openCallback = null;
    let outsideClickCloserBound = false;
    let highlightSeed = 0;
    let firstSizedFit = false;
    let lastFitPoints = [];
    let tileLayer = null;
    let renderedMarkers = [];
    let lastOrigin = null;
    let selectedId = null;
    let zoomHandlerBound = false;

    let cameraLocked = false;
    let originHasBeenFramed = false;
    let originNeedsFrame = false;
    let ringRedrawBound = false;
    let ringStyleHandlerBound = false;
    let ringRedrawTries = 0;
    let deferMapReveal = false;

    // Fallback only when the index has no pins. Prefer the precomputed view from #jobsy-map-boot.
    const NL_CENTER = [52.15, 5.2913];
    const NL_ZOOM = 7;
    const NL_BOUNDS = [[50.29, 2.81], [53.33, 8.44]];
    // Keep in sync with VacancyMapViewCalculator.FilledLocationZoom (fits default 30-min fiets ring).
    const FILLED_LOCATION_ZOOM = 12;

    const CLUSTER_OPTS = {
        showCoverageOnHover: false,
        zoomToBoundsOnClick: false,
        spiderfyOnMaxZoom: false,
        disableClusteringAtZoom: 16,
        maxClusterRadius: 60,
        removeOutsideVisibleBounds: false
    };

    // On-road cruise km/h. Keep in sync with TravelReach.SpeedKmPerHour.
    const CRUISE_KM_H = {
        Fiets: 18.0,
        Auto: 40.0,
        OV: 25.0,
        Lopend: 5.0
    };
    // Road / crow-flies. Keep in sync with TravelReach.RoadCircuity (bike 1.7 ≈ OSRM).
    const ROAD_CIRCUITY = {
        Fiets: 1.7,
        Auto: 1.35,
        OV: 1.5,
        Lopend: 1.4
    };

    const TRANSPORT_LABEL = {
        Fiets: "fietsen",
        Auto: "rijden",
        OV: "OV",
        Lopend: "lopen"
    };

    function workTypeGlyph(workType) {
        const t = String(workType || "").toLowerCase();
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

    function markerClassName(featured, selected) {
        const classes = ["job-marker"];
        if (featured) {
            classes.push("job-marker--featured");
        }
        if (selected) {
            classes.push("job-marker--active");
        }
        return classes.join(" ");
    }

    function markerInnerHtml(featured, workType, categoryColor) {
        const glyph = workTypeGlyph(workType);
        const pulse = featured
            ? "<span class=\"job-marker__pulse\" aria-hidden=\"true\"></span>"
            : "";
        const color = (categoryColor && /^#[0-9A-Fa-f]{6}$/.test(categoryColor))
            ? categoryColor
            : "";
        const style = color
            ? " style=\"--map-pin:" + color + ";--map-pin-deep:" + color + ";--map-pin-glow:" + color + "66\""
            : "";
        return pulse + "<span class=\"job-marker__glyph\"" + style + " aria-hidden=\"true\">" + glyph + "</span>";
    }

    function fillMarkerElement(el, featured, selected, workType, categoryColor) {
        el.className = markerClassName(featured, selected);
        el.innerHTML = markerInnerHtml(featured, workType, categoryColor);
        return el;
    }

    function workTypeOf(v) {
        return Array.isArray(v.workTypes) && v.workTypes.length
            ? v.workTypes[0]
            : (v.workType || "");
    }

    function formatWage(wage) {
        if (wage == null || wage === "") return "";
        return Number(wage).toFixed(2).replace(".", ",");
    }

    function isNarrowViewport() {
        return (window.innerWidth || 0) <= 768;
    }

    // Center-anchored pins: 34px job / 44px cluster. Tip sits on the top of the marker.
    const JOB_POPUP_OFFSET = { bottom: [0, -18] };
    const CLUSTER_POPUP_OFFSET = { bottom: [0, -23] };

    function isFeaturedVacancy(v) {
        return !!(v && (v.highlighted === true || v.isFeatured === true || v.featured === true));
    }

    function jobPopupOptions(featured) {
        const vw = window.innerWidth || 360;
        const narrow = isNarrowViewport();
        const width = narrow
            ? Math.max(280, Math.min(340, vw - 24))
            : 520;
        return {
            className: "job-map-popup" + (featured ? " job-map-popup--featured featured-job" : ""),
            maxWidth: width + "px",
            anchor: "bottom",
            offset: JOB_POPUP_OFFSET,
            closeOnClick: true,
            closeButton: true
        };
    }

    function clusterPopupOptions(featured) {
        const opts = jobPopupOptions(featured);
        opts.className = opts.className + " job-map-popup--cluster";
        opts.offset = CLUSTER_POPUP_OFFSET;
        return opts;
    }

    function syncFeaturedPopupClass(popup, vacancy) {
        const el = popup && typeof popup.getElement === "function" ? popup.getElement() : null;
        if (!el || !el.classList) {
            return;
        }

        const featured = isFeaturedVacancy(vacancy);
        el.classList.toggle("job-map-popup--featured", featured);
        el.classList.toggle("featured-job", featured);
        const content = el.querySelector(".maplibregl-popup-content");
        if (content && content.classList) {
            content.classList.toggle("featured-job", featured);
        }
    }

    function applyPopupOptions(popup, vacancy) {
        if (!popup) {
            return;
        }
        const opts = clusterPopupOptions(isFeaturedVacancy(vacancy));
        popup.options = popup.options || {};
        popup.options.className = opts.className;
        popup.options.maxWidth = opts.maxWidth;
        popup.options.anchor = opts.anchor;
        popup.options.offset = opts.offset;
        if (typeof popup.setOffset === "function") {
            popup.setOffset(opts.offset);
        }

        const el = typeof popup.getElement === "function" ? popup.getElement() : null;
        if (el) {
            el.classList.add("job-map-popup", "job-map-popup--cluster");
            el.classList.remove(
                "job-map-popup--with-wages",
                "job-map-popup--with-type",
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
        return "<svg class=\"map-popup__spec-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\" focusable=\"false\">" +
            "<path d=\"M5 15.5 7.2 8.8A2 2 0 0 1 9.1 7.5h5.8a2 2 0 0 1 1.9 1.3L19 15.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\"/>" +
            "<circle cx=\"8\" cy=\"16.5\" r=\"1.6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<circle cx=\"16\" cy=\"16.5\" r=\"1.6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "</svg>";
    }

    function travelLineHtml(v) {
        if (v.travelMinutes == null) {
            return "<p class=\"map-popup__travel map-popup__travel--empty\" aria-hidden=\"true\"></p>";
        }
        const transport = String(v.transportLabel || TRANSPORT_LABEL[v.transport] || "reistijd");
        return (
            "<p class=\"map-popup__travel\">" +
                specIcon("travel") +
                "<span>± " + escapeHtml(String(v.travelMinutes)) + " min " + escapeHtml(transport) + "</span>" +
            "</p>"
        );
    }

    function specsHtml(v) {
        const parts = [];

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

    function typeChipHtml(v) {
        if (!v.typeBadgeLabel) {
            return "";
        }
        const color = safeBadgeColor(v.typeBadgeColor || v.categoryColor);
        return (
            "<span class=\"map-popup__type-chip\" style=\"--badge-color:" +
            escapeAttr(color) +
            "\">" +
            escapeHtml(String(v.typeBadgeLabel)) +
            "</span>"
        );
    }

    function mountWageControls(popupEl) {
        if (!popupEl) {
            return;
        }
        const content = popupEl.querySelector(".maplibregl-popup-content") || popupEl;
        const btnInContent = content
            ? content.querySelector(".map-popup__wage-info")
            : null;
        const popoverInContent = content
            ? content.querySelector(".map-popup__wage-popover")
            : null;
        const typeInContent = content
            ? content.querySelector(".map-popup__type-chip")
            : null;

        Array.prototype.slice.call(popupEl.children).forEach(function (child) {
            if (!child.classList) {
                return;
            }
            const isChrome = child.classList.contains("map-popup__wage-info")
                || child.classList.contains("map-popup__wage-popover")
                || child.classList.contains("map-popup__type-chip");
            if (isChrome
                && child !== btnInContent
                && child !== popoverInContent
                && child !== typeInContent) {
                child.remove();
            }
        });

        if (btnInContent) {
            popupEl.appendChild(btnInContent);
            popupEl.classList.add("job-map-popup--with-wages");
        } else {
            popupEl.classList.remove("job-map-popup--with-wages");
        }
        if (popoverInContent) {
            popupEl.appendChild(popoverInContent);
        }
        if (typeInContent) {
            popupEl.appendChild(typeInContent);
            popupEl.classList.add("job-map-popup--with-type");
        } else {
            popupEl.classList.remove("job-map-popup--with-type");
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

    function stopEvent(ev) {
        if (!ev) {
            return;
        }
        if (typeof ev.stopPropagation === "function") {
            ev.stopPropagation();
        }
        if (typeof ev.preventDefault === "function") {
            ev.preventDefault();
        }
        if (typeof ev.stopImmediatePropagation === "function") {
            ev.stopImmediatePropagation();
        }
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
                stopEvent(ev);
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
        const hasImage = !!v.imageUrl;
        const mediaClass = hasImage
            ? "map-popup__media"
            : "map-popup__media map-popup__media--logo-only";

        const badges = [];
        if (v.highlighted) {
            badges.push(
                "<span class=\"map-popup__badge map-popup__badge--featured\">" +
                escapeHtml(String(v.featuredLabel || "Uitgelicht")) +
                "</span>"
            );
        }
        const badgesHtml = badges.length > 0
            ? "<div class=\"map-popup__badges\">" + badges.join("") + "</div>"
            : "";

        let mediaInner = "";
        if (hasImage) {
            mediaInner +=
                "<img class=\"map-popup__photo\" src=\"" + escapeAttr(v.imageUrl) + "\" alt=\"\" loading=\"lazy\" data-logo-fallback=\"1\" />";
        } else if (v.logoUrl) {
            mediaInner +=
                "<img class=\"map-popup__media-logo\" src=\"" + escapeAttr(v.logoUrl) + "\" alt=\"" +
                escapeAttr(v.company) + " logo\" loading=\"lazy\" data-logo-fallback=\"1\" />";
        }

        const detailHref = "/vacancies/" + encodeURIComponent(v.id);
        const applyHref = detailHref + "#apply";
        const companyHref = v.companyHref
            ? String(v.companyHref)
            : detailHref;
        const wage = wageInlineHtml(v);

        return (
            "<div class=\"map-popup\">" +
                typeChipHtml(v) +
                wageInfoHtml(v) +
                "<div class=\"map-popup__main\">" +
                    badgesHtml +
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
                            travelLineHtml(v) +
                            pushBomStatusHtml(v) +
                        "</div>" +
                        (wage || "<p class=\"map-popup__wage map-popup__wage--empty\">&nbsp;</p>") +
                        specsHtml(v) +
                        "<div class=\"map-popup__footer\">" +
                            "<div class=\"map-popup__footer-meta\">" +
                                "<a class=\"map-popup__company map-popup__cta\" href=\"" + escapeAttr(companyHref) + "\"" +
                                    (companyHref === detailHref ? " data-job-id=\"" + escapeAttr(v.id) + "\"" : "") + ">" +
                                    escapeHtml(v.company) +
                                "</a>" +
                                (v.offeredBy
                                    ? "<p class=\"map-popup__offered-by\">" + escapeHtml(String(v.offeredBy)) + "</p>"
                                    : "") +
                            "</div>" +
                            "<a class=\"map-popup__apply map-popup__cta\" href=\"" + applyHref + "\" data-job-id=\"" + escapeAttr(v.id) + "\">Solliciteer</a>" +
                        "</div>" +
                    "</div>" +
                "</div>" +
            "</div>"
        );
    }

    function pushBomStatusHtml(v) {
        if (!v.pushBomActive) {
            return "";
        }
        return "<p class=\"map-popup__status\">PushBom actief</p>";
    }

    const CLUSTER_PAGE_SIZE = 1;

    function highlightShuffleRank(id, explicitRank) {
        if (Number.isFinite(explicitRank)) {
            return explicitRank >>> 0;
        }
        let h = highlightSeed >>> 0;
        const s = String(id || "");
        for (let i = 0; i < s.length; i++) {
            h ^= s.charCodeAt(i);
            h = Math.imul(h, 16777619);
        }
        return h >>> 0;
    }

    function clusterJobsFromMarkers(childMarkers) {
        return childMarkers
            .map(function (marker) {
                return marker.options && marker.options.jobData;
            })
            .filter(Boolean)
            .sort(function (a, b) {
                const ah = a.highlighted ? 1 : 0;
                const bh = b.highlighted ? 1 : 0;
                if (bh !== ah) {
                    return bh - ah;
                }
                if (ah) {
                    const ra = highlightShuffleRank(a.id, a.highlightRank);
                    const rb = highlightShuffleRank(b.id, b.highlightRank);
                    if (ra !== rb) {
                        return ra - rb;
                    }
                    return String(a.title || "").localeCompare(String(b.title || ""), "nl");
                }
                return 0;
            });
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

        return buildClusterPagerHtml(current, pageCount) + buildPopupHtml(job);
    }

    function bindClusterPopupInteractions(popup, childMarkers) {
        const popupEl = popup.getElement();
        if (!popupEl) {
            return;
        }

        bindWageInfoInteractions(popupEl);

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
                stopEvent(ev);
                if (btn.disabled || btn.getAttribute("disabled") != null) {
                    return;
                }
                const nextPage = parseInt(btn.getAttribute("data-cluster-page") || "0", 10);
                const jobs = clusterJobsFromMarkers(childMarkers);
                const pageCount = Math.max(1, jobs.length);
                if (!nextPage || nextPage < 1 || nextPage > pageCount) {
                    return;
                }
                applyPopupOptions(popup, jobs[nextPage - 1]);
                popup.setHTML(buildClusterSingleHtml(childMarkers, nextPage));
                if (typeof popup.update === "function") {
                    popup.update();
                }
                syncFeaturedPopupClass(popup, jobs[nextPage - 1]);
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

    function closeActivePopup() {
        if (activeClusterPopup) {
            activeClusterPopup.remove();
            activeClusterPopup = null;
        }
    }

    function closePopupsIfClickOutside(ev) {
        if (!map) {
            return;
        }

        if (!eventTargetInsideWagePopover(ev)) {
            closeAllWagePopovers(map.getContainer());
        }

        if (activeClusterPopup && eventTargetInsidePopup(ev, activeClusterPopup)) {
            return;
        }

        const target = ev.target || ev.srcElement;
        const onMarkerOrCluster = !!(target && target.closest &&
            target.closest(".job-cluster, .job-marker, .vacancy-detail-marker"));

        if (onMarkerOrCluster) {
            return;
        }

        closeActivePopup();
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

    function popupFromOpts(opts, lngLat, html) {
        const popup = new maplibregl.Popup(opts)
            .setLngLat(lngLat)
            .setHTML(html)
            .addTo(map);
        popup.on("close", function () {
            if (activeClusterPopup === popup) {
                activeClusterPopup = null;
            }
        });
        return popup;
    }

    function prefersReducedMotion() {
        return !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
    }

    function mapFocusRect() {
        const container = map.getContainer();
        const mapRect = container.getBoundingClientRect();
        let top = mapRect.top;
        const pane = container.closest(".map-pane");
        if (pane) {
            const carousel = pane.querySelector(".highlight-carousel--map");
            if (carousel && carousel.offsetParent !== null) {
                const cr = carousel.getBoundingClientRect();
                if (cr.bottom > top && cr.top < mapRect.bottom) {
                    top = Math.max(top, cr.bottom);
                }
            }
        }
        const pad = 12;
        const left = mapRect.left + pad;
        const right = mapRect.right - pad;
        const bottom = mapRect.bottom - pad;
        top += pad;
        return {
            left: left,
            top: top,
            right: right,
            bottom: bottom,
            width: right - left,
            height: bottom - top
        };
    }

    function centerPopupInView(popup) {
        if (!map || !popup) {
            return;
        }
        const el = typeof popup.getElement === "function" ? popup.getElement() : null;
        if (!el) {
            return;
        }

        const run = function () {
            if (!map || activeClusterPopup !== popup) {
                return;
            }
            const pr = el.getBoundingClientRect();
            const vr = mapFocusRect();
            if (pr.width < 8 || pr.height < 8 || vr.width < 32 || vr.height < 32) {
                return;
            }

            let dx;
            let dy;
            if (pr.width >= vr.width) {
                dx = pr.left - vr.left;
            } else {
                dx = (pr.left + pr.width / 2) - (vr.left + vr.width / 2);
            }
            if (pr.height >= vr.height) {
                dy = pr.top - vr.top;
            } else {
                dy = (pr.top + pr.height / 2) - (vr.top + vr.height / 2);
            }

            if (Math.abs(dx) < 1 && Math.abs(dy) < 1) {
                return;
            }

            map.panBy([dx, dy], { duration: prefersReducedMotion() ? 0 : 320 });
        };

        if (typeof requestAnimationFrame === "function") {
            requestAnimationFrame(function () {
                requestAnimationFrame(run);
            });
        } else {
            setTimeout(run, 0);
        }
    }

    function openVacancyPopup(record) {
        if (!map || !record) {
            return;
        }
        closeActivePopup();
        const v = record.options.jobData;
        const opts = Object.assign({}, jobPopupOptions(isFeaturedVacancy(v)));
        activeClusterPopup = popupFromOpts(opts, [record.lng, record.lat], buildPopupHtml(v));
        syncFeaturedPopupClass(activeClusterPopup, v);
        const el = activeClusterPopup.getElement();
        if (el) {
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
        }
        centerPopupInView(activeClusterPopup);
        notifyOpen(v.id);
    }

    function openClusterList(childMarkers, lngLat) {
        if (!map || !childMarkers || childMarkers.length === 0) {
            return;
        }

        closeActivePopup();

        const firstJob = clusterJobsFromMarkers(childMarkers)[0];
        const opts = Object.assign({}, clusterPopupOptions(isFeaturedVacancy(firstJob)));
        const ll = lngLat || [childMarkers[0].lng, childMarkers[0].lat];
        activeClusterPopup = popupFromOpts(opts, ll, buildClusterSingleHtml(childMarkers, 1));
        syncFeaturedPopupClass(activeClusterPopup, firstJob);
        bindClusterPopupInteractions(activeClusterPopup, childMarkers);
        centerPopupInView(activeClusterPopup);
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
        const cruise = CRUISE_KM_H[transport] || CRUISE_KM_H.Fiets;
        const circuity = ROAD_CIRCUITY[transport] || ROAD_CIRCUITY.Fiets;
        return (cruise * 1000 / 60) / circuity;
    }

    function clearTravelRingLabels() {
        travelRingLayers.forEach(function (layer) {
            if (layer && typeof layer.remove === "function") {
                layer.remove();
            }
        });
        travelRingLayers = [];
    }

    function clearTravelRings() {
        if (map && travelRingGeo) {
            try {
                travelRingGeo.ids.forEach(function (id) {
                    if (map.getLayer(id)) {
                        map.removeLayer(id);
                    }
                });
                if (map.getSource(travelRingGeo.sourceId)) {
                    map.removeSource(travelRingGeo.sourceId);
                }
            } catch (e) { }
        }
        travelRingGeo = null;
        clearTravelRingLabels();
    }

    function travelRingSourceReady() {
        return !!(map && typeof map.getSource === "function" && map.getSource("jobsy-travel-rings"));
    }

    function bindTravelRingStyleGuard() {
        if (!map || ringStyleHandlerBound) {
            return;
        }
        ringStyleHandlerBound = true;
        map.on("styledata", onTravelRingStyleData);
        map.on("idle", onTravelRingStyleData);
    }

    function onTravelRingStyleData() {
        if (!lastOrigin || !map) {
            return;
        }
        if (typeof map.isStyleLoaded === "function" && !map.isStyleLoaded()) {
            return;
        }
        if (travelRingSourceReady()) {
            return;
        }
        drawTravelRings(lastOrigin.lat, lastOrigin.lng);
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

    function destinationLngLat(lat, lng, distanceM, bearingDeg) {
        const R = 6378137;
        const br = bearingDeg * Math.PI / 180;
        const lat1 = lat * Math.PI / 180;
        const lng1 = lng * Math.PI / 180;
        const ang = distanceM / R;
        const lat2 = Math.asin(Math.sin(lat1) * Math.cos(ang) + Math.cos(lat1) * Math.sin(ang) * Math.cos(br));
        const lng2 = lng1 + Math.atan2(
            Math.sin(br) * Math.sin(ang) * Math.cos(lat1),
            Math.cos(ang) - Math.sin(lat1) * Math.sin(lat2)
        );
        return [lng2 * 180 / Math.PI, lat2 * 180 / Math.PI];
    }

    function circlePolygon(lat, lng, radiusM) {
        const coords = [];
        for (let i = 0; i <= 64; i++) {
            coords.push(destinationLngLat(lat, lng, radiusM, i * (360 / 64)));
        }
        return [coords];
    }

    function scheduleTravelRingRedraw() {
        if (!map || ringRedrawBound || ringRedrawTries >= 12) {
            return;
        }
        ringRedrawBound = true;
        ringRedrawTries += 1;
        map.once("idle", function () {
            ringRedrawBound = false;
            if (lastOrigin) {
                drawTravelRings(lastOrigin.lat, lastOrigin.lng);
            }
        });
    }

    function eachTravelRing(lat, lng, fn) {
        const minutes = ringMinutes(travelOptions.maxMinutes);
        const cap = maxRingRadiusMeters();
        minutes.forEach(function (mins, index) {
            const radius = ringRadiusForMinutes(mins);
            if (radius < 40) {
                return;
            }
            if (index > 0 && radius >= cap - 1 && ringRadiusForMinutes(minutes[index - 1]) >= cap - 1) {
                return;
            }
            fn(mins, index, radius, index === minutes.length - 1);
        });
    }

    function buildTravelRingFeatures(lat, lng) {
        const features = [];
        eachTravelRing(lat, lng, function (mins, index, radius, outer) {
            features.push({
                type: "Feature",
                properties: { index: index, outer: outer ? 1 : 0 },
                geometry: { type: "Polygon", coordinates: circlePolygon(lat, lng, radius) }
            });
        });
        return features;
    }

    function placeTravelRingLabels(lat, lng) {
        clearTravelRingLabels();
        const transport = travelOptions.transport || "Fiets";
        const labelVerb = TRANSPORT_LABEL[transport] || "reistijd";
        eachTravelRing(lat, lng, function (mins, index, radius) {
            // East-southeast keeps labels off the featured carousel and zoom stack.
            const labelLngLat = destinationLngLat(lat, lng, radius, 125);
            const effectiveMins = Math.max(1, Math.round(radius / metersPerMinute(transport)));
            const labelEl = document.createElement("div");
            labelEl.className = "travel-ring-label";
            labelEl.innerHTML = "<span>" + effectiveMins + " min " + escapeHtml(labelVerb) + "</span>";
            const labelMarker = new maplibregl.Marker({
                element: labelEl,
                anchor: "center",
                pitchAlignment: "viewport",
                rotationAlignment: "viewport"
            })
                .setLngLat(labelLngLat)
                .addTo(map);
            travelRingLayers.push(labelMarker);
        });
    }

    function drawTravelRings(lat, lng) {
        if (!map || typeof map.addSource !== "function") {
            return;
        }
        if (typeof map.isStyleLoaded === "function" && !map.isStyleLoaded()) {
            scheduleTravelRingRedraw();
            return;
        }

        const features = buildTravelRingFeatures(lat, lng);
        if (!features.length) {
            clearTravelRings();
            return;
        }

        const sourceId = "jobsy-travel-rings";
        const fillId = sourceId + "-fill";
        const haloId = sourceId + "-halo";
        const lineId = sourceId + "-line";

        try {
            const data = { type: "FeatureCollection", features: features };
            if (map.getSource(sourceId)) {
                map.getSource(sourceId).setData(data);
            } else {
                map.addSource(sourceId, {
                    type: "geojson",
                    data: data
                });
            }

            if (!map.getLayer(fillId)) {
                map.addLayer({
                    id: fillId,
                    type: "fill",
                    source: sourceId,
                    paint: {
                        "fill-color": "#0d6efd",
                        "fill-opacity": 0.16
                    }
                });
            }
            if (!map.getLayer(haloId)) {
                map.addLayer({
                    id: haloId,
                    type: "line",
                    source: sourceId,
                    paint: {
                        "line-color": "#ffffff",
                        "line-width": 5,
                        "line-opacity": 0.72
                    }
                });
            }
            if (!map.getLayer(lineId)) {
                map.addLayer({
                    id: lineId,
                    type: "line",
                    source: sourceId,
                    paint: {
                        "line-color": "#0d6efd",
                        "line-width": [
                            "case",
                            ["==", ["get", "outer"], 1],
                            3.2,
                            2.4
                        ],
                        "line-opacity": 0.95
                    }
                });
            }
            travelRingGeo = { sourceId: sourceId, ids: [fillId, haloId, lineId] };
            ringRedrawTries = 0;
        } catch (e) {
            scheduleTravelRingRedraw();
            return;
        }

        placeTravelRingLabels(lat, lng);
    }

    function ringBounds(lat, lng, radiusM) {
        const bounds = new maplibregl.LngLatBounds();
        [0, 90, 180, 270].forEach(function (bearing) {
            bounds.extend(destinationLngLat(lat, lng, radiusM, bearing));
        });
        return bounds;
    }

    function overlayFitPadding() {
        const edge = 36;
        const padding = { top: edge, right: edge, bottom: edge, left: edge };
        if (!map) {
            return padding;
        }
        const container = map.getContainer();
        const pane = container && container.closest ? container.closest(".map-pane") : null;
        if (pane) {
            const carousel = pane.querySelector(".highlight-carousel--map");
            if (carousel && carousel.offsetParent !== null && carousel.offsetHeight > 0) {
                padding.top = Math.max(padding.top, carousel.offsetHeight + 28);
            }
        }
        const locate = container ? container.querySelector(".job-map-locate") : null;
        if (locate && locate.offsetHeight) {
            // Locate sits at the corner; zoom + 3D sit above it (bottom: 58px).
            padding.bottom = Math.max(padding.bottom, 110);
        }
        padding.right = Math.max(padding.right, 52);
        return padding;
    }

    function sameOrigin(lat, lng) {
        return !!(lastOrigin
            && Math.abs(lastOrigin.lat - lat) < 1e-7
            && Math.abs(lastOrigin.lng - lng) < 1e-7);
    }

    function fitToOriginRings(animate) {
        if (!map || !lastOrigin) {
            return;
        }
        const radius = maxRingRadiusMeters() * 1.12;
        if (!(radius > 40)) {
            finishOpeningFrame();
            return;
        }
        const opening = deferMapReveal || originHasBeenFramed !== true;
        const opts = {
            padding: overlayFitPadding(),
            maxZoom: 13,
            animate: animate === true && !opening && !prefersReducedMotion(),
            duration: animate === true && !opening && !prefersReducedMotion() ? 450 : 0
        };
        if (!mapHasUsableSize()) {
            originNeedsFrame = true;
            safeJumpTo({
                center: [lastOrigin.lng, lastOrigin.lat],
                zoom: FILLED_LOCATION_ZOOM
            });
            finishOpeningFrame();
            return;
        }
        try {
            map.fitBounds(ringBounds(lastOrigin.lat, lastOrigin.lng, radius), opts);
            originNeedsFrame = false;
            originHasBeenFramed = true;
            cameraLocked = true;
            firstSizedFit = true;
            finishOpeningFrame();
            map.once("idle", function () {
                if (lastOrigin) {
                    drawTravelRings(lastOrigin.lat, lastOrigin.lng);
                }
            });
        } catch (e) {
            originNeedsFrame = true;
            safeJumpTo({
                center: [lastOrigin.lng, lastOrigin.lat],
                zoom: FILLED_LOCATION_ZOOM
            });
            finishOpeningFrame();
        }
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

    function readCoord(obj, names) {
        if (!obj) {
            return NaN;
        }
        for (let i = 0; i < names.length; i++) {
            const n = Number(obj[names[i]]);
            if (Number.isFinite(n)) {
                return n;
            }
        }
        return NaN;
    }

    function pointFromVacancy(v) {
        const lat = readCoord(v, ["lat", "Lat", "latitude", "Latitude"]);
        const lng = readCoord(v, ["lng", "Lng", "lon", "Lon", "longitude", "Longitude"]);
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return null;
        }
        return [lat, lng];
    }

    function collectVacancyPoints(vacancies) {
        const points = [];
        (vacancies || []).forEach(function (v) {
            const pt = pointFromVacancy(v);
            if (pt) {
                points.push(pt);
            }
        });
        return points;
    }

    function readOpeningView(raw) {
        if (!raw || typeof raw !== "object") {
            return null;
        }
        const lat = Number(raw.lat);
        const lng = Number(raw.lng);
        const zoom = Number(raw.zoom);
        if (!Number.isFinite(lat) || !Number.isFinite(lng) || !Number.isFinite(zoom)) {
            return null;
        }
        return { lat: lat, lng: lng, zoom: zoom };
    }

    function readFilledOrigin() {
        try {
            if (!window.jobsyGeo || typeof window.jobsyGeo.getStoredOrigin !== "function") {
                return null;
            }
            const stored = window.jobsyGeo.getStoredOrigin();
            if (!stored) {
                return null;
            }
            const lat = Number(stored.lat);
            const lng = Number(stored.lng);
            if (!Number.isFinite(lat) || !Number.isFinite(lng)
                || Math.abs(lat) > 90 || Math.abs(lng) > 180) {
                return null;
            }
            return { lat: lat, lng: lng, zoom: FILLED_LOCATION_ZOOM };
        } catch (e) {
            return null;
        }
    }

    function openingCamera(openingPoints, openingView, preferFilledLocation) {
        if (preferFilledLocation !== false) {
            const filled = readFilledOrigin();
            if (filled) {
                return { center: [filled.lng, filled.lat], zoom: filled.zoom, locked: true };
            }
        }
        const view = readOpeningView(openingView);
        if (view) {
            return { center: [view.lng, view.lat], zoom: view.zoom, locked: true };
        }
        if (openingPoints && openingPoints.length) {
            const startBounds = boundsFromPoints(openingPoints);
            const start = startBounds.getCenter();
            return { center: [start.lng, start.lat], zoom: zoomForPoints(openingPoints), locked: true };
        }
        return { center: [NL_CENTER[1], NL_CENTER[0]], zoom: NL_ZOOM, locked: true };
    }

    function safeJumpTo(opts) {
        if (!map || !opts) {
            return;
        }
        try {
            map.jumpTo(opts);
        } catch (e) { }
    }

    function safeEaseTo(opts) {
        if (!map || !opts) {
            return;
        }
        try {
            map.easeTo(opts);
        } catch (e) { }
    }

    function lockCamera(openingPoints, openingView, preferFilledLocation) {
        if (cameraLocked || !map) {
            return;
        }
        const opening = openingCamera(openingPoints, openingView, preferFilledLocation);
        if (!opening.locked) {
            return;
        }
        safeJumpTo({ center: opening.center, zoom: opening.zoom });
        cameraLocked = true;
        firstSizedFit = true;
    }

    function zoomForPoints(points) {
        if (!points.length) {
            return 8;
        }
        let minLat = 90, maxLat = -90, minLng = 180, maxLng = -180;
        points.forEach(function (p) {
            minLat = Math.min(minLat, p[0]);
            maxLat = Math.max(maxLat, p[0]);
            minLng = Math.min(minLng, p[1]);
            maxLng = Math.max(maxLng, p[1]);
        });
        const span = Math.max(maxLng - minLng, maxLat - minLat);
        if (span < 0.08) return 13;
        if (span < 0.2) return 12;
        if (span < 0.5) return 11;
        if (span < 1.2) return 10;
        if (span < 2.5) return 9;
        return 8;
    }

    function revealMapStage() {
        if (!map || deferMapReveal) {
            return;
        }
        const stage = map.getContainer() && map.getContainer().closest(".map-stage");
        if (stage) {
            stage.classList.add("is-live");
        }
    }

    function finishOpeningFrame() {
        deferMapReveal = false;
        revealMapStage();
    }

    function mapHasUsableSize() {
        if (!map) {
            return false;
        }
        const c = map.getContainer();
        return !!(c && c.clientWidth >= 32 && c.clientHeight >= 32);
    }

    function vacancyPoints() {
        return Object.keys(markersById).map(function (id) {
            return [markersById[id].lat, markersById[id].lng];
        });
    }

    function boundsFromPoints(points) {
        const bounds = new maplibregl.LngLatBounds();
        points.forEach(function (p) {
            bounds.extend([p[1], p[0]]);
        });
        return bounds;
    }

    function fitMapToVacancies(markerBounds) {
        if (!map) return;

        const points = Array.isArray(markerBounds) && markerBounds.length
            ? markerBounds.slice()
            : vacancyPoints();
        lastFitPoints = points.slice();
        if (points.length === 0) {
            return;
        }

        if (cameraLocked || firstSizedFit) {
            ensureVacancyTiles();
            revealMapStage();
            return;
        }

        const opening = !firstSizedFit;
        const opts = { padding: 48, maxZoom: 13, animate: !opening, duration: opening ? 0 : 450 };
        const bounds = boundsFromPoints(points);
        if (mapHasUsableSize()) {
            try {
                map.fitBounds(bounds, opts);
            } catch (e) { }
            firstSizedFit = true;
        } else {
            safeJumpTo({
                center: bounds.getCenter(),
                zoom: zoomForPoints(points)
            });
        }
        ensureVacancyTiles();
        revealMapStage();
    }

    function ensureVacancyTiles() {
        if (!map || tileLayer) {
            return;
        }
        tileLayer = { kind: "openfreemap-vector" };
        const fire = function () {
            revealMapStage();
            if (openCallback && typeof openCallback.invokeMethodAsync === "function") {
                openCallback.invokeMethodAsync("OnMapTilesReady");
            }
        };
        if (map.loaded()) {
            map.once("idle", fire);
        } else {
            map.once("load", function () {
                map.once("idle", fire);
            });
        }
    }

    function clusterProject(lat, lng) {
        const p = maplibregl.MercatorCoordinate.fromLngLat({ lon: lng, lat: lat });
        const scale = 512 * Math.pow(2, map.getZoom());
        return { x: p.x * scale, y: p.y * scale };
    }

    function computeClusters() {
        const items = Object.keys(markersById).map(function (id) {
            return markersById[id];
        });
        const zoom = map.getZoom();
        if (zoom >= CLUSTER_OPTS.disableClusteringAtZoom) {
            return items.map(function (item) {
                return { type: "pin", items: [item], lat: item.lat, lng: item.lng };
            });
        }

        const radius = CLUSTER_OPTS.maxClusterRadius;
        const pts = items.map(function (item) {
            const p = clusterProject(item.lat, item.lng);
            return { item: item, x: p.x, y: p.y, used: false };
        });
        const clusters = [];
        for (let i = 0; i < pts.length; i++) {
            if (pts[i].used) {
                continue;
            }
            const group = [pts[i]];
            pts[i].used = true;
            for (let j = i + 1; j < pts.length; j++) {
                if (pts[j].used) {
                    continue;
                }
                const dx = pts[i].x - pts[j].x;
                const dy = pts[i].y - pts[j].y;
                if (dx * dx + dy * dy <= radius * radius) {
                    pts[j].used = true;
                    group.push(pts[j]);
                }
            }
            if (group.length === 1) {
                clusters.push({
                    type: "pin",
                    items: [group[0].item],
                    lat: group[0].item.lat,
                    lng: group[0].item.lng
                });
            } else {
                let lat = 0;
                let lng = 0;
                group.forEach(function (g) {
                    lat += g.item.lat;
                    lng += g.item.lng;
                });
                clusters.push({
                    type: "cluster",
                    items: group.map(function (g) { return g.item; }),
                    lat: lat / group.length,
                    lng: lng / group.length
                });
            }
        }
        return clusters;
    }

    function clearRenderedMarkers() {
        renderedMarkers.forEach(function (m) {
            if (m && typeof m.remove === "function") {
                m.remove();
            }
        });
        renderedMarkers = [];
        Object.keys(markersById).forEach(function (id) {
            markersById[id].marker = null;
            markersById[id].element = null;
        });
    }

    function refreshClusters() {
        if (!map) {
            return;
        }
        clearRenderedMarkers();
        const clusters = computeClusters();
        clusters.forEach(function (cluster) {
            if (cluster.type === "cluster") {
                const count = cluster.items.length;
                let sizeClass = "job-cluster--sm";
                if (count >= 10) sizeClass = "job-cluster--lg";
                else if (count >= 4) sizeClass = "job-cluster--md";
                const hasFeatured = cluster.items.some(function (m) {
                    return m && m.options && m.options.jobData && m.options.jobData.highlighted;
                });
                const el = document.createElement("div");
                el.className = "job-cluster " + sizeClass + (hasFeatured ? " job-cluster--featured" : "");
                el.innerHTML = "<div><span>" + count + "</span></div>";
                el.addEventListener("click", function (ev) {
                    stopEvent(ev);
                    openClusterList(cluster.items, [cluster.lng, cluster.lat]);
                });
                const marker = new maplibregl.Marker({
                    element: el,
                    anchor: "center",
                    pitchAlignment: "viewport",
                    rotationAlignment: "viewport"
                })
                    .setLngLat([cluster.lng, cluster.lat])
                    .addTo(map);
                renderedMarkers.push(marker);
                return;
            }

            const record = cluster.items[0];
            const v = record.options.jobData;
            const el = document.createElement("div");
            fillMarkerElement(
                el,
                !!v.highlighted,
                selectedId != null && String(record.id) === String(selectedId),
                workTypeOf(v),
                v.categoryColor
            );
            el.addEventListener("click", function (ev) {
                stopEvent(ev);
                highlight(v.id);
                openVacancyPopup(record);
            });
            const marker = new maplibregl.Marker({
                element: el,
                anchor: "center",
                pitchAlignment: "viewport",
                rotationAlignment: "viewport"
            })
                .setLngLat([record.lng, record.lat])
                .addTo(map);
            record.marker = marker;
            record.element = el;
            renderedMarkers.push(marker);
        });
    }

    function restoreOverlays() {
        refreshClusters();
        if (lastOrigin) {
            ensureOriginMarker(lastOrigin.lat, lastOrigin.lng);
            drawTravelRings(lastOrigin.lat, lastOrigin.lng);
        }
        if (window.jobsyMapLibre) {
            window.jobsyMapLibre.hideChrome(map);
        }
    }

    function createMapInstance(el, openingPoints, openingView, preferFilledLocation) {
        firstSizedFit = false;
        lastFitPoints = [];
        tileLayer = null;
        selectedId = null;
        cameraLocked = false;
        originHasBeenFramed = false;
        originNeedsFrame = false;
        ringRedrawBound = false;
        ringStyleHandlerBound = false;
        ringRedrawTries = 0;

        const opening = openingCamera(openingPoints, openingView, preferFilledLocation);
        if (openingPoints && openingPoints.length) {
            lastFitPoints = openingPoints.slice();
        }

        map = window.jobsyMapLibre.createMap(el, {
            center: opening.center,
            zoom: opening.zoom,
            controlsPosition: "bottom-right"
        });
        cameraLocked = opening.locked;
        firstSizedFit = true;
        map._jobsyOnStyleRestored = restoreOverlays;
        map.on("load", function () {
            restoreOverlays();
        });
        if (typeof map.loaded === "function" && map.loaded()) {
            restoreOverlays();
        }
        window.addEventListener("resize", invalidate);
    }

    function bindMapRuntime() {
        if (!map) {
            return;
        }
        if (!clusterGroup) {
            clusterGroup = {
                refreshClusters: refreshClusters,
                clearLayers: function () {
                    clearRenderedMarkers();
                    markersById = {};
                },
                zoomToShowLayer: function (record, cb) {
                    map.easeTo({
                        center: [record.lng, record.lat],
                        zoom: Math.max(map.getZoom(), CLUSTER_OPTS.disableClusteringAtZoom),
                        duration: 280
                    });
                    map.once("idle", function () {
                        refreshClusters();
                        if (typeof cb === "function") {
                            cb();
                        }
                    });
                }
            };
        }
        if (!zoomHandlerBound) {
            zoomHandlerBound = true;
            map.on("zoomend", refreshClusters);
        }
        addLocateControl();
        bindOutsideClickCloser();
        bindTravelRingStyleGuard();
    }

    function readBootPayload() {
        const node = document.getElementById("jobsy-map-boot");
        if (!node || !node.textContent) {
            return { pins: [], view: null, preferFilledLocation: true };
        }
        try {
            const parsed = JSON.parse(node.textContent);
            if (Array.isArray(parsed)) {
                return { pins: parsed, view: null, preferFilledLocation: true };
            }
            const pins = parsed && Array.isArray(parsed.pins) ? parsed.pins : [];
            let view = parsed ? readOpeningView(parsed.view) : null;
            const preferFilledLocation = !parsed || parsed.preferFilledLocation !== false;
            if (preferFilledLocation) {
                const filled = readFilledOrigin();
                if (filled) {
                    view = filled;
                }
            }
            return { pins: pins, view: view, preferFilledLocation: preferFilledLocation };
        } catch (e) {
            return { pins: [], view: null, preferFilledLocation: true };
        }
    }

    // Paint the basemap immediately from a single jobMap.init after hydrate.
    // Pre-circuit boot() was discarded when Blazor replaced #job-map (double tile load).
    function boot(elementId) {
        return;
    }

    function init(elementId, vacancies, options) {
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

        const openingPoints = collectVacancyPoints(vacancies || []);
        const openingView = options && options.view ? options.view : null;
        const preferFilledLocation = !options || options.preferFilledLocation !== false;
        const filledOrigin = preferFilledLocation ? readFilledOrigin() : null;
        const hasOrigin = !!(options && options.origin) || !!filledOrigin;
        const live = !!(map && typeof map.getContainer === "function"
            && map.getContainer() === el && el.isConnected);

        if (!live) {
            if (map) {
                dispose();
            }
            deferMapReveal = hasOrigin;
            try {
                createMapInstance(el, openingPoints, openingView, preferFilledLocation);
            } catch (e) {
                createMapInstance(el, openingPoints, null, false);
            }
        }

        openCallback = options && options.dotNetRef ? options.dotNetRef : null;
        normalizeTravelOptions(options && options.travel);
        highlightSeed = options && Number.isFinite(Number(options.highlightSeed))
            ? (Number(options.highlightSeed) >>> 0)
            : 0;

        bindMapRuntime();

        setVacancies(vacancies || []);
        var originApplied = false;
        if (options && options.origin) {
            try {
                setOrigin(options.origin.lat, options.origin.lng, options.travel);
                originApplied = true;
            } catch (e) { }
        } else if (filledOrigin) {
            try {
                setOrigin(filledOrigin.lat, filledOrigin.lng, options && options.travel);
                originApplied = true;
            } catch (e) { }
        }
        ensureVacancyTiles();
        if (!originApplied) {
            finishOpeningFrame();
        }

        invalidate();
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
        const host = map.getContainer();
        if (host.querySelector(".job-map-locate")) {
            syncLocateButton();
            return;
        }

        const bar = document.createElement("div");
        bar.className = "job-map-locate";
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "job-map-locate__btn";
        btn.title = "Mijn locatie";
        btn.setAttribute("aria-label", "Mijn locatie");
        btn.innerHTML = locateIconHtml();
        bar.appendChild(btn);
        host.appendChild(bar);

        btn.addEventListener("click", function (ev) {
            stopEvent(ev);
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
                        fitToOriginRings(true);
                    })
                    .then(done, done);
            } else {
                done();
            }
        });

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
            const pt = pointFromVacancy(v);
            if (!pt) {
                return;
            }
            const lat = pt[0];
            const lng = pt[1];
            const record = {
                id: v.id,
                lat: lat,
                lng: lng,
                marker: null,
                element: null,
                options: { jobData: v },
                getLatLng: function () { return { lat: lat, lng: lng }; },
                getLngLat: function () { return { lng: lng, lat: lat }; }
            };
            markersById[v.id] = record;
            bounds.push([lat, lng]);
        });

        refreshClusters();
        ensureVacancyTiles();
        revealMapStage();
    }

    function ensureOriginMarker(la, ln) {
        if (originMarker) {
            originMarker.setLngLat([ln, la]);
            return;
        }
        const el = document.createElement("div");
        el.className = "job-map-origin";
        el.title = "Jouw locatie";
        originMarker = new maplibregl.Marker({
            element: el,
            anchor: "center",
            pitchAlignment: "viewport",
            rotationAlignment: "viewport"
        })
            .setLngLat([ln, la])
            .addTo(map);
    }

    function setOrigin(lat, lng, travel) {
        if (!map) {
            finishOpeningFrame();
            return;
        }
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) {
            finishOpeningFrame();
            return;
        }

        const originChanged = !sameOrigin(la, ln);
        normalizeTravelOptions(travel);
        lastOrigin = { lat: la, lng: ln };

        ensureOriginMarker(la, ln);
        drawTravelRings(la, ln);
        syncLocateButton();

        if (originChanged || !originHasBeenFramed) {
            fitToOriginRings(originHasBeenFramed);
        } else {
            finishOpeningFrame();
        }
    }

    function setTravelOptions(options) {
        const prevRadius = lastOrigin ? maxRingRadiusMeters() : 0;
        normalizeTravelOptions(options);
        if (lastOrigin) {
            drawTravelRings(lastOrigin.lat, lastOrigin.lng);
            if (Math.abs(maxRingRadiusMeters() - prevRadius) > 1) {
                fitToOriginRings(true);
            }
        }
    }

    function clearOrigin() {
        clearTravelRings();
        lastOrigin = null;
        originHasBeenFramed = false;
        originNeedsFrame = false;
        if (originMarker) {
            originMarker.remove();
            originMarker = null;
        }
        syncLocateButton();
    }

    function panTo(lat, lng, zoom) {
        if (!map) {
            return;
        }
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) {
            return;
        }
        const opts = { center: [ln, la], duration: 400 };
        const z = Number(zoom);
        if (Number.isFinite(z) && z > 0) {
            opts.zoom = z;
        }
        safeEaseTo(opts);
    }

    function jumpToLocation(lat, lng, zoom) {
        if (!map) {
            return;
        }
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) {
            return;
        }
        const opts = { center: [ln, la] };
        const z = Number(zoom);
        if (Number.isFinite(z) && z > 0) {
            opts.zoom = z;
        }
        safeJumpTo(opts);
        cameraLocked = true;
        firstSizedFit = true;
    }

    function invalidate() {
        if (!map) {
            return;
        }
        map.resize();
        if (originNeedsFrame && lastOrigin) {
            fitToOriginRings(false);
        }
    }

    function isAlive() {
        if (!map || !clusterGroup) {
            return false;
        }
        const container = typeof map.getContainer === "function" ? map.getContainer() : null;
        return !!(container && container.isConnected);
    }

    function applyMarkerSelected(el, selected) {
        if (!el || !el.classList) {
            return;
        }
        el.classList.toggle("job-marker--active", !!selected);
    }

    function highlight(id) {
        if (id == null && activeClusterPopup) {
            return;
        }
        selectedId = id;
        Object.keys(markersById).forEach(function (key) {
            const record = markersById[key];
            if (record.element) {
                applyMarkerSelected(record.element, id != null && key === String(id));
            }
        });
    }

    function focus(id) {
        const record = markersById[id];
        if (!record || !map || !clusterGroup) {
            return;
        }

        highlight(id);

        clusterGroup.zoomToShowLayer(record, function () {
            openVacancyPopup(record);
        });
    }

    function normalizeCompanyId(value) {
        return String(value ?? "").toLowerCase().replace(/[{}-]/g, "");
    }

    function openCompanyClusterPopup(childMarkers) {
        if (!map || !childMarkers || childMarkers.length === 0) {
            return;
        }
        openClusterList(childMarkers, [childMarkers[0].lng, childMarkers[0].lat]);
    }

    /**
     * Deep-link helper for raamflyer QR: center on a vestiging and open the cluster
     * popup when 2+ vacancies share that company.
     */
    function focusCompany(companyId) {
        if (!map || !clusterGroup || companyId == null || companyId === "") {
            return;
        }

        const wanted = normalizeCompanyId(companyId);
        const markers = [];
        Object.keys(markersById).forEach(function (key) {
            const record = markersById[key];
            const data = record.options.jobData || {};
            if (normalizeCompanyId(data.companyId) === wanted) {
                markers.push(record);
            }
        });

        if (markers.length === 0) {
            return;
        }

        if (markers.length === 1) {
            focus(markers[0].options.jobData && markers[0].options.jobData.id);
            return;
        }

        invalidate();
        const points = markers.map(function (m) { return [m.lat, m.lng]; });
        map.fitBounds(boundsFromPoints(points), { padding: 64, maxZoom: 16, animate: true, duration: 450 });
        setTimeout(function () {
            invalidate();
            openCompanyClusterPopup(markers);
        }, 450);
    }

    function dispose() {
        window.removeEventListener("resize", invalidate);
        activeClusterPopup = null;
        openCallback = null;
        clearTravelRings();
        lastOrigin = null;
        originMarker = null;
        clearRenderedMarkers();
        if (map) {
            unbindOutsideClickCloser();
            map.remove();
            map = null;
        }
        clusterGroup = null;
        markersById = {};
        lastFitPoints = [];
        firstSizedFit = false;
        cameraLocked = false;
        originHasBeenFramed = false;
        originNeedsFrame = false;
        ringRedrawBound = false;
        ringStyleHandlerBound = false;
        ringRedrawTries = 0;
        deferMapReveal = false;
        tileLayer = null;
        selectedId = null;
        zoomHandlerBound = false;
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

    function safeBadgeColor(color) {
        const s = String(color ?? "").trim();
        return /^#[0-9A-Fa-f]{6}$/.test(s) ? s : "#64748b";
    }

    return {
        boot,
        init,
        setVacancies,
        setOrigin,
        panTo,
        jumpToLocation,
        fitToOriginRings,
        setTravelOptions,
        clearOrigin,
        highlight,
        focus,
        focusCompany,
        dispose,
        invalidate,
        isAlive
    };
})();
