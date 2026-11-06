# Kandidatenkaart (omgekeerde banenkaart) — static mockups

Design-only HTML/CSS for the paid employer feature **Kandidatenkaart**: aggregated market insight showing where **opt-in, vindbare** Lobsy candidates match a vacancy (or profession + requirements). No individual points or profiles.

## Product rules (reflected in UI copy)

- Areas appear only with **≥ 30** matching candidates; smaller counts are merged or hidden.
- Per area: **count + match-score bands** only (e.g. `42 kandidaten · 12 boven 80%`).
- **Contact**: anonymous **uitnodiging om te solliciteren** per area; candidate decides.
- **Paid**: calm locked preview vs full access; pricing line **Prijs en pakket op aanvraag**.
- **Alleen actieve kandidaten**: visible in preview upsell and full UI (2-day reminder flow, drop from map until re-available). Per area: qualitative **Actief deze week** / **Laatst bijgewerkt: actief deze week** (no invented stats).

## Variants

| Variant | Layout | Best for |
|--------|--------|----------|
| **A** | Map-first: full-width `map-pane` with filters in top chrome + **right insight panel** (desktop) / bottom sheet (mobile) | Employers who think geographically first (mirrors banenkaart discovery). |
| **B** | **Split list + map**: scrollable area list beside map (desktop) / list-first with map FAB (mobile) | Scanning multiple regions and comparing counts without losing map context. |

## Files

- `shared/tokens.css`, `shared/kk.css` — Lobsy tokens + werkgever shell + banenkaart-style map chrome.
- `pages/a-*.html`, `pages/b-*.html` — states: `preview`, `full`, `area`, `invite`, `filters-open`, `area-sparse`.
- **Filters:** matchscore, reisafstand, startdatum, uren/week; desktop **Meer filters** popover; mobile **Filters** bottom sheet.
- `scripts/generate-pages.mjs` — regenerates HTML from shared fragments (optional).
- `scripts/capture.mjs` — Playwright PNGs at 1440×900 desktop and 390×844 mobile @2x.
- `overview.html` — contact sheet of all captures.

## Kandidaat-kant (uitnodiging & beschikbaarheid)

| Mockup | Bestand | Regels in copy |
|--------|---------|----------------|
| Herinnering in-app | `pages/kandidaat/reminder-notificatie.html` | Na 2 dagen zonder reactie op uitnodiging; warm, niet strafend |
| Herinnering e-mail | `pages/kandidaat/reminder-email.html` | Zelfde boodschap + link naar uitnodiging |
| Weer beschikbaar | `pages/kandidaat/weer-beschikbaar-popup.html` | Direct na login na automatisch **niet beschikbaar**; primair “Ja, ik ben weer vindbaar”, secundair “Later” |
| Activiteitscheck e-mail | `pages/kandidaat/inactivity-email.html` | Na **1 maand** geen login: “Ben je nog op zoek?”; primair “Ja, ik blijf beschikbaar”, secundair “Ik zoek even niet”; **1 week** om te reageren |
| Activiteitscheck in-app | `pages/kandidaat/inactivity-notificatie.html` | Zelfde check bij volgende login (naast e-mail) |

**Product:** geen reactie na uitnodigingsherinnering → niet beschikbaar. **Afwijzen** van een uitnodiging telt wél als reactie (geen consequenties). Geen reactie op activiteitscheck binnen 1 week → niet beschikbaar; volgende login = weer-beschikbaar-popup. Werkgever-copy in **Alleen actieve kandidaten** noemt beide regels.

PNG-prefix: `k-reminder-notificatie-*`, `k-reminder-email-*`, `k-weer-beschikbaar-popup-*`.

## Regenerate screenshots

```bash
cd docs/mockups/kandidatenkaart
node scripts/generate-pages.mjs
node scripts/generate-candidate-pages.mjs
node scripts/capture.mjs
```

Requires Playwright Chromium (`npx playwright install chromium` once).
