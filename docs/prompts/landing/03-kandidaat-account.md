# 03. Candidate account: `/account-maken` with Google, Microsoft and a passwordless e-mail code; every test CTA points there

Read `00-README.md` first. Branch `cursor/landing-3` from `cursor/landing-2`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Password logins, lockout, MFA pages and `MfaEnforcementMiddleware` stay untouched. An e-mail code never signs in a non-candidate account.

| | |
|---|---|
| Branch | `cursor/landing-3` |
| PR title | `feat(auth): candidate self sign-up /account-maken — Google, Microsoft, e-mail code; test CTAs no longer go to company /register` |
| PR body starts with | `Stacked on #<PR 02> (cursor/landing-2)` |
| Mockups | sign-up card in `lp-d4-test-resultaat.png` and `lp-m3-resultaat.png` (provider buttons, "Doorgaan met e-mail"); header/footer from 01 |
| Split seam | **03a** = API + entity + migration + Web endpoints for the e-mail code (03.3, 03.4). **03b** = `/account-maken` pages + all CTA rewiring + Login link (03.2, 03.5–03.7) |

## Goal
A candidate can create a free account in under a minute with Google, Microsoft or just an e-mail address, and the 20 test answers come along. Nothing candidate-facing points at the company KvK registration any more.

## 03.1 Today (verify first)
- Funnel bug: `Pages/Public/GratisDna.razor` L224 `RegisterHref => "/register?van=ontdek"` and `Components/Public/GratisDnaResultView.razor` L119 (default) feed `gd-jobs-teaser` (~L39), the unlock tiles, `gd-quiet-link` and the sticky `gd-sticky` (~L112–114). `GratisDnaUnder16View.razor` L7 → `/register`. `Help/HowLobsyRoleGuides.cs` guest step 3 + secondary CTA → `/register` ("Registreren"). `Pages/Register.razor` = company KvK registration; with `van=ontdek` it shows `GratisDnaRegisterBox`.
- Login: `Pages/Login.razor` has Microsoft (`/account/external/entra`, L45) and Google (`/account/external/google`, L66) buttons, the password form → `POST /account/login`, and `href="/register"` with `Login.RegisterCta` = "Registreer via KVK" (L131).
- Auth plumbing (`Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs`): `MapPost /account/login` (~L260: antiforgery, `AuthRedirects.SafeLocalUrl`, `TryLocalApiLoginProfileAsync`, MFA challenge cookies, `ResolveCandidateReturnUrl`), external OIDC → `ensure-external` (~L1150, sends `referralCode` from the `lobsy_ambassadeur_ref` cookie + `X-Jobsy-Provision-Secret`).
- API `AuthController`: `local-login` (rate limit `"auth"`), `ensure-external` (creates `UserRole.Candidate`, attributes the referral). `User.TermsAcceptedAt` exists. `VerificationCodes` (`CreateNumericCode`, `Hash`, `MaxFailedAttempts = 5`, pepper) is used by applications/me/parental consent. E-mail via `IEmailService` + `IEmailCatalogService` + `Jobsy.Core/Email/TransactionalEmails.cs`.
- Merge: `GratisDnaMerge.razor` in `MainLayout` merges `jobsy.gratisDna.v1` into the profile on the first signed-in page load (`GratisDnaMergeService`). There's **no** candidate e-mail self-sign-up and no password reset.

## 03.2 `/account-maken` page (static SSR, `PublicLayout`, `[ExcludeFromInteractiveRouting]`)
- Card as the sign-up card in lp-d4: `LobsyMascot` waving (Medium), h1 "Maak je gratis account", one line "Bewaar je resultaat en ga verder waar je was." Then:
  1. **Doorgaan met Google** → `/account/external/google?returnUrl=…`
  2. **Doorgaan met Microsoft** → `/account/external/entra?returnUrl=…`
  3. **Doorgaan met e-mail**: an e-mail field + optional "Hoe mogen we je noemen?" (first name, max 60) + button "Stuur mijn code". This is a plain `<form method="post" action="/account/email-code/start">` with antiforgery.
  - Hide a provider button when that provider isn't configured (reuse the check Login uses; see `external-provider-config`).
