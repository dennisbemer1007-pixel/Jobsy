# 06. Droombaan-checker: "Nu: …" from the class, 10 mbo occupations, "docent of mentor" for VO

Read `00-README.md` first (D3, K3, K4). Branch `cursor/vragensets-6` from `cursor/vragensets-5`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-6`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.
> - **Droombaan rules (old spec 06):** no AI, no links, no vacancies, no employers, fixed templates.

| | |
|---|---|
| Branch | `cursor/vragensets-6` |
| PR title | `feat(scholen): droombaan — current year from the class, 10 mbo occupations, VO "docent of mentor"` |
| PR body starts with | `Stacked on #<PR 05> (cursor/vragensets-5)` |
| Mark | **"Needs content review by Dennis"** (10 new routes + needs, the VO variants) |

## 06.1 Today (verify)
- **`Jobsy.Core/Scholen/PupilVerhaalCopy.cs`:** all **48** `LeerlingDroom.Route.{job}.1` values start with `"Nu: klas 2|…"`. Check with `git grep -c "Nu: klas 2" -- Jobsy.Core/Scholen/PupilVerhaalCopy.cs` (expect 48).
- **`LeerlingDroom.UndecidedHint`** = "Praat erover met je leraar." Other pupil lines also say "leraar":
  - `LeerlingStory.Privacy`
  - `LeerlingDroom.Cheer.Low`, `.Mid`
  - `LeerlingPdf.Footer`
  - `LeerlingDroom.Need.Ruimte.Next`
- **`PupilStoryRenderer.RenderDreamRoute(PupilResult, PupilProgress?)`** has no class context. It's called from:
  - `PupilPortalService` ~L584/L654/L694
  - `TeacherPortalService` ~L330/L388
  - `PupilReportPdfService` (via the DTO)
- **`DreamJobCatalog`** (`Jobsy.Core/Rules/DreamJobCatalog.cs`, 48 jobs) is **shared with the candidate onboarding** (`DreamJobStep.razor`) (K3).
  - Every job needs an icon in `Jobsy.Web/Navigation/DreamJobIcons.cs` (`DreamJobCatalogTests` checks it).
  - `PupilDreamJobRoutes.All` needs exactly one route per catalog key (`PupilStoryAndDreamJobTests`, which also asserts `48`).

## 06.2 "Nu: {nu}" from the class
- **Copy:** replace `"Nu: klas 2|"` with `"Nu: {nu}|"` in all 48 `Route.*.1` values. Leave the detail part after `|` untouched (K4).
- **New `PupilClassContext(SchoolLevel Level, int Year, PupilQuestionSet Set)`** (Core record). Change the renderer to `RenderDreamRoute(PupilResult, PupilProgress?, PupilClassContext)` and update every caller; all of them already load the class.
- **`{nu}` resolves to** `SchoolLevelRules.YearLabel` in lower case, plus the level where it helps:
  - `Groep78`: "groep 7" / "groep 8"
  - `VmboB/VmboK/VmboGt`: "klas {Year} vmbo"
  - `Mavo`: "klas {Year} mavo"
  - `Havo`: "klas {Year} havo"
  - `Vwo`: "klas {Year} vwo"
  - `Mix`/`Anders`: "klas {Year}"

  Put this in one pure helper, `PupilDreamJobRoutes.NowLabel(PupilClassContext)`.
- **The PDF** (`PupilReportPdfService`) shows the same resolved text. Never print the literal `{nu}`.
- **Guard tests:**
  - no `LeerlingDroom.Route.*` value contains `klas 2`
  - every `Route.*.1` starts with `Nu: {nu}|`
  - rendering any route for any `PupilClassContext` never leaves a `{` in the output

## 06.3 Ten mbo occupations (catalog + routes + needs + icons)
Add them at the end of `DreamJobCatalog.All`, in this order. Keys are stable kebab-case; synonyms help the search.

| Key | TitleNl | IconKey (Lucide) | Synonyms |
|---|---|---|---|
| `logistiek-medewerker` | Logistiek medewerker | `truck` | magazijn, logistiek, heftruck |
| `verkoper` | Verkoper | `shopping-bag` | winkelmedewerker, verkoop, winkel |
| `beveiliger` | Beveiliger | `shield-check` | beveiliging, security |
| `installateur` | Installateur | `cable` | installatietechniek, zonnepanelen, monteur |
| `verzorgende-ig` | Verzorgende IG | `hand-heart` | verzorgende, zorg, thuiszorg |
| `schilder` | Schilder | `paint-roller` | schilderwerk, verven |
| `loodgieter` | Loodgieter | `droplets` | sanitair, leidingen |
| `doktersassistent` | Doktersassistent | `clipboard-plus` | huisartsassistent, praktijk |
| `ict-medewerker` | ICT-medewerker | `monitor-cog` | ict, systeembeheer, helpdesk |
| `horecamedewerker` | Horecamedewerker | `utensils` | horeca, bediening, restaurant |

