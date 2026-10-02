/* Lightweight MapLibre density map for employer Kandidaatinzichten. Does not modify jobMap.js. */
(function (global) {
  "use strict";

  var instances = new Map();

  function ensureMapLibre() {
    if (global.maplibregl) {
      return Promise.resolve(global.maplibregl);
    }
    return Promise.reject(new Error("maplibregl not loaded"));
  }

  function ringCoords(lat, lng, km, steps) {
    steps = steps || 64;
    var coords = [];
    var earth = 6371;
    for (var i = 0; i <= steps; i++) {
      var bearing = (i / steps) * 2 * Math.PI;
      var lat1 = (lat * Math.PI) / 180;
      var lng1 = (lng * Math.PI) / 180;
      var ang = km / earth;
      var lat2 = Math.asin(
        Math.sin(lat1) * Math.cos(ang) + Math.cos(lat1) * Math.sin(ang) * Math.cos(bearing)
      );
      var lng2 =
        lng1 +
        Math.atan2(
          Math.sin(bearing) * Math.sin(ang) * Math.cos(lat1),
          Math.cos(ang) - Math.sin(lat1) * Math.sin(lat2)
        );
      coords.push([(lng2 * 180) / Math.PI, (lat2 * 180) / Math.PI]);
    }
    return coords;
  }

  function buildGeo(density, branches, selectedRadiusKm) {
    var features = [];
    (density || []).forEach(function (cell) {
      features.push({
        type: "Feature",
        properties: { band: cell.band || cell.Band || 1 },
        geometry: {
          type: "Point",
          coordinates: [cell.centerLng || cell.CenterLng, cell.centerLat || cell.CenterLat]
        }
      });
    });

    var rings = [];
    (branches || []).forEach(function (b) {
      var lat = b.lat || b.Lat;
      var lng = b.lng || b.Lng;
      if (typeof lat !== "number" || typeof lng !== "number") return;
      features.push({
        type: "Feature",
        properties: { kind: "branch" },
        geometry: { type: "Point", coordinates: [lng, lat] }
      });
      [10, 20, 30].forEach(function (km) {
        rings.push({
          type: "Feature",
          properties: { km: km, selected: km === selectedRadiusKm },
          geometry: { type: "Polygon", coordinates: [ringCoords(lat, lng, km)] }
        });
      });
    });

    return {
      density: { type: "FeatureCollection", features: features.filter(function (f) { return !f.properties.kind; }) },
      branches: {
        type: "FeatureCollection",
        features: features.filter(function (f) { return f.properties.kind === "branch"; })
      },
      rings: { type: "FeatureCollection", features: rings }
    };
  }

  async function mount(elementId, options) {
    options = options || {};
    var maplibregl = await ensureMapLibre();
    var el = document.getElementById(elementId);
    if (!el) return null;

    destroy(elementId);

    var center = options.center || [5.2, 52.1];
    var compact = !(window.matchMedia && window.matchMedia("(min-width: 1025px)").matches);
    var map = new maplibregl.Map({
      container: el,
      style: options.styleUrl || "https://demotiles.maplibre.org/style.json",
      center: center,
      zoom: options.zoom || 9,
      interactive: false,
      attributionControl: {
        compact: compact,
        customAttribution:
          '<a href="https://openfreemap.org" target="_blank" rel="noopener">OpenFreeMap</a> © <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>'
      },
      maplibreLogo: false
    });
    map.addControl(new maplibregl.NavigationControl({ showCompass: false }), "top-right");
    // Keep zoom only
    map.scrollZoom.disable();
    map.dragPan.disable();
    map.boxZoom.disable();
    map.dragRotate.disable();
    map.touchZoomRotate.disableRotation();

    map.on("load", function () {
      var geo = buildGeo(options.density, options.branches, options.radiusKm || 20);
      map.addSource("insights-density", { type: "geojson", data: geo.density });
      map.addSource("insights-rings", { type: "geojson", data: geo.rings });
      map.addSource("insights-branches", { type: "geojson", data: geo.branches });

      map.addLayer({
        id: "insights-rings-fill",
        type: "fill",
        source: "insights-rings",
        paint: {
          "fill-color": "#2563eb",
          "fill-opacity": [
            "case",
            ["==", ["get", "km"], 10],
            0.18,
            ["==", ["get", "km"], 20],
            0.1,
            0.05
          ]
        }
      });
      map.addLayer({
        id: "insights-rings-line",
        type: "line",
        source: "insights-rings",
        paint: {
          "line-color": "#2563eb",
          "line-width": ["case", ["get", "selected"], 5, 3],
          "line-opacity": 0.9
        }
      });
      map.addLayer({
        id: "insights-density-circles",
        type: "circle",
        source: "insights-density",
        paint: {
          "circle-radius": ["match", ["get", "band"], 1, 10, 2, 14, 18],
          "circle-color": "#15803d",
          "circle-opacity": ["match", ["get", "band"], 1, 0.35, 2, 0.55, 0.8],
          "circle-stroke-width": 0
        }
      });
      map.addLayer({
        id: "insights-branches",
        type: "circle",
        source: "insights-branches",
        paint: {
          "circle-radius": 7,
          "circle-color": "#0f2d5c",
          "circle-stroke-width": 2,
          "circle-stroke-color": "#ffffff"
        }
      });

      try {
        el.style.filter = "saturate(0.55) contrast(0.95)";
      } catch (_) {}
    });

    instances.set(elementId, map);
    return map;
  }

  function update(elementId, options) {
    var map = instances.get(elementId);
    if (!map || !map.getSource) return;
    var geo = buildGeo(options.density, options.branches, options.radiusKm || 20);
    if (map.getSource("insights-density")) map.getSource("insights-density").setData(geo.density);
    if (map.getSource("insights-rings")) map.getSource("insights-rings").setData(geo.rings);
    if (map.getSource("insights-branches")) map.getSource("insights-branches").setData(geo.branches);
  }

  function destroy(elementId) {
    var map = instances.get(elementId);
    if (map) {
      try { map.remove(); } catch (_) {}
      instances.delete(elementId);
    }
  }

  global.JobsyCandidateInsightsMap = { mount: mount, update: update, destroy: destroy };

  global.JobsyCandidateInsightsMapLoader = {
    _loading: null,
    ensure: function () {
      if (global.JobsyCandidateInsightsMap && global.JobsyCandidateInsightsMap.mount) {
        return Promise.resolve();
      }
      if (this._loading) return this._loading;
      this._loading = new Promise(function (resolve, reject) {
        var s = document.createElement("script");
        s.src = "js/features/kandidaatinzichten-map.js?v=20261002-ch01";
        s.onload = function () { resolve(); };
        s.onerror = reject;
        document.head.appendChild(s);
      });
      return this._loading;
    }
  };
})(typeof window !== "undefined" ? window : globalThis);
