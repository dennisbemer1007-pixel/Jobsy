# 05: /candidate/hoe-werkt-lobsy: "five stones" in the journey style + H1–H3

> **Rules (repeated in every file):**
> - Branch from `origin/acceptatie` (file 01) or from the previous file's branch (stacked). ONE PR per file, always into `acceptatie`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`). Never push to `main` or `acceptatie`. No force-pushes. Push only `cursor/carriere-*` branches.
> - Red build/tests or an unmet success criterion: push, open that PR as **draft**, stop and report. Don't start the next file.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/carriere-5` from `cursor/carriere-4` |
| PR title | `Carrière 05: Hoe werkt Lobsy as five stones; fix _message, role guard and outdated copy` |
| Body starts with | `Stacked on #<PR 04> (cursor/carriere-4)` + re-check outcome A, D |
| Mockups | `cr-d8-hoe-werkt-lobsy`, `cr-m7-hoe-werkt-lobsy` |
| Split if too big | not expected; if needed `05a` = §1 fixes, `05b` = §2–§4 restyle |

**Goal:** a new candidate understands Lobsy in 20 seconds. Five stones, in their own pace, in Dennis' order: De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties. Each stone shows whether it's done and links there. The lobster stands on the stone where you are now. The page stops lying (the "_message" literal, old question counts, wrong roles).

Closes: H1, H2, H3, B14 (this page).

## 1. Fixes (also for the shared panel)
- **H1:** `Message="_message"` → `Message="@_message"`.
- **H2:**
  - On error, stay on the page and show `HowC.Err.Save` ("Dat lukte niet. Probeer het straks opnieuw.") in a `role="alert"` line. Only navigate after success. Never `ex.Message`.
  - Page roles: `Authorize(Roles = "Candidate")`.
  - Other roles that reach `/candidate/hoe-werkt-lobsy` get the standard access flow. To be kind, `RoleNavCatalog.HowLobsyHrefFor` already sends them to `/hoe-werkt-lobsy`; verify with `git grep`, don't edit `RoleNavCatalog`.
  - Add a tiny `/candidate/hoe-werkt-lobsy` → `/hoe-werkt-lobsy` redirect for non-candidates only if an existing link would otherwise dead-end (check `HowLobsyHrefFor` and `AuthRedirects.CandidateHowToGuidePath` users first; report what you found).
  - `BlazorPageRoleAttributesTests` updated.
- **H3 (shared):**
  - `HowLobsyGuidePanel.razor`: remove the inline `style` on the primary `<a>` (use the existing button class, e.g. `btn btn--primary`, or a class in `carriere.css`).
  - Fix the typo "Lik" and the question-count mismatch in `UiStrings.cs` `HowLobsy.*` for **all 5 languages**. The guest/role guides (`/hoe-werkt-lobsy`) keep their structure (D15). Only these copy fixes: the same facts in every language, no numbers that can go stale ("korte vragenlijsten" instead of "25 + 25 vragen"). List the changed keys in the PR.

## 2. Candidate page (cr-d8 / cr-m7)
- `Candidate/HowLobsyWorks.razor` no longer uses `HowLobsyGuidePanel`. It renders the journey shell (`journey-page career-page`, `CareerStage` with the `Stones` scene variant):
  - Eyebrow "Hoe werkt Lobsy?", h1 **"Vijf stenen, in je eigen tempo"** (fewer stones ⇒ "{n} stenen, in je eigen tempo"), lead "Je hoeft niet alles tegelijk. Begin waar je wilt. Alles wordt bewaard."
  - `<ol aria-label="Zo werkt Lobsy">` with one row per stone (number or check, title, one line, state, link):
    1. **De ontdekkingsreis:** "Ontdek wie je bent. 10 korte stappen." · done → "Kijk terug", else "Begin" / "Ga verder"
    2. **Mijn Paspoort:** "Alles over jou op één plek. Jij kiest wie het ziet." · "Open"
    3. **Carrière:** "Kies je droombaan en groei steen voor steen." · "Ga verder" / "Begin" → `/carriere`
    4. **Banenkaart:** "Vind werk dichtbij huis, op de kaart." · "Open"
    5. **Sollicitaties:** "Zie waar je solliciteerde en wat er gebeurt." · "Open"
  - Desktop: text links at the row end. Mobile: the whole row is the link (≥ 44 px), no separate link text.
  - Row states: `done` (check, success), `now` (`aria-current="step"`, accent-soft, the lobster marker), todo (number).
