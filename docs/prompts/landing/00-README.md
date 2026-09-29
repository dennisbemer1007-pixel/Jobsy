# Lobsy landing page, gratis test en kandidaat-funnel (met en zonder werkgevers): Cursor run book

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

**What this stack builds:** a new public front door for Lobsy, in a warm public-pages theme, that works both with and without employers.
- **"/" becomes the landing page** (static SSR, no MapLibre): hero "Soms moet je uit je schild groeien.", what Lobsy is, the kreeft-visie, what you get after sign-up, for whom, privacy, FAQ and a closing CTA. The only live number on the page is one real, cached count.
- **The banenkaart moves to `/banenkaart`** (public, indexed). `/banen` redirects there; signed-in candidates land there instead of "/".
- **Candidate sign-up is fixed.** A new `/account-maken` page offers Google, Microsoft and **e-mail** (a 6-digit code, no password). Every CTA of the mini-test points there instead of the company registration `/register`. The 20 `/ontdek` answers stay in the browser for 7 days and merge at sign-up, as today.
- **The mini-test `/ontdek` gets the warm style**: start, question and result screens, a mobile sign-up sheet, and a sticky CTA that never collides with the cookie banner.
- **"Werkgevers actief" OFF** (switch from `docs/mijn-paspoort` file 01) renders the **-zw variant on the same "/"** on the server. It has no jobs, map, employers or Match. SEO, sitemap, JSON-LD and cache follow the switch. The passport tab reads "Past dit beroep?".
- **Short audience cards** link to the new public pages `/werkgevers` and `/scholen`, and to the existing `/partner`.
- **Languages:** NL and EN in full; PL, RO and AR for the landing page, the test and sign-up; AR in RTL.
- **Measurement:** a cookieless KPI funnel (daily aggregate counters) with a small admin view, performance budgets, and Lighthouse CI in the PR workflow.
- **Mascot:** one `LobsyMascot` component. It uses today's mascot as the fallback, and a written asset contract means new art is a file swap. No art is generated in this stack.

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-publiek-thema-shell.md`: `PublicLayout` (one cookie banner, RTL), the public theme `features/public-theme.css` (`pub-`), `UiStringsLanding.cs` (5 languages), `PublicNavCatalog` (ON/OFF aware), static-SSR language switch (`?lang=` + hreflang), `IEmployersSwitch` seam, the static cookie-banner mode, TeaserLayout funnel fixes | `cursor/landing-1` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-mascotte-assets.md`: `LobsyMascot` component + `MascotAssets` manifest with fallback to today's mascot, the asset contract `docs/brand/mascot-assets.md` (poses, sizes, formats, file names, placement), guard tests. No art. | `cursor/landing-2` | `cursor/landing-1` | `acceptatie` |
| 03 | `03-kandidaat-account.md`: `/account-maken` (Google, Microsoft, e-mail code), passwordless e-mail code sign-up/sign-in, GratisDna CTAs → `/account-maken?van=ontdek`, `/register?van=ontdek` redirect, Login "Nieuw? Maak gratis account", merge verified for all three methods | `cursor/landing-3` | `cursor/landing-2` | `acceptatie` |
| 04 | `04-banenkaart-verhuizen.md`: the map moves to `/banenkaart` (public, indexed), `/banen` → 301, `AuthRedirects.BanenkaartPath`, `RoleNavCatalog`, every `href="/"` that means "the map", perf guards retargeted, SEO/sitemap | `cursor/landing-4` | `cursor/landing-3` | `acceptatie` |
| 05 | `05-landing-met-werkgevers.md`: "/" = landing ON (lp-d1 / lp-m1), static SSR without the Blazor runtime, one real number, inline SVG illustration, no MapLibre (guard), FAQ + JSON-LD, signed-in users redirected server-side | `cursor/landing-5` | `cursor/landing-4` | `acceptatie` |
| 06 | `06-gratis-test-warm.md`: `/ontdek` start/question/result restyle (lp-d2…d4, lp-m2/m3), sign-up sheet, single cookie banner + sticky CTA rules, all existing GratisDna behaviour kept | `cursor/landing-6` | `cursor/landing-5` | `acceptatie` |
| 07 | `07-landing-zonder-werkgevers.md`: -zw variant on "/" server-side, `FeatureRoutes` amendment (anonymous OFF → "/"), `/banenkaart` gated OFF, SEO/sitemap/JSON-LD/cache per flag, OFF copy in the test flow, "Past dit beroep?", clipped -zw mobile chips fixed | `cursor/landing-7` | `cursor/landing-6` | `acceptatie` |
| 08 | `08-werkgevers-scholen-paginas.md`: `/werkgevers` (ON only) and `/scholen` (always), audience cards wired, partner card → `/partner` | `cursor/landing-8` | `cursor/landing-7` | `acceptatie` |
| 09 | `09-talen-rtl.md`: PL/RO/AR for landing + test + sign-up (incl. the GratisDna strings that fall back to EN today), RTL audit, hreflang, native-review list | `cursor/landing-9` | `cursor/landing-8` | `acceptatie` |
| 10 | `10-meten-kpi-lighthouse.md`: cookieless KPI funnel counters + admin view `/admin/funnel`, performance budgets (LCP/TTI), Lighthouse CI job, Playwright LCP test | `cursor/landing-10` | `cursor/landing-9` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/landing-3b`).

## Pointer prompt (the only prompt needed; it runs 01 … 10)
```
Run the Landing stack. First: git fetch origin && git show origin/docs/landing:docs/prompts/landing/00-README.md — read it completely.
Then read and execute each file in docs/prompts/landing/ on that branch strictly in the order the README's table lists (01 … 10; a/b splits where a file allows it), one file = one PR.
File 01 branches from origin/acceptatie; every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 01, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 01 which case applied. Re-run check A at the start of 07.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes.
At the end report: file → branch → PR number → status, plus anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/performance.md`, `docs/i18n/README.md`, `docs/release-flow.md` and, if it exists on `origin/acceptatie`, `docs/feature-flags.md`.
2. Run the **Dependencies** checks below and note the outcome (it goes into PR 01).
3. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column. File 01: `git checkout -b cursor/landing-1 origin/acceptatie`. Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, and the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. Body starts with `Stacked on #<prev PR> (<prev branch>)`, then the PR body items from §0.
   6. Note the PR number, go on to the next file.
4. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, don't continue.
5. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
6. **Never** merge, deploy, or use rule `123` (`.cursor/rules/shortcut-123.mdc`). **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: each file adds its own migration on top of the previous one. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you (or check A flips at 07, see Dependencies), `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches stack** (see above). **ONE PR per file, always into `acceptatie`.** Body starts with `Stacked on #<prev PR> (<prev branch>)`; file 01 says `Stacked on: none (first in the stack)`. The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing.
- **Mockups:** branch `docs/landing`, folder `docs/mockups/landing/`. Read with `git fetch origin docs/landing && git show origin/docs/landing:docs/mockups/landing/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Werkgevers actief **ON**, desktop 1440: `lp-d1-landing.png` (full page, 5844 high), `lp-d2-test-start.png`, `lp-d3-test-vraag.png`, `lp-d4-test-resultaat.png`.
  - **ON**, mobile 390 (full page, dpr 2): `lp-m1-hero.png`, `lp-m2-test-vraag.png`, `lp-m3-resultaat.png`.
  - Werkgevers actief **OFF** (-zw): `lp-d1-landing-zw.png` (desktop, 5707 high), `lp-d4-test-resultaat-zw.png` (desktop), `lp-m1-hero-zw.png` and `lp-m3-resultaat-zw.png` (mobile).
  - The HTML/CSS sources are in the same folder (`build.py`, `w_*.py`, `css_*.py`, `zw_scene.py`). Use them for exact copy, spacing and colour mixes, **not** as code to paste: implementation uses BEM `pub-` classes, tokens and components.
  - The mockups are a **layout and copy reference**. Vacancies, companies, travel times, percentages, match scores and the passport are **Voorbeelddata**. The "Voorbeelddata" pill is mockup-only, **except** on the landing illustrations of product features (the map card, passport, Match and report tiles in "Wat je krijgt"), where it stays as the small label "Voorbeeld" so nobody reads it as their data.
  - **Where the mockup and this spec differ, this spec wins.** Known differences:
    - **Live map:** the landing page never shows a live map, tiles or pins. The hero map disc and the "De banenkaart" tile are static inline SVG. "Binnen 30 minuten van huis: 38 banen" and the three job rows are illustration copy. The **one real number** is D7 (05.4).
    - **"Past deze baan?"** in the -zw passport tabs (lp-d1-landing-zw, "Wat je krijgt") must read **"Past dit beroep?"** when OFF (07.6).
    - **Clipped chips:** the mobile -zw hero passport card (lp-m1-hero-zw) was fixed in the final mockup (card 230 px, chips never wider than the card). The implementation must keep every hero element inside the viewport with ≥ 12 px margin at 360 and 390 px (07.7 test).
    - **Cookie banner:** not drawn in the mockups. It is the existing banner, once per page, restyled for the public theme (01.5, 06.5).
    - **"Doorgaan met e-mail"** on the sign-up sheet (lp-d4/lp-m3) is the passwordless e-mail code flow (03), not a password form.
    - **"€ 2,99"** on the report tile comes from `FlexCommercialSettings.DeepAnalysisPriceEuro` (admin setting), formatted per culture. It's never hard-coded.
    - **"Binnenkort"** pills on the Mijn Paspoort and Ontdekkingsreis tiles are driven by `LandingFeatureAvailability` (05.5). They disappear once those pages are live.
    - **FAQ help card:** "…of stel je vraag via de chat" is dropped (there's no public chat). The copy is "Lees hoe Lobsy werkt." → `/hoe-werkt-lobsy`.
    - **Footer "Cookies"** links to `/privacy#cookies` (add the anchor id if it's missing). There's no separate cookies page.
    - **-zw nav items "Mijn Paspoort" / "Ontdekkingsreis"** are in-page anchors (`#wat-je-krijgt`, `#ontdekkingsreis`), because those are logged-in features.
    - **"Kies je taal"** (-zw card "Nieuw in Nederland") opens the language menu (01.4); it doesn't navigate.
    - **Emoji:** rendered as text in the platform emoji font (`PubEmoji`, aria-hidden), no emoji web font (D9). They look slightly different per OS, which is accepted.
    - **"Nog 5 vragen voor een scherper beeld"** (lp-d4) has no extra question set behind it. It becomes "Scherper beeld? In je account staan de volgende tests klaar." → `/account-maken?van=ontdek` (06.4).
    - **Share row:** lp-d4 shows only WhatsApp. The existing three share buttons (WhatsApp, Instagram, generic) stay, restyled as compact pills.
    - **Mascot poses** (waving, diving, sitting, celebrating) are drawn in the mockups with the single current mascot plus CSS transforms. The implementation does the same through `LobsyMascot` until art exists (02).
