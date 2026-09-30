# Werkgever-aanmelding: bedrijf registreren, verifiëren en "over je bedrijf" (Cursor run book)

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

**What this stack builds:** a neat, lobster-guided employer sign-up that only makes a company visible once it has proven it is real.
- **Find your company** by KvK number **or by name** (KVK Zoeken API), see all its vestigingen, and choose **heel bedrijf** (all free vestigingen, with untick) or **alleen deze vestiging**.
- **Account** with Microsoft, Google or e-mail + password. The contact e-mail is always confirmed with a 6-digit code, and 2FA follows the existing rules.
- **Verification is chosen by the employer:** (1) a business e-mail whose domain matches the website registered at KVK (code by mail), or (2) a **letter with a code** to the KvK address (Pingen via PostNL). An admin manual check is the fallback.
- **Until verified the company is completely invisible publicly:** no company page, no vacancies on the map, search or Match, no sitemap entry, no structured data. This is **one server-side rule** with tests. Unverified employers can prepare drafts and "klaar" vacancies (these go live automatically on verification), fill in the profile and invite colleagues. Candidate data, token purchases, the welcome token and the free-publishing promo unlock only after verification.
- **Already registered?** A new registrant can't become a second owner. They send an **access request** to the bedrijfsmanager (admin after 5 working days). Ownership transfer requires a letter plus admin approval.
- **Over je bedrijf (optional, about 3 minutes):** branche (max 4 of the existing 9, prefilled from the SBI codes), **"Zo werken wij"** (6 sliders mapped 1:1 onto the candidate Cultuurscan, plus 3 kernwaarden on the Schwartz drivers) and **maatschappelijke betrokkenheid** (6 items, honest labels, max +5 match bonus). Vacancies inherit the culture profile, with a per-team override.
- **First** it fixes the bug where the employer Cultuurscan never reaches the match (01). It also fixes every bug from the phase-1 review of today's `/register` (§B).

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-cultuur-match-fix.md`: the employer culture profile reaches the match (`CompanyCultureScores` loaded, vestiging → org fallback), the vacancy culture-fit endpoint too, regression tests | `cursor/werkgever-aanmelding-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-verificatiestatus-zichtbaarheid.md`: `Company.VerificationStatus` + backfill migration (all existing = Verified), explicit status on every creation path, **one** public-visibility rule used by every public query (map, pins, cards, discover, detail, company page, vestiging page, sitemap, JSON-LD, Match, feeds), discovery index refresh | `cursor/werkgever-aanmelding-2` | `cursor/werkgever-aanmelding-1` | `acceptatie` |
| 03 | `03-beperkingen-niet-geverifieerd.md`: server gates for unverified companies (publish, tokens, candidate data), welcome token + free-publish promo moved to verification, "klaar" vacancies (`PublishOnVerification`) + auto-publish, `CompanyVerified` pipeline, reminders day 7/21, deletion at day 60 | `cursor/werkgever-aanmelding-3` | `cursor/werkgever-aanmelding-2` | `acceptatie` |
| 04 | `04-kvk-zoeken.md`: KVK name search (`v2/zoeken`), 3-char minimum, 24 h cache, profile only on select, websites + postadres parsing, SBI → branche map, stub data, rate limits; intermediair client lookup gets name search | `cursor/werkgever-aanmelding-4` | `cursor/werkgever-aanmelding-3` | `acceptatie` |
| 05 | `05-wizard-zoeken-vestiging-account.md`: the new `/register` wizard steps 1–3 (wr-d1…d4, m1, m2), pub theme + mascot, scope with untick, existing-owner detection, Microsoft/Google/e-mail, sales resolver, code step without redirects, all §B bugs | `cursor/werkgever-aanmelding-5` | `cursor/werkgever-aanmelding-4` | `acceptatie` |
| 06 | `06-verificatie-email-brief.md`: verification choice (wr-d8, m6), business e-mail domain match + free-mail blocklist, letter via `ILetterService` (Pingen + stub), letter code rules, admin review queue + manual approval, verification completes → 03 pipeline | `cursor/werkgever-aanmelding-6` | `cursor/werkgever-aanmelding-5` | `acceptatie` |
| 07 | `07-toegang-aanvragen-eigendom.md`: access request (wr-d10) to the bedrijfsmanager, reminder day 3, admin escalation after 5 working days; ownership transfer = letter + admin; claiming companies without users; intermediair keeps its client link | `cursor/werkgever-aanmelding-7` | `cursor/werkgever-aanmelding-6` | `acceptatie` |
| 08 | `08-branche-cultuur-waarden.md`: step 4 part 1–2 (wr-d5, d6, m3, m4): company branches (max 4, SBI prefill), "Zo werken wij" sliders → Cultuurscan answers, 3 kernwaarden → `CompanyValuesProfile` used by the values match; vacancy inherits + per-team override (wr-d12) | `cursor/werkgever-aanmelding-8` | `cursor/werkgever-aanmelding-7` | `acceptatie` |
| 09 | `09-maatschappelijke-betrokkenheid.md`: step 4 part 3 (wr-d7, m5): 6 engagement items with optional proof, honest labels, admin moderation, badges on company page + vacancy, match bonus max +5 | `cursor/werkgever-aanmelding-9` | `cursor/werkgever-aanmelding-8` | `acceptatie` |
| 10 | `10-intermediair-waadi.md`: the intermediair registers through the same wizard and verification, plus the Waadi check through the shared service (Dependencies D) | `cursor/werkgever-aanmelding-10` | `cursor/werkgever-aanmelding-9` | `acceptatie` |
| 11 | `11-dashboard-banner-afronding.md`: "Nog niet zichtbaar voor kandidaten" banner, checklist and visibility panel in the werkgever dashboard (wr-d11), letter-code entry from the dashboard, docs, privacy text, full Playwright E2E | `cursor/werkgever-aanmelding-11` | `cursor/werkgever-aanmelding-10` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/werkgever-aanmelding-5b`).

