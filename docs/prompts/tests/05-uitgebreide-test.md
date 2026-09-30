# 05. The uitgebreide test: one question at a time in 5 parts with pause points, inline motivation; offer, order and checkout screens in the new design

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/tests-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red build/tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Nothing unlocks a paid test without a **paid** status from Mollie (or the stub path, only in Development or with `JobsyAuth:AllowStubPayments=true`). The UI never shows `ex.Message`.
> - Don't change the candidate nav (order, items, labels). Dennis' order is a separate add-on.

| | |
|---|---|
| Branch | `cursor/tests-5` from `cursor/tests-4` (or the last 04 sub-branch) |
| PR title | `Tests 05: uitgebreide test on TestQuestionFlow (5 parts, pause points, inline motivation), offer + order + checkout states in the ontdekkingsreis style, free first on TestDetail` |
| Body starts with | `Stacked on #<PR 04> (cursor/tests-4)` + the re-check of Dependencies A–E |
| Mockups | `ts-d3`/`ts-m3` (question), `ts-d5`/`ts-m5` (offer), `ts-d6` (paid), `ts-d7` (failed), `ts-m6` (checking) |
| Split if too big | `05a` = §1–§3 (question flow, parts, motivation); `05b` = §4–§6 (offer, order, checkout, TestDetail) |

**Goal:** the paid test is calm and doable: one question at a time, 5 parts with a rest after each, and no pop-ups. The offer explains honestly what you get and what you pay; the free result always comes first.

Closes: Q1 (deep), D1, D2, D3, N1 (deep), P5/P6 (design).

## 1. Question flow (`ts-d3`/`ts-m3`)
- `DeepAnalysis.razor` (`/candidate/deep-analysis`, `/{Kind}`) renders `TestPageShell` (04) in deep mode with `TestQuestionFlow`:
  - Questions from `DeepAnalysisCatalog.QuestionsFor(kind)`, displayed in `TestDepthRules.Parts(kind)` order (ids unchanged). The example text (`ExampleNl` → localized in 06) goes into "Voorbeeld uit de praktijk".
  - Header:
    - eyebrow "De bodem · {Test} · onderwerp {p} van 5"
    - topic chips for the 5 parts (done / now / todo with "{answered}/{size}"; hidden on mobile, where the eyebrow carries it)
    - h1 = the part's name (B1, 06 localizes)
    - "Vraag {i} van {total}" with the 25-block bar
  - **Parts names (nl):** competence and values = the domain labels (`DeepAnalysisQuestionHelp.DomainLabel`, B1-checked: e.g. "Nieuwe dingen proberen", "Afmaken en netjes werken", "Energie van mensen", "Samen en aardig", "Kalm blijven"); career and culture = "Deel {p}: {domain labels joined with ' en '}" (max 2 names, else "Deel {p} van 5").
  - **Rail "Jouw duik"** (desktop): the free levels done (collapsed into one "Heel diep · {n} vragen · gedaan" row when all done), then the zone "De bodem · uitgebreid" with the 5 parts (done / now with lobster "Vraag {j} van {size}" / todo). Footer "{answered} van {total} vragen · ± {m} min nog".
  - **Scene** depth 10, ruler ticks = the 5 parts, bubble "Hier beneden is het stil. Neem je tijd, we gaan in stukjes."
- Autosave (01) as on the free pages; resuming starts at the first open question in part order.
- Invalid `{Kind}` ⇒ friendly not-found state "Deze test bestaat niet." + link to `/profiel` (no silent fallback to competence, D2). `/candidate/deep-analysis` without kind ⇒ redirect to `/candidate/deep-analysis/competence`.
- Not unlocked ⇒ the offer (§4). Already completed ⇒ the finish state (§3) with the report link.

## 2. Pause points and inline motivation (D1, D3)
- `DeepAnalysisBoosters` (Core) is replaced by `DeepTestMotivation` (Core, pure): `ForProgress(kind, answered, total, partIndex)` → a key or null. It uses `TestDepthRules.Parts`, so "halfway" is really half (75 of 150, 100 of 200).
- **In the flow**, one quiet line in the card (the `deeper` style, no modal) at most once per part:
  - part start: "Onderwerp {p} van 5: {naam}."
  - half of the test: "Je bent halverwege. Goed bezig."
  - last part: "Laatste onderwerp. Bijna op de bodem."
  - before the last question of a part: "Na dit onderwerp komt een rustpunt. Stoppen mag: alles is bewaard."
- **Pause point** after each completed part except the last. The card shows:
  - pause icon; h1 "Rustpunt"; lead "Onderwerp {p} is klaar. Je hebt {answered} van {total} vragen gedaan."
  - the next part's name
  - primary "Verder met onderwerp {p+1}", secondary "Later verder" (flush → `/profiel/tests/{key}`)
  - The lobster rests (no bob under reduced motion anyway).
  - `aria-live` "Rustpunt. Onderwerp {p} is klaar."
- The old modal + hardcoded Dutch messages ("Halverwege de eerste helft? Nee…", "Bijna thuis") are removed; their keys go in 07's cleanup list.

## 3. Finish
- The last answer ⇒ save with `complete: true` (03 rules for Deep).
- Then the "Weer een laag eraf" moment (04 §4) in deep form:
  - shard label "De bodem · {total} van {total}"
  - h1 "Weer een laag eraf"
  - lead "Je bent op de bodem. Je rapport is klaar."
  - primary **"Bekijk je rapport"** → `/profiel/tests/{key}` (report section), link "Download als PDF" (existing report endpoint)
  - bubble "Voel je dat? Nu ken ik je echt."
  - No teaser, no next paid thing.