- **Stone set and order (D15):** fixed in `CandidateHowStones` (Web, pure). The order above is **not** read from the nav, and this file must not touch the nav.
  - Werkgevers OFF (Dependency D): Banenkaart and Sollicitaties are hidden ⇒ 3 stones (the h1 count follows).
  - Paspoort flag off/absent: stone 2 is "Mijn profiel" → `/candidate/profile` with "Alles over jou op één plek.".
  - Ontdekkingsreis absent (`git grep "@page \"/ontdekkingsreis"` / route in `ROUTES.md`): stone 1 is "Je profiel invullen" → the existing onboarding/profile route.
  - Hrefs come from the existing routes (`git grep "@page"`); an unknown route drops that stone (log a warning) rather than linking to a 404. Unit tests for every flag combination.
- **Done state** per stone, read-only from existing data (one call, `GET api/me/journey-summary` new, Candidate, no writes):
  - ontdekkingsreis finished (the onboarding `FinishReached` / paspoort 06–08 completion if present)
  - paspoort ≥ the existing completeness threshold or "has at least one proof"
  - career = has an active plan with ≥ 1 completed step
  - banenkaart = has at least one saved/liked vacancy (if such data exists; else never done)
  - sollicitaties = has ≥ 1 application
  - `now` = the first not-done stone. Say in the PR which signals exist.
- Left card (desktop; mobile: a disclosure under the list) "Veilig en rustig" / "Goed om te weten":
  - shield "Werkgevers zien je naam pas als jij ja zegt." (hidden with Werkgevers OFF)
  - check "Alles is bewaard. Stoppen mag altijd."
  - wave "Lobsy in jouw taal: Nederlands, English, Polski, Română, العربية." (language names in their own language, always all five, `lang` attribute on each name, `dir="rtl"` on the Arabic name)
  - antenna "Vragen? Tik op **Assistent**, rechts op het scherm." (use `Nav.Assistant` for the word; "rechts" → "links" in RTL via its own key; hide the line if the assistant isn't rendered for candidates)
- Scene: five stones rising, labels "Reis · Paspoort · Carrière · Banenkaart · Sollicitaties" (short names, `aria-hidden`), the lobster (130 px desktop / 70 px mobile, plates 6) on the `now` stone, the trail gold up to it. Bubble: "Kijk: {n} stenen heb je al. Nu kies je waar je naartoe groeit." (n = done count; 0 ⇒ "Begin bij de eerste steen. Ik loop met je mee.") / mobile "{n} stenen heb je al. Op naar steen {n+1}."
- Footer:
  - text "Ik snap het" = calls `CompleteCandidateHowToAsync`, stays on the page, and shows a check line "Top. Je vindt deze uitleg altijd terug in het menu."
  - primary "Verder met {now stone title}" = calls the complete call too (ignore failure after logging, and don't block navigation; the error line only for "Ik snap het"), then navigates to the `now` stone.
  - Mobile: sticky footer with both.

## 3. Copy and languages
- New keys `HowC.*` in `UiStringsHowLobsyCandidate.cs`, 5 languages. The old candidate guide keys (`HowLobsyRoleGuides.Candidate` / `HowLobsy.Candidate.*`) that are no longer used: `git grep` each. Remove only when unused by `/hoe-werkt-lobsy` (the guest page may still show a candidate section; keep it and fix its copy per §1).
- `PageHelpDocs`/`PageSeoCatalog` updated.

## 4. Focus
- The h1 gets focus after navigation **without** the black box (§0 scoped rule). Keyboard focus keeps the brand outline. The same check applies to `/hoe-werkt-lobsy` only if it uses the journey wrapper (it doesn't; the global rule is deferred to 06's report).

## Tests
- bUnit:
  - no literal "_message" rendered; the error stays visible (fake API throws) and there's no navigation on error
  - stones for every flag combination (5 / 3 stones, paspoort off, ontdekkingsreis absent); the order is exactly D15
  - done/now states from a fake summary
  - "Ik snap het" calls the API once and stays; the primary navigates to the now stone
  - no inline `style` in `HowLobsyGuidePanel`
  - `ar`: `dir="rtl"`, the language line has `lang`/`dir` per name
- API: `journey-summary` Candidate-only, no writes (assert no `SaveChanges` side effects via the row counts of the touched tables).
- Localization: `HowLobsy.*` keys have the same facts in 5 languages (a parity test for the question-count wording: no digits in `HowLobsy.Step2Body` in any language).
- `BlazorPageRoleAttributesTests`: the candidate page is Candidate-only.

## Success criteria
- Matches `cr-d8`, `cr-m7` (nav excepted).
- No "_message", no stale numbers, no "Lik", no inline style; the guest `/hoe-werkt-lobsy` still works for guests and every role.
- The stone order is Dennis' order; the nav is untouched.
