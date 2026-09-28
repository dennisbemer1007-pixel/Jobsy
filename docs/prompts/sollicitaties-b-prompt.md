# Cursor prompt: "Mijn sollicitaties" redesign, variant B (Fotokaarten)

## 0. Ground rules
- **Branch:** `git fetch origin && git checkout -b cursor/sollicitaties-fotokaarten origin/acceptatie`.
- **Exactly ONE pull request**, targeting **`acceptatie`**. Do **not** merge it, do **not** deploy, and do **not** use or trigger shortcut/rule **`123`** (or `456`). Never push to `acceptatie` or `main`. See `docs/release-flow.md`.
- **Reference images** are in this repo at `docs/mockups/sollicitaties/`:
  - `sol-b-fotokaarten.png` is the target design.
  - `overview.png` shows the current screen next to the variants; **B** is the one Dennis chose.
  - Fetch them with `git fetch origin docs/sollicitaties-b && git checkout origin/docs/sollicitaties-b -- docs/mockups/sollicitaties docs/prompts/sollicitaties-b-prompt.md`, and include that folder in your PR.
- **The photos in the mockup are illustrative Unsplash images only.** Use real app images via the picture rule in §2. Never hotlink Unsplash or picsum. `VacancyImageUrls.ForCard` already blocks external URLs because of the CSP (`img-src 'self'`).
- **Scope:** only the candidate page `/candidate/applications` and the data it needs. Keep all existing routes and behaviour: the read-only mode for non-candidate roles, persisted prerender state, culture reload, the empty states, CV download and withdraw.
- **Follow the Lobsy design system** (`.cursor/rules` and the design-system rules):
  - Tokens only, no new hex values. Weights 400/600, and 700 only for the `h1`.
  - `--coral` is not needed here.
  - Tap targets are at least 44px. Use logical properties so RTL (ar) works.
  - No inline `style=""`.
  - All text goes through `Culture[...]` in all 5 languages (nl/en/pl/ro/ar) in `Jobsy.Web/Localization/UiStrings*.cs`.

## 1. Current code (origin/acceptatie)
| What | Where |
|---|---|
| Page | `Jobsy.Web/Components/Pages/Candidate/Applications.razor` (`@page "/candidate/applications"`). It has tabs `all/open/rejected/matched` (`apps-tabs`, `admin-sublink`), `application-card` markup, the `application-stepper`, and the buttons `DownloadCvAsync` and `WithdrawAsync`. |
| Status steps | `Jobsy.Core/Rules/ApplicationStatusWizard.cs`: `TrackASteps` (Gesolliciteerd, In behandeling, Contact, Gematched), `TrackBSteps`, `IsRejectedTrack`, `CurrentStepIndex` |
| Status labels | `Jobsy.Web/Localization/UiLabels.cs` → `ApplicationStatus(culture, status)`; keys `Apps.Wizard.*`, `Apps.Status.*` |
| Web model | `Jobsy.Web/Models/DashboardModels.cs` → `ApplicationItem` |
| API DTO | `Jobsy.Api/Models/DashboardDtos.cs` → `record ApplicationDto(...)`, used in several places in `ApplicationsController` and `MeController` |
| Candidate list endpoint | `Jobsy.Api/Controllers/MeController.cs` → `GET api/me/applications` (`GetMyApplications`). It projects rows and uses `CandidateApplicationLocation.ForPublicCard(...)` to decide which company name to show (client or intermediary). |
| Client call | `JobsyApiClient.GetMyApplicationsAsync()` |
| Image helper | `Jobsy.Core/Media/VacancyImageUrls.cs` → `ForCard(imageUrl, logoUrl, id, workType)`: vacancy photo → company logo → local work-type WebP (`/images/vacancies/{slug}.webp`). Used in `VacanciesController` with `WorkTypeLabelList.FirstOrDefault()`. |
| Dialog | `Jobsy.Web/Components/LobsyFriendlyDialog.razor` (`IsOpen`, `Title`, `Lead`, `ChildContent`, `OnClose`) |
| Styles | `.application-card*`, `.application-stepper*` and `.apps-tabs` in `wwwroot/css/app.css` (~line 15358), mirrored in `app.min.css`. Feature CSS lives in `wwwroot/css/features/` and is linked in `Components/App.razor` (both the `<link>` and the `<noscript>` copy, with `?v=`). |
| Tests | `Jobsy.Tests/MobileSaasUxTests.cs` → `Candidate_applications_use_cards_with_current_status_and_bar_stepper` asserts the old markup and CSS. Update it for the new design. Playwright pattern: `MobileSmokePlaywrightTests.cs`, `CandidateTabStabilityPlaywrightTests.cs` (`JOBSY_E2E_BASE_URL`, soft-skip, 390×844). |