## 4. Offer (`ts-d5`/`ts-m5`)
- Reached from TestDetail "Uitgebreide test", the finish teaser (04) and a not-unlocked deep URL. It is a full state of `DeepAnalysis.razor` and replaces the 01 `DeepTestOrderDialog`, which is deleted (one path to pay).
- **Card:**
  - eyebrow "De bodem · uitgebreide test"; h1 **"Duik tot de bodem"**
  - lead "{n} vragen over {topic of the test}. Daarna krijg je een rapport, helemaal over jou."
  - "Wat krijg je?" with 4 lines (`DeepPay.Get.*`, per kind where the report differs; no claims the report doesn't deliver, and **no norm-group claim** unless `AssessmentNormSnapshots` really feeds this kind's report; verify and say which in the PR)
  - "Bekijk eerst een voorbeeld" → the existing `sample-report-preview` endpoint
  - tiles "± {m} minuten" / "In stukjes. Alles wordt bewaard." and "Je gratis uitslag blijft" / "Die {free} vragen blijven altijd van jou."
  - price block (gold): "€ {prijs}" + "Eenmalig · inclusief btw", method chips from `PrimaryMethods`, "Veilig betalen via Mollie" only in `mollie` mode (01.6)
  - the waiver checkbox (01.6 text, required)
- **Footer:** "Nu niet" (link → back) · primary **"Naar betalen · € {prijs}"** (disabled until ticked).
- **Bubble:** "Daar beneden ligt nog meer van jou. Kijken mag, het hoeft niet." The scene is at the bottom, the lobster looks down, the gold only on the bodem tick.
- Server errors (`DeepPay.Err.*`) render **in the card** under the price, never swallowed (D2). The consent gate (02) disables the primary with its text.

## 5. Checkout states (`ts-d6`, `ts-d7`, `ts-m6`)
- `DeepAnalysisCheckout.razor` keeps 01's logic (server status, polling, stub button, unknown id) and gets `TestPageShell` (normal nav, not focus mode):
  - **checking** (`ts-m6`): spinner in the status circle, "We checken je betaling", "Dit duurt meestal een paar seconden." (`role="status"`), the 60 s text, "Je betaalt nooit twee keer voor dezelfde test."
  - **paid** (`ts-d6`): check circle, "Betaald. Je kunt beginnen", "Fijn! De uitgebreide {Test}test staat voor je klaar.", the receipt `<dl>` (Wat, Bedrag incl. btw, Datum, Factuurnummer + PDF link), "Je krijgt de factuur ook per e-mail.", tiles ("{n} vragen · ± {m} min" / "In 5 onderwerpen, met rustpunten."; "Stoppen mag" / "Je gaat later verder waar je was."), link "Later beginnen", primary **"Begin met vraag 1"**; bubble "Gelukt! Zullen we samen naar de bodem duiken?"
  - **failed** (`ts-d7`): warning circle, "De betaling is niet gelukt", "Er is niets afgeschreven. Dat kan gebeuren, bijvoorbeeld als je het scherm van je bank sloot.", 3 lines (try again / answers kept / "Is er toch geld afgeschreven? Tik op Assistent. Dan zoeken we het uit." ; use the existing help/assistant entry, or the support e-mail if there is none), link "Terug naar de test", primary **"Opnieuw betalen"** (→ offer); bubble "Geen zorgen. Er is niets afgeschreven."
  - The rail shows the bodem row "Betaald · klaar om te duiken" on paid.

## 6. TestDetail offer (D3)
- The free action comes first and is the primary (brand). The paid "Uitgebreide test" is secondary with gold accent and goes to the offer (§4), never straight to checkout.
- Paid + not finished: "Ga verder met de uitgebreide test" (primary once the free test is completed).
- Minutes from `TestDepthRules`; the "Veilig betalen" line only in `mollie` mode and right under the price (no longer hidden behind the nav at the bottom; check at 390 px).
- Upsell copy (`DeepAnalysisService.FormatUpsellCopy` L64–78) becomes a key-based text in the Web layer ("Ontdek meer over jezelf met de uitgebreide test: {n} vragen en een rapport."), no "officiële"; the Core method is deleted or returns keys.

## Tests
- Unit:
  - `DeepTestMotivation` (halfway at 75/100, one line per part, last part)
  - part ordering keeps every id exactly once
- bUnit:
  - deep flow: eyebrow/part/chips, pause after part 1, resume at the first open question, invalid kind state, redirect without kind
  - offer: price per kind incl. btw, primary disabled until ticked, errors shown in the card, SecureMollie only in mollie mode
  - checkout 3 states in the shell
  - TestDetail: free first/primary, paid secondary → offer
- Playwright (if you can, stub mode): TestDetail → offer → tick → stub pay → paid → "Begin met vraag 1" → 30 answers → pause point → "Later verder" → resume at 31. Mobile screenshots of m3/m5/m6.

## Success criteria
- The uitgebreide test never renders more than one question's controls at a time (DOM check: 1 radiogroup).
- No modal appears during the test; pause points after parts 1–4.
- The offer shows the admin price for that kind incl. btw and can't be submitted without the waiver.
- TestDetail's primary action is always free.
- Invalid kinds show a friendly state; no silent fallback.

## Done → next
Push, open the PR (stacked on 04), note the number, go to `06-talen-rtl.md`.
