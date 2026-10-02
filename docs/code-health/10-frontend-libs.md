# 10: Frontend libraries: MapLibre 6, html2canvas → maintained fork

- **Branch:** `cursor/code-health-10-frontend`, from `cursor/code-health-09-test-stack`.
- **PR:** into that branch.
- Vendored files live in `Jobsy.Web/wwwroot/lib/`. There are no CDNs, and that must stay so (CSP `script-src 'self'`).

## §1 MapLibre GL JS 5.24.0 → latest 6.x

- Replace `wwwroot/lib/maplibre/maplibre-gl-csp.js`, `maplibre-gl-csp-worker.js`, `maplibre-gl.css` and `.map` with the **CSP build** of the new version, taken from the npm package `maplibre-gl/dist/`. Record the version and the source URL in `wwwroot/lib/maplibre/VERSION.txt`.
- Read the MapLibre 6 changelog and go through every breaking change for our usage in:
  - `wwwroot/js/jobsyMapLibre.js` (loader, worker URL, `setWorkerUrl`, locale, controls, attribution from 01)
  - `jobMap.js` (Banenkaart: clusters, pins, popups, `queryRenderedFeatures`, `easeTo`/`fitBounds`, events)
  - `vacancyDetailMap.js`
  - `js/features/kandidaatinzichten-map.js`
- Rebuild the `.min.js` files (same command as in 01) and bump all `?v=` strings in `App.razor` / the pages.
- CSP: the worker must still load from `'self'` (`worker-src 'self' blob:` — check `JobsyContentSecurityPolicy.cs`), and the OpenFreeMap style/tiles are unchanged.
- **Map retest (required):**
  - all map Playwright suites: `BanenkaartV3PlaywrightTests`, `BanenkaartMapReusePlaywrightTests`, `BanenkaartPersistSizePlaywrightTests`, `BanenkaartClusterCardPlaywrightTests`, `BanenkaartCookiePaddingPlaywrightTests`, `BanenkaartListRerenderPlaywrightTests`, `BanenkaartMapPerfPlaywrightTests`, `JobMapPinsClustersPlaywrightTests`, `MobileSmokePlaywrightTests`
  - screenshots of `/banenkaart` (desktop and mobile), a vacancy detail map and the Kandidaatinzichten map, before and after
  - no console errors and no CSP violations
  - performance: `BanenkaartMapPerfPlaywrightTests` must stay within its budget. Report the numbers before and after.
- If a breaking change needs a large rewrite, stop with a draft PR that lists what's affected.

## §2 html2canvas 1.4.1 (unmaintained since 2022) → maintained fork

- Candidate: `html2canvas-pro` (MIT, a drop-in API fork with modern CSS colour support, e.g. `oklch`/`color-mix`). Check its licence and its latest release at implementation time, and record them in `wwwroot/lib/html2canvas/VERSION.txt`.
- Find where it is used (grep `html2canvas` in `wwwroot/js`, e.g. the feedback widget's screenshot capture). Swap the file and keep the global name the code expects (`window.html2canvas`), or adapt the one call site.
- Test: Playwright opens the feedback widget, triggers a screenshot and asserts a non-empty PNG data URL, on a page that uses the brand CSS colours.

## Acceptance

- The map suites and the screenshot test are green, with before/after screenshots attached.
- Release build: 0 new warnings. Full tests: no new failures.
- `VERSION.txt` files are present for both libraries.