- **Design system.** Follow `.cursor/rules/design-system.mdc` for everything **except** the public theme (D8). File 01 adds a section **"Public theme (landing, gratis test, public info pages)"** to `design-system.mdc` listing exactly these approved deviations, valid **only inside `.pub-theme`**:
  1. Warm tints mixed from existing tokens with `color-mix()` (`--gold`, `--coral`, `--success`, `--brand` with `--surface`); coral may appear several times per screen; navy only for the primary CTA and text. No new hex values.
  2. Radii 24/36 px for cards and scenes, pill-shaped buttons.
  3. Public type sizes: hero h1 `3.25rem` desktop / `2.125rem` mobile, h2 `2.25rem`, result h1 `3rem`, and 700 weight on h2s. Defined once as `--pub-text-*` custom properties.
  4. Emoji as icons (next to some headings, in chips and buttons), always `aria-hidden` with a text label next to them.
  5. Organic blobs, waves and illustration scenes (inline SVG or CSS), plus one extra soft shadow `--pub-shadow-soft` (coral-tinted), public theme only.
  6. Likert buttons with emoji faces; blur on the locked passport teaser.
  - Still binding inside `.pub-theme`: tokens only, no inline `style=""` in `.razor` (use CSS custom properties set via classes, or `style` only for a computed `--pct` value as existing components do), breakpoints 640/900/1024 only, logical properties, tap targets ≥ 44 px, focus rings visible, contrast AA (the -zw and ON palettes both pass; a test checks the main text/background pairs), `prefers-reduced-motion` fallbacks, and **no `!important`** (the mockup CSS uses a few; the implementation must not).
  - **The logged-in app is untouched**: no `pub-` class and no `public-theme.css` rule may affect a page under `MainLayout` (guard test in 01).
- **Strings:** all new UI text via `@Culture["…"]` in new modules registered in `UiStrings.cs`:
  - `Localization/UiStringsLanding.cs` (prefixes `Landing.`, `Public.`, `PublicNav.`, `PublicFooter.`; 01, 05, 07)
  - `Localization/UiStringsCandidateSignup.cs` (prefix `Signup.`; 03)
  - `Localization/UiStringsPublicPages.cs` (prefixes `Werkgevers.`, `Scholen.`; 08)
  - `Localization/UiStringsFunnel.cs` (prefix `AdminFunnel.`; 10)
  - Every key exists in **nl, en, pl, ro, ar** from the file that adds it (so `LocalizationParityReportTests` stays green). nl and en are final copy; pl/ro/ar are written in B1 language and listed for native review in `docs/i18n/landing-review.md` (09). **`docs/i18n/untranslated-baseline.txt` may not grow.**
  - Variant copy: a key that differs when OFF gets a sibling with suffix `.Zw` (e.g. `Landing.Hero.Sub` / `Landing.Hero.Sub.Zw`). Components pick it via one helper `LandingText.For(key, variant)`; never `if` on strings in markup.
  - Tone: B1, "je", short sentences, as in the mockups. Terminology below.
