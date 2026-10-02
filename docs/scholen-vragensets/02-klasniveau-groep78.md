# 02. Class level picks the question set: `SchoolLevel.Groep78`, leerjaar rules, pinning, migration + backfill, level picker

Read `00-README.md` first (§S, §DM, D1, K1). Branch `cursor/vragensets-2` from `cursor/vragensets-1` (or from `origin/acceptatie` if 01 was skipped).

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-2`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.
> - Teachers see only their own classes.

| | |
|---|---|
| Branch | `cursor/vragensets-2` |
| PR title | `feat(scholen): class level picks the question set — Basisschool groep 7/8 + VO levels, leerjaar rules, pinning + backfill` |
| PR body starts with | `Stacked on #<PR 01> (cursor/vragensets-1)` |
| Mockups | `klas-niveau/kl-d01-nieuwe-klas-basisschool.png`, `kl-m01-nieuwe-klas-vo.png`, `kl-m02-klas-bewerken-vergrendeld.png` (+ `html/`) |
| Migration | `AddClassQuestionSet` (the only one in this file) |

## 02.1 Today (verify first)
- **`Jobsy.Core/Enums/SchoolLevel.cs`:** `VmboB=0, VmboK=1, VmboGt=2, Mavo=3, Havo=4, Vwo=5, Mix=6, Anders=7`. It is stored as an int (`JobsyDbContext` `HasConversion<int>()` for `SchoolClass.Level` and `SchoolClassAggregate.Level`).
- **`SchoolClass.Year`** has the doc comment "School year number 1–6". `SchoolPortalService.CreateClassAsync` (~L389) and `UpdateClassAsync` (~L466) reject `Year is < 1 or > 6` with "Leerjaar moet 1–6 zijn.".
- **`SchoolClasses.razor`** (create drawer, ~L77–125):
  - `InputSelect` over `Enum.GetValues<SchoolLevel>()` that prints the **raw enum name** (`@level` → "VmboGt")
  - `InputNumber` for Year; defaults `Havo` / `2`
  - the list prints `@row.Level` and `@row.Year` raw
- **Raw `Level` in other places:**
  - `SchoolClassDetail.razor` L36
  - `LeraarKlasOverview.razor` L41
  - `SchoolLayout.razor` L187 (scope chip "`{c.Level} {c.Year}`")
  - `ScholenRapportage.razor` (filter + "per niveau" table)
- **No edit UI:** `JobsyApiClient.UpdateSchoolClassAsync` and `PUT api/school/classes/{id}` exist, but no `.razor` page calls them.
- **Seeding:** `TestAccountsSeedService` (acceptatie CLI) creates class "1A", `VmboGt`, Year 1.

## 02.2 Core
- **`SchoolLevel`:** add `Groep78 = 8` with an XML comment "Basisschool, groep 7 of 8 (Year = 7 or 8)". No renumbering.
- **New `Jobsy.Core/Enums/PupilQuestionSet.cs`:** `Groep78 = 1`, `Vo = 2`, with XML docs pointing to §S.
- **New `Jobsy.Core/Scholen/SchoolLevelRules.cs`** (pure, unit-tested):
  - `PupilQuestionSet QuestionSetFor(SchoolLevel level)` → `Groep78` for `Groep78`, else `Vo`
  - `bool IsPrimary(SchoolLevel level)`
  - `string? ValidateYear(SchoolLevel level, int year)` → `null` or a Dutch error: "Kies groep 7 of groep 8." / "Leerjaar moet 1–6 zijn."
  - `string LabelKey(SchoolLevel level)` → `School.Level.{Name}`
  - `string YearLabel(SchoolLevel level, int year)` → "Groep 7" / "Klas 2" (Dutch, used in lists, the scope chip and 06)
  - `IReadOnlyList<SchoolLevel> VoLevels` in display order `VmboB, VmboK, VmboGt, Mavo, Havo, Vwo, Mix, Anders`
- **`SchoolClass`:**
  - add `public PupilQuestionSet QuestionSet { get; set; }`
  - fix the `Year` comment ("1–6 for VO; 7–8 for Groep78")
- **`PupilCode`:** add `public PupilQuestionSet? QuestionSet { get; set; }` with the comment "Pinned on the first saved answer to the set actually served; null = follows the class."
- **New `Jobsy.Core/Scholen/PupilQuestionSetResolver.cs`:** `static PupilQuestionSet Effective(PupilCode code, SchoolClass cls) => code.QuestionSet ?? cls.QuestionSet;`. Every later file goes through this.

