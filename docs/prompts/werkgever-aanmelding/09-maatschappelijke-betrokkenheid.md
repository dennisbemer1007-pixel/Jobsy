# 09. Over je bedrijf, part 3: maatschappelijke betrokkenheid (honest labels, moderation, small match bonus)

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-9` from `cursor/werkgever-aanmelding-8` (or `-8b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show a claim as checked unless an admin (or the automated SBB check) checked it. No scraping of any site. The bonus never lowers a score.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-9` |
| PR title | `feat(companies): social-engagement claims with honest labels, admin moderation, badges on company page and vacancies, max +5 match bonus` |
| PR body starts with | `Stacked on #<PR 08> (cursor/werkgever-aanmelding-8)` |
| Mockups | `wr-d7` (step 4 part 3), `wr-m5`; `wr-d12` (badges in the candidate preview) |
| Split seam | none expected (split at 09.4 "match bonus" if it grows) |

## Goal
Employers can say what they do for people and planet. Candidates see it with an honest label, and those who care about it (Waardentest) get a small nudge towards those employers, never a penalty for the others.

## 09.1 Today (verify first)
- Nothing like this exists. Find the badge/chip components the vacancy card and detail use today (`git grep -n "class=\"chip\|VacancyBadge\|vac-card__badges" -- Jobsy.Web`) and the public company page component (the one `PublicCompaniesController`/`VestigingLanding` feed; 02 made it verified-only).
- `ICompanyCultureLookup` (01/08) loads company match data per batch; `ProfileVacancyMatchCalculator` builds the explanation lines.
- Admin queue `/admin/werkgeververificatie` (06) has an empty tab slot **Betrokkenheid**.

## 09.2 Model and API
- `Jobsy.Core/Rules/EngagementCatalog.cs` (ids stable, texts `WaEngage.*` in 5 languages, emoji + text label):

  | Id | Label | Linked driver (D13) | Proof hint |
  |---|---|---|---|
  | `duurzaam` | Duurzaam en CO2-bewust | Impact | e.g. a CO2-prestatieladder or B Corp page |
  | `werk-voor-iedereen` | Werk voor iedereen | Impact | e.g. Participatiewet/banenafspraak, Social Enterprise NL |
  | `leerbedrijf` | Erkend leerbedrijf (SBB) | Connection | the SBB/stagemarkt page |
  | `lokaal` | Lokaal betrokken | Connection | a local sponsor or volunteer page |
  | `diversiteit` | Diversiteit en inclusie | Impact | e.g. Charter Diversiteit |
  | `eerlijk-loon` | Eerlijk loon en cao | Impact | the cao name |

- Entity `CompanyEngagementClaim` (migration `AddCompanyEngagementClaims`): company id (root org; vestigingen show the org's), item id, `ProofUrl?` (https only, ≤ 500, normalized), `ProofText?` (≤ 300, plain text), status `SelfDeclared` / `Checked` / `Removed`, `CheckedSource?` (`Admin` / `Sbb`), `CheckedByUserId?`, `CheckedAtUtc?`, `RemovedReason?`, timestamps. Unique (company, item).
- `GET/PUT api/companies/{id}/engagement` (company managers; allowed while unverified per 03): the PUT sets the list of claimed items with proof. Changing the proof of a `Checked` claim sets it back to `SelfDeclared`. A `Removed` claim can't be re-added by the employer for 30 days (the API says why).

## 09.3 Step 4 part 3 and badges (wr-d7, wr-m5, wr-d12)
- `/register/bedrijf?stap=betrokkenheid` replaces 08's placeholder: 6 toggle cards, each with an optional "Bewijs (link of korte toelichting)", **Overslaan** and **Opslaan en verder** (→ `/register/verifieren` or the dashboard). The same editor in the organisation profile (08.4 placement).
- Public display (only when the company is public, 02): the company page shows all non-removed claims with their label: **"Door werkgever opgegeven"** (`SelfDeclared`), **"Gecontroleerd door Lobsy"** (`Checked`/`Admin`) or **"Gecontroleerd bij SBB"** (`Checked`/`Sbb`), plus a small "Wat betekent dit?" explanation. The vacancy card shows max 2 badges (checked first) + "+n"; the vacancy detail shows all. No claims in JSON-LD/structured data. Proof links are shown on the company page with `rel="nofollow ugc noopener"`.
- The discovery record (02's `VacancyDiscoveryRecord`) gets `EngagementItems` (ids + checked flag) so cards don't query per row; refresh via `InvalidateCompanyAsync` on save/moderation.

## 09.4 Match bonus (D13)
- In `ProfileVacancyMatchCalculator`, after the total is computed: if the candidate has a completed Waardentest, for each claimed item whose linked driver scores **≥ 70** for the candidate: +1 (`SelfDeclared`) or +2 (`Checked`). Sum capped at **+5**; the total capped at 100. Never negative, never applied without a Waardentest.
- The explanation line: "Bonus +3: {bedrijf} zet zich in voor duurzaamheid en werk voor iedereen, en dat vind jij belangrijk." (`WaEngage.MatchBonus`). Store the bonus separately in the breakdown (`EngagementBonus`) so it can be shown and tested.
- Load claims through the batched lookup (01/08; add `Engagement` to its result). Bump the match snapshot version.

## 09.5 Moderation (admin can remove claims)
- `/admin/werkgeververificatie` tab **Betrokkenheid**: new or changed claims with proof (a queue; claims without proof are listed under a filter, not in the queue), and a search across all claims. Actions: **Controleren** (→ `Checked`/`Admin`), **Verwijderen** with a required reason (→ `Removed`; the employer gets an e-mail with the reason), **Terug naar opgegeven**. Audited (dependency E).
- Candidates can report a claim from the company page ("Klopt dit niet?" → a short form, rate-limited, lands in the same tab).

## 09.6 SBB check (only if allowed)
- Investigate whether SBB offers a documented public API or open dataset of erkende leerbedrijven with reuse permitted (check SBB's own open-data/API pages; note the URL and licence in the PR). **If yes:** an `ISbbRecognitionService` + a daily job that matches by KvK number and sets `leerbedrijf` claims to `Checked`/`Sbb` (and back to `SelfDeclared` when no longer listed), with a stub in tests. **If no:** don't build it. Admins check `leerbedrijf` by hand, and add a line to `docs/werkgever-aanmelding-followups.md`. Never scrape stagemarkt.nl or any other site.

## Tests
- Catalog: 6 items, stable ids, every text key in 5 languages.
- API: https-only proof, lengths, unique per item, a proof change resets `Checked`, a removed claim can't be re-added for 30 days, managers only.
- Display: labels per status; a hidden company shows nothing (02 rule); max 2 badges on a card; no claims in JSON-LD; `rel` attributes.
- Bonus: no Waardentest → 0; driver 69 → 0; 70 → counts; self-declared +1, checked +2; cap +5; total cap 100; never negative; the explanation line present; snapshot version bumped.
- Moderation: check/remove/reset audited; the removal mail has the reason; the candidate report is rate-limited and lands in the tab.
- SBB (only if built): matching by KvK, job idempotent, the stub used in tests.

## Success criteria
- A candidate with a high Impact driver sees "Door werkgever opgegeven: Duurzaam en CO2-bewust" and gets at most +5 for that employer. An admin can check or remove any claim, and the label changes everywhere within ≤ 60 s.

Done → next: `10-intermediair-waadi.md`.
