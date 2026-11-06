# 01: Hotfix: Lobsy-CV PDF without date of birth/age and without the AI "Wie ben ik" page, banners once, no split cards

**Standalone hotfix (not stacked).**
- Branch: `cursor/paspoort-partners-hotfix` from `origin/acceptatie`.
- ONE PR into `acceptatie`, titled **"fix(lobsy-cv): drop date of birth/age and the AI 'Wie ben ik' page from CV PDF, keep banners and cards on one page"**.
- Rules: see README (never merge/deploy, 0 warnings, full tests, draft + stop when red).

Mockups (today's output, rendered with the real service and fictional data): `docs/mockups/paspoort-partners/huidig-lobsy-cv-voorbeeld.pdf`, `huidig-lobsy-cv-1.png`, `-2.png`, `-3.png`.

## Why (serious: on every download, for candidate and employer)
`Jobsy.Infrastructure/Services/LobsyCvPdfService.cs` renders the only passport/profile download (`GET api/me/lobsy-cv.pdf`, `MeController.DownloadMyLobsyCv` ~L758–797). It also renders the employer's application CV (`ApplicationsController` ~L461 and ~L513, via `LobsyCvModelFactory.FromApplicationForDownload`).

1. **Date of birth + age are printed.**
   - In the hero: `FormatDateOfBirthAndAge(...)` ~L165–173 prints "12 mei 1994 · 32 jaar". Age is shown even when contact details are hidden.
   - Again in the contact block: "Geboortedatum" / "Leeftijd" ~L224–245.
   - That is not needed for a first impression. It invites age discrimination (AWGB/WGBL) and conflicts with AVG data minimisation.
   - Dennis decided (beslissing 3): **no date of birth/age on the passport/CV**.
2. **Header banners repeat on every page.** The "Eigen CV toegevoegd" / "Persoonsprofiel bijgevoegd" banners (~L133–150) sit inside `page.Header()`, which QuestPDF repeats on each page. Page 2 of the sample is a full header + banner + one orphan line.
3. **Cards split across pages.** Work-experience cards (~L354–389) and the certificates block (~L391–411) can break mid-card: the employer name lands on page 1 and its description on page 2. Section titles (`Section(...)` ~L607–620, "Werkervaring", "Certificaten & cursussen") can be orphaned at a page bottom.
4. **AI "Wie ben ik" page.**
   - When `CandidateWhoAmIProfile.IncludeOnCv` is on, the PDF gets an extra page with the LLM "Wie ben ik" story and score bars (`RenderWhoAmIPage` ~L432–439, banner "Persoonsprofiel bijgevoegd" ~L142–146).
   - Sources:
     - candidate download: `MeController` ~L779 `GetCvAttachmentAsync`
     - employer application CV: snapshot `Application.SnapshotWhoAmIJson`, written in `ApplicationsController` ~L925–926 and read by `LobsyCvModelFactory` ~L257
   - Under the AI Act this is LLM output judging the person in a selection process (`ai-act-beoordeling.md` §5.3).
   - Dennis decided (decision 22, point 10): **remove it from the Lobsy-CV download.**
5. **File name.** `Initials` (~L696–711) takes the first char of each name part. A name like `Marta Kowalska (fictief)` yields `Lobsy-CV-MK(-20261003.pdf`.

## Scope
- **Remove DOB and age from the PDF entirely**, in both renders (live profile and application snapshot):
  - Delete the hero age line and the DOB/age row in the contact block.
  - Delete `FormatDateOfBirthAndAge`.
  - The "Contactgegevens" block keeps e-mail, telephone and the WhatsApp line.
- **Keep the model fields** `LobsyCvModel.DateOfBirth` / `AgeYears` and the snapshot columns (`Application.SnapshotDateOfBirth`, `CandidateAgeYears`).
  - The employer applicant screens use age for youth-wage tables (`Applicants.razor` ~L841, `MinimumWageRate`). Hotfix **01b** replaces the exact age there with a "jeugdloon van toepassing" indicator, so don't change them here.
  - Add an XML doc comment on the two model fields: "Not rendered on the PDF (AVG/age discrimination; beslissing 3)."
- **Remove the AI "Wie ben ik" page from every Lobsy-CV render** (candidate download and employer application CV):
  - Delete `RenderWhoAmIPage` and its call, and the "Persoonsprofiel bijgevoegd" banner.
  - `MeController` no longer calls `GetCvAttachmentAsync` for the CV.
  - `ApplicationsController` stops writing new `SnapshotWhoAmIJson`; store null for new applications.
  - `LobsyCvModelFactory` ignores `SnapshotWhoAmIJson`.
  - Remove `LobsyCvModel.WhoAmI`, or leave it unused with `[Obsolete]`, whichever is fewer lines.
  - **Keep the data:**
    - existing `SnapshotWhoAmIJson` values stay in the DB (privacy export/anonymize unchanged)
    - `CandidateWhoAmIProfile.IncludeOnCv` and its API stay, with no PDF effect
    - the candidate still sees WhoAmI in the app
  - If any UI offers "Voeg dit persoonsprofiel toe als bijlage bij mijn Lobsy-cv" (`WhoAmI.AttachCv`), hide it. At `85a43263` no component renders it.
- **Banners only on page 1:** move both banners from `page.Header()` to the first items of `page.Content()`. The repeating header keeps only the brand band + coral rule.
- **No split cards:**
  - Wrap each work-experience card and each certificate row in `.ShowEntire()`.
  - Put `.EnsureSpace(…)` before each section title (`Section`, "Werkervaring", "Certificaten & cursussen") so the title moves with its first item (~60–80 pt).
  - Keep the availability/location row (`split`) together (`ShowEntire`).
- **File name:** initials from parts whose first char `char.IsLetter`, upper-invariant, max 3, fallback `XX`. `Marta Kowalska (fictief)` → `MKF`.
- **No visual redesign.** Colours and copy stay as they are; v2 is step 04.

## Files to touch
- `Jobsy.Infrastructure/Services/LobsyCvPdfService.cs`: L43–48, L107–151, L432–end of `RenderWhoAmIPage`, L157–176, L203–253, L344–411, L607–620, L637–650, L696–711.
- `Jobsy.Core/Contracts/LobsyCvContracts.cs`: doc comments; `WhoAmI` removed/obsolete.
- `Jobsy.Core/Contracts/LobsyCvModelFactory.cs` (~L257), `Jobsy.Api/Controllers/MeController.cs` (~L779), `Jobsy.Api/Controllers/ApplicationsController.cs` (~L925–926): stop attaching WhoAmI.
- `Jobsy.Tests/LobsyCvPdfServiceTests.cs`: keep green. The two model assertions on DOB/age (~L54–55, ~L308–309) stay, because the model still carries them.
- `Directory.Packages.props` + `Jobsy.Tests/Jobsy.Tests.csproj`: add **`UglyToad.PdfPig`** (MIT) as a **test-only** package for text/page assertions. Pin the version centrally. No runtime reference.

## Tests
New file `LobsyCvPdfLayoutTests.cs`, using PdfPig to extract text per page:
- **No DOB/age text**, for a live profile and an application snapshot with `dateOfBirth` set:
  - no page contains "Geboortedatum", "Leeftijd", the formatted date ("12 mei 1994") or the `"{age} jaar"` pattern
  - the e-mail and telephone still appear when `IncludeContactDetails` is true
- **Banners once:** with `HasUploadedOwnCv = true` plus a long profile (6 employers with descriptions, 5 certificates) that forces ≥ 2 CV pages, "Eigen CV toegevoegd" occurs exactly once (page 1).
- **No WhoAmI page:** for a candidate with `IncludeOnCv = true` and a story containing `ZZ-AI-MARKER-WHOAMI`, and for an application whose `SnapshotWhoAmIJson` holds that story:
  - no page contains the marker, "Wie ben ik" or "Persoonsprofiel bijgevoegd"
  - the page count equals the CV pages only
  - update the existing WhoAmI-page tests in `LobsyCvPdfServiceTests` accordingly
- New applications store `SnapshotWhoAmIJson = null`.
- **No split card:** for each employer, the employer name and its description are on the **same** page.
- **No orphan title:** "Werkervaring" and "Certificaten & cursussen" are never the last text line of a page (before the footer).
- **File name:** `BuildFileName` for `"Marta Kowalska (fictief)"` returns `Lobsy-CV-MKF-yyyyMMdd.pdf`; for `"  "` it returns `Lobsy-CV-XX-…`.
- Existing `LobsyCvAccessRulesTests` and `LobsyCvPdfServiceTests` stay green.

## Success criteria
- No Lobsy-CV PDF (candidate download or employer application CV) shows a date of birth, an age or the AI "Wie ben ik" page. The existing data is untouched.
- Banners appear only on page 1. Cards and section titles never split across pages. The file name contains only letters.
- Release build with 0 warnings, full test suite green.

## Out of scope
- Employer applicant screens showing age (handled in 01b).
- Any redesign of the Lobsy-CV (step 04 builds the new passport PDF).
- Changes to the WhoAmI feature for candidates in the app.
