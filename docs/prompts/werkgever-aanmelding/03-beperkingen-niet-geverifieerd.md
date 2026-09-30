# 03. What an unverified employer can and can't do; "klaar" vacancies go live on verification

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-3` from `cursor/werkgever-aanmelding-2`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Every gate is enforced in the API. The UI only explains it. Mollie/checkout internals and token prices stay untouched.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-3` |
| PR title | `feat(companies): unverified employers can prepare but not publish, buy or see candidates; ready vacancies auto-publish on verification; reminders and 60-day cleanup` |
| PR body starts with | `Stacked on #<PR 02> (cursor/werkgever-aanmelding-2)` |
| Mockups | `wr-d11` (vacancy row "Klaar · gaat live na verificatie", the checklist, the visibility panel); `wr-d8` (what unlocks after verification) |
| Split seam | **03a** = gates + filter + welcome token/promo move (03.2, 03.3). **03b** = ready vacancies + `CompanyVerificationService` + jobs (03.4–03.6) |

## Goal
An unverified employer can do all the preparation (D4). Nothing reaches candidates, and no money or candidate data flows until the company is verified. At that moment everything they prepared goes live by itself.

## 03.1 Today (verify first)
- `CompanyRegistrationService.CompleteConfirmationAsync` (~L460) → `GrantWelcomeTokenAsync` (~L519/538) right at activation: 1 welcome token, skipped during the `FreePublishUntil` promo.
- Free publishing: `FeatureSettings.FreePublishUntil` read in `VacanciesController` (~L794, ~L1154, ~L1615) + `FreePublishRules.IsActive`.
- Publish gates: `KvkVerificationRules.CanPublishOrSpend` in `VacancyProductService` (~L89) and `TokensController` (~L190). `TokensController` `checkout` (~L161) / `checkout/complete` (~L313) start and finish Mollie purchases.
- Candidate data for employers: `ApplicationsController` (employer actions), `TalentPoolController`, `CandidateInsightsController`, talent contact requests (find the controller: `git grep -n "talent-contacts\|TalentContact" -- Jobsy.Api/Controllers`).
- `UnconfirmedRegistrationCleanupHostedService` purges unconfirmed registrations (AVG pattern to follow).

