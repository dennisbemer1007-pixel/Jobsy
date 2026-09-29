# 07 · Kandidaatinzichten as a paid unlock (tokens), server-side locking, admin settings

> Read `00-README.md` first. §0 (privacy, gold pattern), §R and D8, D9, D10, D18, D19 and D20 apply, plus Dependencies A and C.

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-7` from `cursor/werkgever-redesign-6` (stacked) |
| PR | ONE PR into `acceptatie`: `feat(werkgever): Kandidaatinzichten paid unlock with tokens, server-side locked data and admin settings`. Stacked on #<PR of 06> (`cursor/werkgever-redesign-6`) |
| Mockups | `bm-d6-kandidaatinzichten.png` (desktop, free + locked + gold block), `bm-m3-kandidaatinzichten-gratis.png` (mobile) |
| Split seam (if too big) | 7a = domain + API + server-side stripping + settings; 7b = UI (free/locked/premium block, nav lock, mobile, requests) |

**Goal.** Kandidaatinzichten becomes a real paid feature. The free version shows enough to be useful and makes people curious. The full version costs tokens for a set period. **Locked numbers never leave the server.**

## 07.1 Today (verify first)
- `Jobsy.Core/Rules/CandidateInsightsAccess.cs`:
  - `IsFullAccessAsync(tokens, scopeCompanies)` = **any wallet with balance > 0**
  - `ResolveWalletCompanyId` (parent when `TokensManagedByEnterprise`)
  - The doc comment says "Viewing insights never spends tokens". That changes now.
- `CandidateInsightsService` (Infrastructure):
  - ~L86 calls `IsFullAccessAsync`
  - the cache key includes `isFullAccess`
  - it already produces `LockedSections` (`LockedDreamJobs4To10`, `LockedDna`, `LockedStory5To10`) and `Take(isFullAccess ? 10 : 3)` for dream jobs
  - **Most other sections are computed and sent regardless of access.**
- `CandidateInsightsController` (`api/employer/candidate-insights`):
  - `GET` with `branchId`, `radiusKm` (default 20), `period` (default 90), plus `GET branches`
  - `RequireEmployer`; `IsInsightsRole` = BranchManager/RegionalManager/EnterpriseManager; `ForbiddenIdentityParams`
- DTO `CandidateInsightsDto`:
  - `Scope` (`IsFullAccess`)
  - `Kpis` (`CandidatesInRadius`, `AvgHoursPerWeek`, `Candidates32PlusHours`, `Active30d`, `MatchingYourVacancies`)
  - `DreamJobsTop`, `WorkFields`, `DnaRiasec`, `Competences`, `Personality`, `Priorities`, `WorkKinds`
  - `Density` (cells), `Vacancies` (`VacancyReach` with `MatchingCandidates` + `Tips`), `Trend`, `LockedSections`
- Privacy: `CandidateInsightsPrivacy.KAnonymityThreshold = 10`.
- UI: `CandidateInsights.razor` (436 lines) + `InsightsLockedBlock` (blur + CTA to `TokensHref`), `InsightsStoryCard`, `InsightsDistributionBars`, `InsightsRankedList`, `InsightsTipsBlock`, `InsightsVacanciesList`; strings in `UiStringsCandidateInsights.cs`; CSS `kandidaatinzichten.css` (`.insights-locked`, blur 6 px).
- Gold premium pattern: `features/testresultaten.css` (`.test-result-lock-chip`, `.test-result-card-locked__*`, `.test-result-stamp`, `.test-result-premium*`, `.btn-gold`, `.btn-gold__price`).
- Tokens:
  - `ITokenLedgerService.TrySpendAsync(companyId, reason, vacancyId, actorUserId, branchCompanyId, note, onSuccessBeforeCommit, costOverrides)`
  - `TokenSpendReason` 1–5; `TokenSpendCost` (Reason, CostTokens, IsActive), edited generically in `SettingsAdmin.razor` "Financieel — spend costs" (`SettingsController`)
  - `PlatformFeatureSettings` singleton (admin settings)
- Tests: `CandidateInsightsServiceTests`, `…BunitTests`, `…PrivacyTests`, `…DtoPiiGuardTests`, `…FingerprintTests`, `…AuthzApiTests`, `…PlaywrightTests`, `…OnWriteApiTests`, `UiStringsCandidateInsightsTests`.

## 07.2 Free vs locked (exactly the mockup; this spec wins where noted)

| Part | Free | Full (unlocked) |
|---|---|---|
| Filters: vestiging/scope, straal, periode | ✓ | ✓ |
| Filter "Werkveld" (only if it exists in code) | shown with a lock + "Premium", disabled | ✓ |
| KPI "Kandidaten binnen {km} km" (`CandidatesInRadius`) | ✓ (badge "Gratis") | ✓ |
| KPI "Gewenste uren" (`AvgHoursPerWeek`) | ✓ (badge "Gratis") | ✓ |
| KPI "Passend bij je vacatures" (`MatchingYourVacancies`) | locked card, **no value** | ✓ |
| KPI "32+ uur per week" (`Candidates32PlusHours`; replaces the mockup's "Direct beschikbaar") | locked card, **no value** | ✓ |
| KPI "Actief afgelopen 30 dagen" (`Active30d`) | locked | ✓ |
| Map: vestiging pins + radius ring + total | ✓ | ✓ |
| Map: density per wijk (`Density`) | locked; overlay "Waar wonen ze precies? Dichtheid per wijk en reistijd per vestiging" | ✓ |
| Werkvelden, Prioriteiten, Soort werk | locked cards with neutral placeholder bars | ✓ |
| Droombanen, DNA (RIASEC), Competenties, Persoonlijkheid | locked | ✓ |
| Trend, Vacatures reach + tips | locked | ✓ |
| Story cards | the ones built only from free data | all |
| Export | button disabled with a lock | CSV of the aggregated numbers (pdf deferred) |
| Anonymity note "Altijd anoniem · groepen onder 10 kandidaten tonen we niet" | ✓ | ✓ |

- **Server-side (D8):** when not fully accessed, the service **doesn't compute** the locked parts (cheaper), and the DTO carries `null`/empty for them plus `LockedSections` listing every locked key. The **JSON must not contain** locked values, counts or labels. The placeholder bars in the UI are static, not data.
- K-anonymity applies to both versions as today.

## 07.3 Domain
- `TokenSpendReason.InsightsUnlock = 6`. Migration seeds `TokenSpendCost(InsightsUnlock, 12, IsActive: true)` (D9). Because the admin "spend costs" editor is generic, the row shows up there automatically. Check it renders a readable Dutch label ("Kandidaatinzichten ontgrendelen") and add the label if the editor uses a label map.
- `PlatformFeatureSettings` + migration:
  - `CandidateInsightsEnabled` (bool, default `true`)
  - `CandidateInsightsUnlockDays` (int, default `90`, validated 7–365)
  - `CandidateInsightsUnlockPerBranch` (bool, default `false`)
- Entity `CandidateInsightsUnlock`:
  - `Id`
  - `WalletCompanyId` (the organisation wallet via `ResolveWalletCompanyId`)
  - `ScopeKind` (`Company` = the whole organisation, `Branch`)
  - `ScopeCompanyId` (the organisation or vestiging id)
  - `UnlockedAtUtc`, `ExpiresAtUtc`
  - `PriceTokens`, `DurationDays` (a snapshot of the settings at purchase)
  - `ActorUserId`
  - `TokenTransactionId`
  - `IdempotencyKey` (unique)
  - Index `(ScopeCompanyId, ExpiresAtUtc)`.
- Entity `CandidateInsightsUnlockRequest` (VM → BM): `Id`, `WalletCompanyId`, `BranchCompanyId`, `RequestedByUserId`, `Status` (`Open`, `Ontgrendeld`, `Afgewezen`, `Ingetrokken`), `HandledByUserId?`, `HandledAtUtc?`, `CreatedAtUtc`. At most one `Open` per vestiging.

## 07.4 Access rule (replaces "balance > 0")
- `CandidateInsightsAccess.GetCoverageAsync(db, clock, scopeCompanies, settings)` returns `InsightsCoverage(bool IsFull, int CoveredCount, int TotalCount, DateTime? ExpiresAtUtc, bool CanRenew)`.
  - **Company scope** (default): full when an active `ScopeKind.Company` unlock exists for the organisation wallet.
  - **Per vestiging:** each selected vestiging must have an active `Branch` unlock (or a `Company` unlock, which always covers all). For multiple vestigingen: full only if **all** are covered (D19). Otherwise free + "{n} van {m} vestigingen ontgrendeld".
  - `ExpiresAtUtc` = the earliest expiry among the covering unlocks. `CanRenew` = within 14 days of expiry (D8).
- `IsFullAccessAsync` is removed. All callers use the coverage, and the cache key uses `coverage.IsFull` + the covered ids.
- **An unlock bought while scope = per vestiging stays valid when admin switches to company-wide, and vice versa** (coverage checks both kinds).
- Update the doc comment: "Full access = an active paid unlock (tokens). Viewing never spends tokens; unlocking does."

## 07.5 API (`CandidateInsightsController`)
- `GET` (existing): returns the DTO + `Coverage` + `Offer` (`PriceTokens`, `DurationDays`, `ScopeKind`, `CanUnlock`, `CannotUnlockReason`: `ReadOnlyRole` | `NeedsBranchScope` | `InsufficientBalance` | `FeatureDisabled`, `WalletBalance` for BM/VM).
- `POST unlock`:
  - Body `{ scope: "company" | "branch", branchId? }` + header `Idempotency-Key` (required, 400 when missing).
  - Rules:
    - RM → 403 `read_only`
    - VM → 403 unless `CandidateInsightsUnlockPerBranch` **and** `branchId` is his own vestiging, paid from **his allocated balance** (branch wallet via `branchCompanyId`, as allocation does today)
    - BM → company (or branch when per vestiging)
  - **Idempotency:**
    - same key → return the stored result
    - an active unlock already covering the scope and outside the renew window → 200 with the existing unlock, **no spend**
    - within the renew window → a new unlock with `UnlockedAtUtc = now` and `ExpiresAtUtc = currentExpiry + DurationDays` (stacks)
  - Spend: `TrySpendAsync(wallet, InsightsUnlock, vacancyId: null, actor, branchCompanyId, note: "Kandidaatinzichten {scope} t/m {datum}", onSuccessBeforeCommit: insert the unlock row, costOverrides: none)`, so ledger row + unlock are one transaction. Serializable, or a unique partial index on the active scope to prevent a double spend under concurrency.
  - Insufficient balance → 402 `insufficient_tokens` (BM UI opens `TokenTopUpDialog`; VM UI offers "Tokens aanvragen" (06)).
  - Returns `{ unlockId, expiresAtUtc, spentTokens, balanceAfter }`.
- `POST unlock-request` (VM only, own vestiging): creates a `CandidateInsightsUnlockRequest` + the 02 Te doen kind **`InsightsRequests`** for the BM ("{vestiging} vraagt Kandidaatinzichten aan" + "Ontgrendelen" (calls `unlock` for that branch or company as settings allow, marking the request `Ontgrendeld`) / "Afwijzen"). In-app notification both ways (`IUserNotificationService`).
- `GET export.csv` (full access only; 403 `locked` otherwise): the aggregated numbers of the current filters. K-anonymity applies. Writes no PII.
- **Feature off (D18):** `CandidateInsightsEnabled = false` → all insights endpoints answer 404 `{ code: "feature_disabled" }` (via `[RequiresFeature(PlatformFeature.CandidateInsights)]` when Dependencies A applies, and add that enum value; otherwise a small filter). Nav item hidden. Existing unlocks keep their `ExpiresAtUtc`; no refunds.

## 07.6 Admin settings (D9, D10; Dependencies C)
- **Price:** the `TokenSpendCost` row for `InsightsUnlock` in the **existing** spend-cost editor, or in admin-redesign 06 "Prijzen & pakketten" if it exists (it lists spend costs; add the row to its "Wat bepaalt welke prijs?" table: "Kandidaatinzichten ontgrendelen · per organisatie of vestiging · {n} tokens · {d} dagen").
- **Switches/duration/scope:**
  - With admin-redesign 05 (`PlatformSettingsCatalog`): three descriptors in group **Functies › Werkgevers**:
    - `CandidateInsightsEnabled` (Bool, "Kandidaatinzichten beschikbaar")
    - `CandidateInsightsUnlockDays` (Int 7–365, "Looptijd ontgrendeling (dagen)")
    - `CandidateInsightsUnlockPerBranch` (Bool with an impact note, "Ontgrendelen per vestiging in plaats van per organisatie")
  - Otherwise: a small "Kandidaatinzichten" section in today's `SettingsAdmin.razor` next to the feature toggles, saved through the existing settings endpoint.
- **Audit (D10):**
  - With `IAdminAuditLog` (admin-redesign 07): `settings.platform.update` / `settings.pricing.update` with before/after for these keys.
  - Otherwise: write a structured `PlatformLog` row (`Category = "AdminSettings"`, the actor, keys, before → after) and add a TODO + test so it moves to `IAdminAuditLog` when that lands (the admin 07 reflection guard will catch it).
  - Never `PersonalDataAccessLog`.
- Price or duration changes apply to **new** unlocks only (the snapshot on the row). The admin impact note says so.

## 07.7 UI `/werkgever/kandidaatinzichten` (d6, m3)
- Header:
  - h1 "Kandidaatinzichten" + pill "Gratis versie" (free) or gold "Volledig t/m {datum}" (full)
  - lead "Wie zoekt werk rond je vestigingen. Anoniem en opgeteld: je ziet nooit individuele kandidaten."
  - actions: "Naar talentpool", "Exporteren" (locked when free)
- Filter row: scope select (from `EmployerScope`), radius segmented (the existing options), period, and "Werkveld" locked when free (if it exists).
- **KPI row:** free cards with a "Gratis" check badge; locked cards reuse the **test-result lock pattern** (`test-result-lock-chip` "Premium", **no value rendered**, a one-line question as sub: "Hoeveel passen bij je {n} vacatures?", "Hoeveel willen 32+ uur werken?").
- **Map card:** pins + ring + total always. Density is only rendered when full. When free: an overlay card with a lock "Waar wonen ze precies?"; header lock chip "Dichtheid" (lock icon, not an emoji); sub "Gratis: straal en totaal · < 10 kandidaten blijft leeg".
- **Locked section cards:**
  - header chip "Vergrendeld"
  - body = **static neutral placeholder bars** (not data, `aria-hidden`), blurred with the same values as `.test-result-card-locked__body`
  - `role="group"` + `aria-label="{titel}, vergrendeld"`
  - Replace `InsightsLockedBlock` internals with this pattern (one component, `Components/Werkgever/Insights/WgLockedCard.razor`; delete the old blur-hint copy).
  - **No "Voorbeelddata" stamp in production.**
- **Premium block** (gold, `test-result-premium` pattern, once at the bottom):
  - tag "Volledige inzichten", h3 "Weet precies wie er rond je vestigingen zoekt"
  - lead "Ontgrendel alles hierboven met echte cijfers voor {scope}. Altijd anoniem."
  - checks: "Dichtheid per wijk en reistijd", "Match met je vacatures", "Werkvelden, prioriteiten, droombanen", "Trends en export (CSV)"
  - CTA `btn-gold` "Ontgrendel volledige inzichten" + price "{n} tokens" + "{d} dagen · {scope label}"
  - fine print "Afrekenen met je tokensaldo · geen abonnement"
  - A **confirm dialog** (`LobsyFriendlyDialog`): "Kandidaatinzichten ontgrendelen voor {scope}? Dit kost {n} tokens. Je saldo wordt {saldo na}. Geldig t/m {datum}." On success: toast "Ontgrendeld t/m {datum}" and a reload with full data.
  - "Bekijk voorbeeldrapport" is **not built** (§0).
- Per role:
  - **RM:** the CTA is replaced by the text "Vraag je bedrijfsmanager om de volledige inzichten te ontgrendelen." (no button)
  - **VM, per vestiging:** "Ontgrendel voor {vestiging}" paid from his balance
  - **VM, company-wide:** secondary button "Vraag aan bedrijfsmanager" (→ `unlock-request`; after sending: "Aangevraagd op {datum}", disabled)
- **Full state:**
  - no locks
  - a small line under the header "Ontgrendeld t/m {datum} · {scope}"
  - from 14 days before expiry: "Verlengen · {n} tokens" (stacks)
  - partial coverage (D19): free view + "{n} van {m} vestigingen ontgrendeld" + the CTA for the rest
- **Sidebar (D20):** a small gold lock icon next to "Kandidaatinzichten" only while the current scope isn't full (`WerkgeverNavContext.InsightsLocked`). No dashboard upsell.
- **Mobile (m3):**
  - top bar with back + title; pill "Gratis versie" + "{vestiging} · {km} km · {period}"
  - 2×2 KPI's (2 free, 2 locked)
  - map with the density overlay, a locked Werkvelden-en-prioriteiten card
  - the compact premium block (CTA "Ontgrendel" + price)
  - the anonymity note
- The expiry reminder e-mail is **deferred**. Leave `// TODO(D8): reminder` in the service where it would hook in, plus an ADR-free note in the PR.
- Strings: extend `UiStringsCandidateInsights.cs` (nl/en/pl/ro/ar). Remove the unused "Insights.Cta.Full"/"Insights.Locked.BlurHint" keys when replaced (keep parity tests green).

