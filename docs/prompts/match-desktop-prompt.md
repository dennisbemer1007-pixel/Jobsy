# Cursor prompt: Match on desktop — "Jouw top-match" tile + match dialog (variant B)

Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule `123` (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`.

## 0. Rules (read first)
- **Branch:** `git fetch origin && git checkout -b cursor/match-desktop origin/acceptatie`.
  - Open exactly **one** PR: `cursor/match-desktop` → `acceptatie`. Leave it open for review. Do not merge it, do not trigger a deploy, and do not run the `123` shortcut.
  - Code references below are from `origin/acceptatie` @ `e495b4b9` (2026-09-28). Re-check line numbers before editing.
- **Reference mockups** (Dennis chose variant B) are on branch `docs/match-desktop`, folder `docs/mockups/match-desktop/`. Read them with `git show origin/docs/match-desktop:docs/mockups/match-desktop/<file>`:
  - `md-b1-carrousel.png` / `.html`: desktop banenkaart at 1440×900. The first tile in the map-pane highlight carousel is **"Jouw top-match"** with a small coral `83%` badge on the thumbnail.
  - `md-b2-open.png` / `.html`: after clicking the tile. A centred dialog **"Jouw matches · 1 van N"** with a thin progress bar, the full swipe card, like/dislike with ←/→ hints, and a **"Hierna"** column (next 3 matches with their %). The page behind is dimmed by a light scrim.
  - The mockup HTML uses inline styles, raw hex values and a static map image. Do **not** copy that markup. Rebuild with the design-system tokens and existing classes.
  - Vacancies, percentages and texts in the mockups are **example data**. The "Voorbeelddata" labels are mockup-only; do not ship them.
- **Design system:** `.cursor/rules/design-system.mdc`. In particular:
  - Tokens only (`--brand`, `--surface`, `--bg`, `--muted`, `--border`, `--accent-soft`, `--success`, `--danger`, `--coral`, `--space-*`, `--text-*`, `--radius*`, `--shadow*`, `--z-*`). No new hex values, sizes, radii, shadows or z-indexes.
  - Font weights **400 and 600 only**.
  - **Coral at most once per screen.** In the closed state that is the tile's `%` badge. In the open dialog it is the card's match bar; the carousel badge is behind the scrim then and must not compete (see §3.4).
  - Logical properties only (RTL for `ar`). Visible `:focus-visible` outline in `--brand`. Tap targets ≥ 44 px.
  - No inline `style="…"`, no `!important`, no new `*-modal`/`*-btn` classes. Use BEM modifiers on existing blocks.
- **CSS:** new rules go in a new file `Jobsy.Web/wwwroot/css/features/match-desktop.css`. Link it in `Components/App.razor` next to the other feature sheets (see lines ~87–90), with `?v=20260929-match-desktop`. Do **not** append to `app.css` and do **not** change the existing mobile `.swipe-card*` / `.swipe-actions*` / `.match-page*` rules.
- **UI text:** every new string goes into `Jobsy.Web/Localization/UiStrings*.cs` for nl, en, pl, ro and ar, read via `@Culture["…"]`. No hardcoded Dutch in markup.
- **Mobile stays untouched.** `/candidate/match` (`MatchPage.razor`), the mobile toolbar Match button (`VacancyDiscovery.razor:132`, `a.jobsy-action--match`) and all mobile layouts must look and behave exactly as today.

## 1. What exists today (reuse it, don't duplicate it)
- **Desktop banenkaart:** `Jobsy.Web/Components/VacancyDiscovery.razor` (~3.5k lines). List pane `<aside>` left and `<section class="map-pane">` right (~line 520). `_wideViewport` (line ~901) is the existing desktop split-view flag, set from `jobsyViewport.isWide()` (`wwwroot/js/app-core.js:402`, `min-width: 769px`). `_isCandidate` is set at line ~1274.
- **Highlight carousel:** `Jobsy.Web/Components/HighlightVacancyCarousel.razor` (46 lines). Used twice in `VacancyDiscovery.razor`: `highlight-carousel--list` (~line 480) and `highlight-carousel--map` (~line 524, only after `_mapPainted`). The map one is absolutely positioned over the map (`app.css:3436`) and hidden while a cluster popup is open (`features/banenkaart.css:44`).
- **Match deck logic:** `Jobsy.Web/Components/Pages/Candidate/MatchPage.razor`.
  - Gate: `CandidateMatchProfileService.RefreshAsync()` → `MatchProfileGateViewModel` (`IsProfileComplete`, …).
  - Deck: `MatchVacancyService.GetRelevantSwipeVacanciesAsync(gate, take: 24)` → `SwipeViewModel.FromVacancy(item, showMatchPercentage: true)`.
  - Index/`Current`/`PeekNext`, `HandleInterest` → `Api.SetLikedAsync(id, liked: true)` (fire-and-forget, swallow errors), `HandleReject` → toast only (there is **no** dislike endpoint), `HandleMoreInfo` → `/vacancies/{id}`, `HandleSwipeComplete` → `_index++`.
  - Both services are already registered as scoped (`Program.cs:82–83`).
- **Swipe card:** `Jobsy.Web/Components/Match/SwipeCard.razor` (already a shared component). Pointer/touch drag, `SwipeAsync(direction)`, `OnReject`/`OnInterest`/`OnMoreInfo`/`OnSwipeComplete`, `IsBackdrop`. It contains hardcoded Dutch strings ("Laten schieten", "Snel kennismaken", "Waarom jij past", "Meer info", "Kerngegevens", "Match-acties", "Matchpercentage", "Bedrijf").
- **Like API:** `JobsyApiClient.SetLikedAsync` (`Services/ApiClient/JobsyApiClient.Vacancies.cs:516`).
- **Dialogs:** `LobsyFriendlyDialog.razor` has a mascot banner, no Esc handling and no focus trap, so it does not fit this layout. `MockInterviewModal.razor:9` shows the existing pattern of a `lobsy-dialog` with `role="dialog" aria-modal="true" tabindex="-1" @onkeydown`. There is no focus-trap helper in `wwwroot/js` yet.

## 2. Refactor first (no behaviour change)
1. **Extract the deck state out of `MatchPage.razor`** into one plain class, e.g. `Jobsy.Web/Services/MatchDeck.cs` (not a new Razor page, not a second copy of the logic):
   - `LoadAsync(CandidateMatchProfileService, MatchVacancyService, int take = 24, CancellationToken)` → sets `Gate`, `Items` (`IReadOnlyList<SwipeViewModel>`), `Index`.
   - `Current`, `PeekNext`, `UpNext(int count)`, `Count`, `Position` (1-based), `Advance()`, `ResumeAt(Guid vacancyId)`.
   - `LikeAsync(JobsyApiClient, SwipeViewModel)` wrapping the existing `SetLikedAsync(id, true)` with the same swallow-on-error behaviour.
   - `MatchPage.razor` uses `MatchDeck` for its deck, index, persist-state (`MatchPagePersistState` keeps working) and like call. Its markup, texts, toasts and routing stay identical.
2. **Localize `SwipeCard.razor`**: move its hardcoded strings to `UiStrings*.cs` keys under `Match.*` (e.g. `Match.Reject`, `Match.Interest`, `Match.WhyYouFit`, `Match.MoreInfo`, `Match.KeyFacts`, `Match.Actions`, `Match.Percentage`, `Match.CompanyFallback`). Dutch values stay exactly as they are now, so mobile looks the same.
3. **Add a `Variant` parameter to `SwipeCard`**: `SwipeCardVariant.Mobile` (default, current markup and classes) and `SwipeCardVariant.Dialog` (adds modifier class `swipe-card--dialog` on the article and `swipe-actions--dialog` on the actions). Also add a public `Task SwipeAsync(SwipeDirection)` entry point (or an `[Parameter] EventCallback`-friendly method via `@ref`) so the dialog can trigger the same animation from the keyboard. Do **not** create a second card component.

## 3. Build
### 3.1 Top-match tile (closed state, `md-b1-carrousel.png`)
- New component `Jobsy.Web/Components/Match/TopMatchTile.razor`. It renders a `<button type="button">` with the existing `highlight-carousel__card` classes plus the modifier `highlight-carousel__card--top-match`:
  - thumbnail via the same `VacancyPhoto` the carousel uses, with a small `%` badge (`highlight-carousel__match-badge`, `--coral` background, white text, weight 600, `--text-xs`),
  - eyebrow `@Culture["Match.TopMatch"]` ("Jouw top-match" / "Your top match"), in `--muted`,
  - title = job title, meta = company name · travel text (reuse the carousel's `FormatTravel`).
  - Border `--brand` (1 px) instead of `--border`, so it reads as "for you" without extra badges. No icons beyond the small cards glyph shown in the mockup (optional), no glow, no pulse.
  - Accessible name: `@Culture.Format("Match.TopMatchAria", pct, title, company)` (e.g. "Jouw top-match: 83% — Barista Voorhof, Zorg Delft Oost. Open je matches"). Add `aria-haspopup="dialog"` and `aria-expanded`.
- **`HighlightVacancyCarousel.razor`**: add an optional `[Parameter] RenderFragment? LeadingItem`. When set, render it as the first child inside `highlight-carousel__track` (wrapped with `role="listitem"`). Also render the carousel when `Vacancies.Count == 0` but `LeadingItem` is set.
- **`VacancyDiscovery.razor`**: pass `LeadingItem` **only** to the `highlight-carousel--map` instance, and only when all of these hold:
  - `_wideViewport` (desktop split view; do not add a new breakpoint),
  - `_isCandidate`,
  - the deck has loaded, `Gate.IsProfileComplete`, and `Deck.Count > 0`.
- **Hide the tile** (render nothing, no placeholder, no empty state) when the user is not a candidate, the profile gate is incomplete, the deck is empty or failed to load, or the viewport is not wide. The list-pane carousel and mobile never get the tile.
- **Loading:** load the deck lazily **after** `_mapPainted` and only when `_wideViewport && _isCandidate`, once per circuit. Keep the `MatchDeck` instance on `VacancyDiscovery` and never block map or list rendering. Errors are swallowed (the tile simply doesn't appear).
- **Which card is "top":** the tile shows `Deck.Items[0]`, and the dialog starts at the same card. Check how `DiscoverVacanciesAsync` orders results. If the deck is not already ordered by `MatchPercentage` descending, order the **desktop** deck by `MatchPercentage` desc (stable, nulls last) inside `MatchDeck` via an option (e.g. `orderByMatch: true`) that `MatchPage` does **not** use. Mobile order stays unchanged.

### 3.2 Match dialog (open state, `md-b2-open.png`)
- New component `Jobsy.Web/Components/Match/MatchDeckDialog.razor`. It takes the `MatchDeck`, `IsOpen` and `OnClose` as parameters and has no data loading of its own.
- Markup, reusing `lobsy-dialog` as the base with the BEM modifier `lobsy-dialog--match`:
  - scrim with the existing backdrop classes `share-modal-backdrop lobsy-dialog-backdrop` (as used by `LobsyFriendlyDialog`), `z-index: var(--z-overlay)`, light navy tint. Clicking it closes.
  - `role="dialog" aria-modal="true" aria-labelledby="match-dialog-title" aria-describedby="match-dialog-progress"`, `tabindex="-1"`.
  - **Main column:**
    - header `<h2 id="match-dialog-title">@Culture["Match.DialogTitle"]</h2>` ("Jouw matches" / "Your matches"), then `@Culture.Format("Match.DialogProgress", position, count)` ("1 van 12" / "1 of 12") with `id="match-dialog-progress"` and `aria-live="polite"`. Close icon button (`icon-btn`, `aria-label=@Culture["Common.Close"]`).
    - A progress bar (`role="progressbar"`, `aria-valuemin=1`, `aria-valuemax=count`, `aria-valuenow=position`, 4 px, `--border` track, `--brand` fill).
    - `SwipeCard Variant="Dialog"` with `PeekNext` as `IsBackdrop` exactly like `MatchPage` does. Keep drag working with the mouse.
    - Under the action buttons: keyboard hints `<kbd>←</kbd>` / `<kbd>→</kbd>` next to the existing reject/interest labels.
  - **"Hierna" column** (≈ 300 px, `--bg` background): heading `@Culture["Match.UpNext"]` ("Hierna" / "Up next"), then `Deck.UpNext(3)` as compact rows (thumbnail 40 px, title, company, `%` in `--success`, weight 600). Rows are plain `<button>`s; clicking one jumps the deck to that card (`ResumeAt`). Footer note `@Culture["Match.DialogFootnote"]` ("Bewaarde matches vind je terug onder Bewaard.") in `--muted`, `--text-xs`.
  - Width ≈ 760 px (main ≈ 460 + side ≈ 300), max-height `calc(100dvh - var(--app-header-height) - 2*var(--space-6))`, and the main column scrolls internally if needed. Centred in the viewport. Radius `--radius`, `--shadow-lg`.
- **Dialog variant styling** (`match-desktop.css`, scoped to `.swipe-card--dialog` / `.swipe-actions--dialog`):
  - match bar: keep the existing gradient look but use weight 600 for label and value, no `match-hero-pulse` animation, and no text-shadow;
  - title `--text-xl`/600 in `--brand`; tags as `kompas-chip`-style pills (max 2 visible); "Waarom jij past" block in `--success-soft` with `--success` label;
  - reject = outline circle in `--danger`, interest = filled `--success` circle, 56 px;
  - wrap all animation in `@media (prefers-reduced-motion: reduce)` fallbacks (no drag rotation, instant swap).
- **Behaviour:**
  - **Keyboard:** `ArrowLeft` = reject, `ArrowRight` = interest (both call the card's `SwipeAsync`, so the animation, stamps and callbacks are identical to a click/drag). Ignore repeats while animating. `Escape` closes. Ignore arrows when focus is in an input/select.
  - **Focus trap:** on open, move focus to the dialog's heading or first action. `Tab`/`Shift+Tab` cycle inside the dialog. Add one small reusable JS helper (e.g. `window.jobsyDialog.trap(el)` / `release()` in `wwwroot/js/app-core.js`, disposable via Blazor interop) instead of ad-hoc code. Background content gets `inert` (or `aria-hidden="true"`) while open.
  - **Close** (Esc, close button, scrim, or deck finished) returns focus to the **top-match tile** (`ElementReference` + `FocusAsync()`).
  - **Like/dislike reuse the existing API:** interest → `MatchDeck.LikeAsync` (= `SetLikedAsync(id, true)`); reject → local skip only, same as `MatchPage` (no new endpoint). Show the same short status text as mobile (`role="status"`, localized: "Interesse genoteerd" / "Overgeslagen").
  - "Meer info" opens `/vacancies/{id}` like `MatchPage.HandleMoreInfo`.
  - **End of deck:** show a calm inline empty state inside the dialog (`Match.DeckDone`: "Je hebt alle matches gezien" + a close button). After closing, the tile shows the next remaining card or hides if none are left.
  - **State preserved:** opening/closing the dialog must not reload discovery, reset filters, sort, the list scroll position, the carousel scroll position, the selected/hovered vacancy, or the map camera/rings. The deck position is kept while the circuit lives, so reopening continues where the user left off. Do not call `jobMap.invalidate` or re-fit the map on open/close.

### 3.3 Things not to do
- No new route, no change to `/candidate/match`, no change to the mobile toolbar button, no Match entry in the list pane.
- No second swipe-card component and no copy of the deck logic in `VacancyDiscovery`.
- Don't use `LobsyFriendlyDialog` for this (mascot banner doesn't fit). Don't create a new `*-modal` block.
- Don't add extra badges, icons, glows, celebratory popups, or a banner advertising Match.

### 3.4 Coral check
- Closed: the only coral on screen is the tile's `%` badge (the list cards use the green `%` pill, which is fine).
- Open: the only coral inside the dialog is the card's match bar. The carousel badge sits behind the scrim; also give the tile `is-open` while the dialog is open and render the badge in `--muted` in that state, so there is never more than one coral element visible.

## 4. Tests
- **bUnit** (`Jobsy.Tests`, pattern like `CandidateInsightsBunitTests.cs`):
  - `TopMatchTile` renders the eyebrow, title, company and `83%` badge, and exposes `aria-haspopup="dialog"`.
  - `HighlightVacancyCarousel` renders `LeadingItem` as the first list item, and renders nothing when there are no vacancies and no leading item.
  - `MatchDeckDialog`:
    - `ArrowRight` advances the deck and calls the like callback once.
    - `ArrowLeft` advances without liking.
    - `Escape` raises `OnClose`.
    - Progress text updates ("2 van 12").
    - "Hierna" shows exactly the next 3 items.
    - The end-of-deck state appears after the last card.
  - `SwipeCard` in `Variant.Mobile` renders the same Dutch labels as before (regression guard for the localization move).
- **Unit** (`MatchDeck`): load/advance/`UpNext`/`ResumeAt`; `orderByMatch` orders desc with nulls last and is off by default; `LikeAsync` swallows API errors.
- **Visibility rule:** a test (bUnit or a small pure helper, e.g. `TopMatchTileVisibility.ShouldShow(isWide, isCandidate, gateComplete, count)`) covering every hide case from §3.1.
- **Localization:** every new `Match.*` key exists in nl, en, pl, ro and ar (follow the existing UiStrings completeness test if there is one; otherwise add one for these keys).
- **Existing tests stay green:**
  - `BanenkaartPersistSizePlaywrightTests.cs` asserts on `swipe-actions__btn--interest` and the mobile Match flow. Keep that class name.
  - Run `dotnet test` and fix anything you break.
- **Optional E2E:** Playwright at 1280×800 and 1440×900 as `kandidaat@jobsy.local`:
  - the tile appears in the map carousel;
  - clicking it opens the dialog;
  - `→` advances;
  - `Esc` closes and focus is back on the tile;
  - at 390×844 the tile is absent.
  - Soft-skip like the existing E2E tests when no base URL is configured.

## 5. PR
- Title: `feat(match): desktop top-match tile + match dialog on banenkaart`.
- Description:
  - a short summary;
  - a list of changed/new files;
  - the refactor note (`MatchDeck` extracted, `SwipeCard` localized and given a `Variant`, no mobile change);
  - how the "top" card is chosen (§3.1);
  - screenshots at 1440×900 of the closed state, open state, end-of-deck state and the keyboard focus ring, plus one 390×844 screenshot proving mobile is unchanged. Compare them with `md-b1-carrousel.png` / `md-b2-open.png`.
- **One PR into `acceptatie`. Do not merge. Do not deploy. Do not use rule `123`.**

## 6. Done when
- On desktop (≥ the existing split-view width) a candidate with a complete match profile and ≥1 match sees "Jouw top-match" as the first tile of the map carousel with the `%` badge. Everyone else sees no tile.
- Clicking the tile opens the centred dialog. ←/→, Esc, focus trap and focus return work, and like uses `SetLikedAsync`.
- Closing leaves the map, rings, carousel, filters, sort and list exactly as they were.
- `/candidate/match` and all mobile screens are pixel- and behaviour-identical to before.
- Tokens only, weights 400/600, at most one coral element visible, all strings in 5 languages, and tests are green.
