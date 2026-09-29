# 06 · De ontdekkingsreis, part 1: new profile fields, private dislikes, migration, and showing them in the paspoort

> Read `00-README.md` first: the §0 rules apply, plus §F (the paspoort flag gates everything the candidate sees here). Only start this file once the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/ontdekkingsreis-1`, created from `cursor/mijn-paspoort-4` (file 05's branch; `cursor/mijn-paspoort-4b` if 05 was split) |
| PR | ONE PR into `acceptatie`, titled `feat(ontdekkingsreis): new profile fields + private dislikes`. The body starts with `Stacked on #<PR of 05> (cursor/mijn-paspoort-4)` |
| Mockups | `docs/mockups/ontdekkingsreis/`: `or-d3-werk.png` (employer preferences), `or-d4-waar-houd-ik-niet-van.png` (dislikes). The Mijn Paspoort mockups show where the fields appear. |

## Goal
File 07 introduces the wizard, and it collects new information. This file adds that information to the data model **before** the wizard exists, so 07 only builds UI:
- The new fields are stored cleanly.
- They are validated, exported with privacy data and deleted with the account.
- They show up in the paspoort where they fit.
- Dislikes stay **private**: they are never visible to employers, and they are never exposed through any employer-facing path.

## 06.1 Where the data lives (reuse first)
Today the candidate profile is `User.PreferencesJson` ↔ `Core/Contracts/CandidateContracts.cs` `CandidatePreferencesDto`. It already holds AboutMe, DrivingLicenses, Employers, Educations, Certificates, Availability, Roles and more, and it is saved through the existing profile update (`MeController` / `CandidateProfileService` / `CandidateProfileEditor` from 05).

**Profile fields. Extend `CandidatePreferencesDto`** by adding trailing optional parameters, so existing JSON still deserializes:

| Field | Type | Values | Limit |
|---|---|---|---|
| `SpokenLanguages` | `IReadOnlyList<CandidateLanguageDto>?` | `CandidateLanguageDto(string Code, string? Level)`. `Code` is ISO 639-1 (`nl`, `en`, `ar`, `tr`, `pl`, `ro`, `uk`, `so`, `ti`, `fa`, `es`, `fr`, `de`, or other valid codes). `Level` uses the same codes as `DutchLevel`. | max 8 |
| `DutchLevel` | `string?` | `beginner` (A1–A2), `basis` (B1), `goed` (B2), `vloeiend` (C1–C2), `moedertaal` | |
| `EmployerPreferences` | `IReadOnlyList<string>?` | `small-team`, `large-company`, `fixed-workplace`, `learn-on-the-job`, `dutch-support`, `growth`, `close-to-home`, `variety` | |
| `LearningGoals` | `IReadOnlyList<string>?` | free text | max 5, each ≤ 60 chars |
| `Hobbies` | `IReadOnlyList<string>?` | catalog codes and/or free text: `sport`, `music`, `cooking`, `gaming`, `crafts`, `nature`, `reading`, `caring`, `tech`, `art`, `volunteering`, `fashion` | max 10, free text ≤ 40 chars |

`DutchLevel` is stored separately because it matters for Dutch B1 support.

- **No schema change** for these: they live in the existing `PreferencesJson` column.
- Add a JSON round-trip test that **old JSON without the fields** still loads and saves unchanged.
- The codes and their localized label keys go in `Jobsy.Core/Rules/DiscoveryCatalogs.cs`, with `Discovery.Lang.*`, `Discovery.Dutch.*`, `Discovery.Employer.*` and `Discovery.Hobby.*`.
- Language names come from `CultureInfo` in the current UI culture, so they need no string keys.

**Private field. Dislikes get their own table** instead of `PreferencesJson`. Several employer-facing services read `PreferencesJson` (`TalentPoolService`, `ApplicationsController`, `CandidateInsightsComputer`/`Service`, `CandidateMatchSnapshotService`, `VacanciesController`), so a separate table keeps dislikes out of those paths by design.
- **New entity** `Jobsy.Core/Entities/CandidatePrivatePreferences.cs`:
  - `UserId` (PK/FK → `User`, cascade delete)
  - `DislikesJson` (`string`, default `"[]"`)
  - `CustomDislikesJson` (`string`, default `"[]"`)
  - `UpdatedAtUtc`
- **Codes:** `night-shifts`, `heavy-lifting`, `working-alone`, `phone-customers`, `noise`, `cold-outdoor`, `computer-work`, `changing-hours`, `crowded`, `long-travel`. Custom dislikes: max 5, each ≤ 40 chars.
- **EF:** `DbSet` + config + migration `AddCandidatePrivatePreferences`. `EfModelSnapshotTests`, `PendingModelChangesTests` and `EfMigrationDiscoveryTests` must stay green.
- **API:** `GET` / `PUT api/me/private-preferences` on `MeController`, or a small `CandidatePrivatePreferencesController`.
  - `[Authorize(Roles = Candidate)]`, own user only.
  - No admin read endpoint. Admin sees only aggregate counts, if anything.
  - Web client methods live in `JobsyApiClient.Me.cs`.
- **Privacy:**
  - `PrivacyDataService` includes the row in the candidate's data export and deletes it on account deletion (cascade plus an explicit test).
  - `PersonalDataAccessLog` is not needed, because nobody but the candidate reads it.
- **Guard test (the important one):** a reflection/serialization test that none of these contains a property or JSON field named `Dislike*`, `CustomDislike*` or `PrivatePreferences*`:
  - the employer-facing DTOs (talent pool, applications, candidate insights, public candidate/CV views, Lobsy-CV `LobsyCvContracts`)
  - the `PreferencesSummary` column (see migration `20260728120000_ExpandPreferencesJsonAndPreferencesSummary`)
  - the AI prompt builders for employer features

