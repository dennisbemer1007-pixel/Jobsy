> **Warnings:** deliver a Release build with **0 warnings** (`TreatWarningsAsErrors=true`). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. No blanket `<NoWarn>`.

# Step 03: UI button, sheet/panel, reason chips, i18n

**Branch:** `git fetch origin && git checkout -b cursor/vv-03-ui origin/cursor/vv-02-api`. **PR base:** `cursor/vv-02-api`. Title: `feat(vv-03): "Meer rotsen zoals deze" button + sheet + chips`.

Read [00-README.md](00-README.md) (D11, D12). The visual target is in `docs/mockups/vergelijkbare-vacatures/`; open the `.html` files in a browser next to the PNGs.

## 0. Rules (read first, they apply to this step)
- **Stacked PRs.** One PR for this step only. Branch and base are given in the header above. Each step's PR targets the previous step's branch (01 targets `acceptatie`), so the chain is `acceptatie ← 01 ← 02 ← 03 ← 04 ← 05`. If the parent branch moves, **merge** the parent into your branch (no rebase of pushed commits).
- **Never merge and never deploy.** Do not merge any PR, do not trigger a Render deploy, and do not use the shortcuts `123`, `456` or `999` from `.cursor/rules/`. Dennis merges.
- **Never push to `main` or `acceptatie`. No force-push** of any kind (`--force` and `--force-with-lease` included).
- **Red tests: stop with a draft PR.** If any test is red and you cannot fix it inside the scope of this step, open the PR as **draft**, list the failing tests and your findings in the PR body, and stop.
- **Warnings.** Release build with 0 warnings: `dotnet build Jobsy.sln -c Release` with `TreatWarningsAsErrors=true` (already set in `Directory.Build.props:10`). Run `dotnet format` and `.github/scripts/count-build-warnings.sh` before opening the PR. Fix any warning you hit **in the same PR**, even if it is old. No blanket `<NoWarn>`, no `#pragma warning disable` without a one-line reason.
- **Serious bugs elsewhere go in a standalone hotfix.** If you find a serious bug outside this feature (security, data loss, crash, wrong money), do not fix it in this PR. Open a separate branch `cursor/hotfix-<topic>` from `origin/acceptatie`, a separate PR into `acceptatie`, and link it from this PR body.
- **Code references** below were checked on `origin/acceptatie` @ `32f47798` (2026-10-02 20:50 CEST). Re-check line numbers before editing, since they move.
- **Mockups** live on branch `docs/vergelijkbare-vacatures`, folder `docs/mockups/vergelijkbare-vacatures/`. The mockup CSS uses raw hex and inline styles for speed. Do **not** copy that; rebuild with design tokens (`.cursor/rules/design-system.mdc`).
- **Privacy.** Never put names, e-mail addresses, phone numbers, street addresses, dates of birth or user ids into embedding input, logs or analytics.

## 1. What exists today (verified)
| Topic | Where | Fact |
|---|---|---|
| Page | `Jobsy.Web/Components/Pages/VacancyDetail.razor` | `@page "/vacancies/{Id:guid}"`, `InteractiveServerRenderMode(prerender: true)`, `[AllowAnonymous]`. States: `_vacancy` (open), `_closed` (410 view with the existing "similar" list), not found. |
| Rail | `VacancyDetail.razor:241-265` | `<aside class="kb-detail__rail">` holds `@TravelCardFragment` and then `.kb-detail__rail-actions` (apply/save/share/passport hint). |
| Rail CSS | `wwwroot/css/features/kandidaat-banen.css:1249-1253, 1279-1286` | `.kb-detail__rail-actions` is **hidden ≤ 1023 px** (the sticky bottom bar takes over on mobile). The rail itself (travel card) stays visible and is moved under the facts on mobile. |
| Origin | `VacancyDetail.razor:1407-1452`, `:2056` | The page resolves an `OriginPoint` from `jobsyGeo.getStoredOrigin` (banenkaart origin), and `_transport`. **Do not** call `jobsyGeo.requestLocation` for this feature (no permission prompt). |
| API client | `Jobsy.Web/Services/ApiClient/JobsyApiClient.Vacancies.cs:297` `GetVacancySimilarAsync` | Pattern for query string + invariant culture numbers. Web model `SimilarVacancyItem` in `Jobsy.Web/Models/VacancyListItem.cs:293`. |
| Existing sheet | `.filter-sheet` in `wwwroot/css/app.css:2117-2175` | Look to copy (handle, mascot header, rounded top). **Do not reuse the class**: `app.css:1898-1903` hides it ≥ 769 px with `!important`. |
| Mascot | `BrandImages.AbsoluteWebp128` | `lobsy-128.webp`. |
| Travel display | `Jobsy.Web/Components/KandidaatBanen/KbTravelTime.razor` | `Minutes`, `Transport`, `Approx`. Use it on every row that has minutes. |
| Card bits | `kandidaat-banen.css` `.kb-card*` | Thumb/title/company tokens. Reuse tokens, not classes that bring layout. |
| CSS linking | `Jobsy.Web/Components/App.razor:98-125` (inside `@if (LoadBlazorRuntime)`, `kandidaat-banen.css` at `:119`) and the `<noscript>` line `:126` | Both places. Plus `Jobsy.Tests/asset-versions.json` (`sha256` + `v`), checked by `AssetVersionGuardTests`. |
| i18n | `Jobsy.Web/Localization/UiStringsKandidaatBanen.cs:5-18` | `Add(key, nl, en, pl, ro, ar)`. **All 5 languages.** `LocalizationParityReportTests` counts values identical to NL as untranslated (baseline `docs/i18n/untranslated-baseline.txt` must not grow). |
| Routes | `PublicRoutes.Test` (`/ontdek`), `OnboardingRoutes.StartPath(flags)` (`/candidate/ontdekkingsreis`) | CTA targets. |
| Crawlers | `CrawlerUserAgent.ShouldSkipInteractiveRuntime` (`App.razor:325`) | No Blazor runtime for crawlers: render nothing for them. |

