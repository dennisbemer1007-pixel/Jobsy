# 09. Salesmanager aanbevelen, AVG retention and privacy statement, cleanup + docs

Read `00-README.md` first (§0, §IA, §R, §P, D8 (Ambassadeur parked), D13, D15). Branch `cursor/salesmanager-9` from `cursor/salesmanager-8`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-9` (from `cursor/salesmanager-8`) |
| PR title | `feat(sales): recommend flow with notice + retention, privacy statement, cleanup + docs` |
| PR body starts with | `Stacked on #<PR 08> (cursor/salesmanager-8)` + the list of deleted files/endpoints + the list of parked ambassadeur files that were deliberately kept |
| Mockups | none new; `/sales/aanbevelen` follows the salesmanager pages' layout and components |
| Split seam | **09a** = aanbevelen redesign + retention + privacy statement (09.2–09.4). **09b** = cleanup + docs (09.5–09.6) |

## Goal
The recommend flow treats the recommended person fairly (informed, able to object, data removed on time), the privacy statement explains what a salesmanager link does, the old salesmanager pages and endpoints are gone, and the docs describe the finished portal with the Ambassadeur role parked (D8).

## 09.1 Today (verify first)
- `Components/Pages/SalesManager/Referrals.razor` + `SalesManagerApplicationService` (`me/applications`, admin approve/reject); `SalesManagerApplication` stores `CandidateFullName`, `CandidateEmail`, `Motivation` with no retention; the recommended person is not informed.
- After 01–08 the old salesmanager page components are still referenced from the moved routes (01.5) and the inline-style allow-list (01.9).
- The ambassadeur code (pages under `Components/Pages/Ambassadeur/*`, `AmbassadeurHomePanel.razor`, `AmbassadeursController`, `AmbassadeurDashboardService`, `AmbassadeurOnboardingService`, `AmbassadeurFlyerPdfService`, `AmbassadeurCommissionRules`) is parked behind `AmbassadorsEnabled` (01.10). **Out of scope for 09** except the gate check in 09.5.