## Tests
- **No leak (the most important):** `CandidateInsightsLockedJsonTests` calls the real API as BM (no unlock), RM and VM with seeded data and asserts that the JSON has **no** values for every locked key (`MatchingYourVacancies`, `Candidates32PlusHours`, `Active30d`, `Density`, `WorkFields`, `Priorities`, `WorkKinds`, `DreamJobsTop`, `DnaRiasec`, `Competences`, `Personality`, `Trend`, `Vacancies`) and that `LockedSections` lists them all. With an unlock: all are present. Extend `CandidateInsightsDtoPiiGuardTests`.
- `CandidateInsightsAccessTests`:
  - company coverage, per-branch coverage, partial (D19)
  - expiry boundary (exactly at `ExpiresAtUtc` → locked)
  - a switch of the scope setting keeps existing unlocks valid
  - `CanRenew` at 14 days
- `CandidateInsightsUnlockApiTests`:
  - BM unlock spends exactly `TokenSpendCost` and writes 1 ledger row + 1 unlock row
  - the same Idempotency-Key twice → one spend
  - a different key while active → 200 with no spend
  - renew in the window stacks the expiry
  - an insufficient balance → 402 and nothing written
  - a concurrency test (2 parallel requests → 1 spend)
  - RM → 403; VM with company scope → 403; VM per-branch own → 2xx from the branch wallet; VM foreign branch → 403
  - feature off → 404 `feature_disabled`
  - export 403 when locked, CSV when full