- **Icons:** add the 10 paths to `DreamJobIcons` (Lucide, ISC, same `SvgStart`/`SvgEnd` pattern) and to its `TryGet` map.
- **Routes** (`PupilDreamJobRoutes.BuildAll`): one `R(...)` per job. Use 5 needs from the existing `PupilNeedSignal` values (no new signals) and **3 steps** + Goal, following the existing pattern. The route goes **via mbo**: "Nu: {nu}" → "Mbo … (niveau 2–4)" → "Stage / bbl" → Goal. Proposed needs:

  | Job | Needs (signal) |
  |---|---|
  | logistiek-medewerker | RiasecR, RiasecC, CompetenceResultaat, CompetenceSamenwerken, ValueStability |
  | verkoper | RiasecE, RiasecS, CompetenceExtraversie, CompetenceSamenwerken, ValueConnection |
  | beveiliger | CompetenceStress, RiasecR, CompetenceResultaat, ValueStability, ValueImpact |
  | installateur | RiasecR, ChipTechniek, CompetenceResultaat, RiasecI, ValueAutonomy |
  | verzorgende-ig | RiasecS, CompetenceSamenwerken, CompetenceStress, ValueConnection, ValueImpact |
  | schilder | RiasecR, RiasecA, CompetenceResultaat, ValueAutonomy, ChipBouwen |
  | loodgieter | RiasecR, ChipTechniek, CompetenceStress, ValueAutonomy, RiasecI |
  | doktersassistent | RiasecS, RiasecC, CompetenceResultaat, CompetenceStress, ValueConnection |
  | ict-medewerker | RiasecI, ChipComputers, RiasecC, CompetenceInnovatie, ValueAutonomy |
  | horecamedewerker | RiasecS, CompetenceExtraversie, CompetenceStress, CompetenceSamenwerken, ChipKoken |

  Reuse existing `LeerlingDroom.Need.*` sentence keys where they fit. A new need key gets both a `…` and a `….Next` line (B1, ≤ 10 words, no work words that the G78 wordlist forbids, e.g. no "klant"/"collega"; the droombaan is shared by both sets).
- **Copy:** add `Route.{key}.1–3`, `.Goal` and, where useful, `Alt.{key}` in `PupilVerhaalCopy.cs`, following the existing title|detail format. No school names except the generic "mbo"; no URLs.
- **Tests:**
  - change the hardcoded `48` in `PupilStoryAndDreamJobTests` to `DreamJobCatalog.All.Count` and assert `>= 58`
  - all catalog guards stay green (unique keys and titles, icons present, search: `Search("magazijn")` finds `logistiek-medewerker`)
- **K3 in the PR body:** the candidate onboarding (`DreamJobStep.razor`) now also offers these 10 jobs. Attach a screenshot of the candidate step to show it still lays out well.

## 06.4 "docent of mentor" for VO
- **Add `.Vo` variants**, picked through the served set (04's `PupilCopy.For` pattern, here on `PupilVerhaalCopy`):

  | Key | VO variant |
  |---|---|
  | `LeerlingDroom.UndecidedHint.Vo` | "Praat erover met je docent of mentor." |
  | `LeerlingStory.Privacy.Vo` | "Je docent kan dit verhaal ook zien, met jouw code. Niemand anders." |
  | `LeerlingDroom.Cheer.Low.Vo` / `.Mid.Vo` | the same text with "docent of mentor" instead of "leraar" |
  | `LeerlingPdf.Footer.Vo` | "Lobsy bewaart geen namen. Deze PDF is voor jou en je docent." |
  | `LeerlingDroom.Need.Ruimte.Next.Vo` | "Praat met je mentor over zelfstandig werken" |

- **G78** keeps today's "leraar" texts unchanged.
- **The renderer** takes the set from `PupilClassContext.Set`. The teacher sees the **pupil's** variant: the same text the pupil saw.

## Tests
- **`PupilStoryAndDreamJobTests`:**
  - every route renders for a G78 context ("Nu: groep 8"), a Havo-3 context ("Nu: klas 3 havo") and a Mix-1 context ("Nu: klas 1")
  - no `{nu}` and no "klas 2" are left
  - VO context → "docent of mentor" hint; G78 → "leraar"
- **The PDF** (`PupilReportPdfService` test) contains the resolved "Nu: …" and the set's footer.
- **Catalog and icon guards** for the 10 new jobs; `PupilDreamJobFit` evaluates every new route without exceptions for an all-3 result.
- **Teacher detail** (`TeacherPortalUnitTests`) shows the same resolved route as the pupil.

## Success criteria
- A groep 8 pupil reads "Nu: groep 8"; a havo-3 pupil reads "Nu: klas 3 havo". No route anywhere says "klas 2" unless the pupil really is in klas 2.
- 10 mbo occupations are pickable with working routes and icons.
- VO pupils read "docent (of mentor)", G78 pupils "leraar".

Done → next: `07-puzzels-engine.md`.