- Consent line under the buttons: "Door verder te gaan ga je akkoord met de **voorwaarden** en de **privacyverklaring**." (links `/gebruiksvoorwaarden`, `/privacy`). Record `User.TermsAcceptedAt` for new e-mail users exactly as external sign-up does (if external doesn't set it, set it for both here and say so).
- `?van=ontdek`: show `GratisDnaRegisterBox` ("Je testresultaat gaat mee"), reusing it as is. `?van=onder16`: a short line "Ben je jonger dan 16? Na het aanmaken vragen we toestemming aan je ouder of voogd." (the existing parental-consent flow does the rest, D19).
- Footer links: "Heb je al een account? Inloggen" → `/login?returnUrl=…`; "Ben je werkgever? Bedrijf registreren" → `/register` (**ON only**, via `IEmployersSwitch`).
- `returnUrl`: `AuthRedirects.SafeLocalUrl`. The default for new candidates is what external sign-up uses today (`ResolveCandidateReturnUrl`). Keep `van` in the flow so KPI (10) can count `SignupComplete{method}`.
- `noindex`. Add it to `PageSeoCatalog`, `PageHelpDocs` (short help), `docs/ROUTES.md`, `BlazorPageRoleAttributesTests` (anonymous).
- Signed-in visitors → 302 to their home (`FeatureRoutes.HomeFor` / today's post-login URL).

## 03.3 E-mail code: data + API
- Entity `EmailSignInChallenge` (Infrastructure, own migration):
  - `Id`, `EmailNormalized` (via `LoginIdentity.Normalize`), `CodeHash`, `Purpose` (`SignUp` | `SignIn`)
  - `FirstName?`, `ReferralCode?`, `ReturnUrl?`
  - `CreatedAtUtc`, `ExpiresAtUtc` (+10 min), `FailedAttempts`, `ConsumedAtUtc?`
  - Index on (`EmailNormalized`, `CreatedAtUtc`). Rows older than 24 h are deleted on each start (cheap delete) or by an existing cleanup job if one fits.
- `POST api/auth/email-code/start` (`[AllowAnonymous]`, `[EnableRateLimiting("auth")]`, requires the Web→API `X-Jobsy-Provision-Secret` like `ensure-external`, so only the Web app can call it):
  - body `{ email, firstName?, referralCode?, returnUrl?, culture }`
  - per-address throttle: max **3** starts per 15 min and **10** per day (409 `too_many_codes` to Web, which shows a friendly message)
  - no user → `Purpose = SignUp`, send the e-mail "Je code voor Lobsy: 123456" (template in `TransactionalEmails`, 5 languages, culture from the request)
  - existing user with **only** the Candidate role → `SignIn`, same e-mail with "Je inlogcode"
  - existing **non-candidate** user (employer/staff/admin/ambassadeur/etc.) → **no code**; send "Log in met je wachtwoord of je Microsoft/Google-account" with a link to `/login`
  - The response is **identical in all cases** (202 + `challengeId` or a dummy id), so it can't be used to discover accounts.
- `POST api/auth/email-code/verify` (same attributes): body `{ challengeId, code, rememberDevice, userAgent }`.
  - Wrong code → `FailedAttempts++`; at `VerificationCodes.MaxFailedAttempts` the challenge is burned (410 `code_expired`). Expired/consumed → 410.
  - Success: mark consumed. `SignUp` → create `User { Role = Candidate, Email, FullName = firstName (or null / the e-mail local part if the column is required), TermsAcceptedAt = now }` exactly like `ensure-external` (same defaults, same referral attribution via `_ambassadeurAttribution`). `SignIn` → load the user. Mark the e-mail verified if the user model has such a flag.
  - Return the same login-profile shape `local-login` returns (so Web reuses its principal creation). If the account has 2FA enrolled, return `RequiresMfa` + challenge token exactly as `local-login` does.
  - Race: two verifies for one challenge → one wins (concurrency token or conditional update).
- Constant-time hash compare via `VerificationCodes`. Codes and e-mail addresses are never logged in plain text (log the challenge id only).

## 03.4 E-mail code: Web endpoints + code page
- `POST /account/email-code/start` (form, antiforgery like `/account/login`): calls the API with the referral cookie + culture. It stores `challengeId` in a short-lived, HttpOnly, SameSite=Lax cookie `Jobsy.EmailCode` (10 min) and redirects to `/account-maken/code?returnUrl=…` (the e-mail address is **not** in the URL; the page shows it masked from the cookie-bound server state, e.g. "d•••@gmail.com").
- `/account-maken/code` (static SSR): 6 separate digit inputs **or** one `inputmode="numeric" autocomplete="one-time-code" maxlength=6` field (use the single field; simpler and it works with SMS/e-mail autofill). "Nieuwe code sturen" (re-posts start; the throttle applies) and "Ander e-mailadres" (back).
- `POST /account/email-code/verify`: antiforgery, calls the API, then signs in through the **same** code path `/account/login` uses after `TryLocalApiLoginProfileAsync` (extract a shared `SignInFromApiProfileAsync` if needed; no copy-paste). MFA → the existing MFA challenge cookies + redirect. Success → `ResolveCandidateReturnUrl(returnUrl)`; the `GratisDnaMerge` in `MainLayout` runs on that first page.
- Errors show inline on the code page: wrong code (attempts left), expired → "Vraag een nieuwe code aan", too many → "Probeer het over een kwartier opnieuw".

## 03.5 Rewire every candidate CTA (D16)
- `GratisDna.razor` `RegisterHref` and `GratisDnaResultView` default → `PublicRoutes.CreateAccountFromTest`. All four uses (jobs teaser, tiles, quiet link, sticky) follow automatically. Verify with a test that the rendered result contains no `/register`.
- `GratisDnaUnder16View` CTA → `/account-maken?van=onder16`, label "Account maken".
- `HowLobsyRoleGuides` guest step 3 + secondary CTA → `/account-maken` with label `PublicNav.CreateAccount`. (Leave step 1 "/" for 04.)
- **`/register?van=ontdek` → 302 `/account-maken?van=ontdek`**: a tiny middleware (or endpoint filter) before Blazor routing that only matches path `/register` with `van=ontdek` (case-insensitive). Every other `/register` request is untouched. `Register.razor`'s `GratisDnaRegisterBox` branch becomes dead; leave it with a `// reachable only if the redirect is removed` comment (don't touch the KvK flow).
- `TeaserLayout` already links `/account-maken` (01). `WestlandTeaser`'s `/register` buttons are employer CTAs: leave them.

## 03.6 Login page
- Under the form: **"Nieuw bij Lobsy? Maak gratis account"** → `/account-maken` (+ `returnUrl`), styled as the primary secondary-link.
- The KvK link keeps `/register` but the label becomes "Bedrijf registreren (KvK)" (`Login.RegisterCta`, 5 languages), shown only when ON (`IEmployersSwitch`). The werkgevers-actief gate may already hide it; don't double-hide.
- Keep the `/ontdek` quiet link.

## 03.7 Privacy text
One sentence in `/privacy` (5 languages if the page is localized, else nl) under account data: "Maak je een account met je e-mailadres, dan sturen we een eenmalige code naar dat adres. De code is 10 minuten geldig; we bewaren alleen een versleutelde versie en verwijderen die binnen 24 uur."

## Tests
- API: start → identical 202 for new / candidate / non-candidate / invalid-looking address; the non-candidate gets the password e-mail and **no** challenge that can verify; throttle 3/15 min + 10/day; verify happy path creates a Candidate with `TermsAcceptedAt` and the referral; wrong code ×5 burns it; expired → 410; consumed can't be reused; parallel verify only once; MFA-enrolled candidate gets `RequiresMfa`; the provision secret is required.
- Web: antiforgery on both posts; `Jobsy.EmailCode` cookie HttpOnly + short; `returnUrl` open-redirect cases (`//evil`, `https://evil`) → safe default; sign-in sets the same auth cookie as password login (compare claims).
- `/register?van=ontdek` → 302 `/account-maken?van=ontdek`; `/register` and `/register?ref=x` unchanged (200).
- bUnit: `/account-maken` providers hidden when unconfigured; `van=ontdek` shows the register box; ON shows "Bedrijf registreren", OFF (test double) doesn't; Login shows "Nieuw bij Lobsy?".
- GratisDna bUnit/Playwright: the result view renders `/account-maken?van=ontdek` on all CTAs and **no** `/register`. Existing `GratisDnaBunitTests` / `GratisDnaPlaywrightTests` stay green (update only the hrefs they assert).
- Playwright E2E (CI stack; read the code from a test mailbox or a test `IEmailService` sink): test → result → "Maak gratis account" → e-mail → code → signed in → the profile contains the 20 answers (merge). Also via the Google/Microsoft buttons (assert the redirect target only).
- `ExternalAuthAndInvitePromotionTests`, `PlatformUxSpecTests` stay green. Routes/SEO/help/role attributes. `LocalizationParityReportTests`.

## Success criteria
- From any test CTA, a new candidate reaches a signed-in state with the e-mail code in ≤ 4 screens, and the 20 answers are merged.
- No candidate-facing link in the Web project points at `/register` any more (grep test: only `Register.razor` itself, `RegisterActivate`, the Login KvK link, `/werkgevers` (08), `PartnerSales`, `WestlandTeaser`, the admin flyer default and SEO/help entries).
- An employer's e-mail address never receives a sign-in code.

Done → next: `04-banenkaart-verhuizen.md`.
