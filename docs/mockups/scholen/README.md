# Mockups: Lobsy voor scholen (Schoolbeheerder · Leraar · Leerling)

A layout and copy reference for the stack in `docs/prompts/scholen/`. **Where a mockup and the spec differ, the spec wins** (the differences are listed in `docs/prompts/scholen/00-README.md` §0). All schools, classes, codes, teachers and numbers are **Voorbeelddata**; the "Voorbeelddata" pill is mockup-only.

## Screens
| File | Viewport | What it shows |
|---|---|---|
| `sc-s1-school-dashboard.png` | desktop 1440 | School dashboard: KPIs, tests per class with completion %, Te doen, "Interesses hele school" (k ≥ 5) |
| `sc-s2-klassen-codes.png` | desktop 1440 | Klassen & codes: new-class drawer, 28 codes, code list with an empty "Naam (vul zelf in)" column, "Lobsy bewaart geen namen", CSV / Codelijst printen, ouders-bevestiging |
| `sc-s3-leraren-2fa.png` | desktop 1440 | Leraren with 2FA status. Invite drawer: own classes only, no role choice, "Tweestapsverificatie · Verplicht" |
| `sc-s4-resultaten-per-code.png` | desktop 1440 | Resultaten per code per class, "Per code: aan" pill + admin-setting note |
| `sc-t1-leraar-klasoverzicht.png` | desktop 1440 | Leraar Klasoverzicht: codes with status/progress, interests, drives, dream jobs |
| `sc-t2-leraar-code-detail.png` | desktop 1440 | Leraar detail per code: story, 4 tiles, likes/dislikes, conversation starters, droombaan route, PDF |
| `sc-p1-leerling-inloggen.png` | mobile 390×844 | Pupil login: school + class dropdown + code |
| `sc-p2-leerling-vraag-info.png` | mobile 390×844 | Question 23/60 with the (i) "Stel je voor…" bubble, 5 answer buttons, puzzle strip |
| `sc-p3-leerling-hobbys-niet-leuk.png` | mobile 390 (long) | Pauze-eiland: leuk / niet leuk chips + the name warning |
| `sc-p4-leerling-dit-ben-jij.png` | mobile 390 (long) | "Dit ben jij" story result |
| `sc-p5-leerling-droombaan-checker.png` | mobile 390 (long) | Droombaan-checker (dierenarts): 3 van 5, needs, route, encouragement, PDF (no AI, no links, no vacancies) |
| `sc-p6-leerling-pdf.png` | A4 794×1123 | One-page pupil PDF |
| `sc-p7-chromebook-vraag.png` | Chromebook 1366×768 | Question screen: rail + card + lobster |

`vragen-voorbeelden.md`: tone reference for the 60-item question bank (B1/A2, "Stel je voor…" examples, Lobsy encouragements).

## Rebuild
```bash
cd docs/mockups/scholen
pip install playwright && python3 -m playwright install chromium   # once
python3 build.py            # writes html/ and re-renders every sc-*.png
```
- `portal.py` builds the school/leraar screens with the enterprise UI helpers in `base/ui.py`, `base/ui_css.py` and `base/bmui.py` (copies from the admin/bedrijfsmanager mockups).
- `pupil.py` builds the pupil screens and reuses the ontdekkingsreis scene/lobster from `base/ontdekkingsreis_build.py`. The assets are in `base/src/`.
- `build.py` renders desktop 1440×900, mobile 390×844 @2x, Chromebook 1366×768 and A4 794×1123. Long screens grow to their scroll height so fixed footers stay at the bottom.
- `html/` and `__pycache__/` are build output; don't commit them.

## Decisions reflected (29-09-2026)
- No pupil names anywhere: codes only, the school keeps the name list.
- No decaan: a leraar sees only the classes assigned to them.
- Mandatory 2FA for schoolbeheerder and leraar.
- Per-code results for the school sit behind an admin setting (default on).
- Pupil fit checker without AI, partner links or vacancies.
- 60 questions in one lesson, with progress saved.
