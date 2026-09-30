# 01. Fix: the employer culture profile reaches the match

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-1` from `origin/acceptatie`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Don't change the Cultuurscan questions, scoring, weights or storage. This file only wires existing data.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-1` |
| PR title | `fix(match): employer culture scan now counts in the candidate match (per vacancy company, vestiging → organisation fallback)` |
| PR body starts with | `Stacked on: none (first in the stack)` + the Dependencies outcome (README) |
| Mockups | none (backend); wr-d12 shows the resulting "Cultuur past" line |
| Split seam | none (small) |

## Goal
When an employer has completed the Cultuurscan, candidates' match scores for that employer's vacancies use it, as the calculator already intends. Today that part of the match is dead code.

## 01.1 Today (verify first)
- `Jobsy.Core/Rules/ProfileVacancyMatchCalculator.cs` ~L36–47: when the candidate's culture scan is complete and `input.CompanyCultureScores` has a value, it blends `0.55 * personality01 + 0.45 * CulturePersonalityFitRules.CultureFit01(candidate, company)`; otherwise personality only.
- `ProfileVacancyMatchService.cs` ~L77 copies `context.CompanyCultureScores` into the input, but `ProfileVacancyMatchContext` is built **per candidate** (~L178–200, `LoadForUserAsync`) and never sets `CompanyCultureScores`. No code path fills it (`git grep -n "CompanyCultureScores" -- '*.cs'`).
- The company data exists: `CompanyCultureProfiles` (`CompanyCultureService` in `CandidateCulturePersonalityService.cs` ~L201; `GetCompletedScoresAsync(companyId)` ~L289), filled by `/employer/culture` (`Pages/Employer/CultureScan.razor`, 12 items).
- `VacanciesController.GetCultureFit` (`{id:guid}/culture-fit`, ~L665) reads `match.CultureFit` from the same service.

## 01.2 Fix
- New `ICompanyCultureLookup` (Core interface, Infrastructure implementation):
  - `Task<IReadOnlyDictionary<Guid, CulturePersonalityScores>> GetForCompaniesAsync(IReadOnlyCollection<Guid> companyIds, CancellationToken)`.
  - It returns, per id, the **company's own** completed profile, else the **parent organisation's** (`Company.ParentCompanyId`), else nothing. One query for the ids plus one for the parents, no N+1.
  - `IMemoryCache` 10 minutes per company id. `CompanyCultureService.SaveAsync` evicts the saved company **and** its children (a small `CompanyCultureCacheKeys` helper).
- `ProfileVacancyMatchService`: before the per-record loop, collect the distinct `record.CompanyId`s of the batch, call the lookup once, and set `CompanyCultureScores` **per record** on the calculator input. For intermediary vacancies, use the end-client `CompanyId` (the employer whose culture the candidate joins), never `IntermediaryCompanyId`. If README Dependencies **G** is Present (the intermediair stack moved clients to `IntermediaryClient` links and `CompanyId` is the bureau vestiging), intermediary vacancies get **no** company culture.
- Remove `ProfileVacancyMatchContext.CompanyCultureScores` (candidate-level, always null) so nobody reuses it. Keep the calculator's input property.
- Check every other `ProfileVacancyMatchCalculator` caller (`git grep -n "new ProfileVacancyMatchInput\|ProfileVacancyMatchCalculator\." -- '*.cs'`: e.g. `CandidateMatchSnapshotService`, `RoleFitCheckService`, `CandidateInsightsService`) and fill it the same way where a vacancy is involved. List them in the PR.
- Match snapshots: bump the snapshot/fingerprint version the snapshot service uses (or its equivalent) so cached match scores recompute once. If there's no version, clear them through the existing invalidation path and say which one.

## Tests
- `ProfileVacancyMatchServiceCompanyCultureTests`: a candidate with a complete Cultuurscan; vacancy A's company has a complete employer scan that fits, vacancy B's company has none → A's culture component uses the 0.55/0.45 blend, B uses personality only. A vestiging without its own profile uses its organisation's profile. An incomplete profile is ignored. The intermediary vacancy uses the end client.
- Lookup: one DB round-trip per batch (count queries with the test interceptor or the existing pattern), cache hit on the second call, eviction after save (company + children).
- `CultureFitTests` and `VacancyCultureFitTranslationApiTests` stay green.
- The culture-fit endpoint returns a `CultureFit` for a vacancy whose company completed the scan (it returned null before for this reason).

## Success criteria
- `git grep -n "CompanyCultureScores" -- 'Jobsy.Infrastructure/**/*.cs'` shows a per-record assignment from the lookup.
- A completed employer Cultuurscan measurably changes the candidate's match score for that employer's vacancies (test), and changes nothing for other employers.

Done → next: `02-verificatiestatus-zichtbaarheid.md`.