**Withdraw today:** `WithdrawAsync` calls `Api.WithdrawApplicationAsync(id)` straight away, with **no confirmation**. The string `Apps.WithdrawConfirm` ("Sollicitatie intrekken?") already exists in all languages but is unused.
- Keep the existing withdraw API call, the busy state and the `Apps.WithdrawnMsg` message.
- Put a confirm step in front of it using `LobsyFriendlyDialog`, with title `Apps.WithdrawConfirm`, a new lead string, and the buttons "Annuleren" and "Intrekken" (the danger action).

## 2. Data change: picture per application
1. Add `string? PictureUrl` (and `string? PictureKind`: `"photo" | "logo" | "placeholder"`) to `ApplicationDto` as **optional trailing parameters with default `null`**, so the other constructors keep compiling. Add the same to `ApplicationItem`.
2. In `MeController.GetMyApplications`, also project:
   - `a.Vacancy.ImageUrl`
   - `a.Vacancy.Company.LogoUrl`
   - the intermediary company's `LogoUrl`
   - `a.Vacancy.WorkTypes` and `a.Vacancy.WorkTypeLabels`

   Then compute in memory:
   - `logo` = the logo of **the same company whose name is shown**. Follow `CandidateApplicationLocation.ForPublicCard`: if the intermediary name is shown, use the intermediary logo.
   - `workType = WorkTypeLabels.ResolveLabels(row.WorkTypes, row.WorkTypeLabels).FirstOrDefault()`, or the same helper `VacanciesController` uses.
   - `PictureUrl = VacancyImageUrls.ForCard(row.ImageUrl, logo, row.VacancyId, workType)`. **Reuse `ForCard` as is.** The fallback order is therefore vacancy photo → company logo → category SVG.
   - `PictureKind`: `"photo"` if the result is not the normalised logo and not a `/images/vacancies/` SVG, `"logo"` if it equals the normalised logo, otherwise `"placeholder"`. Put this in a small pure helper next to `ForCard`, with unit tests.
3. Don't return data-URIs; `ForCard` already maps them to `/api/vacancies/{id}/image`. No DB migration is needed.
4. Add unit tests for the fallback chain (photo, logo only, neither → SVG, external https → logo or SVG) and for the intermediary logo choice.

## 3. Layout (mobile first; see `sol-b-fotokaarten.png`)
Keep the page shell (`panel-page apps-page`) and the app header and bottom nav as they are. Put new styles in **`wwwroot/css/features/applications.css`** under `.apps-page`, and link it in `App.razor` like `onboarding-wizard.css` (both the link and the noscript copy). Bump `?v=`. Remove the now-unused `.application-stepper*`, `.application-card__actions` and `.apps-tabs` rules from **both** `app.css` and `app.min.css`.

**Header**
- `h1` "Mijn sollicitaties": `--text-2xl`, 700, `--brand`.
- A friendly subtitle, `--text-sm`, `--muted`:
  - New key `Apps.LeadFriendly` = "Goed bezig, {0}. Dit is de stand van zaken." Use the first name from the `given_name` / `ClaimTypes.GivenName` claim, the same source as `AuthHeader.DisplayName`.
  - Without a name, use `Apps.LeadFriendlyNoName` = "Goed bezig! Dit is de stand van zaken."
  - Keep `Apps.LeadReadOnly` for read-only roles.

**Counters = the filter** (these replace the `apps-tabs` pill row)
- A 3-column grid with a 8px gap and 16px vertical margin. Each counter is a `<button>` with `aria-pressed`, min-height 64px, `--surface`, `--radius`, `--shadow`, padding 10px 12px.
- Inside: the number (`--text-xl`, 600, `--brand`) with a label under it (`--text-xs`, `--muted`).
- Selected: `box-shadow: inset 0 0 0 2px var(--brand)`. The "Lopend" number uses `--success`.

| Counter | Key (NL) | Statuses |
|---|---|---|
| Open | `Apps.TabOpen` "Open" | `Pending` |
| Lopend | `Apps.TabRunning` "Lopend" (new; replaces the *Gematched* tab, so remove `Apps.TabMatched` usage) | `Accepted`, `EmployerContacting`, `Hired` |
| Afgewezen | `Apps.TabRejected` "Afgewezen" | `Rejected`, `FilledElsewhere`, `Withdrawn` |

- **Default:** no counter is selected and every application is shown (the old "Alles").
  - Tapping a counter filters the list. Tapping the selected counter again clears the filter.
  - While a filter is active, show a small text button "Alles tonen" (`Apps.TabAll`, 44px) under the grid.
- Counts come from the full `_items`. Update them after a withdraw.
- Rename the filter predicates: `IsOpen` = Pending, `IsRunning` = the three statuses above, `IsRejected` unchanged.
- Wrap the grid in `role="group"` with `aria-label="@Culture["Nav.MyApplications"]"`. Each button's accessible name is "{n} {label}".

