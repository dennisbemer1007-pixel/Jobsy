# 03 · Mijn Paspoort, Phase 2: tab Mijn tests + course data model + "Groei verder"

> Read `00-README.md` first: §0 rules apply, plus the §F gate helpers where needed. Execute this file only after the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/mijn-paspoort-2`, created from `cursor/mijn-paspoort-1` |
| PR | ONE PR into `acceptatie`, title `feat(paspoort): Mijn tests tab and course suggestions`. Body starts with `Stacked on #<PR of 02> (cursor/mijn-paspoort-1)` |
| Mockups | `pp-d2-mijn-tests.png`, `pp-m2-mijn-tests.png` |

## 03.1 Test list with depth
- **Data:** extract the loading and mapping from `TestsOverviewPanel.razor` (L56–150: kompas result + 4 test states + 4 deep states) into `TestsOverviewBuilder` (pure), used by both `TestsOverviewPanel` and the new `PassportTestsTab`. No extra API calls: reuse the shared kompas state when present.
- **Card header strip:** "Oppervlakte → Diep" as 3 **stepped** segments (`--accent-soft`, `--accent`, `--brand`). No gradient.
- **Rows:** Competentie, Beroepen, Cultuur, Waarden.
  - Icon, name, one-line question ("Wat kun jij goed?" etc.).
  - Mini progress with 3 segments: Quick-Scan (light) · Uitgebreid (medium) · Rapport (dark). Done = filled; in progress = partly filled by answered / total; locked report = gold outline (`--gold-soft` / `--gold-light`).
  - **Quota line under the bars:**
    - Uses the testresultaten rules and strings `TestResult.Quota.Line` / `LineOne` / `Zero`, from `GetAssessmentAdjustmentsAsync` (`AssessmentAdjustmentState`), with 3 dots.
    - At 0 it uses the `--warn-soft` note style from testresultaten.
    - A not-finished Quick-Scan shows "Nog {x} van {y} vragen" instead.
  - **One action per row:**
    - completed extended → "Rapport" (the existing report/PDF link)
    - in progress → "Ga verder" (primary)
    - Quick-Scan done → "Uitgebreid" (secondary; it goes to the existing deep-analysis start, and price/checkout are untouched)
    - not started → "Start" (primary)
  - The selected row (the latest activity) is highlighted with `--accent-soft`.
- **"Wat kost uitgebreid?"** is a quiet link to the existing explanation. No new prices.

## 03.2 Locked report card (right column)
- Show the **testresultaten locked cards** for the test with the most relevant next step (Beroepen in the mockup). Extract the card grid + gold block from `TestDetail.razor` into a reusable `TestResultLockedPreview` if it isn't a component yet, and use it in both places.
- **All testresultaten honesty rules apply unchanged** (card gating §4.3, client-side placeholder data, the "Voorbeelddata" stamp per card, no server data, checkmarks only for cards that are shown, one gold CTA).
- If the candidate has already done every extended test, the right column shows "Download je rapporten" (the existing PDFs) instead.

## 03.3 Course data model (reused in 04)
Extend the **existing** training model (`Core/Entities/TrainingEntities.cs`, `TrainingUpskillService`, `TrainingUpskillController`, `Pages/Admin/TrainingAdmin.razor`, `TrainingOffersBlock.razor`). **Don't add a parallel model.**
- **`TrainingOffer`** gets:
  - `TrainingOfferType Type`: `Opleiding | Cursus | Workshop`
  - `int? DurationValue` + `TrainingDurationUnit? DurationUnit` (`Hours | Days | Weeks | Months | Years`), localized when rendered
  - `TrainingDeliveryMode Delivery`: `Online | OnSite | Blended` + `string? Location`
  - `bool IsFree`
  - `bool IsPartner`
  - `string? AffiliateCode`
  - `bool ShowInPassport` (curation, default **false**)
