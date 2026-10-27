(function () {
  "use strict";

  function root() {
    return document.querySelector('[data-bedrijvenkaart-ssr="1"]');
  }

  function readPins() {
    var el = document.getElementById("bedrijvenkaart-ssr-pins");
    if (!el) {
      return [];
    }
    try {
      var parsed = JSON.parse(el.textContent || "[]");
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      return [];
    }
  }

  function radiusKm() {
    var el = root();
    if (!el) {
      return 25;
    }
    var raw = el.getAttribute("data-bk-radius-km");
    var n = parseInt(raw || "25", 10);
    return n === 10 || n === 25 || n === 40 ? n : 25;
  }

  function toMarkers(pins) {
    return (pins || []).map(function (p) {
      return {
        id: String(p.companyId || p.CompanyId || ""),
        companyId: String(p.companyId || p.CompanyId || ""),
        title: p.name || p.Name || "",
        company: p.name || p.Name || "",
        lat: p.latitude != null ? p.latitude : p.Latitude,
        lng: p.longitude != null ? p.longitude : p.Longitude,
        openForInitiative: !!(p.openForInitiative || p.OpenForInitiative),
        isIntermediary: !!(p.isIntermediary || p.IsIntermediary),
        publicMapRadiusKm: p.isIntermediary || p.IsIntermediary ? 5 : null,
        companyHref: p.publicPath || p.PublicPath || null
      };
    });
  }

  function pinsUrl(km) {
    return "/api/company-map-initiative/pins?radiusKm=" + encodeURIComponent(String(km));
  }

  function ensureMaps() {
    if (window.jobsyMaps && typeof window.jobsyMaps.ensure === "function") {
      return window.jobsyMaps.ensure("discovery");
    }
    return Promise.reject(new Error("jobsyMaps missing"));
  }

  function initMap(markers, km) {
    if (!window.jobMap || typeof window.jobMap.init !== "function") {
      return;
    }
    window.jobMap.init("bedrijvenkaart-map", markers, {
      pinsUrl: pinsUrl(km),
      mapMode: "companies",
      preferFilledLocation: false
    });
    if (typeof window.jobMap.invalidate === "function") {
      window.requestAnimationFrame(function () {
        window.jobMap.invalidate();
      });
    }
  }

  function setRadiusActive(km) {
    var host = root();
    if (!host) {
      return;
    }
    host.setAttribute("data-bk-radius-km", String(km));
    host.querySelectorAll("button[data-bk-radius-km]").forEach(function (btn) {
      var val = parseInt(btn.getAttribute("data-bk-radius-km"), 10);
      var on = val === km;
      btn.classList.toggle("is-active", on);
      btn.setAttribute("aria-pressed", on ? "true" : "false");
    });
  }

  function updateMeta(count, km, openCount) {
    var host = root();
    if (!host) {
      return;
    }
    var meta = host.querySelector(".bk-result-meta");
    if (!meta) {
      return;
    }
    meta.textContent =
      String(count) +
      " bedrijven binnen " +
      String(km) +
      " km • " +
      String(openCount != null ? openCount : 0) +
      " staan open voor jou";
  }

  function openCount(pins) {
    var n = 0;
    (pins || []).forEach(function (p) {
      if (p.openForInitiative || p.OpenForInitiative) {
        n += 1;
      }
    });
    return n;
  }

  function applyClientFilters(pins) {
    var host = root();
    if (!host) {
      return pins;
    }
    var openOnly = host.querySelector('[data-bk-filter="openOnly"]');
    if (openOnly && openOnly.getAttribute("aria-pressed") === "true") {
      pins = pins.filter(function (p) {
        return p.openForInitiative || p.OpenForInitiative;
      });
    }
    var agencies = host.querySelector('[data-bk-filter="agencies"]');
    if (agencies && agencies.getAttribute("aria-pressed") === "false") {
      pins = pins.filter(function (p) {
        return !(p.isIntermediary || p.IsIntermediary);
      });
    }
    return pins;
  }

  function refreshFromPins(pins, km) {
    var filtered = applyClientFilters(pins);
    var script = document.getElementById("bedrijvenkaart-ssr-pins");
    if (script) {
      script.textContent = JSON.stringify(filtered);
    }
    initMap(toMarkers(filtered), km);
    updateMeta(filtered.length, km, openCount(filtered));
  }

  function fetchPins(km) {
    return fetch(pinsUrl(km), {
      credentials: "same-origin",
      headers: { Accept: "application/json" }
    }).then(function (res) {
      if (!res.ok) {
        throw new Error("pins fetch failed");
      }
      return res.json();
    });
  }

  function onRadius(km) {
    setRadiusActive(km);
    fetchPins(km)
      .then(function (pins) {
        refreshFromPins(pins, km);
      })
      .catch(function () {
        refreshFromPins(readPins(), km);
      });
  }

  function bootMap() {
    if (!root()) {
      return;
    }
    ensureMaps()
      .then(function () {
        var pins = readPins();
        initMap(toMarkers(applyClientFilters(pins)), radiusKm());
        document.dispatchEvent(new CustomEvent("jobsy-bk-map-pane-visible"));
      })
      .catch(function () { });
  }

  function bindFilters() {
    if (document.documentElement.getAttribute("data-bk-ssr-map-bound") === "1") {
      return;
    }
    document.documentElement.setAttribute("data-bk-ssr-map-bound", "1");

    document.body.addEventListener("click", function (ev) {
      var host = ev.target.closest('[data-bedrijvenkaart-ssr="1"]');
      if (!host) {
        return;
      }
      var radiusBtn = ev.target.closest("button[data-bk-radius-km]");
      if (radiusBtn) {
        ev.preventDefault();
        var km = parseInt(radiusBtn.getAttribute("data-bk-radius-km"), 10);
        if (km === 10 || km === 25 || km === 40) {
          onRadius(km);
        }
        return;
      }
      var chip = ev.target.closest("button[data-bk-filter]");
      if (chip) {
        ev.preventDefault();
        var pressed = chip.getAttribute("aria-pressed") === "true";
        chip.setAttribute("aria-pressed", pressed ? "false" : "true");
        chip.classList.toggle("is-active", !pressed);
        refreshFromPins(readPins(), radiusKm());
      }
    });
  }

  function start() {
    bindFilters();
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", bootMap);
    } else {
      bootMap();
    }
  }

  start();
})();
