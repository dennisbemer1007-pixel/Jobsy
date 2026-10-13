# 02: Data model: shareable work preferences, work region, own car, contract preference, verified flags, feature flags

**Stacked.**
- Branch: `cursor/paspoort-partners-2` from `cursor/paspoort-partners-hotfix` (or `origin/acceptatie` once PR 01 is merged).
- ONE PR into `acceptatie`, titled **"feat(paspoort): shareable work preferences, work region, contact verification flags + partner/PDF v2 flags"**.
- Rules: see README.

Mockup: `docs/mockups/paspoort-partners/a-recruiterpagina-p1.png`. The fields marked `NIEUW` come from this step, except housing, which is **out of v1**.

## Goal
Add the data the new passport needs (decisions 1, 2, 6) without showing anything new yet. With the flags OFF, nothing user-visible changes, except the new optional fields in the passport Data tab, which are gated by `PassportPdfV2Enabled`.

## Facts (checked on `85a43263`)
- Preferences live as JSON in `User.PreferencesJson` → `CandidatePreferencesDto` (`Jobsy.Core/Contracts/CandidateContracts.cs` L3–43).
  - They are saved via `PUT api/me/profile` (`MeController.UpdateProfile` ~L147, merged and serialized ~L312).
  - Already present: availability matrix (`Ma..Zo` × `Ochtend/Middag/Avond/Nacht`, `DayPartMatrix`), hours, `FlexibleTimes`, `SpokenLanguages` (+ level), `DutchLevel`, `DrivingLicenses`, `PreferredTransport`, `MaxTravelMinutes`, `Roles`, `Employers`, `Educations`, `Certificates`, `LearningGoals`, `EmployerPreferences` (codes in `DiscoveryCatalogs.EmployerPreferenceCodes`), `HomeAddress`.
  - `User.AvailableFromDate` already exists.
- **Shifts need no new field.** They are derived from the availability matrix (step 04).
- **Private dislikes** (`CandidatePrivatePreferencesDto`, codes `night-shifts`, `heavy-lifting`, `cold-outdoor`, …) are documented "Never employer-facing". **Keep it that way.** The new shareable fields are separate, positive choices.
- There is no verified flag for candidate e-mail or telephone on `User`.
  - E-mail is proven by the passwordless code login (`AuthController.VerifyEmailCode` ~L687) and by external IdPs (`AuthServiceCollectionExtensions` ~L1753–1772 rejects `email_verified=false`).
  - Telephone is only format-validated (`CandidatePhoneRules`, `MeController` ~L209–217).
- There is no SMS provider in the solution.
- Feature flags: `PlatformFeature` (`Employers`, `CandidatePassport`), `PlatformFeatureSettings`, documented in `docs/feature-flags.md`.

## Scope

### Feature flags (new, all default false)
- `PlatformFeatureSettings.PassportPartnersEnabled` (+ `PlatformFeature.PassportPartners`)
- `PlatformFeatureSettings.PassportPdfV2Enabled` (+ `PlatformFeature.PassportPdfV2`)
- `PlatformFeatureSettings.PhoneVerificationEnabled`: a setting, not a route gate

Wire them through `IFeatureFlags` / `IPlatformFeatureService`, the admin feature-flag toggles (`UiStringsFeatureFlags`, 5 languages) and `GET api/settings/feature-flags`, and document them in `docs/feature-flags.md`.

`PassportPartners` implies nothing on its own. Steps 03/05–08 gate their routes and controllers with `[RequiresFeature(PlatformFeature.PassportPartners)]` and step 04 gates with `PassportPdfV2`.

### Shareable preferences (JSON, no migration)
Extend `CandidatePreferencesDto` with optional members. Defaults are `null`, so older JSON stays valid.

- `SharedWorkPreferences? WorkPreferences`, a new record:
  - `Indoor`: `prefer | ok | rather-not`
  - `Outdoor`: `prefer | ok | ok-not-frost | rather-not`
  - `PhysicalWork`: `light | standing | lifting-15 | lifting-25`
  - `Pace`: `norm-ok | calm`
- The working environment re-uses the existing `EmployerPreferences` codes. They are shown on the passport only when `ShareEmployerPreferences == true` (new bool, default false).
- `string? WorkRegion`: max 60 chars, trimmed, no digits (no postcode).
  - Suggested default: `"{City} e.o."` from `LobsyCvModelFactory.ExtractCity(HomeAddress)`.
  - The UI pre-fills the suggestion. It is stored only when the candidate saves.
- `bool? HasOwnCar`
- `IReadOnlyList<string>? ContractPreferences`: codes `uitzend`, `vast`, `tijdelijk`, `seizoen`, `oproep`, `geen-voorkeur` (max 3; `geen-voorkeur` is exclusive)
- New catalog `Jobsy.Core/Rules/WorkPreferenceCatalogs.cs`:
  - code arrays and `Canonical*`/`IsKnown*` helpers in the style of `DiscoveryCatalogs`
  - label keys `WorkPref.*`, `Contract.*`
