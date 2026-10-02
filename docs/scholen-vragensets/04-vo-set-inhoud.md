# 04. VO question set: 100 items from the CSV, guards, island after 50, two lesson parts, calm VO copy

Read `00-README.md` first (§S, D2, K8–K10). Branch `cursor/vragensets-4` from `cursor/vragensets-3b`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-4`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.
> - **Use the 100 VO texts exactly as in the CSV.** Never "improve" a word; Dennis approved them.

| | |
|---|---|
| Branch | `cursor/vragensets-4` |
| PR title | `feat(scholen): VO question set — 100 items (9101–9200), island after 50, two lesson parts, calm VO copy` |
| PR body starts with | `Stacked on #<PR 03b> (cursor/vragensets-3b)` |
| Content source | `vo-set/vo-vragenset-100.csv` (+ `vo-vragenset-100.md` for counts, readability and judgement calls) |
| Mark | **"Needs content review by Dennis"** (cheer lines, part-break and stop texts, VO "docent" variants) |

## 04.1 Content into the repo
- **Copy the CSV.** `git show origin/docs/scholen-vragensets:docs/mockups/scholen-vragensets/vo-set/vo-vragenset-100.csv > docs/scholen/vo-vragenset-100.csv`.
  - This is the **content source** on the code branch: a data file, not a mockup.
  - Columns are `world,dimension,reversed,text,scenario`, in display order. `dimension` already uses the catalog constants (`CompetencyTestCatalog.Samenwerken` … `CulturePersonalityCatalog.PeopleFirst`). `reversed` is `true/false`.
- **Ids** follow CSV order: rows 1–25 Koraalrif → **9101–9125**, 26–50 Schatgrot → **9126–9150**, 51–75 Vuurtoren → **9151–9175**, 76–100 Lagune → **9176–9200**.
- **New `Jobsy.Web/Localization/UiStringsLeerlingVragenVo.cs`** (nl-only, `MergeNl`). In display order, for each item:
  - a comment `// 9101 · Koraalrif · Samenwerken · rev:no`
  - `nl["LeerlingQ.9101"] = "<text>"`
  - `nl["LeerlingQ.9101.Voorbeeld"] = "<scenario>"`

  The ids don't overlap with G78, so the `LeerlingQ.{id}` key pattern stays. Register it in `UiStrings.cs` next to `UiStringsLeerlingVragen.MergeNl(nl)`.
- **New `PupilQuestionBankVo : PupilQuestionBankBase`** (Core) with `MinId 9101`, `MaxId 9200`, `Count 100` and the 100 `Q(...)` lines in CSV order (world, model, category constant, reverse). Use `AssessmentKind`:
  - Koraalrif = Competence
  - Schatgrot = Career
  - Vuurtoren = Values
  - Lagune = Culture
- **Parity test `VoQuestionBankCsvParityTests`.** It reads `docs/scholen/vo-vragenset-100.csv` and asserts, for row *n*:
  - item `9100+n` has that world, category and reverse flag
  - `LeerlingQ.{id}` equals `text` and `LeerlingQ.{id}.Voorbeeld` equals `scenario`, **byte for byte** after trimming
  - there are 100 rows
  
  This is the "as-is" guarantee.

## 04.2 Register VO (`PupilQuestionSetRegistry`)
- **The `Vo` def:**
  - bank `PupilQuestionBankVo`, worlds 25×4, `IslandAfter = 50`, no puzzle slots (F1)
  - `AnswerLabels` = `Leerling.Vo.Answer.1…5`
  - `ItemsPerPlate = 10`, `PlateCount = 10`
  - `CheerKeyPrefix = "LeerlingQ.Vo.Cheer."`, `CheerCount = 12`
  - `PartBreakAfterIsland = true`, `StartTimeKey = "Leerling.Vo.Start.NoteTime"`, `ScoringVersion = "vo-1"`
- **`IsAvailable(Vo)` = true** from now on, so `Serve(Vo)` returns VO.
  - Unstarted codes in VO classes get VO.
  - Pinned `Groep78` codes keep G78 (K1).
- **Answer labels** (`UiStringsScholen.cs`):

  | Key | Text |
  |---|---|
  | `Leerling.Vo.Answer.1` | "Klopt niet" |
  | `Leerling.Vo.Answer.2` | "Klopt meestal niet" |
  | `Leerling.Vo.Answer.3` | "Klopt deels" |
  | `Leerling.Vo.Answer.4` | "Klopt meestal" |
  | `Leerling.Vo.Answer.5` | "Klopt helemaal" |

  No emoji. Today's answer markup renders them until 05 replaces it.

