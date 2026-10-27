(function () {
  "use strict";

  function root() {
    return document.querySelector('[data-bedrijvenkaart-ssr="1"]');
  }

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function escapeAttr(value) {
    return escapeHtml(value).replace(/'/g, "&#39;");
  }

  function readJsonScript(id) {
    var el = document.getElementById(id);
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

  function writeJsonScript(id, pins) {
    var el = document.getElementById(id);
    if (el) {
      el.textContent = JSON.stringify(pins);
    }
  }

  function readAllPins() {
    return readJsonScript("bedrijvenkaart-ssr-pins-all");
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

  function originCoords() {
    var el = root();
    if (!el) {
      return null;
    }
    var lat = parseFloat(el.getAttribute("data-bk-origin-lat") || "");
    var lng = parseFloat(el.getAttribute("data-bk-origin-lng") || "");
    if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
      return null;
    }
    return { lat: lat, lng: lng };
  }

  function pinsUrl(km) {
    var url = "/api/bedrijvenkaart-client/pins?radiusKm=" + encodeURIComponent(String(km));
    var o = originCoords();
    if (o) {
      url += "&lat=" + encodeURIComponent(String(o.lat)) + "&lng=" + encodeURIComponent(String(o.lng));
    }
    return url;
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
    var opts = {
      pinsUrl: pinsUrl(km),
      mapMode: "companies",
      preferFilledLocation: false
    };
    var o = originCoords();
    if (o) {
      opts.origin = { lat: o.lat, lng: o.lng };
      opts.travel = { radiusKm: km };
    }
    window.jobMap.init("bedrijvenkaart-map", markers, opts);
    if (typeof window.jobMap.invalidate === "function") {
      window.requestAnimationFrame(function () {
        window.jobMap.invalidate();
        window.requestAnimationFrame(function () {
          window.jobMap.invalidate();
        });
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

  function initials(name) {
    var parts = String(name || "")
      .trim()
      .split(/\s+/)
      .filter(Boolean);
    if (parts.length === 0) {
      return "?";
    }
    if (parts.length === 1) {
      return parts[0].slice(0, 2).toUpperCase();
    }
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  function buildCardHtml(pin) {
    var id = String(pin.companyId || pin.CompanyId || "");
    var name = pin.name || pin.Name || "";
    var city = pin.city || pin.City || "Nederland";
    var sector = pin.sectorLabel || pin.SectorLabel || "Bedrijf";
    var path = pin.publicPath || pin.PublicPath || "";
    var tag = path ? "a" : "article";
    var href = path ? ' href="' + escapeAttr(path) + '"' : "";
    var linkClass = path ? " bk-card--link" : "";
    var open = pin.openForInitiative || pin.OpenForInitiative;
    var vac = pin.vacancyCount != null ? pin.vacancyCount : pin.VacancyCount;
    var chips =
      (open ? '<span class="bk-chip bk-chip--open">Open voor initiatief</span>' : "") +
      (vac > 0
        ? '<span class="bk-chip">' + escapeHtml(String(vac)) + " vacatures</span>"
        : '<span class="bk-chip">Geen vacatures</span>');
    return (
      "<" +
      tag +
      ' class="bk-card' +
      linkClass +
      '"' +
      href +
      ' role="listitem" data-bk-company-id="' +
      escapeAttr(id) +
      '">' +
      '<div class="bk-card__row"><div class="bk-card__avatar">' +
      escapeHtml(initials(name)) +
      "</div><div>" +
      '<h2 class="bk-card__title">' +
      escapeHtml(name) +
      "</h2>" +
      '<p class="bk-card__meta">' +
      escapeHtml(sector) +
      " • " +
      escapeHtml(city) +
      "</p>" +
      chips +
      "</div></div></" +
      tag +
      ">"
    );
  }

  function renderList(pins) {
    var list = root() && root().querySelector(".bk-list");
    if (!list) {
      return;
    }
    list.innerHTML = (pins || []).map(buildCardHtml).join("");
  }

  function syncListVisibility(filteredPins) {
    var ids = {};
    (filteredPins || []).forEach(function (p) {
      ids[String(p.companyId || p.CompanyId || "")] = true;
    });
    root().querySelectorAll(".bk-card[data-bk-company-id]").forEach(function (card) {
      var id = card.getAttribute("data-bk-company-id");
      card.hidden = !ids[id];
    });
  }

  function refreshFromPins(allPins, km, reRenderList) {
    var filtered = applyClientFilters(allPins);
    writeJsonScript("bedrijvenkaart-ssr-pins", filtered);
    if (reRenderList) {
      renderList(allPins);
      syncListVisibility(filtered);
    } else {
      syncListVisibility(filtered);
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
        writeJsonScript("bedrijvenkaart-ssr-pins-all", pins);
        refreshFromPins(pins, km, true);
      })
      .catch(function () {
        refreshFromPins(readAllPins(), km, false);
      });
  }

  function bootMap() {
    if (!root()) {
      return;
    }
    ensureMaps()
      .then(function () {
        var all = readAllPins();
        refreshFromPins(all, radiusKm(), false);
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
        refreshFromPins(readAllPins(), radiusKm(), false);
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
