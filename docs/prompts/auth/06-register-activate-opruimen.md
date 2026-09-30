# 06. Clean up `/register/activate` after werkgever-aanmelding owns the code step

Read `00-README.md` first (§0, §IA, Decision D6, Dependency G). Branch `cursor/auth-6` from `cursor/auth-5` (from `cursor/auth-4` when 05 was skipped, case E-wait). **Stacked.** **Re-run Dependency G first.** When G is **absent**, this file is **skipped** (see 06.5).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/auth-6`; no force-push.
> - ONE PR into `acceptatie`; the body starts with `Stacked on #<PR 05 or 04> (cursor/auth-5 or cursor/auth-4)` and quotes the G check output.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start file 07.
> - Never activate, change or delete a real registration on acceptatie or production while testing.
> - Microsoft and Google logins never get an extra Lobsy 2FA step (admins can't use Google, 02).

| | |
|---|---|
| Branch | `cursor/auth-6` |
| PR title | `chore(auth): remove the old /register/activate page and activation-link code (werkgever wizard owns the code step)` |
| Mockups | none |
| Migration | none |

## Goal
There is one way to activate an employer account: the code step in the werkgever-aanmelding wizard. The old link page, the link builder and the dev stub go away. Old links keep landing somewhere useful.

## 06.1 Today (verify first; line numbers from `origin/acceptatie` a611db40)
- **Page:** `Jobsy.Web/Components/Pages/RegisterActivate.razor` (`@page "/register/activate"`). 01 added `LegacyAuthRouteRedirects` for the no-token case and removed the "Code invoeren" mail buttons + `EmailLayout.RegisterActivateUrl`.
- **Link builder:**
  - `CompanyRegistrationService.BuildActivationUrl` (L1487), used at L288, L317 and L1452
  - results exposed as `ActivationUrl` only when `ExposeRegistrationActivationLinks` is on (L296, L334, L1459)
- **Dev stub:** `RegistrationController.StubActivation` (`GET api/registration/stub-activation`, ~L300–342, admin-only, gated by `ExposeRegistrationActivationLinks` outside Development).
- **Token activation:**
  - `POST api/registration/activate?token=` (`RegistrationController.Activate` L151)
  - Web client `JobsyApiClient.Employer.ActivateRegistrationAsync` (L306)
  - service `CompanyRegistrationService.ActivateAsync` (~L407), used by many tests (`Sprint7RegistrationTests`, `FreePublishRulesTests`)
- **The feature flag** `ExposeRegistrationActivationLinks`:
  - `PlatformFeatureSettings` (DB column), `IPlatformFeatureService` records, `PlatformFeatureService` L52/L161
  - `SettingsController` L329/L537, `Sprint6Dtos` L175/L187
  - `appsettings*.json` of the Api, `docs/deploy-render.md` L79, several tests
- **References:**
  - `docs/ROUTES.md` L143, `docs/TESTSCENARIOS_PER_ROL.md` L200
  - `PageSeoCatalog` L103, `SeoEndpoints` L29 (robots `Disallow`)
  - `PageHelpDocs` (find the entry with `rg -n "register/activate" Jobsy.Web`)
  - `PageSeoTests`

## 06.2 Check what the wizard uses (G present)
Before you delete anything, run and paste into the PR:
- `git grep -n "ActivateRegistrationAsync\|api/registration/activate\|ActivateAsync(" -- Jobsy.Web Jobsy.Api Jobsy.Infrastructure`
- `git grep -n "ActivationUrl\|ExposeRegistrationActivationLinks" -- Jobsy.Web Jobsy.Api Jobsy.Infrastructure Jobsy.Core`

Werkgever-aanmelding 05 activates after a correct **code** and signs the user in. Keep every piece it still calls (the service `ActivateAsync` and, if the wizard posts to it, the API endpoint). Delete only what is left for the link flow.

## 06.3 Delete and redirect
- **Delete:**
  - `RegisterActivate.razor` and its CSS (check `rg -n "register-activate|activate-" Jobsy.Web/wwwroot/css`)
  - `BuildActivationUrl` and the `ActivationUrl` fields on the registration result records (and their DTO mapping), unless 06.2 shows the wizard uses them
  - `StubActivation`
  - the Web client method `ActivateRegistrationAsync` and `POST api/registration/activate` **only if** 06.2 shows no remaining caller
  - the `Page.ActivateTitle`, `Seo.ActivateDescription` and other `Activate*` UI strings that are no longer used (all languages)
- **Feature flag `ExposeRegistrationActivationLinks`:**
  - remove it from the settings UI, DTOs, options, `appsettings*.json`, `docs/deploy-render.md` and the tests that set it
  - **keep the DB column** (no migration in this file): mark the entity property `[Obsolete("Unused since auth 06; drop in a later migration")]` and ignore it in the service
  - follow-up line in `docs/auth-followups.md`: "Drop `PlatformFeatureSettings.ExposeRegistrationActivationLinks` column."
  - If 06.2 shows the wizard still reads it, keep it everywhere and say so.
- **Redirect:**
  - replace 01's no-token redirect in `LegacyAuthRouteRedirects` with a **permanent 301** `/register/activate` (with or without `token`, any query) → `/register`
  - an old token is never read or logged
  - `/register` shows its normal first step. Werkgever-aanmelding may show "Deze link werkt niet meer. Vul je gegevens opnieuw in of log in." when a `?van=activatie` flag is passed: add the flag to the redirect only if the wizard supports it (check `rg -n "van=activatie|oud-link" Jobsy.Web`).
- **References:**
  - `docs/ROUTES.md`: the row becomes "`/register/activate` → 301 `/register` (removed in auth 06)"
  - `PageSeoCatalog` L103: remove the entry
  - `SeoEndpoints` L29: remove the `Disallow` line (a 301 needs no disallow)
  - `PageHelpDocs`: remove the entry
  - `PageSeoTests`: update
  - `docs/TESTSCENARIOS_PER_ROL.md` L200: "Open `/register/activate` (with or without token) → 301 to `/register`"
  - `CHANGELOG.md`

## 06.4 Tests
- `RegisterActivateRedirectTests` (01's class, updated): `/register/activate`, `/register/activate?token=abc`, and `/register/activate?token=abc&x=1` → 301 `/register`; the response doesn't contain the token; a log capture shows no token.
- `RegisterActivateRemovedTests`: no component with the route `/register/activate` (reflection over `RouteAttribute`), `BuildActivationUrl` gone, `stub-activation` → 404.
- The wizard's activation tests (werkgever-aanmelding 05) and `Sprint7RegistrationTests` stay green. Tests that only exercised the link URL are removed or rewritten; list them in the PR.
- `TransactionalEmailCatalogTests`/`EmailLayoutTests` (already updated in 01) stay green. No mail contains `/register/activate` (a grep test over the rendered catalog).

## 06.5 G absent: skip
- Don't delete anything. Keep 01's no-token redirect.
- Add to `docs/auth-followups.md`: "auth 06: remove `/register/activate`, `BuildActivationUrl`, `StubActivation` and `ExposeRegistrationActivationLinks` after werkgever-aanmelding 05 merges (spec `docs/prompts/auth/06-register-activate-opruimen.md`)."
- Continue with 07 from the previous branch. Report 06 as "skipped (waiting on werkgever-aanmelding 05)".

## Success criteria
- G present: the page, the link builder and the stub are gone, and old links return a 301 to `/register`. No mail or page links to `/register/activate`. The wizard activation still works and is green. The PR contains the 06.2 grep output and the list of removed and rewritten tests.
- G absent: no code change beyond the follow-up line, and 06 is reported as skipped.

Done → next: `07-talen-rtl-toegankelijkheid.md`.
