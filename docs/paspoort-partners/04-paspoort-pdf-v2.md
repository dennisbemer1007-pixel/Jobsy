# 04: DNA-paspoort PDF v2: recruiter page, page 2, bilingual, co-branding, QR, Lobsy-geverifieerd

**Stacked.**
- Branch: `cursor/paspoort-partners-4` from `cursor/paspoort-partners-3`.
- ONE PR into `acceptatie`, titled **"feat(paspoort): DNA-paspoort PDF v2 (recruiter page, bilingual, co-branding, QR, Lobsy-geverifieerd)"**.
- Rules: see README.
- Gated by `PassportPdfV2Enabled`. Co-branding also needs `PassportPartnersEnabled`.
- **Likely split:**
  - **04a:** sector choices, strengths, tips, translations, completeness, `PassportDocument`, model builder, candidate UI
  - **04b:** renderer, download endpoint and dialog

Mockups in `docs/mockups/paspoort-partners/` (src `src/a.html`, `b.html`, `c.html`, `f.html`; generator `src/pages.py`):
- `a-recruiterpagina-p1.png` (p1)
- `b-pagina2-sectoren-profiel.png` (p2)
- `c-tweetalig-pl-nl.png` (bilingual)
- `f-cobranded-uitzendbureau.png` (co-branded p1)

Deviations, where the spec wins (README):
- no housing
- no percentages, bars or fit bands anywhere on the PDF
- tips only after approval
- `ar` falls back to the partner language

## Goal
A new, separate document **"DNA-paspoort"** that adds strongly to a CV and is useful to staffing agencies (decisions 1–6):
- **Page 1 = the recruiter page:** everything an agency needs on one A4.
- **Page 2 = depth:** the candidate's sectors with "waarom het bij mij past", the 4 DNA layers with dates, how I like to work, tips the candidate approved, own words, and what "Lobsy-geverifieerd" means.
- Bilingual: candidate language + NL/EN.
- A QR code to the live/verification page (the page itself comes in step 05).

The existing **Lobsy-CV stays** for applications (employer download after Accept). The passport download switches to v2 when the flag is ON.

## Facts (checked on `85a43263`)
- PDFs use QuestPDF (`QuestPDF` 2025.12.4, Community license) and QRCoder 1.8.0 (already used for sales QR PNGs).
- Brand logo: `IPlatformCompanySettingsService.GetBrandLogoPng()` (`Jobsy.Infrastructure/Assets/lobsy.png`).
- Test completion: `CompletedAtUtc` on `CandidateCompetency`, `CandidateCareerInterest` (Holland code, RIASEC, `CompassJson`), `CandidateCulturePersonalityProfile` and `CandidateValuesProfile`. The Web-side `CandidateDnaViewBuilder` (`Jobsy.Web/Components/Candidate/Dna/`) builds labels for the UI.
- Sectors: `OccupationTaxonomy` (`Jobsy.Core/Rules/OccupationTaxonomy.cs`), sector → family → title. Sectors include zorg, onderwijs, horeca, logistiek, techniek, groen (tuinbouw: `greenhouse` "Medewerker tuinbouw"), retail, it, admin, lab, dieren, luchtvaart.
- WhoAmI: story + keywords, opt-in `CandidateWhoAmIProfile.IncludeOnCv`, read by `WhoAmIService.GetCvAttachmentAsync`.
- Machine translation: `ITranslationService.TranslateAsync` (OpenAI; stub fallback).
- `ReportLanguage` only knows nl/en. Lato (QuestPDF default) renders Polish/Romanian diacritics, not Arabic.
- Public base URL: `PlatformFeatureSettings.PublicWebBaseUrl`.

## Scope

### Candidate choices (JSON in `PreferencesJson`, no migration)
**Sectors** (`PassportSectors`, max 3, ordered):
- Each item is `{ Code, ReasonCodes[] (max 3), OwnReason? (max 80) }`.
- New `Jobsy.Core/Passport/PassportSectorSuggestions.cs` suggests up to 5 sectors from:
  - `CompassJson` occupations → `OccupationTaxonomy.Resolve(title).Sector`
  - `Roles` and `Employers[].Role` resolved the same way
