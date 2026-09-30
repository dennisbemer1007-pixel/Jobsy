# Werkgever-aanmelding follow-ups

## Salesmanager 03 referral
Dependency **B** was ABSENT on acceptatie at 05. Implement `IRegistrationReferralResolver` with `SalesAttributionResolver` + `lobsy_sales_ref`; delete `DefaultRegistrationReferralResolver`.

## Intermediair data model (Dependencies G) — recheck at 07
**STILL ABSENT** on acceptatie at file 07: no `IntermediaryClient` entity. File 07 follows the Absent path (client companies with intermediary `UserCompany` membership). Guard test asserts registration / claims / takeovers / access requests never invent or touch an `IntermediaryClient` type. When intermediair 01/02 lands, switch claim/takeover guards to the Present case (links are never companies).

## SBB erkende leerbedrijven (09.6)
SBB’s BPV-API (https://www.s-bb.nl/onderwijs/bpv-api) is for mbo schools only (aanvraag + Edukoppeling), not a public open dataset with reuse licence for matching by KvK. No automated `ISbbRecognitionService` was built; admins check `leerbedrijf` claims by hand. Revisit if SBB publishes a documented open dataset.

## Intermediair 04b partly done (werkgever-aanmelding 10)
Dependency **D** was ABSENT on acceptatie at file 10 (Only-in-spec path). Implemented `ILenderRegistrationCheck`, providers (`WaadiKvkProvider`, `WttaNauProvider` disabled, `AdminManual`), entity `LenderRegistration`, publish gate `409 lender_registration_pending`, and admin Waadi decisions on `/admin/werkgeververificatie` via `RecordDecisionAsync`.

When intermediair 04b runs: skip what already exists; only add the admin tab on `/admin/intermediairs` and move the Waadi decision there.

## Intermediair data model (Dependencies G) — recheck at 10
**STILL ABSENT** on acceptatie at file 10: no `IntermediaryClient` entity. File 10 follows the Absent path (client companies with intermediary `UserCompany` membership).
## Werkgever shell (Dependencies C) — recheck at 11
**STILL ABSENT** on acceptatie at file 11: no `WerkgeverLayout` / `WgPageShell` / `WerkgeverNav`. File 11 uses `CompanyVerificationBanner` in `MainLayout` for employer roles, checklist + visibility panel in `EmployerHomePanel`, and `EmployerLinks` for today’s URLs. When werkgever-redesign 01/02 lands: move the banner into `WgPageShell` Banner slot, checklist items into Te doen, and point `EmployerLinks` at `/werkgever/...` (301s keep old URLs working).
