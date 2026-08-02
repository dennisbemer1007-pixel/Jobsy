# Functioneel Testplan: Lobsy / Jobsy

## 1. Doel

Valideer de kritieke user-flows: registratie & onboarding (Company Manager / Salesmanager / Admin), prepaid tokens (“geen tokens, geen actie”), Mollie checkout/webhook, commissieverdeling, en dynamische session inactivity timeout.

## 2. Automatisering (xUnit)

Playwright UI-flows uit eerdere MVP-notities zijn nog niet in-repo; de actuele regressie zit in `Jobsy.Tests` (EF InMemory + `WebApplicationFactory`, geen Postgres vereist).

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

**Env:** meeste tests gebruiken EF InMemory / TestServer. Live Mollie wordt gemockt; `LiveApiSmokeTests` kan een draaiende API vereisen.