## 2. Components
New in `Jobsy.Web/Components/KandidaatBanen/`:

### `SimilarVacanciesEntry.razor` (the button)
- Placement: inside `<aside class="kb-detail__rail">`, **directly after** `.kb-detail__rail-actions` (`VacancyDetail.razor:265`). It must be visible on mobile and desktop, so it is **not** inside rail-actions.
- Render only when `_vacancy is not null` (never on the `_closed`/410 view, nor on not found), the feature switch is on (step 04; until then always on), and the viewer is not a crawler.
- Markup: one `<button type="button" class="similar-entry" aria-haspopup="dialog" aria-expanded="@_open" aria-controls="similar-sheet">` with:
  - icon tile (stones SVG from the mockup, `aria-hidden`)
  - title `Similar.Button.Title`
  - sub `Similar.Button.Sub` (candidate with passport) or `Similar.Button.SubAnon` (otherwise; see README open question 1)
  - chevron
- Full width of the rail, min-height 64 px, whole row clickable.

### `SimilarVacanciesSheet.razor` (the result surface)
- **< 900 px:** a bottom sheet (`max-block-size: 88dvh`, rounded top, handle, backdrop). **≥ 900 px:** a right panel 460 px wide, full height, with a backdrop. One component, CSS decides the layout.
- `role="dialog" aria-modal="true" aria-labelledby="similar-sheet-title" id="similar-sheet"`. Focus goes to the title on open. Focus trap (reuse the existing trap helper the share sheet uses, if there is one; otherwise a small `similarSheet.trapFocus` in a new `wwwroot/js/features/similar-sheet.js`). `Esc`, backdrop click and × (`Common.Close`) close it. Focus returns to the entry button. Lock body scroll while open.
- Header: mascot (`BrandImages.AbsoluteWebp128`, 40 px, `alt=""`), title, sub, ×.
- Why line by `Mode`: `Similar.Sheet.Why.Personal` | `.Vacancy` | `.Fallback`.
- **Lazy:** call the API only when opened the first time; keep the result while on the page. Show `Similar.Sheet.Loading` with 3 skeleton rows.
- Rows (max 6), each an `<a href="/vacancies/{id}">` (min 44 px, full row):
  - 76 px thumbnail (the card image, else the category tile)
  - title (2 lines max, ellipsis)
  - company · place (intermediary display rules come from `MapCard`)
  - travel: `KbTravelTime` with minutes when known, else `≈ {km} km` (literal, no key)
  - **one** reason chip
- Under the list: `Similar.Sheet.More` link to `/banenkaart`.
- `ShowDiscoveryCta`: card `Similar.Cta.Title` / `Similar.Cta.Text` + button `Similar.Cta.Button`.
  - Anonymous → `PublicRoutes.Test`.
  - Candidate without passport → `OnboardingRoutes.StartPath(flags)`.
- Empty (0 items): mascot + `Similar.Sheet.Empty` + the banenkaart link. An API error shows the same empty state (the API should never fail, but the UI must not throw either).
- The "Voorbeelddata" pill in the mockup is the existing test-data badge: show it only where the cards already show it.

