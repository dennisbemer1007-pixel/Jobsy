# De ontdekkingsreis mockups (Voorbeelddata)

This folder holds the layout and copy reference for `docs/prompts/mijn-paspoort/06–08`. All names, answers, counts and employers in it are **Voorbeelddata**.

| File | Viewport | Screen |
|---|---|---|
| or-d1-start.png | 1440×900 | Start on the beach: the 3 zones, "Begin de reis", the route rail |
| or-d2-over-jou.png | 1440×900 | Step 1 Over jou (aan de kust) |
| or-d3-werk.png | 1440×900 | Step 3 Werk: employers + "Wat voor werkgever zoek ik?" (tussen de rotsen) |
| or-d4-waar-houd-ik-niet-van.png | 1440×900 | Step 6 Waar houd ik niet van: skippable, private, calm spot between stones |
| or-d5-test-vraag.png | 1440×900 | Step 7 Competenties: question 4 of 5 (in de diepte) |
| or-d6-laag-eraf.png | 1440×900 | "Weer een laag eraf" moment + choice to go deeper (5 / 10 / 25) |
| or-d7-einde-paspoort.png | 1440×900 | End "Naar het licht": first impression + "Bekijk je paspoort" |
| or-m1-start.png | 390×844 | Mobile start |
| or-m2-wanneer-en-hoe.png | 390×844 | Mobile step 2 Wanneer en hoe |
| or-m3-test-vraag.png | 390×844 | Mobile step 10 Waarden question |
| or-m4-einde.png | 390×844 | Mobile end |

`build.py` is the generator. It holds the lobster shell-plate SVG paths (`PLATES`, `BODY`, `CRACKS`, `SHARD`), the scene per depth (`scene()`, `GRAD`), the step copy (`STEPS`, `SAY`) and the derived sea tokens (`--sea-0…9`, `--sky`, `--sand`, `--sun`, `--rock`, `--weed`, `--shell-old`, all `color-mix()` of existing tokens).

**The mockup nav shows 6 items including Bewaard. Ignore that and follow §N / D8 (max 5).**
