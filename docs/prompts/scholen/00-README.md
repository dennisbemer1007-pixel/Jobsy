# Lobsy voor scholen (Schoolbeheerder · Leraar · Leerling): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

**What this stack builds:** a new, separate school product inside Lobsy.
- **Lobsy admin** creates schools, records the verwerkersovereenkomst, invites the first schoolbeheerder and controls three settings.
- **Schoolbeheerder** (`SchoolAdmin`, mandatory 2FA) creates classes with a number of pupils. Lobsy generates anonymous **codes**; the school prints the code list (with an **empty** "Naam (vul zelf in)" column), invites teachers, confirms per class that parents were informed and opens/closes the test window.
- **Leraar** (`Teacher`, mandatory 2FA) sees **only the classes assigned to them**: codes with status and progress, group results (k ≥ 5) and per code the story, the droombaan route and a PDF.
- **Leerling** (no account) logs in with **school (dropdown) + klas (dropdown) + code**. They get a playful wizard: 60 child-friendly questions over the 4 Lobsy models, an info bubble "Stel je voor…" per question, the lobster that sheds shell plates, hobbies/niet-leuk chips, then "Dit ben jij" (fixed templates), a **droombaan-checker without AI** and a PDF.
- **Privacy by design:** Lobsy stores **no pupil names at all**. The school is the controller, Lobsy the processor. There are no affiliate/partner links, no vacancies, no employer visibility and no AI on pupil data. Individual data is deleted at the end of the school year; anonymous aggregates (k ≥ 5) are kept separately.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-datamodel-rollen-admin.md`: roles `SchoolAdmin` + `Teacher` (mandatory 2FA via `MfaPolicy`), entities `School`, `SchoolClass`, `TeacherClassAssignment`, `PupilCode`, `PupilProgress`, `PupilResult`, aggregate tables, code generator, rights-matrix foundation, admin "Scholen" pages (create school, verwerkersovereenkomst, invite schoolbeheerder), 3 admin settings, `SchoolLayout` shell | `cursor/scholen-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-school-portaal.md`: school dashboard, Te doen, Klassen & codes (create class → codes, code list PDF/CSV with empty name column, reset/delete code), Leraren (invite, own classes, 2FA verplicht), parental-information confirmation per class, test window open/close, Resultaten (per-code switch) | `cursor/scholen-2` | `cursor/scholen-1` | `acceptatie` |
| 03 | `03-leraar-portaal.md`: klasoverzicht, codes, group results (k ≥ 5), droombanen, detail per code (story + fit result + PDF slot), class switcher, test window for own class, code list reprint | `cursor/scholen-3` | `cursor/scholen-2` | `acceptatie` |
| 04 | `04-leerling-login-wizard.md`: pupil login (school/klas dropdown + code), separate `Pupil` cookie scheme (session cookie, short idle/absolute timeout), rate limit + lockout, wizard shell (4 worlds, shell plates, progress saved after every answer, continue later), pause/stop | `cursor/scholen-4` | `cursor/scholen-3` | `acceptatie` |
| 05 | `05-vragenbank.md`: the 60-item child-friendly question bank (15 per model, B1/A2, "Stel je voor…" per item) mapped onto the existing scoring of the 4 models, one reviewable file + generated doc, readability guards, hobbies/niet-leuk chips | `cursor/scholen-5` | `cursor/scholen-4` | `acceptatie` |
| 06 | `06-verhaal-droombaan-pdf.md`: "Dit ben jij" from fixed templates, droombaan-checker (no AI, no links, no vacancies), PDF via QuestPDF on demand (never stored), for pupil and teacher | `cursor/scholen-6` | `cursor/scholen-5` | `acceptatie` |
| 07 | `07-bewaartermijn-aggregaten.md`: aggregate snapshots (k ≥ 5), retention job (school-year end, default 31 July, configurable), early deletion paths, admin reporting | `cursor/scholen-7` | `cursor/scholen-6` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/scholen-1b`).

