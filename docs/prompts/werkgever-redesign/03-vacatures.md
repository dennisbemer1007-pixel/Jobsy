# 03 · Vacatures: one table for all roles, publication requests, plain-Dutch visibility options

> Read `00-README.md` first. §0 (terminology), §IA, §R and D4, D5, D7, D11 and D14 apply.

| | |
|---|---|
| Branch | `cursor/werkgever-redesign-3` from `cursor/werkgever-redesign-2` (stacked) |
| PR | ONE PR into `acceptatie`: `feat(werkgever): vacatures table with filters, bulk actions and publicatieaanvragen`. Stacked on #<PR of 02> (`cursor/werkgever-redesign-2`) |
| Mockups | `bm-d2-vacatures.png` (BM), `bm-d8-vacatures-vestigingsmanager.png` (VM) |
| Split seam (if too big) | 3a = table + tabs + filters; 3b = bulk actions + dialog copy |

**Goal.** One Vacatures page for BM, RM and VM, where the scope decides the data and the role decides the actions. Publicatieaanvragen are approved inline.

## 03.1 Today (verify first)
- `Pages/Werkgever/…Vacancies.razor` (moved in 01, 733 lines) uses these API calls:
  - `Api.GetManagedVacanciesAsync` → `GET api/vacancies/manage` (`RequireAdminOrEmployer`)
  - `ApprovePublishAsync` → `POST {id}/approve-publish`
  - `HighlightVacancyAsync` → `{id}/highlight`
  - `ExtendVacancyAsync` → `{id}/extend`
  - `DeactivateVacancyAsync` → `{id}/inactive`
  - contact preference / e-mail verification
- It reuses `PublishOptionsDialog`, `PushBomConfirmDialog` (`{id}/pushbom` + `/pushbom/preview`), `TokenTopUpDialog` and `RowActionsMenu`. Duplicate and reactivate exist as row actions (verify how).
- `VacancyStatus`: `Draft`, `Active`, `Archived`, `PendingApproval`, `Fulfilled`.
- Costs come from `GET api/tokens/costs` (`TokenSpendCost`).
- **No bulk endpoints** exist.

## 03.2 Page `/werkgever/vacatures` (redesign)
- `WgPageShell`:
  - Title "Vacatures"
  - Lead (BM "Alle vacatures van je {n} vestigingen." / RM "Vacatures in regio {x}." / VM "Vacatures van vestiging {x}.")
  - Actions: "Exporteren" (CSV of the current filter, client-side) and primary "Vacature plaatsen" (`WgAction RequiresWrite`)
- Card with `EntTabs`, URL `?tab=`:

  | Tab | Contents |
  |---|---|
  | Alle | all vacancies |
  | Actief | `Active` |
  | Wacht op goedkeuring (`wacht`) | `PendingApproval` |
  | Concept | `Draft` |
  | Verloopt binnenkort (`verloopt`) | **only if an end date exists**, see §0 |
  | Gesloten | `Archived` + `Fulfilled` |

  Each tab shows its count.
- `EntFilterBar`:
  - search "Zoek op titel of nummer"
  - Vestiging (BM/RM; multi-select, defaults to the scope chip)
  - Regio (BM with regions)
  - Type (Fulltime/Parttime/Flex/Bijbaan from the existing vacancy type field)
  - Geplaatst door (BM)
  - VM: Salaristabel instead of Vestiging/Regio
  - "Kolommen" (show/hide optional columns, persisted in `localStorage`)
  - Filters are URL-synced (`?q=&vestiging=&type=`).
