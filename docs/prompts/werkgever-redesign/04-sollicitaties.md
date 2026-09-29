# 04 · Sollicitaties: pipeline per vacancy, candidate drawer with privacy stages, list view

> Read `00-README.md` first. §0 (privacy, terminology), §R and D4 and D11 apply.

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-4` from `cursor/werkgever-redesign-3` (stacked) |
| PR | ONE PR into `acceptatie`: `feat(werkgever): sollicitaties pipeline and candidate drawer`. Stacked on #<PR of 03> (`cursor/werkgever-redesign-3`) |
| Mockups | `bm-d3-sollicitaties-pipeline.png` (desktop pipeline + drawer), `bm-m2-sollicitatie.png` (mobile candidate) |
| Split seam (if too big) | 4a = pipeline + list; 4b = drawer + mobile |

**Goal.** Managers see, per vacancy, where every candidate stands, and act in one click. **What they may see follows the existing privacy stages.**

## 04.1 Today (verify first)
- The moved `Applicants.razor` (622 lines) uses these API calls:
  - `Api.GetApplicationsAsync` → `GET api/applications` (`companyId`, `vacancyId`, `page`, `pageSize`; `RequireAdminOrEmployer`)
  - `ReactToApplicationAsync` → `POST {id}/react` (Accepted or Rejected; `JobsyRoles.ApplicationReactRoles` = BranchManager, EnterpriseManager, Intermediary, Admin, so **RM is already excluded**)
  - `MarkEmployerContactAsync` → `POST {id}/contact` (sets `EmployerContacting`)
  - `FulfillVacancyAsync` → `POST vacancies/{vacancyId}/fulfill/{applicationId}` (sets `Hired`)
  - `DownloadApplicationLobsyCvPdfAsync` / `DownloadApplicationUploadedCvAsync`
- `ApplicationStatus`: `Pending`, `Accepted`, `Rejected`, `EmployerContacting`, `Hired`, `FilledElsewhere`, `Withdrawn`.
- Privacy: `LobsyCvAccessRules.IsPiiRevealed`, `IsDirectContactRevealed`, `CanEmployerDownloadCv(status, emailVerifiedAt)`. **The API already strips PII per stage. Keep that as the only source.**
- `Application.CreatedAt` and `RespondedAt` exist. Check whether an employer note field exists (§0 known differences: "Interne notitie").

## 04.2 Page `/werkgever/sollicitaties` (redesign)
- `WgPageShell`: title "Sollicitaties", lead "Per vacature, van nieuw tot aangenomen."
- Filter row:
  - **Vacature** select (search; defaults to `?vacature=` or the vacancy with the most pending items)
  - **Vestiging** (BM/RM)
  - toggle "Alleen > 48 uur" (`?filter=overdue`, linked from Te doen)
  - segmented view switch **Pijplijn · Lijst** (`?view=`, persisted)
- **Pijplijn** (≥ 1024): 5 columns, each header showing the label, a count and a hint:

  | Column | Status | Hint | Card shows |
  |---|---|---|---|
  | Nieuw | `Pending` | "anoniem" | "Kandidaat #{kort-id}", match %, 3 facts (reistijd, beschikbaarheid, uren/week, from the existing DTO), age ("3 dagen · te laat" in `--danger` when > 48 h) |
  | Geaccepteerd | `Accepted` | "naam + cv" | initials avatar, name, match, 2 facts, "Sinds {datum}" |
  | Uitgenodigd | `EmployerContacting` | | same + "Uitgenodigd {datum}" |
  | Aangenomen | `Hired` | "contact" | same + "Contact zichtbaar" |
  | Afgewezen | `Rejected` + `FilledElsewhere` + `Withdrawn` | | one summary card "{n} afgewezen · {m} elders aan de slag · {k} teruggetrokken", which opens the list view filtered to those statuses |

  - **No drag-and-drop.** Stage changes go through the drawer buttons only (explicit, accessible, server-validated).
  - Columns scroll vertically on their own, max 50 cards each, then "Toon alle {n}" (→ list view).
  - The names shown are exactly what the API returns for that stage. **Never** compute a display name client-side.
- **Lijst**: an `EntDataTable` (density D14):
  - columns Kandidaat (anonymous or name, per the API), Vacature, Vestiging, Status pill, Match, Gesolliciteerd, Eerste reactie
  - sortable, paged with `EntPager` through the existing `page`/`pageSize`
  - bulk "Afwijzen" only for BM/VM, with a confirm dialog and a sequential loop as in 03
- Mobile < 1024: the list view only (cards), grouped by status with chips; tap → the full-screen candidate page (04.4).

## 04.3 Candidate drawer (`EntDrawer`, right 460 px)
- Header: avatar (initials, or a neutral user icon while anonymous), name or "Kandidaat #{id}", sub-line "{vacature} · {vestiging} · gesolliciteerd {datum}".
- **Stepper** Nieuw → Geaccepteerd → Uitgenodigd → Aangenomen (done / current / upcoming; `aria-current="step"`).
- Tabs: Profiel · Motivatie · Tijdlijn (Tijdlijn = CreatedAt, RespondedAt and status changes that already exist; otherwise just these two).
- **Profiel:**
  - "Match {n}% met deze vacature"
  - key/value facts (Reistijd, Beschikbaar, Rijbewijs, Opleiding, Ervaring), only fields the DTO has
- **"Wat je ziet"** box (always visible, check or lock icon per row), driven by `LobsyCvAccessRules` for the current status:
  - ✓ "Anoniem profiel, match en motivatie"
  - ✓ or lock "Naam en Lobsy-cv (na accepteren)"
  - ✓ or lock "E-mail en telefoon: zichtbaar na aanname"
  - The rows use the existing check and lock icons, not emoji.
- **Interne notitie:** only if the field exists (§0).
- Footer, one primary action per stage:

  | Stage | Primary | Also |
  |---|---|---|
  | Nieuw | "Accepteren" (`react` Accepted) | outline-danger "Afwijzen" (`react` Rejected, confirm) |
  | Geaccepteerd | "Uitnodigen" (`contact`) | "Lobsy-cv" download (when `CanEmployerDownloadCv`), "Afwijzen" |
  | Uitgenodigd | "Aannemen" (`fulfill`, confirm "Vacature als vervuld markeren?" when that's what the endpoint does; verify, and word it truthfully) | "Lobsy-cv", "Afwijzen" |
  | Aangenomen | none (contact block shown) | "Lobsy-cv" |

  - **RM:** no action buttons. Show `wg-readonly-hint` "Reageren doet de vestigings- of bedrijfsmanager." CV download follows the same stage rules as BM (D4) and keeps writing `PersonalDataAccessLog`.
  - **VM:** actions only for applications of their own vestiging (server-enforced).
- Focus trap, Esc closes and returns focus to the card. `?kandidaat={applicationId}` deep-links the drawer.

## 04.4 Mobile candidate page (`bm-m2`)
- Route `/werkgever/sollicitaties/{applicationId:guid}`: a full-screen page, also used as the drawer's deep link < 1024.
- Top bar with back arrow, "Kandidaat #{id}" (or name) and the vacancy · vestiging; header block with the status line ("Nieuwe sollicitatie", "3 dagen zonder reactie" in danger when > 48 h) + match pill.
- A 2×2 fact grid, Motivatie, Opleiding en ervaring.
- Privacy note "Naam en cv zie je na accepteren. Contactgegevens na aanname." (per stage).
- Sticky footer: "Afwijzen" (outline-danger) + primary per stage; hidden for RM.

## 04.5 Server
- Add filters to `GET api/applications` only if missing: `status` (multi), `overdueHours`, `branchIds` (intersected with accessible companies). No new controller.
- **Verify** `contact` and `fulfill` reject RM and a foreign VM (matrix rows). If either uses a looser attribute than `ApplicationReactRoles`, tighten it and list it in the PR.

## Tests
- Privacy: for each status, the DTO fields (name, e-mail, phone, city, cv flags) equal what `LobsyCvAccessRules` allows, for BM, RM and VM alike. A bUnit test proves the drawer renders "Kandidaat #…" for `Pending` even when a (mocked) name were present, i.e. the UI never shows more than the stage allows.
- Matrix rows: RM → 403 on `react`, `contact`, `fulfill`; foreign VM → 403; BM/VM own → 2xx; RM cv download follows the stage rules and writes `PersonalDataAccessLog`.
- bUnit: column mapping (incl. the Afgewezen summary), overdue marker, the stepper per stage, the footer actions per stage and role, the "Wat je ziet" rows per stage, list view sort/paging, the mobile page sticky footer hidden for RM.
- Terminology: no "Gematcht"/"Contact opgenomen" in nl on these pages.
- Playwright: BM accepts a Nieuw candidate → the card moves to Geaccepteerd and the name appears (API-driven); 390 candidate page shows the sticky footer.

## Success criteria
- Pipeline and drawer match d3; mobile matches m2.
- Privacy stages identical to today, proven by tests for all three roles.
- RM can look, not act; VM acts only within own vestiging.

## Done → next
Push, open the PR, note its number. Continue with **`05-organisatie-team.md`**. If anything is red, stop and report.
