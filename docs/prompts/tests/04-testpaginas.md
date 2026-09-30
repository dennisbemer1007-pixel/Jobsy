# 04. The four free test pages in the ontdekkingsreis style: intro with depth choice, question screen, "Weer een laag eraf", finish to the result

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/tests-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/tests-4` from `cursor/tests-3` |
| PR title | `Tests 04: Competenties, Beroepen, Cultuur, Waarden in the ontdekkingsreis style (intro, dive rail, one question, "Weer een laag eraf"), finish to /profiel/tests/{key}, career matches as bands` |
| Body starts with | `Stacked on #<PR 03> (cursor/tests-3)` + the re-check of Dependencies A–E |
| Mockups | `ts-d1`/`ts-m1` (intro), `ts-d2`/`ts-m2` (question), `ts-d4`/`ts-m4` (finish) |
| Split if too big | `04a` = §1–§3 (page shell, intro, question screen); `04b` = §4–§6 (finish moment, career matches, copy/TestDetail alignment) |

**Goal:** `/candidate/competencies`, `/candidate/career`, `/candidate/culture` (+ `/candidate/disc`) and `/candidate/values` look and feel like one more dive of the ontdekkingsreis. The candidate chooses how deep, answers one question at a time, gets a calm finish moment and lands on their result.

Closes: Q4 (jargon), F1, F2, N1 (free pages), L3 (layout part).

## 1. Page shell (`TestPageShell.razor`, `Components/Candidate/Tests/`)
- One shell for the 4 pages. Each page keeps its route, its data loading and its save client and passes `Kind` to the shell.
- **Desktop ≥ 1024** (`ts-d1`/`ts-d2`): three columns 272 / 640 / rest.
  - **Rail "Jouw duik"** (`<aside>`, `<ol>`), with zones:
    - "Gratis · de hele test": Aan de oppervlakte (done) → Eerste indruk 5 → Iets dieper 10 → Heel diep 25/18
    - "De bodem · uitgebreid": Uitgebreide test, locked with gold lock + "{n} vragen · rapport · € {prijs}" from 01.3; when paid, "Betaald · klaar om te duiken"
    - rows: done = check + "{n} vragen · gedaan", current = lobster marker + "Vraag {i} van {n}", the chosen target = "Jouw doel · {n} vragen"
    - footer: clock + "Nog {x} vragen · ± {m} min" (`TestDepthRules`) and "Alles is bewaard. Stoppen mag altijd." (only after a real save, 01.11)
  - **Card** in the middle; **scene** `TestDiveScene` (02) on the right with the bubble (`LobsyBubble Variant=Scene`).
- **Tablet 640–1023:** no rail; the rail's content collapses into the mobile progress bar; the scene shrinks to the band above the card.
- **Mobile < 640** (`ts-m1`/`ts-m2`):
  - sticky progress bar under the header: "{Test} · {answered} van {n}" (or "Vraag {i} van {n} · {Test}") + "Bewaard"
  - then the scene band with the lobster + bubble, the card, and the sticky footer (primary action full width)
