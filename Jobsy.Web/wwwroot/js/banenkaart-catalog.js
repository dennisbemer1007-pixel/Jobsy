(function () {
    "use strict";

    var STORAGE_BODY = "jobsy.bk.catalog.body.v1";
    var STORAGE_ETAG = "jobsy.bk.catalog.etag.v1";
    var STORAGE_AT = "jobsy.bk.catalog.at.v1";
    var REVALIDATE_MS = 15 * 60 * 1000;
    var CATALOG_URL = "/api/banenkaart-client/all";

    var memory = null;
    var rows = null;
    var lastEtag = null;
    var lastLoadedAt = 0;
    var networkInFlight = null;
    var listShown = 20;
    var currentListFilters = null;
    var LIST_PAGE = 20;

    var WORK_TYPE_FLAG = {
        horeca: 1, winkel: 2, logistiek: 4, tuinbouw: 8, zorg: 16,
        kantoor: 32, bouw: 64, schoonmaak: 128, productie: 256
    };

    var IDB_NAME = "jobsy.bk.catalog.v1";
    var IDB_STORE = "catalog";

    function openIdb() {
        return new Promise(function (resolve, reject) {
            if (!window.indexedDB) {
                resolve(null);
                return;
            }
            var req = indexedDB.open(IDB_NAME, 1);
            req.onupgradeneeded = function () {
                req.result.createObjectStore(IDB_STORE);
            };
            req.onsuccess = function () { resolve(req.result); };
            req.onerror = function () { reject(req.error); };
        });
    }

    function idbWrite(body, etag) {
        openIdb().then(function (db) {
            if (!db) {
                return;
            }
            var tx = db.transaction(IDB_STORE, "readwrite");
            tx.objectStore(IDB_STORE).put({ body: body, etag: etag || null, at: Date.now() }, "v1");
        }).catch(function () { });
    }

    function idbRead() {
        return openIdb().then(function (db) {
            if (!db) {
                return null;
            }
            return new Promise(function (resolve) {
                var tx = db.transaction(IDB_STORE, "readonly");
                var get = tx.objectStore(IDB_STORE).get("v1");
                get.onsuccess = function () { resolve(get.result || null); };
                get.onerror = function () { resolve(null); };
            });
        }).catch(function () { return null; });
    }

    function saveData(body, etag) {
        lastEtag = etag || null;
        lastLoadedAt = Date.now();
        memory = body;
        rows = null;
        try {
            sessionStorage.setItem(STORAGE_BODY, body);
            if (etag) {
                sessionStorage.setItem(STORAGE_ETAG, etag);
            }
            sessionStorage.setItem(STORAGE_AT, String(lastLoadedAt));
        } catch (e) { }
        idbWrite(body, etag);
    }

    function ingestRaw(body, etag) {
        if (!body) {
            return;
        }
        saveData(body, etag);
        try {
            expand(body);
        } catch (eIngest) { }
    }

    function readCachedRaw() {
        if (memory) {
            return memory;
        }
        try {
            var body = sessionStorage.getItem(STORAGE_BODY);
            if (!body) {
                return null;
            }
            memory = body;
            lastEtag = sessionStorage.getItem(STORAGE_ETAG);
            lastLoadedAt = parseInt(sessionStorage.getItem(STORAGE_AT) || "0", 10) || 0;
            return body;
        } catch (e2) {
            return null;
        }
    }

    function expand(raw) {
        if (rows) {
            return rows;
        }
        var data = typeof raw === "string" ? JSON.parse(raw) : raw;
        var n = data.n || 0;
        rows = [];
        for (var i = 0; i < n; i++) {
            rows.push({
                id: data.id[i],
                lat: data.lat[i],
                lng: data.lng[i],
                title: data.t[i],
                company: data.c[i],
                offeredBy: data.ob[i],
                wageCents: data.w[i],
                minHours: data.h0[i] != null ? data.h0[i] : 0,
                maxHours: data.h1[i] != null ? data.h1[i] : 40,
                categoryId: data.cat[i],
                companyId: data.co[i],
                workTypes: data.wt[i],
                transportReq: data.tr[i],
                colour: data.col[i],
                suitable65: data.s65[i] === 1,
                publicMapRadiusKm: data.pm[i],
                imageUrl: data.iu[i],
                categoryName: data.cn ? data.cn[i] : "",
                place: data.pl ? data.pl[i] : ""
            });
        }
        return rows;
    }

    function transportMode(label) {
        var t = String(label || "Fiets").toLowerCase();
        if (t.indexOf("auto") !== -1 || t.indexOf("car") !== -1) {
            return "car";
        }
        if (t.indexOf("ov") !== -1 || t.indexOf("transit") !== -1 || t.indexOf("public") !== -1) {
            return "transit";
        }
        if (t.indexOf("loop") !== -1 || t.indexOf("walk") !== -1) {
            return "walk";
        }
        return "bike";
    }

    function crowFliesKmPerHour(mode) {
        if (mode === "car") {
            return 80 / 1.35;
        }
        if (mode === "transit") {
            return 25 / 1.5;
        }
        if (mode === "walk") {
            return 5 / 1.4;
        }
        return 18 / 1.7;
    }

    function haversineKm(lat1, lng1, lat2, lng2) {
        var r = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.sin(dLat / 2) * Math.sin(dLat / 2)
            + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180)
            * Math.sin(dLng / 2) * Math.sin(dLng / 2);
        return r * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function travelMinutes(origin, lat, lng, modeLabel) {
        if (!origin || origin.lat == null || origin.lng == null) {
            return null;
        }
        var km = haversineKm(origin.lat, origin.lng, lat, lng);
        var speed = crowFliesKmPerHour(transportMode(modeLabel));
        return Math.ceil((km / speed) * 60);
    }

    function matchesFilters(row, filters) {
        if (!filters) {
            return true;
        }
        if (filters.q) {
            var q = String(filters.q).toLowerCase();
            var hay = (row.title + " " + row.company + " " + (row.offeredBy || "")).toLowerCase();
            if (hay.indexOf(q) === -1) {
                return false;
            }
        }
        if (filters.categoryIds && filters.categoryIds.length) {
            var cat = row.categoryId || "";
            var ok = false;
            for (var ci = 0; ci < filters.categoryIds.length; ci++) {
                if (filters.categoryIds[ci] === cat) {
                    ok = true;
                    break;
                }
            }
            if (!ok) {
                return false;
            }
        }
        if (filters.companyIds && filters.companyIds.length) {
            var coOk = false;
            for (var coi = 0; coi < filters.companyIds.length; coi++) {
                if (filters.companyIds[coi] === row.companyId) {
                    coOk = true;
                    break;
                }
            }
            if (!coOk) {
                return false;
            }
        }
        if (filters.workTypes && filters.workTypes.length) {
            var wt = Number(row.workTypes) || 0;
            var branchOk = false;
            for (var wi = 0; wi < filters.workTypes.length; wi++) {
                var key = String(filters.workTypes[wi] || "").toLowerCase();
                var bit = WORK_TYPE_FLAG[key] || 0;
                if (bit && (wt & bit) !== 0) {
                    branchOk = true;
                    break;
                }
            }
            if (!branchOk) {
                return false;
            }
        }
        var minH = filters.minHours != null ? filters.minHours : 0;
        var maxH = filters.maxHours != null ? filters.maxHours : 40;
        if (row.maxHours < minH || row.minHours > maxH) {
            return false;
        }
        if (filters.minWageCents != null && row.wageCents < filters.minWageCents) {
            return false;
        }
        if (filters.maxWageCents != null && row.wageCents > filters.maxWageCents) {
            return false;
        }
        return true;
    }

    function withinTravel(row, filters) {
        if (!filters || filters.originLat == null || filters.originLng == null) {
            return true;
        }
        var mins = travelMinutes(
            { lat: filters.originLat, lng: filters.originLng },
            row.lat,
            row.lng,
            filters.transport);
        if (mins == null) {
            return true;
        }
        var maxM = filters.maxMinutes || 20;
        var reachKm = crowFliesKmPerHour(transportMode(filters.transport)) * (maxM / 60) * 1.25;
        var km = haversineKm(filters.originLat, filters.originLng, row.lat, row.lng);
        return mins <= maxM && km <= reachKm;
    }

    function highlightRank(id, seed) {
        var h = (seed >>> 0) || 0;
        var s = String(id || "");
        for (var i = 0; i < s.length; i++) {
            h ^= s.charCodeAt(i);
            h = Math.imul(h, 16777619);
        }
        return h >>> 0;
    }

    function readHighlightSeed() {
        var node = document.getElementById("jobsy-map-boot");
        if (!node || !node.textContent) {
            return 0;
        }
        try {
            var parsed = JSON.parse(node.textContent);
            var seed = parsed && parsed.mapInit ? parsed.mapInit.highlightSeed : 0;
            return Number(seed) >>> 0;
        } catch (eSeed) {
            return 0;
        }
    }

    function sortRows(list, filters) {
        var sort = filters && filters.sort ? String(filters.sort) : "distance";
        var copy = list.slice();
        if (sort === "title") {
            copy.sort(function (a, b) {
                return String(a.title || "").localeCompare(String(b.title || ""), "nl");
            });
            return copy;
        }
        if (filters && filters.originLat != null && filters.originLng != null) {
            copy.sort(function (a, b) {
                var ma = travelMinutes(
                    { lat: filters.originLat, lng: filters.originLng },
                    a.lat,
                    a.lng,
                    filters.transport) || 9999;
                var mb = travelMinutes(
                    { lat: filters.originLat, lng: filters.originLng },
                    b.lat,
                    b.lng,
                    filters.transport) || 9999;
                if (ma !== mb) {
                    return ma - mb;
                }
                return String(a.title || "").localeCompare(String(b.title || ""), "nl");
            });
        }
        return copy;
    }

    function filterRows(all, filters, mapAllPins) {
        var out = [];
        for (var i = 0; i < all.length; i++) {
            var row = all[i];
            if (!matchesFilters(row, filters)) {
                continue;
            }
            if (!mapAllPins && !withinTravel(row, filters)) {
                continue;
            }
            out.push(row);
        }
        return sortRows(out, filters);
    }

    function toPin(row) {
        var pin = { id: row.id, lat: row.lat, lng: row.lng };
        if (row.colour) {
            pin.colour = row.colour;
        }
        if (row.publicMapRadiusKm > 0) {
            pin.publicMapRadiusKm = row.publicMapRadiusKm;
        }
        return pin;
    }

    function fetchCatalog(force) {
        var now = Date.now();
        if (!force && readCachedRaw() && now - lastLoadedAt < REVALIDATE_MS) {
            return Promise.resolve(expand(readCachedRaw()));
        }
        if (!readCachedRaw()) {
            return idbRead().then(function (rec) {
                if (rec && rec.body) {
                    memory = rec.body;
                    lastEtag = rec.etag || null;
                    lastLoadedAt = rec.at || 0;
                    try {
                        sessionStorage.setItem(STORAGE_BODY, rec.body);
                    } catch (eIdb) { }
                    if (!force && now - lastLoadedAt < REVALIDATE_MS) {
                        return expand(rec.body);
                    }
                }
                return fetchCatalogNetwork(force);
            });
        }
        return fetchCatalogNetwork(force);
    }

    function fetchCatalogNetwork(force) {
        if (networkInFlight) {
            return networkInFlight;
        }
        var headers = { Accept: "application/json" };
        if (lastEtag) {
            headers["If-None-Match"] = lastEtag;
        }
        networkInFlight = fetch(CATALOG_URL, { credentials: "same-origin", headers: headers })
            .then(function (res) {
                if (res.status === 304 && readCachedRaw()) {
                    lastLoadedAt = Date.now();
                    return expand(readCachedRaw());
                }
                if (!res.ok) {
                    if (readCachedRaw()) {
                        lastLoadedAt = Date.now();
                        return expand(readCachedRaw());
                    }
                    throw new Error("catalog " + res.status);
                }
                var etag = res.headers.get("ETag");
                return res.text().then(function (body) {
                    saveData(body, etag);
                    return expand(body);
                });
            })
            .finally(function () {
                networkInFlight = null;
            });
        return networkInFlight;
    }

    function readBootFilters() {
        var node = document.getElementById("jobsy-map-boot");
        if (!node || !node.textContent) {
            return null;
        }
        try {
            var parsed = JSON.parse(node.textContent);
            return parsed && parsed.mapInit && parsed.mapInit.filters ? parsed.mapInit.filters : null;
        } catch (e) {
            return null;
        }
    }

    function syncMapTravel(filters) {
        if (!filters || !window.jobMap) {
            return;
        }
        if (typeof window.jobMap.setTravelOptions === "function") {
            window.jobMap.setTravelOptions({
                maxMinutes: filters.maxMinutes,
                transport: filters.transport
            });
        }
    }

    function applyToMap(filters) {
        if (!rows) {
            return;
        }
        syncMapTravel(filters);
        var travelPins = filterRows(rows, filters, false);
        var mapPins = travelPins.length > 0 ? travelPins : filterRows(rows, filters, true);
        var dimmed = travelPins.length === 0 && mapPins.length > 0;
        var pins = mapPins.map(function (row) {
            var pin = toPin(row);
            if (dimmed) {
                pin.outsideTravel = true;
            }
            return pin;
        });
        var meta = {
            zoomOutsideTravel: dimmed && mapPins.length > 0,
            skipAutoFit: !!window.__jobsyBkRestoringView
        };
        if (window.jobMap && typeof window.jobMap.setVacanciesFromCatalog === "function") {
            window.jobMap.setVacanciesFromCatalog(pins, meta);
        } else {
            document.dispatchEvent(new CustomEvent("jobsy-bk-catalog-pins", {
                detail: {
                    pins: pins,
                    meta: meta
                }
            }));
        }
    }

    function readOriginFromUrl() {
        try {
            var params = new URLSearchParams(window.location.search);
            var lat = parseFloat(params.get("originLat"));
            var lng = parseFloat(params.get("originLng"));
            if (!isFinite(lat) || !isFinite(lng)) {
                return null;
            }
            return {
                originLat: lat,
                originLng: lng,
                originLabel: params.get("originLabel") || null
            };
        } catch (eOrigin) {
            return null;
        }
    }

    function filtersFromForm(form) {
        var origin = readOriginFromUrl() || {};
        var boot = readBootFilters() || {};
        var data = {};
        Array.prototype.forEach.call(form.elements, function (el) {
            if (!el.name || el.disabled) {
                return;
            }
            if (el.type === "checkbox" || el.type === "radio") {
                if (el.checked) {
                    if (!data[el.name]) {
                        data[el.name] = [];
                    }
                    data[el.name].push(el.value);
                }
                return;
            }
            if (el.type === "submit" || el.type === "button") {
                return;
            }
            data[el.name] = el.value;
        });
        var categoryIds = data.categoryId || [];
        if (!Array.isArray(categoryIds)) {
            categoryIds = [categoryIds];
        }
        var workTypes = data.workType || [];
        if (!Array.isArray(workTypes)) {
            workTypes = workTypes ? [workTypes] : [];
        }
        var keyword = readKeywordFromUi();
        if (keyword != null) {
            data.q = keyword;
        }
        var formLat = parseFloat(data.originLat);
        var formLng = parseFloat(data.originLng);
        var originLat = isFinite(formLat) ? formLat
            : (origin.originLat != null ? origin.originLat : boot.originLat);
        var originLng = isFinite(formLng) ? formLng
            : (origin.originLng != null ? origin.originLng : boot.originLng);
        var originLabel = (data.originLabel && String(data.originLabel).trim())
            || origin.originLabel
            || boot.originLabel
            || null;
        var sortSel = document.querySelector("[data-testid=kb-list-sort]");
        var sortVal = (sortSel && sortSel.value) || data.sort || boot.sort || null;
        return {
            q: data.q || null,
            maxMinutes: parseInt(data.maxMinutes, 10) || boot.maxMinutes || 20,
            transport: data.transport || boot.transport || "Fiets",
            minHours: parseInt(data.minHours, 10) || 0,
            maxHours: parseInt(data.maxHours, 10) || 40,
            minWageCents: data.minWage != null && data.minWage !== "" ? Math.round(parseFloat(data.minWage) * 100) : null,
            maxWageCents: data.maxWage != null && data.maxWage !== "" ? Math.round(parseFloat(data.maxWage) * 100) : null,
            categoryIds: categoryIds,
            workTypes: workTypes,
            originLat: originLat,
            originLng: originLng,
            originLabel: originLabel,
            sort: sortVal
        };
    }

    function renderMapTopChrome(filters, travelRows) {
        var host = document.querySelector("[data-testid=kb-map-top-chrome]");
        if (!host || !travelRows || !travelRows.length) {
            if (host) {
                host.innerHTML = "";
            }
            return;
        }
        var seed = readHighlightSeed();
        var featured = travelRows.slice().sort(function (a, b) {
            return highlightRank(a.id, seed) - highlightRank(b.id, seed);
        }).slice(0, 12);
        var cards = featured.map(function (row) {
            var mins = travelMinutes(
                filters.originLat != null ? { lat: filters.originLat, lng: filters.originLng } : null,
                row.lat,
                row.lng,
                filters.transport);
            var travel = mins != null
                ? "± " + mins + " min · " + String(filters.transport || "Fiets").toLowerCase()
                : "";
            var img = row.imageUrl
                ? "<img class=\"highlight-carousel__media vacancy-photo\" src=\"" + escapeHtml(row.imageUrl)
                + "\" alt=\"\" width=\"160\" height=\"107\" loading=\"lazy\" decoding=\"async\" />"
                : "";
            return "<button type=\"button\" class=\"highlight-carousel__card\" role=\"listitem\" data-vacancy-id=\""
                + escapeHtml(row.id) + "\">" + img
                + "<span class=\"highlight-carousel__body\"><span class=\"highlight-carousel__title-text\">"
                + escapeHtml(row.title) + "</span><span class=\"highlight-carousel__meta\">"
                + escapeHtml(row.company) + "</span>"
                + (travel ? "<span class=\"highlight-carousel__travel\">" + escapeHtml(travel) + "</span>" : "")
                + "</span></button>";
        }).join("");
        var legendMap = {};
        travelRows.forEach(function (row) {
            var name = row.categoryName || "";
            if (!name || legendMap[name]) {
                return;
            }
            legendMap[name] = row.colour || "#0f2d5c";
        });
        var legendNames = Object.keys(legendMap);
        var legendHtml = "";
        if (legendNames.length) {
            var dots = legendNames.slice(0, 4).map(function (name) {
                return "<i style=\"background:" + escapeHtml(legendMap[name]) + "\"></i>";
            }).join("");
            var items = legendNames.map(function (name) {
                return "<li class=\"map-legend__item\"><span class=\"map-legend__swatch\" style=\"background:"
                    + escapeHtml(legendMap[name]) + "\" aria-hidden=\"true\"></span><span>"
                    + escapeHtml(name) + "</span></li>";
            }).join("");
            legendHtml = "<div class=\"map-legend\" data-testid=\"kb-map-legend\">"
                + "<button type=\"button\" class=\"map-legend__toggle\" aria-expanded=\"false\" aria-controls=\"map-legend-panel-ssr\">"
                + "<span class=\"map-legend__toggle-dots\" aria-hidden=\"true\">" + dots + "</span>"
                + "<span class=\"visually-hidden\">Legenda</span></button>"
                + "<aside id=\"map-legend-panel-ssr\" class=\"map-legend__panel\" hidden aria-hidden=\"true\">"
                + "<ul class=\"map-legend__list\">" + items + "</ul></aside></div>";
        }
        host.innerHTML = "<aside class=\"highlight-carousel highlight-carousel--map\" aria-label=\"Uitgelicht\">"
            + "<div class=\"highlight-carousel__track\" role=\"list\">" + cards + "</div></aside>" + legendHtml;
        host.querySelectorAll(".highlight-carousel__card").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var id = btn.getAttribute("data-vacancy-id");
                if (id && window.jobMap) {
                    if (typeof window.jobMap.focus === "function") {
                        window.jobMap.focus(id);
                    } else if (typeof window.jobMap.highlight === "function") {
                        window.jobMap.highlight(id);
                    }
                }
            });
        });
        var legend = host.querySelector(".map-legend");
        if (legend) {
            var toggle = legend.querySelector(".map-legend__toggle");
            var panel = legend.querySelector(".map-legend__panel");
            if (toggle && panel) {
                toggle.addEventListener("click", function (ev) {
                    ev.stopPropagation();
                    var open = legend.classList.toggle("is-open");
                    toggle.setAttribute("aria-expanded", open ? "true" : "false");
                    panel.hidden = !open;
                    panel.setAttribute("aria-hidden", open ? "false" : "true");
                });
            }
        }
        if (window.jobMap && typeof window.jobMap.invalidate === "function") {
            window.jobMap.invalidate();
        }
    }

    function readKeywordFromUi() {
        var inputs = document.querySelectorAll(
            "[data-banenkaart-ssr=\"1\"] input[name=\"q\"][form=\"kb-filter-form\"], #kb-filter-form input[name=\"q\"]"
        );
        if (!inputs.length) {
            return null;
        }
        var best = "";
        for (var i = 0; i < inputs.length; i++) {
            var el = inputs[i];
            if (el.offsetParent === null && el.type !== "hidden") {
                continue;
            }
            var val = String(el.value || "");
            if (val.length >= best.length) {
                best = val;
            }
        }
        return best.trim() || null;
    }

    function vacancyHref(id, filters) {
        var href = "/vacancies/" + encodeURIComponent(id);
        if (!filters) {
            return href;
        }
        var q = [];
        if (filters.transport) {
            q.push("transport=" + encodeURIComponent(filters.transport));
        }
        if (filters.maxMinutes) {
            q.push("maxMinutes=" + filters.maxMinutes);
        }
        return q.length ? href + "?" + q.join("&") : href;
    }

    function travelSubtitle(filters, count) {
        var mins = filters && filters.maxMinutes ? filters.maxMinutes : 20;
        var mode = filters && filters.transport ? filters.transport : "Fiets";
        return count + " banen binnen " + mins + " min " + mode.toLowerCase();
    }

    function updateResultChrome(filters, travelCount) {
        var sub = travelSubtitle(filters, travelCount);
        document.querySelectorAll(
            "[data-testid=kb-desktop-result-count], [data-testid=kb-mobile-result-count], .results-meta__count"
        ).forEach(function (el) {
            el.textContent = travelCount + " banen";
        });
        document.querySelectorAll(".kb-nearby-banner__sub, .kb-filter-bar__count-sub").forEach(function (el) {
            el.textContent = sub;
        });
        var loadMore = document.querySelector("[data-testid=kb-load-more]");
        if (loadMore) {
            loadMore.style.display = travelCount > listShown ? "" : "none";
        }
    }

    function buildBanenkaartUrl(filters) {
        var q = [];
        if (filters.q) {
            q.push("q=" + encodeURIComponent(filters.q));
        }
        if (filters.maxMinutes && filters.maxMinutes !== 20) {
            q.push("maxMinutes=" + filters.maxMinutes);
        }
        if (filters.transport && filters.transport !== "Fiets") {
            q.push("transport=" + encodeURIComponent(filters.transport));
        }
        if (filters.minHours) {
            q.push("minHours=" + filters.minHours);
        }
        if (filters.maxHours && filters.maxHours !== 40) {
            q.push("maxHours=" + filters.maxHours);
        }
        if (filters.categoryIds && filters.categoryIds.length) {
            filters.categoryIds.forEach(function (id) {
                q.push("categoryId=" + encodeURIComponent(id));
            });
        }
        if (filters.workTypes && filters.workTypes.length) {
            filters.workTypes.forEach(function (wt) {
                q.push("workType=" + encodeURIComponent(wt));
            });
        }
        if (filters.originLat != null && filters.originLng != null) {
            q.push("originLat=" + filters.originLat);
            q.push("originLng=" + filters.originLng);
            if (filters.originLabel) {
                q.push("originLabel=" + encodeURIComponent(filters.originLabel));
            }
        }
        return q.length ? "/banenkaart?" + q.join("&") : "/banenkaart";
    }

    function escapeHtml(text) {
        return String(text || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function renderListRows(listRows, filters, resetPage) {
        if (resetPage !== false) {
            listShown = LIST_PAGE;
        }
        currentListFilters = filters;
        var travelRows = filterRows(listRows, filters, false);
        var slice = travelRows.slice(0, listShown);
        var listRoot = document.querySelector("[data-banenkaart-ssr=\"1\"] [data-testid=vacancy-list]");
        if (!listRoot) {
            return travelRows.length;
        }
        listRoot.querySelectorAll("[data-testid=kb-empty-travel], .state-message").forEach(function (el) {
            el.remove();
        });
        listRoot.querySelectorAll("[data-testid=kb-list-rows]").forEach(function (el) {
            el.remove();
        });
        if (travelRows.length === 0) {
            listRoot.insertAdjacentHTML(
                "afterbegin",
                "<p class=\"state-message\" data-testid=\"kb-empty-travel\">Geen vacatures binnen je reistijd</p>"
                + "<button type=\"button\" class=\"filter-sheet__cancel kb-widen-filters-btn\" data-testid=\"kb-widen-filters\">Filters verruimen</button>"
            );
            updateResultChrome(filters, 0);
            return 0;
        }
        var html = "<div class=\"kb-list-rows\" data-testid=\"kb-list-rows\">";
        slice.forEach(function (row, index) {
            var href = vacancyHref(row.id, filters);
            var mins = travelMinutes(
                filters.originLat != null ? { lat: filters.originLat, lng: filters.originLng } : null,
                row.lat,
                row.lng,
                filters.transport);
            var wage = row.wageCents != null && row.wageCents > 0
                ? "€ " + (row.wageCents / 100).toFixed(2).replace(".", ",") + "/u"
                : "";
            var travel = mins != null
                ? "± " + mins + " min · " + String(filters.transport || "Fiets").toLowerCase()
                : "";
            html += "<article class=\"kb-list-row kb-list-row--compact kb-list-row--card\" data-vacancy-id=\""
                + escapeHtml(row.id)
                + "\" data-testid=\"kb-list-row\">";
            html += "<a class=\"kb-list-row__media\" href=\"" + escapeHtml(href) + "\">";
            if (row.imageUrl) {
                html += "<img class=\"kb-list-row__photo\" src=\"" + escapeHtml(row.imageUrl)
                    + "\" alt=\"\" width=\"400\" height=\"250\" loading=\"" + (index === 0 ? "eager" : "lazy")
                    + "\" decoding=\"async\" />";
            }
            if (row.categoryName) {
                html += "<span class=\"kb-list-row__sector\">" + escapeHtml(row.categoryName) + "</span>";
            }
            html += "<div class=\"kb-list-row__overlay\"><h2 class=\"kb-list-row__title\">"
                + escapeHtml(row.title) + "</h2><p class=\"kb-list-row__overlay-meta\">";
            if (travel) {
                html += "<span class=\"kb-list-row__travel\">" + escapeHtml(travel) + "</span>";
            }
            if (wage) {
                html += "<span class=\"kb-list-row__wage\">" + escapeHtml(wage) + "</span>";
            }
            html += "</p></div></a><div class=\"kb-list-row__footer\">";
            html += "<p class=\"kb-list-row__company\">" + escapeHtml(row.company) + "</p>";
            if (row.place) {
                html += "<p class=\"kb-list-row__place\">" + escapeHtml(row.place) + "</p>";
            }
            html += "</div></article>";
        });
        html += "</div>";
        listRoot.insertAdjacentHTML("afterbegin", html);
        updateResultChrome(filters, travelRows.length);
        renderMapTopChrome(filters, travelRows);
        return travelRows.length;
    }

    function loadMoreListRows() {
        if (!rows || !currentListFilters) {
            return;
        }
        listShown += LIST_PAGE;
        renderListRows(rows, currentListFilters, false);
    }

    function patchBootFilters(filters) {
        var node = document.getElementById("jobsy-map-boot");
        if (!node || !node.textContent) {
            return;
        }
        try {
            var parsed = JSON.parse(node.textContent);
            if (parsed && parsed.mapInit) {
                parsed.mapInit.filters = Object.assign({}, parsed.mapInit.filters || {}, filters);
                node.textContent = JSON.stringify(parsed);
            }
        } catch (eBoot) { }
    }

    function applyClientFilters(filters) {
        if (!rows) {
            return fetchCatalog(false).then(function () {
                applyClientFilters(filters);
            });
        }
        patchBootFilters(filters);
        applyToMap(filters);
        renderListRows(rows, filters);
        try {
            history.replaceState(null, "", buildBanenkaartUrl(filters));
        } catch (eUrl) { }
        document.dispatchEvent(new CustomEvent("jobsy-bk-filters-applied", { detail: { count: filterRows(rows, filters, false).length } }));
    }

    function bootAndApply() {
        var filters = readBootFilters() || {};
        return fetchCatalog(false).then(function () {
            applyToMap(filters);
            if (rows) {
                renderListRows(rows, filters);
            }
            document.dispatchEvent(new CustomEvent("jobsy-bk-catalog-ready", { detail: { count: rows ? rows.length : 0 } }));
            if (!window.jobMap || typeof window.jobMap.setVacanciesFromCatalog !== "function") {
                setTimeout(function () { applyToMap(filters); }, 400);
                setTimeout(function () { applyToMap(filters); }, 1500);
            }
        }).catch(function () { });
    }

    window.jobsyBanenkaartCatalog = {
        url: CATALOG_URL,
        load: fetchCatalog,
        revalidate: function () { return fetchCatalog(true); },
        isReady: function () { return !!rows; },
        getRows: function () { return rows; },
        filterRows: filterRows,
        toPin: toPin,
        travelMinutes: travelMinutes,
        applyToMap: applyToMap,
        filtersFromForm: filtersFromForm,
        buildBanenkaartUrl: buildBanenkaartUrl,
        applyClientFilters: applyClientFilters,
        renderListRows: renderListRows,
        loadMoreListRows: loadMoreListRows,
        readKeywordFromUi: readKeywordFromUi,
        prefetch: function () {
            return fetchCatalog(false);
        },
        ingestRaw: ingestRaw
    };

    if (document.querySelector("[data-banenkaart-ssr=\"1\"]")) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", bootAndApply);
        } else {
            bootAndApply();
        }
    }
})();
