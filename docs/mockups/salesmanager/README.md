# Mockups: Lobsy Partner (Salesmanager)

A layout and copy reference for the stack in `docs/prompts/salesmanager/`. **Where a mockup and the spec differ, the spec wins** (the differences are listed in `docs/prompts/salesmanager/00-README.md` §0). The persona (Tom Hendriks, Hendriks Sales & Advies), employers, amounts and dates are **Voorbeelddata**; the "Voorbeelddata" pill is mockup-only. "Nieuw" tags mark features that don't exist in the code today; they are not rendered in the product.

## Screens
| File | Viewport | What it shows |
|---|---|---|
| `sm-d1-dashboard.png` | desktop 1440 | Dashboard: Beschikbaar (hero + Uitbetaling aanvragen), Verdiend dit jaar, In behandeling, Actieve werkgevers; commission per month (12 months ending in September); "Van link naar klant" funnel; Te doen; Beste werkgevers |
| `sm-d2-link-materiaal.png` | desktop 1440 | Mijn link & materiaal: short link + code + share, QR, how a registration is counted (30-day cookie), 6 materials, pitch in 60 seconds, what the employer gets, own commission 25/10/5 |
| `sm-d3-werkgevers-detail.png` | desktop 1440 | Mijn werkgevers list + detail drawer: commission years, timeline, commission per purchase, privacy note (trade name, place, own commission only) |
| `sm-d4-wallet.png` | desktop 1440 | Wallet & uitbetalingen: Beschikbaar / In behandeling / Aangevraagd / Uitbetaald, Mutaties with states (incl. a correction), how payouts work, latest invoices, payout account + btw |
| `sm-d5-uitbetaling-aanvragen.png` | desktop 1440 | Payout request drawer: amount, btw, "Jij ontvangt", account, run date, self-billing invoice preview with "Factuur uitgereikt door afnemer", KOR hint |
| `sm-d6-profiel-gegevens.png` | desktop 1440 | Profiel & gegevens: company, btw vs KOR, payout account with safe change (2FA + mail + 3-day wait), agreements incl. self-billing consent, 2FA + e-mail preferences |
| `sm-m1-dashboard.png` | mobile 390×844 @2x | Dashboard: hero, 2 KPIs, 6 months, Te doen, share card, 5-item bottom nav |
| `sm-m2-wallet.png` | mobile 390×844 @2x | Wallet: hero, "Zo werkt het" strip, tabs, entries with state pills |
| `sm-m3-link-qr.png` | mobile 390×844 @2x | Mijn link: large QR, code, WhatsApp / Mail / Kopieer, materials |

The Ambassadeur role is parked (spec D8, `AmbassadorsEnabled` off); there are no ambassadeur mockups.

## Rebuild
```bash
cd docs/mockups/salesmanager
pip install playwright segno && python3 -m playwright install chromium   # once; segno renders a real QR (optional)
python3 build.py            # writes html/ and re-renders every sm-*.png
python3 build.py d3 m2      # only screens whose name contains a filter
```
- `sm.py` builds every screen (shell, sidebar, bottom nav, cards) with the enterprise UI helpers in `base/ui.py`, `base/ui_css.py` and `base/bmui.py` (copies from the admin/bedrijfsmanager mockups). The logo is in `base/src/`.
- `build.py` renders desktop 1440×900 and mobile 390×844 @2x with `/usr/bin/google-chrome`, prints layout issues (horizontal overflow, too-wide tables, text under 11.5 px), and grows long screens to their scroll height.
- `html/` and `__pycache__/` are build output; don't commit them.

## Decisions reflected (29-09-2026)
- Commission 25/10/5 % over 3 years; 14 days "In behandeling"; payouts from € 50, approved by Lobsy in a monthly run.
- 30-day link cookie; the typed code wins.
- A salesmanager sees trade name, place and own commission only.
- Self-billing with separate consent and "Factuur uitgereikt door afnemer"; 21 % btw or KOR.
- 2FA mandatory; IBAN change with 2FA, a mail and a 3-day wait.
