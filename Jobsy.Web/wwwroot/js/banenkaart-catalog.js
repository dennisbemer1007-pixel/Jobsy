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
                imageUrl: data.iu[i]
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
            return 40 / 1.35;
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
            /* work type labels matched loosely via bitflags omitted — server SSR already filtered list */
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
        return out;
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
        var now = Date.now();
        var headers = { Accept: "application/json" };
        if (lastEtag) {
            headers["If-None-Match"] = lastEtag;
        }
        return fetch(CATALOG_URL, { credentials: "same-origin", headers: headers })
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
            });
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

    function applyToMap(filters) {
        if (!rows) {
            return;
        }
        var mapPins = filterRows(rows, filters, true);
        var pins = mapPins.map(toPin);
        if (window.jobMap && typeof window.jobMap.setVacanciesFromCatalog === "function") {
            window.jobMap.setVacanciesFromCatalog(pins, {
                zoomOutsideTravel: mapPins.length > 0 && !filterRows(rows, filters, false).length
            });
        } else {
            var travelPins = filterRows(rows, filters, false);
            document.dispatchEvent(new CustomEvent("jobsy-bk-catalog-pins", {
                detail: {
                    pins: pins,
                    meta: {
                        zoomOutsideTravel: travelPins.length === 0 && mapPins.length > 0
                    }
                }
            }));
        }
    }

    function bootAndApply() {
        var filters = readBootFilters();
        return fetchCatalog(false).then(function () {
            applyToMap(filters);
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
