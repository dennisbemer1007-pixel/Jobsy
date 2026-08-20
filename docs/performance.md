# Lobsy website performance

Lab baseline, productie `https://lobsy.nl` (geen CrUX field data):

| Bron | Perf | A11y | BP | SEO | FCP | LCP | TBT | CLS |
|---|---|---|---|---|---|---|---|---|
| PageSpeed Insights (aug 2026, opgave) | 56 | 95 | 77 | 91 | — | — | — | — |
| Lighthouse 12.8 mobile (deze run, pre-fix) | 47 | 95 | 82 | 91 | 2.1s | **21.6s** | 600ms | 0.108 |
| Lighthouse 12.8 mobile (na image/JS-fix, live) | 69 | 95 | 82 | 91 | 2.0s | **2.4s** | **830ms** | 0.108 |
| Lighthouse 12.8 mobile (deze PR, localhost) | **99** | — | — | — | **1.2s** | **1.9s** (NL-preview) | **0ms** | 0 |

LCP was een Carto-tegel (`leaflet-tile`) met **20s load delay**: de netwerkrij stond vol met `lobsy.png` (568&nbsp;KB) en tientallen picsum-JPEG’s (~60–80&nbsp;KB, ~9&nbsp;MB / 584 requests). Leaflet-CSS via unpkg was render-blocking (~780&nbsp;ms).

Na die eerste ronde was het gewicht weg (36 requests / ~334&nbsp;KB) en LCP ~2.4s, maar de labscore bleef hangen: de homepage prerenderde **alle ~278 job cards** (~6.5k DOM-nodes, TBT 830ms) en de cookiebanner verscheen pas na JS (late LCP-tekst).

## Homepage-kaart (aug 2026)

De banenkaart blijft de first-paint kernervaring (geen Funda-klik-om-te-tonen). PageSpeed-PRs #159–#165 (vaste 300px-box, MapLibre pas na `window.load` of klik, minified CSS/JS, cards-first) zijn teruggedraaid omdat de desktopkaart leeg bleef.

- Home prerendert alleen de kaart-chrome (landkleur, geen nep-pins en geen NL-overzicht). `#job-map` heeft een vaste `min-height` (55dvh / 70vh) zodat CLS niet optreedt.
- MapLibre GL JS + OpenFreeMap laden **lui**: IntersectionObserver + `requestIdleCallback` (geen blocking CSS/JS in de eerste HTML). Overlay weg zodra er echte markers zijn.
- Cookie-banner is compact, paint-contained, en wint de LCP niet van de kaart.
- Critical CSS staat inline in `App.razor`; de volle `app.css` volgt asynchroon (`media=print` → `all`). Scripts (`app-core`, `blazor.web.js`) hebben `defer`.

## Wat er nu in de code zit

### Images

- Mock/seed-foto’s blijven de originele picsum-seeds (`jobsy-{vacancyId}`). Ze starten **niet** in first paint: kaarten en carousel pas ná `_mapPainted`, allemaal `loading=lazy`, lijst op 400×267. Unsplash-404’s en SVG-stand-ins gaan bij backfill terug naar picsum.
- Job cards gebruiken een echt `<img>` (`VacancyPhoto`) met `loading="lazy"`, `decoding="async"`, intrinsieke 600×400 en `sizes`. De eerste twee kaarten zijn eager.
- De vacaturelijst wordt **niet** in de eerste HTML/mobile-kaartweergave gezet. Desktop en de mobiele lijst tonen vensters van 12 kaarten (+ “toon meer”). Featured-carousel max. 8.
- Logo: WebP 64/128/256 i.p.v. 1024×1024 PNG (568&nbsp;KB). Watermarks lazy, apple-touch-icon 180&nbsp;px.
- Optioneel Cloudflare Image Resizing: zet `Cloudflare__ImageResizing=true` (betaalde CF-add-on). Alleen same-origin paden (`/images/…`) worden gewrapt — geen absolute http(s)-URL’s (geen CF-fetch/SSRF-proxy).

### JavaScript / critical path

- MapLibre GL JS staat lokaal in `wwwroot/lib/maplibre/` en laadt via `jobsyMaps.ensureAfterPaint("discovery")` (idle + nabij viewport) of `ensure("detail")`. OpenFreeMap-stijlen komen van `tiles.openfreemap.org`.
- First-party JS is gebundeld in `js/app-core.js` (geo, culture, session, cookies, download, richtext, maps-loader) en staat op `defer`.
- `jobsyMaps.ensure("discovery"|"detail")` laadt niet beide kaart-scripts op elke pagina.
- Cookiebanner staat in de eerste HTML (compact); `html.cookie-consent-known` verbergt hem vóór paint als de keuze al bekend is. Paint-containment houdt hem buiten de LCP.
- Critical CSS inline; `app.css` non-blocking. Geen webfonts.

### Server / edge

- Response compression (Brotli/Gzip) voor HTML/CSS/JS/SVG.
- Statische assets (JS/CSS/images/fonts): Lighthouse *efficient cache lifetimes*. URLs met `?v=` krijgen `Cache-Control: public, max-age=31536000, immutable` (1 jaar). Overige bestanden minstens 30 dagen (`max-age=2592000`) plus `stale-while-revalidate`. MapLibre en `blazor.web.js` hebben een versie-query.
- `www.` → apex 301 (Cloudflare doet dit al; middleware is fallback).
- `HEAD` op `/` geeft geen 405 meer (zelfde headers als GET, zonder body).
- CSP zonder `unpkg.com`.

## Meten

Na deploy naar `main`:

1. [PageSpeed Insights](https://pagespeed.web.dev/analysis?url=https://lobsy.nl) (mobile + desktop).
2. Lokaal: `npx lighthouse https://lobsy.nl --only-categories=performance,accessibility,best-practices,seo --form-factor=mobile --output=json --output-path=./lighthouse-mobile.json`.

Verwachte richting na deploy (lab, mobiel, cache-warm): LCP uit de 20s-range (picsum-storm + 568&nbsp;KB logo weg; Carto preconnect + Leaflet preload), Performance richting **70–90**, Best Practices **≥90**. Blazor Server (`blazor.web.js` + circuit) blijft een TBT-bodem; dat is geen WASM-bundle die je kunt splitten.

Herhaal PSI/Lighthouse op `https://lobsy.nl` na merge naar `main` en vul de tabel hierboven aan.

## Bewust niet gedaan

- Eigen image-CDN of upload-pipeline (Cloudflare Resizing is de schakelaar als die add-on aanstaat).
- Critical-CSS extractie van `app.css` (271&nbsp;KB) — te bros voor Blazor; compressie + preload is de praktische winst.
- Blazor WASM / lazy `.razor` assemblies — dit is Interactive Server.
