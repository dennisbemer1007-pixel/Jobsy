# 02 · Mijn Paspoort, Phase 1: flag, nav, route, top overview, tab shell, tab Mijn DNA

> Read `00-README.md` first: §0 rules apply, plus **§F** (add `CandidatePassportEnabled` to the chain built in 01) and **§N** (the paspoort-ON order and the Bewaard tab). Execute this file only after the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/mijn-paspoort-1`, created from `cursor/opleidingen-seed-opruimen` |
| PR | ONE PR into `acceptatie`, title `feat(paspoort): Mijn Paspoort flag, overview and Mijn DNA tab`. Body starts with `Stacked on #<PR of 01b> (cursor/opleidingen-seed-opruimen)` |
| Mockups | `pp-d1-mijn-dna.png`, `pp-m1-overzicht.png` (and the top part of every other mockup) |

## 02.1 Flag and routes
- `CandidatePassportEnabled` (default **false**) plus the admin checkbox (§F).
- **New page** `Components/Pages/Candidate/Passport.razor`:
  - `@page "/candidate/paspoort"`, `[Authorize(Roles = "Candidate")]`
  - `@rendermode InteractiveServer` with prerender, like `Profile.razor`
  - `[RequiresFeature(CandidatePassport, FallbackPath = "/candidate/profile")]`
- **Tabs** via `?tab=`, in a new `Navigation/PassportTabs.cs` modelled on `CandidateKompasTabs` (constants + `Normalize` + `Neighbor`):
  - `dna` (default), `tests`, `fit`, `career`, `proof`, `data`
  - Legacy values map too: `profile`/`profiel` → `data`, `wie-ben-ik` → `dna`, `functiefit` → `fit`, `carriere` → `career`, `bewijzen` → `proof`, `gegevens` → `data`.
- **Flag OFF:** `/candidate/paspoort` redirects to `/candidate/profile`, keeping `?tab=` mapped back to the Kompas tabs, so the paspoort isn't reachable. Everything else is unchanged.
- **Flag ON:**
  - `/candidate/profile` (`Profile.razor`) redirects to `/candidate/paspoort` with the tab mapped (`dna`→`dna`, `profile`→`data`, `tests`→`tests`, `fit`→`fit`) and `returnUrl` preserved. Do it at the top of `OnInitializedAsync`, before loading data.
  - `/profiel` already forwards to `/candidate/profile` and so follows.
  - D4: `/home` for candidates also redirects to the paspoort, so the old Kompas isn't a second profile.
- **Transitional rule (removed in 05).** Until Phase 4 lands, `tab=profile` on `/candidate/profile` is **not** redirected. The paspoort tabs Bewijzen and Mijn gegevens then show one line plus a link "Open je gegevens" → `/candidate/profile?tab=profile`. Keep this in one constant, `PassportRedirects.ClassicTabsUntilPhase4`, so 05 can delete it. Candidates can then always reach their forms.
- **Nav** per §N: add the paspoort-ON order to `CandidateItems`.
  - `Nav.Passport` = "Mijn Paspoort" (reuse `NavIcons.Profile`).
  - Build the `CandidateJobListTabs` Bewaard tab (§N, D8) with the new strings `Nav.ApplicationsTab` ("Sollicitaties") and `Nav.SavedTab` ("Bewaard").
- **Home path:** extend `FeatureRoutes.HomeFor` (from 01) so a candidate goes to `/candidate/paspoort` when the paspoort flag is ON.
- **Docs:** `ROUTES.md`, `PageSeoCatalog` (`Private("Passport.Title", …)`), `PageHelpDocs` entry for `/candidate/paspoort`.

## 02.2 Page structure (all tabs share it)
- **Desktop ≥1024:** 2 columns.
  - Left: the sticky **passport card** (≈330 px).
  - Right: the **overview row**, then the **tab bar**, then the **tab panel**.
- **Mobile:** compact passport card, then the overview card (ring + 2×2 stats), then the sticky tab bar (under the header, horizontal scroll, active tab scrolled into view, edge fade), then the panel.
- **Above the fold:** at 1440×900 the tab bar sits above the fold. The mockup puts it at ~y 322; keep it ≤ 360. On 390×844 the tab bar starts at ≤ ~400 px.
- **Each tab fits roughly one screen.** The rest goes behind "Meer" or links to the full page.
- `h1` = "Mijn Lobsy-paspoort" (visually hidden is fine; the passport card name is `h2`). One mascot bubble per tab (02.6).
- **Tab bar:** reuse the ARIA tab pattern and keyboard handling from `CandidateKompas.razor` (L56–100, `OnTabListKeyDown`).
  - Lazy-load each panel on first activation (like `Active` in `DnaPanel`).
  - Update the URL with `replace`.
  - Icons + text per tab (§0 icon rule).
  - Tab labels: "Mijn DNA", "Mijn tests" (count pill: tests with a next step), "Past deze baan?", "Carrière", "Bewijzen", "Mijn gegevens".