- `CandidateInsightsUnlockRequestTests`: VM creates → the BM Te doen item; BM unlock via the request → the request is `Ontgrendeld`; max one open.
- Settings: defaults seeded (12 / 90 / false / true); validation 7–365; an audit row written on change (the `IAdminAuditLog` or `PlatformLog` path); price changes don't touch existing unlocks.
- bUnit:
  - free vs full rendering, locked cards render **no numbers**
  - the RM text instead of the CTA, the VM request button and state
  - the confirm dialog content
  - partial coverage, the renew button window
  - the gold lock in the sidebar only while locked
  - mobile layout
- Update `CandidateInsightsFingerprintTests` / `OnWriteApiTests` / `PlaywrightTests` for the new gate (balance > 0 no longer unlocks). Playwright: BM free view → unlock → full view with the expiry line.
- Matrix rows for `unlock`, `unlock-request` and `export.csv`.

## Success criteria
- The free view matches d6/m3. A locked value is never in the browser (the JSON test proves it).
- Unlock is idempotent, spends the configured tokens once, shows the expiry, and can be renewed in the window.
- Admin can change price, duration, scope and on/off; every change is audit-logged.
- RM can't unlock; the VM follows the scope rule or asks the BM.

## Done → next
Push, open the PR, note its number. Continue with **`08-opruimen-termen-docs.md`**. If anything is red, stop and report.
