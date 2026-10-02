# 02. Class level picks the test: `SchoolLevel.Groep78`, leerjaar rules, migration + backfill, level picker

Read `00-README.md` first (§S, §DM, §E, D1). Branch `cursor/vragensets-2` from `cursor/vragensets-1` (or from `origin/acceptatie` if 01 was skipped).

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
| PR title | `feat(scholen): class level picks the question set — Basisschool groep 7/8 + VO levels, leerjaar rules, existing classes → VO` |
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
- **No set on `PupilCode`.** The test of a code is always `code.SchoolClass.QuestionSet` (README §S). Don't add a per-code column, resolver, override or "started with" field. A code never moves to another class (no service changes `SchoolClassId` after creation; keep it that way).
- **`SchoolLevelRules.StartedCodesBlockChange(SchoolLevel from, SchoolLevel to)`** → `true` when `QuestionSetFor(from) != QuestionSetFor(to)`.

## 02.3 Migration `AddClassQuestionSet` (Infrastructure)
- **`SchoolClasses.QuestionSet`** int not null, default 2. **Backfill** in SQL: `UPDATE "SchoolClasses" SET "QuestionSet" = CASE WHEN "Level" = 8 THEN 1 ELSE 2 END` (today: all rows 2).
  - Write the SQL so it is idempotent and works on an empty DB. Comment: "Existing classes default to the VO test (Dennis, 2 Oct 2026). Their legacy answers are handled by the cut-over in 04 (README §E)."
- **No change to `PupilCodes`, `PupilProgresses` or `PupilResults`** in this migration. Existing answers are neither moved nor re-labelled.
- **`JobsyDbContext`:** `HasConversion<int>()` for the column.
- **Down:** drop the column.
- **Report in the PR:** run the migration on a local DB seeded with `TestAccountsSeedService` and paste the before/after class counts per `QuestionSet`.

## 02.4 What pupils get in this file (interim until 04, README §E)
- Nothing changes for pupils yet. Every class still gets today's 60 items through today's code. 03a makes this explicit (`LegacyVo` def for VO classes, G78 def for Groep78 classes), and 04 does the cut-over.
- No per-code state is written. Don't add anything to `SaveAnswerAsync` in this file.

## 02.5 API (`SchoolPortalService`, DTOs)
- **`CreateClassAsync` / `UpdateClassAsync`:**
  - replace the Year check with `SchoolLevelRules.ValidateYear`
  - set `QuestionSet = SchoolLevelRules.QuestionSetFor(request.Level)`
- **Lock rule** (D1). In `UpdateClassAsync`, a change is **blocked** when `SchoolLevelRules.StartedCodesBlockChange(old, new)` **and** any code of the class has started: `Status != NotStarted`, **or** a `PupilProgress` row, **or** a `PupilResult` row. Use one `AnyAsync` query and never load answers.
  - Return `(null, "Soort klas ligt vast: er zijn al leerlingen van deze klas begonnen.")`; the controller maps it to 409 with error code `level_locked`.
  - Without started codes the change is allowed and `QuestionSet` follows the new level. All codes of the class follow it, because none has started.
  - A change that keeps the test (havo → vwo, klas 2 → 3, groep 7 → 8) is always allowed.
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
     - **Before 04 and 07 are live** the box must not promise what isn't built yet. Gate the "100 vragen / 2 lesdelen" and "3 puzzelpauzes" parts on the registry (03a: `IsLegacy(Vo)`; 07: the enabled puzzle keys). In 02, show only "Vragenlijst: Groep 7/8" / "Vragenlijst: Middelbare school" + "Elke leerlingcode …". 04 and 07 extend the text.
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
  - update Havo → Groep78 with a started code (InProgress, or a progress row, or a result) → 409 `level_locked`, and nothing is changed
  - update Havo → Groep78 with only NotStarted codes → 200 and the class test changes
  - update Groep78 year 7 → 8 with started codes → 200
  - a Teacher calling create/update → 403 (rights matrix unchanged)
- **Class level → test:** `SchoolLevelRules.QuestionSetFor` for every level, and `StartedCodesBlockChange` truth table (G78↔VO true, within VO false, G78 7↔8 false).
- **No per-code set:** a reflection guard asserts `PupilCode`, `PupilProgress` and `PupilResult` have no property of type `PupilQuestionSet`/`PupilQuestionSet?` (README §S).
- **Migration test** (pattern of the existing migration tests; if none exists, an integration test on a fresh DB): every existing class → `QuestionSet = 2`; codes, progress and results are byte-identical before and after.
- **bUnit** (`SchoolClassForm`):
  - switching kind changes the fields and resets the year
  - the info box text per set
  - locked mode disables the cards and shows the warning
  - radios have accessible names
- **Guards:** `NoPupilNameFieldsTests` (the new column is an enum), `ScholenRightsMatrix` (no new staff endpoint), `BlazorPageRoleAttributesTests`.

## Success criteria
- A school can create a groep 7 class and a havo-2 class. The list shows "Groep 7 · Vragenlijst Groep 7/8" and "Klas 2 · VO" with readable labels, and no raw enum names anywhere.
- The migration is correct for any mix of existing data (02.3) and touches only `SchoolClasses`; the class level is the only source of the test.
- The edit drawer enforces the lock rule (API and UI).

Done → next: `03-vragenset-abstractie.md`.
