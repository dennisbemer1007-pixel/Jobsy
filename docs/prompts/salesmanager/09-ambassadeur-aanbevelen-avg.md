# 09. Ambassadeur on the shared pages, Salesmanager aanbevelen, GDPR retention and notices, cleanup + docs

Read `00-README.md` first (§0, §IA "Ambassadeur", §R, §P, D8, D13, D15). Branch `cursor/salesmanager-9` from `cursor/salesmanager-8`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-9` (from `cursor/salesmanager-8`) |
| PR title | `feat(sales): ambassadeur on shared partner pages (counts only), recommend flow with notice + retention, cleanup` |
| PR body starts with | `Stacked on #<PR 08> (cursor/salesmanager-8)` + the list of deleted files/endpoints |
| Mockups | the salesmanager mockups apply with the ambassadeur differences from §IA (no separate ambassadeur mockups) |
| Split seam | **09a** = ambassadeur differences + Mijn kandidaten + initials removal + candidate notice (09.2–09.4). **09b** = aanbevelen redesign + retention + cleanup + docs (09.5–09.8) |

## Goal
The ambassadeur uses the same Lobsy Partner pages as the salesmanager with only the differences Dennis asked for, sees counts instead of people, and the recommend flow treats the recommended person fairly (informed, able to object, data removed on time). The old pages and endpoints are gone.

## 09.1 Today (verify first)
- `AmbassadeurDashboardService` L132 builds candidate rows with `Initials(c.FullName)` + application counts (L208 `Initials`), shown by `AmbassadeurHomePanel.razor`. Candidates attributed via `/werven/{code}` (`User.ReferredByAmbassadeurUserId`, `ReferredByAmbassadeurTrackingCode`) are not told.
- Ambassadeur commission: `AmbassadeurCommissionRules` (5 % + 1 pp per 50 referred candidates, max 15 %; `AmbassadeurSettings`, per-user override).
- `Components/Pages/SalesManager/Referrals.razor` + `SalesManagerApplicationService` (`me/applications`, admin approve/reject); `SalesManagerApplication` stores `CandidateFullName`, `CandidateEmail`, `Motivation` with no retention; the recommended person is not informed.
- After 01–08 the old page components still exist behind the role hosts (01.5) and the inline-style allow-list (01.9).

## 09.2 Ambassadeur differences (D8)
- `SalesNav`: ambassadeur items per §IA (Mijn kandidaten; Mijn werkgevers only when ≥ 1 attributed root; no aanbevelen). Bottom nav: Overzicht · Mijn link · Kandidaten · Wallet · Meer.
- Dashboard (04 components): the commission KPI shows **"Jouw commissie {p} %"** with "Nog {n} kandidaten tot {p+1} %" (or "Maximaal: 15 %"), from `AmbassadeurCommissionRules` + settings/override; the funnel reads Bezoeken → Aangemelde kandidaten → Gesolliciteerd → Werkgevers (if any). "Beste werkgevers" only when attributed companies exist.
- Mijn link & materiaal: `/werven/{code}` link + existing ambassadeur flyer kinds (05.3 variant).
- Wallet, profiel, hulp: identical (07, 06), ambassadeur agreement.
- Remove `SalesHomeHost`/role hosts: one set of pages renders both roles from the same components with role-specific slots; no duplicated page per role.

## 09.3 Mijn kandidaten `/sales/kandidaten` (ambassadeur only)
- `GET api/sales/me/candidates/summary?period=` → `SalesCandidateSummaryDto`: `RegisteredTotal`, `RegisteredInPeriod`, `AppliedTotal` (candidates with ≥ 1 application), `ApplicationsTotal`, `Monthly[12]` (registered, applied), `NextTier` info. **No per-person rows, no names, no initials, no dates per person.**
- Page: KPI row (Aangemelde kandidaten · Gesolliciteerd · Sollicitaties · Jouw commissie %), 12-month bars, the privacy line "Je ziet alleen aantallen. Nooit namen of gegevens van kandidaten."
- Delete `AmbassadeurDashboardService.Initials` and every DTO/UI field that carried per-candidate data; the 01.9 privacy guard now covers these DTOs.

