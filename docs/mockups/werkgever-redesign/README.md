# Werkgever redesign mockups (bedrijfsmanager · regiomanager · vestigingsmanager)

Layout and copy reference for `docs/prompts/werkgever-redesign/`. All data is **Voorbeelddata** (fictional). Where a mockup and the spec differ, the spec wins (see `00-README.md` §0 "Known mockup differences").

One shared page set: the same pages render per role (scope chip, role-scoped nav, read-only variant).

| File | Screen |
|---|---|
| `bm-d1-dashboard.png` | Bedrijfsmanager dashboard: 5 KPIs, Te doen, wervingstrechter, vestigingen table |
| `bm-d2-vacatures.png` | Vacatures: status tabs, filters (vestiging/regio/type/geplaatst door), approval rows, bulk bar with token cost |
| `bm-d3-sollicitaties-pipeline.png` | Sollicitaties pipeline per vacature + candidate drawer (privacy stages) |
| `bm-d4-vestigingen-team.png` | Vestigingen & regio's tree + team table + one "Iemand uitnodigen" drawer |
| `bm-d5-tokens-facturen.png` | Tokens & facturen: balance KPIs, verbruik per vestiging, tokens kopen, recente facturen |
| `bm-d6-kandidaatinzichten.png` | Kandidaatinzichten free version: free KPIs, locked (blurred) premium parts, gold unlock block |
| `bm-d7-dashboard-regiomanager.png` | Same dashboard as regiomanager: region chip, "Alleen lezen", Signalen, disabled action + tooltip |
| `bm-d8-vacatures-vestigingsmanager.png` | Same Vacatures page as vestigingsmanager: fixed branch chip, own balance, request waiting for BM |
| `bm-m1-dashboard.png` | Mobile dashboard (390×844) with bottom nav |
| `bm-m2-sollicitatie.png` | Mobile new (anonymous) application with sticky Afwijzen / Accepteren |
| `bm-m3-kandidaatinzichten-gratis.png` | Mobile Kandidaatinzichten free version with locked parts (390×1030) |

Rebuild: `python3 build.py [filter]` (Python 3 + Playwright, Chrome at `/usr/bin/google-chrome`, Inter font). Sources: `bmui.py` (role configs, nav per role, shared CSS incl. the premium/lock pattern), `screens_dash.py` (dashboard + vacatures, rendered per role), `screens_more.py`, `screens_insights.py`; `base/` is a copy of the admin-redesign `ui.py` / `ui_css.py` (tokens from `app.css`, icons).
