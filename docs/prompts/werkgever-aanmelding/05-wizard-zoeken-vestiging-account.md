# 05. New registration wizard: find the company, choose vestigingen, create the account (+ all review bugs)

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-5` from `cursor/werkgever-aanmelding-4`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Re-check dependencies **A** (landing layout/mascot) and **B** (salesmanager referral resolver) before you start (README "Dependencies"). Use the fallback when one isn't on `acceptatie`.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-5` |
| PR title | `feat(register): 3-step employer wizard (search → vestigingen → account) with prerender, Microsoft/Google sign-up, referral resolver, domain-match verification and all review fixes` |
| PR body starts with | `Stacked on #<PR 04> (cursor/werkgever-aanmelding-4)` |
| Mockups | `wr-d1`–`wr-d4`, `wr-m1`, `wr-m2`, `wr-d10` (in use → access). The code and done screens have no mockup: build them from the same public-theme card, stepper and mascot as wr-d4 |
| Split seam | **05a** = wizard shell + step 1 (search) + step 2 (scope/vestigingen) + bug fixes §B in those parts (05.2–05.4). **05b** = step 3 account + code + sign-in + external login + referral + D15 (05.5–05.8) |

## Goal
Replace the 870-line `Register.razor` with a clear, public-theme wizard of 3 steps that renders on the server first. It finds a company by name, lets the user pick exactly which vestigingen they manage, and creates a working, signed-in account. When the e-mail domain proves it, the company is verified straight away.

## 05.1 Today (verify first)
- `Jobsy.Web/Components/Pages/Register.razor` (870 lines): `@rendermode InteractiveServer(prerender: false)` (L5); KvK number only; the error `@_error ?? "…"` renders the literal `??` text (L72); manual entry sets NL centre coordinates `52.1326 / 5.2913` (L638–639); `Task.Delay(1800)` + redirect to Banenkaart on code expiry (L565, L840); no Microsoft/Google option; free-text sales code `_salesManagerCode` (L296–299); inline styles (L360, L405); hard-coded Dutch strings; the candidate `GratisDnaRegisterBox` + quiet link on an employer page (L25–35); a "Stub-KVK's" hint for everyone (L65); after activation the user has to sign in by hand.
- `CompanyRegistrationService` (1795 lines): `SubmitAsync` (~L77), `ConfirmAsync` (~L338), `ActivateAsync` (~L407), `ClaimSiblingEstablishmentsAsync` (~L1022), `ApproveTakeoverAsync` (~L622), `ApplySalesManagerReferralAsync` (~L1513), `ApplyPartnerReferralAsync` (~L1648). `CompanyRegistration.SalesManagerTrackingCode`.
- Scope today: `RegistrationScope` (BranchOnly / whole company); the intermediair is forced to BranchOnly + `UserRole.Intermediary` (SBI 78).
- External login: `Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs` (Entra + Google schemes) → `api/auth/ensure-external` (creates **candidates** only; invited managers keep their DB role). `UserExternalLogin` entity.
- Tests: `RegisterWizardUiTests`, `Sprint7RegistrationTests`, `RegistrationPasswordRulesTests` + E2E.

## 05.2 Wizard shell
- Route `/register` stays (and `/register?kvk=` deep links keep working). `@rendermode @(new InteractiveServerRenderMode(prerender: true))`; the first paint must work without the circuit (search box + text visible), so guard every JS/`ProtectedSessionStorage` use with `OnAfterRenderAsync(firstRender)`.
- Components in `Jobsy.Web/Components/Registration/`: `RegisterWizard.razor` (state + stepper), `WaStepSearch.razor`, `WaStepScope.razor`, `WaStepAccount.razor`, `WaStepCode.razor`, `WaDone.razor`, `WaKvkSearchBox.razor` (reusable; 10 uses it too), `WaVestigingList.razor`. State in a scoped `RegistrationWizardState` (survives step changes and a reconnect via `ProtectedSessionStorage`; no passwords stored).
- Stepper "1 Bedrijf · 2 Vestigingen · 3 Account" (wr-d1). Mobile per wr-m1/m2 (and the same pattern for step 3) with a `pub-fixed-bottom` action bar (dependency A; fallback `.wa-fixed-bottom`).
- Layout: `PublicLayout` + `LobsyMascot` (dependency A); fallback `WaPublicLayout` + `WaMascot`.
- Every string through `UiStringsWerkgeverAanmelding` (`Wa.*`), no inline `style=`, CSS only in `features/werkgever-aanmelding.css` (`wa-` block).
- Remove the `GratisDnaRegisterBox`/quiet link from this page (the candidate DNA flow lives on `/ontdek`). `/register?van=ontdek` (old candidate links): if landing 03 routes it, leave it; otherwise redirect `?van=ontdek` to `/ontdek`.

