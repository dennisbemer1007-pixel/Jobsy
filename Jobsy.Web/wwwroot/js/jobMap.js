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

    // Used to paint the basemap immediately (before the vacancy catalog arrives).
    const NL_CENTER = [52.15, 5.2913];
    const NL_ZOOM = 7;
    const NL_BOUNDS = [[50.29, 2.81], [53.33, 8.44]];

    const CLUSTER_OPTS = {
        showCoverageOnHover: false,
        zoomToBoundsOnClick: false,
        spiderfyOnMaxZoom: false,
        disableClusteringAtZoom: 16,
        maxClusterRadius: 60,
        removeOutsideVisibleBounds: false
    };

    const SPEED_M_PER_MIN = {
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

    function jobPopupOptions() {
        const vw = window.innerWidth || 360;
        const narrow = isNarrowViewport();
        const width = narrow
            ? Math.max(280, Math.min(340, vw - 24))
            : 520;
        return {
            className: "job-map-popup",
            maxWidth: width + "px",
            anchor: "bottom",
            offset: JOB_POPUP_OFFSET,
            closeOnClick: true,
            closeButton: true
        };
    }

    function clusterPopupOptions() {
        const opts = jobPopupOptions();
        opts.className = opts.className + " job-map-popup--cluster";
        opts.offset = CLUSTER_POPUP_OFFSET;
        return opts;
    }

    function applyPopupOptions(popup) {
        if (!popup) {
            return;
        }
        const opts = clusterPopupOptions();
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
                "<img class=\"map-popup__photo\" src=\"" + escapeAttr(v.imageUrl) + "\" alt=\"\" loading=\"lazy\" data-logo-fallback=\"1\" onerror=\"window.jobsyLogoFallback&&window.jobsyLogoFallback(this)\" />";
        } else if (v.logoUrl) {
            mediaInner +=
                "<img class=\"map-popup__media-logo\" src=\"" + escapeAttr(v.logoUrl) + "\" alt=\"" +
                escapeAttr(v.company) + " logo\" loading=\"lazy\" data-logo-fallback=\"1\" onerror=\"window.jobsyLogoFallback&&window.jobsyLogoFallback(this)\" />";
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
                applyPopupOptions(popup);
                popup.setHTML(buildClusterSingleHtml(childMarkers, nextPage));
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
        const opts = Object.assign({}, jobPopupOptions());
        activeClusterPopup = popupFromOpts(opts, [record.lng, record.lat], buildPopupHtml(v));
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

        const opts = Object.assign({}, clusterPopupOptions());
        const ll = lngLat || [childMarkers[0].lng, childMarkers[0].lat];
        activeClusterPopup = popupFromOpts(opts, ll, buildClusterSingleHtml(childMarkers, 1));
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
        return SPEED_M_PER_MIN[transport] || SPEED_M_PER_MIN.Fiets;
    }

    function clearTravelRings() {
        if (map && travelRingGeo) {
            travelRingGeo.ids.forEach(function (id) {
                if (map.getLayer(id)) {
                    map.removeLayer(id);
                }
            });
            if (map.getSource(travelRingGeo.sourceId)) {
                map.removeSource(travelRingGeo.sourceId);
            }
        }
        travelRingLayers.forEach(function (layer) {
            if (layer && typeof layer.remove === "function") {
                layer.remove();
            }
        });
        travelRingLayers = [];
        travelRingGeo = null;
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

    function drawTravelRings(lat, lng) {
        clearTravelRings();
        if (!map || typeof map.addSource !== "function") {
            return;
        }

        const transport = travelOptions.transport || "Fiets";
        const labelVerb = TRANSPORT_LABEL[transport] || "reistijd";
        const minutes = ringMinutes(travelOptions.maxMinutes);
        const cap = maxRingRadiusMeters();
        const features = [];
        const layerIds = [];

        minutes.forEach(function (mins, index) {
            const radius = ringRadiusForMinutes(mins);
            if (radius < 40) return;
            if (index > 0 && radius >= cap - 1 && ringRadiusForMinutes(minutes[index - 1]) >= cap - 1) {
                return;
            }

            features.push({
                type: "Feature",
                properties: {
                    index: index,
                    outer: index === minutes.length - 1
                },
                geometry: {
                    type: "Polygon",
                    coordinates: circlePolygon(lat, lng, radius)
                }
            });

            const labelLngLat = destinationLngLat(lat, lng, radius, 0);
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

        if (!features.length || !map.isStyleLoaded()) {
            return;
        }

        const sourceId = "jobsy-travel-rings";
        map.addSource(sourceId, {
            type: "geojson",
            data: { type: "FeatureCollection", features: features }
        });
        const fillId = sourceId + "-fill";
        const lineId = sourceId + "-line";
        map.addLayer({
            id: fillId,
            type: "fill",
            source: sourceId,
            paint: {
                "fill-color": "#007bff",
                "fill-opacity": 0.1
            }
        });
        map.addLayer({
            id: lineId,
            type: "line",
            source: sourceId,
            paint: {
                "line-color": "#007bff",
                "line-width": 1.6,
                "line-opacity": 0.7
            }
        });
        layerIds.push(fillId, lineId);
        travelRingGeo = { sourceId: sourceId, ids: layerIds };
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
        if (!map) {
            return;
        }
        const stage = map.getContainer() && map.getContainer().closest(".map-stage");
        if (stage) {
            stage.classList.add("is-live");
        }
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

        const opening = !firstSizedFit;
        const opts = { padding: 48, maxZoom: 13, animate: !opening, duration: opening ? 0 : 450 };
        const bounds = boundsFromPoints(points);
        if (mapHasUsableSize()) {
            map.fitBounds(bounds, opts);
            firstSizedFit = true;
        } else {
            map.jumpTo({
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

    function createMapInstance(el, openingPoints) {
        firstSizedFit = false;
        lastFitPoints = [];
        tileLayer = null;
        selectedId = null;

        let center;
        let zoom;
        if (openingPoints && openingPoints.length) {
            lastFitPoints = openingPoints.slice();
            const startBounds = boundsFromPoints(openingPoints);
            const start = startBounds.getCenter();
            center = [start.lng, start.lat];
            zoom = zoomForPoints(openingPoints);
        } else {
            center = [NL_CENTER[1], NL_CENTER[0]];
            zoom = NL_ZOOM;
        }

        map = window.jobsyMapLibre.createMap(el, {
            center: center,
            zoom: zoom
        });
        map._jobsyOnStyleRestored = restoreOverlays;
        map.on("load", function () {
            restoreOverlays();
            invalidate();
        });
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
    }

    // Paint the basemap immediately as soon as #job-map exists — do not wait for Blazor/catalog.
    function boot(elementId) {
        if (map) {
            return;
        }
        if (typeof maplibregl === "undefined" || !window.jobsyMapLibre) {
            return;
        }
        const el = document.getElementById(elementId || "job-map");
        if (!el) {
            return;
        }
        createMapInstance(el, []);
        bindMapRuntime();
        revealMapStage();
        invalidate();
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
        const reuse = !!(map && typeof map.getContainer === "function"
            && map.getContainer() === el && el.isConnected);

        if (!reuse) {
            if (map) {
                dispose();
            }
            createMapInstance(el, openingPoints);
        }

        openCallback = options && options.dotNetRef ? options.dotNetRef : null;
        normalizeTravelOptions(options && options.travel);
        highlightSeed = options && Number.isFinite(Number(options.highlightSeed))
            ? (Number(options.highlightSeed) >>> 0)
            : 0;

        bindMapRuntime();

        setVacancies(vacancies || []);
        if (options && options.origin) {
            setOrigin(options.origin.lat, options.origin.lng, options.travel);
        }
        ensureVacancyTiles();

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
        fitMapToVacancies(bounds);
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
        if (!map) return;
        const la = Number(lat);
        const ln = Number(lng);
        if (!Number.isFinite(la) || !Number.isFinite(ln)) return;

        normalizeTravelOptions(travel);
        lastOrigin = { lat: la, lng: ln };

        ensureOriginMarker(la, ln);
        drawTravelRings(la, ln);
        syncLocateButton();
        fitMapToVacancies(vacancyPoints());
    }

    function setTravelOptions(options) {
        normalizeTravelOptions(options);
        if (lastOrigin) {
            drawTravelRings(lastOrigin.lat, lastOrigin.lng);
            fitMapToVacancies(vacancyPoints());
        }
    }

    function clearOrigin() {
        clearTravelRings();
        lastOrigin = null;
        if (originMarker) {
            originMarker.remove();
            originMarker = null;
        }
        syncLocateButton();
    }

    function invalidate() {
        if (!map) {
            return;
        }
        map.resize();
        if (!firstSizedFit && lastFitPoints.length > 0) {
            fitMapToVacancies(lastFitPoints);
        }
    }

    function isAlive() {
        if (!map || !clusterGroup) {
            return false;
        }
        const container = typeof map.getContainer === "function" ? map.getContainer() : null;
        return !!(container && container.isConnected);
    }

    function highlight(id) {
        selectedId = id;
        Object.keys(markersById).forEach(function (key) {
            const record = markersById[key];
            const data = record.options.jobData || {};
            const featured = !!data.highlighted;
            const selected = id != null && key === String(id);
            if (record.element) {
                fillMarkerElement(record.element, featured, selected, workTypeOf(data), data.categoryColor);
                record.element.style.zIndex = featured || selected ? "1000" : "";
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
