# Mockups: Intermediair (uitzendbureau) in the werkgever shell

A layout and copy reference for the stack in `docs/prompts/intermediair/`. **Where a mockup and the spec differ, the spec wins** (the differences are listed in `docs/prompts/intermediair/00-README.md` §0). All bureaus, opdrachtgevers, KvK numbers, addresses and numbers are **Voorbeelddata**; the "Voorbeelddata" pill is mockup-only.

There is **no free location field anywhere**: the werklocatie always comes from the opdrachtgever's KvK vestiging (read-only, "Uit KvK"). The only per-vacancy choice is where the pin stands: **onze vestiging** (standaard) or **echte werklocatie**.

## Screens
| File | Viewport | What it shows |
|---|---|---|
| `im-d1-dashboard.png` | desktop 1440 | Dashboard in the /werkgever shell: role chip "Intermediair" + scope "Alle opdrachtgevers ▾", KPIs, Te doen (KvK-adres gewijzigd, vacature ver van je vestiging, opdrachtgever zonder vacature), wervingstrechter, opdrachtgevers table with the "Op de kaart" mode |
| `im-d2-opdrachtgevers.png` | desktop 1440 | Opdrachtgevers list + detail: "Gegevens uit KvK" read-only with lock "Uit KvK", "Opnieuw ophalen", map of onze vestiging vs werklocatie, default map mode for new vacancies, vacancies of this opdrachtgever |
| `im-d3-vacature-opdrachtgever-locatie.png` | desktop 1440 | New vacancy step 1 "Opdrachtgever & locatie": picker (only KvK-added opdrachtgevers), read-only KvK werklocatie, the two radio cards, travel-time note, candidate preview (map + card) |
| `im-d4-kandidaatweergave.png` | desktop 1440 (long) | What the candidate sees, both modes side by side: pin + 15-min ring, job card ("Uitzendbureau" / "via …"), vacancy page rows, visible vs hidden |
| `im-d5-opdrachtgever-toevoegen-kvk.png` | desktop 1440 | Drawer "Opdrachtgever toevoegen": KvK number → choose the KvK vestiging that is the werklocatie (one disabled "Al je opdrachtgever"), declaration checkbox, KvK-failure note (no manual address) |
| `im-m1-dashboard.png` | mobile 390 | Mobile dashboard, bottom nav with Opdrachtgevers |
| `im-m2-vacature-locatie.png` | mobile 390 | Mobile vacancy step 1 with the location choice |

## Rebuild
```bash
cd docs/mockups/intermediair
pip install playwright && python3 -m playwright install chromium   # once
python3 build.py            # writes html/ and re-renders every im-*.png
python3 build.py d2 d5      # only screens whose name contains d2 or d5
CHROME_PATH=/usr/bin/google-chrome python3 build.py   # optional: use a system Chrome
```
- `screens_im.py` holds the screens; `imui.py` is the intermediair shell (role chip, scope chip, nav, `mapsvg()` map helper, example data).
- `bmui.py`, `base/ui.py`, `base/ui_css.py` are copies of the bedrijfsmanager/admin mockup helpers (same visual language as `docs/mockups/werkgever-redesign/`).
- `build.py` prints a layout CHECK per screen (overflow, clipped text, font < 11.5 px); every screen should print `ok`.