## Pointer prompt (the only prompt needed; it runs 01 … 11)
```
Run the Werkgever-aanmelding stack. First: git fetch origin && git show origin/docs/werkgever-aanmelding:docs/prompts/werkgever-aanmelding/00-README.md — read it completely.
Then read and execute each file in docs/prompts/werkgever-aanmelding/ on that branch strictly in the order the README's table lists (01 … 11; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 01 which case applied. Re-run check A at the start of 05, check B at 05, check C at 11, check D at 10 and check G at 05, 07 and 10.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/i18n/README.md`, `docs/release-flow.md`, `docs/adr/0005*` (2FA) and, if present on `origin/acceptatie`, `docs/feature-flags.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01).
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. File 01: `git checkout -b cursor/werkgever-aanmelding-1 origin/acceptatie`. Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, plus the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. The body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, go on to the next file.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, and don't continue.
5. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
6. **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you (or a dependency check flips at a re-check point), `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** The body starts with `Stacked on #<prev PR> (<prev branch>)`; file 01 says `Stacked on: none (first in the stack)`. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** Run `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, open a draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing.
- **Mockups:** branch `docs/werkgever-aanmelding`, folder `docs/mockups/werkgever-registratie/`. Read with `git fetch origin docs/werkgever-aanmelding && git show origin/docs/werkgever-aanmelding:docs/mockups/werkgever-registratie/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop 1440 (full page, dpr 1): `wr-d1-start-zoeken.png`, `wr-d2-zoekresultaten.png`, `wr-d3-bedrijf-vestigingen.png`, `wr-d4-gegevens-account.png`, `wr-d5-branche.png`, `wr-d6-kernwaarden.png`, `wr-d7-betrokkenheid.png`, `wr-d8-verificatie-keuze.png`, `wr-d9-brief-onderweg.png`, `wr-d10-al-geregistreerd.png`, `wr-d11-welkom-dashboard.png`, `wr-d12-vacature-cultuur.png`.
  - Mobile 390 (full page, dpr 2): `wr-m1-zoeken.png`, `wr-m2-vestiging-keuze.png`, `wr-m3-branche.png`, `wr-m4-kernwaarden.png`, `wr-m5-betrokkenheid.png`, `wr-m6-verificatie-keuze.png`, `wr-m7-brief-code.png`.
  - The HTML/CSS sources are in the same folder (`build.py`, `wr_ui.py`, `s_*.py`, `css_wr.py`, `data.py`, `base/` = copy of the landing mockup base). Use them for exact copy, spacing and colour mixes, **not** as code to paste: the implementation uses BEM classes, tokens and components.
  - The mockups are a **layout and copy reference**. "Groen & Zorg Thuiszorg B.V.", KvK 90123456, the vestigingen, names, e-mail addresses, codes, dates, percentages and badges are **Voorbeelddata**. The "Voorbeelddata" pill is mockup-only.
  - **Where the mockup and this spec differ, this spec wins.** Known differences:
    - **Microsoft/Google selected in wr-d4:** choosing the method doesn't skip the 6-digit contact e-mail code. The code is always sent (05.6); the external login is linked right after it.
    - **"Past bij groenenzorg.nl":** shown only when the KvK profile has a website whose registrable domain matches (06.2). No website at KVK → no pill, and the e-mail method card in wr-d8 is disabled with "Bij KVK staat geen website. Kies de brief."
    - **The 8-character code** in wr-d9/m7 has a dash for readability only. Input accepts it with or without the dash and in lower case (06.4).
    - **Dates** in wr-d9/d11 ("wo 30 sep", "verwacht do 1 of vr 2 okt", "t/m 29 okt", "vanaf ma 5 okt") are computed (06.4). The delivery estimate is "1–3 werkdagen" from the send date.
    - **"Heeft al een beheerder" / "Al op Lobsy"** in wr-d2/d3 are the boolean `IsInUse` only. No owner name, e-mail or count is ever shown publicly (07.2).
    - **wr-d11/d12 sidebar** is the illustrative werkgever shell. Use `WerkgeverLayout` when present, else today's employer layout (Dependencies C). Only the banner, checklist and visibility panel are this stack's scope.
    - **wr-d12 tabs "1 Basis · 2 Uren en loon · 3 Cultuur en waarden · 4 Vragen"** are illustrative. Add the culture section to today's vacancy editor (`CreateVacancy.razor`, or its werkgever-redesign successor) where culture pillars live now.
    - **Mascot poses:** the mockups draw today's mascot with CSS transforms. Use `LobsyMascot` (`Waving` on steps 1–3, `Default` on 4–5, `Celebrating` on the welcome) or the fallback from Dependencies A.
    - **Emoji** are system-font text (`aria-hidden`, next to a text label), as in the landing stack.