## 04.3 Guards (set-aware `PupilQuestionBankTests`)
- **Run the same rules over every registered set:**
  - text ≤ 14 words, example ≤ 30 words (`DutchReadability.CountWords`)
  - every example starts with "Stel je voor:"
  - no digits in text
  - no two adjacent items in a world share a category
  - readability: average ≤ 12 words per sentence, long-word share ≤ 10 % per set
  - all keys present
- **Distribution per set** (exact): G78 = today's table. VO:
  - **Koraalrif:** each competency 5, of which 1 reversed
  - **Schatgrot:** Realistic 5, the others 4, none reversed
  - **Vuurtoren:** each value 5, none reversed
  - **Lagune:** Collaboration 5, the others 4; only Informal has 1 reversed
- **Forbidden words per set:**
  - G78 keeps today's list
  - VO uses today's list **minus** `collega` and `klant`
  - keep the lists as two named arrays in the test, plus a comment that cites D2
- **VO reversed items** contain no negation word (`niet`, `geen`, `nooit`, `niemand`, `niets`) in the **text**, so there's no double negative with "Klopt niet".
- **Expected numbers** (from `vo-vragenset-100.md`, put them in the PR):
  - average 11.0 words per sentence
  - 1.8 % long words
  - text max 14 words
  - scenario max 24 words
  
  Your `DutchReadability` numbers may differ slightly (different tokenizer). They must stay within the limits.
- **Generated doc.** `PupilQuestionBankDocFreshnessTests` generates one doc per set:
  - `docs/scholen/vragenbank-leerlingen.md`: G78, only the title becomes "Vragenbank leerlingen groep 7/8 (review)"
  - `docs/scholen/vragenbank-leerlingen-vo.md`: VO
  - same env var `JOBSY_UPDATE_PUPIL_QBANK_DOC=1`
  - The failure message names the stale file.

## 04.4 Flow: island after 50, two lesson parts
- The island comes after 50 through `PupilFlow` (03a). Nothing is VO-specific in the engine.
- **Part break** (`PartBreakAfterIsland`, VO only). After a successful chip save, `LeerlingEiland.razor` doesn't navigate straight on. It shows a panel in the same card (`role="status"`, focus moves to its heading):
  - `Leerling.Vo.PartBreak.Title` = "Deel 1 is klaar!"
  - `Leerling.Vo.PartBreak.Body` = "Goed gedaan. Je antwoorden zijn bewaard. Je docent zegt of je nu verdergaat of in een volgende les."
  - **Primary button:** `Leerling.Vo.PartBreak.Continue` = "Verder met deel 2" → `/leerling/reis`.
  - **Secondary:** `Leerling.Vo.PartBreak.Stop` = "Stoppen voor nu". It's a form POST to `/leerling/stop` (the same endpoint as the header "Pauze" in `LeerlingLayout.razor`) with a hidden `part=1`.
- **`PupilAuthEndpoints`:** the `/leerling/stop` POST keeps signing out exactly as today. Only the redirect changes: to `/leerling/stop?done=deel1` when `part=1`, otherwise `?done=1` (today). Don't touch anything else in the cookie scheme.
- **`LeerlingStop.razor`:** `Done == "deel1"` shows `Leerling.Vo.Stop.Part1Body` = "Volgende keer log je weer in met je code. Je gaat dan verder bij de vuurtoren." Other values show today's text.
- **No new state.** After a re-login, `PupilFlow` puts the pupil on item 51 (Vuurtoren). The part break shows only right after the island save; that's enough.
- **G78:** no part break; the flow is unchanged.

## 04.5 Calm VO copy variant ("docent", no childish lines)
- **Cheers** `LeerlingQ.Vo.Cheer.1–12` in `UiStringsLeerlingVragenVo.cs`. Proposal (content review):
  1. "Er zijn geen foute antwoorden. Kies wat het best bij je past."
  2. "Neem gerust de tijd. Er is geen haast."
  3. "Je antwoorden worden steeds bewaard."
  4. "Twijfel je? Kies wat meestal klopt."
  5. "Je bent al een flink stuk op weg."
  6. "Denk aan school, stage, je bijbaan of je vrije tijd."
  7. "Kies wat bij jou past, niet wat anderen vinden."
  8. "Stoppen mag. Je gaat later verder waar je was."
  9. "Goed bezig. Nog even en dan komt de volgende wereld."
  10. "Eerlijk antwoorden helpt jou het meest."
  11. "Lobsy leert je steeds beter kennen."
  12. "Klaar voor de volgende vraag?"

  Guard them like the cheers today: they exist, ≤ 14 words, VO wordlist.
