# 05. Kandidaatinzichten for the Intermediair: BM rules, bureau wallet, area around the bureau vestiging(en)

Read `00-README.md` first. Branch `cursor/intermediair-5` from `cursor/intermediair-4` (or `-4b`). **Re-run Dependencies check B first.**

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/intermediair-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Location only from KvK (no free location field anywhere), the opdrachtgever's identity never leaves the server in hidden mode, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/intermediair-5` |
| PR title | `feat(intermediair): Kandidaatinzichten for bureaus with the bedrijfsmanager rules, paid from bureau tokens` |
| PR body starts with | `Stacked on #<PR 04> (cursor/intermediair-4)`, and which case of Dependencies B applied |
| Mockups | none of its own; the page is the werkgever one (`docs/mockups/werkgever-redesign/bm-d6-kandidaatinzichten.png`, `bm-m3-…`); the sidebar lock as in `im-d1` |
| Split seam | none (small) |

## Goal
A bureau user sees Kandidaatinzichten exactly like a bedrijfsmanager (D10): the same free/locked split, the same unlock or access rule, the same k-anonymity and forbidden identity filters. The scope is the bureau's own vestiging(en) (never an opdrachtgever's werklocatie), and every token spend comes from the bureau wallet.

## 05.1 Today (verify first)
- `CandidateInsightsController.IsInsightsRole` (~L88) = BranchManager/RegionalManager/EnterpriseManager only, so the Intermediary gets 403 on `api/candidate-insights*`.
- `CandidateInsightsService` builds the scope from the user's accessible companies (branches) and their locations. Radii 10/20/30 km, periods 30/90/365 days. `CandidateInsightsAccess.IsFullAccessAsync(tokens, scope.Companies)` (full when any scope wallet balance > 0) and `ResolveWalletCompanyId(company)`.
- The page `Pages/Employer/CandidateInsights*` is `[Authorize(Roles = Branch/Regional/Enterprise)]`.
- After 02, an Intermediary's accessible companies are **only** bureau companies.

## 05.2 Role and scope
- Add `Intermediary` to `IsInsightsRole` and the page's `[Authorize(Roles = …)]` (and to werkgever 07's role set when present).
- **Scope = the bureau vestigingen** from `IIntermediaryContext` (never `IntermediaryClient` locations). The branch picker lists bureau vestigingen; with one vestiging it is a fixed chip. `?branchId=` for a non-bureau company → 403 (the existing `ForbiddenCompanyAccessException` path).
- `ForbiddenIdentityParams`, `KAnonymityThreshold = 10` and the aggregated shape: unchanged, and asserted for the Intermediary in tests.

## 05.3 Access rule (Dependencies B)
- **B present (werkgever 07 landed):**
  - the Intermediary follows `CandidateInsightsAccess.GetCoverageAsync` with BM semantics: organisation scope = bureau org, per-vestiging scope = a bureau vestiging, wallet = `ResolveWalletCompanyId` of the bureau
  - `POST unlock` spends `TokenSpendReason.InsightsUnlock` from the bureau wallet, with the same idempotency, expiry and renew window
  - there is no VM-style "request" flow for a bureau (all bureau users have the same rights, §R)
- **B absent:**
  - the Intermediary gets **the BM rule of that moment** (`IsFullAccessAsync` on the bureau vestigingen' wallets)
  - no new spend reason, no unlock UI in this stack
  - add `Intermediary_insights_rule_equals_BM_rule`: the same fixture evaluated as BM and as Intermediary gives the same free/full result, so werkgever 07 carries the Intermediary along
  - say in PR 05 that werkgever 07 must list `Intermediary` in its role set (a TODO comment next to `IsInsightsRole`)

## 05.4 UI
- Nav item Kandidaatinzichten for the Intermediary. Today's nav or `WerkgeverNav` depending on Dependencies A (06 finalizes). Gold lock only while the current scope isn't full (werkgever D20).
- Page copy: the scope label reads "{bureau vestiging}" / "Alle vestigingen van {bureau}". No mention of opdrachtgevers on this page.

## Tests
- Intermediary: free part 200; full part per the applicable rule; `branchId` of a foreign company or of an opdrachtgever-derived id → 403; forbidden identity params → 400 as for BM; k < 10 suppression.
- B present: unlock from the bureau wallet (ledger row on the bureau), idempotent, renew window. B absent: the equality test.
- Matrix rows (§R), page role attribute test.

## Success criteria
- A bureau user and a bedrijfsmanager with equivalent fixtures get identical Kandidaatinzichten behaviour. Every token spent comes from the bureau, and the area is always around the bureau vestiging(en).

Done → next: `06-intermediair-in-werkgever-shell.md`.