- **Focus mode:** question screens keep today's `IsQuestionnairePath` (no bottom nav); the intro and finish screens show the normal nav (the mockups show that). Implement it by having the page tell `MainLayout` its mode through the existing mechanism; if that only works by path, the intro/finish states use the query `?stap=intro|klaar` and `IsQuestionnairePath` exempts those. Say which in the PR.
- Test names and h1 (the journey's 08.1 h1s): Competenties "Hoe werk jij?", Beroepen "Wat vind je leuk?", Cultuur "Waar voel je je thuis?", Waarden "Wat vind je belangrijk?". Eyebrow "Mijn tests · {Test}" (intro) / "In de diepte · {Test} · {level}" (questions).

## 2. Intro (`ts-d1`/`ts-m1`)
- Shown when the candidate opens the page and the test isn't completed (or via "Aanpassen" from TestDetail, with the 03 draft banner).
- Lead per test (nl final):
  - Competenties: "25 korte zinnen over hoe je werkt. Zo zien we waar je sterk in bent."
  - Beroepen: "25 korte zinnen over werk dat je leuk vindt. Zo vinden we beroepen die bij je passen."
  - Cultuur: "18 korte zinnen over waar je graag werkt. Zo vinden we werkplekken waar je je thuis voelt."
  - Waarden: "25 korte zinnen over wat je belangrijk vindt in werk."
- Four fact tiles:
  - "Je deed er al {k}" / "In de ontdekkingsreis. Die tellen mee." (only when k > 0; else "25 vragen · ± {m} min")
  - "Nog {x} vragen" / "± {m} minuten. Stoppen mag altijd."
  - "Geen foute antwoorden" / "Kies wat het eerst in je opkomt."
  - "Alleen voor jou" / "Werkgevers zien je antwoorden niet."
    - With Werkgevers ON and the tests feeding matching, use "Werkgevers zien alleen je uitslag, niet je antwoorden." instead.
    - Verify what the employer side really shows today (`CandidateInsights`/match detail) and pick the true sentence. Say which in the PR (**flag for Dennis** if unclear).
- **"Hoe diep wil je duiken?"** fieldset = radiogroup of depth cards (paspoort `or-d6` style):
  - Eerste indruk / Iets dieper / Heel diep (Cultuur: "Alle 18") with "{n} vragen · + {m} min" and 5 depth dashes
  - levels already reached are shown done (disabled)
  - the default is **Heel diep** (the whole test)
  - the choice is the flow's `Target`
- "Wat meet deze test? Lees de uitleg" → `/profiel/tests/{key}`.
- Footer: "Mijn tests" (secondary → `/profiel/tests/{key}`; `/candidate/profile` when the paspoort flag is OFF) · primary **"Ga verder bij vraag {k+1}"** / "Begin" when k = 0.
- The consent gate (02 §5) replaces the depth choice and the primary when closed.
- Bubble: "Vijf vragen heb je al gedaan. Zullen we samen wat dieper gaan?" (k ≥ 5) / "Klaar voor een duik? Ik ga met je mee." (k = 0).

## 3. Question screen (`ts-d2`/`ts-m2`)
- `TestQuestionFlow` (02) inside the card, with `Target` from the intro.
- h1 = the test h1; lead "Hoe goed past deze zin bij jou? Er zijn geen foute antwoorden."
- Bubble: "Er zijn geen foute antwoorden. Kies wat het eerst in je opkomt." (mobile short: "Geen foute antwoorden. Kies wat eerst opkomt.").
- The lobster moves down the ruler with the answered count (CSS custom property `--y`).
- Career category labels drop the RIASEC letter: "Aanpakken met je handen", not "R — Aanpakken met je handen" (Q4); the letter stays only in the report glossary.
- "Later verder" → flush → `/profiel/tests/{key}` with a quiet toast "Je antwoorden zijn bewaard. Je gaat later verder bij vraag {i}."

## 4. "Weer een laag eraf" (`ts-d4`/`ts-m4`; D2)
- Shown when the target level is reached (`OnLevelReached` for the target) or on "Afronden".
  - Full count ⇒ the save runs with `complete: true` (and 03 rules); otherwise `complete: false`.
- **Card:**
  - eyebrow "In de diepte · {Test} · {level} klaar"
  - pic tile with the fallen shard (`build.py` `SHARD`), labelled "{level} · {n} van {total}"
  - h1 **"Weer een laag eraf"**
  - lead "{Test} is helemaal klaar. Dit staat nu in je paspoort:" (Full) / "{Test}: {level} is klaar. Dit staat nu in je paspoort:" (lower level)
  - 3 check lines from the result: the top 2 strengths/interests + 1 "minder jouw ding", from the existing result DTO in B1 labels, no numbers
- **Tiles:**
  - "Iets aanpassen?" / "Dat mag nog {remaining} keer" (03; hidden before the first completion)
  - or, for a lower level, "Nog dieper?" / "{Next level} · + {m} min"
  - next test: "Volgende test: {Test}" / "Nog {x} vragen · ± {m} min", the next free test not yet at Full, in the journey order Competenties → Beroepen → Cultuur → Waarden; hidden when all are done
- **Teaser** (quiet, gold dashed outline, only when the uitgebreide test exists for this kind and isn't bought):
  - "Nog dieper? Tot de bodem." / "De uitgebreide test: {n} vragen en een rapport."
  - link "Bekijk wat je krijgt ›" → the offer (05)
  - Never a price or a buy button on this screen.
- **Footer:** "Terug naar Mijn tests" (link) · primary **"Bekijk je uitslag"** → `/profiel/tests/{key}` (D7).
- **Scene:** the shard falls once (1.2 s) next to the lobster, and the lobster stays at its plate state (D2). Reduced motion: shard already lying there.
  - Bubble: "Voel je dat? Weer een laag eraf. Nu zie ik nog beter {wat je leuk vindt | hoe je werkt | waar je je thuis voelt | wat je belangrijk vindt}."
  - `aria-live` announces "Weer een laag eraf. {Test} {level} is klaar."
- **Journey:** when the page was opened from the ontdekkingsreis (`?from=reis&stap={n}`), the primary is "Terug naar de reis" instead (08's own moment stays in the wizard).

## 5. Career matches (F2, D7; Dependencies D, E)
- The "Top 10 vacatures … 60% of hoger" panel in `CareerTest.razor` is removed from the test page.
- On the Beroepen finish moment, **only with Werkgevers ON**, one quiet line under the gained list:
  - **E present:** "{n} vacatures passen goed bij wat je leuk vindt" (count of `FitBand ≥ Good`, only when `CandidateFitGate.IsOpen`) + link "Bekijk ze" (banenkaart with the career filter)
  - **E absent:** "Bekijk vacatures die passen bij wat je leuk vindt" + link, no count
  - No percentages anywhere. Werkgevers OFF ⇒ nothing, and the server doesn't compute it.

## 6. Copy, names and TestDetail alignment (N1 free part)
- Remove from candidate test pages: "Quick-Scan", "diepte-analyse", "Diepteanalyse", "Uitgebreide competentie-analyse", "officiële", "tags voor matching", "Nog 25 vragen · op de laatste vraag wordt dit 'Afronden'".
  - The product name is "Uitgebreide test" (D1).
  - `TestsOverviewPanel` and `TestDetail` get the same names and read `TestDepthRules` for "Nog {x} van {y} vragen" and the level ("Eerste indruk gedaan", "Heel diep gedaan").
  - Dependency C present ⇒ `TestsOverviewBuilder` too.
- `TestDetail`:
  - the start/continue buttons go to the new intro
  - the raw DOI citation becomes "Gebaseerd op onderzoek" with the citation behind a disclosure
  - minutes from `TestDepthRules`
  - Its restyled look stays; only these texts/links change here (the paid offer is 05).
- Back links on all 4 pages → `/profiel/tests/{key}` (D7).
- Strings: `TestFlow.*`, `TestDepth.*`, `TestDone.*` nl final (above), en written, pl/ro/ar = en + listed for 06.

## Tests
- bUnit per page:
  - intro (k = 0 / k = 5 / completed + draft), depth choice default Heel diep, levels done disabled
  - question → finish moment on target; Full save sends `complete: true`
  - finish links (result, next test, teaser only when not bought)
  - Werkgevers OFF has no vacancy line
  - the journey return variant
- `/candidate/disc` renders Cultuur.
- Guards: no hex, no inline style; `RoutesDocFreshnessTests`, `PageHelpDocsTests`, `PageSeoTests` updated.
- Playwright (if you can): Beroepen from 5 answered → Heel diep → 20 answers → "Weer een laag eraf" → "Bekijk je uitslag" lands on `/profiel/tests/career`. Desktop + mobile screenshots, and `ar` at 390 px (dir rtl, no overflow, primary at inline-end).

## Success criteria
- The 4 pages match `ts-d1/d2/d4` and `ts-m1/m2/m4` (spec wins on the listed differences).
- Every "Afronden" ends in the finish moment and then `/profiel/tests/{key}`; no page navigates to `/candidate/profile` or `/profiel` directly after finishing.
- No percentage and no vacancy link on these pages with Werkgevers OFF.
- None of the removed words appear in nl candidate copy of these pages (string test).

## Done → next
Push, open the PR (stacked on 03), note the number, go to `05-uitgebreide-test.md`.
