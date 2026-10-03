# DNA-paspoort v2 + paspoortpartners (staffing agencies & employers): Cursor run book

Cursor: **read this file completely**, then **execute the step files strictly in order**, one at a time. Each file is one PR.

> **Global rules (they apply to every file, repeat them in every PR body):**
> - **Never merge. Never deploy. Never use rule `123`** (`.cursor/rules/shortcut-123.mdc`).
> - **Never push to `main` or `acceptatie`. Never force-push.** Push only the current file's `cursor/paspoort-partners-*` branch.
> - **One stacked PR per step**, into `acceptatie`:
>   - 01 and 01b are **standalone hotfixes**, each on `origin/acceptatie`, independent of each other.
>   - 02 branches from the hotfix branch (or `origin/acceptatie` once PR 01 is merged).
>   - Every later step branches from the **previous** step's branch.
>   - PR body starts with "Stacked on #<prev PR>" (01/01b: "Standalone hotfix (not stacked)").
> - **Tests red, or a success criterion that can't be met:** push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Release build with 0 warnings.**
>   - `TreatWarningsAsErrors` is on (`Directory.Build.props`, .NET 10, `AnalysisLevel` 10.0-recommended), so any warning fails `dotnet build -c Release`.
>   - Warnings you hit are fixed **in the same PR**.
>   - Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.
> - Run the **full** test suite (`dotnet test -c Release`), not only the new tests.
> - **Add tests:** unit tests for every rule/service, bUnit for components, and **Playwright** wherever UI changes (390×844 and 1440×900).
>   - Playwright suites soft-skip without `JOBSY_E2E_BASE_URL`, which is fine.
>   - Unit and bUnit tests must always run.
> - **i18n:** every new UI string exists in **nl/en/pl/ro/ar** (the `UiStrings*.MergeAll` pattern, `LocalizationParityReportTests` stays green). Full Arabic **RTL layout of the PDF is later** (decided). Web pages must not break in `dir="rtl"`.
> - **AVG:** never put special-category or discriminating data on the passport, its live view or any partner surface:
>   - no date of birth/age, nationality, photo, BSN, health, religion or work permit
>   - no home address/postcode
>   - no private dislikes (`CandidatePrivatePreferences`)
>   - no test answers or raw scores
> - **AI Act:** Lobsy gives partners **no score, ranking or automatic selection** of candidates.
>   - No percentages, fit bands, "match" labels or bars on partner-facing surfaces.
>   - Partner lists sort only by date/status and filter only by status. The passport is conversation input; a human decides.
> - **No AI output on partner views** (decision 21; rationale `ai-act-beoordeling.md` §4.2 variant B and §4.4).
>   - "Partner views" means the PDF v2, the `/v` live view and the partner portal.
>   - Never shown: the AI "Wie ben ik" story or keywords, RoleFit bands ("Past goed"), matches, test outcomes (scores, labels, strengths, "hoe ik graag werk"), tips (AI-generated or test-derived), percentages.
>   - Only facts the candidate entered or confirmed, plus tests as done/not done.
>   - AI-assisted CV extraction is allowed only for fields the candidate confirmed. Machine translations are allowed only after the candidate approves them.
>   - Details: the no-AI rule in step 04.
> - **Feature flags:**
>   - Everything new is behind `PassportPartnersEnabled` and/or `PassportPdfV2Enabled` (added in 02, both default **false**), except the hotfixes 01 and 01b.
>   - After 01b, `EmployersEnabled` defaults to **false**. Nothing in 02–09 may depend on `EmployersEnabled` being ON. The partner portal works with Employers OFF.
>   - With both flags OFF, the product behaves exactly as today.
> - **Report per PR** with the template at the bottom.

**Source:**
- On 03-10-2026 Dennis approved all 19 proposals in `beslissingen-nl.md` exactly as proposed, plus decisions 20 (employer part OFF until phase 2), 21 (no AI output on the partner passport) and 22 (the former open points, see below). That file is in this folder, in Dutch, and is the decision record.
- The AI Act rationale is in `ai-act-beoordeling.md` (concept analysis of 03-10-2026, Dutch, to be reviewed by a lawyer), copied from the DPIA work.
- Code facts were checked on `origin/acceptatie` `85a43263`.
- Where the mockups differ from the spec, **the spec wins** (see "Mockup deviations" below).