- Suggestion ranking is internal and only shown to the candidate as order, never as a number.
- **Reason codes** are deterministic facts:
  - `holland-{letter}` (top Holland letters)
  - `experience-{years}` for a resolved role in that sector ("2,5 jaar ervaring als …")
  - `certificate-{name}` (keyword map: VCA, heftruck/reachtruck → logistiek/techniek; HACCP → horeca; BHV → any)
  - `strength-{dimension}` (candidate's chosen strengths)
  - `pref-indoor` / `pref-outdoor` / `pref-physical` from step 02
- **Example roles:** up to 3 taxonomy titles in that sector (prefer the candidate's compass occupations).
- Sector display labels: new `PassportSectorCatalog` with label keys for 5 languages. `groen` shows as "Groen & (glas)tuinbouw".

**Strengths** (`PassportStrengths`, 3 competence dimension codes):
- Default is the top 3 of `CandidateCompetency`.
- The candidate may swap in any of their top 6.
- Rendered as label + one-line description, **no numbers**. The caption says "zelfinzicht uit de Lobsy-competentietest · door {voornaam} gekozen".

**Tips** "Zo haal je het beste uit {voornaam}" (`PassportTips`, `{ Codes[] (max 4), ApprovedAtUtc }`):
- Deterministic catalog `PassportTipCatalog` driven by culture/values dimensions (structure, team, autonomy, feedback, instruction style).
- No free AI text.
- Shown on p2 **only when `ApprovedAtUtc` is set**.
- Editing the codes clears approval.

**Free-text translation approval:**
- New entity **`PassportTextTranslation`**, migration `AddPassportDocuments` (shared with `PassportDocument` below).
- Fields: `UserId`, `FieldKey` (`AboutMe`, `SectorReason:{code}`), `SourceLanguage`, `TargetLanguage`, `SourceHash` (SHA-256), `Text`, `CreatedAtUtc`, `ApprovedAtUtc`.
- The PDF uses a translation **only if approved and `SourceHash` still matches**. Otherwise it shows the original text with a small language tag (e.g. "PL").
- Approved text carries the label "automatisch vertaald".

### Share-readiness (decision 1)
`PassportCompletenessRules.IsShareReady(user, prefs, tests)` is true only if all of these hold:
- hours (min or max) set
- availability matrix has ≥ 1 slot, or `FlexibleTimes`
- ≥ 1 spoken language
- `DutchLevel` set
- transport (`PreferredTransport` or a licence) set
- `WorkRegion` set
- ≥ 1 completed test

`AvailableFromDate = null` keeps meaning "Direct".

If not ready:
- The PDF still downloads for own use, with a coral "Concept, nog niet compleet" ribbon and **no QR**.
- Sharing (05) and partner access (07) require ready.
- The passport UI shows a checklist with links to the missing fields.

### Lobsy-geverifieerd (decision 6)
`PassportVerificationRules.Evaluate(...)` returns `{ IsVerified, TestDates[4], EmailVerified, PhoneVerified, PhoneRequired }`.
- `IsVerified` = all 4 tests completed + `EmailVerifiedAtUtc` set + (`PhoneVerifiedAtUtc` set **if** `PhoneVerificationEnabled`).
- The badge text is "**Lobsy-geverifieerd**" + "4/4 DNA-tests afgerond · {date of the last test}". Use this exact name.
- The p2 box lists exactly what was checked (tests with dates, e-mail, telephone if required) and what was **not**: identity/werkvergunning, diploma's/certificaten, referenties.
- Not verified → no badge; p1 shows "{n}/4 DNA-tests afgerond" as plain status.

### `PassportDocument` (migration `AddPassportDocuments`)
Fields:
- `Id`
- `PublicId`: 12 chars from the short-code alphabet, unique, displayed `XXXX-XXXX-XXXX`, random, not derived from the user id
- `CandidateUserId`, `GeneratedAtUtc`
- `PrimaryLanguage`, `SecondaryLanguage`
- `IncludesPage2`, `IncludesContact`
- `PassportPartnerId` (null; co-branded)
- `ShareLinkId` (null; FK in 05)
- `ContentHash`: SHA-256 of the canonical JSON of the rendered model, used by step 05 to show "gewijzigd sinds je PDF"

A row is written on every download. Delete on anonymize. Export lists PublicId + date.

### Model builder (`Jobsy.Core/Passport/PassportDocumentModel.cs` + factory in Infrastructure)
Properties are a **whitelist**. A reflection guard test fails when a property is added without updating the whitelist.

**Allowed:**
- full name and initials
- `WorkRegion`, PassportMemberNumber (`LB-xxxxx`), open-for-work
- available-from, hours
- derived shifts, per day-part:
  - `ja`: selected on ≥ 2 days
  - `in overleg`: 1 day or `FlexibleTimes`
  - `nee`: otherwise
- transport: licences, preferred transport, max travel minutes, own car
- contract preferences
- languages + Dutch level + explain line
- shareable work preferences
- employer preferences, only if `ShareEmployerPreferences`
- chosen strengths (labels), WhoAmI keywords (only if `IncludeOnCv`)
- chosen sectors (label, reasons, example roles)
- experience: role, employer, period; max 3 on p1, all on p2
- certificates (+ year), education
- own words (`AboutMe`, approved translation)
- 4 DNA layer summaries: label text + completion date, no numbers
- culture "how I like to work" lines (text only)
- approved tips
- verification result
- contact (e-mail/telephone/WhatsApp-OK) **only when allowed by the context**:
  - own download: candidate toggle
  - partner context: link `ContactConsentAtUtc`
  - share link: `ShowContact` (05)
- co-brand (partner display name, logo bytes, consent date)
- `PublicId`, QR url, generated date, languages

**Never** (guard test asserts that none of these names or types are reachable): `DateOfBirth`, `AgeYears`, nationality, photo, BSN, health, `HomeAddress`, postcode, coordinates, `CandidatePrivatePreferences`/dislikes, answers JSON, any `*Percent`/score/fit/rank.

### Renderer `IPassportPdfService` (`Jobsy.Infrastructure/Services/Passport/PassportPdfService.cs`)
- A4, QuestPDF, Lobsy styling per mockups: purple `#5b2a9a`, magenta `#b5309a`, coral `#f5503a`, ink `#23173a`, soft purple `#f4effb`, soft coral `#fff1ec`, gradient rule under the header.
- **p1** per mockup a:
  - header: logo + "DNA-paspoort · recruiterpagina" + badge
  - hero: initials avatar, name, AFGELEID tagline from WhoAmI keywords or empty, region/open-for-work/passport-number chips
  - 4 cards: available, shifts, transport, contact
  - languages + work preferences
  - strengths (labels)
  - "Sectoren die bij mij passen" (3 compact cards, no numbers)
  - "Zoekt" line (roles + contract preferences)
  - experience & papers
  - footer:
    - QR (78 pt) + "Scan voor de live, geverifieerde versie" + `lobsy.nl/v/XXXX-XXXX-XXXX` + generated date + page x/y
    - privacy line "Bewust níet op dit paspoort: geboortedatum, foto, nationaliteit, BSN, gezondheid."
    - AI-Act line "Gespreksinput, geen beoordeling: Lobsy geeft bureaus en werkgevers geen score, ranking of automatische selectie van kandidaten."
- **p2** per mockup b. It is optional (toggle) and only rendered when ≥ 1 test is complete.
- **Bilingual** per mockup c:
  - primary language = candidate UI language if in {nl, en, pl, ro}, else the secondary language
  - secondary language ∈ {nl, en} (default nl)
  - labels render primary + small secondary; catalog values likewise; free text per the translation rules above
  - primary == secondary → monolingual
  - Strings live in `Jobsy.Core/Passport/PassportPdfStrings.cs` (nl/en/pl/ro/ar; ar is used by web surfaces later; parity test).
- **Co-branding** per mockup f, only when `PassportPartnerId` is a **consented, active** link of this candidate and the partner has a logo:
  - header "Lobsy × {logo}" + "in samenwerking met {DisplayName}"
  - ribbon "Gedeeld met {DisplayName} ({Uitzendbureau|Werkgever}) op {consent date} met uitdrukkelijke toestemming van {voornaam} · intrekbaar · {DisplayName} ziet alleen dit paspoort, geen testantwoorden"
- **QR:** QRCoder PNG of `{PublicWebBaseUrl}/v/{PublicId}` (step 05 adds `?s=`). Not rendered when not share-ready.
- Cards use `ShowEntire()` and titles `EnsureSpace` (lessons from 01). Any profile renders in ≤ 2 pages for p1 + p2. Long lists truncate with "+n meer op de live-versie".
- File name: `Lobsy-DNA-paspoort-{initials}-{yyyyMMdd}.pdf` (initials rule from 01).

### API + UI
- `GET api/me/passport.pdf?primary=&secondary=&page2=&contact=&partnerId=`:
  - `RequireCandidate`, rate limit `public-pdf`, `[RequiresFeature(PassportPdfV2)]`
  - `partnerId` is validated through `IPassportPartnerService` (own, consented, not revoked/suspended)
- **Passport card** (`PassportCard.razor` ~L129–134): when the flag is ON, the button becomes "DNA-paspoort (PDF)" and opens a dialog with:
  - language pair
  - p2 on/off
  - contact on/off
  - "Voor wie?" (none / one of my partners); partner options require `PassportPartnersEnabled`
  - translation preview + "Klopt, gebruik deze vertaling" per field
  - completeness checklist
- The Lobsy-CV button moves to the Proof tab only (`PassportProofTab.razor` ~L176–178) with its existing copy.
- **Fit tab** (`PassportFitTab.razor`): new blocks:
  - "Sectoren die bij mij passen": suggestions, pick + order up to 3, reason checkboxes + own line
  - "Mijn sterke punten op het paspoort": swap strengths
  - "Tips voor werkgevers": pick up to 4 + approve
- Copy everywhere: "Dit is zelfinzicht en gespreksinput, geen score."

## Tests
- **Unit:**
  - sector suggestions from taxonomy (incl. `greenhouse` → `groen`)
  - reason codes deterministic
  - strengths default/swap within top 6
  - tips approval cleared on edit
  - shift derivation table
  - completeness rules
  - verification rules: phone required only when the setting is ON; badge text exactly "Lobsy-geverifieerd"
  - translation used only when approved + hash matches
  - `PublicId` alphabet/length/uniqueness
  - model whitelist guard
  - "never" guard
- **PDF tests** (PdfPig from 01), fixture profile Marta (fictional):
  - p1 contains the name, region, available-from, languages, "Lobsy-geverifieerd" (when verified) and the QR url text
  - contains **no** "%", "match", "score", date of birth or age, "Huisvesting"
  - bilingual pl+nl shows both label languages
  - co-branded render contains the partner name and consent ribbon; render with a **revoked** link → 403 at the API, and the service refuses co-branding
  - not share-ready → "Concept" ribbon, no QR url text
  - ≤ 2 pages for a maximal fixture (8 employers, 8 certificates, long about-me)
- **bUnit:** dialog options; partner option hidden when the partners flag is OFF; Fit tab sector picker enforces max 3; tips approve button.
- **Playwright 390×844 + 1440** (soft-skip):
  - candidate opens the dialog and downloads; response is `application/pdf`
  - Fit tab picker works without overflow

## Success criteria
- With `PassportPdfV2Enabled` ON, candidates download the v2 passport per mockups a/b/c/f with no special-category data and no scores.
- Lobsy-geverifieerd is shown exactly per the rules. Bilingual and co-branded variants work. The QR points to `/v/{PublicId}`.
- Flag OFF → today's behaviour (Lobsy-CV). Release build with 0 warnings, full tests green.

## Out of scope
- The `/v` page itself (05).
- Share links (05).
- Partner downloads from the portal (07, re-using this renderer).
- RTL/Arabic PDF.
- Housing.
