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

    function toPin(row, filters) {
        filters = filters || currentListFilters;
        var pin = {
            id: row.id,
            lat: row.lat,
            lng: row.lng,
            title: row.title,
            company: row.company,
            offeredBy: row.offeredBy || "",
            place: row.place || "",
            categoryName: row.categoryName || "",
            workTypes: row.workTypes
        };
        if (row.colour) {
            pin.colour = row.colour;
            pin.categoryColor = row.colour;
        }
        if (row.publicMapRadiusKm > 0) {
            pin.publicMapRadiusKm = row.publicMapRadiusKm;
        }
        if (filters && filters.originLat != null && filters.originLng != null) {
            var mins = travelMinutes(
                { lat: filters.originLat, lng: filters.originLng },
                row.lat,
                row.lng,
                filters.transport);
            if (mins != null) {
                pin.travelMinutes = mins;
            }
            pin.transport = filters.transport;
            pin.transportLabel = transportVerbNl(filters.transport);
        }
        if (row.offeredBy && String(row.offeredBy).trim().toLowerCase()
            !== String(row.company || "").trim().toLowerCase()) {
            pin.isIntermediary = true;
            pin.pinKind = "intermediary";
        }
        if (row.minHours != null) {
            pin.minHoursPerWeek = row.minHours;
        }
        if (row.maxHours != null) {
            pin.maxHoursPerWeek = row.maxHours;
        }
        if (row.workTypes && row.workTypes.length) {
            pin.workType = row.workTypes[0];
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
                    return idbRead().then(function (rec) {
                        if (rec && rec.body) {
                            saveData(rec.body, rec.etag || null);
                            return expand(rec.body);
                        }
                        throw new Error("catalog " + res.status);
                    });
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

    function normalizeFilters(raw) {
        raw = raw || {};
        var categoryIds = raw.categoryIds || [];
        if (!Array.isArray(categoryIds)) {
            categoryIds = categoryIds ? [categoryIds] : [];
        }
        var workTypes = raw.workTypes || [];
        if (!Array.isArray(workTypes)) {
            workTypes = workTypes ? [workTypes] : [];
        }
        var maxMinutes = parseInt(raw.maxMinutes, 10);
        return {
            q: raw.q || null,
            maxMinutes: isFinite(maxMinutes) ? maxMinutes : 20,
            transport: raw.transport || "Fiets",
            minHours: raw.minHours != null ? parseInt(raw.minHours, 10) || 0 : 0,
            maxHours: raw.maxHours != null ? parseInt(raw.maxHours, 10) || 40 : 40,
            minWageCents: raw.minWageCents != null ? raw.minWageCents : null,
            maxWageCents: raw.maxWageCents != null ? raw.maxWageCents : null,
            categoryIds: categoryIds,
            workTypes: workTypes,
            originLat: raw.originLat != null ? Number(raw.originLat) : null,
            originLng: raw.originLng != null ? Number(raw.originLng) : null,
            originLabel: raw.originLabel || null,
            sort: raw.sort || "distance"
        };
    }

    function readFiltersFromUrl() {
        try {
            var params = new URLSearchParams(window.location.search);
            var lat = parseFloat(params.get("originLat"));
            var lng = parseFloat(params.get("originLng"));
            var maxMinutes = parseInt(params.get("maxMinutes") || "", 10);
            var minHours = parseInt(params.get("minHours") || "", 10);
            var maxHours = parseInt(params.get("maxHours") || "", 10);
            var categoryIds = params.getAll("categoryId");
            var workTypes = params.getAll("workType");
            return normalizeFilters({
                q: params.get("q") || null,
                maxMinutes: isFinite(maxMinutes) ? maxMinutes : 20,
                transport: params.get("transport") || "Fiets",
                minHours: isFinite(minHours) ? minHours : 0,
                maxHours: isFinite(maxHours) ? maxHours : 40,
                categoryIds: categoryIds,
                workTypes: workTypes,
                originLat: isFinite(lat) ? lat : null,
                originLng: isFinite(lng) ? lng : null,
                originLabel: params.get("originLabel") || null,
                sort: params.get("sort") || "distance"
            });
        } catch (eUrl) {
            return normalizeFilters({});
        }
    }

    function urlHasFilterParams() {
        try {
            var params = new URLSearchParams(window.location.search);
            return params.has("originLat") || params.has("maxMinutes") || params.has("transport")
                || params.has("minHours") || params.has("maxHours") || params.has("q")
                || params.has("categoryId") || params.has("workType") || params.has("sort");
        } catch (eParams) {
            return false;
        }
    }

    function resolveFilters() {
        if (urlHasFilterParams()) {
            return readFiltersFromUrl();
        }
        var boot = readBootFilters();
        if (boot) {
            return normalizeFilters(boot);
        }
        return readFiltersFromUrl();
    }

    function defaultFilters() {
        return normalizeFilters({
            q: null,
            maxMinutes: 20,
            transport: "Fiets",
            minHours: 0,
            maxHours: 40,
            categoryIds: [],
            workTypes: [],
            originLat: null,
            originLng: null,
            originLabel: null,
            sort: "distance"
        });
    }

    function travelChipLabel(filters) {
        var mins = filters.maxMinutes || 20;
        var mode = filters.transport || "Fiets";
        if (mode === "Fiets") {
            return mins + " min fietsen";
        }
        if (mode === "Auto") {
            return mins + " min rijden";
        }
        if (mode === "OV") {
            return mins + " min ov";
        }
        if (mode === "Lopend") {
            return mins + " min lopen";
        }
        return mins + " min " + transportVerbNl(mode);
    }

    function syncFilterFormToState(filters) {
        var form = document.getElementById("kb-filter-form");
        if (!form) {
            return;
        }
        var slider = form.querySelector("[data-testid=kb-sheet-travel-slider]");
        if (slider) {
            slider.value = String(filters.maxMinutes || 20);
        }
        var transportHidden = document.getElementById("kb-filter-transport");
        if (transportHidden) {
            transportHidden.value = filters.transport || "Fiets";
        }
        document.querySelectorAll("[data-kb-transport]").forEach(function (btn) {
            btn.classList.toggle("is-selected", btn.getAttribute("data-kb-transport") === (filters.transport || "Fiets"));
        });
        document.querySelectorAll("[data-kb-travel-preset]").forEach(function (btn) {
            var preset = parseInt(btn.getAttribute("data-kb-travel-preset"), 10);
            btn.classList.toggle("is-selected", preset === (filters.maxMinutes || 20));
        });
        var minHours = form.querySelector("input[name=minHours]");
        var maxHours = form.querySelector("input[name=maxHours]");
        if (minHours) {
            minHours.value = String(filters.minHours != null ? filters.minHours : 0);
        }
        if (maxHours) {
            maxHours.value = String(filters.maxHours != null ? filters.maxHours : 40);
        }
        form.querySelectorAll("input[name=categoryId]").forEach(function (input) {
            var id = input.value;
            var on = false;
            for (var i = 0; i < filters.categoryIds.length; i++) {
                if (filters.categoryIds[i] === id) {
                    on = true;
                    break;
                }
            }
            input.checked = on;
        });
        form.querySelectorAll("input[name=workType]").forEach(function (input) {
            var on = false;
            for (var w = 0; w < filters.workTypes.length; w++) {
                if (String(filters.workTypes[w]).toLowerCase() === String(input.value).toLowerCase()) {
                    on = true;
                    break;
                }
            }
            input.checked = on;
        });
        var latEl = document.getElementById("kb-filter-origin-lat");
        var lngEl = document.getElementById("kb-filter-origin-lng");
        var labelEl = document.getElementById("kb-filter-origin-label");
        if (latEl) {
            latEl.value = filters.originLat != null ? String(filters.originLat) : "";
        }
        if (lngEl) {
            lngEl.value = filters.originLng != null ? String(filters.originLng) : "";
        }
        if (labelEl) {
            labelEl.value = filters.originLabel || "";
        }
        document.querySelectorAll("[data-testid=kb-list-sort]").forEach(function (sel) {
            sel.value = filters.sort || "distance";
        });
        document.querySelectorAll("[data-banenkaart-ssr=\"1\"] input[name=\"q\"][form=\"kb-filter-form\"]").forEach(function (input) {
            input.value = filters.q || "";
        });
    }

    function updateActiveFiltersUi(filters) {
        var travelBtn = document.querySelector("[data-testid=kb-active-tag-travel]");
        var travelTag = travelBtn && travelBtn.querySelector("span:first-child");
        var hasOrigin = filters.originLat != null && filters.originLng != null;
        if (travelBtn) {
            travelBtn.hidden = !hasOrigin;
            travelBtn.style.display = hasOrigin ? "" : "none";
        }
        if (travelTag && hasOrigin) {
            travelTag.textContent = travelChipLabel(filters);
        }
        var bar = document.querySelector("[data-testid=kb-active-filters]");
        if (bar) {
            var anyVisible = false;
            bar.querySelectorAll(".kb-active-tag").forEach(function (tag) {
                if (tag.hidden || tag.style.display === "none") {
                    return;
                }
                anyVisible = true;
            });
            bar.hidden = !anyVisible;
            bar.style.display = anyVisible ? "" : "none";
        }
    }

    function syncDiscoveryChrome(filters, travelCount) {
        var root = document.querySelector("[data-banenkaart-ssr=\"1\"]");
        if (root) {
            root.classList.remove("kb-ssr-travel-empty");
        }
        updateActiveFiltersUi(filters);
        updateResultChrome(filters, travelCount);
    }

    function persistFilterCookieQuietly(filters) {
        var form = document.getElementById("kb-filter-form");
        if (!form || typeof fetch !== "function") {
            return;
        }
        syncFilterFormToState(filters);
        try {
            var fd = new FormData(form);
            fetch(form.action, {
                method: "POST",
                body: fd,
                credentials: "same-origin",
                redirect: "manual"
            }).catch(function () { });
        } catch (ePost) { }
    }

    function clearFilterCookieQuietly() {
        var resetForm = document.querySelector("form[action*=\"reset-filters\"]");
        if (!resetForm || typeof fetch !== "function") {
            return;
        }
        try {
            var fd = new FormData(resetForm);
            fetch(resetForm.action, {
                method: "POST",
                body: fd,
                credentials: "same-origin",
                redirect: "manual"
            }).catch(function () { });
        } catch (eReset) { }
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

    function readBootZoomOutsideTravel() {
        var node = document.getElementById("jobsy-map-boot");
        if (!node || !node.textContent) {
            return false;
        }
        try {
            var parsed = JSON.parse(node.textContent);
            return parsed && parsed.zoomToPinsOutsideTravel === true;
        } catch (eBoot) {
            return false;
        }
    }

    function applyToMap(filters) {
        if (!rows) {
            return;
        }
        syncMapTravel(filters);
        var travelPins = filterRows(rows, filters, false);
        var allowOutside = readBootZoomOutsideTravel();
        var mapPins = travelPins.length > 0
            ? travelPins
            : (allowOutside ? filterRows(rows, filters, true) : []);
        var dimmed = travelPins.length === 0 && mapPins.length > 0;
        currentListFilters = filters;
        var pins = mapPins.map(function (row) {
            var pin = toPin(row, filters);
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

    function mapIcons() {
        return window.jobsyBanenkaartMapIcons || null;
    }

    function transportVerbNl(mode) {
        var t = String(mode || "Fiets");
        if (t === "Fiets") {
            return "fietsen";
        }
        if (t === "Auto") {
            return "auto";
        }
        if (t === "OV") {
            return "ov";
        }
        if (t === "Lopend") {
            return "lopen";
        }
        return t.toLowerCase();
    }

    function hoursLabel(row) {
        var minH = row.minHours != null ? row.minHours : 0;
        var maxH = row.maxHours != null ? row.maxHours : 40;
        if (minH > 0 && maxH > 0 && minH !== maxH) {
            return minH + " tot " + maxH + " uur";
        }
        if (maxH > 0) {
            return maxH + " uur";
        }
        return "";
    }

    function renderMapTopChrome(filters, travelRows) {
        var host = document.querySelector("[data-testid=kb-map-top-chrome]");
        if (!host) {
            return;
        }
        if (!travelRows || !travelRows.length) {
            host.innerHTML = "<div class=\"strip strip--empty\" role=\"status\"><p>Geen vacatures in beeld — pas filters aan of verruim je reistijd.</p></div>";
            return;
        }
        var ic = mapIcons();
        var seed = readHighlightSeed();
        var featured = travelRows.slice().sort(function (a, b) {
            return highlightRank(a.id, seed) - highlightRank(b.id, seed);
        }).slice(0, 12);
        var cards = featured.map(function (row) {
            var wtKey = ic ? ic.workTypeKey(row) : "box";
            var colors = ic ? ic.workTypeColors(wtKey) : { bg: "#F3EADB", fg: "#7E5E2C" };
            var ico = ic ? ic.workTypeIconSvg(row, false) : "";
            var mini = ic && ic.isIntermediaryRow(row)
                ? ic.pinI(0.5, false, false)
                : (ic ? ic.pinW(0.5) : "");
            return "<div class=\"sc\" role=\"button\" tabindex=\"0\" data-vacancy-id=\""
                + escapeHtml(row.id) + "\"><span class=\"ico\" style=\"background:"
                + escapeHtml(colors.bg) + ";color:" + escapeHtml(colors.fg) + "\">" + ico
                + "</span><div><b>" + escapeHtml(row.title) + "</b><small>" + mini
                + escapeHtml(row.company) + "</small></div></div>";
        }).join("");
        host.innerHTML = "<div class=\"strip\" role=\"list\">" + cards + "</div>";
        host.querySelectorAll(".sc").forEach(function (btn) {
            function activate() {
                var id = btn.getAttribute("data-vacancy-id");
                if (id && window.jobMap) {
                    if (typeof window.jobMap.focus === "function") {
                        window.jobMap.focus(id);
                    } else if (typeof window.jobMap.highlight === "function") {
                        window.jobMap.highlight(id);
                    }
                }
            }
            btn.addEventListener("click", activate);
            btn.addEventListener("keydown", function (ev) {
                if (ev.key === "Enter" || ev.key === " ") {
                    ev.preventDefault();
                    activate();
                }
            });
        });
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

    function travelSubtitle(filters) {
        var mins = filters && filters.maxMinutes ? filters.maxMinutes : 20;
        var mode = filters && filters.transport ? filters.transport : "Fiets";
        return "binnen " + mins + " min " + transportVerbNl(mode);
    }

    function updateResultChrome(filters, travelCount) {
        var sub = travelSubtitle(filters);
        document.querySelectorAll(
            "[data-testid=kb-desktop-result-count], [data-testid=kb-mobile-result-count], .results-meta__count"
        ).forEach(function (el) {
            el.textContent = travelCount + " vacatures";
        });
        document.querySelectorAll(".results-meta__sub, .kb-filter-bar__count-sub").forEach(function (el) {
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
        var hasOrigin = filters.originLat != null && filters.originLng != null;
        if (hasOrigin || (filters.maxMinutes && filters.maxMinutes !== 20)) {
            q.push("maxMinutes=" + (filters.maxMinutes || 20));
        }
        if (hasOrigin || (filters.transport && filters.transport !== "Fiets")) {
            q.push("transport=" + encodeURIComponent(filters.transport || "Fiets"));
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

    function listRowHasPhoto(row) {
        return !!(row && row.imageUrl && String(row.imageUrl).trim());
    }

    function listMapPopupPhotoUrl(id) {
        if (id == null || id === "") {
            return "";
        }
        return "/api/vacancies/" + encodeURIComponent(String(id)) + "/map-popup-photo?variant=mobile";
    }

    var listPhotoObserver = null;

    function observeLazyListPhotos(root) {
        if (!root || typeof IntersectionObserver !== "function") {
            return;
        }
        var pending = root.querySelectorAll("img[data-kb-list-photo][data-photo-src]:not([data-photo-loaded])");
        if (!pending.length) {
            return;
        }
        if (!listPhotoObserver) {
            listPhotoObserver = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (!entry.isIntersecting) {
                        return;
                    }
                    var img = entry.target;
                    if (!img || img.getAttribute("data-photo-loaded") === "1") {
                        return;
                    }
                    var src = img.getAttribute("data-photo-src");
                    if (!src) {
                        return;
                    }
                    img.setAttribute("data-photo-loaded", "1");
                    img.src = src;
                    listPhotoObserver.unobserve(img);
                });
            }, { root: null, rootMargin: "120px 0px", threshold: 0.01 });
        }
        pending.forEach(function (img) {
            listPhotoObserver.observe(img);
        });
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
        listRoot.querySelectorAll(
            "[data-testid=kb-empty-travel], [data-testid=kb-widen-filters], [data-testid=kb-list-rows], .state-message"
        ).forEach(function (el) {
            el.remove();
        });
        if (travelRows.length === 0) {
            listRoot.insertAdjacentHTML(
                "afterbegin",
                "<p class=\"state-message\" data-testid=\"kb-empty-travel\">Geen vacatures binnen je reistijd</p>"
                + "<button type=\"button\" class=\"filter-sheet__cancel kb-widen-filters-btn\" data-testid=\"kb-widen-filters\">Filters verruimen</button>"
            );
            syncDiscoveryChrome(filters, 0);
            renderMapTopChrome(filters, []);
            return 0;
        }
        var ic = mapIcons();
        var bike = ic && ic.bikeSvg ? ic.bikeSvg.replace("class=\"ic\"", "class=\"ic s\"") : "";
        var html = "<div class=\"kb-list-rows\" data-testid=\"kb-list-rows\">";
        slice.forEach(function (row) {
            var href = vacancyHref(row.id, filters);
            var mins = travelMinutes(
                filters.originLat != null ? { lat: filters.originLat, lng: filters.originLng } : null,
                row.lat,
                row.lng,
                filters.transport);
            var travel = mins != null
                ? mins + " min " + transportVerbNl(filters.transport)
                : "";
            var wtKey = ic ? ic.workTypeKey(row) : "box";
            var colors = ic ? ic.workTypeColors(wtKey) : { bg: "#E3EEDD", fg: "#3F6E31" };
            var ico = ic ? ic.workTypeIconSvg(row, true) : "";
            var typeHtml = ic ? ic.typeMarkerHtml(row, false) : "";
            var sector = row.categoryName ? escapeHtml(row.categoryName) : "";
            var hours = hoursLabel(row);
            var photo = listRowHasPhoto(row);
            html += "<a class=\"vc\" href=\"" + escapeHtml(href) + "\" data-vacancy-id=\""
                + escapeHtml(row.id) + "\" data-testid=\"kb-list-row\">";
            if (photo) {
                html += "<div class=\"vimg vimg--photo\" style=\"background:linear-gradient(135deg,"
                    + escapeHtml(colors.bg) + ",#fff 150%);\">";
                html += "<img class=\"kb-list-lazy-photo\" alt=\"\" width=\"400\" height=\"250\" decoding=\"async\" "
                    + "data-kb-list-photo=\"1\" data-photo-src=\""
                    + escapeHtml(listMapPopupPhotoUrl(row.id)) + "\" />";
            } else {
                html += "<div class=\"vimg\" style=\"background:linear-gradient(135deg,"
                    + escapeHtml(colors.bg) + ",#fff 150%);color:" + escapeHtml(colors.fg) + "\">"
                    + ico;
            }
            if (sector) {
                html += "<span class=\"vtag\">" + sector + "</span>";
            }
            html += "</div><div class=\"vb\"><b>" + escapeHtml(row.title) + "</b>";
            html += "<div class=\"who\">" + typeHtml + "</div>";
            html += "<div class=\"who\" style=\"margin-top:4px\">" + escapeHtml(row.company) + "</div>";
            html += "<div class=\"meta\">";
            if (travel) {
                html += "<span>" + bike + escapeHtml(travel) + "</span>";
            }
            if (hours) {
                html += "<span>" + escapeHtml(hours) + "</span>";
            }
            html += "</div></div></a>";
        });
        html += "</div>";
        listRoot.insertAdjacentHTML("afterbegin", html);
        observeLazyListPhotos(listRoot);
        syncDiscoveryChrome(filters, travelRows.length);
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
        filters = normalizeFilters(filters);
        if (!rows) {
            return fetchCatalog(false).then(function () {
                applyClientFilters(filters);
            });
        }
        patchBootFilters(filters);
        syncFilterFormToState(filters);
        applyToMap(filters);
        renderListRows(rows, filters);
        try {
            history.replaceState(null, "", buildBanenkaartUrl(filters));
        } catch (eUrl) { }
        persistFilterCookieQuietly(filters);
        document.dispatchEvent(new CustomEvent("jobsy-bk-filters-applied", {
            detail: { count: filterRows(rows, filters, false).length, filters: filters }
        }));
    }

    function resetClientFilters() {
        var defaults = defaultFilters();
        syncFilterFormToState(defaults);
        if (window.jobMap && typeof window.jobMap.clearOrigin === "function") {
            window.jobMap.clearOrigin();
        }
        clearFilterCookieQuietly();
        applyClientFilters(defaults);
    }

    function bootAndApply() {
        var listRoot = document.querySelector("[data-banenkaart-ssr=\"1\"] [data-testid=vacancy-list]");
        if (listRoot) {
            observeLazyListPhotos(listRoot);
        }
        var filters = resolveFilters();
        return fetchCatalog(false).then(function () {
            patchBootFilters(filters);
            syncFilterFormToState(filters);
            applyToMap(filters);
            if (rows) {
                renderListRows(rows, filters);
            }
            document.dispatchEvent(new CustomEvent("jobsy-bk-catalog-ready", { detail: { count: rows ? rows.length : 0 } }));
            if (!window.jobMap || typeof window.jobMap.setVacanciesFromCatalog !== "function") {
                setTimeout(function () { applyToMap(resolveFilters()); }, 400);
                setTimeout(function () { applyToMap(resolveFilters()); }, 1500);
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
        resetClientFilters: resetClientFilters,
        resolveFilters: resolveFilters,
        readFiltersFromUrl: readFiltersFromUrl,
        renderListRows: renderListRows,
        observeLazyListPhotos: observeLazyListPhotos,
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
