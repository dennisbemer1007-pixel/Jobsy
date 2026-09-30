# Werkgever-aanmelding follow-ups

## Salesmanager 03 referral
Dependency **B** was ABSENT on acceptatie at 05. Implement `IRegistrationReferralResolver` with `SalesAttributionResolver` + `lobsy_sales_ref`; delete `DefaultRegistrationReferralResolver`.

## Intermediair data model (Dependencies G) — recheck at 07
**STILL ABSENT** on acceptatie at file 07: no `IntermediaryClient` entity. File 07 follows the Absent path (client companies with intermediary `UserCompany` membership). Guard test asserts registration / claims / takeovers / access requests never invent or touch an `IntermediaryClient` type. When intermediair 01/02 lands, switch claim/takeover guards to the Present case (links are never companies).
