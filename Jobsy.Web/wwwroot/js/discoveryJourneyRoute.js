// Brush-style route through halte vignette centers (ontdekkingsreis overview).
export function refreshById(elementId) {
  if (!elementId) {
    return;
  }
  refresh(document.getElementById(elementId));
}

export function refresh(canvas) {
  if (!canvas) {
    return;
  }
  const layout = canvas.dataset.routeLayout || "desktop";
  if (layout === "mobile") {
    refreshMobile(canvas);
    return;
  }
  refreshDesktop(canvas);
}

function refreshDesktop(canvas) {
  const pathEl = canvas.querySelector("#discovery-route-path");
  if (!pathEl) {
    return;
  }
  const vignettes = canvas.querySelectorAll("[data-discovery-vignette]");
  if (vignettes.length < 2) {
    pathEl.setAttribute("d", "");
    return;
  }
  const cr = canvas.getBoundingClientRect();
  const points = vignetteCenters(vignettes, cr);
  const svg = pathEl.closest("svg");
  if (svg) {
    svg.setAttribute("viewBox", `0 0 ${cr.width} ${cr.height}`);
  }
  pathEl.setAttribute("d", smoothPath(points));
  positionConnector(canvas);
}

function refreshMobile(canvas) {
  const futureEl = canvas.querySelector("#discovery-route-path-future");
  const doneEl = canvas.querySelector("#discovery-route-path-done");
  if (!futureEl || !doneEl) {
    return;
  }
  const vignettes = canvas.querySelectorAll(".journey-route-mobile__stops [data-discovery-vignette]");
  if (vignettes.length < 2) {
    futureEl.setAttribute("d", "");
    doneEl.setAttribute("d", "");
    return;
  }
  const cr = canvas.getBoundingClientRect();
  const w = Math.max(1, cr.width);
  const h = Math.max(1, cr.height);
  const svg = futureEl.closest("svg");
  if (svg) {
    svg.setAttribute("viewBox", `0 0 ${w} ${h}`);
  }
  const points = vignetteCenters(vignettes, cr);
  futureEl.setAttribute("d", smoothPath(points));

  const youPhase = canvas.dataset.youAtPhase || "";
  let youIndex = 0;
  if (youPhase) {
    const stops = canvas.querySelectorAll(".journey-route-mobile__stop[data-phase]");
    stops.forEach((stop, idx) => {
      if (stop.dataset.phase === youPhase) {
        youIndex = idx;
      }
    });
  }
  const fracRaw = parseFloat(canvas.dataset.progressFrac || "0.45", 10);
  const frac = Number.isFinite(fracRaw) ? Math.min(1, Math.max(0.08, fracRaw)) : 0.45;
  doneEl.setAttribute("d", partialPath(points, youIndex, frac));
}

function vignetteCenters(vignettes, cr) {
  const points = [];
  vignettes.forEach((v) => {
    const er = v.getBoundingClientRect();
    points.push({
      x: er.left + er.width / 2 - cr.left,
      y: er.top + er.height / 2 - cr.top,
    });
  });
  return points;
}

function positionConnector(canvas) {
  const wrap = canvas.closest(".journey-route")?.querySelector(".journey-route__panel-wrap");
  if (!wrap) {
    return;
  }
  const active = canvas.querySelector(".journey-route__stop.is-current");
  const vig = active?.querySelector("[data-discovery-vignette]");
  if (!vig) {
    return;
  }
  const cr = canvas.getBoundingClientRect();
  const er = vig.getBoundingClientRect();
  const x = er.left + er.width / 2 - cr.left;
  const pct = (x / cr.width) * 100;
  wrap.style.setProperty("--discovery-connector-x", `${pct.toFixed(2)}%`);
}

export function bindResize(canvas) {
  if (!canvas) {
    return;
  }
  const run = () => {
    refresh(canvas);
    requestAnimationFrame(() => refresh(canvas));
  };
  if (canvas.dataset.routeBound !== "1") {
    canvas.dataset.routeBound = "1";
    window.addEventListener("resize", run);
    if (window.ResizeObserver) {
      const ro = new ResizeObserver(run);
      ro.observe(canvas);
      const mapCard = canvas.closest(".journey-route-mobile__map");
      if (mapCard && mapCard !== canvas) {
        ro.observe(mapCard);
      }
    }
  }
  run();
}

function partialPath(points, endIndex, frac) {
  if (points.length < 2) {
    return "";
  }
  const idx = Math.min(Math.max(0, endIndex), points.length - 2);
  const head = points.slice(0, idx + 1);
  const a = points[idx];
  const b = points[idx + 1];
  head.push({
    x: a.x + (b.x - a.x) * frac,
    y: a.y + (b.y - a.y) * frac,
  });
  return smoothPath(head);
}

function smoothPath(points) {
  if (points.length < 2) {
    return "";
  }
  let d = `M ${points[0].x.toFixed(1)} ${points[0].y.toFixed(1)}`;
  for (let i = 0; i < points.length - 1; i++) {
    const p0 = points[i - 1] || points[i];
    const p1 = points[i];
    const p2 = points[i + 1];
    const p3 = points[i + 2] || p2;
    const cp1x = p1.x + (p2.x - p0.x) / 6;
    const cp1y = p1.y + (p2.y - p0.y) / 6;
    const cp2x = p2.x - (p3.x - p1.x) / 6;
    const cp2y = p2.y - (p3.y - p1.y) / 6;
    d += ` C ${cp1x.toFixed(1)} ${cp1y.toFixed(1)}, ${cp2x.toFixed(1)} ${cp2y.toFixed(1)}, ${p2.x.toFixed(1)} ${p2.y.toFixed(1)}`;
  }
  return d;
}
