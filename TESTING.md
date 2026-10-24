# Functioneel Testplan: Lobsy / Jobsy

## 1. Doel

Valideer de kritieke user-flows: registratie & onboarding (Company Manager / Salesmanager / Admin), prepaid tokens (“geen tokens, geen actie”), Mollie checkout/webhook, commissieverdeling, en dynamische session inactivity timeout.

## 2. Automatisering (xUnit)

Actuele regressie zit in `Jobsy.Tests` (EF InMemory + `WebApplicationFactory`, geen Postgres vereist).
Mobiele candidate-tab guardrail: `CandidateTabStabilityPlaywrightTests` (Playwright 390×844).
Zet `JOBSY_E2E_BASE_URL` (Acceptatie) + optioneel `JOBSY_E2E_CANDIDATE_EMAIL` / `JOBSY_E2E_CANDIDATE_PASSWORD` om live te draaien; zonder URL soft-skipt de test. CI: `.github/workflows/pr-tests.yml`.

### A. End-to-end kernketen

| Suite | Dekking |
|-------|---------|
| `CoreFunctionalFlowE2ETests` | SM-hiërarchie (upline + direct) → Company Manager registratie + e-mailverificatie + referral → billing preference iDEAL/creditcard → empty balance blokkeert publish/highlight/PushBom/extend → Exact Match + bulk packs → Mollie stub checkout + pending action → webhook fulfillment (tokens + auto-publish + 15%/3% commissie) → Admin session timeout 5/30 min + graceful re-auth |
| `CoreFunctionalFlowApiTests` | HTTP: 402 InsufficientTokens, `top-up-quote`, billing-preference, checkout + pending Publish, afwijzing ongeldige betaalmethode, Admin `platform-features` session timeout + anonieme `session-security` |

### B. Gerelateerde suites

| Domein | Bestanden |
|--------|-----------|
| Registratie | `Sprint7RegistrationTests`, `RegistrationPasswordRulesTests` |
| Prepaid / Mollie | `PrepaidTokenCheckoutTests`, `MolliePaymentMethodTests`, `MolliePaymentServiceTests`, `MollieWebhookCommissionSettlementTests`, `TokenPurchaseFulfillmentIdempotencyTests` |
| Commissies / SM | `SalesCommissionRulesTests`, `SalesManagerCommissionTests`, `SalesManagerReferralHierarchyTests`, `RevenueShareServiceTests` |
| Session security | `SessionSecurityTests` |
| Rol-regressie | `RoleFunctionalRegressionTests` |
| Token producten | `Sprint4TokenProductsTests`, `TokenLedgerServiceTests` |

## 3. Scenario-mapping (handmatige QA ↔ geautomatiseerd)

### Role-based registration & onboarding

1. **Company Manager** — register + e-mail activate + welcome token + billing preference iDEAL/CC → `CoreFunctionalFlowE2ETests`, `Sprint7RegistrationTests`, `CoreFunctionalFlowApiTests` (billing).
2. **Salesmanager** — invite/upline referral, tracking code, dashboard 15%/3% visibility via commercial settings + ledger balances → `CoreFunctionalFlowE2ETests`, `SalesManagerReferralHierarchyTests`, `SalesManagerCommissionTests`.
3. **Admin** — platform features session timeout → `CoreFunctionalFlowApiTests`, `SessionSecurityTests`, `CoreFunctionalFlowE2ETests`.

### “No tokens, no action” + Mollie webhook

1. Empty balance → InsufficientTokens / HTTP 402 + in-context quote (Exact Match + bulk) + iDEAL/CC.
2. Paid webhook → token credit + pending Publish/Highlight auto-exec + idempotent replay.

### Automated commission distribution

1. Direct 15% + upline 3% on paid fulfillment; 1-year window hard stop; dashboard balances.

### Security & dynamic session timeout