## What this stack delivers
- **01: hotfix.** The current Lobsy-CV PDF prints the candidate's **date of birth and age** on every download, for the candidate and for employers. That is an AVG data-minimisation and age-discrimination risk. It can also attach the **AI "Wie ben ik" page**, which the hotfix removes too (decision 22). The step also fixes layout faults: banners repeat on every page, cards split across pages, and the file name breaks on names with punctuation.
- **01b: hotfix.** The employer part is OFF by default until phase 2 (`EmployersEnabled` = false in all defaults, plus a one-shot migration). Data is kept, employer routes show a friendly "binnenkort" page, and candidate flows keep working. Employer screens show "jeugdloon van toepassing" instead of an exact age. On acceptatie, employers currently see match % with breakdown, a talent pool with personality scores and the AI "Wie ben ik" story.
- **02:** shareable work preferences, work region, own car and contract preference (stored in `PreferencesJson`), plus email/phone verified flags and the two feature flags.
- **03:** the partner model: `PassportPartner` (type Uitzendbureau/Werkgever from `Company.Type`, logo, branches), partner codes in the school-code format, candidate links with consent, withdrawal and 6-month reconfirmation, a maximum of 3 partners, and an access log.
- **04:** the new **DNA-paspoort PDF v2**:
  - recruiter page p1 + p2 in Lobsy styling
  - bilingual, co-branding, QR code
  - **Lobsy-geverifieerd**
  - sector choices by the candidate
  - **no AI output**; CV-extracted fields only after confirmation
- **05:** the live verification page `/v/{id}`, share links (30 days), access requests and the candidate's **Delen & toegang** screen.
- **06:** start via partner code `/p/{code}` plus the explicit consent flow.
- **07:** the partner portal `/partnerportaal`: own candidates only, passport only, 2FA, and counts from 5 for candidates who have not consented.
- **08:** the public pricing page `/voor-partners` plus partner subscriptions. Amounts are indicative config, there are no tokens, and the Westland pilot offer is included.
- **09:** Playwright hardening across all flows.

**Explicitly out of scope for the whole stack:**
- **the phase-2 employer view.** It will be redesigned later: no percentages or personality scores, candidate-driven matching, and employers see only applicants in chronological order. 01b only switches the current employer part off; it does not redesign it.
- **Lobsy voor teams** (mockup i is "concept – later": do NOT build)
- **matching for Pro** (no ranking of candidates on partner vacancies)
- **talent-pool changes** (`TalentPool*`, `TalentContactRequest`; the partner portal gets no talent-pool access, and the pool's future is decided separately)
- the existing `AgencyAnnualSubscription` / `FlexCommercialSettings.AgencyAnnualPriceEuro` (vacancy carte blanche, untouched)
- the existing affiliate partner codes (`PartnerAffiliateProfile` IM-/BM-, `/partner`, `PartnerFlyerPdfService`)

### Naming (important)
"Partner" already means something else in this codebase:
- `PartnerAffiliateProfile`, `PartnerTrackingCode`, `PartnerReferralStatus`
- the `/partner` route (`PartnerSales.razor`)
- `PartnerFlyerPdfService`

All new types therefore use the prefix **`PassportPartner…`** (or `Passport…` for share and verification types). The portal lives at **`/partnerportaal`**, not `/partner`. Do not rename or reuse the affiliate types.

## Mockups (`docs/mockups/paspoort-partners/`, @2x, fictional data only)
| File | Shows | Used by |
|---|---|---|
| `huidig-lobsy-cv-voorbeeld.pdf`, `huidig-lobsy-cv-1..3.png` | Today's Lobsy-CV rendered with the real `LobsyCvPdfService`: DOB + age in hero and contact block; WhoAmI banner repeated on page 2; work-experience card split across pages | 01 |
| `a-recruiterpagina-p1.png` | Passport v2 page 1 (recruiter page), NL. Labels `NIEUW` (new field) / `AFGELEID` (derived) are annotations only | 02, 04 |
| `b-pagina2-sectoren-profiel.png` | Page 2: sectors (candidate's choice + "why"), DNA in 4 layers with dates, "Hoe ik graag werk", tips, own words (translated), what "Lobsy-geverifieerd" means. **DNA layers, "Hoe ik graag werk" and tips are dropped by decision 21** | 04 |
| `c-tweetalig-pl-nl.png` | Bilingual variant: candidate language (PL) primary, NL secondary | 04 |
| `f-cobranded-uitzendbureau.png` | Page 1 co-branded: "Lobsy × {partner}", "in samenwerking met …", consent ribbon | 04, 07 |
| `d-verificatiepagina-en-delen.png` | 1) `/v/…` with access (live); 2) `/v/…` without access (authenticity only + access request); 3) candidate "Delen & toegang" | 05 |
| `e-start-partnercode-toestemming.png` | 1) `/p/{code}` co-branded start in own language; 2) consent for an uitzendbureau; 3) same for a werkgever (HR) | 06 |
| `g-partneroverzicht.png` | Partner portal overview: 3 rule banner, KPIs, consented candidates table, partner code + QR + flyers, co-branding | 07 |
| `h-prijzen-partners.png` | Public pricing page: type switch, pilot banner, Gratis / Partner / Pro, 5 rules, FAQ | 08 |
| `i-teams-dashboard-concept-later.png` | **Out of scope**: Lobsy voor teams concept | n/a |
| `src/` | HTML/CSS + generator scripts (`pages.py`, `screens.py`, `teams.py`, `shot.py`) | n/a |