- Info note (`EntImpactNote`, info tone):
  - BM when publication requests exist: "{n} vestigingen vragen tokens aan om te publiceren. Goedkeuren boekt de tokens af van het centrale saldo ({saldo})." plus the link "Alleen aanvragen tonen" (→ `tab=wacht`).
  - VM: "Vestiging {x} heeft **{saldo} tokens**, toegewezen door de bedrijfsmanager. Publiceer je boven dit saldo, dan gaat de vacature als aanvraag naar de bedrijfsmanager." plus "Tokens bekijken". This appears only when tokens are managed by the enterprise (today's `TokensManagedByEnterprise` rule).
- `EntDataTable` (density D14), columns:
  - ☐
  - **Vacature**: title bold, sub-line `{vestiging · }type` plus an info line ("Aanvraag {naam} · {n} tokens" / "Nog {n} velden invullen" when the existing completeness data gives it / "Klaar om te publiceren")
  - **Status**: `status-pill` + label. Actief = ok, Wacht op goedkeuring = warning (VM: "Wacht op bedrijfsmanager"), Concept/Gesloten = neutral, Vervuld = neutral "Vervuld"
  - **Sollicitaties**: a new-count badge + total
  - **Weergaven**: only if `manage` returns views
  - **Online tot**: only with an end date
  - **Zichtbaarheid**: pills "Uitgelicht" (star) and "Pushbericht" (zap)
  - **Actions**
  - Pending rows get the `hl` highlight; selected rows get `sel`.
- Row actions (one visible button + `RowActionsMenu`), following §R:

  | Status | BM | VM | RM |
  |---|---|---|---|
  | Wacht op goedkeuring | **"Goedkeuren"** (primary, inline `approve-publish`, confirm through `LobsyFriendlyDialog` showing the cost + the central balance after), menu: Afwijzen (only if a reject path exists today; otherwise leave it out) | "Aanvraag intrekken" (only if a withdraw path exists; otherwise nothing) | nothing |
  | Concept | "Afmaken" | "Afmaken" | view |
  | Actief | Sollicitaties (inbox icon → `/werkgever/sollicitaties?vacature={id}`) + menu: Bewerken, Zichtbaarheid & kosten, Verlengen, Uitlichten, Pushbericht naar kandidaten, Dupliceren, Deactiveren | same, own vestiging only | Sollicitaties (view) |
  | Gesloten | "Dupliceren" / "Heropenen" (reactivate, as today) | same | nothing |
- **RM** sees no checkbox column, no bulk bar and no write menu items. Clicking the title opens the read-only vacancy view (existing detail).

## 03.3 Bulk actions (`EntBulkBar`)
- Actions on the selected rows:
  - "Verlengen · {n} tokens"
  - "Uitlichten · {n×cost} tokens"
  - "Dupliceren"
  - "Deactiveren"
  - Costs come from `api/tokens/costs` and are never hardcoded.
- Implementation: the client calls the **existing per-vacancy endpoints** sequentially (max 50 selected), shows progress, then a summary toast "{ok} gelukt, {fail} niet gelukt" with the reasons (e.g. "onvoldoende tokens"). No new bulk endpoint. Each call is server-authorized as today.
- Before any token-spending bulk action: a confirm dialog with the total cost, the current balance and the balance after. When the balance is insufficient: BM → `TokenTopUpDialog`; VM → "Vraag tokens aan bij je bedrijfsmanager" (06 link; text until then).

## 03.4 Dialog copy (terminology, UI text only)
- `PublishOptionsDialog` title → "Zichtbaarheid & kosten"; "Highlight" → "Uitlichten"; "PushBom" → "Pushbericht naar kandidaten"; "Extend" → "Verlengen".
- `PushBomConfirmDialog` title "Pushbericht naar kandidaten versturen?" with the recipient count from `/pushbom/preview`.
- Every price label reads from costs ("Uitlichten · 2 tokens").
- Code identifiers stay the same.

## 03.5 VM publishing (D7)
- Unchanged server path: a VM whose tokens are managed by the enterprise creates `PendingApproval`.
- In `CreateVacancy` (moved in 01): final-step copy "Je vestiging heeft {saldo} tokens. Publiceren kost {cost}. Is je saldo te laag, dan gaat de vacature als publicatieaanvraag naar de bedrijfsmanager."
- The page itself is **not** redesigned in this stack (1.800 lines). Only strings, terminology and the new URLs.

## Tests
- `api/vacancies/manage` scope: BM all, RM region, VM own. Add a `companyIds` filter param if missing, intersected server-side.
- Matrix rows:
  - RM → 403 on create / update / publish / approve-publish / highlight / pushbom / extend / inactive
  - VM → 403 on approve-publish and on a vacancy of a sibling vestiging
  - BM → 2xx
- bUnit: tabs + counts; the "Verloopt binnenkort" tab is absent without an end date; BM pending row shows "Goedkeuren"; VM pending row shows "Wacht op bedrijfsmanager" and no Goedkeuren; RM has no checkboxes or bulk bar and the disabled primary; bulk bar costs from mocked costs; the confirm dialog shows balance after; the partial failure summary.
- Terminology: the dialogs render no "PushBom"/"Highlight" in nl.
- Playwright: BM approves a pending vacancy inline; VM at 1440 sees the balance note.

## Success criteria
- One page serves all three roles and matches d2/d8.
- Approve-publish works inline for BM only.
- Bulk actions show the true token cost and use the existing endpoints.
- No English token jargon in the UI.

## Done → next
Push, open the PR, note its number. Continue with **`04-sollicitaties.md`**. If anything is red, stop and report.
