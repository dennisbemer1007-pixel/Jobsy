# Werkgever-aanmelding follow-ups

## Salesmanager 03 referral
Dependency **B** was ABSENT on acceptatie at 05. Implement `IRegistrationReferralResolver` with `SalesAttributionResolver` + `lobsy_sales_ref`; delete `DefaultRegistrationReferralResolver`.

## Intermediair data model (Dependencies G) — recheck at 07
**STILL ABSENT** on acceptatie at file 07: no `IntermediaryClient` entity. File 07 follows the Absent path (client companies with intermediary `UserCompany` membership). Guard test asserts registration / claims / takeovers / access requests never invent or touch an `IntermediaryClient` type. When intermediair 01/02 lands, switch claim/takeover guards to the Present case (links are never companies).

## SBB erkende leerbedrijven (09.6)
SBB’s BPV-API (https://www.s-bb.nl/onderwijs/bpv-api) is for mbo schools only (aanvraag + Edukoppeling), not a public open dataset with reuse licence for matching by KvK. No automated `ISbbRecognitionService` was built; admins check `leerbedrijf` claims by hand. Revisit if SBB publishes a documented open dataset.