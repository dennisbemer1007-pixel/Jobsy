# 01. Foundation: roles, data model, code generator, rights matrix, admin "Scholen", settings, SchoolLayout

Read `00-README.md` first (§0 shared rules, §IA, §R, §D, §P, Decisions, Dependencies). Branch `cursor/scholen-1` from `origin/acceptatie`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/scholen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No pupil names anywhere, no AI / partner links / vacancies in anything pupil-related, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/scholen-1` (from `origin/acceptatie`) |
| PR title | `feat(scholen): foundation — SchoolAdmin/Teacher roles (2FA), school data model, admin Scholen + settings` |
| PR body starts with | `Stacked on: none (first in the stack)` + the Dependencies outcome (A–F, which case applied) |
| Mockups | none directly. Shell layout reference: the sidebar/top bar of `sc-s1-school-dashboard.png` and `sc-t1-leraar-klasoverzicht.png` |
| Split seam | **01a** = roles + MfaPolicy + entities + migration + code generator + rights-matrix test scaffold + guards (01.1–01.5, 01.8). **01b** = admin Scholen pages + settings + SchoolLayout/nav + staff invite plumbing (01.6, 01.7, 01.9) |

## Goal
Everything later files build on: two new staff roles with mandatory 2FA, the complete data model from §D (including 07's aggregate tables, created **here** so the migration chain stays simple), a secure code generator, the §R matrix test scaffold, the admin pages to create schools and invite the first schoolbeheerder, the three admin settings, and an empty `SchoolLayout` with the nav catalog. After 01 an admin can create a school, record the verwerkersovereenkomst and invite a schoolbeheerder, who can sign in with forced 2FA and lands on an empty `/school` dashboard.

## 01.1 Today (verify first)
- `Jobsy.Core/Enums/UserRole.cs`: `Candidate=0 … Ambassadeur=7`. `Jobsy.Core/Authorization/JobsyRoles.cs` has the role name constants and `EmployerRoles`.
- `Jobsy.Core/Security/MfaPolicy.cs`: `IsRequired` covers Admin + BranchManager/RegionalManager/EnterpriseManager/Intermediary. Tests: `Jobsy.Tests/MfaForcedEnrollmentTests.cs`.
- `Jobsy.Core/Entities/User.cs` has `Role`, `CompanyId`, the parental consent fields (candidate-only; **not** used for pupils).
- Invite services: `Jobsy.Infrastructure/Services/SalesManagerInviteService.cs`, `AmbassadeurInviteService.cs` (token hash + expiry + set-password page).
- `PlatformFeatureSettings` entity + `PUT api/settings/platform-features`; `/admin/settings` = `Components/Pages/Admin/SettingsAdmin.razor`.
- `Jobsy.Core/Entities/PersonalDataAccessLog.cs` + `IPersonalDataAccessLogger`.
- `Jobsy.Infrastructure/Security/IbanProtector.cs`: the `IDataProtector` pattern to copy for codes.
- `Jobsy.Web/Navigation/RoleNavCatalog.cs`: per-role nav catalogs (snapshot-tested).
- `docs/security/roles-matrix.md` contains a "Decaan — Not built" row.

## 01.2 Roles + 2FA
- `UserRole.SchoolAdmin = 8`, `UserRole.Teacher = 9` (append, never renumber). `JobsyRoles.SchoolAdmin = "SchoolAdmin"`, `JobsyRoles.Teacher = "Teacher"`, plus `JobsyRoles.SchoolStaff` (both). **Not** in `EmployerRoles`.
- `MfaPolicy.IsRequired(SchoolAdmin|Teacher) == true`. Tests:
  - an extended `MfaForcedEnrollmentTests` case: a local-password Teacher is forced to `/account/2fa/instellen` before `/leraar`
  - an Entra/Google-signed-in Teacher is not forced (ADR 0005)
- Role display names (nl): "Schoolbeheerder", "Leraar". Admin user lists show them, and admin 2FA reset works for them (existing flow, just verify).
- `RoleNavCatalog`: the new roles get **no** candidate/employer items. Their nav comes from `ScholenNav` (01.6). Snapshot tests for existing roles unchanged.
- Post-login redirect: SchoolAdmin → `/school`, Teacher → `/leraar`. Both are blocked (403 page) from candidate and employer areas, and vice versa.
- `docs/security/roles-matrix.md`: replace the Decaan row with SchoolAdmin + Teacher (scope, 2FA mandatory, "sees only assigned classes"). Add a short ADR `docs/adr/0006-school-roles-and-pupil-codes.md` summarising D1–D10.

## 01.3 Entities + migration
- Add every entity of §D (`School`, `User.SchoolId`, `SchoolClass`, `TeacherClassAssignment`, `PupilCode`, `PupilProgress`, `PupilResult`, `SchoolClassAggregate`, `SchoolYearAggregate`, `SchoolRetentionRun`) and `PersonalDataAccessLog.SubjectPupilCodeId`. Namespaces: `Jobsy.Core.Entities.Scholen`, enums in `Jobsy.Core.Enums` (`SchoolLevel`, `TestWindowState`, `PupilCodeStatus`).
- EF config:
  - cascade delete `School → SchoolClass → PupilCode → PupilProgress/PupilResult`, and `SchoolClass → TeacherClassAssignment`
  - `User.SchoolId` FK with `Restrict`
  - aggregates have **no** FK to classes/codes (only `SchoolId` with `SetNull`), so deleting classes never removes them
- Indexes: `PupilCode(SchoolClassId, CodeLookupHash)` unique, `SchoolClass(SchoolId, SchoolYearStart, Name)` unique, `SchoolClass(TestWindow)`, `SchoolClassAggregate(SchoolId, SchoolYearStart)`.
- One migration `AddScholenFoundation`. Guards green.

## 01.4 Code generator + protector
- `Jobsy.Core/Scholen/PupilCodeFormat.cs`:
  - alphabet (D5)
  - `Normalize(string input)`: upper-case, strip spaces and dashes, then **reject** anything outside the alphabet (no silent lookalike mapping)
  - `Display(string code)` → `K7Q-M2P`
  - `IsWellFormed`
- `Jobsy.Infrastructure/Scholen/PupilCodeService.cs`:
  - `Generate(int count, SchoolClass)` uses `RandomNumberGenerator.GetItems`, unique within the class, retries on collision
  - `LookupHash(code)` = HMAC-SHA256 with a key from Data Protection / configuration `Scholen:CodeHmacKey`. In Development it's generated and persisted like the other DP keys. A missing key in Production fails at startup with a clear message.
  - `Protect/Unprotect` (purpose `Scholen.PupilCode`) so the list can be reprinted
  - `Replace(PupilCode)` ("Nieuwe code"): new string, `SessionVersion++`, progress kept
- `IPersonalDataAccessLogger` overload for `SubjectPupilCodeId` (resource `school.pupil-code`).

## 01.5 Scope services (single source of truth for §R)
- `Jobsy.Core/Scholen/SchoolScope.cs` + `Jobsy.Infrastructure/Scholen/SchoolScopeService.cs`:
  - `GetSchoolIdOrThrow(user)`
  - `CanManageClass(user, classId)` (SchoolAdmin of that school)
  - `CanTeachClass(user, classId)` (TeacherClassAssignment exists; a SchoolAdmin counts only if assigned, D2)
  - `CanSeeClassTotals`
  - `CanSeePerCodeShortResult(user, classId)` (teacher-of-class **or** SchoolAdmin && `SchoolPerCodeResultsEnabled`)
  - `CanSeePerCodeDetail` (teacher-of-class only)

  Every later controller/page calls these; no inline role logic.
- `Jobsy.Core/Scholen/SchoolYear.cs`: `Current(DateOnly today, cutoffMonth, cutoffDay)` → `SchoolYearStart` (e.g. 29-09-2026 → 2026; 15-07-2027 → 2026; 01-08-2027 → 2027 with the 31-07 default), `Label(start)` → "2026–2027", `EndsOn(start, cutoff)` → 31-07-2027. Used by 02 (default year), 04 (dropdown) and 07 (retention).
- `SchoolAnonymity.MinGroupSize = 5`, `SchoolAnonymity.MinDreamJobCount = 2` (constants in Core).
- `Jobsy.Tests/Scholen/ScholenRightsMatrix.cs`: the data-driven table + runner (WebApplicationFactory). 01 fills the admin rows and the "foreign school" / "candidate/employer gets 403" rows. Later files append.

## 01.6 SchoolLayout + nav catalog
- `Components/Layout/SchoolLayout.razor`: enterprise top bar (logo, product label "Lobsy voor scholen", role chip, account menu) + grouped sidebar from `Navigation/ScholenNav.cs` (§IA tables, `IsAvailable` flags). Leraar scope chip = class switcher (populated in 03). Sidebar footer with the no-names line (§IA). Mobile < 1024: sidebar becomes a sheet + 4-item bottom nav (School: Dashboard, Klassen, Resultaten, Meer; Leraar: Klas, Codes, Groep, Meer).
- Pages `/school` (SchoolAdmin) and `/leraar` (Teacher) exist with an empty state only ("Hier komt je dashboard"). Private, non-indexable, help docs, ROUTES.md.
- `lang="nl"`, no `LanguageSelector` (§0 Strings). `UiStringsScholen.cs` + parity exemption (§0) land here.
- `wwwroot/css/features/scholen.css` created + linked + asset version.

## 01.7 Admin: Scholen + settings
- `/admin/scholen`: table (Naam, Plaats, BRIN, Status (Actief/Inactief pill), Verwerkersovereenkomst (datum or "Ontbreekt" warning pill), Klassen, Leraren) + primary "School toevoegen" drawer (Naam, Plaats, BRIN optional, E-maildomeinen (chips, validated as domains), Actief).
- `/admin/scholen/{id}`:
  - tabs **Gegevens** (edit), **Verwerkersovereenkomst** (date + version + short note "De school is verwerkingsverantwoordelijke, Lobsy is verwerker."), **Schoolbeheerders** (invite by name + e-mail within the allowed domains; list with 2FA status; remove), **Overzicht** (counts only: classes, codes, completed; no per-code data)
  - "School deactiveren" (outline-danger + confirm): blocks logins of staff and pupils; the data stays until retention
- API `api/admin/schools` (CRUD, invite SchoolAdmin, agreement), `RequireAdmin`, audited (Dependencies B).
- Settings (Dependencies B, group **"Scholen"**):
  - `SchoolsEnabled` (switch, default **false**, help: "Zet scholen, leraren en leerlingen aan of uit. Bewaartermijn blijft altijd draaien.")
  - `SchoolPerCodeResultsEnabled` (switch, default **true**, help: "Schoolbeheerders zien resultaten per code. Uit: alleen totalen per klas (vanaf 5 leerlingen). Leraren zien altijd hun eigen klas.")
  - `SchoolRetentionCutoff` (month + day selects, default **31 juli**, help: "Op deze dag worden alle leerlinggegevens van het afgelopen schooljaar verwijderd. Anonieme totalen blijven.")

  Persist on `PlatformFeatureSettings` (`SchoolsEnabled bool`, `SchoolPerCodeResultsEnabled bool`, `SchoolRetentionCutoffMonth int`, `SchoolRetentionCutoffDay int`; validate the date, e.g. reject 31-02). Nullable in the PUT = keep.
- Feature gate (Dependencies C) wired to `/school*`, `/leraar*`, `/leerling*` + their APIs; test both states.

## 01.8 Guards
- `NoPupilNameFieldsTests`: reflection over every type in `Jobsy.Core.Entities.Scholen` + every DTO in `Jobsy.Core.Contracts.Scholen`. Fail on property names matching `(?i)(name|naam|voornaam|achternaam|firstname|lastname|email|phone|telefoon|birth|geboorte|address|adres)`, except the allow-list `School.Name`, `SchoolClass.Name`, `ClassLabel`, `SchoolName` and `ClassName` on DTOs, and staff (`TeacherDisplayName` on staff DTOs).
- `ScholenNoExternalProcessingTests`: the assemblies' pupil services (namespace `Jobsy.*.Scholen.Pupil*`) don't reference `IOpenAi*`, `HttpClient`, `IEmailSender`, `ITrainingOffer*`, `IVacancy*`, `IAnalytics*` (reflection on constructor parameters). Extended in 04/06.
- `BlazorPageRoleAttributesTests` rows for `/school`, `/leraar`, `/admin/scholen*`.

## 01.9 Staff invites (plumbing, UI in 02)
- `SchoolStaffInviteService` (pattern D14): `InviteSchoolAdminAsync(schoolId, name, email)` (admin), `InviteTeacherAsync(schoolId, name, email, classIds)` (SchoolAdmin; UI in 02). Token hash + 7-day expiry, set-password page reuse, then forced 2FA. The domain check is server-side. Re-invite and revoke. E-mail template nl: "Je bent uitgenodigd voor Lobsy voor scholen bij {School}. Tweestapsverificatie is verplicht."

## Tests
- MFA: forced enrollment for local SchoolAdmin/Teacher; external login not forced.
- Code generator:
  - 10.000 codes are alphabet-only, 6 chars, unique per class
  - `Normalize` accepts `k7q-m2p`, ` K7Q M2P `, rejects `K7O-M2P`
  - `Replace` invalidates the old hash and bumps `SessionVersion`
- Scope service unit tests for every §R cell touched here (incl. setting off, SchoolAdmin-as-teacher).
- Admin API: create/edit/deactivate, agreement, invite SchoolAdmin (domain mismatch 400), audited, non-admin 403.
- Settings: defaults, validation, persisted, gate on/off.
- Guard tests (01.8), EF guards, localization parity with the exemption, asset version, routes/SEO/help.

## Success criteria
- `dotnet build` + `dotnet test` green; migration applies on an empty DB and on a copy of acceptatie's schema.
- An admin creates "Voorbeeldcollege", records the agreement, invites a schoolbeheerder. The invitee sets a password, is forced through 2FA and lands on the empty `/school`.
- With `SchoolsEnabled = false`, `/school` returns 404 `feature_disabled` (admin pages still work).
- No pupil name-like field exists (guard green). The roles matrix doc has no Decaan row.

Done → next: `02-school-portaal.md`.
