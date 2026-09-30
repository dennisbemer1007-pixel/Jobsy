# 01. Security hotfix: parental consent via website POST, single-use links instead of passwords and API keys, escaped support-access mail

Read `00-README.md` first ("How to run", §0, §IA, §B). Branch `cursor/emails-hotfix` from `origin/acceptatie`. **Standalone: not stacked on anything.**

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-hotfix`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Standalone hotfix (not stacked)`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 02.
> - No plaintext passwords, API keys or reusable secrets in any mail, log, API response or UI message. GET never mutates.
> - Don't redesign the layout here. Keep today's `EmailLayout` and only change what this file names. The redesign is 02+.

| | |
|---|---|
| Branch | `cursor/emails-hotfix` |
| PR title | `fix(security): parental consent only via website POST; single-use links replace mailed passwords and API keys; escaped support-access mail` |
| PR body starts with | `Standalone hotfix (not stacked)` |
| Mockups | `em-d06`/`em-m06` for the invite copy and button (in today's layout) |
| Migration | `AddOneTimeLinks` (the only one in this file) |
| Split seam | if > ~1.500 lines: **01a** = (a) + (c) + the `OneTimeLinks` foundation + set-password (01.2, 01.3.1–01.3.5, 01.4); **01b** = the API-key reveal (01.3.6) on `cursor/emails-hotfix-b`, stacked on 01a, body "Stacked on #<01a> (standalone hotfix part 2)" |

## Goal
Three security gaps close without waiting for the redesign.
- A link scanner can no longer confirm a minor's parental consent.
- No mail contains a password or an API key.
- The support-access notice can't carry injected HTML.

## 01.1 Today (verify first)
- **Parental consent:**
  - `MeController.RequestParentalConsent` (~L920–960) stores `ParentalConsentEmail`, the hashed token and a 7-day expiry.
  - It then builds `{Request.Scheme}://{Request.Host}/api/parental-consent/confirm?token=…` (L951) and sends bare HTML (L953–958, category `ParentalConsent`). The mail doesn't name the child.
  - `ParentalConsentController.Confirm` is `[HttpGet("confirm")]` and **sets `ParentalConsentAt`** on GET (L20–45).
- **Temporary passwords:**
  - `CompanyUsersController` invite (~L340–395): `GenerateTemporaryPassword()` L598. It creates **or overwrites** the credential (L360–362), sends `TransactionalEmails.UserInvite(…, temporaryPassword, …)` and echoes it in Development (L394).
  - `SalesManagerInviteService` (~L173, `baseUrl: null`), `SalesManagerApplicationService.ApproveAsync` (returns `TemporaryPassword`), `AmbassadeurInviteService` (~L46–160, `baseUrl: null`).
  - `CompanyRegistrationService.ResolvePasswordHash` (~L1369): a legacy temporary password when none was chosen at register. It goes to `RegistrationCredentials` (~L1352) and `TakeoverApproved` (~L828).
  - The templates print it: `TransactionalEmails` L541–547, L636–644, L701, L724, L747.
  - UI echoes: `Branches.razor` L330, `Regions.razor` L254, `SalesManagersAdmin.razor` L224/L270, `AmbassadeursAdmin.razor` L223. DTOs: `Sprint5Dtos` L123, `Sprint7Dtos` L48, `SalesManagersController` L562, `ICompanyRegistrationService` L96, `ISalesManagerInviteService` L20, `IAmbassadeurInviteService` L15, `ISalesManagerApplicationService` L51.
- **API key:** `CompanyApiKeyService.EmailCredentialsAsync` (L175–238) generates the plaintext and mails it through `CompanyApiKeyCredentials` (L759). Only after a successful send does it deactivate the old keys and store the new hash. Called from `CompanyApiKeysController` L108 (UI: `CompanyDetails.razor` L385/L546/L1011).
- **Support access:** `SupportAccessService.MaybeNotifyAdminsAsync` (~L215–247) sends `"Admin {adminEmail} vroeg support-toegang aan tot {grant.ExpiresAt:u}. Reden: {grant.Reason}"` as `BodyHtml`, unescaped, with no layout, when `SupportAccessNotifyAdmins` is on.
- No password-reset or set-password flow exists (`git grep -n -i "PasswordReset\|setup-password" -- '*.cs'` is empty). `RegistrationPasswordRules` (MinLength 12, MaxLength 128), `JobsyPasswordHasher`, `User.SessionVersion` and `LoginLockoutRules` exist.

## 01.2 (a) Parental consent: website page + POST only
- **Link:** built from `(await _features.GetAsync()).PublicWebBaseUrl` (`EmailLayout.Absolute`), never `Request.Host`:
  - `{PublicWebBaseUrl}/toestemming?t={token}`
  - the token is unchanged: 32 random bytes, hex, stored hashed, 7 days
- **Mail:** a new `TransactionalEmails.ParentalConsent(baseUrl, childFirstName, confirmUrl, expiresAtUtc)` in today's layout.
  - Subject "{Voornaam} vraagt je toestemming voor Lobsy". The first name is `User.FirstName`; if that is empty, the subject is "Je kind vraagt je toestemming voor Lobsy".
  - Heading "Geef je toestemming?". Body in B1: "{Voornaam} wil Lobsy gebruiken: tests doen en een analyse met AI krijgen. Omdat {voornaam} jonger is dan 16 jaar, hebben we toestemming nodig van een ouder of voogd."
  - One button "Toestemming bekijken" → the link. Note: "Ben je geen ouder of voogd, of weet je hier niets van? Dan hoef je niets te doen. De link werkt tot {datum}." The date is in Europe/Amsterdam, `d MMMM yyyy`, nl.
  - Every value is escaped. The category stays `ParentalConsent`.
  - The age comes from `CandidateConsentRules.ParentalConsentAge` (16 today), not a literal.
- **API:**
  - `GET api/parental-consent/preview?token=` (anonymous, `otp-verify` limit) → `{ valid, childFirstName?, expiresAtUtc? }`. It never mutates, never returns the e-mail or the last name, and returns `valid:false` for unknown/expired tokens (same shape, no oracle beyond validity).
  - `POST api/parental-consent/confirm` `{ token }` (anonymous, `otp-verify` limit, `[IgnoreAntiforgeryToken]` only if the API has global antiforgery) → the existing logic: set `ParentalConsentAt`, clear the hash and expiry. Returns `{ ok, childFirstName }` or 400 `invalid_or_expired`.
  - `GET api/parental-consent/confirm?token=` (links already sent) → **302** to `{PublicWebBaseUrl}/toestemming?t={token}`. No DB write.
- **Web page `/toestemming`:**
  - `Pages/Public/ParentalConsent.razor`, `[AllowAnonymous]`, static SSR (no interactive circuit), noindex, public layout of today.
  - GET calls preview. Valid: heading "Toestemming voor {Voornaam}", 3 short lines (what Lobsy is, what the tests and AI analysis are, that consent can be withdrawn via `privacy@`/the privacy page), then one form with antiforgery and a button "Ik geef toestemming". Invalid: "Deze link werkt niet meer. Vraag {je kind} om een nieuwe aanvraag te sturen."
  - POST calls confirm and then shows "Dank je. De toestemming is gegeven."
  - Strings via `UiStrings` (`Consent.*`, 5 languages; pl/ro/ar drafts).
  - Add to `docs/ROUTES.md`, `PageSeoCatalog` (noindex) and `PageHelpDocs` if it requires entries for anonymous pages.
- The Web → API call uses the normal `JobsyApiClient`. The page renders no token in any link besides the form's hidden field.

## 01.3 (b) Single-use links instead of passwords and API keys
### 01.3.1 Data model
- Entity `OneTimeLink`, table `OneTimeLinks`, migration `AddOneTimeLinks`:
  - Fields: `Id`, `Purpose` (enum `OneTimeLinkPurpose { SetPassword = 1, ApiKeyReveal = 2 }`, stored as int), `TokenHash` (string 128, unique index), `UserId?` (FK Users, cascade), `CompanyId?` (FK Companies, cascade), `Email` (normalized, 254), `CreatedAtUtc`, `ExpiresAtUtc`, `UsedAtUtc?`, `CreatedByUserId?`.
  - Index on (`Purpose`, `UserId`, `UsedAtUtc`).
- `IOneTimeLinkService` (Core interface, Infrastructure impl):
  - `CreateAsync(purpose, userId?, companyId?, email, lifetime, createdBy?)` → `(Guid id, string token)`. The token is 32 random bytes, base64url. Only the hash is stored (`VerificationCodes.Hash`, same pepper). Creating a link **invalidates** older unused links with the same purpose + user (or purpose + company for `ApiKeyReveal`) by setting `UsedAtUtc`.
  - `PeekAsync(purpose, token)` → valid + safe preview data. It never mutates.
  - `ConsumeAsync(purpose, token)` → atomic single use: `UPDATE … SET UsedAtUtc = now WHERE TokenHash = @h AND UsedAtUtc IS NULL AND ExpiresAtUtc > now` via `ExecuteUpdateAsync`; 0 rows = invalid.
  - Tokens are never logged; the PlatformLog gets link id + purpose only.
- Lifetimes as constants in `Jobsy.Core/Security/OneTimeLinkRules.cs`: `SetPassword = 7 days`, `ApiKeyReveal = 72 hours` (README D7).
- Daily cleanup: delete rows used or expired more than 30 days ago. Add it to an existing daily hosted job (e.g. the retention/cleanup job), not a new one, if one exists.

### 01.3.2 Set-password page
- `/account/wachtwoord-instellen?t=` (`Pages/Account/SetPassword.razor`, `[AllowAnonymous]`, static SSR form + antiforgery, noindex).
  - GET → `GET api/account/setup-link?token=` → `{ valid, maskedEmail, expiresAtUtc }` (masking like `EmailServiceStub.RedactEmail`).
  - Valid: heading "Kies je wachtwoord", the masked e-mail, password + repeat, the `RegistrationPasswordRules` hint (12–128 characters), button "Wachtwoord opslaan".
  - Below: "Of log in met Google of Microsoft" → `/login` (the external login works when the IdP e-mail matches, as today).
  - Invalid/expired: "Deze link werkt niet meer. Vraag degene die je uitnodigde om een nieuwe uitnodiging." with a `/login` link.
- `POST api/account/setup-password` `{ token, password }` (anonymous, `auth` limit):
  - validates with `RegistrationPasswordRules`
  - consumes the link
  - creates or updates the user's `LocalAuthCredential` (`JobsyPasswordHasher.Hash`) and resets `FailedLoginCount`/`LockoutUntil`
  - increments `User.SessionVersion` if the user already had a credential (not for a first password)
  - writes `PlatformLog` `account.password-set` (user id, link id; no e-mail)
  - Web then redirects to `/login?setup=done` (Login shows "Je wachtwoord is opgeslagen. Log nu in.", with the e-mail prefilled from the preview if the login page supports a prefill; never in the URL).
- MFA: unchanged. Password users hit `MfaEnforcementMiddleware` at their first login as today.
- Strings via `UiStrings` (`SetPassword.*`, 5 languages).

### 01.3.3 Invite paths (no password is ever generated or mailed)
- `CompanyUsersController` invite:
  - New user → **no** `LocalAuthCredential` is created. Create a `SetPassword` link (7 days) and mail `UserInvite` with it.
  - Existing user (re-invite / new membership) → **never touch** the existing credential (fixes L360–362). If the user has no credential and no external login, mail a new link; otherwise mail the invite with a "Inloggen" button to `/login`.
- `SalesManagerInviteService`, `SalesManagerApplicationService.ApproveAsync`, `AmbassadeurInviteService`: same rule (new user → link, existing → no credential change). Pass the real `PublicWebBaseUrl` (fix `baseUrl: null`).
- `CompanyRegistrationService`:
  - When no password was chosen at submit (`ResolvePasswordHash` legacy path), create no credential. `RegistrationCredentials` and `TakeoverApproved` carry a `SetPassword` link with the button "Kies je wachtwoord".
  - When a password was chosen, the button stays "Inloggen".
  - Pass the real base URL in the calls this file touches (L885, L1333, L1352).
- `GenerateTemporaryPassword` is **deleted** everywhere (controller + 2 services + registration). Guard test (01.6) keeps it out.

### 01.3.4 Templates (today's layout, minimal copy change)
- `UserInvite`, `SalesManagerInvite`, `AmbassadeurInvite`: the parameter becomes `string setPasswordUrl` (or `null` for an existing user with a login). The password block (`<code>` box) is removed. One primary button "Uitnodiging accepteren" → the link (or "Inloggen" → `/login`).
  - Line under it: "De link werkt tot {datum}. Daarna vraag je een nieuwe uitnodiging." The date is Europe/Amsterdam, nl.
  - Sales/Ambassadeur: the onboarding button goes away. Onboarding is where the first login lands (existing redirect), so the mail has one button.
- `RegistrationCredentials`, `TakeoverApproved`: the `temporaryPassword` parameter becomes `string? setPasswordUrl`, with the same button rule.
- `CompanyApiKeyCredentials(baseUrl, companyName, revealUrl, expiresAtUtc, keyPrefixOfCurrentKey?)`:
  - no key, no prefix of the new key
  - the endpoint and Swagger URL stay as text (they aren't secret)
  - one button "API-sleutel ophalen" → the reveal link
  - "De link werkt 72 uur en maar één keer. Je huidige sleutel blijft werken tot je de nieuwe ophaalt."
- `EmailSampleContext`: `TemporaryPassword` → `SetPasswordUrl = "{base}/account/wachtwoord-instellen?t=voorbeeld"`; `SampleApiKey`/`SamplePassword` constants are deleted; add `SampleRevealUrl`.

### 01.3.5 API responses and UI
- Remove `TemporaryPassword` from every DTO/record listed in 01.1 (or keep the property only where removing it breaks a public contract, and then always `null`; say which in the PR).
- UI messages become "Uitnodiging gemaild naar {e-mail}." (`Branches.razor`, `Regions.razor`, `SalesManagersAdmin.razor`, `AmbassadeursAdmin.razor`). No password, not even in Development. In Development the stub mail in `PlatformLogs` contains the link, as today.
- The registration activation response (`RegistrationController` ~L356) no longer returns a password.

### 01.3.6 API key: reveal once
- `EmailCredentialsAsync(companyId, recipientEmail)` no longer generates a key. It creates an `ApiKeyReveal` link (company id, recipient e-mail, 72 h) and mails `CompanyApiKeyCredentials` with `{PublicWebBaseUrl}/koppeling/sleutel?t=`. If the mail fails, delete the link and throw as today ("De bestaande key blijft actief"). The result DTO keeps `Sent`, `Email`, and has **no** prefix of a key that doesn't exist yet.
- Page `/koppeling/sleutel?t=` (`Pages/Public/ApiKeyReveal.razor`, `[AllowAnonymous]`, static SSR form + antiforgery, noindex, response header `Cache-Control: no-store`):
  - GET → peek → "API-sleutel voor {bedrijfsnaam}", "Je ziet de sleutel maar één keer. Bewaar hem meteen in je wachtwoordkluis of je systeem." Button "Toon de sleutel" (POST).
  - POST → `POST api/company-api-keys/reveal` `{ token }` (anonymous, `auth` limit):
    - consume the link
    - generate the key (`ApiKeyHasher.GeneratePlaintext`)
    - deactivate the company's active keys, store the new `ApiKey` (name "API-koppeling (e-mail)", prefix)
    - `PlatformLog` `apikey.revealed` (company id, key id, link id)
    - return the plaintext
  - The page shows the key in a read-only field with a copy button, plus the endpoint, the header name and the docs link. The key is not stored in any circuit state or cache; the page renders once.
  - Expired/used: "Deze link werkt niet meer. Vraag een nieuwe aan via Lobsy (Bedrijfsgegevens → API)."
- `CompanyDetails.razor`: the confirmation text becomes "We hebben een link gemaild naar {e-mail}. Met die link haal je de nieuwe sleutel één keer op."
- Admin `ApiKeysAdmin` (shows a new key on screen once) is unchanged: that isn't a mail.

## 01.4 (c) Support-access mail
- New `TransactionalEmails.SupportAccessRequested(baseUrl, adminDisplay, reason, expiresAtUtc, scopeLabel)` in today's layout; `SupportAccessService` sends it (to the same recipients, same setting).
  - Every value is escaped by the template (`EmailLayout.Escape`); nothing unescaped goes into `BodyHtml`.
  - The admin is shown as their masked e-mail (`RedactEmail`).
  - Subject "Support-toegang aangevraagd door een admin".
  - Body: "{admin} vroeg tijdelijk toegang tot persoonsgegevens aan." Then facts: Reden, Toegang tot, Geldig tot "{d MMMM yyyy, HH:mm} (Nederlandse tijd)". The time zone is Europe/Amsterdam via the existing lookup pattern (`"Europe/Amsterdam"`, then `"W. Europe Standard Time"`, as `FreePublishRules` L45).
  - One button "Bekijk de toegang" → `{base}/admin/personal-data-access-log`.
  - Category `SupportAccessRequested`.
- Put the Amsterdam conversion in one helper `Jobsy.Core/Time/AmsterdamTime.cs` (`ToLocal(DateTime utc)`, `FormatDate/FormatDateTime(utc, culture)`). The consent and invite expiry lines use it too. 04 builds on it.

## 01.5 Tests
- **Consent:**
  - `ParentalConsentFlowTests`: a GET on `api/parental-consent/confirm?token=` returns 302 to `{PublicWebBaseUrl}/toestemming?t=…` and leaves `ParentalConsentAt` null.
  - Preview doesn't mutate. POST confirms once; a second POST gives 400. An expired token gives 400.
  - The mail link host equals the configured `PublicWebBaseUrl` host even when the request `Host` header is `evil.example`.
  - The mail contains the child's first name (escaped: `<b>Sam</b>` renders as text) and falls back without a name.
  - bUnit: the `/toestemming` GET render contains a form with method POST and no auto-submit script.
- **One-time links:**
  - `OneTimeLinkServiceTests`: hash-only storage (no plaintext in the row), single use under concurrency (two parallel consumes → one wins), expiry, a new link invalidates the old one.
  - `SetPasswordFlowTests`: invite → link mailed (stub) → the POST sets the password → login works → the link can't be reused. Password rules are enforced.
  - `ReinviteKeepsPasswordTests`: re-inviting an existing user with a password leaves `PasswordHash` unchanged.
  - Sales/ambassadeur approve/invite create a link and no credential. Update `SalesManagerReferralHierarchyTests` L78 (assert a `SetPassword` link exists for the provisioned user instead of `TemporaryPassword`) and `Sprint7RegistrationTests` L46.
- **API key:** `ApiKeyRevealFlowTests`: the mail contains no key and no `ApiKeyHasher` prefix pattern. The old key stays active until reveal; reveal activates the new key, deactivates the old one and returns the plaintext once; a second reveal gives 400. The response has `Cache-Control: no-store`.
- **Guards (`NoSecretsInMailsTests`):**
  - Compose every catalog template with the sample context: no `<code>` password box, no string matching the old temp-password alphabet pattern of length 12, no API key pattern.
  - `git grep`-style source test: no `GenerateTemporaryPassword` and no `TemporaryPassword` property in `Jobsy.Api/Models`, `Jobsy.Core/Interfaces` or `Jobsy.Web`.
- **Update** `TransactionalEmailCatalogTests.Test_samples_do_not_look_like_live_secrets` (L125–134):
  - assert the API-key mail contains the reveal URL and **not** a key
  - assert the invite contains `/account/wachtwoord-instellen?t=` and **not** a password
  - `Working_cta_targets_match_live_routes` keeps `/login` for Sales; drop `/salesmanager/onboarding` (one button)
- **Support access:** `SupportAccessMailTests`: a reason `<img src=x onerror=alert(1)>` arrives escaped; the expiry shows Amsterdam local time (a UTC 12:00 value in summer shows 14:00; in winter 13:00); the category is right.
- `RoutesDocFreshnessTests`, `PageSeoTests`, `BlazorPageRoleAttributesTests`, `EfModelSnapshotTests`, `PendingModelChangesTests`, `EfMigrationDiscoveryTests` stay green.

## Success criteria
- `git grep -n "GenerateTemporaryPassword\|TemporaryPassword" -- 'Jobsy.Api' 'Jobsy.Core/Interfaces' 'Jobsy.Web' 'Jobsy.Infrastructure/Services'` is empty (or only the justified always-null contract property named in the PR).
- No mail template takes a password or an API key parameter.
- `GET api/parental-consent/confirm` never writes to the database (test).
- The consent, invite, API-key and support-access mails link to the configured `PublicWebBaseUrl`.
- PR body lists the new routes (`/toestemming`, `/account/wachtwoord-instellen`, `/koppeling/sleutel`), the endpoints, the migration, and notes for the scholen stack (README dependency I: `IOneTimeLinkService` + the set-password page are the invite-token pattern it reuses).

Done → next (only when running the full stack): `02-layout-fundament.md`.
