# Mockups: candidate test pages (`/candidate/competencies`, `/career`, `/culture` + `/disc`, `/values`, `/deep-analysis`, `/deep-analysis/checkout`)

Layout and copy reference for `docs/prompts/tests/`. All names, answers, prices, dates and counts are **Voorbeelddata**; the "Voorbeelddata" pill and the dashed "Ontwerpnotitie" boxes are mockup-only. Desktop 1440×900; mobile 390 wide at @2x, captured full length. Rendered with `prefers-reduced-motion: reduce`, so every PNG shows the complete still state.

Style = "De ontdekkingsreis" (`docs/mockups/ontdekkingsreis/` on `docs/mijn-paspoort`), the same visual language as Carrière (`docs/mockups/carriere/`): tokens, sea scene tokens, lobster + shell plates, rail, eyebrow + h1, bubble.
Test metaphor: **diving**. Eerste indruk 5 · Iets dieper 10 · Heel diep 25 (Cultuur 18) = the free test; **De bodem** = the paid uitgebreide test (150; Beroepen 200). The deeper you go, the darker the scene; a depth ruler shows the levels and the lobster sits at your current depth.

| File | Screen |
|---|---|
| `ts-d1-test-intro.png` / `ts-m1-…` | Test intro (Beroepen): 5 already answered in the journey, fact tiles, "Hoe diep wil je duiken?" depth radiogroup, "Ga verder bij vraag 6" |
| `ts-d2-vraag-likert.png` / `ts-m2-…` | Question 7 of 25, one at a time, segment bar with level ticks, level line, "Beantwoord (6)", rail "Jouw duik", focus mode |
| `ts-d3-vraag-uitgebreid.png` / `ts-m3-…` | Uitgebreide test Competenties, question 52 of 150, 5 topic chips, "Voorbeeld uit de praktijk", pause-point hint instead of a pop-up |
| `ts-d4-test-klaar.png` / `ts-m4-…` | "Weer een laag eraf": shard, what's now in the paspoort, "Iets aanpassen? (nog 3 keer)", next test, quiet teaser for De bodem |
| `ts-d5-uitgebreid-aanbod.png` / `ts-m5-…` | "Duik tot de bodem": what you get, sample link, price incl. btw, payment methods, 14-day waiver checkbox, "Nu niet" |
| `ts-d6-checkout-gelukt.png` | Back from Mollie, paid: receipt (what, amount incl. btw, date, reference), invoice by mail, "Begin met vraag 1" |
| `ts-d7-checkout-mislukt.png` | Payment failed: nothing charged, answers kept, "Opnieuw betalen" |
| `ts-m6-checkout-bezig.png` | Checking the payment (`role="status"`), mail promise if it takes long |

**Known differences (the spec wins):**
- **Nav:** Dennis' order (De ontdekkingsreis · Mijn Paspoort · Carrière · Banenkaart · Sollicitaties) is a separate add-on; this stack doesn't change the nav.
- **Prices:** € 2,99 is an example; the price is the admin value per test type.
- **Other example values:** the payment methods, minutes, "9 pagina's" and the receipt reference are examples.
- There is no forced-choice or ranking variant: every test is Likert 1–5.

Build: `python3 build.py` (Playwright + `/usr/bin/google-chrome`). `base/ontdekkingsreis_build.py` + `base/src/` make it self-contained.
