# 04. KvK refresh + holds + uitleenregistratie: weekly re-check, address changes, deregistration, beyond 25 km, `ILenderRegistrationCheck`

Read `00-README.md` first. Branch `cursor/intermediair-4` from `cursor/intermediair-3` (or `-3b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/intermediair-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Location only from KvK (no free location field anywhere), the opdrachtgever's identity never leaves the server in hidden mode, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/intermediair-4` |
| PR title | `feat(intermediair): weekly KvK refresh with holds, and an isolated uitleenregistratie check gating publication` |
| PR body starts with | `Stacked on #<PR 03> (cursor/intermediair-3)`, and which case of Dependencies F applied |
| Mockups | `im-d1-dashboard.png` (Te doen texts), `im-d2-opdrachtgevers.png` ("Gecontroleerd in KvK · {datum}", "Opnieuw ophalen") |
| Split seam | **04a** = KvK refresh + holds + signals (04.2–04.4). **04b** = uitleenregistratie service + gate + admin tab (04.5–04.7) |

## Goal
KvK stays the source of truth for every link. A weekly job re-fetches each Active/PendingReview link. Address changes flow into the link and the vacancies (with a Te doen signal). A deregistered or vanished vestiging puts its vacancies on hold. A move beyond 25 km puts hidden vacancies on hold (never auto-revealed). Separately, a bureau can only publish once its uitleenregistratie is verified, through one isolated service that the future werkgever-aanmelding spec can reuse.

## 04.1 Today (verify first)
- 01's `refresh` endpoint updates the snapshot only. 02's `HoldReason`, `IntermediaryVacancyWriter`, `IntermediaryVacancyLocation`.
- `KvkVerificationRetryHostedService` (pattern for a periodic job with backoff); `KvkServiceUnavailableException`.
- Check what the KvK DTOs expose for an ended vestiging (e.g. a `datumEinde` / `indNonMailing` / 404 on the vestigingsprofiel). The stub needs a deregistered case.

## 04.2 `IntermediaryClientKvkRefresher` (one code path for the job and "Opnieuw ophalen")
- `RefreshAsync(IntermediaryClient link, CancellationToken)`:
  1. Fetch the establishment (KvK number → establishments → match on vestigingsnummer).
  2. **KvK unavailable** → no change, `LastKvkCheckAtUtc` untouched, retry next run. After 3 failed weeks in a row: signal `KvkCheckOverdue` (bureau) + admin log.
  3. **Not found / ended** → `KvkStatus = Deregistered` (or `NotFound`); every Active vacancy of the link gets hold `KvkDeregistered`; signal **"KvK meldt {opdrachtgever} {vestiging} als uitgeschreven. {n} vacatures staan op pauze."** New vacancies for the link are blocked (409 `client_deregistered`). The link can only be archived.
  4. **Name/address changed** → store `PreviousAddress`, update name/address/postcode/number, re-geocode (01 resolver).
     - Geocode fails → keep the old location, set a flag, and signal "We konden het nieuwe KvK-adres niet op de kaart zetten".
     - Success: `LastKvkChangeAtUtc = now`, recompute every vacancy of the link through `IntermediaryVacancyWriter` (location for real mode; the 25 km check for hidden mode).
     - Signal **"KvK-adres van {opdrachtgever} is gewijzigd · werklocatie bijgewerkt uit KvK · controleer {n} vacatures"** with a link to the opdrachtgever.
  5. **Hidden and now > 25 km** from its owner vestiging → hold `OutsideMaskRadius` (stays hidden, D5/D16). Signal **"{vacature} staat nu {n} km van je vestiging. Kies 'Toon op echte werklocatie' of een andere vestiging."** The hold lifts automatically when the bureau switches mode or owner (02 endpoint) and the rules pass.
  6. **Unchanged** → `LastKvkCheckAtUtc = now`.
  7. `KvkStatus` back to `Active` (a KvK correction) lifts `KvkDeregistered` holds automatically (signal "weer actief").
- Every change writes `PlatformLog` `intermediary.client.kvk-changed|deregistered|reactivated` (ids, no addresses) and refreshes the discovery entries of the affected vacancies.

## 04.3 Weekly job
- `IntermediaryClientKvkRefreshHostedService` runs daily at 03:00 Europe/Amsterdam. Each run takes the links with `LastKvkCheckAtUtc < now − 7 days` (oldest first), capped per run by config `Intermediary:KvkRefresh:MaxPerRun` (default 500), with a delay between calls (default 250 ms) to respect KvK quotas.
- It also re-checks the bureau vestigingen (Type `Intermediary` companies) the same way. An address change there recomputes every hidden vacancy owned by that vestiging (location + 25 km).
- Metrics: one `PlatformLog` summary per run (checked, changed, deregistered, failed).

## 04.4 Signals (data for Te doen)
- `IIntermediarySignalsService.GetAsync(bureau)` returns typed items, each `{ Kind, Severity, Title, Detail, ActionLabel, ActionUrl, ClientId?, VacancyId?, CreatedAtUtc }`:
  - the kinds from 04.2
  - `DeclarationMissing` (02 migration, D20): "Bevestig je opdracht met {opdrachtgever}"
  - `ClientPendingReview`: "{n} opdrachtgevers wachten op controle door Lobsy"
  - `ClientRejected`: with the admin reason
  - `LenderRegistrationPending` (04.6)
  - `NoLiveVacancy`: "{opdrachtgever} heeft nog geen live vacature" (d1), only for links < 30 days old
- API `GET api/intermediary/signals`. 06 renders them (werkgever 02 Te doen when present, Dependencies A).
- The mockup d1 item "vacature staat op 24 km": see README §0 differences (only > 25 km is a signal; 20–25 km is an info line in the vacancy list, 06).

## 04.5 `ILenderRegistrationCheck` (isolated, D23, Dependencies F)
- **Interface** `Jobsy.Core/Interfaces/ILenderRegistrationCheck.cs`:
  - `Task<LenderRegistrationState> GetStateAsync(Guid bureauOrgId, CancellationToken)`
  - `Task<LenderRegistrationState> StartForNewBureauAsync(Guid bureauOrgId, string kvkNumber, CancellationToken)` (creates a `Pending` row and asks every enabled provider)
  - `Task RecordDecisionAsync(Guid bureauOrgId, LenderRegistrationDecision decision, Guid adminUserId, CancellationToken)`
  - `bool CanPublish(LenderRegistrationState)`
- **Providers** behind `ILenderRegistrationProvider` (`Name`, `IsEnabled`, `Task<ProviderResult> CheckAsync(kvkNumber)`), in `Jobsy.Infrastructure/Services/LenderRegistration/`:
  - `WaadiKvkProvider`: KvK has no public Waadi API. It returns `Unknown` and a deep link to the KvK Waadi check for the admin. Enabled.
  - `WttaNauProvider`: slot for the NAU public register (expected 1 July 2027). Disabled by default (`LenderRegistration:WttaNau:Enabled=false`). A TODO with the date only, no scraping.
  - `AdminManual`: decisions recorded by admin.
- **Entity** `LenderRegistration` (§D), with history rows; the latest decides. `Verified` can carry `ValidUntil` (admin sets it, e.g. for a Wtta certificate).
- **The only call site** in registration is one call after an SBI-78 activation in `CompanyRegistrationService` (`StartForNewBureauAsync`). Existing bureaus get a `NotChecked` state via a small one-shot backfill (no publishing block for **already live** vacancies; the gate applies to new publications, extensions and pushberichten).

## 04.6 Publish gate
- `VacancyProductService` publish/extend/highlight/pushbom and the approval path, for vacancies with `IntermediaryCompanyId != null`: `CanPublish` must be true, else 409 `{ code: "lender_registration_pending", message: "We controleren je uitleenregistratie nog. Je kunt vacatures als concept opslaan; publiceren kan zodra Lobsy dit heeft bevestigd." }`. Drafts are always allowed.
- The gate runs **before** any token spend (no spend on a 409; test).
- Signal `LenderRegistrationPending` for the bureau. `Rejected` → signal with the admin note; publications stay blocked.
- There is no admin switch: the gate is always on for intermediaries (D9).

## 04.7 Admin tab "Uitleenregistratie" (`/admin/intermediairs`)
- Table: Bureau · KvK · Status · Bron · Referentie · Gecontroleerd op · Geldig tot · actions. Filters: status.
- **Detail drawer:**
  - the KvK Waadi-check link
  - fields Referentie (registratienummer), Bron (Waadi via KvK / Wtta / Handmatig), Geldig tot (optional), Notitie
  - buttons **Bevestigen** / **Afwijzen** (note required)
- Audit (Dependencies D). In-app notification to the bureau users.
- API `GET api/admin/intermediary/lender-registrations`, `POST …/{bureauId}/decision`.

## Tests
- Refresher, each branch: unchanged, address change (vacancies recomputed, real-mode pin moved, hidden unchanged), move beyond 25 km (hold, stays hidden, hold lifted on mode switch), deregistered (holds + blocked new vacancies + reactivation lifts), geocode failure on change, KvK down (no change; overdue after 3 weeks).
- Job selection (7-day window, cap, order); bureau vestiging address change → hidden vacancies recomputed.
- Signals service: each kind; Te doen text snapshot.
- Lender check:
  - `StartForNewBureauAsync` called exactly once on SBI-78 activation and never for employers
  - the gate on publish/extend/highlight/pushbom with no token spend on 409
  - drafts allowed
  - admin decision + audit + notification
  - `ValidUntil` expiry → back to not publishable
- Matrix rows: bureau can read its own state, can't decide; other roles 403.

## Success criteria
- A vestiging deregistered in KvK disappears from the map within one job run, with a clear Te doen item. A hidden vacancy never flips to real mode by itself.
- A new bureau can do everything except publish until an admin confirms the uitleenregistratie. The check lives in one service with one registration call site.

Done → next: `05-kandidaatinzichten-intermediair.md`.