## 09.2 Salesmanager aanbevelen `/sales/aanbevelen` (salesmanager with `CanRecruitSalesManagers`)
- Header "Salesmanager aanbevelen", lead "Ken je iemand die goed kan verkopen? Beveel hem of haar aan. Lobsy beslist."
- Explainer card: "Jij krijgt {indirect} % extra in jaar 1 over de aankopen van hun werkgevers. Zij krijgen {referredYear1} % in jaar 1." (from settings), and "Via je aanbevelingen verdiend: € {x}" (sum of `IndirectTokenCommission`).
- Form (drawer or inline card): Naam, E-mail, "Waarom past deze persoon?" (max 500), required checkbox (D15) "Deze persoon weet dat ik hem of haar aanmeld en vindt dat goed." Primary "Aanbeveling versturen". Server: stores `ReferrerConfirmedPermission = true`; rejects duplicates for the same e-mail within 60 days.
- On submit: send `SalesMail.RecommendedNotice` to the recommended person (D15): who recommended them (the salesmanager's display name), what Lobsy stores and for how long, a link "Ik wil dit niet – verwijder mijn gegevens" (one-time token, 60 days) → `SubjectObjectedAtUtc`, application `Rejected` with reason "Bezwaar", PII cleared **immediately**. `SubjectNotifiedAtUtc` set.
- List "Jouw aanbevelingen": date, name (or "Gegevens verwijderd" after clearing), status (Wacht op Lobsy · Goedgekeurd · Afgewezen · Verlopen · Bezwaar); no e-mail shown after submit.
- Admin approve/reject stays in the existing admin flow (sales admin tab), now showing "Persoon geïnformeerd op {datum}".

## 09.3 Retention (D13) in `DataRetentionHostedService`
- `SalesManagerApplication`: `Pending` older than 60 days → status `Expired` (enum value from 01) and PII cleared; `Rejected` → PII cleared 30 days after `ReviewedAtUtc`; `Approved` → PII cleared 30 days after `ReviewedAtUtc` once `ProvisionedUserId` is set (the account holds the data from then on). "PII cleared" = `CandidateFullName = ""`, `CandidateEmail = ""` (`CandidateEmailSha256`, set at submit, keeps the 60-day duplicate rule working), `Motivation = ""`, `PersonalDataClearedAtUtc = now`.
- `SalesLinkClickDaily` older than 25 months deleted (if 03 didn't already wire it).
- Invoices, ledger, payout requests/runs: **kept** (7 years, fiscal); no deletion job, documented.
- Test each rule with a fixed clock.

## 09.4 Privacy statement
- Add a short paragraph (string or the privacy page's content source) "Aanmelden via een salesmanager" explaining the cookie `lobsy_sales_ref` (30 days, only the code), what the salesmanager sees (employers: trade name, place, own commission; never contacts, candidates or vacancies) and why (commission). Dennis reviews the text in the PR.
- Don't add or change any ambassadeur text (parked); the existing `lobsy_ambassadeur_ref` cookie entry stays in the cookie list (03.2).

## 09.5 Cleanup
- Delete the old salesmanager components now unused: `Pages/SalesManager/*.razor` (Dashboard, SalesToolkit, Referrals, Onboarding, Invoices, PayoutCheckoutStub), `Home/SalesManagerHomePanel.razor`, and `Shared/PayoutCheckoutStubView.razor` only if neither the werkgever partner stub nor the parked ambassadeur `PayoutCheckoutStub` uses it (check; if the ambassadeur page uses it, keep it).
- **Keep** (parked, D8): every `Pages/Ambassadeur/*.razor` incl. the public `Landing.razor`, `Home/AmbassadeurHomePanel.razor`, `AmbassadeursController` and all its endpoints, the ambassadeur services, settings, entities and data, and `RoleNavCatalog.Ambassadeur`. Don't refactor them. List them in the PR as "deliberately kept".
- Keep public `Pages/Partner/PartnerSales.razor`.
- API: delete the self-service endpoints no page uses anymore on `SalesManagersController` (`me/dashboard`, `me/profile`, `me/invoices*`, `me/payouts/*` incl. the 410s from 07, `me/applications` if replaced), after a `git grep` shows no caller (Web, tests, docs). Admin endpoints stay. List every deleted route in the PR.
- `SalesManagerPayoutService` checkout/stub code paths removed; `AllowStubPayouts` stays only if other code uses it (the parked ambassadeur checkout does; then it stays).
- The inline-style allow-list (01.9) is empty; the test asserts it.
- `RoleNavCatalog.SalesManager` array removed (nav comes from `SalesNav` since 01).
- Gate check: re-run the 01.10 parking tests unchanged; they must still pass after the cleanup (nothing in 09 may make an ambassadeur page, endpoint, cookie or commission path reachable while off).

## 09.6 Docs
- `docs/security/roles-matrix.md`: SalesManager row (portal, 2FA mandatory, what they see: employers trade name/place/own commission; payouts via request + admin approval). The Ambassadeur row stays **"Geparkeerd"** as 01 set it.
- ADR `docs/adr/00NN-sales-partner-portal.md` (next free number, Dependencies G): D1–D16 in short, including D8: Ambassadeur parked behind `AmbassadorsEnabled` (default off), data kept, shared ledger/invoice/payout code role-agnostic, 2FA required when re-enabled, what a re-enable needs (own spec: portal pages, commission tier, candidate counts only, candidate notice).
- `docs/ROUTES.md` (ambassadeur routes marked "geparkeerd"), `PageHelpDocs`, `HowLobsyRoleGuides` (salesmanager guide points to `/sales/link`; the ambassadeur guide stays hidden while parked), `CHANGELOG.md`.

## Tests
- Aanbevelen: permission checkbox required; duplicate rule; notice mail sent once; objection link clears PII immediately and is single-use; list shows "Gegevens verwijderd".
- Retention rules (09.3) with a fixed clock.
- Cleanup: deleted routes 404 (or 301 where §IA lists them); no dangling references (build + `git grep`); inline-style allow-list empty; rights matrix complete for every `/sales/*` page and `api/sales/*` endpoint.
- Parking (01.10) tests still green; with the switch on in a test host, the kept ambassadeur pages still build and render (smoke test), so nothing parked was broken by the cleanup.

## Success criteria
- `dotnet build` + `dotnet test` green.
- A salesmanager with `CanRecruitSalesManagers` recommends someone; that person gets the notice mail and can object; data is cleared on time.
- No page, API response, PDF or mail in the partner portal contains a candidate's name or initials, or an employer's contact data.
- Old salesmanager page components are gone; every old salesmanager URL still lands on its new home. The parked ambassadeur code is still there and still unreachable while `AmbassadorsEnabled` is off.

Done. Report the stack table (file → branch → PR → status) and anything deferred.
