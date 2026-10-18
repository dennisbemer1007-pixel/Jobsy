(function (global) {
  "use strict";

  var instances = new Map();

  function ensureMapLibre() {
    if (global.maplibregl) return Promise.resolve(global.maplibregl);
    return Promise.reject(new Error("maplibregl not loaded"));
  }

  function areasGeo(areas, selectedKey) {
    var features = (areas || []).map(function (a) {
      var ring = a.polygonRing || a.PolygonRing || [];
      var coords = ring.map(function (p) {
        return [p[0], p[1]];
      });
      if (coords.length && (coords[0][0] !== coords[coords.length - 1][0] || coords[0][1] !== coords[coords.length - 1][1])) {
        coords.push(coords[0]);
      }
      return {
        type: "Feature",
        properties: {
          key: a.areaKey || a.AreaKey,
          label: a.label || a.Label,
          selected: (a.areaKey || a.AreaKey) === selectedKey
        },
        geometry: { type: "Polygon", coordinates: [coords] }
      };
    });
    return { type: "FeatureCollection", features: features };
  }

  async function mount(elementId, options) {
    options = options || {};
    var maplibregl = await ensureMapLibre();
    var el = document.getElementById(elementId);
    if (!el) return;

    destroy(elementId);

    var center = options.center || [5.38, 52.15];
    var map = new maplibregl.Map({
      container: elementId,
      style: options.styleUrl || "https://basemaps.cartocdn.com/gl/positron-gl-style/style.json",
      center: center,
      zoom: options.zoom || 9,
      attributionControl: false
    });

    map.addControl(new maplibregl.NavigationControl({ showCompass: false }), "top-right");

    map.on("load", function () {
      var geo = areasGeo(options.areas, options.selectedAreaKey);
      map.addSource("kk-areas", { type: "geojson", data: geo });
      map.addLayer({
        id: "kk-areas-fill",
        type: "fill",
        source: "kk-areas",
        paint: {
          "fill-color": [
            "case",
            ["boolean", ["get", "selected"], false],
            "rgba(220, 80, 70, 0.35)",
            "rgba(220, 80, 70, 0.18)"
          ],
          "fill-outline-color": "rgba(180, 50, 45, 0.55)"
        }
      });
      map.on("click", "kk-areas-fill", function (e) {
        if (!e.features || !e.features.length) return;
        var key = e.features[0].properties.key;
        if (options.onAreaSelect && key) options.onAreaSelect(key);
      });
      map.getCanvas().style.cursor = "pointer";
    });

    instances.set(elementId, map);
  }

  function updateAreas(elementId, areas, selectedAreaKey) {
    var map = instances.get(elementId);
    if (!map || !map.getSource("kk-areas")) return;
    map.getSource("kk-areas").setData(areasGeo(areas, selectedAreaKey));
  }

  function destroy(elementId) {
    var map = instances.get(elementId);
    if (map) {
      map.remove();
      instances.delete(elementId);
    }
  }

  global.JobsyKandidatenkaartMap = { mount: mount, updateAreas: updateAreas, destroy: destroy };
})(typeof window !== "undefined" ? window : globalThis);