### Web model + client
- `Jobsy.Web/Models/MoreLikeThisModels.cs`: `MoreLikeThisResult(Mode, ShowDiscoveryCta, Items)`, `MoreLikeThisItem(VacancyListItem Card, ReasonCode, ReasonArg)`.
- `JobsyApiClient.Vacancies.cs`: `GetVacancyMoreLikeThisAsync(Guid id, double? originLat, double? originLng, string transport, int? maxMinutes, CancellationToken ct)`. Pass the page's resolved origin (stored origin only) and `_transport`.

### Reason chip mapping (`Jobsy.Web/Components/KandidaatBanen/SimilarReasonText.cs`, pure)
| Code | Key | Arg → text |
|---|---|---|
| `closer` | `Similar.Reason.Closer` | none |
| `value` | `Similar.Reason.Value` | `WaProfile.Card.{arg}` (e.g. `teamgevoel` → "Een hecht team") |
| `strength` | `Similar.Reason.Strength` | `Competency.Cat.{arg}` |
| `outdoor` | `Similar.Reason.Outdoor` | none |
| `flexible` | `Similar.Reason.Flexible` | none |
| `no-experience` | `Similar.Reason.NoExperience` | none |
| `engagement` | `Similar.Reason.Engagement` | `WaEngage.Item.{arg}` |
| `same-branch` | `Similar.Reason.SameBranch` | the label itself (already shown on cards as-is) |
| `same-work` / unknown | `Similar.Reason.SameWork` | none |

If the arg key is missing in the culture, fall back to `same-work` text. Never show a raw key or arg.

## 3. CSS
- New file `Jobsy.Web/wwwroot/css/features/vergelijkbare-vacatures.css`, BEM blocks `similar-entry`, `similar-sheet`, `similar-row`, `similar-chip`, `similar-cta`.
- Link it in **both** places in `App.razor` (after `kandidaat-banen.css`) and register it in `Jobsy.Tests/asset-versions.json`.
- **Tokens only** (`var(--…)` from the theme; the mockup HTML uses raw hex for portability, so do not copy those values).
  - Brand purple for the icon tile and title accent.
  - Chip: tinted brand background + brand text, radius pill, 13 px, one line, ellipsis.
- Breakpoints 640 / 900 / 1024. Logical properties (`inset-inline-end`, `padding-inline`) so `ar` (RTL) mirrors correctly.
- `prefers-reduced-motion: reduce` → no slide animation.
- Must not cause horizontal overflow at 320–1440 px (long Polish/Romanian chips ellipsize).

