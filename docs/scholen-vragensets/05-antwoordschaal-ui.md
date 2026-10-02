# 05. Answer scale per set: `LeerlingAnswerScale` (VO labels without smileys, G78 unchanged)

Read `00-README.md` first (§S, D2, K2). Branch `cursor/vragensets-5` from `cursor/vragensets-4`.

> **Rules (same as README §0):**
> - Never merge, deploy or use rule `123`.
> - Never push to `main` or `acceptatie`; push only `cursor/vragensets-5`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red or an unmet criterion → draft PR, stop, report.
> - Release build with 0 warnings.
> - No `.github/workflows` changes.

| | |
|---|---|
| Branch | `cursor/vragensets-5` |
| PR title | `feat(scholen): set-aware answer scale — VO "Klopt niet … Klopt helemaal" (no smileys), real radio semantics` |
| PR body starts with | `Stacked on #<PR 04> (cursor/vragensets-4)` |
| Reference | print booklet layout for VO: 5 equal boxes with the label under/next to an empty square, no faces (Dennis approved the VO booklet on 2 Oct). The G78 look stays as today (K2). |

## 05.1 Today (verify)
- **Markup:** `LeerlingReis.razor` L97–109 renders `<div class="ll-answers" role="radiogroup">` with `<button class="ll-answer" aria-pressed>` + `<span class="ll-answer__dot">` + the label.
  - `aria-pressed` buttons inside a `radiogroup` is a semantics mismatch.
  - Keys 1–5 are handled on the wrapper (`OnKey`).
- **CSS:** `scholen.css` ~L899–935:
  - one column on mobile, five columns from 900 px
  - `min-height: 56px`
  - the selected state is the navy fill + gold dot

## 05.2 Component `Jobsy.Web/Components/Leerling/LeerlingAnswerScale.razor`
- **Parameters:**
  - `IReadOnlyList<(int Value, string Key)> Labels` (from `def.AnswerLabels`)
  - `PupilQuestionSet Set`
  - `int? Selected`
  - `EventCallback<int> OnSelect`
  - `string GroupLabel` (`Leerling.Reis.Answers`)
  - `bool Disabled`
- **Semantics:**
  - `role="radiogroup"` with `aria-label`
  - each option is `<button type="button" role="radio" aria-checked="true|false">`
  - roving `tabindex`: the selected option, or the first one when none is selected, is `0`; the others are `-1`
  - **Arrow keys** move focus and select (WAI-ARIA radio pattern); Home/End go to the first/last option
  - Keys **1–5** keep working (move the handler from the page wrapper into the component, so it isn't fired twice)
  - **Accessible name** per option is "{label}, {n} van 5"; the "n van 5" part is visually hidden
- **Look per set:**
  - **G78:** **exactly today's look** (dot + label, navy selected, same sizes). Put a pixel snapshot/screenshot in the PR to show nothing changed.
  - **VO** (`ll-answer--vo`):
    - an empty **square box** (≈ 20 px, 2 px border `--brand`) before the label; selected = a filled box with a check mark (inline SVG, `aria-hidden`) + the navy card
    - **no emoji, no faces, no colour scale** (the scale is neutral, like the booklet)
    - the label is left-aligned and wraps; it never truncates ("Klopt meestal niet" may take 2 lines on desktop)
    - mobile: a vertical list, each option full width, `min-height: 56px`; desktop ≥ 900 px: 5 equal columns, `min-height: 72px`, text centred under the box
- **Tap targets ≥ 44 px** everywhere (they are 56/72 px). Visible focus ring (`outline: 3px solid var(--brand)`, as today). Contrast AA in both states.
- **`prefers-reduced-motion`:** no transition on select.
- **Use it** in `LeerlingReis.razor` (replace L97–109 and the page-level `OnKey` 1–5). Nothing else changes in the page.

## 05.3 Strings
- No new texts (the labels come from 04).
- Add `Leerling.Reis.AnswerPosition` = "{0} van {1}" for the hidden part.

## Tests
- **bUnit (`LeerlingAnswerScaleTests`):**
  - G78 renders the 5 G78 labels with `ll-answer__dot` and no `ll-answer--vo`
  - VO renders the 5 VO labels with boxes, no emoji (assert there are no characters in the emoji ranges), and no `ll-answer__dot`
  - `role="radio"` + `aria-checked` on the selected option, exactly one `tabindex=0`
  - arrow right/left move the selection; Home/End work
  - key "4" selects value 4
  - `OnSelect` fires once per key press (no double handler)
- **Guard:** update `PupilPagesNoCandidateChromeTests` (AnswerLabels now per set: 5 labels, values 1..5, for **every** registered set).
- **Playwright** comes later (file 12): VO and G78 question screens at 360, 390 and 1366 widths, no horizontal scroll, labels fully visible.

## Success criteria
- **VO:** pupils see "Klopt niet / Klopt meestal niet / Klopt deels / Klopt meestal / Klopt helemaal" with boxes and no smileys, readable at 360 px.
- **G78:** looks exactly as before.
- **Both sets** are a real radio group (screen reader + keyboard), and keys 1–5 still work.

Done → next: `06-droombaan.md`.