## Pointer prompt (the only prompt needed; it runs 01 … 07)
```
Run the Scholen stack. First: git fetch origin && git show origin/docs/scholen:docs/prompts/scholen/00-README.md — read it completely.
Then read and execute each file in docs/prompts/scholen/ on that branch strictly in the order the README's table lists (01 … 07; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 01 which case applied.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/security/roles-matrix.md`, `docs/adr/0004-roles-and-scope.md`, `docs/adr/0005-mfa-local-only.md` and `docs/release-flow.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01).
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. File 01: `git checkout -b cursor/scholen-1 origin/acceptatie`. Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. Body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, go on to the next file.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, don't continue.
5. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
6. **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you, `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** Body starts with `Stacked on #<prev PR> (<prev branch>)`; file 01 says `Stacked on: none (first in the stack)`. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST), which includes the merged `fix/2fa-enrollment` work (`MfaPolicy`, `MfaEnforcementMiddleware`, forced enrollment, admin 2FA reset). Re-check line numbers before editing.
- **Mockups:** branch `docs/scholen`, folder `docs/mockups/scholen/`. Read with `git fetch origin docs/scholen && git show origin/docs/scholen:docs/mockups/scholen/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - School desktop 1440×900: `sc-s1-school-dashboard.png`, `sc-s2-klassen-codes.png`, `sc-s3-leraren-2fa.png`, `sc-s4-resultaten-per-code.png`.
  - Leraar desktop 1440×900: `sc-t1-leraar-klasoverzicht.png`, `sc-t2-leraar-code-detail.png`.
  - Leerling mobile 390 wide: `sc-p1-leerling-inloggen.png` (844), `sc-p2-leerling-vraag-info.png` (844), `sc-p3-leerling-hobbys-niet-leuk.png` (1212), `sc-p4-leerling-dit-ben-jij.png` (1047), `sc-p5-leerling-droombaan-checker.png` (1053). PDF A4: `sc-p6-leerling-pdf.png`. Chromebook 1366×768: `sc-p7-chromebook-vraag.png`.
  - Tone reference for the question bank: `vragen-voorbeelden.md`.
  - The mockups are a **layout and copy reference**. Schools, classes, codes, teachers and numbers are **Voorbeelddata**. The "Voorbeelddata" pill is mockup-only.
  - **Where the mockup and this spec differ, this spec wins.** Known differences:
    - **Search** in the top bar (s1–t2) is **not built** (deferred).
    - **"Nu bezig: gem. 24 min per test"** and **"Gem. tijd"** KPIs: build only from `PupilProgress.StartedAtUtc/CompletedAtUtc`; show "—" below 5 completed.
    - The PDF mockup (`sc-p6`) shows a paper line **"Naam (vul zelf in)"**. It is printed as an **empty line** and never filled by Lobsy. Nothing typed ends up there.
    - The **"Iets anders"** chip (p3) opens a one-word input with a warning (05.4); nothing else in the pupil flow accepts free text.
    - **Klasrapport (PDF)** on t1 is **deferred** (06 builds the per-code PDF only).
- **Design system.** Follow `.cursor/rules/design-system.mdc` plus these rules, which win when they conflict:
  - **School + leraar portal:** enterprise visual language (as the admin and werkgever redesign): 56 px `--brand-deep` top bar, 248 px grouped sidebar, breadcrumbs, desktop list density (44 px rows, `--text-sm`, tabular numbers). Reuse the shared enterprise primitives (Dependencies, check A). Mobile < 1024: same pages, stacked, with a 4-item bottom nav (01.6). Never create a second variant of a table/drawer/tabs/KPI primitive.
  - **Leerling:** the ontdekkingsreis visual language (lobster with 10 shell plates, ocean scene from tokens via `color-mix()`, speech bubble), **lighter and kid-friendly**, with no candidate BottomNav. If the ontdekkingsreis components exist on the branch (`git grep -n "Ontdekkingsreis" -- Jobsy.Web/Components`), reuse them. Otherwise build `Components/Leerling/Scene/*` here, using the mockup source `docs/mockups/scholen/base/ontdekkingsreis_build.py` as the reference. Mobile first; Chromebook 1366 uses the rail + card + lobster layout (`sc-p7`). Motion only with `prefers-reduced-motion` fallbacks.
  - **Colours:** tokens only (`app.css :root`), no new hex values, no inline `style=""` in `.razor`. Status pills pair colour **and** a label. Gold (`--gold*`) only for the lobster's new shell, the puzzle pieces and the droombaan "goal" step, exactly as the mockups use it.
  - **Type:** weights 400/600 (700 only for the page `h1`); only the type scale; spacing from `--space-*`. Pupil question text uses `--text-lg` (mobile) / `--text-xl` (≥ 1024). Tap targets ≥ 44 px everywhere in the pupil flow; answer buttons ≥ 56 px high.
  - **Layout:** breakpoints 640/900/1024 only; logical properties; chevrons flip under `[dir="rtl"]` (even though only nl is live now).
  - **Calm UI:** one primary action per card/drawer/screen; destructive actions are outline-danger, the confirming button inside the dialog is filled danger; no decorative emoji.
- **Strings (D12):** **Dutch only for now, but through the localization pattern.** All new UI text via `@Culture["…"]` in new nl-only modules:
  - `Localization/UiStringsScholen.cs` (prefixes `School.`, `Leraar.`, `Leerling.`, `AdminScholen.`)
  - `Localization/UiStringsLeerlingVragen.cs` (prefix `LeerlingQ.`, 05)
  - `Localization/UiStringsLeerlingVerhaal.cs` (prefixes `LeerlingStory.`, `LeerlingDroom.`, 06)

  Register them like the other modules (`UiStrings.cs`). Add **one** named exemption in `LocalizationParityReportTests` (`Every_key_exists_in_all_languages` + `Identical_to_nl_counts…`) for exactly these prefixes, with a comment pointing to D12. Other languages fall back to nl for these keys: verify `UiStrings.Get` falls back to nl; if it doesn't, add the fallback **only** for these prefixes. School/leraar/leerling pages render `lang="nl"` and hide the `LanguageSelector`. **No hardcoded Dutch in markup.**
- **Terminology (nl final):**

  | Use | Instead of |
  |---|---|
  | Schoolbeheerder | School admin, schooladmin |
  | Leraar | Docent, mentor (in UI labels) |
  | Leerling | Kandidaat, gebruiker (in the pupil flow) |
  | Code / leerlingcode | Account, login, ID |
  | Testvenster | Test window, periode |
  | Codelijst | Codekaartjes, namenlijst |

- **No names, anywhere (D3).** No entity, DTO, form, CSV, PDF or log field holds a pupil name, date of birth, e-mail, phone or address. The code list has an empty "Naam (vul zelf in)" column that Lobsy never fills. Reflection guard in 01 (01.8).
- **No AI, no partners, no vacancies in anything pupil-related (D9).** Pupil data is never sent to OpenAI or any other external service. Pupil pages never render `TrainingOffersBlock`, affiliate/partner links, vacancies, the banenkaart, `RoleFitCheckPanel` or employer information. Pupil data never enters candidate profiles, matching, `CandidateInsights`, `AssessmentNormSnapshotHostedService` norms, marketing e-mail or analytics events with identifiers. Guard tests in 04 and 06.
- **Authorization: server first (§R).** Every school/leraar page carries `[Authorize(Roles = …)]` equal to its §R row. Every school API resolves the school from the **signed-in user** (never from a route or body value alone). Every leraar API checks `TeacherClassAssignment` for the class id. Pupil APIs accept **only** the `Pupil` scheme (04). The UI hiding a button is **never** the only guard. The matrix test (01) enforces it.
- **Feature switch (D11):** `SchoolsEnabled` (default **false**). While false, `/school*`, `/leraar*`, `/leerling*` and `api/school|teacher|pupil/*` answer like a paused feature: 404 `feature_disabled`, via `[RequiresFeature(PlatformFeature.Schools)]` when the §F attribute exists (Dependencies, check C), else an equivalent check. Admin pages stay reachable. **The retention job (07) always runs, also when the switch is off.**
- **Audit:** admin settings changes and school creation go through `IAdminAuditLog` (admin redesign 07) when it exists, else an interim structured `PlatformLog` row (Dependencies, check B). Staff views of per-code detail and PDF downloads write `PersonalDataAccessLog` (01.4).
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (all new pages private, non-indexable; `PageSeoTests`), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `docs/security/roles-matrix.md` (replace the "Decaan — Not built" row with the new roles, see D1), `CHANGELOG.md`.
- **CSS:** new `wwwroot/css/features/scholen.css` (BEM blocks `sch-…` for school/leraar and `ll-…` for leerling), linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-scholen`, added to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Don't append to `app.css`. Shared enterprise primitives keep their own stylesheet.
- **Migrations:** `dotnet ef migrations add …` in `Jobsy.Infrastructure`, snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests` stay green.
- **Must NOT touch:**
  - candidate, employer and admin navigation catalogs beyond adding the admin "Scholen" item (01) and returning empty catalogs for the new roles
  - the existing tests' question banks and scoring results (05 may extract a shared scorer; adult results must stay byte-identical, with a test)
  - `RoleFitCheck*`, `TrainingOffersBlock`, affiliate code, `GratisDna*`, `CandidateConsentRules`
  - the MFA pages and `MfaEnforcementMiddleware` (only `MfaPolicy` gets the two roles)
  - Mollie/checkout, token pricing
  - `features/questionnaire.css`, banenkaart CSS/JS, `app-core.js`, the cookie banner
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 (school + leraar) and mobile 390 + Chromebook 1366 (leerling) of each new or changed screen
  - the new URL list
  - the §R rows this PR added or changed
  - test list
  - "Out of scope / deferred"
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched if you can; say so if you couldn't.

---

## §IA. Information architecture and URLs (the contract for all files)

**Label = page `h1` = breadcrumb**, from one catalog `Navigation/ScholenNav.cs` (01). Items whose page arrives in a later file are present with `IsAvailable = false` and **not rendered** until that file flips them.

### Schoolbeheerder (`SchoolLayout`, scope chip = school name, fixed)
| Group | Item (nl) | URL | Built in |
|---|---|---|---|
| Overzicht | Dashboard | `/school` | 01 shell, 02 content |
| | Te doen | `/school/te-doen` | 02 |
| Leerlingen | Klassen & codes | `/school/klassen` (+ `/school/klassen/{classId}`) | 02 |
| | Resultaten | `/school/resultaten?klas={classId}` | 02 |
| Team | Leraren | `/school/leraren` | 02 |
| School | Schoolgegevens | `/school/gegevens` (read-only; Lobsy admin edits) | 02 |
| | Privacy & ouders | `/school/privacy` (verwerkersovereenkomst status, ouderbrief template, confirmations per class) | 02 |
| | Lesbrief & materiaal | `/school/materiaal` (static lesbrief PDF + how-to) | 02 |

Sidebar footer: "Hulp voor scholen" + the line "Lobsy kent geen leerlingnamen. Alleen codes. De namenlijst houdt de school zelf."

### Leraar (`SchoolLayout`, scope chip = class switcher "Klas 2B ▾", only own classes)
| Group | Item (nl) | URL | Built in |
|---|---|---|---|
| Mijn klas | Klasoverzicht | `/leraar/klas/{classId}` | 03 |
| | Leerlingcodes | `/leraar/klas/{classId}/codes` | 03 |
| | Groepsresultaten | `/leraar/klas/{classId}/groep` | 03 |
| | Droombanen | `/leraar/klas/{classId}/droombanen` | 03 |
| | (no nav item) Code detail | `/leraar/klas/{classId}/code/{codeId}` | 03, PDF in 06 |
| In de les | Testvenster | `/leraar/klas/{classId}/testvenster` | 03 |
| | Codelijst & lesbrief | `/leraar/klas/{classId}/materiaal` | 03 |
| Mijn klassen | one item per assigned class | → `/leraar/klas/{id}` | 03 |

`/leraar` redirects to the first assigned class; with none: empty state "Je hebt nog geen klas. Vraag je schoolbeheerder om je aan een klas te koppelen."

### Leerling (`LeerlingLayout`, no nav, header = logo + "2B · K7Q-M2P" chip + Pauze)
| Screen | URL | Built in |
|---|---|---|
| Inloggen | `/leerling` | 04 |
| Welkom (how it works, 1 screen) | `/leerling/start` | 04 |
| Reis: current question | `/leerling/reis` | 04 shell, 05 content |
| Pauze-eiland (hobby's / niet leuk) | `/leerling/eiland` | 05 |
| Dit ben jij | `/leerling/dit-ben-jij` | 06 |
| Droombaan-checker | `/leerling/droombaan` | 06 |
| PDF (GET, download) | `/leerling/pdf` | 06 |
| Pauze / stoppen | `/leerling/stop` (POST, clears the cookie) | 04 |

### Lobsy admin
| Item | URL | Built in |
|---|---|---|
| Scholen (list + create) | `/admin/scholen` | 01 |
| School detail (data, e-mail domains, verwerkersovereenkomst, schoolbeheerders, counts) | `/admin/scholen/{schoolId}` | 01 |
| Scholen-rapportage (aggregates, retention runs) | `/admin/scholen/rapportage` | 07 |
| Settings (3 entries) | admin Functies catalog, group "Scholen" (Dependencies, check B) | 01 |

If admin redesign 01 landed, "Scholen" goes into its sidebar group **Organisaties** (after Bedrijven & vestigingen). Otherwise add one item to today's admin nav.

### API prefixes
`api/admin/schools/*` (`RequireAdmin`) · `api/school/*` (SchoolAdmin) · `api/teacher/*` (Teacher, plus SchoolAdmin where §R says) · `api/pupil/*` (Pupil scheme only; `api/pupil/login` and `api/pupil/schools`, `api/pupil/schools/{id}/classes` are anonymous + rate-limited).

---

## §R. Rights matrix (server-side contract; 01 encodes it, every file keeps it true)

● = allowed · ◯ = own scope only · — = 403. "Own school" = `User.SchoolId`; "own class" = a `TeacherClassAssignment` row.

| Page / action | Lobsy Admin | Schoolbeheerder | Leraar | Leerling (code session) |
|---|---|---|---|---|
| Create / edit / deactivate school, record verwerkersovereenkomst, e-mail domains | ● | — | — | — |
| Invite / remove schoolbeheerder | ● | — | — | — |
| Invite / remove leraar, assign classes | ● (support) | ◯ own school | — | — |
| Create / edit / delete class, set pupil count, generate codes | — | ◯ own school | — | — |
| Code list PDF/CSV (codes + empty name column) | — | ◯ all classes of own school | ◯ own classes | — |
| Confirm "ouders geïnformeerd" per class | — | ◯ own school | — | — |
| Open / close test window | — | ◯ own school | ◯ own classes | — |
| Nieuwe code (reset string, keep progress) | — | ◯ own school | ◯ own classes | — |
| Code verwijderen (delete all data of that code, objection) | — | ◯ own school | — | — |
| Class totals (counts, completion %, group results k ≥ 5) | aggregates only (07) | ◯ all classes of own school | ◯ own classes | — |
| Per-code status + progress | — | ◯ own school | ◯ own classes | own code |
| Per-code short result (interessecode, top-drijfveer, droombaan) | — | ◯ **only if `SchoolPerCodeResultsEnabled`** | ◯ own classes | own |
| Per-code story, likes/dislikes, droombaan route, PDF | — | — (unless also assigned as leraar of that class, D2) | ◯ own classes | own |
| Raw answers (60 Likert values) | — | — | — | own (to continue), never shown back as a list |
| Answer questions, hobbies, droombaan | — | — | — | ● while window open |
| View own result + PDF after completion | — | — | — | ● until retention (D8) |
| Admin settings (3), retention run log, aggregate reports | ● | — | — | — |
| Candidates, employers, sales, ambassadeurs | — (403 on every school/teacher/pupil endpoint) | | | |

The matrix test is data-driven from **one** table in `Jobsy.Tests/Scholen/ScholenRightsMatrix.cs` (page route × role → allow/deny; endpoint × actor → expected status, including **foreign school**, **foreign class**, **pupil of another class** and **setting off** cases). Each later file adds its rows.

---

## §D. Data model (01 builds it; later files only add what they name)

All ids `Guid`, all times UTC. **No name/contact fields for pupils anywhere.**

| Entity | Fields | Notes |
|---|---|---|
| `School` | `Id`, `Name`, `City`, `BrinCode?` (public school code), `AllowedEmailDomains` (json list, e.g. `["voorbeeldcollege.nl"]`), `IsActive`, `ProcessorAgreementSignedOn?` (DateOnly), `ProcessorAgreementVersion?`, `CreatedAtUtc`, `CreatedByUserId` | Appears in the pupil school dropdown only when `IsActive && ProcessorAgreementSignedOn != null` and it has ≥ 1 class with an open window |
| `User` (existing) | + `SchoolId?` | Set for `SchoolAdmin` and `Teacher` only. One school per staff account |
| `SchoolClass` | `Id`, `SchoolId`, `Name` (e.g. "2B", max 12, pattern `^[\p{L}\d\- ]+$`), `Level` (enum: vmbo-b, vmbo-k, vmbo-gt, mavo, havo, vwo, mix, anders), `Year` (1–6), `SchoolYearStart` (int, 2026 = "2026–2027"), `PupilCount` (1–40), `TestWindow` (enum `NotOpen/Open/Closed`), `TestWindowClosesOn?` (DateOnly), `ParentalInfoConfirmedAtUtc?`, `ParentalInfoConfirmedByUserId?`, `ParentalInfoTextVersion?`, `LoginPausedUntilUtc?`, `CreatedAtUtc` | Unique `(SchoolId, SchoolYearStart, Name)` |
| `TeacherClassAssignment` | `TeacherUserId`, `SchoolClassId`, `CreatedAtUtc` | PK both. Teacher and class must be in the same school (server check) |
| `PupilCode` | `Id`, `SchoolClassId`, `Number` (1..n, for the printed list), `CodeLookupHash` (HMAC-SHA256 of the normalized code with a server key), `CodeProtected` (Data Protection, purpose `Scholen.PupilCode`, so the list can be reprinted), `Status` (`NotStarted/InProgress/Completed`), `SessionVersion` (int), `LockedUntilUtc?`, `LastSeenAtUtc?`, `CreatedAtUtc` | Unique `(SchoolClassId, CodeLookupHash)`. Code format D5 |
| `PupilProgress` | `PupilCodeId` (PK), `AnswersJson` (item id → 1..5), `CurrentIndex`, `LikesJson` (chip keys), `DislikesJson`, `LikeOtherWord?` / `DislikeOtherWord?` (max 24 chars, 05.4), `DreamJobKey?`, `StartedAtUtc`, `UpdatedAtUtc`, `CompletedAtUtc?` | Raw answers stay here only; no staff API returns them |
| `PupilResult` | `PupilCodeId` (PK), `SchoolClassId`, `CompletedAtUtc`, `CompetenceScoresJson`, `RiasecScoresJson`, `HollandCode`, `ValuesScoresJson`, `TopValue`, `CultureScoresJson`, `TopCulture`, `ScoringVersion`, `StoryTemplateVersion`, `StoryKeysJson` (template keys, not text), `DreamJobKey?`, `FitSnapshotJson?` (keys + counts) | Rendered text is always regenerated from templates |
| `SchoolClassAggregate` | `Id`, `SchoolId`, `SchoolYearStart`, `ClassLabel` (snapshot "2B"), `Level`, `Year`, `PupilCount`, `StartedCount`, `CompletedCount`, `RiasecTop3CountsJson`, `TopValueCountsJson`, `TopCultureCountsJson`, `CompetenceBandCountsJson`, `DreamJobCountsJson`, `SnapshotAtUtc` | **No FK to `SchoolClass`/`PupilCode`**, so it survives deletion. Written only when `CompletedCount ≥ 5` (07) |
| `SchoolYearAggregate` | same counts summed per `SchoolId` + `SchoolYearStart` (and one platform-wide row with `SchoolId = null`) | 07 |
| `SchoolRetentionRun` | `Id`, `RanAtUtc`, `CutoffDate`, `ClassesDeleted`, `CodesDeleted`, `ResultsDeleted`, `AggregatesWritten`, `Outcome` | 07, admin reporting |

`PersonalDataAccessLog` (existing) gets one nullable column `SubjectPupilCodeId` (no FK) so staff views of per-code detail / PDF downloads are logged (resource `school.pupil-code`, actions `view` / `pdf`).

---

## §P. Privacy, safety and retention rules (every file)
- **Roles in AVG terms (D10):** the school is the **controller**, Lobsy the **processor**. Admin records the signed verwerkersovereenkomst (`ProcessorAgreementSignedOn` + version) before the school can open any test window (server 409 `processor_agreement_missing`).
- **Parental information (D6):** before a class's first test window opens, the schoolbeheerder confirms per class: "Ouders zijn geïnformeerd en kunnen bezwaar maken." (text versioned in `ParentalInfoTextVersion`). Without it, opening answers 409 `parental_info_missing`. `/school/privacy` offers a downloadable ouderbrief template (static text, nl). Objections are handled by the school: **Code verwijderen** deletes that code's data immediately.
- **Pseudonymous, not anonymous:** codes are linkable to a child **only** through the school's own list. Lobsy never receives that list. No name field exists (D3), and the code list export has an empty "Naam (vul zelf in)" column.
- **Data minimisation:**
  - staff never see raw answers
  - the schoolbeheerder sees per-code short results only while `SchoolPerCodeResultsEnabled` (D4)
  - group results only for groups of **≥ 5 completed** pupils (`SchoolAnonymity.MinGroupSize = 5`, a constant, not a setting)
  - dream jobs chosen by **< 2** pupils show as "Overig" in group views and aggregates
- **No external processing:** no AI, no analytics identifiers, no marketing, no partner/affiliate links, no vacancies, no employer visibility (D9).
- **Retention (D8):** individual data (`PupilCode`, `PupilProgress`, `PupilResult`, the `SchoolClass` itself, `TeacherClassAssignment` for that class) is **deleted** by the retention job on the school-year cutoff (default **31 July**, admin-configurable month + day) for every class whose `SchoolYearStart` year has ended. Aggregates (k ≥ 5) are snapshotted **before** deletion into the separate aggregate tables and kept. `PersonalDataAccessLog` rows keep their existing retention.
- **Pupil session:** separate `Pupil` cookie scheme, **session cookie** (no `Expires`, never persistent), `HttpOnly`, `Secure`, `SameSite=Strict`, idle timeout **20 min**, absolute **90 min**. One active session per code (`SessionVersion`). Shared-Chromebook friendly: "Pauze" ends the session.
- **Login abuse:** rate limits + lockout (04.3). Codes only let pupils answer while the class window is **Open**. After completion, pupils can still open their result + PDF (read-only) until retention.

---

## Decisions (defaults applied; Dennis can override any of them)
- **D1. Roles.** New `UserRole.SchoolAdmin = 8` (nl "Schoolbeheerder") and `UserRole.Teacher = 9` (nl "Leraar"). **No decaan role**: the "Decaan — Not built" row in `docs/security/roles-matrix.md` is replaced by these two roles. Both are in `MfaPolicy.IsRequired` (**mandatory 2FA**; Microsoft/Google school login counts as 2FA per ADR 0005). Neither is an employer role (`JobsyRoles.IsEmployer` false). *(Dennis, 29-09)*
- **D2. Leraar sees ONLY the classes assigned to them** via `TeacherClassAssignment`. No school-wide view for teachers. The invite has **no** role choice (no Mentor/Decaan): name, school e-mail, classes. A schoolbeheerder may also be assigned as leraar of a class, and then gets the leraar view of **that** class only. *(Dennis, 29-09)*
- **D3. No names anywhere.** Not even first names. Pupils are codes only; the school keeps the name list outside Lobsy. The code list PDF/CSV has an empty "Naam (vul zelf in)" column and the note "Lobsy bewaart geen namen". *(Dennis, 29-09)*
- **D4. Per-code results for the school:** admin setting `SchoolPerCodeResultsEnabled`, default **true**, **server-enforced**. When **off**, the school sees only per-class totals: the Resultaten page and every `api/school` result endpoint omit per-code result fields. Code **status/progress** stays visible in Klassen & codes because it's needed to manage the codes (hand out, reset, delete). The leraar is unaffected. *(Dennis, 29-09; the status nuance is a default)*
- **D5. Code format:** 6 characters from `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (30 characters; no I, L, O, 0, 1), shown as `K7Q-M2P`, input case- and dash-insensitive. Generated with `RandomNumberGenerator`, unique within the class. **Nieuwe code** replaces the string (old one stops working at once) and keeps progress.
- **D6. Parental information** is confirmed by the schoolbeheerder per class before the first window opens (see §P). No Lobsy e-mail consent flow for pupils (`CandidateConsentRules` is not used). *(Dennis, 29-09)*
- **D7. Test window:** opened/closed by the schoolbeheerder (all classes) or the leraar (own classes). Optional close date: the window auto-closes at 23:59 Europe/Amsterdam on that date. 60 questions fit in one lesson (~25 min); progress is saved after every answer, so a pupil can continue in a next lesson with the same code. *(Dennis, 29-09)*
- **D8. Retention:** individual pupil data is kept **max 1 school year** and auto-deleted at the cutoff. Default **31 July**, admin setting `SchoolRetentionCutoffMonth` / `SchoolRetentionCutoffDay`. Anonymous aggregates (k ≥ 5) are kept as separate records. *(Dennis, 29-09)*
- **D9. Pupil fit checker:** **no AI**, no partner links, no vacancies, **fixed templates** only. The pupil can only pick a dream job from `DreamJobCatalog` (+ "Weet ik nog niet"), so no free text is stored. *(Dennis, 29-09)*
- **D10. AVG roles:** school = controller, Lobsy = processor (verwerkersovereenkomst recorded by admin). *(Dennis, 29-09)*
- **D11. Feature switch `SchoolsEnabled`** default **false** (ships dark; Dennis turns it on once the first verwerkersovereenkomst is signed). Retention always runs.
- **D12. Dutch only**, via nl-only localization modules (§0 Strings), so other languages can be added later.
- **D13. Free text:** only one optional word per chip group ("Iets anders", max 24 letters, no digits or `@`), with the warning "Typ geen namen (ook niet je eigen naam), adres of telefoonnummer." Shown to the leraar as a chip. Everything else is chips.
- **D14. Staff invites** reuse the existing invite-token pattern (`SalesManagerInviteService` / `AmbassadeurInviteService`): e-mail link → set password → forced 2FA enrollment. The e-mail must match `School.AllowedEmailDomains` (server 400 `email_domain_not_allowed`). Microsoft/Google sign-in works when the address matches.
- **D15. Pupil results never feed adult systems:** no candidate profile, matching, norms, insights or talent pool (the talent pool is 18+ anyway).

## Dependencies (check before 01; say in PR 01 which case applied)
- **A. Shared enterprise UI primitives** (admin redesign 01 / werkgever redesign 01). Check: `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/Components/Ui/Enterprise Jobsy.Web/Components/Admin/Ui`.
  - **`Components/Ui/Enterprise/` exists:** reuse `EntDataTable`, `EntFilterBar`, `EntPager`, `EntTabs`, `EntKpiCard`, `EntDrawer`, `EntImpactNote`, `EntScopeChip`.
  - **Only `Admin*` exists:** move them to `Components/Ui/Enterprise/` exactly as werkgever README Dependencies B prescribes (own first commit, `git mv`, no behaviour change).
  - **Neither exists:** build the minimal `Ent*` set under `Components/Ui/Enterprise/` as werkgever 01.2 describes, and say in PR 01 that the admin/werkgever stacks must consume it.
  - Either way: **one** set.
- **B. Admin redesign 05 (settings catalog) + 07 (audit log).**
  - Check: `git grep -n "class PlatformSettingsCatalog" origin/acceptatie -- Jobsy.Web` and `git grep -n "interface IAdminAuditLog" origin/acceptatie -- Jobsy.Core`.
  - **Catalog present:** add group **"Scholen"** with the three entries (01.7).
  - **Catalog absent:** add a "Scholen" section to today's `/admin/settings` (`SettingsAdmin.razor`) with the same three controls, backed by `PlatformFeatureSettings` fields + `PUT api/settings/platform-features` (nullable fields = keep). Mark it `// moves into PlatformSettingsCatalog group "Scholen"`.
  - **Audit present:** `[AdminAudit("settings.platform.update")]` / `[AdminAudit("school.create")]` etc. **Absent:** interim structured `PlatformLog` row.
- **C. §F feature flags / `RequiresFeatureAttribute`** (`docs/mijn-paspoort` 01). Check: `git grep -n "class RequiresFeatureAttribute" origin/acceptatie -- Jobsy.Core`.
  - **Present:** add `PlatformFeature.Schools` and gate with `[RequiresFeature(PlatformFeature.Schools)]`.
  - **Absent:** use a `SchoolsFeatureGate` filter/page check reading `SchoolsEnabled`, plus a reflection guard test that passes vacuously on the attribute and enforces the gate check.
- **D. 2FA (`MfaPolicy`, ADR 0005)** is present at `a611db40` (`Jobsy.Core/Security/MfaPolicy.cs`). 01 only adds the two roles. If `MfaPolicy` moved or changed shape, keep its semantics and say so.
- **E. Ontdekkingsreis components** (pupil scene/lobster). Reuse if present (§0 Design); else build under `Components/Leerling/Scene/`.
- **F. QuestPDF** is present (`Directory.Packages.props`, `AssessmentReportPdfService`). 02 (code list) and 06 (report) use it.
- **Recommended landing order:** admin redesign 01 → (admin 05/07 optional) → this stack. Not a hard requirement: every case above has a fallback. Never branch from an unmerged branch of another stack.