- **Migration defaults:** everything false/null for existing rows, so **nothing existing appears in the paspoort** until an admin curates it.
- **Admin:** `TrainingAdmin.razor` gets the fields. Validation: `IsPartner` requires an `AffiliateCode` and a valid course deep link (`TrainingDeepLinkRules.IsCourseDeepLink`); `IsFree` and `IsPartner` can't both be true.
- **Slot rule** `CourseSlotRules.Pick(offers, context)` (Core/Rules, pure, unit-tested):
  - Only `IsActive && Provider.IsActive && ShowInPassport` offers, matched with the existing `TrainingMatchRules` on keys/fields for the context.
  - **Slot 1 = the best free offer.** **Slot 2 = the best partner offer.** Max 2.
  - **No free match → the block is hidden** (candidate first, D5).
  - Paid non-partner offers never show in the paspoort block.
- **Links:**
  - Every click goes through the existing tracked outbound `TrackAsync`.
  - `AffiliateCode` is appended there. `TrainingTracking.AppendParameters` gets an optional code; add a test.
  - Partner links get `rel="sponsored noopener noreferrer" target="_blank"`. Free links get `rel="noopener"`.
- **Card component** `CourseSuggestionBlock.razor` (shared by 03 and 04), with an eyebrow + 2 option cards:
  - line 1: pill **"Gratis"** (`--success-soft`/`--success`, check icon) or **"Partnerlink"** (neutral outline) + title
  - line 2: "{Type} · {duur} · {online | plaats} · {aanbieder}"
  - line 3, the "waarom": the free option uses a claw icon + "Laat je klauw ‘{skill}’ groeien"; the partner option uses a check icon + a context reason
  - Block note (always visible when a partner option is shown): **"Partnerlink: Lobsy kan een vergoeding krijgen."**
- **Production data:** ship **no** seeded courses for this block. No fake providers in any environment. File 01b already stopped the demo seeds; don't reintroduce any. The example providers in the mockups (LeerPlein, ZorgStart, Voorbeeld-…) exist only in test fixtures. With no curated data the block simply doesn't render.

## 03.4 "Groei verder" (in Mijn tests)
- Under the row of the test the suggestion is based on. Default: Competentietest, when completed.
- The context is the **lowest competence** (from the existing scores) mapped to training keys through the existing `KeysCsv` matching. The `{skill}` label is the localized competence name.
- Eyebrow: "Groei verder". Max 2 options. Hidden when there is no curated free match.

## Tests
- **Builder:** `TestsOverviewBuilder` parity with the old panel.
- **Quota:** the line uses the `TestResult.Quota.*` strings with the correct counts (3/2/1/0 and the Zero state).
- **Action mapping:** the per-row action for each status.
- **Locked cards:** the testresultaten "no deep-report data for non-payers" DTO test still passes; the preview renders only gated cards.
- **Model and migration:** the new fields; existing rows stay `ShowInPassport = false`.
- **`CourseSlotRules`:**
  - free first; max 2
  - no free → empty
  - partner without affiliate code → excluded
  - paid non-partner → excluded
  - both flags true → invalid
- **Tracking:** the affiliate code is appended; `rel` attributes are correct.
- **bUnit:** `CourseSuggestionBlock` shows "Gratis" first, "Partnerlink" second and the disclosure note; nothing is rendered with an empty list.
- **Admin validation.**
- **Playwright:** the tab `tests` at 1440×900 fits the screen (panel bottom ≤ 900 with no curated courses, and with 2 fixture courses on the test DB); 390×844 row layout.

## Success criteria (all must hold before you open the PR)
- The Mijn tests tab replaces the Phase 1 placeholder. `TestsOverviewPanel` and the new tab share `TestsOverviewBuilder`; the old panel's markup is unchanged.
- **Quota line and locked cards:**
  - The quota line uses the testresultaten strings and counts.
  - The locked report preview obeys every testresultaten honesty rule, and the non-payer DTO test is still green.
- **Course model:**
  - The fields exist; existing offers stay `ShowInPassport = false`.
  - `CourseSlotRules` puts the free option first, max 2, and nothing without a free match.
  - Partner links carry `rel="sponsored"` and the affiliate code, plus the "Partnerlink: Lobsy kan een vergoeding krijgen." note.
- **Production:** with no curated data, no course block shows anywhere.
- **Layout:** the tab fits 1440×900.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has: stacked-on line, what and why, screenshots (where there is UI), test list, "Out of scope / deferred".

## Done → next
Push, open the PR, note its number. Then continue with **`04-past-deze-baan-carriere.md`**. If anything above is red, stop and report (see 00-README "How to run" step 3).
