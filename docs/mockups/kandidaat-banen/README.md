# Kandidaat banen: new look & feel (mockups, approved by Dennis 30-09)

Scope: banenkaart, lijst, vacature-detail, sollicitaties, bewaard and Match for the candidate role.
Everything in these screens is **Voorbeelddata**. The mockups are not a spec.

Baseline reviewed: `origin/acceptatie` a611db40 (29-09 18:29 CEST), plus live acceptatie on the same commit.
Screenshots of the current app: `../../review/kandidaat-banen-2026-09-30/`.

## Build
`python3 basemap.py` (once) then `python3 build.py [filter]`.

- The map uses the real OpenFreeMap "liberty" style, the same style the app uses (`jobsyMapLibre.js`). It is shown muted, so the rings and pins stand out.
- The travel-time rings are **real Valhalla bike isochrones** (10/20/30 min) from Herenstraat 20, Wateringen, stored in `src/iso-fiets-wateringen.json`.
- The photos are the app's own fallback WebP images (#374).

## Screens
| File | What it shows |
|---|---|
| kd-d1-banenkaart | Filter bar with a travel-time chip, strong rings (halo + line + soft graduated fill, dashed 30 min, pill labels), side list with "82% past bij jou" + a why line, the selected vacancy docked bottom-left, the top-match tile, a Lobsy tip, and a legend "echte wegen, geen cirkel". Clusters outside the rings are muted. |
| kd-d2-lijst | Full-width list mode: fit + why, big travel time, a dislike row ranked lower with a reason, and a rail with a mini ring map and "zo sorteren we" |
| kd-d3-vacature | Fit panel with 4 DNA bars (Cultuur/Waarden/Competenties/Interesses) + "Nieuw voor jou". Employer kernwaarden, branche, and betrokkenheid tiles "door werkgever opgegeven". Travel card with mini isochrone + transport switch. One primary: Solliciteer. |
| kd-d4-sollicitaties | Dated 4-step timeline per application, "Wat nu?" per status, a kind rejection with similar jobs, and a Lobsy tip |
| kd-d5-bewaard | Status per saved job (open / sluit over x dagen / vergeven / gesloten), fit, travel, bewaard-datum, remove, and apply or "zoek banen die hierop lijken" |
| kd-d6-match | match-desktop variant B: swipe dialog over the map with "Waarom jij past" per DNA dimension, the "Hierna" column, and keyboard hints |
| kd-m1-kaart | Mobile map with a bottom sheet (peek), chips, and the top-match tile |
| kd-m2-lijst | Mobile list with a floating "Kaart" button |
| kd-m3-vacature | Intermediary in hidden mode: "via uitzendbureau FlexPlus", map pin on the bureau's office, sticky apply bar |
| kd-m4-sollicitaties | Vertical timeline for the active application, compact bars for the rest |
| kd-m5-match | Mobile swipe card with the DNA reasons |

Nav follows Dennis's latest order (add-on after Mijn Paspoort 08, Werkgevers ON): De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties (5 items, DS max). Bewaard is NOT a nav item: it is a tab inside Sollicitaties (`CandidateJobListTabs`, paspoort D8), shown on d4/d5/m4.

Spec: `docs/prompts/kandidaat-banen/00-README.md`. Where the mockups and the spec differ, the spec wins (README §0 lists the known differences).
