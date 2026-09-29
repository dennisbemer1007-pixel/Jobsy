# 09. Languages: PL/RO/AR for landing, test and sign-up; RTL audit; native-review list

Read `00-README.md` first. Branch `cursor/landing-9` from `cursor/landing-8`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Translations only for the public funnel (landing, test, sign-up, e-mail code mail, public audience pages). Don't retranslate the logged-in app.

| | |
|---|---|
| Branch | `cursor/landing-9` |
| PR title | `feat(i18n): Polish, Romanian and Arabic for the landing page, gratis test and sign-up; RTL fixes` |
| PR body starts with | `Stacked on #<PR 08> (cursor/landing-8)` |
| Mockups | all `lp-*` (layout must hold in every language); no language-specific mockups |
| Split seam | **09a** = strings (09.2, 09.3). **09b** = RTL audit + fixes + screenshots (09.4) |

## Goal
Someone who is new to the Netherlands can do the whole funnel in Polish, Romanian or Arabic: landing → test → result → account. Arabic reads right-to-left without broken layouts. NL and EN are complete everywhere (D10).

## 09.1 Today (verify first)
- `UiStringsGratisDna.cs`: `AddTile`/`AddUi` copy the **English** sentence into pl/ro/ar (L64–85). So the test shows English for those languages. `LocalizationParityReportTests` doesn't catch it, because it counts identical-to-**nl** values (`docs/i18n/untranslated-baseline.txt`).
- The 20 question texts: find where `/ontdek` gets them (they reuse items of the existing banks: Competency Q1/6/11/16/21, Career Q1/6/14/18/22, Culture Autonomy Q1 / Informal Q3 / Collaboration Q5 / Flexibility Q7 / PeopleFirst Q11, Values Q1/6/11/16/21) and whether those items have pl/ro/ar.
- `UiStringsLanding.cs`, `UiStringsCandidateSignup.cs`, `UiStringsPublicPages.cs` have all 5 languages since 01/03/08 (B1 drafts).
- `CultureState.IsRightToLeft` → `dir="rtl"` on `PublicLayout` (01). `tools/i18n-export` exists.

## 09.2 Translate the funnel
- **GratisDna:** replace the en copies for pl/ro/ar in `UiStringsGratisDna.cs` with real translations for every key used by `/ontdek` (start, questions UI, result tiles, share, wipe, sign-up card, under-16, sticky/sheet, OFF `.Zw` keys). Keep `AddTile`/`AddUi` helpers but give them explicit pl/ro/ar parameters.
- **The 20 questions + Likert anchors:** if the bank items lack pl/ro/ar, translate **only those 20 items** and the anchors ("Past niet" … "Past wel"), in the bank's own localization structure; don't fork the question text into GratisDna.
- **Landing, sign-up, public pages:** review the B1 drafts from 01/03/05/07/08 for all keys with prefixes `Landing.`, `Public.`, `PublicNav.`, `PublicFooter.`, `Signup.`, `Werkgevers.`, `Scholen.`.
- **E-mail code mail** (03) and the "use your password" mail: pl/ro/ar.
- **Style:** B1, short sentences, informal "you" where the language allows it (pl "ty", ro "tu", ar the neutral form used elsewhere in the app). Keep the brand "Lobsy" and the product names "Banenkaart" and "Paspoort" in Dutch **with** a short gloss the first time on the landing (e.g. pl "Banenkaart (mapa ofert pracy)"). Emoji stay.
- **SEO strings** (`Landing.Seo.*`, `.Zw`, `GratisDna.Seo.*`, `Werkgevers.Seo.*`, `Scholen.Seo.*`) in all languages; they're used by `?lang=` responses.

## 09.3 Guards + review list
- `PublicFunnelI18nCoverageTests`: for every key with the funnel prefixes above (+ `GratisDna.`), pl/ro/ar values must differ from **en** as well as from nl. There's an allow-list for values that are legitimately identical (brand names, numbers, emoji-only strings, "OK"), kept in the test with a reason per entry.
- `docs/i18n/untranslated-baseline.txt` may only go **down**.
- Generate `docs/i18n/landing-for-review.csv` (key, nl, en, pl, ro, ar) with `tools/i18n-export` filtered to the funnel prefixes (add a `--prefix` option if missing), plus `docs/i18n/landing-review.md`: who should review (a native speaker per language), priority order (hero → test → result → sign-up → FAQ → the rest), and "Dennis signs off before the PL/RO/AR links are promoted".

## 09.4 RTL audit + fixes (ar)
Check and fix, in `public-theme.css` / `landing.css` / `gratis-dna.css` only:
- logical properties everywhere (`margin-inline`, `inset-inline-start`, `text-align: start`); no `left`/`right` except in illustrations that are deliberately not mirrored
- arrows and chevrons in CTAs ("→") flip under `[dir="rtl"]` (CSS `scaleX(-1)` on the icon span, not on the text)
- the mascot: `Mirror` flips under RTL so it faces into the content; the illustration scenes mirror their layout (card positions), but text inside SVG stays readable (never mirror text)
- the Likert row: "Past niet" at the start side (right in RTL), and the faces order follows it
- progress text and the block rail order; `<details>` markers; the bottom sheet; the language menu alignment
- numbers: the same digits as the rest of the app uses for ar (check `CultureState`/formatting); percentages and "1.230" formatting per culture
- the font stack renders Arabic (Segoe UI / system); no clipping of Arabic diacritics in pill buttons (line-height ≥ 1.4 inside `.pub-theme [lang="ar"]`)

## Tests
- `PublicFunnelI18nCoverageTests` (09.3), `LocalizationParityReportTests`, `GratisDnaI18nTests` (extend to pl/ro/ar: the result tiles show non-English text).
- bUnit: `/`, `/ontdek` result, `/account-maken` render in pl/ro/ar with no English fallback strings (sample assertions per section).
- Playwright: `?lang=ar` on `/`, `/ontdek` (start + a question + result) and `/account-maken` at 390 and 1440: `dir=rtl`, no horizontal overflow, the chips test from 07.7 passes in ar, and screenshots for the PR. `?lang=pl` and `?lang=ro` smoke (one screenshot each, overflow check: Polish and Romanian strings are longer).

## Success criteria
- The funnel shows no English in pl/ro/ar (guarded).
- Arabic `/`, `/ontdek` and `/account-maken` look correct in RTL at 390 and 1440 (screenshots in the PR).
- The review CSV + md exist, and the baseline didn't grow.

Done → next: `10-meten-kpi-lighthouse.md`.