### Mockup deviations: the spec wins
- **Housing ("Huisvesting")** appears on mockups a/c/f, but is **out of v1** (decision 2). Do not build it.
- **Percentages** in mockup b's sector cards were already removed. Page 1 shows no numbers either. Nothing numeric about fit appears on any passport or partner surface (decision 4).
- **Partner codes:** the mockups show `KAS-FLX`. Real codes use the school-code alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (no I, L, O, 0, 1), so `L` is invalid. Codes are 6 characters displayed `XXX-XXX`.
- **Routes:** the pricing mockup shows `lobsy.nl/partners/prijzen`. The real route is **`/voor-partners`** (section `#prijzen`).
- **Pro's "later: your own vacancies"** in mockup h is listed as "binnenkort" text only. No matching gets built.
- **No AI output and no test outcomes** (decision 21). Leave out these mockup elements:
  - a/c/f: the `AFGELEID` tagline and "Sterke punten"
  - b: "DNA in 4 lagen", "Hoe ik graag werk" and "Zo haal je het beste uit …"
  - b: Holland/strength-based sector reasons
  - d/g: any test-outcome or AI text in the live view and portal

  Tests appear only as done/not done. Sector reasons are only facts the candidate confirmed (experience, certificates, preferences, own line).
- **Arabic:** the PDF falls back to the partner language (nl/en) when the candidate language is `ar` (RTL PDF later).

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-lobsy-cv-pdf.md`: **standalone hotfix**. Remove DOB/age and the AI "Wie ben ik" page from all Lobsy-CV PDFs, banners only on page 1, no split cards, safe file name | `cursor/paspoort-partners-hotfix` | `origin/acceptatie` | `acceptatie` |
| 01b | `01b-hotfix-werkgevers-standaard-uit.md`: **standalone hotfix**. `EmployersEnabled` default OFF (code + one-shot migration), real `IEmployersSwitch`, gate the remaining employer pages, "binnenkort" page, youth-wage indicator instead of age, candidate flows intact | `cursor/paspoort-partners-hotfix-werkgevers` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-datamodel-voorkeuren-verificatie.md`: shareable work prefs, work region, own car, contract pref (PreferencesJson), `EmailVerifiedAtUtc`/`PhoneVerifiedAtUtc`, SMS stub, flags `PassportPartnersEnabled`/`PassportPdfV2Enabled`/`PhoneVerificationEnabled` | `cursor/paspoort-partners-2` | `cursor/paspoort-partners-hotfix` (or `origin/acceptatie` once 01 is merged) | `acceptatie` |
| 03 | `03-partnermodel.md`: `PassportPartner`, `PassportPartnerCode` (shared short-code format + HMAC lookup), `PassportPartnerCandidateLink` (consent, revoke, 6-month reconfirm, max 3), `PassportAccessLog`, candidate/public APIs, minimal admin page, privacy hooks | `cursor/paspoort-partners-3` | `cursor/paspoort-partners-2` | `acceptatie` |
| 04 | `04-paspoort-pdf-v2.md`: sector choices + CV-field confirmation + translation approval, no-AI rule, `PassportDocument`, PDF v2 (p1/p2, bilingual, co-branding, QR, Lobsy-geverifieerd), download dialog | `cursor/paspoort-partners-4` | `cursor/paspoort-partners-3` | `acceptatie` |
| 05 | `05-verificatie-en-delen.md`: `/v/{id}` (authenticity / live), `PassportShareLink` (7/30/90, default 30), `PassportAccessRequest`, `/candidate/paspoort/delen` | `cursor/paspoort-partners-5` | `cursor/paspoort-partners-4` | `acceptatie` |
| 06 | `06-start-partnercode-toestemming.md`: `/p/{code}`, code through sign-up/login, consent screen, "Code toevoegen", reconfirm UI | `cursor/paspoort-partners-6` | `cursor/paspoort-partners-5` | `acceptatie` |
| 07 | `07-partnerportaal.md`: `/partnerportaal` (overview, candidates, passport view, code + flyers, logo/co-branding, branches), 2FA, min-5 counts, no-score guard | `cursor/paspoort-partners-7` | `cursor/paspoort-partners-6` | `acceptatie` |
| 08 | `08-prijzen-abonnementen.md`: `/voor-partners`, `PassportPartnerPlanSettings` (indicative amounts as config), `PassportPartnerSubscription` (admin-activated, no tokens, no Mollie), pilot slots, lead form | `cursor/paspoort-partners-8` | `cursor/paspoort-partners-7` | `acceptatie` |
| 09 | `09-playwright-hardening.md`: seeded partner fixtures, end-to-end flows, privacy/AI-Act guards, RTL/CSP/overflow smoke | `cursor/paspoort-partners-9` | `cursor/paspoort-partners-8` | `acceptatie` |

