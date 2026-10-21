// Brush-style route through halte vignette centers (ontdekkingsreis overview).
export function refresh(canvas) {
  if (!canvas) {
    return;
  }
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
  const points = [];
  vignettes.forEach((v) => {
    const er = v.getBoundingClientRect();
    points.push({
      x: er.left + er.width / 2 - cr.left,
      y: er.top + er.height / 2 - cr.top,
    });
  });
  const svg = pathEl.closest("svg");
  if (svg) {
    svg.setAttribute("viewBox", `0 0 ${cr.width} ${cr.height}`);
  }
  pathEl.setAttribute("d", smoothPath(points));
  positionConnector(canvas);
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
  if (!canvas || canvas.dataset.routeBound === "1") {
    return;
  }
  canvas.dataset.routeBound = "1";
  const run = () => refresh(canvas);
  window.addEventListener("resize", run);
  if (window.ResizeObserver) {
    const ro = new ResizeObserver(run);
    ro.observe(canvas);
  }
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