- **The lobster and plates** stay (same component). Only the lines change. World intro and "Wereld klaar!" lines stay shared.
- **Start screen:** `Leerling.Vo.Start.NoteTime` = "Duurt ongeveer 40–45 minuten, in twee delen." G78 keeps "Duurt ongeveer 25 minuten." until 10, which changes it to "ongeveer 30 minuten" once all three puzzles are live.
- **Leftover set-specific lines (also hardcoded; not listed in 03.1):**
  - `LeerlingStory.DonePill` = "Klaar · 60 van 60" (`PupilVerhaalCopy.cs`) becomes "Klaar · {0} van {0}", filled with the served set's item count.
  - `Leerling.Reis.Bubble` = "Goed bezig! Elke 6 vragen valt er een schaaltje af." gets a VO variant `Leerling.Reis.Bubble.Vo` = "Elke 10 vragen valt er een schaaltje af. Je antwoorden worden bewaard."
  - Guard: no pupil-facing string contains a literal "60 van 60" or "Elke 6 vragen" without a G78-only key.
- **"docent" for VO pupils.** Add a VO variant and pick it by the pupil's served set, through a small helper `PupilCopy.For(def, key)` that tries `key + ".Vo"` first for VO:
  - `Leerling.WindowClosed.Body`
  - `Leerling.Login.Error.Invalid`, `.Cooldown`, `.Window`. On the login page the set comes from the selected class: append `PupilQuestionSet QuestionSet` to `PupilClassOptionDto`.
  - the hardcoded Dutch messages in `PupilPortalService` (`"Je leraar zet het weer open."`, `"Je antwoorden zijn bewaard. Je leraar zet de test weer open."`, …). Move the pupil-facing ones to keys with a `.Vo` variant (K10). Error **codes** stay the same.

  Droombaan/story/PDF lines are file 06.

## 04.6 Factual count lines (K9, D5 *extra*)
- **`OuderbriefTemplate.DutchText`** (`SchoolPortalService.cs` ~L1415): replace **only** the sentence "De test bestaat uit 60 kindvriendelijke vragen en past in één lesuur." with "De vragenlijst past bij de leeftijd: in groep 7/8 zijn het 60 korte vragen (ongeveer een half uur), op de middelbare school 100 korte vragen in twee lesdelen."
  - Bump `ParentalInfoTexts.CurrentVersion` to `"ouders-2026-10-v2"`.
  - Nothing else in the letter changes (N1 is Dennis's).
- **`Jobsy.Web/wwwroot/docs/scholen/lesbrief-lobsy.html`:** "Beantwoord 60 vragen; progressie wordt bewaard." → "Beantwoord de vragen (groep 7/8: 60 vragen; middelbare school: 100 vragen in twee lesdelen, pauze na het Pauze-eiland). Alles wordt bewaard."
- **`SchoolClassForm` set note** (02.6): the VO text now shows "100 vragen (Klopt niet … Klopt helemaal) · 2 lesdelen · ongeveer 40–45 minuten" and "Pauze na het Pauze-eiland: daar kan de les stoppen." (`IsAvailable(Vo)`).

## Tests
- `VoQuestionBankCsvParityTests` (04.1), the set-aware bank guards (04.3), doc freshness for both docs.
- **VO scoring fixtures** (hand-computed, in `PupilQuestionBankTests` or `PupilScoringPerSetTests`):
  - all-3 → 50 % on every dimension
  - all-5 → 100 % except the reversed dimensions, which give the exact hand-computed value (e.g. Koraalrif Samenwerken with 4×5 + rev 5 → (4×5 + 1)/5 = 4.2 → 80 %)
  - one fixture per model that includes the reversed item
  - `ScoringVersion = "vo-1"`, `QuestionSet = Vo`
- **Class level → set (API):**
  - a fresh code in a Havo class answers `9101` → 200 and is pinned `Vo`, and the progress total is 100
  - the same pupil answering `9001` → 400 `wrong_set`
  - a code pinned `Groep78` in a Havo class still gets 60 items
  - a Groep78 class code gets 60 items
- **Flow:**
  - VO: the island is due at 50 and not at 30
  - after chips, `NextStep = question` index 50
  - the part-break panel renders (bUnit), "Stoppen voor nu" posts `part=1`, and the stop page shows the deel-1 text
- **Copy:** VO cheer keys are used for a VO pupil and G78 keys for a G78 pupil; the VO login error from a VO class says "docent".
- **Ouderbrief:** the version is `ouders-2026-10-v2` and the letter contains no "60 kindvriendelijke".

## Success criteria
- A VO pupil does 100 questions with the island after 50, and can stop after deel 1 and resume at item 51.
- All 100 texts are byte-identical to the CSV, and every guard is green for both sets.
- G78 is unchanged (the 03a golden flow still passes).

Done → next: `05-antwoordschaal-ui.md`.