## 03.2 Gates (D4)
- `CompanyVerificationRules` (Core): `CanPublish`, `CanBuyTokens`, `CanSeeCandidates`, `CanUseWelcomeToken`, `CanUseFreePublishPromo`: all `== Verified` of the **root organisation** (`ParentCompanyId` chain) of the acting company. `KvkVerificationRules` stays as an extra, separate condition.
- `[RequiresVerifiedCompany]` action filter (Api): resolves the acting company exactly like the action does (reuse `ICompanyAuthorizationService` / the action's `companyId` resolution). On failure it returns **403** `{ "code": "company_unverified", "message": … }` (`WaBanner.Blocked.*` text, 5 languages). Admin bypasses.
- Apply it to:
  - publishing: `publish`, `{id}/approve-publish`, `{id}/highlight`, `{id}/pushbom`, `{id}/extend`, plus the service-level check in `VacancyProductService` (defence in depth)
  - token purchases: `TokensController.checkout` and `checkout/complete`, any other purchase start (`git grep -n "CreatePayment\|Checkout" -- Jobsy.Api/Controllers`), and the Kandidaatinzichten unlock if werkgever-redesign 07 added one
  - candidate data: every employer-side read in `ApplicationsController`, `TalentPoolController`, `CandidateInsightsController` and talent contacts (list, detail, export, contact)
- **Allowed** while unverified: vacancy CRUD as `Draft`, the new "klaar" action (03.4), company profile, culture/values/branche/engagement (08, 09), `CompanyUsersController` invites, vestigingen management, verification endpoints (06), reading their own token balance (0).
- Guard test `VerifiedCompanyGateCoverageTests`: a reflection list of the actions above must carry `[RequiresVerifiedCompany]`; a second list must **not** (the allowed ones), so a refactor can't silently open or close a door.

## 03.3 Welcome token and free publishing only after verification
- Remove `GrantWelcomeTokenAsync` from activation. It runs from `CompanyVerificationService` (03.5) once per root organisation (idempotent: a ledger marker or `Company.WelcomeTokenGrantedAtUtc`, whichever the ledger supports cleanly).
- `FreePublishRules` usage: free publishing applies only when `CanUseFreePublishPromo` is true. An unverified company that clicks publish gets the "klaar" path (03.4), not a free publish.
- The existing registration-confirmation e-mail no longer mentions the welcome token. The verification e-mail (03.5) does, when granted.

## 03.4 "Klaar" vacancies (publish on verification)
- `Vacancy.PublishOnVerification` (bool) + `ReadyMarkedAtUtc?` + `ReadyMarkedByUserId?` (migration `AddVacancyPublishOnVerification`).
- `POST api/vacancies/{id}/ready` (employer mutate roles, company access): only when the company is not verified. It validates the vacancy with the **same** validation publishing uses (so "klaar" means publishable) and sets the flag; `DELETE …/ready` clears it. A vestigingsmanager's "klaar" still needs the bedrijfsmanager's approval if today's approval flow requires it (keep `PendingApproval` semantics).
- In the editor and list (`CreateVacancy.razor` / Vacatures page): for unverified companies the primary button reads **"Klaarzetten · gaat live na verificatie"** (`WaBanner.ReadyCta`) and the status pill **"Klaar · gaat live na verificatie"**. Drafts keep "Concept".

## 03.5 `CompanyVerificationService` (the one place that verifies)
- `Task MarkVerifiedAsync(Guid rootCompanyId, CompanyVerificationMethod method, Guid? actorUserId, string? note, CancellationToken)`, idempotent, in one transaction where possible:
  1. Set `Verified` + method + `VerifiedAtUtc` on the root and every company in its tree whose status is `Unverified`/`Pending` (children get `InheritedFromOrganization`). Also flip `IntermediaryClient` companies that belong to a verifying intermediary (10); nothing to flip when README Dependencies **G** is Present.
  2. Grant the welcome token (03.3) unless the promo is active.
  3. Publish the company's `PublishOnVerification` vacancies through the **normal** publish service, oldest `ReadyMarkedAtUtc` first, acting as `ReadyMarkedByUserId` (fall back to the bedrijfsmanager). If tokens run out, the remaining ones stay "klaar" and one Te doen/notification "Koop tokens om {n} klaargezette vacatures te publiceren" is created. Never skip validation or pricing.
  4. `IVacancyDiscoveryIndex.InvalidateCompanyAsync` (02).
  5. E-mail "Je bedrijf is geverifieerd 🎉" (+ the list of published vacancies) and an in-app notification to all company managers. Write a `PlatformLog` row `company.verified` (company id, method, actor; no PII).
- `Task MarkPendingAsync(...)`, `Task MarkRejectedAsync(..., reason)` (Rejected keeps everything hidden and lets the user try another method, 06).
- 06 (e-mail/letter/manual), 07 (claim) and 10 (Waadi) call **only** this service.

## 03.6 Reminders and cleanup (D4, D17)
- `UnverifiedCompanyReminderHostedService` (daily, Europe/Amsterdam 09:00): e-mail on **day 7** and **day 21** after activation to the bedrijfsmanager of an unverified root (status `Unverified` or `Rejected`), with a link to `/register/verifieren`. The day-21 mail states the deletion date (day 60). Send each once (store `ReminderSentDay7/21AtUtc` on the registration or company).
- `UnverifiedCompanyCleanupHostedService` (daily): on **day 60** delete what D17 lists. Only companies the registration created with no other members; `PublishOnVerification`/draft vacancies; pending codes/letters (06); the user when they have no other role or membership. Write one `PlatformLog` row `company.unverified-deleted` (counts, KvK number, no names or e-mails). A company with an **open manual check** (06) is skipped until the check is decided, plus 7 days. Letters already sent are not recalled.
- Templates in `TransactionalEmails` (5 languages): `CompanyVerificationReminder`, `CompanyVerified`, `CompanyUnverifiedDeleted` (sent on the deletion day, no data kept afterwards).

## Tests
- Gates: each gated action → 403 `company_unverified` for an unverified company's manager, 200/normal for a verified one; admin bypass; the allowed list works while unverified. Coverage reflection tests (03.2).
- Welcome token: none at activation; exactly one at verification; none during the promo; idempotent on a second `MarkVerifiedAsync`.
- Ready: validation errors block `ready`; on verification the ready vacancies are published in order through the normal path (tokens debited as usual), become public (02 test helper) and the "not enough tokens" branch leaves the rest ready and creates one notification.
- Jobs: day 7/21 mails once each; day 60 deletes exactly the D17 set and nothing else (a colleague's membership or another role keeps the user); an open manual check postpones deletion. Uses a fake clock (`TimeProvider`).
- `Sprint7RegistrationTests` and other existing registration/token tests stay green (update only the welcome-token timing assertions, and say which).

## Success criteria
- An unverified bedrijfsmanager can create, edit and mark vacancies "klaar", invite a colleague and fill in the profile, but can't publish, buy tokens or read any candidate data, whether through the UI or the API directly.
- Verification publishes the ready vacancies and grants the welcome token exactly once, and they appear publicly within ≤ 60 s.

Done → next: `04-kvk-zoeken.md`.
