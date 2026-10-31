# Leerlingreis groep 7/8 · drie werelden

Goedgekeurd door Dennis op 27-10-2026.

**Zelfde vragen en spellen, alleen een andere wereld.**
Alle drie de werelden hebben dezelfde 60 vragen in dezelfde volgorde, dezelfde denkspellen met dezelfde regels (Schelpenrij, Schatkaart en Vuurtorenlampen na vraag 15, 30 en 45), hetzelfde Pauze-eiland en dezelfde uitkomst. Alleen de visuele laag verschilt. Het kind kiest zelf een wereld met één tik. Wisselen kan later altijd en de antwoorden blijven bewaard.

## De werelden
- **Zee-expeditie**: de bestaande wereld, stoer en avontuurlijk. Van de haven langs Koraalrif, Schatgrot en Vuurtoren naar de Lagune.
- **Eilandtuin**: warm en kleurrijk, met bloemen, koraal, warme baaien en een waterval. Pruimbruin en terracotta, niet kinderachtig. Stops: Steiger, Bloemenbaai, Koraaltuin, Pauze-eiland, Waterval, Zonnelagune.
- **Sterrenkust**: rustig en neutraal, een kust in de nacht met vuurtoren en maan. De route is een sterrenbeeld en elke ster is een stop. Stops: Strandpaal, Duinpad, Maangrot, Pauze-eiland, Vuurtoren, Sterrenbaai.

De kreeft is in alle werelden dezelfde gids (bestaande poses).

## Schermen (reisvolgorde)
00 Kies jouw wereld · 01-03 Zee-expeditie (route, vraag, puzzel-intro) · **11a/11b Pauze-eiland** (wat vind ik leuk / niet leuk, per wereld) · 04-06 Eilandtuin · 07-09 Sterrenkust · 10 Dit ben jij · **12 Beroep kiezen** · **13 Een dag als …** (detail na tik op kaart; voorbeeld: dierenverzorger)

Pauze-eiland is één stop op de reis, maar twee schermen: **11a** hobby-chips (LikeChips) en **11b** niet-leuk-chips (DislikeChips, inclusief school-extra’s en “Iets anders”). Zelfde inhoud in alle werelden; alleen thema en achtergrond verschillen.

## Bestanden
- `walkthrough-desktop.html` en `walkthrough-mobiel.html`: klikbaar, ook met de pijltjestoetsen
- `groep78-werelden-desktop.pdf` en `groep78-werelden-mobiel.pdf`: voorblad en 13 schermen (inclusief uitkomst per wereld)
- `vergelijking-vraagscherm.png`: dezelfde vraag in alle drie de werelden
- `renders/desktop` (1440) en `renders/mobile` (390)
- `illustraties/`: nieuwe aquarellen voor Eilandtuin en Sterrenkust
- Bron: `gen.py` (bouwt `src/`), `crops.py`, `meta.py`, `build_extra.py`, `render.js` en `pdf.js` (playwright-core met Chrome)

## Scherm 13 · Een dag als … (product-bron)
In de app staat dezelfde inhoud op **`OccupationDayPage.razor`** (`/candidate/beroep/{EscoId}/dag`), gelezen via **`OccupationDayInLifeReader`** en getoond als **`OccupationDayBlock`**-timeline (labels met klok). UI-teksten komen uit **`UiStringsOccupationDay`** (`Day.Title`, `Day.Disclaimer`, `Day.Skills`, …). Links: **`OccupationDayLink.razor`** (kompas, functiefit, paspoort). Feature flag: **`OccupationDayInLifeEnabled`**.

Mockup-data: `data/occupation-day-dierenverzorger.json` (ESCO `1ee1b792-4d5f-4555-be4e-ce7e4c05d1a9`). Vernieuwen: `dotnet run --project tools/day_export -- dierenverzorger`. Als `data/occupation-day-in-life.nl.json` (admin-export) aanwezig is, gebruikt het tool de opgeslagen bloktekst.

## Open punten
1. **Deel 1 van 4**: in de reisbalk stond eerst "wereld 1 van 4". Nu heten de thema's werelden, dus de vier blokken van 15 vragen heten nu "deel 1 van 4".
2. **Eigen stopnamen per wereld**: Pauze-eiland en de namen van de spellen zijn overal gelijk. De andere stops hebben per wereld een eigen naam.
3. **Schatkaart en Vuurtorenlampen** bestaan nu alleen in de zee-stijl. De versies voor Eilandtuin en Sterrenkust moeten nog worden ontworpen.
4. **Geen wisselscherm vanuit Pauze**: wisselen staat nu alleen op het keuzescherm en als link op de routepagina.