**Card** (`article.application-card`, keeping the class names `application-card-list`, `application-card`, `application-card__title` and `application-card__company`)
- **Container:** a flex row with a 12px gap, `--surface`, radius 14px, `--shadow` plus a very soft second shadow, padding 10px, and a 12px gap between cards.
- **Picture:** `<img class="application-card__img">`, 84×84, `--radius`, `object-fit: cover`, `loading="lazy"`, `alt=""` (decorative, since the title is next to it), with `width`/`height` attributes.
  - When `PictureKind == "logo"`: `object-fit: contain`, `--surface` background, a 1px `--border` ring and 10px padding, so logos are never cropped.
  - Placeholder SVGs use cover.
- **Body:** a column, `min-width: 0`, with 32px end padding reserved for the ⋯ button.
  - **Title:** a link to `/vacancies/{VacancyId}`, `--text-md`, 600, `--text`, a single line with ellipsis. Also make the picture a link to the same URL (with `tabindex="-1"`, so there's only one tab stop).
  - **One secondary line:** the company name only (`--text-sm`, `--muted`, ellipsis). Location and travel no longer show on the card; they're on the vacancy page.
  - **Bottom row** (`margin-top: auto`), a flex row with a 10px gap, left to right:
    1. **one status pill**
    2. **the mini progress bar**
    3. **the date**, pushed to the end
- **Status pill:** 24px high, `--radius-pill`, `--text-xs`, 600, with a 7px dot in `currentColor`. The label comes from the existing `WizardStepName` / `UiLabels.ApplicationStatus`.

  | Status | Label | Style |
  |---|---|---|
  | Pending | Gesolliciteerd | `--neutral-soft`-like pearl background (use an existing soft neutral token), `--muted` text |
  | Accepted | In behandeling | `--accent-soft` background, `--brand` text |
  | EmployerContacting | Contact | `--brand` background, white text |
  | Hired | Gematched | `--success-soft` background, `--success` text |
  | Rejected / FilledElsewhere / Withdrawn | Afgewezen / Ingevuld elders / Ingetrokken | transparent, 1px `--border` ring, `--muted` text |

- **Mini progress bar:** `<span class="application-card__progress" aria-hidden="true">` with **4 segments**, each 14×4px, radius 2px, 3px gap.
  - Segments `0..CurrentStepIndex` are `--brand`; the rest are `--pearl`/`--border`.
  - On the rejected track, show 2 segments in `--border` (no brand colour).
  - Put the full status text in the pill; the old "Status: …" line (`Apps.StatusNow`) becomes a visually hidden text inside the card for screen readers.
- **Date:** a short date, e.g. "20 sep" (for other years "20 sep 2025"), `--text-xs`, `--muted`, nowrap, in `<time datetime="…">`. Keep the culture handling from `AppliedOnLabel`, and set the full `Apps.AppliedOn` text as `title`/`aria-label`.
- **Hired cards** get the modifier `application-card--hired`: `box-shadow: inset 0 0 0 1.5px` with a soft green (derive it from `--success` with `color-mix`, or use an existing soft-success border token) plus a very soft green shadow. No other colour change.
- **Rejected-track cards** get `application-card--closed`: the picture has `filter: grayscale(1)` at opacity .6, and the title turns `--muted`. They stay fully readable.

**⋯ menu** (replaces the two big buttons)
- A 44×44 `icon-btn` at the top end of the card (inset 4px). Use a `more-horizontal` icon, with `aria-label="Acties voor {titel}"` (new key `Apps.ActionsFor`), `aria-haspopup="menu"` and `aria-expanded`. When open, it gets an `--accent-soft` background.
- The popover is anchored under the button at the end: 218px wide, `--surface`, `--radius`, `--shadow-lg`, a 1px pearl ring, padding 6px, `role="menu"`. Items are `role="menuitem"` rows, 44px high, with an 18px brand icon and `--text-sm` text:
  1. **Lobsy-CV bekijken** (`Apps.MenuCv`) → the existing `DownloadCvAsync(a)`, including the busy state ("…").
  2. **Vacature bekijken** (`Apps.MenuVacancy`) → navigate to `/vacancies/{VacancyId}`.
  3. A divider, then **Sollicitatie intrekken** (`Apps.MenuWithdraw`) in `--danger` (text and icon). Show it **only** when `CanWithdraw(a.Status)` (Pending). It opens the `LobsyFriendlyDialog` confirm (§1); on confirm, run the existing `WithdrawAsync(a)`.
- **Read-only roles** see only "Vacature bekijken", which is the same as today: no CV and no withdraw.
- Only one menu can be open at a time. It closes on outside click/tap, on Esc (focus returns to the ⋯ button), after choosing an item, and when the filter changes.
  - ↑/↓ moves between items, and focus moves to the first item when the menu opens.
  - Implement the outside click with a transparent full-screen backdrop button inside the component. Don't add a new global JS dependency; reuse an existing JS helper if there is one.
- The menu must not be clipped by the card: don't use `overflow: hidden` on the card, and give the open card a higher z-index.

**Empty states:** keep `Apps.Empty` + `Apps.BrowseMap` and `Apps.EmptyTab`. Style them as a centred calm card (`--surface`, the muted text, and one link).

**Loading:** keep `PageContentSkeleton Variant="apps"`, and adjust the skeleton to the new card shape (84px square plus 3 lines) if that's easy.

**Desktop and tablet:** the same cards in one centred column, `max-width: 720px; margin-inline: auto`. From 900px, the picture may be 96×96. No grid and no other layout.

## 4. Localization
- New keys, in all 5 languages: `Apps.LeadFriendly`, `Apps.LeadFriendlyNoName`, `Apps.TabRunning`, `Apps.ActionsFor`, `Apps.MenuCv`, `Apps.MenuVacancy`, `Apps.MenuWithdraw`, `Apps.WithdrawConfirmLead` ("De werkgever krijgt hiervan bericht. Dit kun je niet ongedaan maken."), `Common.Cancel` (only if it doesn't exist yet).
- Stop using `Apps.TabMatched` on this page. Leave the key in place if other code uses it.

## 5. Tests
**Unit / structure**
- Update `MobileSaasUxTests.Candidate_applications_use_cards_…`:
  - It should assert the counters (`application-counters`), `application-card__img`, `application-card__progress`, the ⋯ menu (`aria-haspopup="menu"`), `application-card--hired`, `LobsyFriendlyDialog` + `Apps.WithdrawConfirm`, and `Apps.TabRunning`.
  - Keep the `DoesNotContain("<table")` checks.
  - Update the CSS assertions to the new feature file.
- Add tests for the picture helper (§2).
- Add a test for the filter mapping (Open = Pending; Lopend = Accepted/EmployerContacting/Hired; Afgewezen = Rejected/FilledElsewhere/Withdrawn). Extract it into a small static helper, e.g. `ApplicationStatusWizard.FilterGroup(status)`, so it can be unit tested.

**Playwright:** a new `Jobsy.Tests/CandidateApplicationsPlaywrightTests.cs`, following the `MobileSmokePlaywrightTests` pattern (`JOBSY_E2E_BASE_URL`, soft-skip when unreachable). Run it on **mobile 390×844** and **desktop 1280×800**, and save screenshots to `artifacts/playwright-applications/`.
- **Seed data:** the e2e candidate needs applications in at least these statuses: Pending (×2, so one can be withdrawn), Accepted, Hired, Rejected. Check the existing dev/e2e seeder and add them there if they're missing (dev/e2e only, no public endpoint). Include one vacancy with a photo and one with only a company logo.
- **Counters filter:** read the three numbers. Tap Open → the visible card count equals the Open number and every pill is "Gesolliciteerd". Do the same for Lopend and Afgewezen. Tap the same counter again, or "Alles tonen" → all cards are back.
- **Menu actions:**
  - ⋯ opens the menu (`aria-expanded=true`); Esc closes it and focus returns.
  - "Vacature bekijken" navigates to `/vacancies/{id}`; go back.
  - "Lobsy-CV bekijken" triggers a download (`page.WaitForDownloadAsync`) or at least finishes without an error message.
  - "Sollicitatie intrekken" opens the confirm. Cancel → nothing changes. Confirm → the card's pill becomes "Ingetrokken", the Afgewezen counter goes up by 1, Open goes down by 1, and the `Apps.WithdrawnMsg` message shows.
  - The withdraw item is absent on a non-Pending card.
- **Hired card:** it has the class `application-card--hired`.
- **No overflow:** `document.documentElement.scrollWidth <= viewport width`, and every card fits in the viewport (with the menu closed and open).
- **No page errors:** collect `page.PageError` and console `error` events during the whole test and assert there are none (ignore only known third-party noise, if it's documented in the existing smoke tests).
- Run `dotnet build` and `dotnet test` (unit tests). Run Playwright where the stack is available and report the results in the PR.

## 6. Definition of done
- The page matches `docs/mockups/sollicitaties/sol-b-fotokaarten.png`, using real app images.
- Everything else keeps working as before: read-only mode, empty states, CV download, withdraw (now with a confirmation) and culture switching.
- **One PR** into `acceptatie`, titled "Mijn sollicitaties: fotokaarten (variant B)". The description lists the data change, the filter renaming (Gematched → Lopend), the new confirm step, the i18n keys, test results and screenshots, and includes the `docs/mockups/sollicitaties/` files.
- Not merged, not deployed, no shortcut `123`.
