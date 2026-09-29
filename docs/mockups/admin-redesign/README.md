# Admin redesign mockups (Beheer)

These are the layout and copy reference for `docs/prompts/admin-redesign/`. All data is **Voorbeelddata** (fictional). Where a mockup and the spec differ, the spec wins (see `00-README.md` §0).

| File | Screen |
|---|---|
| `ad-d1-dashboard.png` | Dashboard: KPI cards, Te doen, Systeemstatus, Platform-modus, Recente beheeracties |
| `ad-d2-gebruikers-2fa.png` | Alle gebruikers + detail drawer (Beveiliging: 2FA resetten, sessies, gemaskeerde gegevens) |
| `ad-d3-organisaties.png` | Bedrijven & vestigingen: tree + detail panel with takeover note |
| `ad-d4-platforminstellingen.png` | Functies: grouped switches, impact notes, change history, save bar (1440×1330) |
| `ad-d5-beveiliging-audit.png` | Auditlog + support access / privacy / admins cards |
| `ad-d6-financien.png` | Omzet & transacties: KPIs, Mollie transactions, open payouts, btw |
| `ad-m1-dashboard.png` | Mobile dashboard (390×844) |
| `ad-m2-2fa-resetten.png` | Mobile 2FA reset confirmation sheet (390×844) |

Rebuild: `python3 build.py` (Python 3 + Playwright, Chrome at `/usr/bin/google-chrome`, Inter font). Sources: `ui.py` (icons, sidebar IA, helpers), `ui_css.py` (tokens from `app.css`), `screens_a/b/c.py`.
