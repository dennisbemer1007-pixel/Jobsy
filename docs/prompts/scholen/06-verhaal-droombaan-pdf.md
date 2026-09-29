# 06. "Dit ben jij" (fixed templates) + droombaan-checker (no AI) + PDF on demand

Read `00-README.md` first. Branch `cursor/scholen-6` from `cursor/scholen-5` (or `-5b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/scholen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No pupil names anywhere, no AI / partner links / vacancies in anything pupil-related, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/scholen-6` |
| PR title | `feat(scholen): Dit ben jij + droombaan-checker (templates, no AI/links/vacancies) + PDF on demand` |
| PR body starts with | `Stacked on #<PR 05> (cursor/scholen-5)` |
| Mockups | `sc-p4-leerling-dit-ben-jij.png`, `sc-p5-leerling-droombaan-checker.png`, `sc-p6-leerling-pdf.png`; leraar side `sc-t2-leraar-code-detail.png` |
| Split seam | **06a** = story templates + renderer + `/leerling/dit-ben-jij` + teacher detail wiring (06.2–06.3). **06b** = droombaan catalog + checker + PDF (06.4–06.6) |

## Goal
After question 60 the pupil gets a warm, short story "Dit ben jij" and four tiles, built **only from fixed, reviewed templates**. They can pick a droombaan from a fixed list and see, in plain language, what they already have, what they'd need and a school route (no AI, no links, no vacancies, no employers). Pupil and leraar can download a one-page PDF, generated on demand and never stored.

## 06.1 Today (verify first)
- `Jobsy.Core/Rules/DnaSummarySentences.cs` (adult template sentences; pattern only, **don't** reuse the texts).
- `Jobsy.Core/Rules/DreamJobCatalog.cs` (48 curated `DreamJob(Key, TitleNl, IconKey, Synonyms)`), `CareerOccupationDetail.Requirements(title)` (plain-language diplomas), `OccupationTaxonomy`.
- `RoleFitCheck*` (OpenAI prompt, `TrainingOffersBlock`, vacancies, map): **must not be used or imported** (§0).
- QuestPDF: `AssessmentReportPdfService` (layout helpers, fonts), `LobsyCvPdfService`.
- 03's `IPupilStoryRenderer` stub and the teacher detail page.

## 06.2 Story templates + renderer
- `Jobsy.Core/Scholen/PupilStoryTemplates.cs` (`Version = 1`): pure, deterministic. Input = `PupilResult` scores + chips. Output = **keys** (stored in `PupilResult.StoryKeysJson`); text is rendered at view time from `UiStringsLeerlingVerhaal.cs`, so a copy fix never needs a data migration.
- Story = 4–5 sentences, in this order:
  1. **Big Five:** top category → an opener, e.g. `LeerlingStory.Bf.Samenwerken` = "Jij bent iemand die goed op anderen let." (5 variants, one per category, + a "balanced" variant when the spread is < 10 points)
  2. **RIASEC:** top-2 letters → one sentence for the pair (15 unordered pairs + 6 "strong single" variants), e.g. SA "Je helpt graag en je maakt graag iets met je handen."
  3. **Schwartz:** top value → "Belangrijk voor jou: …" (5 variants)
  4. **Culture:** top culture dim → "Je voelt je thuis op een plek waar …" (6 variants)
  5. **Likes** (if any): "Je houdt van {chip 1} en {chip 2}." (chip labels only, max 2; the "Iets anders" word is **not** used in the story)
- Tone rules (guard test like 05.4):
  - only positive or neutral framing, no labels like "slecht in", no comparisons with others
  - ≤ 20 words per sentence
  - forbidden-word list from 05 + `niet goed`, `zwak`, `slecht`
- **Four tiles** (sc-p4): Zo ben je (Big Five top) · Dit doe je graag (Holland code, kid labels Maken/Onderzoeken/Creatief/Helpen/Leiden/Ordenen) · Dit vind je belangrijk (top value, `SchwartzValuesCatalog.EverydayLabel` or a kid variant) · Hier voel je je thuis (top culture). Each has one line of explanation.
- **"Beroepen om eens te bekijken"** (sc-p4): 4 titles from a fixed map `PupilRiasecJobIdeas` (per RIASEC letter 6 kid-friendly titles, chosen by the top-2 letters, deterministic order, each a `DreamJobCatalog` key where possible). Plain text, **no links**.
- **Gespreksstarters** for the leraar (sc-t2): 3 per result from templates keyed by the top RIASEC letter + top value (gender-neutral, addressed to the leraar, e.g. "Vraag wanneer deze leerling het liefst iemand helpt: op school, thuis of bij de sport?"). Group prompts for 03's "Gesprek in de klas" come from the same catalog keyed by the class's top-2.
- `PupilStoryRenderer` replaces 03's stub (pupil page, teacher detail, PDF all use it).

## 06.3 `/leerling/dit-ben-jij` (sc-p4)
- The lobster with the new gold shell + "Klaar · 60 van 60" pill + h1 "Dit ben jij!"
- the story card, the 4 tiles, "Je houdt van" chips, "Beroepen om eens te bekijken"
- privacy line "Je leraar kan dit verhaal ook zien, met jouw code. Niemand anders."
- actions: secondary **PDF**, primary **"Check je droombaan"**

Read-only after completion (D8). No share buttons, no social links, no "maak een account" upsell.

## 06.4 Droombaan catalog (fixed, reviewable)
- `Jobsy.Core/Scholen/PupilDreamJobRoutes.cs`: for **each** `DreamJobCatalog` key (48) one `PupilDreamJobRoute`:
  - `Needs`: exactly **5** requirement keys, each tied to a scoreable signal, e.g. `LeerlingDroom.Need.ZorgVoorDieren` → RIASEC S or R ≥ 60 **or** chip "Dieren" liked; `…Need.Nauwkeurig` → Resultaatgerichtheid ≥ 60; `…Need.RustigBlijven` → Stressbestendigheid ≥ 60
  - `Route`: 3–4 steps (school subjects/profile → level: vmbo/havo/vwo → mbo/hbo/wo study name + duration → "Waar je kunt werken" in general terms)
  - `AltRoute?`: a vmbo/mbo line where one exists ("Zit je op vmbo? Via mbo Dierenartsassistent kan je ook verder.")

  Texts in `UiStringsLeerlingVerhaal.cs` (prefix `LeerlingDroom.`), in catalog order in **one** section so Dennis can review them in one place. Generated review doc `docs/scholen/droombanen-leerlingen.md` + freshness test (like 05).
- Content rules:
  - factual, Dutch education system
  - institution names **only** when the study exists at one or two Dutch institutions (e.g. Diergeneeskunde: Universiteit Utrecht); no rankings, **no URLs, no brands, no employers, no vacancies**
  - reuse `CareerOccupationDetail.Requirements` wording for diplomas where it fits
  - encouraging, never "dit is niets voor jou"
- "Weet ik nog niet" is a valid choice: it shows "Beroepen om eens te bekijken" again + "Praat erover met je leraar."

## 06.5 Droombaan-checker `/leerling/droombaan` (sc-p5)
- Search/select from `DreamJobCatalog` (matches `TitleNl` + `Synonyms`, client-side, no free-text storage). Selecting saves `PupilProgress.DreamJobKey` / `PupilResult.DreamJobKey` (`PUT api/pupil/dreamjob {key}`, unknown key → 400).
- Result card:
  - "{n} van 5 heb je al!" with 5 stars (gold = have)
  - **"Dit heb je al"** (met needs, as positive sentences)
  - **"Wat heb je nodig?"** (unmet needs as "Kies straks …" / "Oefen met …" steps)
  - the **route** as a vertical stepper, the goal step in gold
  - Lobsy's encouragement from a template by band (0–1 / 2–3 / 4–5), e.g. "Het is een lange reis, maar jij hebt al een goede start. Een kreeft groeit ook stap voor stap. Vraag je leraar wat je nu al kunt doen."
  - Always at least one "Dit heb je al" line: if none are met, show the generic "Je bent nieuwsgierig — daarom kijk je hier. Dat is een goed begin."
- `FitSnapshotJson` stores keys + counts only (for the teacher view and aggregates).
- Primary **"Bewaar als PDF"**.
- Deterministic: the same inputs give the same output (unit tests over all 48 jobs × fixture results).

## 06.6 PDF (sc-p6), on demand, never stored
- `IPupilReportPdfService` (QuestPDF, A4 portrait, one page):
  - header "Mijn ontdekkingsreis" + "Lobsy voor scholen · {date}" + "Klas {X} · {school}"
  - an **empty** "Naam (vul zelf in)" line (paper only, never filled)
  - lobster image
  - story, the 4 tiles, "Je houdt van" / "Niet zo leuk vind je" chips, "Beroepen om eens te bekijken"
  - droombaan route (if chosen) + the encouragement line
  - footer "Lobsy bewaart geen namen. Deze PDF is voor jou en je leraar." + page number

  The code is printed small in the footer (so the leraar can match it to the list).
- Endpoints:
  - `GET /leerling/pdf` (Pupil session)
  - `GET api/teacher/classes/{classId}/codes/{codeId}/report.pdf` (teacher-of-class, §R)
  - SchoolAdmin: **no** PDF endpoint (§R)
- Response `Content-Disposition: attachment; filename="lobsy-ontdekkingsreis-{klas}.pdf"` (no code in the filename), `Cache-Control: no-store`. Nothing is written to disk or blob storage (test with a fake file system / by asserting no `IBlobStorage` call). Teacher downloads are logged (`PersonalDataAccessLog`, action `pdf`).
- Wire the "PDF downloaden" button on sc-t2 and the "PDF" / "Bewaar als PDF" buttons on sc-p4/p5.

## Tests
- Templates: every key has nl text; all 5×… combinations render; tone guard; no chip "Iets anders" word in the story; determinism.
- Dream-job catalog: 48 routes, 5 needs each, every need maps to a valid signal, route 3–4 steps, no URL/`http`/`www`/`@` in any text, doc freshness.
- Checker: counts per fixture; the "none met" fallback; unknown key 400.
- PDF: text extraction contains the story + "Naam (vul zelf in)" + "Lobsy bewaart geen namen"; no storage call; `no-store`; teacher-of-class ok, other teacher 404, SchoolAdmin 404/403, pupil only own.
- Guards: `ScholenNoExternalProcessingTests` covers the story/fit/PDF services (no OpenAI/HttpClient/TrainingOffers/Vacancy types); `PupilPagesNoCandidateChromeTests` covers the 3 new pages (no `RoleFitCheckPanel`, `TrainingOffersBlock`, vacancy or map components; no `<a href="http` in the rendered markup).

## Success criteria
- A pupil finishes → sees "Dit ben jij" (sc-p4) → checks "Dierenarts" (sc-p5) → downloads the PDF (sc-p6). The leraar sees the same story + fit on sc-t2 and downloads the PDF.
- Zero AI calls, zero external links, zero vacancies (guards green). The PDF is never persisted.
- All template and route texts are reviewable in one place each, with the generated docs linked in the PR.

Done → next: `07-bewaartermijn-aggregaten.md`.
