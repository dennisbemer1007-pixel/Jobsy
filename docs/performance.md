# Lobsy website performance

Lab baseline, productie `https://lobsy.nl` (geen CrUX field data):

| Bron | Perf | A11y | BP | SEO | FCP | LCP | TBT | CLS |
|---|---|---|---|---|---|---|---|---|
| PageSpeed Insights (aug 2026, opgave) | 56 | 95 | 77 | 91 | — | — | — | — |
| Lighthouse 12.8 mobile (deze run, pre-fix) | 47 | 95 | 82 | 91 | 2.1s | **21.6s** | 600ms | 0.108 |

LCP was een Carto-tegel (`leaflet-tile`) met **20s load delay**: de netwerkrij stond vol met `lobsy.png` (568&nbsp;KB) en tientallen picsum-JPEG’s (~60–80&nbsp;KB, ~9&nbsp;MB / 584 requests). Leaflet-CSS via unpkg was render-blocking (~780&nbsp;ms).

## Wat er nu in de code zit

### Images

- Mock/seed-foto’s zijn same-origin SVG’s (`/images/vacancies/{branche}-{0\|1}.svg`, ~0.6&nbsp;KB). Bestaande picsum/Unsplash-URL’s worden bij render herschreven en bij de volgende media-backfill in de database gezet.
- Job cards gebruiken een echt `<img>` (`VacancyPhoto`) met `loading="lazy"`, `decoding="async"`, intrinsieke 600×400 en `sizes`. De eerste twee kaarten zijn eager.
- Logo: WebP 64/128/256 i.p.v. 1024×1024 PNG (568&nbsp;KB). Watermarks lazy, apple-touch-icon 180&nbsp;px.
- Optioneel Cloudflare Image Resizing: zet `Cloudflare__ImageResizing=true` (betaalde CF-add-on). Alleen same-origin paden (`/images/…`) worden gewrapt — geen absolute http(s)-URL’s (geen CF-fetch/SSRF-proxy).

### JavaScript / critical path

- Leaflet + MarkerCluster staan lokaal in `wwwroot/lib/leaflet/` en laden pas als een kaartpagina `jobsyMaps.ensure()` aanroept.
- First-party JS is gebundeld in `js/app-core.js` (geo, culture, session, cookies, download, richtext, maps-loader).
- `app.css` wordt gepreload. Geen webfonts (systeemstack).

### Server / edge

- Response compression (Brotli/Gzip) voor HTML/CSS/JS/SVG.
- `Cache-Control: public, max-age=604800` op statische assets (versie-querystrings blijven de cache-bust).
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
