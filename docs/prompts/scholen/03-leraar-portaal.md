# 03. Leraar portal: klasoverzicht, codes, groepsresultaten, droombanen, detail per code

Read `00-README.md` first. Branch `cursor/scholen-3` from `cursor/scholen-2` (or `-2b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/scholen-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No pupil names anywhere, no AI / partner links / vacancies in anything pupil-related, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/scholen-3` |
| PR title | `feat(scholen): leraar portal — klasoverzicht, codes, groepsresultaten (k≥5), detail per code` |
| PR body starts with | `Stacked on #<PR 02> (cursor/scholen-2)` |
| Mockups | `sc-t1-leraar-klasoverzicht.png`, `sc-t2-leraar-code-detail.png` |
| Split seam | **03a** = class switcher + Klasoverzicht + Leerlingcodes + Testvenster + Materiaal (03.2–03.3, 03.6). **03b** = Groepsresultaten + Droombanen + Code detail (03.4–03.5) |

## Goal
A leraar signs in (2FA), picks one of **their own** classes and sees exactly what they need for the lesson and the follow-up talks: who has started or finished, the group picture (only with ≥ 5 completed), and per code the story, likes/dislikes, conversation starters and the droombaan route, plus a PDF (the button is wired in 06). Nothing outside their assigned classes is reachable, not even by guessing ids.

## 03.1 Today (verify first)
- 01 scope service (`CanTeachClass`, `CanSeePerCodeDetail`), 02's `ClassResultsAggregator`, `TestWindowDialog`, code list PDF/CSV services.
- `PupilResult` rows are still seeded in tests only. Real rows come in 05/06. The story/fit renderers arrive in 06: render placeholders behind a `IPupilStoryRenderer` interface with a stub implementation that 06 replaces.

## 03.2 Class switcher + routing
- `/leraar` → redirect to the first assigned class (sorted by name), or the empty state (§IA).
- The scope chip is the class switcher, listing **only** assigned classes of the current school year. The sidebar "Mijn klassen" group has one item per class.
- Every `/leraar/klas/{classId}/…` page and `api/teacher/classes/{classId}/…` endpoint checks `CanTeachClass`. A foreign or unassigned class → **404** (not 403, don't leak existence). A code id not belonging to that class → 404.

## 03.3 Klasoverzicht `/leraar/klas/{classId}` (sc-t1)
- KPIs: **Codes** · **Afgerond** (n / % bar) · **Bezig** · **Nog niet gestart** · **Gem. tijd** ("—" below 5 completed).
- Card **"Leerlingcodes"** (compact, first 10 + "Alle codes"): # · Code · Status · Voortgang (x/60 bar) · Laatst actief · link "Bekijk" when completed.
- Card **"Interesses in de klas"** (RIASEC distribution) and **"Wat drijft de klas"** (top values) and **"Droombanen"** (top list, "Overig" < 2). All only when ≥ 5 completed; else the empty state "Zichtbaar vanaf 5 afgeronde tests. Nu: {n}."
- Header actions: **Testvenster** (dialog from 02), **Codelijst** (PDF/CSV from 02, own classes only). Klasrapport is deferred (README differences).
- Status freshness: poll every 30 s while the window is open (or SignalR if a hub pattern exists; keep it simple).

## 03.4 Leerlingcodes `/leraar/klas/{classId}/codes`, Testvenster, Materiaal
- Full code table (`EntDataTable`, sort by #/status/last active, filter by status). Row menu: **Nieuwe code** (same confirm as 02). No delete (§R: SchoolAdmin only; show the hint "Code verwijderen bij bezwaar? Vraag je schoolbeheerder.").
- `/testvenster`: the state + `TestWindowDialog`, same server rules (409s) with teacher scope. If parents aren't confirmed yet: "De schoolbeheerder moet eerst bevestigen dat ouders zijn geïnformeerd."
- `/materiaal`: code list downloads + the lesbrief + "Zo werkt het in de les" (3 steps: 1. Deel de kaartjes uit. 2. Leerlingen gaan naar lobsy.nl/leerling. 3. Kiezen school, klas en typen de code.)

## 03.5 Groepsresultaten, Droombanen, Code detail
- `/groep`: from `ClassResultsAggregator` (k ≥ 5):
  - RIASEC bars with kid labels (Maken, Onderzoeken, Creatief, Helpen, Leiden, Ordenen)
  - top-3 drijfveren
  - culture preference tops
  - competence bands (counts per band, never per code)
  - a "Gesprek in de klas" card with 3 fixed discussion prompts from templates (06 fills the catalog; stub here)
- `/droombanen`: droombaan counts (≥ 2, rest "Overig") + "Weet ik nog niet" count. No codes listed next to jobs.
- `/code/{codeId}` (sc-t2), only when the code is **Completed**; else "Deze code is nog bezig (x/60)."
  - header "Code K7Q-M2P · Klas 2B" + note "Weet je wie dit is? Kijk op je eigen codelijst."
  - **Verhaal** "Dit ben jij" (renderer from 06)
  - 4 tiles (one per model: top category with kid label + one-line explanation)
  - **Leuk / Niet leuk** chips (incl. the one "Iets anders" word)
  - **Gespreksstarters** (3, from templates)
  - **Droombaan** card (chosen job + route summary from 06, "x van 5 heb je al")
  - primary **PDF downloaden** (wired in 06)
  - Every view writes `PersonalDataAccessLog` (resource `school.pupil-code`, action `view`, `SubjectPupilCodeId`).
- API `api/teacher/classes/{classId}/overview|codes|group|dreamjobs|codes/{codeId}`. The detail DTO never contains raw answers.

## Tests
- §R rows: Teacher own class ok; unassigned class → 404 on every page/endpoint; code of another class → 404; SchoolAdmin not assigned → 404 on `/leraar/…/code/{id}` but assigned SchoolAdmin ok (D2); per-code setting **off** doesn't affect the teacher.
- k-anonymity: 4 completed → group cards empty, 5 → shown; dream jobs < 2 → Overig.
- Access log row per detail view.
- bUnit for the switcher, KPI row, code table, detail page with stub renderer. Playwright (if runnable): sign in as teacher → switch class → open detail.

## Success criteria
- A leraar with classes 2B and 3A sees only those; any other class id is a 404.
- Klasoverzicht and code detail match sc-t1/t2 (placeholders only where 06 fills in).
- No raw answers in any teacher DTO (DTO guard test).

Done → next: `04-leerling-login-wizard.md`.