1. Default 30 min; Admin custom (5–480); idle → sign-out → `/login?error=session-expired` (+ returnUrl); opt-in form drafts via `sessionIdle.js`.

## 4. Uitvoeren

```bash
cd /workspace
dotnet restore Jobsy.sln
dotnet build Jobsy.sln
dotnet test Jobsy.Tests/Jobsy.Tests.csproj

# Gerichte kernketen:
dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter "FullyQualifiedName~CoreFunctionalFlow"
```

### Playwright functional gate (`e2e-functional`)

Required status check for PRs targeting **`acceptatie`**. Post-deploy smoke runs on every push to **`acceptatie`**
(read-only against `acceptatie.lobsy.nl` when `JOBSY_E2E_BASE_URL` is configured in GitHub secrets).

**Local (stack on :5201):**

```bash
chmod +x .github/scripts/start-ci-stack.sh
export ConnectionStrings__JobsyDb='Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres'
export VerificationCodes__Pepper='ci-verification-otp-pepper-32chars!!'
.github/scripts/start-ci-stack.sh

export JOBSY_E2E_FUNCTIONAL_GATE=1
export JOBSY_E2E_BASE_URL=http://127.0.0.1:5201
export JOBSY_E2E_CAPTURE=1
export JOBSY_E2E_ARTIFACTS_ROOT=/opt/cursor/artifacts/functional-sweep
pwsh Jobsy.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
dotnet test Jobsy.Tests/Jobsy.Tests.csproj -c Release --filter "Suite=FunctionalE2e"
```

Uses throwaway `@jobsy.local` registrations (OTP capture via `JOBSY_E2E_CAPTURE_EMAIL_CODES=1` on the API).
Candidate sweep tests reuse `kandidaat@jobsy.local` / `Jobsy123!` on seeded CI, or Acc secrets when set.

**Env:** meeste tests gebruiken EF InMemory / TestServer. Live Mollie wordt gemockt; `LiveApiSmokeTests` kan een draaiende API vereisen.

## 5. Testaccounts op acceptatie

Na merge + Dashboard env vars (`TestAccounts__Enabled`, `TestAccounts__Password__*` op `lobsy-acc-api`; marker + switch on `lobsy-acc-web`):

1. Shell on `lobsy-acc-api`: `dotnet Jobsy.Api.dll test-accounts seed --dry-run`, then `seed`.
2. Log in at `https://acceptatie.lobsy.nl/login` as `test-<role>@lobsy.nl` with the Dashboard password (no 2FA while the guard is active).
3. Cleanup: `dotnet Jobsy.Api.dll test-accounts cleanup` then `--execute --expect-users <n>`.

Never put real passwords in tests or docs. Automated coverage lives under `Jobsy.Tests/TestAccounts/`.

### Sollicitaties mockup demo data (acceptatie)

When the **test-accounts guard** is active (`Lobsy__DeploymentEnvironment=Acceptatie`, `TestAccounts__Enabled=true`, `lobsy-acc-*`, stub payments, etc.), the API runs **`TestAccountsSollicitatiesDemoSeeder`** on **every startup** (after migrate). It is idempotent and only touches **`IsTestAccount`** candidates:

- Primary lookup: `lobsy-test@mail.grokbot.com` (when that user is a test account)
- Fallback: catalog candidate e-mail (`test-kandidaat@<TestAccounts__EmailDomain>`)

It adds fake employers (e.g. **Testbedrijf Groen**), vacancy applications (Verstuurd/Gezien, **Kies een moment** with open slots, gesprek gepland, unread berichten, afgerond), **Buiten Lobsy** external applied, and **Ik wil hier werken** initiatives.

**First deploy / empty DB:** run `dotnet Jobsy.Api.dll test-accounts seed` once on `lobsy-acc-api` so the candidate + branch manager exist; subsequent acc deploys refresh demo rows automatically at startup.

Manual re-run without redeploy: run `test-accounts seed` again (same guard).
