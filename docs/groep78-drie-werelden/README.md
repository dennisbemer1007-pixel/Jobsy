# Groep 7/8: drie werelden (themakeuze)

Productbesluiten voor de visuele reis (**Zee-expeditie**, **Eilandtuin**, **Sterrenkust**) naast de bestaande logische wereldstructuur (koraalrif, schatgrot, Pauze-eiland, vuurtoren, lagune).

## Besluiten (Dennis, ronde #1290)

### 1. Denkspel-tekeningen per wereld

- Elke wereld heeft **eigen aquarel-tekeningen** voor de drie denkspelen (sorteerpuzzel, kaartspel, lichtjes), afgeleid van `docs/mockups/leerling/werelden` (zelfde stijl als Zee-expeditie).
- Assets staan onder `Jobsy.Web/wwwroot/images/leerling/werelden/puzzles/{zee,tuin,ster}/`, worden **lazy** geladen in `LeerlingPuzzel.razor` en zijn **versioned** via `WwwrootAssetCatalog` / `asset-versions.json`.
- **Puzzel-logica en antwoorden blijven identiek** (zelfde stappen, zelfde oplossingen); alleen presentatie (titels + raster-art) verschilt per thema.

### 2. Wisselen van wereld

- Thema wisselen kan **alleen**:
  - **vóór de eerste vraag** (start / wereldkeuze), en
  - op het **Pauze-eiland**.
- Tijdens wereld 1–4 op de reisroute is wisselen geblokkeerd (API `theme_locked`).
- Bij wisselen blijven **antwoorden, puzzelstatus en voortgang** behouden; alleen `VisualThemeKey` verandert.

### 3. Stopnamen per thema

- Generieke namen zoals **Steiger** of **Strandpaal** niet hergebruiken over alle thema's heen.
- Elke wereld heeft **eigen stopnamen** die bij het thema passen (tuin/plant voor Eilandtuin, sterren/kust voor Sterrenkust, zee-expeditie voor Zee-expeditie).
- Copy: **NL en EN** (via `UiStringsScholen`), **geen gedachtestreepjes**, **geen tellers** in stopnamen.

## Technische ankers

| Onderdeel | Locatie |
|-----------|---------|
| Thema-keys + stop/puzzel copy keys | `Jobsy.Core/Scholen/PupilJourneyTheme.cs` |
| Opslag thema | `PupilProgress.VisualThemeKey` |
| Keuze UI | `/leerling/werelden`, `LeerlingWerelden.razor` |
| Wissel-gate | `PupilJourneyTheme.CanChangeVisualTheme`, `PUT /api/pupil/progress/theme` |
| Denkspel UI | `LeerlingPuzzel.razor`, `scholen.css` (`.ll-theme-*`) |

## Testen

- `PupilJourneyThemeTests` (stop keys, wissel-gate)
- `PupilWebLoginTests` / circuit: thema vóór start; geen wissel midden in reis
