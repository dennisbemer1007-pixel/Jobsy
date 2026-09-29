# 06. Gratis test `/ontdek` in the warm style: start, question, result, sign-up sheet, one cookie banner

Read `00-README.md` first. Branch `cursor/landing-6` from `cursor/landing-5` (or `-5b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Restyle only.** The 20 questions, scoring, the `jobsy.gratisDna.v1` storage (7 days), the merge and every `data-testid` stay as they are.

| | |
|---|---|
| Branch | `cursor/landing-6` |
| PR title | `feat(gratis-dna): warm public theme for /ontdek — start, question, result, sign-up sheet; sticky CTA vs cookie banner` |
| PR body starts with | `Stacked on #<PR 05> (cursor/landing-5)` |
| Mockups | `lp-d2-test-start.png`, `lp-d3-test-vraag.png`, `lp-d4-test-resultaat.png`, `lp-m2-test-vraag.png`, `lp-m3-resultaat.png`; sources `w_test.py`, `css_warm.py`, `css_lp.py` |
| Split seam | **06a** = layout switch + start + question screens (06.2, 06.3). **06b** = result + sign-up card/sheet + sticky/cookie rules (06.4, 06.5) |

## Goal
The test looks and feels like the landing page: friendly, calm, clearly private, with the mascot along for the ride. At the end, one clear step: make a free account with Google, Microsoft or e-mail. On mobile, nothing fixed at the bottom ever fights the cookie banner.

## 06.1 Today (verify first)
- `Pages/Public/GratisDna.razor` (`/ontdek`, `/dna`; `@layout TeaserLayout`; `InteractiveServerRenderMode(prerender: true)`): landing step (16+ check + privacy consent), 20 questions with Likert, result, `GratisDnaUnder16View`, `wwwroot/js/gratis-dna.js`, `features/gratis-dna.css` (`gd-…`).
- `Components/Public/GratisDnaResultView.razor`: `gd-result`, `gd-result-badge`, `gd-tiles` + `gd-tile-riasec|strength|culture|value`, `gd-jobs-teaser`, `gd-share` (3 buttons), `gd-wipe` / `gd-wipe-link`, `gd-sticky` + `gd-sticky-cta` (dismissable); `RegisterHref` = `/account-maken?van=ontdek` since 03.
- `GratisDnaBunitTests`, `GratisDnaPlaywrightTests`, `GratisDnaI18nTests`, `GratisDnaScoringTests`, `GratisDnaStorageValidatorTests`, `GratisDnaMerge*Tests`.
- `UiStringsGratisDna.cs` (nl/en full; pl/ro/ar fall back to en, fixed in 09).

## 06.2 Layout
- `GratisDna.razor` → `@layout PublicLayout` (it stays interactive: `PublicLayout` renders the **Interactive** cookie-banner mode here, 01.5). The header is the compact test header from lp-d2: logo → "/", a "🧪 Gratis test" chip, language menu, "Inloggen". No full nav on the test (fewer exits).
- `FeedbackWidget` stays available on `/ontdek` (it was on TeaserLayout): add it to the page, not to `PublicLayout`.
- `PageSeoCatalog["/ontdek"]`: `Hreflang = true`. Canonical `/ontdek` (also for `/dna`).
- Restyle in `features/gratis-dna.css` scoped under `.pub-theme` (keep the `gd-` BEM names; add `gd-…--warm` modifiers only where the structure changes). Bump its `?v=`.

## 06.3 Start + question screens
- **Start (lp-d2):** a card with the chip "👋 Gratis · geen account nodig", h1 "Ontdek je werk-DNA", lead, the meta row (📝 20 vragen · ⏱️ ± 3 minuten · 📱 Blijft op dit apparaat), the 4 blocks (Zo werk jij · Dit vind je leuk · Hier voel je je thuis · Dit vind je belangrijk, "5 vragen" each), the existing 16+ and privacy checkboxes (copy per mockup: "…7 dagen op dit apparaat… Werkgevers zien ze nooit. Meer over privacy"), "Start de test", "Al een account? Inloggen". Side scene: `LobsyMascot Waving Large` + bubble "Duik maar diep! 🌊 Je kunt altijd terug." + the "🎁 Wat krijg je na de test?" card.
  - `?voor=nieuw` (from the landing chips): one extra line under the lead, "Liever in je eigen taal? Kies je taal ↗" (opens the language menu). Other `voor` values change nothing visible (KPI only, 10).
- **Question (lp-d3 / lp-m2):**
  - top bar: "Vorige" · "Vraag 8 van 20 · goed bezig! 💪" (encouragement from a small rotating key set, never random per render: index = question number) · "Later verder" (answers stay stored; → "/")
  - the 4-block rail with the current block highlighted
  - the card: block label + "Vraag 3 van 5", the question text, 5 Likert buttons with faces 😟😕😐🙂😄 **and** the digits 1–5 (the face is `aria-hidden`; the button's accessible name is the digit + "Past niet" … "Past wel")
  - hint "Twijfel je? Kies wat je het eerst voelt…", privacy line "🔒 Je antwoorden blijven op dit apparaat. Werkgevers zien ze nooit."
  - Mobile: answer buttons ≥ 56 px high, full width grid as in lp-m2.
- Keyboard: 1–5 keys answer (if `gratis-dna.js` already supports it, keep; else add), ← = Vorige. Focus moves to the new question heading.

## 06.4 Result + sign-up (lp-d4 / lp-m3)
- Head: "Eerste indruk · op basis van 20 vragen", h1 "Dit ben jij 🎉", lead "Wauw, wat een mooi begin!…". `LobsyMascot Celebrating Medium`.
- 4 result tiles (existing `gd-tile-*` data, new look: emoji, title, the result word big, one sentence, 2 chips).
- **Locked passport teaser:** a blurred passport card with tabs Mijn DNA · Mijn tests · Past deze baan? · Carrière · Bewijzen and the pill "🔐 Je volledige paspoort · na gratis account". It's decorative (`aria-hidden`, the pill text is readable). 07 changes the tab label when OFF.
- Row: share pills (existing 3), "Scherper beeld? In je account staan de volgende tests klaar." (→ sign-up, README §0 difference), "🧹 Wis mijn antwoorden" (existing `gd-wipe`).
- **Sign-up card** (`GratisDnaSignupCard`, new component, shared with `/account-maken` markup where sensible): title "Bewaar je DNA en zie je hele paspoort 🎁", sub "Gratis. Je tests in je account zijn dan al klaar.", unlock list (ON: paspoort · "Past deze baan?" bij elke vacature · banen op je reistijd-kaart · top-match in Match; 07 swaps the list for OFF), buttons **Doorgaan met Google** → `/account/external/google?returnUrl=…&van=ontdek`, **Doorgaan met Microsoft** → `/account/external/entra?…`, **Doorgaan met e-mail** → `/account-maken?van=ontdek#email` (focuses the e-mail field), note "Je 20 antwoorden gaan mee", "Al een account? Inloggen". Hide unconfigured providers (same check as 03). `data-kpi="ResultCtaSignup"` + `data-kpi-target=google|microsoft|email`.
  - Provider sign-ups from here must still end in the GratisDna merge: the `returnUrl` goes through `ResolveCandidateReturnUrl`, and the merge runs in `MainLayout` on the first signed-in page. E2E test for Google/Microsoft asserts the redirect target; the e-mail path is tested end-to-end (03).
- The existing `gd-jobs-teaser` stays (ON) with the warm style; 07 hides it OFF.
- **Mobile (lp-m3):** the sign-up card sits inline after the tiles. The sticky bar `gd-sticky` ("Bewaar je DNA · Maak gratis account", dismiss "Later") becomes a `pub-fixed-bottom` element. Tapping it opens a **bottom sheet** (`<dialog>`, focus-trapped, Esc/"Sluiten") with the same three buttons.

## 06.5 One cookie banner + sticky rules (D15)
- `/ontdek` shows exactly **one** `.cookie-consent` (from `PublicLayout`); `GratisDna.razor` and its children render none.
- `gd-sticky` and the sheet trigger carry `pub-fixed-bottom`: hidden while `html:not(.cookie-consent-known)`; they appear right after a choice (the banner code adds the class; add a tiny listener so the sticky appears without reload).
- The banner never overlaps the question's answer buttons at 360×640: if it's visible on a question screen, the page gets bottom padding equal to the banner height (CSS var set by the banner script), as `BanenkaartCookiePaddingPlaywrightTests` does for the map.
- The sticky never covers the sign-up card: hide it while the inline card is in view (IntersectionObserver in `gratis-dna.js`).

## Tests
- All existing GratisDna tests stay green; the only allowed changes are selectors that moved inside the new markup (keep `data-testid`s so this should be near zero).
- bUnit: start screen (checkboxes required, `voor=nieuw` line), question screen (Likert accessible names "1 – Past niet" … "5 – Past wel", faces `aria-hidden`), result (4 tiles, locked teaser `aria-hidden`, sign-up card hrefs, no `/register`), exactly one `.cookie-consent`.
- Playwright (desktop 1440 + mobile 390): full run start → 20 answers → result; screenshots of all three screens; on mobile with no consent yet: `gd-sticky` hidden, banner visible, answer buttons not covered; after "Alleen noodzakelijk": sticky visible; the sheet opens/closes with the keyboard; keys 1–5 answer.
- `LocalizationParityReportTests`, `AssetVersionGuardTests`, `PublicThemeCssGuardTests`.

## Success criteria
- `/ontdek` matches lp-d2/d3/d4 and lp-m2/m3 within the spec's differences, in nl and en.
- A new visitor can't end up on `/register` from any test screen.
- On a 360×640 phone the cookie banner and the sticky CTA are never on screen at the same time.
- Scoring output for a fixed answer set is identical before and after (existing `GratisDnaScoringTests`).

Done → next: `07-landing-zonder-werkgevers.md`.
