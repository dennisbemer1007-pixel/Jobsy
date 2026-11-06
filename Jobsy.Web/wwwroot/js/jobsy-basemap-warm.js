(function () {
    "use strict";

    var STYLE_CDN = "https://tiles.openfreemap.org/styles/liberty";
    var PLANET_JSON = "https://tiles.openfreemap.org/planet";
    var STORAGE_STYLE = "jobsy.bk.basemap.style.v1";
    var memoryStyle = null;
    var inflight = null;

    function readStoredStyle() {
        if (memoryStyle) {
            return memoryStyle;
        }
        try {
            var raw = sessionStorage.getItem(STORAGE_STYLE);
            if (!raw) {
                return null;
            }
            memoryStyle = JSON.parse(raw);
            return memoryStyle;
        } catch (e) {
            return null;
        }
    }

    function storeStyle(style) {
        memoryStyle = style;
        try {
            sessionStorage.setItem(STORAGE_STYLE, JSON.stringify(style));
        } catch (e2) { }
    }

    function lngLatToTile(lng, lat, z) {
        var n = Math.pow(2, z);
        var x = Math.floor((lng + 180) / 360 * n);
        var latRad = lat * Math.PI / 180;
        var y = Math.floor((1 - Math.log(Math.tan(latRad) + 1 / Math.cos(latRad)) / Math.PI) / 2 * n);
        return { x: x, y: y };
    }

    function warmUrl(url) {
        if (!url || typeof fetch !== "function") {
            return;
        }
        fetch(url, { mode: "cors", credentials: "omit", cache: "force-cache" }).catch(function () { });
    }

    function warmSprites(style) {
        if (!style || !style.sprite) {
            return;
        }
        var base = String(style.sprite);
        warmUrl(base + ".json");
        warmUrl(base + ".png");
        warmUrl(base + "@2x.json");
        warmUrl(base + "@2x.png");
    }

    function warmGlyphs(style) {
        var glyphRoot = style && style.glyphs ? String(style.glyphs) : "https://tiles.openfreemap.org/fonts/{fontstack}/{range}.pbf";
        var stacks = ["Noto Sans Regular", "Noto Sans Bold"];
        var ranges = ["0-255", "256-511", "512-767"];
        for (var s = 0; s < stacks.length; s++) {
            for (var r = 0; r < ranges.length; r++) {
                warmUrl(glyphRoot.replace("{fontstack}", encodeURIComponent(stacks[s])).replace("{range}", ranges[r]));
            }
        }
    }

    function warmRaster(style, lng, lat, zoom) {
        if (!style || !style.sources) {
            return;
        }
        var src = style.sources.ne2_shaded;
        if (!src || !src.tiles || !src.tiles.length) {
            return;
        }
        var z = Math.max(0, Math.min(6, Math.floor(zoom)));
        var tile = lngLatToTile(lng, lat, z);
        var tpl = src.tiles[0];
        warmUrl(tpl.replace("{z}", String(z)).replace("{x}", String(tile.x)).replace("{y}", String(tile.y)));
    }

    function warmVectorTiles(style, lng, lat, zoom) {
        if (!style || !style.sources || !style.sources.openmaptiles) {
            return;
        }
        var src = style.sources.openmaptiles;
        var tiles = src.tiles;
        if (!tiles || !tiles.length) {
            return;
        }
        var tpl = tiles[0];
        var z = Math.max(4, Math.min(14, Math.floor(zoom)));
        var center = lngLatToTile(lng, lat, z);
        for (var dx = -1; dx <= 1; dx++) {
            for (var dy = -1; dy <= 1; dy++) {
                var x = center.x + dx;
                var y = center.y + dy;
                warmUrl(tpl.replace("{z}", String(z)).replace("{x}", String(x)).replace("{y}", String(y)));
            }
        }
        if (z > 4) {
            var z2 = z - 1;
            var c2 = lngLatToTile(lng, lat, z2);
            warmUrl(tpl.replace("{z}", String(z2)).replace("{x}", String(c2.x)).replace("{y}", String(c2.y)));
        }
    }

    function mergePlanetIntoStyle(style, planet) {
        if (!style || !planet || !style.sources || !style.sources.openmaptiles) {
            return style;
        }
        var next = JSON.parse(JSON.stringify(style));
        var omt = next.sources.openmaptiles;
        if (planet.tiles && planet.tiles.length) {
            omt.tiles = planet.tiles.slice();
            delete omt.url;
        }
        return next;
    }

    function fetchResolvedStyle() {
        if (inflight) {
            return inflight;
        }
        var cached = readStoredStyle();
        if (cached && cached.sources && cached.sources.openmaptiles && cached.sources.openmaptiles.tiles) {
            return Promise.resolve(cached);
        }
        inflight = fetch(STYLE_CDN, { credentials: "omit", cache: "force-cache" })
            .then(function (res) {
                if (!res.ok) {
                    throw new Error("style " + res.status);
                }
                return res.json();
            })
            .then(function (style) {
                return fetch(PLANET_JSON, { credentials: "omit", cache: "force-cache" })
                    .then(function (res) {
                        if (!res.ok) {
                            return style;
                        }
                        return res.json().then(function (planet) {
                            return mergePlanetIntoStyle(style, planet);
                        });
                    })
                    .catch(function () { return style; });
            })
            .then(function (style) {
                storeStyle(style);
                inflight = null;
                return style;
            })
            .catch(function (err) {
                inflight = null;
                throw err;
            });
        return inflight;
    }

    function warmView(view) {
        var lng = view && view.lng != null ? Number(view.lng) : 5.3;
        var lat = view && view.lat != null ? Number(view.lat) : 52.15;
        var zoom = view && view.zoom != null ? Number(view.zoom) : 10.5;
        if (!isFinite(lng) || !isFinite(lat)) {
            lng = 5.3;
            lat = 52.15;
        }
        return fetchResolvedStyle().then(function (style) {
            warmSprites(style);
            warmGlyphs(style);
            warmRaster(style, lng, lat, zoom);
            warmVectorTiles(style, lng, lat, zoom);
            return style;
        }).catch(function () { });
    }

    window.jobsyBasemapWarm = {
        STYLE_CDN: STYLE_CDN,
        getCachedStyle: readStoredStyle,
        ensureStyle: fetchResolvedStyle,
        warm: warmView
    };
})();