- **Design system.** The public wizard pages (`/register…`) use the **public theme** from `docs/landing` 01 (`.pub-theme`, `pub-` BEM, its approved deviations) or the fallback in Dependencies A. The dashboard banner, vacancy editor section and admin pages use the normal design system (`.cursor/rules/design-system.mdc`). Always: tokens only, no inline `style=""` in `.razor` (a computed `--pct` custom property via `style` is fine, as existing components do), breakpoints 640/900/1024, logical properties, tap targets ≥ 44 px, visible focus rings, AA contrast, `prefers-reduced-motion`, no `!important`.
- **CSS:** new `wwwroot/css/features/werkgever-aanmelding.css` (BEM block `wa-…`, every selector scoped under `.pub-theme` or the fallback root class), linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-wa`, added to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Never append to `app.css`. The banner/editor/admin parts go into the existing feature CSS of those pages.
- **Strings:** all new UI text via `@Culture["…"]` in a new `Localization/UiStringsWerkgeverAanmelding.cs` (prefixes `Wa.` wizard, `WaVerify.`, `WaAccess.`, `WaProfile.`, `WaEngage.`, `WaBanner.`, `AdminWa.`), registered in `UiStrings.cs` like `UiStringsMatch.MergeAll`. Every key exists in **nl, en, pl, ro, ar** from the file that adds it (`LocalizationParityReportTests`). nl and en are final. pl/ro/ar are B1 drafts listed in `docs/i18n/werkgever-aanmelding-review.md` (11). **`docs/i18n/untranslated-baseline.txt` may not grow.** E-mails and the letter go through `TransactionalEmails` / the letter template (06) in the same 5 languages; the letter itself is **nl** (the KvK address is Dutch), with a short en line.
- **Terminology (nl final):**

  | Use | Instead of |
  |---|---|
  | Bedrijf registreren | Registreren, aanmelden (the CTA) |
  | Heel bedrijf / Alleen deze vestiging | Organisatie / BranchOnly (UI) |
  | Bedrijfsmanager | EnterpriseManager, eigenaar (UI) |
  | Verifiëren, Geverifieerd, Niet geverifieerd | Valideren, goedgekeurd |
  | Brief met code | Postverificatie, aangetekende brief |
  | Toegang aanvragen | Overname (for joining as a colleague) |
  | Eigendom overnemen | Takeover (UI) |
  | Door werkgever opgegeven / Gecontroleerd | Claim, geverifieerd (for engagement badges) |
  | Werkwaarden (Waardentest), Cultuurscan | Schwartz, Big Five (UI) |

- **Security and privacy (every file):** rate limits on every anonymous endpoint (§A). Codes are stored hashed (`VerificationCodes.Hash`, constant-time compare) and never logged. No owner PII in any public response. Every admin decision writes an audit row (`IAdminAuditLog` when admin redesign 07 landed, else a structured `PlatformLog` row). All server gates are enforced in the API, never only in the UI.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (`PageSeoTests`), `Seo/SeoEndpoints.cs`, `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `CHANGELOG.md`.
- **Migrations:** `dotnet ef migrations add …` in `Jobsy.Infrastructure`, snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests` and `EfMigrationDiscoveryTests` stay green. Migrations are added in 02, 03, 04 (`KvkUsageDaily`), 05 (registration fields: selected vestigingen, representation consent, resolved salesmanager id, and `Company.LocationSource` if Dependencies G is absent), 06, 07, 08, 09, 10 (only when it builds `LenderRegistration`, Dependencies D) and 11 (dismissed vestiging suggestions). One migration per (sub-)PR at most.
- **Must NOT touch:**
  - `KvkVerificationStatus` semantics (the KvK-existence check stays as it is; the new status is separate, D2)
  - `LocalAuthCredential` password rules, lockout, the MFA pages and `MfaEnforcementMiddleware` (only reuse them)
  - Mollie/checkout internals, token pricing and `FlexCommercialSettings` semantics (03 only moves **when** the welcome token and free publishing apply)
  - the candidate Cultuurscan / Waardentest question sets, scoring and storage (08 only reuses them)
  - `VacancyDiscovery` UI, `jobMap*.js`, banenkaart CSS (02 only filters what goes into the index)
  - the other stacks' in-progress branches (`cursor/landing-*`, `cursor/salesmanager-*`, `cursor/werkgever-redesign-*`, `cursor/intermediair-*`, `cursor/werkgevers-actief`): never branch from or merge them
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 and mobile 390 of each new or changed screen
  - the new/changed URL + endpoint list
  - test list
  - "Out of scope / deferred"
- **Playwright in CI:** every new Playwright test class goes into **both** filter lists in `.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step), like the existing classes.

