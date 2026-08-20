# MapLibre GL JS (vendored)

Local copy of [MapLibre GL JS 5.24.0](https://maplibre.org/) (CSP build: `maplibre-gl-csp.js` + worker) so the homepage does not block on a CDN, CSP stays `'self'` for scripts, and the worker is parsed off the main thread.

Used with OpenFreeMap vector styles:

- Liberty: `https://tiles.openfreemap.org/styles/liberty`
- Bright (3D camera): `https://tiles.openfreemap.org/styles/bright`

License: BSD-3-Clause.

A `maplibre-gl-csp.js.map` sits next to the bundle so Lighthouse/PageSpeed can map the ~950&nbsp;KB first-party file. Original `sourcesContent` is omitted (the TypeScript sources are not hosted on lobsy.nl).
