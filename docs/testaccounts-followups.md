# Test accounts — follow-ups

## D10 — Demo passwords on acceptatie

`DemoUsersSeeder` still seeds privileged `@jobsy.local` accounts (including `admin@jobsy.local`) with the public password `Jobsy123!` when `Seed__Enabled=true`. Once the CLI test accounts are in use, stop seeding privileged demo accounts on acceptatie (or give them env-var passwords). Do not change that seeder in the test-accounts PR.

## Auth stack — keep `IsRequiredFor`

Callers of MFA must use `MfaPolicy.IsRequiredFor(role, isTestAccount, testAccountsActive)`. Future auth-stack work (docs/auth 02+) must keep that shape so the test-account exemption stays intact.

## Boundary coverage (01b candidates)

Fully wiring every interaction entry point (talent unlock, likes, shares, referral codes, invoice/VAT/commission stubs, metrics exclusions, company public pages, sitemap) may continue on a stacked PR if this file exceeds the ~1.500 line seam. Core helpers (`TestDataRules`, query extensions, mail guard, apply boundary, discovery exclusion) ship with 01.