## 4. i18n keys (add via `Add(...)` in `UiStringsKandidaatBanen.cs`)
| Key | NL | EN | PL | RO | AR |
|---|---|---|---|---|---|
| Similar.Button.Title | Meer rotsen zoals deze | More rocks like this one | Więcej skał takich jak ta | Mai multe stânci ca aceasta | المزيد من الصخور مثل هذه |
| Similar.Button.Sub | Lijkt hierop en past bij jou | Similar to this and a fit for you | Podobne do tej i pasujące do ciebie | Seamănă cu acesta și ți se potrivește | تشبه هذه وتناسبك |
| Similar.Button.SubAnon | Lijkt op deze baan | Similar to this job | Podobne do tej oferty | Seamănă cu acest job | تشبه هذه الوظيفة |
| Similar.Sheet.Why.Personal | Gekozen op het werk, je waarden en je reistijd. Alleen echte bedrijven. | Picked on the work, your values and your travel time. Only real companies. | Wybrane według pracy, twoich wartości i czasu dojazdu. Tylko prawdziwe firmy. | Alese după muncă, valorile tale și timpul de drum. Doar firme reale. | اخترناها حسب العمل وقيمك ووقت التنقل. شركات حقيقية فقط. |
| Similar.Sheet.Why.Vacancy | Gekozen op het soort werk. Alleen echte bedrijven. | Picked on the type of work. Only real companies. | Wybrane według rodzaju pracy. Tylko prawdziwe firmy. | Alese după tipul de muncă. Doar firme reale. | اخترناها حسب نوع العمل. شركات حقيقية فقط. |
| Similar.Sheet.Why.Fallback | Zelfde soort werk in de buurt. Alleen echte bedrijven. | Same kind of work nearby. Only real companies. | Ten sam rodzaj pracy w pobliżu. Tylko prawdziwe firmy. | Același tip de muncă în apropiere. Doar firme reale. | نفس نوع العمل بالقرب منك. شركات حقيقية فقط. |
| Similar.Sheet.Loading | Lobsy zoekt rotsen… | Lobsy is looking for rocks… | Lobsy szuka skał… | Lobsy caută stânci… | لوبسي يبحث عن صخور… |
| Similar.Sheet.Empty | Nog geen andere rotsen in de buurt. Kijk later nog eens. | No other rocks nearby yet. Check again later. | W pobliżu nie ma jeszcze innych skał. Zajrzyj później. | Încă nu sunt alte stânci în apropiere. Revino mai târziu. | لا توجد صخور أخرى قريبة بعد. عد لاحقًا. |
| Similar.Sheet.More | Bekijk meer op de banenkaart | See more on the job map | Zobacz więcej na mapie ofert | Vezi mai multe pe harta joburilor | شاهد المزيد على خريطة الوظائف |
| Similar.Cta.Title | Wil je banen die bij jóu passen? | Want jobs that fit you? | Chcesz ofert, które pasują do ciebie? | Vrei joburi care ți se potrivesc? | هل تريد وظائف تناسبك أنت؟ |
| Similar.Cta.Text | Doe de ontdekkingsreis. Gratis. | Take the discovery journey. It's free. | Wyrusz w podróż odkrywczą. Za darmo. | Pornește în călătoria de descoperire. Gratuit. | ابدأ رحلة الاستكشاف. مجانًا. |
| Similar.Cta.Button | Start | Get started | Zacznij | Începe | ابدأ |
| Similar.Reason.Closer | Dichter bij huis | Closer to home | Bliżej domu | Mai aproape de casă | أقرب إلى المنزل |
| Similar.Reason.Outdoor | Ook buiten werken | Also work outdoors | Też praca na zewnątrz | Tot muncă în aer liber | عمل في الهواء الطلق أيضًا |
| Similar.Reason.Value | Past bij je waarde: {0} | Fits your value: {0} | Pasuje do twojej wartości: {0} | Se potrivește cu valoarea ta: {0} | تناسب قيمتك: {0} |
| Similar.Reason.Strength | Past bij je sterkte: {0} | Fits your strength: {0} | Pasuje do twojej mocnej strony: {0} | Se potrivește cu punctul tău forte: {0} | تناسب نقطة قوتك: {0} |
| Similar.Reason.SameBranch | Zelfde branche: {0} | Same sector: {0} | Ta sama branża: {0} | Același sector: {0} | نفس القطاع: {0} |
| Similar.Reason.Flexible | Ook flexibele tijden | Also flexible hours | Też elastyczne godziny | Tot program flexibil | أوقات مرنة أيضًا |
| Similar.Reason.NoExperience | Geen ervaring nodig | No experience needed | Doświadczenie niepotrzebne | Nu e nevoie de experiență | لا حاجة إلى خبرة |
| Similar.Reason.Engagement | Zelfde inzet: {0} | Same commitment: {0} | To samo zaangażowanie: {0} | Același angajament: {0} | نفس الالتزام: {0} |
| Similar.Reason.SameWork | Zelfde soort werk | Same kind of work | Ten sam rodzaj pracy | Același tip de muncă | نفس نوع العمل |

- EN `Similar.Cta.Button` is "Get started", not "Start": an EN value identical to NL counts as untranslated.
- PL/RO/AR were written by hand. Ask a native speaker to check them in the PR (list them in the PR body), but do not block on it.
- "Rotsen" is Lobsy's brand metaphor (the lobster's rocks = jobs). Keep the metaphor in every language rather than translating to "jobs".

## 5. Tests (this PR)
- bUnit `SimilarVacanciesEntryBunitTests`:
  - Renders in the open state and not in the closed/410 state.
  - Sub text for a candidate with a passport vs anonymous.
  - `aria-haspopup`/`aria-expanded`/`aria-controls`.
- bUnit `SimilarVacanciesSheetBunitTests` (fake `JobsyApiClient` handler):
  - The API is not called before open.
  - Loading → items; max 6 rows; each row has one chip and an `href` to `/vacancies/{id}`.
  - Why line per mode.
  - CTA visible with the right route (anonymous vs candidate).
  - Empty and error states.
  - `role=dialog`, `aria-modal`, `aria-labelledby`; × uses `Common.Close`.
- `SimilarReasonTextTests`: every code maps; a missing arg key falls back to same-work; no raw keys.
- Localization: the 21 keys exist in all 5 cultures; `LocalizationParityReportTests` baseline unchanged.
- `AssetVersionGuardTests` green with the new CSS file.

## 6. Acceptance
- CI green; Release build 0 warnings.
- PR body has screenshots of a local run at 390×844 and 1440×900 (button, sheet personal, sheet anonymous with CTA), side by side with the mockup PNGs.
- No horizontal overflow at 320, 390, 768, 1024, 1440 (check in devtools, as in step 05).
- Keyboard only: Tab to the button → Enter opens → Tab cycles inside → Esc closes → focus is back on the button.
