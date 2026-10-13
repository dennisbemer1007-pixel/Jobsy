# Werkgever-aanmelding

Employer sign-up, verification and “Over je bedrijf”. Stack files `01`–`11` under
`docs/prompts/werkgever-aanmelding/` (branch `docs/werkgever-aanmelding`).

## Flow

1. **Search** (`/register`) — KvK number or name (3+ chars), pick company + vestigingen
   (heel bedrijf with untick, or alleen deze vestiging).
2. **Account** — Microsoft, Google or e-mail + password; contact e-mail always confirmed
   with a 6-digit code; 2FA via existing MFA middleware.
3. **Over je bedrijf** (`/register/bedrijf`, optional) — branche (max 4), Zo werken wij
   (6 sliders → Cultuurscan), kernwaarden, maatschappelijke betrokkenheid.
4. **Verify** (`/register/verifieren`) — business e-mail (domain match vs KVK website) or
   letter with 8-char code to the KvK address (Pingen / stub). Admin manual check is fallback.
5. **Dashboard** (`/home`) — while unverified: “Nog niet zichtbaar voor kandidaten” banner,
   checklist, visibility panel; letter code can be entered from the banner.

Until verified the company is invisible publicly (`PublicVisibility`). Unverified employers
may draft vacancies and mark them “klaar” (`PublishOnVerification`); they go live on
verification. Candidate data, tokens and free-publish promo stay gated
(`CompanyVerificationRules` / `company_unverified`).

## Verification methods

| Method | Endpoint | Notes |
|--------|----------|--------|
| Business e-mail | `POST api/company-verification/email/start\|confirm` | Domain must match a KVK website; freemail blocked |
| Letter | `POST api/company-verification/letter` + `…/letter/confirm` | Crockford base32 `XXXX-XXXX`, 30 days, max 2 resends, 5 wrong attempts → block |
| Manual | `POST api/company-verification/manual` | Admin queue `/admin/werkgeververificatie` |

Letter settings: `CompanyVerificationSettings.MonthlyLetterCap` (default 500), max 1 letter
per KvK / 30 days. Stub provider when Pingen is not configured.

## Gates (server)

- Publish / tokens / candidates → `company_unverified` 403 until Verified.
- Intermediary (SBI 78): after ownership verify, publish may still return
  `409 lender_registration_pending` until Waadi/admin confirms (`ILenderRegistrationCheck`).

## Jobs

| Job | Purpose |
|-----|---------|
| `UnverifiedCompanyReminderHostedService` | Day 7 / 21 reminders |
| `UnverifiedCompanyCleanupHostedService` | Delete unverified trees at day 60 |
| `AccessRequestEscalationHostedService` | Access-request day 3 / day 5 (workdays) |
| `VestigingSuggestionHostedService` | Weekly: new free KVK vestigingen for Organisation-scope verified roots |

## Admin

`/admin/werkgeververificatie` — manual checks, blocked letters, escalated access requests,
engagement proof, Waadi decisions.

## Dashboard API (11)

- `GET api/employer/onboarding/status` — banner/checklist/visibility/suggestions
- `POST api/employer/onboarding/vestiging-suggestions/accept|dismiss`

## Testing with stubs

- KVK: `KvkServiceStub` / demo numbers in tests (`99990…`).
- Letters: stub `ILetterService` — admin can download stub PDF.
- No real Pingen/PostNL calls in Development without credentials.

## Follow-ups

See `docs/werkgever-aanmelding-followups.md` and `docs/werkgever-aanmelding-landing-followup.md`.