## 05.3 Step 1: find the company (wr-d1, wr-d2, wr-m1)
- `WaKvkSearchBox`: a single field "Bedrijfsnaam of KvK-nummer" + an optional "Plaats"; debounce **300 ms**, minimum **3** characters (a hint below 3), calls `GET api/kvk/search` (04). Results grouped per company: name, place, legal form, vestiging count, "Al op Lobsy" chip (wr-d2). Keyboard: arrows + Enter, `aria-live` result count.
- Select → `GET api/registration/kvk/{kvk}/profile` → the company card (wr-d3) with website domain and SBI branche preview.
- KVK Unavailable or "Ik vind mijn bedrijf niet": manual entry (name, KvK number, address, postcode + place). **Fix:** geocode the entered address with the existing geocoder (`IGeocodingService`/PDOK: `git grep -n "Geocod" -- Jobsy.Core/Interfaces`). If that fails, save with **no coordinates** (nullable, or the existing "unknown location" convention) and mark the location as unknown, never the NL centre. Re-check Dependencies **G**: if `Company.LocationSource` exists, use the intermediair stack's PDOK resolver and `LocationSource = Unknown` (the map skips those rows); if not, add `Company.LocationSource` (`Unknown = 0`, `Kvk = 1`, `Pdok = 2`) with exactly those names so both stacks converge. Manual entries always need verification (06); they can't use D15 unless the KvK check succeeds later (`KvkVerificationRetryHostedService`).
- The "Stub-KVK's" hint shows only when `IHostEnvironment.IsDevelopment()` (or the stub is active), never in acceptatie/production.

