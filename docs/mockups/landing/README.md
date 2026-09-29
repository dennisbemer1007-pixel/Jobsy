# Mockups: Lobsy landing page, gratis test and sign-up (with and without werkgevers)

A layout and copy reference for the stack in `docs/prompts/landing/`. **Where a mockup and the spec differ, the spec wins** (the differences are listed in `docs/prompts/landing/00-README.md` §0). Vacancies, companies, travel times, percentages, match scores and the passport are **Voorbeelddata**; the "Voorbeelddata" pill is mockup-only (the landing keeps a small "Voorbeeld" label on product illustrations).

Variants: `lp-*` = switch "Werkgevers actief" **ON**; `lp-*-zw` = **OFF** (no jobs, vacancies, employers, map or Match).

## Screens
| File | Viewport | What it shows |
|---|---|---|
| `lp-d1-landing.png` | desktop 1440 (full page, 5844 high) | Landing ON: header, hero "Soms moet je uit je schild groeien." with the map-disc illustration and mascot, Wat is Lobsy (3 steps), De kreeft-visie (story + 6 mini scenes), Wat je krijgt (banenkaart, paspoort, ontdekkingsreis, Match, rapport), Voor wie (kandidaat, werkgever, school, partner), Privacy, FAQ, closing CTA, footer |
| `lp-d1-landing-zw.png` | desktop 1440 (full page, 5707 high) | Landing OFF: passport hero with "je oude schild", "Past dit beroep bij mij?" and Ontdekkingsreis cards, OFF steps/tiles/audiences (Jij · Nieuw in Nederland · Even vastgelopen · School), OFF privacy/FAQ/footer |
| `lp-d2-test-start.png` | desktop 1440×900 | Gratis test start: 4 blocks, 16+ and privacy checks, "Start de test", mascot "Duik maar diep!" |
| `lp-d3-test-vraag.png` | desktop 1440×900 | Question 8 of 20: block rail, Likert 1–5 with faces, "Later verder", privacy line |
| `lp-d4-test-resultaat.png` | desktop 1440 | Result ON: "Dit ben jij 🎉", 4 tiles, locked passport teaser, share / wipe, sign-up card (Google, Microsoft, e-mail) with the ON unlock list |
| `lp-d4-test-resultaat-zw.png` | desktop 1440 | Result OFF: the same, with the OFF unlock list (paspoort, "Past dit beroep bij mij?", droombaan-check, Ontdekkingsreis) |
| `lp-m1-hero.png` | mobile 390 (full page, dpr 2) | Landing ON on mobile, all sections |
| `lp-m1-hero-zw.png` | mobile 390 (full page, dpr 2) | Landing OFF on mobile. **The passport-card chips were clipped in an earlier version; this render is the fixed one** (card 230 px, wheel without centre text, chips inside the card) |
| `lp-m2-test-vraag.png` | mobile 390×844 | Question screen on mobile (56 px answer buttons) |
| `lp-m3-resultaat.png` | mobile 390 | Result ON on mobile: tiles, inline sign-up card, sticky "Bewaar je DNA" bar with "Later" |
| `lp-m3-resultaat-zw.png` | mobile 390 | Result OFF on mobile |

## Notes for the implementation (details in the spec)
- **Switch "Werkgevers actief"** (`docs/mijn-paspoort` 01): "/" renders the variant on the server (no flash). This **amends** that file: anonymous "/" no longer redirects to `/ontdek` when OFF, and the home canonical stays "/". The map moves to `/banenkaart` (`/banen` → 301); OFF, `/banenkaart` → 302 "/". The sitemap, JSON-LD and cache follow the flag (README §S).
- **Performance:** the landing loads no MapLibre/jobMap JS, no tiles, no pin API calls and no Blazor runtime. The hero map disc and all scenes are inline SVG/CSS. Budgets: LCP < 2.5 s p75 mobile, TTI < 3.5 s (Lighthouse CI in file 10).
- **Warm public theme** (`.pub-theme`, public pages only). Approved deviations from `design-system.mdc`, inside that theme only: warm `color-mix` tints of existing tokens and coral used several times per screen · radii 24/36 px and pill buttons · larger public type sizes and 700 on h2 · emoji as icons (aria-hidden, next to text) · blobs, waves, scenes and one extra soft shadow · Likert faces and the blurred locked teaser. The mockup CSS uses a few `!important`s; the implementation must not.
- **Mascot:** every pose here is the single current mascot plus CSS transforms. The required art (poses waving/diving/sitting/celebrating, SVG or WebP+AVIF 64–512 px, file names, placement) is defined in `docs/prompts/landing/02-mascotte-assets.md`.

## Rebuild
```bash
cd docs/mockups/landing
pip install playwright && python3 -m playwright install chromium   # once
python3 build.py              # writes html/ and re-renders every lp-*.png
python3 build.py zw           # only the screens whose name contains "zw"
```
- Uses `/usr/bin/google-chrome` when present (or `CHROME_PATH`), else Playwright's Chromium. For identical renders install the **Inter** and **Noto Color Emoji** fonts.
- `build.py` prints per screen the page height and layout issues: horizontal overflow, clipped text, text under 11.5 px, and **chips outside the passport card** (the check added for the clipped-chips fix). Every screen should print `ok`.
- Sources:
  - `ui.py`: page shell, icons, embedded images from `src/`
  - `css_a…d.py`, `css_zw.py`, `css_warm.py`: the CSS layers, combined in `css_lp.py`; `css_warm.py` is the final warm style
  - `w_common.py`: blobs, waves, the mascot helper, the 6 mini scenes
  - `w_hero.py`: the hero scenes, desktop and mobile
  - `w_land.py`, `w_land2.py`: the landing sections
  - `w_test.py`: the test screens and the sign-up card
  - `zw_scene.py`: the passport card and wheel
  - `lp_map.py`: the static map illustration
