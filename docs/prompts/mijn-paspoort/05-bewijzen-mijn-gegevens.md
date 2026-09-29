# 05 · Mijn Paspoort, Phase 4: tabs Bewijzen + Mijn gegevens

> Read `00-README.md` first: §0 rules apply, plus §F (`<FeatureVisible Feature="Employers">`). Execute this file only after the previous file's PR is open and green.

| | |
|---|---|
| Branch | `cursor/mijn-paspoort-4`, created from `cursor/mijn-paspoort-3` |
| PR | ONE PR into `acceptatie`, title `feat(paspoort): Bewijzen and Mijn gegevens tabs`. Body starts with `Stacked on #<PR of 04> (cursor/mijn-paspoort-3)` |
| Mockups | `pp-d5-bewijzen.png`, `pp-d6-mijn-gegevens.png`, `pp-m4-mijn-gegevens.png` |

> Size note: if the diff grows past ~1,500 lines, split it into **05a** (`cursor/mijn-paspoort-4a`: the extraction + Bewijzen) and **05b** (`cursor/mijn-paspoort-4b`, stacked on 05a: Mijn gegevens + removing the transitional rule). Each is ONE PR into `acceptatie` with a stacked-on line.

## 05.1 Extract the profile form (no duplicate logic)
- **Split `Profile.razor`** (1,921 lines) into section components under `Components/Candidate/ProfileSections/`:
  - `PersonalSection` (names, phone, WhatsApp, date of birth, address + geocoding)
  - `DevicesSection`
  - `PreferencesSection` (max travel, transport, interests, licenses)
  - `AvailabilitySection` (hours + presets)
  - `ExperienceSection` (employers)
  - `EducationSection`
  - `CertificatesSection`
  - `ReferencesSection` (max 3)
  - `CvSection` (own CV upload/replace/remove/download + Lobsy-CV)
  - `MotivationSection` (about me, default motivation)
  - `ConsentSection` (tests/AI consent accept, withdraw, and withdraw + delete results; talent-pool consent; parental consent under 16)
  - `DeleteAccountSection` (the `UnsubscribeDialog` flow)
- **One shared editor state** `CandidateProfileEditor` (Web, scoped per circuit) holds the model, dirty tracking, validation, the existing save calls and the save bar.
  - Reuse `CandidateProfileService` / the existing API calls; don't add endpoints.
  - `Profile.razor` becomes a thin host of these sections with **identical markup and classes**, so existing tests keep working (`AccountUnsubscribeTests`, `CandidateProfileServiceTests`, `AccountDeleteCopyTests`, Playwright profile flows).
- **Localize** the hardcoded Dutch consent strings on the way (`Profile.razor` ~L585–630 and the toast texts ~L1607–1630).

## 05.2 Tab "Bewijzen"
- **Top strip "Je nieuwe schaal":**
  - A soft → hard segment bar: 8 segments in stepped tokens, filled = the number of proofs (employers + education + certificates + references + own CV), capped at 8.
  - Label "{n} bewijzen · steeds steviger".
  - Right side: the next missing item (for example "Nog 1 recensie en 1 certificaat, dan is je schaal hard."). This is derived in `ProofStrengthRules`. No storage.
- **3 cards:**
  - "Ervaring": timeline + "Werkgever toevoegen" → `ExperienceSection` in an inline editor or sheet
  - "Opleiding & certificaten": level pills + timeline + "Certificaat toevoegen"
  - "Recensies & CV": references (max 3, with "Recensie toevoegen (max 3)") + own CV with "Vervangen" / "Download"
  - Editing reuses the section components (05.1) in a `filter-sheet` on mobile and inline on desktop. No second form.
- **Note:** "Je Lobsy-CV (PDF) maken we automatisch van je bewijzen en je tests." + the existing download.

## 05.3 Tab "Mijn gegevens"
- **Quick switches** (row of 3):
  - "Beschikbaar voor werk" (`OpenForWork`): hidden when employers are OFF
  - "Anoniem in de talentpool" (talent-pool consent, "staat standaard uit"): hidden when employers are OFF
  - "Tests en AI-analyse": status pill "Aan · sinds {datum}" / "Uit", which opens Privacy
  - Switches save through the same editor/consent calls.
- **Accordions** (2 columns desktop, 1 mobile; one open at a time; `aria-expanded`/`aria-controls`; the row is a `<button>`):
  - Persoonlijk (incl. devices)
  - Voorkeuren & reistijd
  - Beschikbaarheid
  - Mijn motivatie
  - Privacy & toestemming (incl. parental consent)
  - Account verwijderen (`--danger` title, the existing confirm flow)
  - Each accordion hosts its section component (05.1).
- **Remove the transitional rule** (`PassportRedirects.ClassicTabsUntilPhase4`). With the flag ON, `/candidate/profile?tab=profile` now redirects to `?tab=data`, and the tabs `proof`/`data` render the real content.

## Tests
- **Extraction parity:**
  - bUnit renders `Profile.razor` sections with the same markup and classes as before (snapshot of key selectors)
  - the editor save round-trip
  - the consent actions call the same API methods
  - unsubscribe/delete still requires verification
- **`ProofStrengthRules`** table test.
- **Bewijzen:** the add flows open the shared section; references are capped at 3.
- **Mijn gegevens:**
  - the switches are hidden when employers are OFF
  - parental consent shows only under 16
  - "Account verwijderen" opens the existing dialog
- **Redirect:** with ON, `tab=profile` now goes to `data`.
- **Localization:** no hardcoded Dutch left in the moved sections (grep test on `ProfileSections/*.razor`).
- **Playwright:** both tabs at 1440×900 fit about 1 screen with the accordions closed; `m4` at 390×844; the full edit → save flow works in the paspoort and in the classic profile (flag OFF).

## Success criteria (all must hold before you open the PR)
- `Profile.razor` is a thin host of the `ProfileSections` components. Its markup, classes and every existing profile/unsubscribe test are unchanged with the paspoort flag OFF.
- **Bewijzen and Mijn gegevens:**
  - Both edit through the same sections and the same `CandidateProfileEditor`, with no second form or save path.
  - Consent, parental consent and account deletion behave exactly as before.
- The transitional rule is removed: with ON, `tab=profile` redirects to `?tab=data`.
- No hardcoded Dutch is left in the moved sections.
- **Werkgevers actief OFF:** the "Beschikbaar voor werk" and talent-pool switches are hidden.
- **Layout:** both tabs fit about one screen at 1440×900 with the accordions closed, and `m4` works at 390×844.
- `dotnet build` and `dotnet test` are green. Any existing test you changed has its reason in the PR body.
- The PR body has: stacked-on line, what and why, screenshots (where there is UI), test list, "Out of scope / deferred".

## Done → next
Push, open the PR, note its number. Then continue with **the final report (00-README step 4)**. If anything above is red, stop and report (see 00-README "How to run" step 3).
