# Step 10 — MapLibre 6 blocker (analysis)

**Status:** Stopped per step 10 (“if a breaking change needs a large rewrite, draft PR and list what is affected”). **No MapLibre 6 code bump in this PR.**

## Current

- Vendored **maplibre-gl 5.24.0** CSP build under `Jobsy.Web/wwwroot/lib/maplibre/` (`VERSION.txt`).
- Loader: `app-core.js` injects classic `<script src="/lib/maplibre/maplibre-gl-csp.js">` and calls `maplibregl.setWorkerUrl(...)` for `maplibre-gl-csp-worker.js`.
- Consumers: `jobsyMapLibre.js`, `jobMap.js`, `vacancyDetailMap.js`, `kandidaatinzichten-map.js` (global `window.maplibregl`).
- CSP: `worker-src 'self' blob:` (`JobsyContentSecurityPolicy.cs`).

## What MapLibre 6 changes

From the [v6 changelog](https://github.com/maplibre/maplibre-gl-js/blob/main/CHANGELOG.md) (#6254):

> Switch to an ESM-only distribution (`maplibre-gl.mjs`). The UMD bundles (`maplibre-gl.js`, `maplibre-gl-csp.js`) are **no longer published**. The CSP-specific bundle is also dropped: the ESM build loads its worker as a real URL, so `worker-src blob:` is no longer required. Consumers using `<script src=".../maplibre-gl.js">` must switch to `<script type="module">`.

Latest checked: **6.11.2** — `dist/` has `maplibre-gl.mjs` + `maplibre-gl-worker.mjs` + `maplibre-gl-shared.mjs` only (no `*-csp*`).

## Affected surfaces (rewrite scope)

| Area | Work |
|---|---|
| `app-core.js` / `jobsyMaps` | Replace classic script injection with `type="module"` import (or a tiny module boot that assigns `window.maplibregl`) |
| Worker URL | Drop `setWorkerUrl` CSP worker path; use ESM worker URL from package (still `'self'`) |
| CSP | Can tighten `worker-src` (blob optional); keep `script-src 'self'` (no CDN) |
| `jobsyMapLibre` / `jobMap` / vacancy / inzichten | Keep globals or migrate to imports; minify pipeline (`terser`) must accept ESM or a bundled IIFE we build ourselves |
| Playwright map suites | Full retest + before/after screenshots once loader lands |

## Effort

**High** — not a drop-in file replace. Needs a deliberate ESM loader design under the existing CSP/no-CDN rules, then the map Playwright matrix from step 10.

## Shipped in this PR instead

- **html2canvas → html2canvas-pro 2.5.0** (MIT, drop-in `window.html2canvas`) under `wwwroot/lib/html2canvas/`.