## 09.4 Candidate notice
- Candidate registration (`Register.razor` candidate path, and the external-login completion) shows one calm line when an ambassadeur code/cookie applies: "Je komt via een ambassadeur van Lobsy. Die ziet alleen aantallen, nooit jouw gegevens." (`Sales.Candidate.AmbassadeurNotice`).
- Privacy statement: add a short paragraph (string or the privacy page's content source) "Aanmelden via een salesmanager of ambassadeur" explaining the cookie (`lobsy_sales_ref` / `lobsy_ambassadeur_ref`, 30 days, only the code), what the partner sees (employers: trade name, place, own commission; candidates: counts only) and why (commission). Dennis reviews the text in the PR.

## 09.5 Salesmanager aanbevelen `/sales/aanbevelen` (salesmanager with `CanRecruitSalesManagers`)
- Header "Salesmanager aanbevelen", lead "Ken je iemand die goed kan verkopen? Beveel hem of haar aan. Lobsy beslist."
- Explainer card: "Jij krijgt {indirect} % extra in jaar 1 over de aankopen van hun werkgevers. Zij krijgen {referredYear1} % in jaar 1." (from settings), and "Via je aanbevelingen verdiend: € {x}" (sum of `IndirectTokenCommission`).
- Form (drawer or inline card): Naam, E-mail, "Waarom past deze persoon?" (max 500), required checkbox (D15) "Deze persoon weet dat ik hem of haar aanmeld en vindt dat goed." Primary "Aanbeveling versturen". Server: stores `ReferrerConfirmedPermission = true`; rejects duplicates for the same e-mail within 60 days.
- On submit: send `SalesMail.RecommendedNotice` to the recommended person (D15): who recommended them (the salesmanager's display name), what Lobsy stores and for how long, a link "Ik wil dit niet – verwijder mijn gegevens" (one-time token, 60 days) → `SubjectObjectedAtUtc`, application `Rejected` with reason "Bezwaar", PII cleared **immediately**. `SubjectNotifiedAtUtc` set.
- List "Jouw aanbevelingen": date, name (or "Gegevens verwijderd" after clearing), status (Wacht op Lobsy · Goedgekeurd · Afgewezen · Verlopen · Bezwaar); no e-mail shown after submit.
- Admin approve/reject stays in the existing admin flow (sales admin tab), now showing "Persoon geïnformeerd op {datum}".

## 09.6 Retention (D13) in `DataRetentionHostedService`
- `SalesManagerApplication`: `Pending` older than 60 days → status `Expired` (enum value from 01) and PII cleared; `Rejected` → PII cleared 30 days after `ReviewedAtUtc`; `Approved` → PII cleared 30 days after `ReviewedAtUtc` once `ProvisionedUserId` is set (the account holds the data from then on). "PII cleared" = `CandidateFullName = ""`, `CandidateEmail = ""` (`CandidateEmailSha256`, set at submit, keeps the 60-day duplicate rule working), `Motivation = ""`, `PersonalDataClearedAtUtc = now`.
- `SalesLinkClickDaily` older than 25 months deleted (if 03 didn't already wire it).
- Invoices, ledger, payout requests/runs: **kept** (7 years, fiscal); no deletion job, documented.
- Test each rule with a fixed clock.

## 09.7 Cleanup
- Delete the old components now unused: `Pages/SalesManager/*.razor` (Dashboard, SalesToolkit, Referrals, Onboarding, Invoices, PayoutCheckoutStub), `Pages/Ambassadeur/{Dashboard,Toolkit,Finance,Onboarding,PayoutCheckoutStub}.razor`, `Home/SalesManagerHomePanel.razor`, `Home/AmbassadeurHomePanel.razor`, `Shared/PayoutCheckoutStubView.razor` (only if the werkgever partner stub doesn't use it; check), the role hosts.
- Keep public `Pages/Ambassadeur/Landing.razor` and `Pages/Partner/PartnerSales.razor`.
- API: delete the self-service endpoints no page uses anymore on `SalesManagersController` / `AmbassadeursController` (`me/dashboard`, `me/profile`, `me/invoices*`, `me/payouts/*` incl. the 410s from 07, `me/applications` if replaced), after a `git grep` shows no caller (Web, tests, docs). Admin endpoints stay. List every deleted route in the PR.
- `SalesManagerPayoutService` checkout/stub code paths removed; `AllowStubPayouts` stays only if other code uses it.
- The inline-style allow-list (01.9) is empty; the test asserts it.
- `RoleNavCatalog` has no sales/ambassadeur arrays left (they came from `SalesNav` since 01).

## 09.8 Docs
- `docs/security/roles-matrix.md`: SalesManager + Ambassadeur rows (portal, 2FA mandatory, what they see: employers trade name/place/own commission; candidates counts only; payouts via request + admin approval).
- ADR `docs/adr/00NN-sales-partner-portal.md` (next free number, Dependencies G): D1–D16 in short.
- `docs/ROUTES.md`, `PageHelpDocs`, `HowLobsyRoleGuides` (salesmanager + ambassadeur guides point to `/sales/link`), `CHANGELOG.md`.

## Tests
- Ambassadeur: nav, dashboard tier card (thresholds, override, max), candidates summary numbers on a fixed set, no per-person data in any response (JSON scan for names/initials), Mijn werkgevers hidden without attributed companies.
- Candidate notice shown only with an ambassadeur code/cookie.
- Aanbevelen: permission checkbox required; duplicate rule; notice mail sent once; objection link clears PII immediately and is single-use; list shows "Gegevens verwijderd".
- Retention rules (09.6) with a fixed clock.
- Cleanup: deleted routes 404 (or 301 where §IA lists them); no dangling references (build + `git grep`); inline-style allow-list empty; rights matrix complete for every `/sales/*` page and `api/sales/*` endpoint.

## Success criteria
- `dotnet build` + `dotnet test` green.
- An ambassadeur signs in (2FA), sees Lobsy Partner with "Ambassadeur AM-…", Mijn kandidaten with counts only, the tier card, and the same wallet as a salesmanager.
- No page, API response, PDF or mail in the partner portal contains a candidate's name or initials, or an employer's contact data.
- Old salesmanager/ambassadeur page components are gone; every old URL still lands on its new home.

Done. Report the stack table (file → branch → PR → status) and anything deferred.