- **Terminology (nl final):**

  | Use | Instead of |
  |---|---|
  | Gratis test | DNA-scan, quiz, assessment (on public pages) |
  | Account maken | Registreren (for candidates) |
  | Bedrijf registreren | Registreren (for employers, KvK) |
  | Banenkaart | Kaart, vacaturekaart, map |
  | Paspoort / Mijn Paspoort | Profiel, cv |
  | Werkgever | Bedrijf, klant (on public pages) |
  | Lobsy (the mascot's name, the brand) | Jobsy |

- **Performance (every file touching "/" or `/ontdek`):** "/" loads no MapLibre, `jobMap*.js`, map tiles, pin APIs, `blazor.web.js` or SignalR (D13). Guard tests in 05 and budgets in 10. `/ontdek` keeps its current weight or less.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (`PageSeoTests`), `Seo/SeoEndpoints.cs` (sitemap/robots), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `docs/performance.md` (perf-relevant changes), `CHANGELOG.md`.
- **CSS:** new `wwwroot/css/features/public-theme.css` (BEM block prefix `pub-`, root class `.pub-theme` on `PublicLayout`; 01) and, where a file says so, `features/landing.css` (`pub-landing__…`, 05). Each is linked in `Components/App.razor` (normal list **and** `<noscript>`) with `?v=YYYYMMDD-x`, and added to `Jobsy.Tests/asset-versions.json` (`AssetVersionGuardTests`). Never append to `app.css`. `features/gratis-dna.css` gets the test-flow restyle (06), scoped under `.pub-theme`.
- **Migrations:** `dotnet ef migrations add …` in `Jobsy.Infrastructure`, snapshot updated. `EfModelSnapshotTests`, `PendingModelChangesTests` and `EfMigrationDiscoveryTests` stay green. Only 03 (e-mail code challenge) and 10 (funnel counters) add migrations.
- **Must NOT touch:**
  - the design and behaviour of logged-in pages (only their links to "/" or `/register` change, per 03/04)
  - `VacancyDiscovery` internals, `jobMap*.js` and banenkaart CSS (04 only moves the page)
  - the GratisDna question set, scoring and storage format (`jobsy.gratisDna.v1`, 7 days), and `GratisDnaMerge*` logic (03 only adds the new sign-up entry points; 06 only restyles)
  - the company registration flow `/register` (KvK) apart from the `van=ontdek` redirect (03)
  - `LocalAuthCredential` password logins, lockout rules, MFA pages and `MfaEnforcementMiddleware`
  - Mollie/checkout, token pricing, `FlexCommercialSettings` semantics
  - the other stacks' in-progress branches (`cursor/werkgevers-actief`, `cursor/mijn-paspoort-*`, `cursor/scholen-*`): never branch from or merge them
- **PR description:**
  - what changed and why
  - screenshots desktop 1440 and mobile 390 of each new or changed screen, in **both variants** (ON and OFF) wherever the file touches a variant-aware page, plus an AR (RTL) mobile shot from 05 on
  - the new/changed URL list
  - the Lighthouse numbers for "/" (from 05 on; the CI job from 10)
  - test list
  - "Out of scope / deferred"
- **Playwright in CI:** every new Playwright test class goes into **both** filter lists in `.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step), like the existing classes. Both variants are tested on one stack with the Development-only `?_variant=on|zw` override (01.8).
- **Run** `dotnet build` and `dotnet test` (unit + bUnit). Run the Playwright suites you touched if you can; say so if you couldn't.

---

## §IA. Routes and render modes (the contract for all files)

| Route | What | ON | OFF | Render | SEO | Built in |
|---|---|---|---|---|---|---|
| `/` | Landing page | lp-d1 / lp-m1 | lp-d1-zw / lp-m1-zw | static SSR (`[ExcludeFromInteractiveRouting]`), `PublicLayout`, no Blazor runtime | index, canonical `/`, hreflang | 05 (ON), 07 (OFF) |
| `/` signed in | — | 302 → `FeatureRoutes.HomeFor(user)` (candidate → `/banenkaart`) | 302 → `HomeFor(user)` | server redirect | — | 05 |
| `/banenkaart` | Banenkaart (today's `Home.razor` content) | public | 302 → `/` | InteractiveServer prerender (unchanged) | index (ON only) | 04, gate 07 |
| `/banen` | legacy | 301 → `/banenkaart` | 302 → `/` | endpoint | — | 04 |
| `/ontdek`, `/dna` | Gratis test (20 questions) | lp-d2…d4, lp-m2/m3 | lp-d4-zw, lp-m3-zw | InteractiveServer prerender (unchanged), `PublicLayout` | index (`/ontdek` canonical) | 06, 07 |
| `/account-maken` | Candidate sign-up / sign-in (Google, Microsoft, e-mail code) | ● | ● | static SSR forms | noindex | 03 |
| `/account-maken/code` | Enter the 6-digit code | ● | ● | static SSR form | noindex | 03 |
| `/login` | unchanged + "Nieuw bij Lobsy? Maak gratis account" | ● | ● | unchanged | noindex | 03 |
| `/register` | Company KvK registration (unchanged) | ● | gated by werkgevers-actief 01 | unchanged | unchanged | — |
| `/register?van=ontdek` | legacy test CTA | 302 → `/account-maken?van=ontdek` | same | endpoint | — | 03 |
| `/werkgevers` | Public page for employers | ● | 302 → `/` | static SSR, `PublicLayout` | index (ON) | 08 |
| `/scholen` | Public page for schools | ● | ● | static SSR, `PublicLayout` | index | 08 |
| `/partner` | existing | unchanged | gated by werkgevers-actief 01 | unchanged | unchanged | — |
| `/taal/{lang}` | Language switch for static pages (sets `Jobsy.Culture`, 302 back to a local `returnUrl`) | ● | ● | endpoint | noindex, `rel=nofollow` links | 01 |
| `/admin/funnel` | KPI funnel view | Admin | Admin | InteractiveServer | private | 10 |

`/scholen` (public, marketing) and the scholen stack's portals (`/school*`, `/leraar*`, `/leerling*`) don't overlap. `/scholen` never links into a portal while `SchoolsEnabled` is false or absent (08.3).

## §V. Variant matrix for "/" (05 builds ON; 07 adds OFF; one component tree with `Variant` parameters)

| Section (id) | ON (`lp-d1`) | OFF (`lp-d1-zw`) |
|---|---|---|
| Header nav | Hoe het werkt · Banenkaart · Werkgevers · Scholen · Partners · taal · Inloggen · **Doe de gratis test** | Hoe het werkt · Mijn Paspoort (anchor) · Ontdekkingsreis (anchor) · Scholen · taal · Inloggen · **Doe de gratis test** |
| Hero `#top` | h1 "Soms moet je uit je schild groeien.", sub "…Eerst jij, dan de baan…", meta "20 vragen · 3 minuutjes" · "Werkgevers zien je antwoorden niet" · "Of kijk eerst op de banenkaart" (link), audience chips, **map-disc illustration** + mascot waving + D7 number | sub "…Eerst jij, dan je richting…", meta "Alleen jij ziet je antwoorden" · "Gratis paspoort", **passport illustration** + mascot + "Past dit beroep bij mij?" + "Ontdekkingsreis" floating cards; no number |
| Wat is Lobsy `#wat-is-lobsy` | 3 steps: Ontdek jezelf · Zie wat bij je past (banenkaart, reistijd) · Laat zien wat je kunt | step 2 = Functiefit · Droombaan-check |
| Kreeft-visie `#kreeft` | 6 tiles; "De juiste rots → In Lobsy: Banenkaart" | "De juiste rots → In Lobsy: Past dit beroep?" |
| Wat je krijgt `#wat-je-krijgt` | h2 "…“hier wil ik werken”."; De banenkaart (static), Mijn Paspoort, Ontdekkingsreis, Match, Jouw rapport | h2 "…“dit past bij mij”."; Mijn Paspoort (big, tabs with **"Past dit beroep?"**), Past dit beroep bij mij?, Droombaan-check, Ontdekkingsreis, Jouw rapport; **no** map, Match, vacancies |
| Voor wie `#voor-wie` | Kandidaat · Werkgever (→ `/werkgevers`) · School (→ `/scholen`) · Partner (→ `/partner`) | Jij, op zoek naar je richting · Nieuw in Nederland (→ language menu) · Even vastgelopen (→ `/hoe-werkt-lobsy`) · School (→ `/scholen`) |
| Privacy `#privacy` | 4 cards; "Werkgevers zien niets zonder jou" | "Alleen jij kijkt mee" |
| FAQ `#faq` | 7 questions incl. "Ik ben werkgever of school. Hoe begin ik?" | same set, "gratis" answer without "banenkaart", "Ik werk op een school. Hoe begin ik?" |
| Closing CTA | "Klaar om uit je schild te groeien?" | same |
| Footer | Voor: Kandidaten · Werkgevers · Scholen · Partners; Account: Inloggen · Account maken · Werkgever registreren | Voor: Jou · Nieuw in Nederland · Scholen; Account: Inloggen · Account maken |

**Rule:** OFF output contains **no** link to `/banenkaart`, `/banen`, `/vacancies/*`, `/werkgevers`, `/register`, `/partner`, `/westland` or `/candidate/match`, and no text key from the employer-only set (test in 07).

## §S. SEO, sitemap and cache rules (05 for ON, 07 for OFF)
- "/" canonical is always `/` (both variants). Title/description from `PageSeoCatalog`: ON = `Landing.Seo.Title` / `Landing.Seo.Description`, OFF = `Landing.Seo.Title.Zw` / `Landing.Seo.Description.Zw`.
- `/banenkaart` takes over today's "/" SEO entry (`Page.JobMapTitle`, `Seo.HomeDescription`). OFF: not in the sitemap, and it answers 302 → `/`.
- hreflang on "/", `/ontdek`, `/werkgevers`, `/scholen`: `?lang=nl|en|pl|ro|ar` alternates plus `x-default` = no parameter (01.4). The `lang` query sets the culture for that response only, and the canonical stays without `?lang`.
- Sitemap ON: `/`, `/banenkaart`, `/ontdek`, `/werkgevers`, `/scholen`, `/hoe-werkt-lobsy`, `/wie-zijn-wij`, the legal pages, vacancies/companies as today. OFF: drop `/banenkaart`, `/werkgevers`, `/partner`, `/westland`, `/vacancies/*` and company URLs.
- JSON-LD on "/": `WebSite` + `Organization` (existing `StructuredData.WebsiteAndOrganization`) + `FAQPage` built from the same FAQ keys that are rendered (both variants). No `JobPosting` on "/" ever; OFF: no `JobPosting` anywhere (werkgevers-actief rule).
- Cache: "/" HTML responds `Cache-Control: no-cache, private` and `Vary: Cookie`, plus the diagnostic header `X-Lobsy-Variant: on|zw`. `/sitemap.xml` and `robots.txt` respond `Cache-Control: public, max-age=300` with an `ETag` that includes the flag value, so a toggle is visible within 5 minutes. The app has no output cache today (`AddOutputCache` is absent); if one is added later, its policy key must include variant + culture. A test guards the headers.

## §K. KPI funnel (10 builds it; earlier files only add the `data-kpi` hooks)
- Steps (enum `FunnelStep`): `LandingView`, `LandingCtaTest`, `LandingCtaLogin`, `LandingCtaMap` (ON), `LandingCtaAudience` (werkgever/school/partner), `TestStart`, `TestComplete`, `ResultCtaSignup`, `SignupStart` (per method: google/microsoft/email), `SignupComplete` (per method), `FirstValue` (ON: first `/banenkaart` or `/candidate/match` view after sign-up; OFF: first passport/profile view).
- Dimensions: date (Europe/Amsterdam), variant (`on|zw`), culture (`nl|en|pl|ro|ar`), device class (`mobile|desktop`, from the viewport hint sent by the client, never the UA string stored), optional `method`/`target`.
- Storage: **daily aggregate counters only** (`FunnelDailyCounter`), no identifiers, no IP, no user id, nothing on the device → no consent needed (D12). Rates are computed from counts (e.g. TestComplete / TestStart); they're not per-person funnels.
- LCP: the landing's small inline script reports `LCP` in 250 ms buckets as a counter dimension (`LcpBucket`), same cookieless endpoint.
- Hooks: every CTA in 05–08 carries `data-kpi="<step>"` (+ `data-kpi-target`) from the start, so 10 only wires the sender.

## Decisions (defaults applied; Dennis can override any of them)
- **D1. "/" is the landing page for anonymous visitors, in both variants.** Signed-in users get a **server** 302 to `FeatureRoutes.HomeFor(user)`. ON-candidate goes to `/banenkaart`, OFF-candidate to their home per werkgevers-actief, admin/staff to their home as today. *(Dennis, 29-09: "no live map on the landing")*
- **D2. The map moves to `/banenkaart`**, public and indexed. `/banen` answers **301** → `/banenkaart` (server-side, replacing today's client `NavigateTo`). `AuthRedirects.BanenkaartPath` becomes `/banenkaart`. *(Dennis, 29-09)*
- **D3. The -zw variant renders server-side on the same "/"** from the switch value at request time. There's no client flash, no separate URL, and SEO/sitemap/cache follow §S. This **amends `docs/mijn-paspoort` 01**: anonymous "/" no longer redirects to `/ontdek` when OFF, and the home canonical stays "/" (Dependencies A). *(Dennis, 29-09)*
- **D4. Candidate sign-up = Google, Microsoft and e-mail** on one page `/account-maken`. E-mail is **passwordless**: a 6-digit code, 10 minutes valid, 5 attempts, via `VerificationCodes`. The same page signs in an existing candidate. Password accounts (company/invite/demo) keep their password login on `/login`, and an e-mail code never signs in a non-candidate account (03.4). *(Dennis, 29-09: email self-sign-up; passwordless is the extra default)*
- **D5. Route name `/account-maken`** (Dutch, like `/ontdek`, `/banenkaart`). Labels are localized, and there's no second route per language. `?van=ontdek` shows the "Je testresultaat gaat mee" box (reuse `GratisDnaRegisterBox`).
- **D6. The 20 `/ontdek` questions are reused unchanged**: stored locally for 7 days (`jobsy.gratisDna.v1`) and merged at sign-up by the existing `GratisDnaMerge` in `MainLayout`, for all three sign-up methods. *(Dennis, 29-09)*
- **D7. One real number on the landing (ON only):** the count of active public vacancies. It's cached 10 minutes, shown rounded down to tens as "Nu ruim {n} vacatures op de banenkaart", and hidden when below **25**. OFF shows no number. *(Dennis, 29-09: "static illustration plus one real number"; threshold and wording are extra defaults)*
- **D8. The warm style is a separate public theme** (`.pub-theme`, `features/public-theme.css`) for "/", `/ontdek`, `/account-maken`, `/werkgevers` and `/scholen`. The logged-in app keeps the design system. The deviations listed in §0 are approved **for that theme only**. *(Dennis, 29-09)*
- **D9. Emoji** are system-font text in a `PubEmoji` component (`aria-hidden="true"`, always next to a text label). No emoji web font.
- **D10. Languages:** NL + EN in full (all public pages). PL, RO, AR for landing + test + sign-up; `/werkgevers` and `/scholen` also get all five (parity) but only NL/EN are reviewed before launch. AR renders RTL (`dir="rtl"` from `CultureState.IsRightToLeft`). *(Dennis, 29-09)*
- **D11. Language switch on static pages** through `/taal/{lang}?returnUrl=…` (sets the `Jobsy.Culture` cookie, local `returnUrl` only), and `?lang=` for crawlers and hreflang (response-only culture, no cookie). The interactive `LanguageSelector` stays for the app.
- **D12. KPI tracking is cookieless aggregate counting** (§K). No consent is needed, and nothing is sent to third parties. The existing consent-gated `site-visits` stays as it is. *(Dennis, 29-09: KPI tracking; the cookieless design is the extra default)*
- **D13. No Blazor runtime on "/"**, `/werkgevers` and `/scholen`: these pages are marked `[ExcludeFromInteractiveRouting]` plus a new `[NoBlazorRuntime]` attribute, and `App.razor` skips `blazor.web.js` for them. `app-core.js` (cookies, culture) stays deferred, plus one small `landing.js` (≤ 4 KB gz: menu, sheet, KPI beacon). Budgets: LCP < 2.5 s p75 mobile, TTI < 3.5 s mid-range 4G (Lighthouse mobile preset: LCP ≤ 2.5 s, TTI ≤ 3.5 s, TBT ≤ 200 ms, CLS ≤ 0.1). *(Dennis, 29-09)*
- **D14. Audience pages:** `/werkgevers` (ON only; CTA "Bedrijf registreren" → `/register`, second "Inloggen") and `/scholen` (always; CTA "Mail ons" → `mailto:` the new placeholder `PlatformLegalIdentity.SchoolsEmail`, plus "Lobsy voor scholen start binnenkort" while `SchoolsEnabled` is false or absent). The partner card → existing `/partner`. *(Dennis, 29-09: short cards + new pages; the CTAs are extra defaults)*
- **D15. One cookie banner per page.** `PublicLayout` renders it once, and a guard test asserts exactly one `.cookie-consent` in the HTML of every public page. On mobile, bottom-fixed CTAs (`gd-sticky`, the sign-up sheet trigger) are hidden while the banner is visible (`html:not(.cookie-consent-known)`), and they appear after a choice. The banner never covers the hero CTA at 360×640. *(Dennis, 29-09)*
- **D16. Funnel bug fix:** every candidate-facing "maak gratis account" / "Registreren" goes to `/account-maken`, and the TeaserLayout logo goes to "/". `/register` stays the company (KvK) page, labelled "Bedrijf registreren". *(Dennis, 29-09)*
- **D17. Passport tab label:** "Past deze baan?" → "Past dit beroep?" when OFF, through the same `.Zw` key pattern (07.6). *(Dennis, 29-09)*
- **D18. Mascot:** one component, today's mascot as the fallback, and an asset contract (02). New art = drop files + flip the manifest entry, with no markup change. *(Dennis didn't say whether he has a designer; the contract makes either path work.)*
- **D19. Age and consent stay as they are.** The test keeps its 16+ question (`GratisDnaUnder16View`), and under-16s are sent to `/account-maken?van=onder16` instead of `/register`. Sign-up doesn't add an age gate: the existing onboarding birth-date step and the parental-consent flow (`ParentalConsentController`, `CandidateConsentRules`) apply unchanged to e-mail sign-ups, exactly as for Google/Microsoft. `/account-maken` shows the line "Door verder te gaan ga je akkoord met de voorwaarden en de privacyverklaring" and records the same acceptance the external sign-up records today.
- **D20. `FeatureRoutes` change (with the switch):** ON-candidate home = `/banenkaart` (was "/"), OFF-anonymous home = `/` (was `/ontdek`). Tests in the werkgevers-actief code are updated in 07 (or documented, when absent, per Dependencies A).

## Dependencies (check before 01; say in PR 01 which case applied)
- **A. "Werkgevers actief" switch, `docs/mijn-paspoort` file 01 (§F feature flags).** Today it lives on the unmerged branch `origin/cursor/werkgevers-actief` (`FeatureRoutes`, `IFeatureFlags`, `RequiresFeatureAttribute`, `PlatformFeatureSettings.EmployersEnabled`, `FeatureRouteGate`). Check: `git grep -n "interface IFeatureFlags" origin/acceptatie -- Jobsy.Core` and `git grep -n "class FeatureRoutes" origin/acceptatie -- Jobsy.Core`.
  - **Present:** 01 implements `IEmployersSwitch` as a 5-line adapter over `IFeatureFlags` (`EmployersEnabled`). 05/07 use `FeatureRoutes.HomeFor`. 07 applies D20 to `FeatureRoutes` and updates `FeatureFlagFoundationTests` (`HomeFor(null, off)` becomes "/") and any Playwright test that expects "/" → `/ontdek` when OFF. `/banenkaart` and `/werkgevers` get `[RequiresFeature(PlatformFeature.Employers)]` with fallback "/". Add the change to `docs/feature-flags.md` ("Changed by landing 07").
  - **Absent:** 01 adds `IEmployersSwitch` with `AlwaysOnEmployersSwitch` (returns ON) registered by default. Every OFF behaviour is still built and tested through a test double. Gates use a local `EmployersGate` page/endpoint check that asks `IEmployersSwitch`. Add **`docs/feature-flags-landing-followup.md`**, stating exactly what werkgevers-actief 01 must do when it lands: (1) register an `IEmployersSwitch` adapter over `IFeatureFlags`, (2) **anonymous "/" OFF renders the landing -zw variant instead of redirecting to `/ontdek`; the home canonical stays "/"**, (3) ON-candidate home = `/banenkaart`, (4) replace `EmployersGate` with `RequiresFeature`. Also add the same four lines as a comment on the `IEmployersSwitch` interface.
  - **Re-check at the start of 07.** If it flipped to present, `git merge origin/acceptatie` into `cursor/landing-7` (normal merge, say so) and switch to the "present" path there.
  - Never branch from or merge `cursor/werkgevers-actief` itself.
- **B. Passport tab key `Passport.Tab.Fit`** (`docs/mijn-paspoort` 04b). Check: `git grep -n '"Passport.Tab.Fit"' origin/acceptatie -- Jobsy.Web/Localization`.
  - **Present:** 07 adds `Passport.Tab.Fit.Zw` = "Past dit beroep?" (5 languages) and makes the tab pick the key via `IEmployersSwitch`.
  - **Absent:** 07 adds only the landing illustration label (`Landing.Get.Passport.TabFit` / `.Zw`) and adds a line to `docs/feature-flags-landing-followup.md`: "Passport tab `Passport.Tab.Fit` must use `.Zw` = 'Past dit beroep?' when OFF".
- **C. GratisDna components** (present at `a611db40`: `Pages/Public/GratisDna.razor`, `Components/Public/GratisDnaResultView.razor`, `GratisDnaRegisterBox`, `GratisDnaMerge`, `GratisDnaUnder16View`, `UiStringsGratisDna.cs`). If any moved, keep the semantics and say so.
- **D. CI stack** (present: `.github/workflows/pr-tests.yml`, `.github/scripts/start-ci-stack.sh`, base URL `http://127.0.0.1:5201`). 10 adds the Lighthouse job on top of it. If the script moved, use its replacement; if there's no way to start the stack in CI, run Lighthouse against the Playwright host and say so.
- **E. Scholen stack `SchoolsEnabled`** (`docs/scholen` 01). Check: `git grep -n "SchoolsEnabled" origin/acceptatie -- Jobsy.Core`. Present → `/scholen` shows the portal login link when true. Absent → always "binnenkort" + mail CTA.
- **F. Admin redesign** (`docs/admin-redesign`). Check: `git grep -n "class AdminNavCatalog\|AdminSidebar" origin/acceptatie -- Jobsy.Web`. Present → `/admin/funnel` goes into its sidebar group "Inzicht" (or the closest analytics group). Absent → one item in today's admin nav next to the existing metrics pages.
- **Recommended landing order:** werkgevers-actief (mijn-paspoort 01) → this stack. Not a hard requirement: every case above has a fallback. Never branch from an unmerged branch of another stack.
