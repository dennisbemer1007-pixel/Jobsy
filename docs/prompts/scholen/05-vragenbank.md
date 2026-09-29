# 05. Question bank: 60 child-friendly items on the existing scoring + hobbies/niet-leuk chips

Read `00-README.md` first. Branch `cursor/scholen-5` from `cursor/scholen-4` (or `-4b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/scholen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No pupil names anywhere, no AI / partner links / vacancies in anything pupil-related, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/scholen-5` |
| PR title | `feat(scholen): leerling question bank — 60 B1/A2 items on the 4 existing models + hobby chips` |
| PR body starts with | `Stacked on #<PR 04> (cursor/scholen-4)` |
| Mockups | `sc-p2-leerling-vraag-info.png`, `sc-p3-leerling-hobbys-niet-leuk.png`, `sc-p7-chromebook-vraag.png`; tone: `vragen-voorbeelden.md` |
| Split seam | **05a** = shared scorer extraction + `PupilQuestionBank` + all 60 texts + generated doc + guards (05.2–05.5). **05b** = Pauze-eiland chips + wiring into the wizard + `PupilResultBuilder` (05.6–05.7) |

## Goal
Pupils (12–16, vmbo to vwo) answer **60** short, concrete statements, 15 per model, in B1/A2 Dutch, each with an "Stel je voor…" example from school, home or free time (never from work). The answers are scored with **the same category scoring the adult tests use**, so the results mean the same thing. All 60 texts sit in **one** reviewable file, plus a generated markdown copy Dennis can read and comment on.

## 05.1 Today (verify first)
- The four catalogs in `Jobsy.Core/Rules/`:
  - `CompetencyTestCatalog`: categories Samenwerken, Resultaatgerichtheid, Stressbestendigheid, Innovatie, Extraversie
  - `CareerTestCatalog`: RIASEC, `HollandCode(RiasecScores?)`
  - `SchwartzValuesCatalog`: Autonomy, Connection, Achievement, Stability, Impact; `EverydayLabel`
  - `CulturePersonalityCatalog`: culture dims Autonomy, Informal, Collaboration, Flexibility, Innovation, PeopleFirst + Big Five facets

  Each has a `Score(IReadOnlyDictionary<int,int>)` that averages its own `Questions` per category: reverse = `LikertMax + LikertMin − raw`, then `LikertAnswerJson.ToPercent`. Question records: `CompetencyQuestion(Id, Category, Reverse, TextKey, IsRiasec)`, `CareerQuestion`.
- `Jobsy.Core/Rules/AssessmentTestCatalog.cs` (kinds, titles), `DeepAnalysisQuestionHelp.cs` (adult examples; **not** reused).
- Tone reference: `git show origin/docs/scholen:docs/mockups/scholen/vragen-voorbeelden.md` (16 example items + (i) texts + Lobsy encouragements). Use them as the **style**; you may keep them as items.

## 05.2 Shared scorer (no behaviour change for adults)
- Extract `Jobsy.Core/Rules/LikertCategoryScorer.cs`: `int? ScoreCategory(IEnumerable<(int Id, string Category, bool Reverse)> items, IReadOnlyDictionary<int,int> answers, string category)` with exactly today's math.
- Make the four catalogs' `Score` call it with their own question lists. **Golden test first** (commit before the refactor): for each catalog, 200 random complete answer sets + the edge cases (all 1, all 5, missing items) → snapshot of today's scores; after the refactor it's byte-identical.
- Add overloads that take an explicit item list: `CompetencyTestCatalog.Score(answers, items)`, `CareerTestCatalog.Score(answers, items)`, `SchwartzValuesCatalog.Score(answers, items)`, `CulturePersonalityCatalog.Score(answers, items)` → same records (`CompetencyScores`, `RiasecScores`, `SchwartzValuesScores`, `CulturePersonalityScores`). The pupil culture result has the Big Five facets **null** (Big Five comes from the Koraalrif).

## 05.3 `PupilQuestionBank` (structure, Core)
- `Jobsy.Core/Scholen/PupilQuestionBank.cs`: `PupilQuestion(int Id, PupilWorld World, AssessmentTestKind Model, string Category, bool Reverse, string TextKey, string ExampleKey)`. Ids **9001–9060** (never collide with adult ids), fixed order = display order. `Version = 1` (stored on `PupilResult.ScoringVersion`).
- Distribution (15 per model; mapped onto the existing categories):

  | World (model) | Category → items (reverse) |
  |---|---|
  | Koraalrif (Competentietest, Big Five) | Samenwerken (vriendelijkheid) 3 (1 rev) · Resultaatgerichtheid (zorgvuldigheid) 3 (1 rev) · Stressbestendigheid (rust) 3 (1 rev) · Innovatie (openheid) 3 (1 rev) · Extraversie 3 (1 rev) |
  | Schatgrot (Beroepentest, RIASEC) | Realistic 3 · Investigative 2 · Artistic 3 · Social 3 · Enterprising 2 · Conventional 2 (no reverse items: interests are liking, same as the adult RIASEC) |
  | Vuurtoren (Waarden op werk, Schwartz) | Autonomy 3 · Connection 3 · Achievement 3 · Stability 3 · Impact 3 (no reverse) |
  | Lagune (Cultuur & persoonlijkheid) | Autonomy 3 · Collaboration 3 · PeopleFirst 3 · Informal 2 (1 rev = "duidelijke regels") · Flexibility 2 · Innovation 2 |

  If an adult category's direction differs from what this table assumes, check `Reverse` against the adult items and follow the adult semantics. Say so in the PR.
- Interleave within a world so two items of the same category are never adjacent. The order is fixed and the same for everyone (no randomisation: simpler to review and to support in class).

## 05.4 The 60 texts (Cursor writes them; one file)
- **One file:** `Jobsy.Web/Localization/UiStringsLeerlingVragen.cs` (nl-only module, §0 Strings). In display order, for each item:
  - a comment line `// 9023 · Schatgrot · Social · rev:no`
  - `["LeerlingQ.9023"] = "…"`
  - `["LeerlingQ.9023.Voorbeeld"] = "Stel je voor: …"`

  Plus the world intro bubbles (`LeerlingQ.World.{n}.Intro`), "Wereld klaar!" lines and the encouragement lines (`LeerlingQ.Cheer.{n}`, ≥ 10, from `vragen-voorbeelden.md` style).
- **Generated review doc:** `docs/scholen/vragenbank-leerlingen.md`, a table # · Wereld · Model · Categorie · Omgekeerd · Vraag · Stel je voor…, generated by a test helper from the catalog + strings. `PupilQuestionBankDocFreshnessTests` fails when the doc is stale (pattern `RoutesDocFreshnessTests`; the failure message prints the regenerate command).
- **Writing rules** (B1/A2, 12–16 years, Dutch):
  - statements in **ik-vorm**, present tense, one idea per item, **≤ 14 words**, no double negatives, no "niet" in reverse items where a positive opposite reads easier ("Ik raak snel in paniek" beats "Ik blijf niet rustig")
  - contexts from **school, thuis, sport, hobby's, vrienden, bijbaantje-free**: no work, collega's, klanten, leidinggevende, sollicitatie, salaris, carrière
  - Values items may say "later" ("Ik wil later …") but describe the value, not a job
  - no brands, no social-media platform names, no gendered examples, no cultural/religious assumptions, no family-situation assumptions ("je ouders" → "thuis"), no body/appearance, no grades as a proxy for intelligence
  - Example ("Stel je voor…") **≤ 30 words**, one concrete scene, starts with "Stel je voor:", and never tells what the "right" answer is
- **Guard tests** (`PupilQuestionBankTests`):
  - exactly 60 items, 15 per model, the distribution above, unique ids 9001–9060, every `TextKey`/`ExampleKey` present and non-empty
  - word counts (≤ 14 / ≤ 30)
  - example starts with "Stel je voor:"
  - forbidden words (case-insensitive: `collega`, `klant`, `baas`, `leidinggevende`, `sollicit`, `salaris`, `vacature`, `carrière`, `werkgever`, `kantoor`, `instagram`, `tiktok`, `snapchat`, `youtube`, `vader`, `moeder`)
  - no digits in question texts
  - no two adjacent items with the same category
  - reverse flags match the table
- **Readability check:** add a simple `DutchReadability` helper (average sentence length + share of words > 3 syllables). Assert average ≤ 12 words and long-word share ≤ 10 % over the whole bank. Report the numbers in the PR.
- Mark the PR **"Needs content review by Dennis"** and link the generated doc in the PR body.

## 05.5 Info bubble (sc-p2)
- The (i) button (`aria-expanded`, `aria-controls`) toggles a speech bubble from Lobsy with the item's "Stel je voor…" text. Closed by default, closes on answer. Screen readers: the example is `aria-describedby` of the question when open.

## 05.6 Pauze-eiland `/leerling/eiland` (sc-p3)
- After item 30. Two chip groups from `PupilInterestChipCatalog` (Core; keys + nl labels in `UiStringsScholen`):
  - **"Wat vind jij leuk?"** ~24 chips (as in sc-p3, e.g. Sport, Buiten zijn, Dieren, Gamen, Tekenen, Muziek, Koken of bakken, Fietsen repareren, Bouwen & knutselen, Techniek, Lezen, Dansen, Theater, Filmpjes maken, Mode, Kleine kinderen, Natuur, Auto's & motoren, Computers, Puzzels, Rekenen, Talen, Reizen)
  - **"Wat vind je níet leuk?"** the same chips plus school-life ones (Voor de klas praten, Lang stilzitten, Hard werken in de kou, Veel lezen, Alleen werken, Druk en lawaai)
  - A chip can't be in both groups (selecting it in one deselects it in the other).
- **"Iets anders"** chip per group opens one input: max 24 chars, letters/spaces/hyphens only (server-validated regex `^[\p{L} \-]{1,24}$`), single word or two. The warning is always visible: "Typ geen namen (ook niet je eigen naam), adres of telefoonnummer." Server rejects digits, `@`, and any value matching a small list of common Dutch first names (`PupilNameGuard`, list ~500 names in a resource file) with "Dat lijkt op een naam. Kies liever een knop." (D13).
- "Klaar, verder!" is always enabled ("Het mag ook niks zijn"). Saved to `PupilProgress.LikesJson/DislikesJson/…OtherWord` via `PUT api/pupil/progress/chips`.

## 05.7 Wiring + result builder
- Replace the 04 placeholder `IPupilQuestionBank` with the real bank; the wizard now runs 1–60 with the island after 30.
- `PupilResultBuilder` (Infrastructure):
  - on the 60th answer: split answers per model, call the four `Score(answers, items)` overloads
  - `HollandCode` via `CareerTestCatalog.HollandCode`; top value via the max Schwartz category; top culture via the max culture dim (ties → catalog order)
  - write `PupilResult`, set `PupilCode.Status = Completed`, `PupilProgress.CompletedAtUtc`
  - Idempotent (re-completion recomputes).
  - Story keys and fit snapshot are filled in 06 (null now).
- Redirect after completion to `/leerling/dit-ben-jij` (placeholder until 06: "Klaar! Je verhaal komt eraan.").

## Tests
- Adult golden snapshots unchanged (05.2).
- Pupil scoring: known answer sets → expected percentages per category (hand-computed fixtures incl. reverse items); all-3 answers → 50 %.
- Bank guards + readability + doc freshness (05.4).
- Chips: exclusivity, "Iets anders" validation (digits, `@`, a first name → 400), warning rendered.
- Resume across the island; completion writes one `PupilResult`; no adult tables touched (assert `CandidateAssessment*`/norm tables unchanged).

## Success criteria
- 60 items in one file + the generated doc, all guards green, and the PR flagged for content review.
- Adult scoring byte-identical; pupil results use the same category math.
- The island matches sc-p3, and a pupil can finish all 60 items plus the chips.

Done → next: `06-verhaal-droombaan-pdf.md`.
