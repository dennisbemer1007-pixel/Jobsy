# Mockups: Carrière (`/carriere`), Contactverzoeken, Hoe werkt Lobsy

Layout and copy reference for `docs/prompts/carriere/`. All names, companies, courses, dates and counts are **Voorbeelddata**; the "Voorbeelddata" pill is mockup-only. Desktop 1440×900, mobile 390×844 (@2x). Rendered with `prefers-reduced-motion: reduce`, so every PNG shows the complete still state.

Style = "De ontdekkingsreis" (`docs/mockups/ontdekkingsreis/` on `docs/mijn-paspoort`): the same tokens, sea scene tokens (`--sea-0…9`, `--sky`, `--sand`, `--sun`, `--rock`, `--weed`, `--shell-old`), lobster + shell plates, rail, eyebrow + h1, bubble.
Career metaphor: the lobster **climbs stone by stone from the deep to the light**. The dream job is the golden stone. On every stone it passed, its old shell stays behind; per completed step it grows and gets a gold new shell.

| File | Screen |
|---|---|
| `cr-d1-geen-droombaan.png` / `cr-m1-…` | No dream job yet: lobster with antennas, 3 suggestions from the paspoort, search in the job list |
| `cr-d2-reis-overzicht.png` / `cr-m2-…` | Journey overview: rail (In de diepte → De klim → Naar het licht), growing-shells stepper, "Nu aan de beurt", "Wat je al hebt", climb scene |
| `cr-d3-stap-detail-opleidingen.png` / `cr-m3-…` | Step detail: claws (missing / "heb je al"), match as band text, courses Gratis first + Partnerlink + disclosure (mobile: courses first) |
| `cr-d4-stap-klaar.png` / `cr-m4-…` | Step completed: old shell falls once, gold new shell, what was added, next stone, "Toch nog niet klaar" |
| `cr-d5-droombaan-wijzigen.png` / `cr-m5-…` | Change dream job in `LobsyFriendlyDialog` (no `window.confirm`): what you achieved stays, old plan restorable for 30 days |
| `cr-d6-contactverzoeken.png` / `cr-m6-…` | `/candidate/talent-contacts`: "Zo werkt het", open / accepted / declined requests, lobster listening with antennas |
| `cr-d7-contact-delen-bevestigen.png` | Confirm before sharing: the exact name, e-mail and phone the employer will get |
| `cr-d8-hoe-werkt-lobsy.png` / `cr-m7-…` | `/candidate/hoe-werkt-lobsy`: five stones in Dennis' nav order, progress per stone, "Veilig en rustig" |

**Nav in the mockups** shows Dennis' order (De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties). That order is a **separate add-on**; this stack does not change the nav.

Build: `python3 build.py` (Playwright + `/usr/bin/google-chrome`). `base/ontdekkingsreis_build.py` + `base/src/` make it self-contained.