## 02.3 Migration `AddClassQuestionSet` (Infrastructure)
- **`SchoolClasses.QuestionSet`** int not null, default 2. **Backfill** in SQL: `UPDATE "SchoolClasses" SET "QuestionSet" = CASE WHEN "Level" = 8 THEN 1 ELSE 2 END` (today: all rows 2).
- **`PupilCodes.QuestionSet`** int null. **Backfill:** set it to 1 where the code has a `PupilProgresses` row whose `AnswersJson` is not `'{}'`/empty, **or** a `PupilResults` row. Leave the rest `NULL` (§DM, K1).
  - Write the SQL so it is idempotent and works on an empty DB.
  - Put the reasoning in a comment above it.
- **Index** `IX_PupilCodes_SchoolClassId_QuestionSet`.
- **`JobsyDbContext`:** `HasConversion<int>()` for both columns.
- **Down:** drop the columns and the index.
- **Report in the PR:** run the migration on a local DB seeded with `TestAccountsSeedService` plus 3 hand-made codes (no progress / progress / result) and paste the before/after counts.

## 02.4 Pin on first answer (Infrastructure)
- In `PupilPortalService.SaveAnswerAsync`: when `code.QuestionSet is null`, set it to the set that **served** this item.
  - In this file that is always `PupilQuestionSet.Groep78`: the only bank is still the 60-item `PupilQuestionBank`, and the VO bank arrives in 04.
  - 03a replaces this with "the served set".
- Note that a `Vo` class keeps getting the 60 items until 04 ships. That's intended: those pupils are pinned to `Groep78` and stay valid (README §S).
- Write it once, on the same `SaveChanges` as the answer. Never change a pinned value.

## 02.5 API (`SchoolPortalService`, DTOs)
- **`CreateClassAsync` / `UpdateClassAsync`:**
  - replace the Year check with `SchoolLevelRules.ValidateYear`
  - set `QuestionSet = SchoolLevelRules.QuestionSetFor(request.Level)`
- **Lock rule** (D1 *extra*). In `UpdateClassAsync`, a change is **blocked** when the new level maps to a different set **and** any code of the class has `QuestionSet != null` with a different value.
  - Return `(null, "Soort klas ligt vast: er zijn al leerlingen van deze klas begonnen.")`; the controller maps it to 409 with error code `level_locked`.
  - Otherwise unpinned codes simply follow the class.
- **DTOs** (append parameters at the end; update all `new(...)` sites and tests):
  - `SchoolPortalClassListItemDto`, `SchoolPortalClassDetailDto`: `PupilQuestionSet QuestionSet`, `bool LevelLocked` (detail only)
  - teacher class DTOs in `TeacherPortalDtos.cs` that carry `Level`/`Year`: `PupilQuestionSet QuestionSet`
- **Admin:** `AdminSchoolsController` / `ScholenRapportage` level filter: add `Groep78` with its label. Admin keeps seeing only aggregates.

## 02.6 UI (school portal; mockups `kl-*`)
- **New component `Jobsy.Web/Components/School/SchoolClassForm.razor`.** Create and edit share it. Fields in this order:
  1. **Klasnaam**: unchanged validation.
  2. **Soort klas**: a `radiogroup` of 2 cards "Basisschool / Groep 7 of groep 8" and "Middelbare school / Vmbo, mavo, havo, vwo". Native `<input type="radio">` inside labels, ≥ 44 px, visible focus.
  3. **Basisschool:** "Groep" as a 2-option segmented radio (Groep 7 / Groep 8), giving Year 7/8. **Middelbare school:** "Niveau" `InputSelect` over `SchoolLevelRules.VoLevels` with labels `School.Level.*` (Vmbo-b, Vmbo-k, Vmbo-gt, Mavo, Havo, Vwo, Gemengd, Anders), plus "Leerjaar" as a 1–6 segmented radio.
  4. **Set info box** (`role="status"`, `sch-set-note`). It updates live, with the texts from the mockup:
     - **G78:** "Vragenlijst: Groep 7/8 · 60 vragen (Nee … Ja!) · 3 puzzelpauzes · ongeveer 30 minuten. Elke leerlingcode van deze klas krijgt deze vragenlijst. Leerlingen kiezen zelf niets."
     - **VO:** "Vragenlijst: Middelbare school · 100 vragen (Klopt niet … Klopt helemaal) · 2 lesdelen · ongeveer 40–45 minuten. … Pauze na het Pauze-eiland: daar kan de les stoppen."
     - **Before 04 and 07 are live** the box must not promise what isn't built yet. Gate the "100 vragen / 2 lesdelen" and "3 puzzelpauzes" parts on `PupilQuestionSetRegistry` capabilities (03a). In 02, show only "Vragenlijst: Groep 7/8" / "Vragenlijst: Middelbare school" + "Elke leerlingcode …". 04 and 07 extend the text.
  5. **Aantal leerlingen** (create only), the codes note, Leraar(en): unchanged.