---

## §IA. Routes and endpoints (the contract for all files)

| Route / endpoint | What | Who | Render | SEO | Built in |
|---|---|---|---|---|---|
| `/register` | Wizard steps 1–3: zoeken → resultaten → bedrijf + vestigingen → gegevens + account → code | anonymous (signed-in employer → 302 to their home) | InteractiveServer **prerender: true**, public theme | index (canonical `/register`) | 05 |
| `/register?ref=…`, `lobsy_sales_ref` cookie | sales/partner code prefill | anonymous | — | — | 05 |
| `/register/koppelen` | Callback after the Microsoft/Google challenge: links the external login to the just-activated user (IdP e-mail must equal the confirmed contact e-mail) | the just-activated user | static SSR | noindex | 05 |
| `/register/bedrijf?stap=branche\|cultuur\|betrokkenheid` | Step 4 "Over je bedrijf" (optional) | the new bedrijfsmanager (signed in) | InteractiveServer prerender, public theme | noindex | 08, 09 |
| `/register/verifieren` | Step 5: choose e-mail or letter (+ manual check) | bedrijfsmanager of an unverified company | InteractiveServer prerender, public theme | noindex | 06 |
| `/register/verifieren/brief` | Letter on its way + code entry | same | same | noindex | 06 |
| `/register/toegang` | "Al geregistreerd → vraag toegang aan" | anonymous (step-3 details) | same | noindex | 07 |
| `/admin/werkgeververificatie` | Admin review queue (manual checks, flagged registrations, Waadi, escalated access requests, engagement proof) | Admin | InteractiveServer | private | 06 (+07, 09, 10 add tabs) |
| `GET api/kvk/search?q=&plaats=` | name or number search | anonymous, rate-limited | — | — | 04 |
| `GET api/registration/kvk/{kvk}/profile` | profile + vestigingen (+ `IsInUse`) on select | anonymous, rate-limited | — | — | 04 |
| `POST api/registration` / `{id}/confirm` | existing, extended | anonymous | — | — | 05 |
| `POST api/company-verification/email/start` / `…/email/confirm` | business e-mail verification | bedrijfsmanager | — | — | 06 |
| `POST api/company-verification/letter` / `…/letter/confirm` | letter request / code | bedrijfsmanager | — | — | 06 |
| `POST api/company-verification/manual` | request a manual check | bedrijfsmanager | — | — | 06 |
| `POST api/vacancies/{id}/ready` / `DELETE …/ready` | mark a vacancy "klaar" (goes live on verification) | employer mutate roles of an unverified company | — | — | 03 |
| `GET/PUT api/companies/{id}/profile-extras` | branches, culture sliders, kernwaarden | company managers | — | — | 08 |
| `GET/PUT api/companies/{id}/engagement` | engagement claims | company managers (admin moderates) | — | — | 09 |
| `POST api/access-requests` + inbox endpoints | access request | anonymous submit (rate-limited, e-mail confirmed); BM decide | — | — | 07 |
| `/werkgever` (or today's `/home` for employers) | banner + checklist + visibility panel | employer roles | existing | private | 11 |

## §A. Abuse controls (all enforced server-side)
- Rate limits (new named policies in `Jobsy.Api/Program.cs`, same partitioning as `public-write`): `kvk-search` 30/min per IP; `registration-submit` 5/hour per IP; `verify-start` 5/hour per user; `access-request` 5/day per IP. `public-write` stays for everything else.
- Max 3 registrations per e-mail domain per day (freemail domains count per address). Max 1 pending registration per KvK vestiging (today's rule, kept).
- Free-mail blocklist `Jobsy.Core/Rules/FreeMailDomains.cs` (≈ 60 common consumer domains incl. gmail.com, googlemail.com, outlook.com, hotmail.*, live.*, msn.com, icloud.com, me.com, yahoo.*, ziggo.nl, kpnmail.nl, kpnplanet.nl, planet.nl, home.nl, hetnet.nl, casema.nl, chello.nl, xs4all.nl, telfort.nl, upcmail.nl, tele2.nl, online.nl, proton.me, protonmail.com, gmx.*, web.de, mail.com, aol.com, yandex.*, plus disposable domains in a separate list). One `IsFreeMail(domain)` used by 06 **and** by the salesmanager 03 self-referral guard (if that code exists, switch it to this list; else note it in the follow-up doc).
- Letters: max 1 per KvK number per 30 days, max 2 resends, and a monthly letter cap setting (`CompanyVerificationSettings.MonthlyLetterCap`, default 500) that alerts admin at 80 % and blocks new letters at 100 % (manual check offered instead).
- Admin review queue (06) receives: manual-check requests, registrations flagged by heuristics (≥ 3 registrations from one IP in 24 h, KvK "uitgeschreven" or no active vestiging, freemail contact + letter requested for a KvK that already has an owner), Waadi failures (10), escalated access requests (07) and engagement proof to check (09).

## §B. Review bugs → where fixed (each gets a regression test)

| Bug in today's `/register` (`Pages/Register.razor`, `CompanyRegistrationService`) | Fixed in |
|---|---|
| L72 renders the literal `?? "KVK-dienst…"` text, and the error is duplicated (L51) | 05 (new markup; bUnit asserts no `??` in output and one error region) |
| Manual pending-KvK path pins every company at the NL centre (L638–639: 52.1326, 5.2913) | 05 (geocode the entered address with the existing geocoding service; if it fails, no pin until KvK retry fills it; never the centre) |
| `InteractiveServerRenderMode(prerender:false)` on a public SEO page: blank first paint | 05 (prerender true + a server-rendered step 1) |
| Code expiry and step-5 "Back" do `Task.Delay(1800)` + `NavigateTo(Banenkaart)` (L565–566, L840–841) and lose the registration | 05 (stay on the page, "Vraag een nieuwe code aan", Back = previous step with data kept) |
| No Microsoft/Google sign-up for employers | 05 |
| Free-text sales code, filled only from `?ref=`; the referral cookie is ignored | 05 (resolver, Dependencies B) |
| Hard-coded Dutch strings (manual form, "Kies eerst een vestiging.", "Al een account?", SBI hint, lookup errors) and inline `style=` (L360, L405) | 05 (all via `Wa.*` keys; guard test: no inline `style=` and no Dutch literals in the new components) |
| Candidate GratisDna quiet link + `GratisDnaRegisterBox` on the company page (L25–35) | 05 (removed; the landing 03 redirect handles `?van=ontdek`, or 05 adds it if landing 03 is absent) |
| "Stub-KVK's" dev note visible (L65) | 05 (shown only when `IHostEnvironment.IsDevelopment()`) |
| No sign-in after activation | 05 (signed in right after the code) |
| Welcome token + `FreePublishUntil` apply without any verification | 03 |
| Employer Cultuurscan never reaches the match (`ProfileVacancyMatchContext.CompanyCultureScores` is never set) | 01 |

## Decisions (Dennis "Akkoord" 30-09 on all ten; extra defaults marked *extra*)
- **D1. Verification methods (employer chooses):** (1) business e-mail with domain match, code by mail; (2) letter with a code to the KvK address. Admin manual check is the fallback. iDEAL account-holder matching and eHerkenning are **not** built (possible later). *(Dennis, 30-09)*
- **D2. Separate status:** `Company.VerificationStatus` (`Unverified`, `Pending`, `Verified`, `Rejected`) + `VerificationMethod` (`None`, `BusinessEmail`, `Letter`, `Manual`, `Backfill`, `AdminCreated`, `InheritedFromOrganization`, `IntermediaryClient`) + `VerifiedAtUtc`. `KvkVerificationStatus` keeps meaning "does the KvK record exist". *extra*
- **D3. Letters:** Pingen API (delivery by PostNL), paid by Lobsy. Code: 8 characters, valid 30 days, resend after 7 days, max 2 resends, 5 wrong attempts blocks the code (→ admin queue), max 1 letter per KvK per 30 days. *(Dennis, 30-09)* The code alphabet is Crockford base32 without 0/O/1/I/L, shown as `XXXX-XXXX`. The letter goes to the KvK **postadres** if present, else the **bezoekadres**: heel bedrijf → hoofdvestiging, alleen deze vestiging → that vestiging. *extra*
- **D4. Unverified may:** create drafts and mark vacancies "klaar" (they auto-publish on verification), fill in the profile (step 4), invite colleagues. **May not:** see candidate data (applicants, talentpool, kandidaatinzichten, contact requests), buy tokens, use the welcome token or the free-publishing promo. Reminders on day 7 and 21; deletion on day 60. *(Dennis, 30-09)*
- **D5. Invisible publicly until verified**, through one server-side rule used by every public query, with tests (02). *(Dennis, 30-09)*
- **D6. Backfill:** every company that exists when the 02 migration runs is `Verified` / `Backfill`. *(Dennis, 30-09)*
- **D7. Heel bedrijf** includes all free vestigingen, with untick. Vestigingen owned by someone else are excluded. Vestigingen that appear at KvK later are **suggested** (dashboard Te doen), not added. *(Dennis, 30-09)*
- **D8. Existing owner:** access request to the bedrijfsmanager, reminder on day 3, escalation to admin after 5 working days (Mon–Fri, no holiday calendar *extra*). Ownership transfer only with a letter + admin approval. Companies with no users can be claimed after verification, and the intermediair keeps its client link. *(Dennis, 30-09)*
- **D9. Intermediair:** same wizard and verification, plus the Waadi check (10). *(Dennis, 30-09)*
- **D10. KvK:** debounce 300 ms, 3-character minimum for a name, 24 h cache, the basisprofiel/vestigingen fetched only on select. Pricing (checked 29-09): Zoeken free, basisprofiel/vestigingsprofiel €0,02 per query, €6,40 per month per key, max 300.000 queries per month and 100 per second. *(Dennis, 30-09)*
- **D11. Culture:** 6 sliders mapped 1:1 onto the Cultuurscan culture dimensions + 3 kernwaarden cards on the 5 Schwartz drivers, about 1 minute, skippable. Vacancies inherit it; "Dit team werkt anders" = the existing 3–5 `CulturePillars` as a per-team override. The match bug is fixed first (01). *(Dennis, 30-09)* Slider → answers: slider value v (1–5) writes Cultuurscan item 2k−1 = v and item 2k (reversed) = 6 − v for dimension k, status `Completed`, `Source = Quick`; the full 12-item scan overwrites. Values: a driver with 2 chosen cards = 90, 1 card = 75, 0 cards = 40. A vestiging without its own profile uses the organisation's. *extra*
- **D12. Branche:** max 4 of the existing 9 `WorkTypeLabels` at company level, prefilled via a new SBI → branche map. A vacancy keeps max 2 and defaults to the company's first branche. *(Dennis, 30-09)*
- **D13. Maatschappelijke betrokkenheid:** 6 items (Duurzaam en CO2-bewust, Werk voor iedereen, Erkend leerbedrijf (SBB), Lokaal betrokken, Diversiteit en inclusie, Eerlijk loon en cao) with optional proof. The label is "Door werkgever opgegeven" unless checked. Bonus max +5, never a penalty; admin can remove claims. *(Dennis, 30-09)* Bonus rule: only for candidates whose Waardentest driver is ≥ 70: +1 per matching self-declared item, +2 per checked item, capped at +5 and at a total of 100. Impact ↔ Duurzaam, Werk voor iedereen, Diversiteit, Eerlijk loon; Verbinding & zorg ↔ Lokaal betrokken, Erkend leerbedrijf. "Gecontroleerd" = an admin checked the proof, or the automated SBB check (09) if SBB offers a usable public API/open data. No scraping. *extra*
- **D14. Business e-mail match:** the e-mail's registrable domain (public-suffix aware) equals the registrable domain of a website at KVK, or is a subdomain of it. No website at KVK → the e-mail method is unavailable. A Microsoft/Google login never verifies the company by itself. *extra*
- **D15. Contact e-mail = verification e-mail:** if the contact e-mail confirmed with the step-3 code already matches (D14), the company is verified at that moment (method `BusinessEmail`) without a second code. The wizard then skips to the welcome. *extra*
- **D16. Admin fallback:** a manual check is answered within 2 working days (shown to the user). Admin approves or rejects with a reason (audit). After 3 rejections for one KvK, new registrations for it go straight to the queue. *extra*
- **D17. Deletion at day 60:** an unverified registration's companies (only those it created, with no other members), their drafts, the pending codes/letters and the user (if the user has no other role or membership) are deleted. A `PlatformLog` AVG row is written without PII, and the KvK vestigingen become free again. *extra*
- **D18. Wizard routes:** keep `/register` (`PublicRoutes.CompanyRegister`). The signed-in continuation is `/register/bedrijf` and `/register/verifieren`, in the public theme. Password users pass the existing 2FA setup (`MfaEnforcementMiddleware`) before step 4. *extra*

## Dependencies (check before 01; say in PR 01 which case applied)
- **A. Public theme + mascot (`docs/landing` 01 + 02).** Check: `git grep -n "class LobsyMascot\|LobsyMascot.razor" origin/acceptatie -- Jobsy.Web` and `git ls-tree -r --name-only origin/acceptatie -- Jobsy.Web/wwwroot/css/features/public-theme.css Jobsy.Web/Components/Layout/PublicLayout.razor`.
  - **Present:** the wizard pages use `@layout PublicLayout`, `.pub-theme`, `pub-` primitives (`pub-card`, `pub-btn`, `pub-chip`, `pub-emoji`/`PubEmoji`), `LobsyMascot` (`Pose`, `Size`) and `PublicRoutes`. `werkgever-aanmelding.css` only adds `wa-` blocks.
  - **Absent:** 05 adds `Components/Layout/WaPublicLayout.razor` (header with logo, "Bedrijf registreren", language, "Inloggen"; the existing cookie banner once) with root class `wa-theme`, and `Components/Registration/WaMascot.razor` (today's `BrandImages.MascotWebp*` + transform classes `wa-mascot--waving|default|celebrating`, as the mockup does). `werkgever-aanmelding.css` defines the few warm tints it needs via `color-mix()` on existing tokens (no new hex), scoped `.wa-theme`. Add **`docs/werkgever-aanmelding-landing-followup.md`**: when landing 01/02 land, (1) `WaPublicLayout` → `PublicLayout`, (2) `WaMascot` → `LobsyMascot`, (3) `.wa-theme` → `.pub-theme` and drop the duplicated tints, (4) `PublicRoutes.CompanyRegister`. Never copy landing files or names (no `PublicLayout`/`LobsyMascot` of your own).
  - Re-check at the start of 05. If present by then, use the present path.
- **B. Salesmanager referral resolver (`docs/salesmanager` 03).** Check: `git grep -n "class SalesAttributionResolver\|SalesReferralCookie" origin/acceptatie -- Jobsy.Infrastructure Jobsy.Web`.
  - **Present:** 05 passes the typed code + `SalesReferralCookie.TryRead(HttpContext)` into registration, and `CompanyRegistrationService` calls `SalesAttributionResolver` (typed code wins, then cookie). The cookie is deleted after a successful registration; external-login completion goes through the resolver too.
  - **Absent:** 05 adds `Jobsy.Core/Interfaces/IRegistrationReferralResolver.cs` with `ResolveAsync(string? typedCode, string? linkCode, CancellationToken)` → `(string? Code, ReferralSource Source)` and a `DefaultRegistrationReferralResolver` with today's behaviour (typed code or `?ref=`, validated by the existing SM-/AM-/BM-/IM- rules; no cookie). Registration only talks to this interface. Add a line to **`docs/werkgever-aanmelding-followups.md`**: "salesmanager 03: implement `IRegistrationReferralResolver` with `SalesAttributionResolver` + `lobsy_sales_ref`; delete the default."
  - Re-check at the start of 05.
- **C. Werkgever shell (`docs/werkgever-redesign` 01/02).** Check: `git grep -n "WerkgeverLayout\|class WerkgeverNav\|WgPageShell" origin/acceptatie -- Jobsy.Web`.
  - **Present:** 11 renders the banner through `WgPageShell` (a `Banner` slot, added if missing) on every `/werkgever` page and adds checklist items to the dashboard Te doen. Links go to `/werkgever/...` URLs (culture → `/werkgever/organisatie/profiel?tab=cultuur`).
  - **Absent:** 11 adds `Components/Employer/CompanyVerificationBanner.razor`, rendered by `MainLayout` for employer roles whose company is unverified, plus the checklist and visibility panel in `EmployerHomePanel`. Links use today's URLs through one `EmployerLinks` helper, so the redesign's 301s keep them working. Add a line to `docs/werkgever-aanmelding-followups.md`.
  - Re-check at the start of 11.
- **D. Intermediair uitleenregistratie (Waadi) check (`docs/intermediair` 04.5–04.7).** State when this spec was written (30-09 07:00 CEST): `origin/docs/intermediair` @ `a8b0246c` specifies **`ILenderRegistrationCheck`** (`Jobsy.Core/Interfaces/ILenderRegistrationCheck.cs`: `GetStateAsync`, `StartForNewBureauAsync`, `RecordDecisionAsync`, `CanPublish`), providers behind `ILenderRegistrationProvider` in `Jobsy.Infrastructure/Services/LenderRegistration/` (`WaadiKvkProvider` → `Unknown` + a deep link to the KvK Waadi check, because KvK has no public Waadi API; `WttaNauProvider` off; `AdminManual`), entity `LenderRegistration`, the publish gate 409 `lender_registration_pending` and the admin tab "Uitleenregistratie" on `/admin/intermediairs`. Registration is **not** blocked by it; only publishing is. Check: `git grep -n "interface ILenderRegistrationCheck" origin/acceptatie -- Jobsy.Core` and `git fetch origin docs/intermediair && git show origin/docs/intermediair:docs/prompts/intermediair/04-kvk-verversing-uitleenregistratie.md`.
  - **In code on acceptatie:** reuse it as is. 10 only moves/keeps its single registration call site (`StartForNewBureauAsync` after an SBI-78 activation) in the new wizard flow and links the admin tab from `/admin/werkgeververificatie`.
  - **Only in the intermediair spec (expected):** 10 implements **exactly** intermediair 04.5 (interface, records, providers, entity, one call site) and the 04.6 publish gate, with the names, paths and error codes written there, and records admin decisions from a "Waadi" tab in `/admin/werkgeververificatie` via `RecordDecisionAsync`. Say in PR 10 that intermediair 04b is then partly done, and add it to `docs/werkgever-aanmelding-followups.md` so the intermediair stack skips what exists and moves the tab to `/admin/intermediairs`.
  - **Neither (the intermediair spec no longer mentions it):** define the same shape under the same name `ILenderRegistrationCheck` (the four members above, `LenderRegistrationState` with `Status {NotChecked, Pending, Verified, Rejected}`, `Source`, `Reference?`, `CheckedAtUtc?`, `ValidUntil?`) with only the `WaadiKvkProvider` + `AdminManual` behaviour, and log it in the follow-up doc.
  - Re-check at the start of 10.
- **E. Admin redesign (`docs/admin-redesign`).** Check: `git grep -n "class AdminNavCatalog\|AdminSidebar" origin/acceptatie -- Jobsy.Web`. Present → `/admin/werkgeververificatie` goes into its "Werkgevers" (or closest) group, and decisions use `IAdminAuditLog` if present. Absent → one item in today's admin nav next to the companies pages, with `PlatformLog` audit rows.
- **F. 2FA enrolment (`MfaEnforcementMiddleware`, `MfaPolicy`).** Present at `a611db40`. Reuse it; if it moved, keep the semantics.
- **G. Intermediair data model (`docs/intermediair` 01/02).** That stack replaces client **companies + memberships** with a separate `IntermediaryClient` link (no `Company`, no `UserCompany`; D8/D18 there), makes `Vacancy.CompanyId` the owning **bureau vestiging** (+ `Vacancy.IntermediaryClientId`), and adds `Company.LocationSource` (`Unknown`, `Kvk`, `Pdok`) with PDOK geocoding instead of the NL centroid (D24 there). Check: `git grep -n "class IntermediaryClient\b\|LocationSource" origin/acceptatie -- Jobsy.Core/Entities`.
  - **Present:** 01 gives intermediary vacancies **no** company culture (the link has no culture profile; never use the bureau's). 02 skips the "intermediary client from KvK" creation row (links aren't companies) and `IntermediaryClient` verification applies only to legacy shell companies. 03.5 flips nothing for links. 05 uses the same PDOK resolver + `LocationSource = Unknown` for a failed manual address. 07.6/07.7 need no membership exclusion (links are never touched; add a guard test that registration, claims and takeovers never read or write `IntermediaryClient` rows).
  - **Absent (state at `a611db40`):** follow the text of each file as written (client companies with an intermediary `UserCompany` membership).
  - Re-check at the start of 05, 07 and 10. If it flips mid-stack, follow the Present case from that file on and say so in the PR.
- **Recommended landing order:** landing 01/02 → salesmanager 03 → this stack → werkgever-redesign 11 re-check. Not a hard requirement: every case above has a fallback. Never branch from an unmerged branch of another stack.