- **Normalization on save** (`MeController.UpdateProfile` merge):
  - unknown codes are dropped
  - `WorkRegion` is sanitized (strip digits, collapse whitespace, max 60)
  - a test asserts `HomeAddress` is never copied into `WorkRegion` verbatim when it contains a postcode
- **No housing field** (decision 2).

### Contact verification (migration)
- **Migration `AddPassportV2Foundation`** adds:
  - `User.EmailVerifiedAtUtc` (timestamptz, null)
  - `User.PhoneVerifiedAtUtc` (timestamptz, null)
  - `User.PhoneVerifiedE164` (varchar 20, null): the normalized number that was verified
  - the 3 flag columns on `PlatformFeatureSettings`
- **E-mail:**
  - Set `EmailVerifiedAtUtc` (if null) on a successful `VerifyEmailCode` and on an external IdP login for a candidate.
  - **Backfill:** first confirm, by reading every candidate-creation path in `AuthController`, that each path proves possession of the e-mail (email code or IdP).
    - If all do, the migration sets `EmailVerifiedAtUtc = COALESCE("LastLoginAtUtc", "TermsAcceptedAt")` for `Role = Candidate AND IsActive AND IsTestAccount = false AND LastLoginAtUtc IS NOT NULL`.
    - If any path does not prove possession, **no backfill**: note it in the PR, and verification then happens at the next code login.
- **Telephone:**
  - New `IPhoneVerificationService`: start sends a 6-digit code and stores only a hash, re-using `VerificationCodes` attempt/lockout rules; verify checks the code.
  - New `ISmsSender` with `SmsSenderStub` (logs the code in Development, no-op elsewhere).
  - **Decided (decision 22):** phone verification stays **OFF** (`PhoneVerificationEnabled = false`) until an SMS provider is chosen. Build only the stub and the switch. While OFF, the UI hides "Bevestig telefoon" and "Lobsy-geverifieerd" = 4/4 tests + verified e-mail.
  - Changing `User.PhoneNumber` to a different normalized number clears `PhoneVerifiedAtUtc` / `PhoneVerifiedE164`, both in `UpdateProfile` and in `CvProfileMerge`, ~L874.
  - Endpoints: `POST api/me/phone-verification/start` and `POST api/me/phone-verification/verify`, rate-limit policy `otp-verify`.
- `MeProfileDto` gains `EmailVerified` and `PhoneVerified` (bools), plus the new preference members.

### UI (only when `PassportPdfV2Enabled`)
`Jobsy.Web/Components/Candidate/Passport/PassportDataTab.razor` gets a new section **"Dit deel ik met werkgevers en bureaus"**, id `shared`, placed after `preferences` in `Sections` (~L174):
- work preferences as chip groups
- `ShareEmployerPreferences` toggle (+ existing employer-preference chips)
- work region (pre-filled suggestion, editable)
- own car (yes/no)
- contract preferences
- a helper line: "Dit staat op je paspoort. Wat je níet leuk vindt blijft privé." The `dislikes` section stays separate and unchanged.
- e-mail verified state (✓ or "Bevestig je e-mail" → code flow)
- telephone verified state (hidden unless `PhoneVerificationEnabled`)

All new strings go in `UiStringsPassport` / a new `UiStringsPassportPartners.cs`, in 5 languages.

## Tests
- **Unit:**
  - the catalogs (canonicalization, unknown codes dropped, `geen-voorkeur` exclusive, max 3)
  - `WorkRegion` sanitizer (postcode/digits removed, max 60)
  - JSON round-trip of the old preferences (no new members) → null defaults
  - telephone change clears verification
  - `VerifyEmailCode` sets `EmailVerifiedAtUtc` once (does not overwrite)
  - phone-verification lockout after max attempts
- **Guard test:** no code path maps `CandidatePrivatePreferences` into `CandidatePreferencesDto.WorkPreferences`. Reflection/grep-style test over `Jobsy.Core` + `Jobsy.Api` for the private DTO being used in passport/CV builders: allowed only in its own controller and in the KB dislike source.
- **Migration test** (existing EF model snapshot test pattern): the columns exist and the flag defaults are false.
- **bUnit:**
  - the Data tab shows the "shared" section only when `PassportPdfV2Enabled`
  - the chips save canonical codes
  - the phone-verify button is hidden when `PhoneVerificationEnabled = false`
- **Playwright 390×844** (soft-skip): a candidate edits the shared section and saves; no horizontal overflow.

## Success criteria
- New fields are stored and normalized, and the private dislikes are untouched and still private.
- E-mail verified is set on code/IdP login, telephone verification works with the stub, and the flags default to OFF.
- With all flags OFF the UI is unchanged. Release build with 0 warnings, full tests green.

## Out of scope
- Rendering any of this (step 04).
- Choosing an SMS provider.
- Housing.
- Changes to dislikes.