If a file grows past ~1,500 changed lines (not counting tests, resources and migrations' Designer files), split it into `a`/`b` at a natural seam. The next file then branches from the **last** sub-branch. Step 04 is the most likely to split: 04a covers data + model builder + candidate UI, 04b the renderer + download.

## Pointer prompt (runs 01 … 09)
```
Run the Paspoort-partners stack. First: git fetch origin && git show origin/docs/paspoort-partners:docs/paspoort-partners/00-README.md — read it completely, plus beslissingen-nl.md (decision record).
Then read and execute each file in docs/paspoort-partners/ on that branch strictly in the README's order (01 … 09), one file = one PR. Mockups are in docs/mockups/paspoort-partners/ on the same branch; where they differ, the spec wins.
Files 01 and 01b are standalone hotfixes: 01 on branch cursor/paspoort-partners-hotfix and 01b on cursor/paspoort-partners-hotfix-werkgevers, both from origin/acceptatie, PR body starts with "Standalone hotfix (not stacked)". If a hotfix PR already exists, reuse its branch.
File 02 branches from cursor/paspoort-partners-hotfix (or origin/acceptatie if PR 01 is merged); every later file branches from the previous file's branch. Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
After each file: dotnet build -c Release with 0 warnings (fix warnings in the same PR) and the full dotnet test -c Release; add the unit/bUnit/Playwright checks the file asks for; i18n nl/en/pl/ro/ar. If anything is red or a success criterion can't be met: push, open the PR as draft, stop and report.
AVG: no special-category data on the passport. AI Act: no scores, rankings or automatic selection for partners, and no AI output on partner views (only candidate-entered/confirmed facts). Out of scope: Lobsy voor teams, Pro matching, talent-pool changes.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, never force-push.
Report per PR (README template), and at the end: file → branch → PR number → status.
```

### Pointer prompt: hotfix 01 only
```
Run only file 01 of the Paspoort-partners stack. First: git fetch origin && git show origin/docs/paspoort-partners:docs/paspoort-partners/00-README.md and git show origin/docs/paspoort-partners:docs/paspoort-partners/01-hotfix-lobsy-cv-pdf.md — read both completely.
Branch cursor/paspoort-partners-hotfix from origin/acceptatie, ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)". Release build 0 warnings, full tests. Red → draft PR, stop, report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, never force-push. Don't start file 02.
```

### Pointer prompt: hotfix 01b only
```
Run only file 01b of the Paspoort-partners stack. First: git fetch origin && git show origin/docs/paspoort-partners:docs/paspoort-partners/00-README.md and git show origin/docs/paspoort-partners:docs/paspoort-partners/01b-hotfix-werkgevers-standaard-uit.md — read both completely (rationale: ai-act-beoordeling.md on the same branch).
Branch cursor/paspoort-partners-hotfix-werkgevers from origin/acceptatie, ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)". Keep all data. Release build 0 warnings, full tests. Red → draft PR, stop, report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, never force-push. Don't start other files.
```

## Decided on 03-10-2026 (decision 22 in `beslissingen-nl.md`; formerly open points)
1. **Phone verification** stays OFF (`PhoneVerificationEnabled = false`) until an SMS provider is chosen. **Lobsy-geverifieerd = 4/4 tests + verified e-mail** (02, 04).
2. **Minimum age for partner links: 18.** 16–17-year-olds come later, as a separate decision (03, 06).
3. **Employer applicant screens:** the exact age is replaced by a **"jeugdloon van toepassing"** indicator only. Stored data is kept (01b, section 4). This is part of the phase-2 direction.
4. **Partner 2FA:** the existing rule applies, so a Google/Microsoft login counts as 2FA (03, 07).
5. **Passport translations** use the existing OpenAI `ITranslationService`, and the candidate approves each one. Revisit when an EU provider is chosen (04).
6. **The existing €4,000 `AgencyAnnualSubscription`** stays separate and untouched (08).
7. **Invoicing** of partner subscriptions is manual for now (08).
8. **Legal check** before `PassportPartnersEnabled` goes live in production. Start from `ai-act-beoordeling.md`, a concept that a lawyer must review. It covers:
   - partner terms
   - consent texts
   - the pilot data-sharing agreement
   - the contractual clause: the partner does not score or rank candidates, does not feed passport data into its own AI/matching tools, and makes no white-label use (03)
   - also: document the art. 6(3) position for confirmed CV extraction, and check the AI Act before anything "Pro matching" or the phase-2 employer view is built
9. **The candidate's own PDF v2** has no AI output either, confirmed (04).
10. **Hotfix 01** also removes the AI "Wie ben ik" page from the current Lobsy-CV download (candidate + employer application CV). Data is kept.
11. **Production has no real users yet**, so there is no production urgency. 01b ships with the next regular release. (Production = `origin/main`, which has no `EmployersEnabled` flag, so the employer part is always on there; promoting acceptatie with 01b turns it OFF.)

## Still open (Cursor: use the stated default, list in the PR report)
1. **Production secret `PassportPartners:CodeHmacKey`** must be set before `PassportPartnersEnabled` goes ON (03).
2. **E-mail-verified backfill.** Step 02 checks whether every candidate sign-up path proves the e-mail. It backfills only if all do, and reports the result.
3. **Talent pool future:** decided separately. The partner portal never gets talent-pool access.
4. **Locking the employer toggle before phase 2.** Default: **not locked**. Switching ON needs a confirm dialog and is logged in `AdminAuditLog` (01b).

## Report template (per PR)
```
File: 0X-… · Branch: cursor/paspoort-partners-… · PR: #… (draft? why)
Stacked on: #… / Standalone hotfix
What changed (3–6 bullets) · Migrations (name, reversible yes/no)
Flags touched · i18n keys added (count, 5 languages ✓)
Build: Release 0 warnings ✓/✗ · Tests: total/passed/skipped (Playwright soft-skips listed)
Privacy/AI-Act checks: which guard tests cover this step
Deviations from spec (with reason) · Open questions for Dennis
```