## 06.2 Validation (one place)
`CandidatePreferencesValidator` (Core/Rules). Extend the existing validator if there is one; otherwise add a new one used by the profile update:
- It enforces the codes, the limits and the lengths from 06.1.
- It trims and dedups, case-insensitively.
- It drops unknown codes with a log. It does not return 400, so an old client never loses a save.
- Dislikes use the same rules in `CandidatePrivatePreferencesValidator`.

## 06.3 Where the fields appear (paspoort ON only; classic profile unchanged, D9)
- **Passport column** (02.3). This replaces the "spoken languages deferred" rule. Add a fact **"Talen"**: the Dutch level as text ("Nederlands: goed") plus up to 2 more languages + "+n". Empty: "—" with an "aanvullen" link. Mobile: not in the compact card; it lives in `data`.
- **Mijn DNA** (02.7), "Jouw verhaal" card: a chip row **"Waar word je blij van"** with the hobbies (max 4 + "+n"). It is hidden when empty. No empty state; the card shouldn't nag.
- **Bewijzen** (05.2), "Opleiding & certificaten" card: a line **"Wat ik nog wil leren"** with the learning goals as `kompas-chip`s (max 3).
- **Carrière and course blocks** (03.3/04.2): `CourseSlotRules` gets the learning goals as **extra context keys**. They match through the existing `TrainingMatchRules`, and the gaps still rank higher. That's reuse, not new logic.
- **Past deze baan?** (04.1): unchanged. Employer preferences are **not** shown as a fit result, because there is no employer-side data to compare against yet (D10).
- **Mijn gegevens** (05.3): all fields are editable through the existing section/editor pattern:
  - "Persoonlijk" gains **Talen** (a language picker with a level per language) and **Niveau Nederlands** (5 radio rows with plain labels, e.g. "Beginner: ik leer het net", "Basis: ik red me in gewone gesprekken", "Goed", "Vloeiend", "Moedertaal").
  - "Voorkeuren & reistijd" gains **"Wat voor werkgever zoek ik?"** (chips).
  - A new accordion, **"Wat ik leuk vind en wil leren"**, holds the hobbies and learning goals.
  - A new accordion, **"Waar houd ik niet van"**, has a lock icon and the always-visible note **"Alleen voor jou en je matches. Werkgevers zien dit niet."** It holds the chips + "Iets anders", saved through the private endpoint.
- **Never show** dislikes anywhere except that accordion and wizard step 6 (07). That excludes the passport card, Lobsy-CV, the employer views and the share views.

## 06.4 Using dislikes to filter (small, candidate-side only)
`DislikeMatchRules` (Core/Rules, pure) maps only the dislike codes that map to real vacancy data:
- `night-shifts` → **down-rank** vacancies with `Vacancy.LegalNightShift23To06 == true` in the candidate's own vacancy lists and match ordering (the Kompas `TopMatches`, Match/swipe order, the paspoort vacancies card).
- Down-rank means "sort later", **not** hide.
- Down-ranking **never** changes the match % that employers see. It never enters the employer-side scores, the talent pool or the insights.
- All other codes are stored only (documented in the rule as "not used yet"). **Don't invent** vacancy attributes.
- When Werkgevers actief is OFF, there are no vacancy lists, so this doesn't apply.

## 06.5 Strings
- `Discovery.Lang.*`, `Discovery.Dutch.*` (label + one-line explanation), `Discovery.Employer.*`, `Discovery.Hobby.*`, `Discovery.Dislike.*`
- `Discovery.Dislike.PrivateNote`
- `Passport.Facts.Languages`, `Passport.Dna.Joy`, `Passport.Proof.WantToLearn`
- `Profile.Section.JoyAndLearning`, `Profile.Section.Dislikes`

All in nl/en/pl/ro/ar, in `UiStringsDiscovery.cs` (registered in `UiStrings.cs`).

## Tests
- **DTO:** old `PreferencesJson` without the new fields round-trips unchanged; new fields round-trip.
- **Validators:** limits, unknown codes dropped, trimming and dedup; a free-text length over the limit is truncated or dropped as specified.
- **Private table:**
  - migration up/down
  - `GET`/`PUT` works for the own user only (another user's id → 403/404; not reachable by Employer or Admin roles)
  - included in the privacy export
  - deleted with the account
- **Guard:** the reflection/serialization test from 06.1 (no dislike data in any employer-facing DTO, the summary or the Lobsy-CV).
- **`DislikeMatchRules`:** night-shift vacancies sort after the others with an equal score; the employer-side score is unchanged; other codes have no effect.
- **bUnit:**
  - the passport card "Talen" fact (filled / empty)
  - the Mijn DNA hobbies row hidden when empty
  - the Bewijzen learning-goals line
  - the Mijn gegevens accordions save through the editor / private endpoint
  - the dislikes accordion shows the private note
- **Flag OFF:** the classic `Profile.razor` markup is unchanged (the existing snapshot/selector tests pass).

## Success criteria (all must hold before you open the PR)
- All new fields are stored and validated. Old profiles load and save unchanged.
- Dislikes live only in `CandidatePrivatePreferences`, are reachable only by the candidate, and are in the privacy export and the account deletion. The guard test proves no employer-facing path contains them.
- With the paspoort ON, the languages, hobbies and learning goals appear as described and are editable in Mijn gegevens. With the paspoort OFF, nothing visible changes.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has the stacked-on line, what and why, screenshots (the passport card with Talen, the Mijn gegevens accordions at desktop + mobile), the test list, and "Out of scope / deferred".

## Done → next
Push, open the PR and note its number. Then continue with **`07-ontdekkingsreis-wizard-stappen-1-6.md`**. If anything above is red, stop and report (see 00-README "How to run" step 3).