## 02.3 Passport card (identity column): reuse the existing profile data
Data comes from the same sources as `Profile.razor` / `CandidateKompas` (`MeProfile`, `CandidateKompasState`, `CandidateDnaSummary`). No new endpoint.
- **Banner:** "LOBSY PASPOORT" and a short member number (a stable non-PII hash of the user id, e.g. `LB-` + 5 chars; don't show the database id), plus a shell-shaped stamp "GESTART {maand ’jj}" (the account creation month).
- **Avatar:** **initials** in a `--brand` circle. Photo upload does not exist, so it's **deferred**: no edit badge, no fake photo.
- **Name and pills** (max 2 + 1):
  - "Beschikbaar voor werk" when `OpenForWork`, only if Werkgevers actief is ON.
  - Up to 2 DNA keywords (`WhoAmI.Keywords`).
- **Shell layers** instead of the % bar:
  - 5 segments filled `floor(ProfileCompletenessPercent / 20)` in stepped tokens.
  - Text: "{n} van 5 lagen · aanvullen", which opens the tab `data` (in Phase 1 the transitional link).
  - `role="progressbar"` with `aria-valuenow` = the percent. Reuse the same percent as `CandidateKompas.EffectiveCompletenessPercent`: extract that getter into the shared builder (02.5).
- **"Mijn verhaal":** the candidate's own "Over jezelf" text (`Profile.AboutMe`), clamped to 3 lines. If empty: "Schrijf in een paar zinnen wie je bent." plus a link.
- **Facts 2×2:** Ik woon in (city from the address), Reizen (max travel + transport), Uren per week, Beschikbaar (availability presets).
  - Then "Wat ik zoek" (interests chips, max 2 + "+n") and Rijbewijs (licenses).
  - An empty fact shows "—" with an "aanvullen" link. Never invent values.
- **Actions:**
  - "Lobsy-CV" (the existing Lobsy-CV download, `Profile.DownloadLobsyCv`)
  - "Aanpassen" (opens `data`)
  - **"Deel mijn paspoort" is deferred.** Sharing needs a public link and a privacy review, so don't render it.
- **Tagline** at the bottom (desktop only): shell icon (`--coral`, the only coral on the screen) + "Lobsy: ontdek wie je bent onder de schaal".
- **Mobile card:** avatar, name, one line "{stad} · {vervoer} {min} min · {uren} u", 2 pills, shell stamp, shell layers. No facts grid; the facts live in the `data` tab.
- **Spoken languages don't exist** in the model, so they're **deferred**. Don't show a languages row.

## 02.4 Overview: "Dit ben jij" (always on top)
- **DNA ring** (new `Components/Candidate/Passport/DnaRing.razor`, pure SVG, `aria-label`):
  - 4 arcs: Competenties `--brand`, Beroepen `--success`, Cultuur `--gold`, Waarden `--warn`.
  - Each arc is filled by that test's progress: completed = full, else answered / question count, from the same `DnaCard` data that `DnaPanel` uses.
  - Centre: the mascot (`mascot-128.webp`) inside a dashed `--gold-light` circle, meaning the next shell.
  - Text next to it (desktop) or under it (mobile): "Dit ben jij" / "Jouw kreeft · **fase {k} van 4**", where k = the number of completed tests (0–4), plus the legend.
  - At k = 0 the text reads "Jouw kreeft · nog in het ei" and the arcs are ghosts.
- **4 stat cards** (desktop row; mobile 2×2 inside the ring card). They reuse the existing DNA highlights (`Dna.HighlightStrongest` / `HighlightWork` / `HighlightImportant` from `DnaPanel` ~L481–491) plus the culture home label:

  | Card | Label | Value source | Status line |
  |---|---|---|---|
  | 1 (`--brand`, claw icon) | "Jouw sterkste klauw" | strongest competence | "Uitgebreid" / "Quick-Scan" / "Voorlopig" |
  | 2 (`--success`) | "Werk dat bij je past" | top RIASEC label | RIASEC code |
  | 3 (`--gold`) | "Hier voel je je thuis" | culture top pole | |
  | 4 (`--warn`) | "Dit vind je belangrijk" | top value | |

  - Status is text, not colour alone.
  - When a test isn't done, the card shows "Nog niet ontdekt" plus a quiet link "Doe de test" (to the tab `tests`).

## 02.5 Extraction (no duplicate logic)
- **Move the card/highlight/slide/detail building out of `DnaPanel.razor @code`** (~L290–905: `BuildCompetence`/`BuildCareer`/`BuildCulture`/`BuildValues`, `Paint`, `BuildSlide`, `BuildCultureDetail`, `BuildValuesDetail`, the highlights and the private records `DnaCard`/`DnaHighlight`/`DnaSlide`/`DetailBlock`) into `Jobsy.Web/Components/Candidate/Dna/CandidateDnaViewBuilder.cs`.
  - It's pure and testable, and takes `CandidateKompasState`/`CandidateDnaSummary` plus a string lookup.
  - `DnaPanel` then only renders. Its markup and classes stay identical, which existing tests like `KompasDnaLandingTests` and `MijnDnaLoadSpeedTests` check.
- The completeness getter moves into the builder too; `CandidateKompas` uses it from there.
- **Loading:** the paspoort page loads once, sharing the same state objects that `Profile.razor` passes to `CandidateKompas` (`SharedKompas`, `SharedDna`, `PersistentComponentState`), so there's no double fetch. Reuse the persist keys and pattern.

## 02.6 Mascot bubbles (one short line per tab; B1 NL is final)
One `LobsyBubble` component (mascot 40 px + `accent-soft` bubble; use the existing mascot bubble if one is already there). The bubble is the one notice per screen (§0).

| Tab | Key | NL |
|---|---|---|
| dna | `Passport.Bubble.Dna` | "Je hebt net een laag afgeworpen. Dit is wie eronder zat." (0 tests: "Hier werp je je oude schaal af. Begin met een test.") |
| tests | `Passport.Bubble.Tests` | "Niet zoeken aan de oppervlakte. Duik dieper." |
| fit | `Passport.Bubble.Fit` | "Je antennes wijzen deze kant op. Twijfel je? Typ een baan, ik kijk mee." |
| career | `Passport.Bubble.Career` | "Elke stap is een stukje nieuwe schaal dat aangroeit." |
| proof | `Passport.Bubble.Proof` | "Dit is je nieuwe, sterkere schaal. Alles wat je deed telt, ook mantelzorg." |
| data | `Passport.Bubble.Data` | "Jouw schaal, jouw regels. Jij bepaalt wie wat ziet." |

## 02.7 Tab "Mijn DNA"
3-card grid (mobile stacked), then the shells row.
- **"Wat maakt jou jou"** (eyebrow "Onder je schaal"): the 3 DNA highlights as rows (icon + bold line + one muted line) from `CandidateDnaViewBuilder`. With fewer than 3, show only what exists.
- **"Jouw verhaal":**
  - The WhoAmI story (`Dna.Story*`, same clamp/more as `DnaPanel`), keywords as `kompas-chip` (max 3).
  - Meta: "Door Lobsy geschreven uit je tests · {datum}".
  - Empty state: the existing `Dna.StoryEmpty` + CTA to `tests`.
- **"Waar voel jij je thuis":** 2 culture pole sliders (reuse `PoleSliders`) and "Waarden op werk · voorlopig|klaar", top 3 ranked (reuse the `BarList` data, shown as a numbered list). Ghost state plus a teaser when not done (existing `Dna.Teaser*`).
- **"Mijn schalen"** (the old "stempels").
  - Subtitle: "Hier werp je je oude schaal af · {n} van 6". Right side (desktop): "Groot worden doe je door je schaal af te werpen." plus "Nog {x} vragen tot je volgende schaal" (x = remaining questions of the nearest unfinished test).
  - Stamps do **not** exist yet, so they are **derived from existing data** (no storage, no dates) in `PassportShellRules.cs` (Core/Rules, unit-tested):

    | Shell | Earned when |
    |---|---|
    | Eerste test | ≥1 test completed |
    | Eerste rapport | ≥1 extended/deep analysis completed |
    | CV erbij | own CV uploaded **or** Lobsy-CV downloaded at least once, if that's tracked (else only own CV) |
    | 3 tests gedaan | ≥3 completed |
    | DNA compleet | 4 completed |
    | Eerste baan / sollicitatie | ≥1 application; **hidden** when Werkgevers actief is OFF, so the total becomes "van 5" |

  - Rendering: a shell outline per stamp (SVG, dashed stroke in the stamp's token), an icon, a 2-line label, and a slight rotation that is off under reduced motion. Unearned shells are dotted `--border` with muted text. Mobile wraps to 2 rows.
- **Tabs 2–6 in Phase 1:** render exactly what already exists, so nothing is lost:
  - `tests`: the existing `TestsOverviewPanel`
  - `fit`: the existing `RoleFitCheckPanel`
  - `career`: one card "Mijn loopbaanplan" with a link to `/carriere`
  - `proof` and `data`: the transitional link (02.1)
  - Each still gets its bubble. 03–05 replace these.

## 02.8 Strings (02)
- `Nav.Passport`
- `Passport.Title`, `Passport.Banner`, `Passport.MemberNo`, `Passport.StartedStamp`
- `Passport.Layers` ("{0} van 5 lagen"), `Passport.LayersAria`, `Passport.Fill` ("aanvullen")
- `Passport.MyStory`, `Passport.MyStoryEmpty`
- `Passport.Facts.*` (LivesIn, Travel, Hours, Available, LookingFor, License)
- `Passport.LobsyCv`, `Passport.Edit`, `Passport.Tagline`
- `Passport.Overview.Title` ("Dit ben jij"), `Passport.Overview.Stage` ("Jouw kreeft · fase {0} van 4"), `Passport.Overview.Egg`, `Passport.Legend.*`
- `Passport.Stat.*`, `Passport.Stat.NotYet`
- `Passport.Tab.*`, `Passport.Bubble.*`
- `Passport.Dna.Eyebrow`, `Passport.Dna.Home`, `Passport.Dna.ValuesProvisional`
- `Passport.Shells.Title`, `Passport.Shells.Sub`, `Passport.Shells.Tagline`, `Passport.Shells.Next`, `Passport.Shells.Item.*`
- `Passport.Transitional.OpenData`

## Tests
- **Flag and nav:**
  - flag default false; admin round-trip
  - `CandidateItems` order per §N (4 combinations, count ≤ 5)
  - `CandidateJobListTabs` renders only when Bewaard is not in the nav; `/candidate/liked` keeps the Sollicitaties nav item active
  - bUnit `BottomNav`: "Mijn Paspoort" as the first item when ON (slot 1 is still empty), "Profiel" last when OFF
- **Redirects:**
  - OFF: `/candidate/paspoort?tab=data` → `/candidate/profile?tab=profile`
  - ON: `/candidate/profile?tab=tests` → `/candidate/paspoort?tab=tests`, `returnUrl` kept
  - ON: `/candidate/profile?tab=profile` is **not** redirected (transitional)
  - `/profiel` follows
  - D4: candidate `/home` → paspoort when ON
- **Builder parity:** `CandidateDnaViewBuilder` gives the same highlights and cards for the fixtures that `DnaPanel` rendered before (golden test); `DnaPanel` markup is unchanged.
- **Rules:** `PassportShellRules` table test (incl. employers OFF → 5); `PassportTabs.Normalize` mappings.
- **bUnit:**
  - `DnaRing` (0/2/4 tests, labels, aria)
  - passport card: empty facts show "—", no fake photo, no share button, the "Beschikbaar" pill hidden when employers OFF
  - stat cards not-done state
  - tab keyboard navigation
- **Playwright** (`MijnPaspoortPlaywrightTests.cs`), flag ON:
  - 1440×900: the tab bar bottom is ≤ 360 px and the Mijn DNA panel is visible
  - 390×844: the tab bar is sticky under the header and scrolls horizontally
  - flag OFF: the nav equals today's

## Success criteria (all must hold before you open the PR)
- With the paspoort flag **OFF**:
  - the nav, `/candidate/profile` and every existing test are exactly as before
  - `/candidate/paspoort` redirects to the profile
- With the paspoort flag **ON**:
  - the nav is [empty slot] · Mijn Paspoort · Zoeken · Sollicitaties · Carrière (Werkgevers ON) or Mijn Paspoort · Carrière (OFF)
  - Bewaard is a tab inside Sollicitaties
  - `/candidate/profile?tab=…` redirects with the tab mapped (except the transitional `tab=profile`)
- **Layout:** at 1440×900 the tab bar sits at ≤ 360 px and the Mijn DNA tab fits about one screen. At 390×844 the tab bar is sticky and scrolls.
- `DnaPanel` markup is unchanged, and its logic lives in `CandidateDnaViewBuilder` (no duplicate code).
- **Honest content:** no fake photo, no share button, no languages row. Every new string is in nl/en/pl/ro/ar.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has: stacked-on line, what and why, screenshots (where there is UI), test list, "Out of scope / deferred".

## Done → next
Push, open the PR, note its number. Then continue with **`03-mijn-tests.md`**. If anything above is red, stop and report (see 00-README "How to run" step 3).