- **Switching** Soort klas resets the year to a valid default (7 for Basisschool, 1 for VO) so the form is never invalid without a message.
- **Create:** `SchoolClasses.razor` uses the form in its `EntDrawer`. Default = Middelbare school · Havo · 2 (today's default).
- **Edit (new, small):** a "Bewerken" button on `SchoolClassDetail.razor` (SchoolAdmin only, same rule as today's API) opens the form in an `EntDrawer` (`PUT api/school/classes/{id}`).
  - When `LevelLocked`: the "Soort klas" cards are disabled, and the warning from `kl-m02` is shown (`role="note"`). The level within VO and the year stay editable.
  - A 409 `level_locked` from the API shows the same text.
- **Lists and labels.** Replace every raw `@…Level` / `@…Year` with `Culture[SchoolLevelRules.LabelKey(level)]` and `SchoolLevelRules.YearLabel(...)`:
  - `SchoolClasses.razor` adds a column "Vragenlijst" with a pill: `sch-pill` "Groep 7/8" / "VO", and the label is always text
  - `SchoolClassDetail.razor`
  - `LeraarKlasOverview.razor`
  - the `SchoolLayout.razor` scope chip ("Klas 7A · Groep 7")
  - `ScholenRapportage.razor`
- **Strings** (`UiStringsScholen.cs`):
  - `School.Level.*` (9)
  - `School.Class.Field.Kind`, `School.Class.Kind.Primary`, `.PrimaryHint`, `.Secondary`, `.SecondaryHint`
  - `School.Class.Field.Group`, `School.Class.Group.7`, `.8`
  - `School.Class.SetNote.*`
  - `School.Class.LevelLocked`
  - `School.Col.QuestionSet`
  - `School.Class.Edit`
- **CSS:** `scholen.css` gets `sch-kind-card`, `sch-seg`, `sch-set-note` (tokens only, no inline `style=""`, mobile first).

## 02.7 Seed (acceptatie CLI)
`TestAccountsSeedService`: keep "1A" (VO). Add class "7A", `Groep78`, Year 7, 5 codes, `IsTestData = true`, same school, idempotent like "1A".

## Tests
- **`SchoolLevelRulesTests`:**
  - every enum value maps to a set (`Groep78` → `Groep78`, all others → `Vo`)
  - `ValidateYear`: Groep78 7/8 ok and 6/9 rejected; VO 1–6 ok and 0/7 rejected
  - labels exist for every level
- **API (`SchoolPortalApiTests`):**
  - create a Groep78 class with Year 8 → `QuestionSet = Groep78`
  - create a Groep78 class with Year 3 → 400 with the message
  - create a Havo class → `Vo`
  - update Havo → Vwo with started codes → 200
  - update Havo → Groep78 with a pinned code → 409 `level_locked`
  - update Havo → Groep78 with no pinned codes → 200 and the class set changes
  - a Teacher calling create/update → 403 (rights matrix unchanged)
- **Class level → set selection** (`PupilQuestionSetResolverTests`): class `Vo` + code `null` → `Vo`; class `Vo` + code `Groep78` → `Groep78`; class `Groep78` + code `null` → `Groep78`.
- **Pinning:** the first answer pins, a second answer doesn't change it, and a level change after the pin doesn't change it.
- **Migration test** (pattern of the existing migration tests; if none exists, an integration test on a fresh DB): the backfill rules from 02.3 on 3 fixture codes.
- **bUnit** (`SchoolClassForm`):
  - switching kind changes the fields and resets the year
  - the info box text per set
  - locked mode disables the cards and shows the warning
  - radios have accessible names
- **Guards:** `NoPupilNameFieldsTests` (new columns are enums), `ScholenRightsMatrix` (no new staff endpoint), `BlazorPageRoleAttributesTests`.

## Success criteria
- A school can create a groep 7 class and a havo-2 class. The list shows "Groep 7 · Vragenlijst Groep 7/8" and "Klas 2 · VO" with readable labels, and no raw enum names anywhere.
- The migration is correct for any mix of existing data (02.3), and no pupil's in-progress answers become invalid.
- The edit drawer enforces the lock rule (API and UI).

Done → next: `03-vragenset-abstractie.md`.