## 05.4 Step 2: scope and vestigingen (wr-d3, wr-d4, wr-m2, wr-d10)
- Two cards (D7): **"Heel het bedrijf"** (bedrijfsmanager; all vestigingen ticked, each one untickable) and **"Eén of een paar vestigingen"** (vestigingsmanager; the user ticks). SBI 78 → the intermediair path (10); keep today's behaviour until 10 lands.
- The vestigingen list: address, "hoofdvestiging" label, and the status per row: free / "Al beheerd op Lobsy" (disabled, with a "Vraag toegang aan" link → `/register/toegang?kvk=&vestiging=` (07); until 07 lands it opens today's takeover path).
- The server honours exactly the ticked set: `SubmitAsync` gets `SelectedEstablishmentIds`; `ClaimSiblingEstablishmentsAsync` claims only those and **skips vestigingen owned by another company account** (D7, never silently transfers). New vestigingen appearing in KVK later are suggested in the dashboard (11), not added.
- If every vestiging is already owned: only the access-request path (07) is offered.

## 05.5 Step 3: account (wr-d4)
- Fields: name, function (optional), **business e-mail** with a live domain pill ("groenenzorg.nl · past bij de website ✓" / "gratis mailadres: verificatie via brief" / "ander domein"), phone (optional), login method: **Microsoft** / **Google** / **e-mail + wachtwoord** (password rules = `RegistrationPasswordRules`, unchanged).
- Sales code (dependency B): show a read-only chip "Via {salesmanager}" when `SalesAttributionResolver` resolves a cookie (`lobsy_sales_ref`); otherwise an optional field "Code van je accountmanager" validated on blur through the same resolver (unknown → an inline message, not an error on submit). The resolved **salesmanager id** is stored, not the raw text; `ApplySalesManagerReferralAsync` uses the resolver result. Fallback when salesmanager 03 is absent: `IRegistrationReferralResolver` (see README).
- Required checkboxes: terms/privacy, and "Ik mag {bedrijfsnaam} vertegenwoordigen op Lobsy" (`Wa.Account.Represent`). Store the timestamp + text version on the registration (migration `AddRegistrationWizardFields`: `SelectedEstablishmentIdsJson`, `RepresentationConsentAtUtc`, `RepresentationConsentVersion`, `SalesManagerUserId?`).
- Submit → `SubmitAsync` (rate limit `registration-submit`, README §A; max 3 per domain per day).

## 05.6 Code, sign-in and external login (no mockup; wr-d4 pattern)
- The 6-digit e-mail code is **always** sent to the contact e-mail (it proves the mailbox, whatever the login method). The countdown stays. **Fix:** on expiry or "verwijderd", show an in-place state "Code verlopen" + "Stuur nieuwe code" (resend with the existing limits) and **no** automatic redirect or `Task.Delay`.
- After a correct code: `ActivateAsync`, then **sign the user in** with the normal cookie flow (reuse the Login page's sign-in endpoint/handler; no second password entry):
  - **Microsoft/Google chosen:** after the code, challenge the chosen scheme with `returnUrl=/register/koppelen`. The callback links `UserExternalLogin` to the **new** user only when the IdP e-mail equals the confirmed contact e-mail (case-insensitive); otherwise show "Dit account hoort bij een ander e-mailadres" and keep the e-mail + password route. Change `ensure-external` so an existing, just-activated employer user with a pending link isn't turned into a candidate (it already preserves DB roles: add a test).
  - **Password chosen:** `MfaPolicy.IsRequired` → go to MFA setup first (D18), then the dashboard.
- A Microsoft/Google login **never** verifies a company by itself (D14).
- Done screen (mascot `Celebrating`): "Je account is klaar" + what's next: the verification card (06) or "Geverifieerd" (D15), and a button to the profile steps (08).

## 05.7 Instant verification by domain (D15)
- When the confirmed contact e-mail domain matches a KVK website domain (public-suffix aware, `DomainMatch` from 06a; if 06 hasn't landed, put `DomainMatch` here and 06 reuses it) and the domain isn't a free-mail domain, call `CompanyVerificationService.MarkVerifiedAsync(root, BusinessEmail, user)` right after activation. The done screen shows "Geverifieerd via je zakelijke e-mail".

## 05.8 Existing owner / takeover
- A KvK number where the whole company is already managed: step 2 shows "Dit bedrijf staat al op Lobsy" + "Vraag toegang aan" (07) + "Ik ben de eigenaar en er beheert iemand anders" (ownership transfer = letter + admin, 07). Today's direct `EstablishmentTakeoverRequest` from the wizard is replaced by those two links (07 moves the logic).

## Tests
- bUnit/`RegisterWizardUiTests`: prerender output contains the search box and no `??`; the stub hint is absent outside Development; no `style=` in the new components (source scan); every `Wa.*` key exists in 5 languages.
- Manual entry: geocoded → real coordinates; geocoder fails → no coordinates + `LocationSource = Unknown`, never 52.1326/5.2913.
- Scope: unticked vestigingen aren't claimed; one owned by someone else is skipped; SBI 78 still goes to the intermediair path.
- Code expiry: no redirect; resend works; `Task.Delay` gone (source scan).
- Sign-in: after a correct code the response sets the auth cookie; password users with an MFA role go to MFA setup; external link only when the IdP e-mail matches; `ensure-external` keeps the employer role.
- Referral: cookie → chip; a typed code → resolver; an unknown code doesn't block the submit; the stored value is the salesmanager id.
- D15: matching domain → Verified/BusinessEmail right after activation; free-mail or mismatch → Unverified.
- `Sprint7RegistrationTests`, `RegistrationPasswordRulesTests` green (update only selectors/flow steps, and list them in the PR).
- Playwright: desktop + mobile happy path (search "groen" → select → heel bedrijf → account → code → dashboard).

## Success criteria
- A new employer goes from `/register` to a signed-in dashboard in 3 steps + a code, with Microsoft, Google or a password. Every item marked "→ 05" in README §B is fixed and covered by a test.

Done → next: `06-verificatie-email-brief.md`.
