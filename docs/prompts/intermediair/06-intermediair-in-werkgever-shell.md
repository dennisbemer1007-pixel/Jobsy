# 06. Intermediair in the werkgever shell: nav, role + scope chip, dashboard, Opdrachtgevers, vestigingen, team, tokens, mobile

Read `00-README.md` first. Branch `cursor/intermediair-6` from `cursor/intermediair-5`. **Re-run Dependencies check A first**; it decides where the pages live.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/intermediair-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Location only from KvK (no free location field anywhere), the opdrachtgever's identity never leaves the server in hidden mode, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/intermediair-6` |
| PR title | `feat(intermediair): bureau in the werkgever shell — role chip, opdrachtgevers scope, dashboard, Opdrachtgevers pages, mobile` |
| PR body starts with | `Stacked on #<PR 05> (cursor/intermediair-5)`, and which case of Dependencies A applied |
| Mockups | `im-d1-dashboard.png`, `im-d2-opdrachtgevers.png`, `im-d5-opdrachtgever-toevoegen-kvk.png`, `im-m1-dashboard.png` (and the shell parts of `im-d3`) |
| Split seam | **06a** = nav + chips + dashboard + Te doen + redirects (06.2–06.4, 06.8). **06b** = Opdrachtgevers list/detail + Vestigingen + Team/Tokens wiring + mobile (06.5–06.7, 06.9) |

## Goal
A bureau user works in the same shell as a bedrijfsmanager. The role chip reads "Intermediair" and the scope chip "Alle opdrachtgevers ▾". The sidebar has an Opdrachtgevers group, and the dashboard and Opdrachtgevers pages match d1/d2. Every data point comes from 01–05's endpoints; no new business logic in this file.

## 06.1 Today (verify first)
- Dependencies A result:
  - **present:** `WerkgeverNav.cs`, `WerkgeverLayout`, `EmployerScopeState`, `EntScopeChip`, werkgever 02 Te doen builder, 04 Sollicitaties, 06 Tokens
  - **absent:** today's `MainLayout` + `RoleNavCatalog.Intermediary` (~L97)
- `IntermediaryDashboard.razor` (`/intermediary`, "Bedrijvenoverzicht", uses 02.9 data now) and `Team.razor` (`/intermediary/team`, `EmployerInviteRules`: Intermediary invites only Intermediary colleagues).
- 01 drawer, 02 form step, 04 signals, 05 insights.

## 06.2 Nav (A present: `WerkgeverNav.cs`; A absent: `RoleNavCatalog.Intermediary`)
- The §IA items for the Intermediary role, in that order, with label = h1 = crumb:
  - **Opdrachtgevers** group with the item Opdrachtgevers + count badge (Active links)
  - **Organisatie:** Vestigingen, Team & rechten, **Bureauprofiel** (label override of Bedrijfsprofiel), Salaristabellen
  - **Tokens & facturen:** Saldo & kopen, Mutaties, Facturen (Verbruik per vestiging only with > 1 bureau vestiging)
  - **Meer:** Koppelingen (conditional as werkgever), Wervingsmateriaal
  - Sidebar footer: "Banenkaart bekijken"
- Crumb root = the bureau name ("Voorbeeld Flexwerk Westland › Opdrachtgevers").
- The **role chip** "Intermediair" (small gold-outline pill as in d1; tokens only) sits left of the scope chip, for the Intermediary only.
- Mobile bottom nav: Overzicht · Vacatures · Sollicitaties · Opdrachtgevers · Meer.
- Remove `Nav.Clients` "Klanten" and the flat intermediary catalog once unused (A present: empty catalog as werkgever D1).

## 06.3 Scope chip "Alle opdrachtgevers ▾"
- Extend `EmployerScopeState` (A present) or a small `IntermediaryScopeState` (A absent) with the scope kinds `AllClients`, `Client(id)` and (only with > 1 bureau vestiging) `Vestiging(id)`. `?scope=client:{id}` deep-links. A foreign/archived id falls back with the toast "Je hebt geen toegang tot die opdrachtgever."
- The chip menu: search box (≥ 8 links), "Alle opdrachtgevers {n}", the list (initials, name, gemeente), then the "Vestiging" section when applicable.
- Server: vacancy and applicant list endpoints accept `intermediaryClientId` as a filter, checked against the bureau (404 foreign). The chip only **narrows**.

## 06.4 Dashboard `/werkgever` (d1) + Te doen
- Header "Dashboard", sub "{bureau} · Vestiging {plaats} · {n} opdrachtgevers · laatste {period}", period tabs 7 d / 30 d / 90 d, **Exporteren** (CSV of the dashboard numbers, as werkgever), primary **Vacature plaatsen**.
- **KPI row** (`EntKpiCard`):
  - Actieve vacatures
  - Nieuwe sollicitaties
  - Gem. eerste reactie (only if werkgever 02 has it; otherwise leave it out and say so)
  - Aangenomen
  - Tokensaldo + "± {n} weken bij huidig verbruik" (werkgever 06 logic if present; otherwise just the balance)
- **Te doen** (max 5, "Alles bekijken"): werkgever 02's builder with the intermediary kinds from 04's signals (A present), or a card fed by `api/intermediary/signals` (A absent). Plus the werkgever kinds that apply (applications waiting > 48 h, tokens running low).
- **Wervingstrechter** (as werkgever 02, scoped).
- **Opdrachtgevers card:** table Opdrachtgever (initials, name, "{gemeente} · KvK …") · Live vac. · Sollicitaties · **Op de kaart** pill (Via onze vestiging / Echte werklocatie / Gemengd) → row link to the detail. Footer "Alle {n} opdrachtgevers". Info line: "Naam en adres van opdrachtgevers komen uit KvK. 'Via onze vestiging': kandidaten zien alleen {bureau}."
- **20–25 km info** (README §0 difference): in the vacancy list, a small info line per vacancy "Bijna 25 km van je vestiging", no Te doen item.

## 06.5 Opdrachtgevers `/werkgever/opdrachtgevers` (+ `/{clientId}`) (d2, d5)
- **Page intro:** "Bedrijven waarvoor je vacatures plaatst. Naam en adres komen altijd uit KvK." Actions **Exporteren** (CSV: name, KvK, vestigingsnummer, gemeente, status, live vacancies, default mode) and primary **"Opdrachtgever toevoegen via KvK"** (opens the 01 drawer).
- **List** (left, 360 px on desktop):
  - search "Zoek op naam, KvK of plaats", filters **Status** (Actief / Ter controle / Uitgeschreven / Gearchiveerd) and **Op de kaart** (Alle / Via onze vestiging / Echte werklocatie / Gemengd)
  - rows with initials, name, "{gemeente} · {n} live", count of applications
  - pager
- **Detail** (right):
  - header: initials, name, "KvK … · vestiging … · {gemeente}", status pill ("Gecontroleerd in KvK · {LastKvkCheckAtUtc date}" / "Ter controle" / "Uitgeschreven in KvK"), primary **Vacature plaatsen** (prefilled link), row menu (Opnieuw ophalen, Archiveren)
  - tabs **Overzicht · Vacatures · Sollicitaties · Tokenverbruik** (`EntTabs`, `?tab=`)
  - **Overzicht:**
    - "Gegevens uit KvK": Handelsnaam, Werklocatie (address + "bezoekadres vestiging {nr}"), Hoofdactiviteit (SBI). Each is the read-only KvK pattern with the lock "Uit KvK", with the line "Je kunt deze gegevens niet wijzigen. Verandert iets in KvK, dan werken we het hier automatisch bij. **Opnieuw ophalen**" (01/04 refresh endpoint; 429 → "Net opgehaald. Probeer het over een paar minuten opnieuw.")
    - "Op de kaart": static map with "Onze vestiging" and "Opdrachtgever" pins, dashed line, pill "{n} km tussen je vestiging en de werklocatie". Bureau-only; this is the one place the bureau sees both.
    - "Standaard voor nieuwe vacatures" select (Via onze vestiging / Echte werklocatie), disabled with the D16 text beyond 25 km
    - two info lines: "Kandidaten zien de naam alleen bij 'Echte werklocatie'" and "Andere bureaus en de opdrachtgever zelf zien jouw vacatures en sollicitanten niet"
    - table Vacature · Status · Op de kaart · Sollicitaties (holds shown as a pill "Gepauzeerd · {reason}")
  - **Vacatures / Sollicitaties:** the werkgever list components filtered on `intermediaryClientId` (A present), or today's lists with the filter (A absent)
  - **Tokenverbruik:** read-only ledger rows of the bureau wallet whose vacancy belongs to this link (sum + table). Line "Tokens komen altijd van het saldo van {bureau}."
- **Ter controle / Afgewezen / Uitgeschreven** links show the matching banner (review line, admin reason, or "Vacatures staan op pauze. Archiveer deze opdrachtgever of wacht tot KvK de vestiging weer actief meldt.").

## 06.6 Vestigingen (bureau), Team, Bureauprofiel, Tokens
- **Vestigingen** `/werkgever/organisatie/vestigingen` for the Intermediary:
  - the bureau's Type-`Intermediary` companies
  - "Vestiging toevoegen via KvK" through the existing employer vestiging add flow (01 geo fix applies), creating Type `Intermediary` children of the bureau org; server check: only KvK numbers of the bureau's own organisation (same KvK number as the org)
  - **no** regions tab
- **Team & rechten:** `/intermediary/team` moves (A present) with today's rules (`EmployerInviteRules`).
- **Bureauprofiel:** the werkgever profile page with the label override.
- **Tokens & facturen:** the werkgever pages. Add the line "Jaarabonnement uitzendbureau: actief t/m {datum}" or "Geen jaarabonnement" on Saldo & kopen (data from `FlexCommercialService.GetAgencySubscriptionAsync` on the org/vestiging per 02.7).

## 06.7 Vacancy form in the shell
- `/werkgever/vacatures/nieuw` shows 02's `OpdrachtgeverLocatieStep` as step 1 of 5 for the Intermediary ("Voor een opdrachtgever. Stap 1 van 5."), with the scope chip showing the selected opdrachtgever (d3). No logic changes.

## 06.8 Redirects (A present)
- `/intermediary` → 301 `/werkgever/opdrachtgevers` (the dashboard content now lives at `/werkgever`), `/intermediary/team` → 301 `/werkgever/organisatie/team`. Query kept. Internal links updated; the werkgever "no old hrefs" test covers the old intermediary URLs too.
- A absent: no redirects; pages under today's routes as Dependencies A says.

## 06.9 Mobile (< 1024) (m1)
- Bottom nav per §IA. Dashboard stacked: KPI 2×2, Te doen, Opdrachtgevers (cards instead of the table). Opdrachtgevers list → detail as a full page; the drawer becomes a full-screen sheet (`EntDrawer`). Tap targets ≥ 44 px.

## Tests
- Nav snapshot for the Intermediary (A present/absent variants), role chip only for the Intermediary, scope chip narrowing + foreign-id fallback, server filter 404 for a foreign `intermediaryClientId`.
- bUnit: dashboard with signals; Opdrachtgevers list filters; detail tabs; read-only KvK fields (no inputs; the §0 design guard); default-mode select disabled beyond 25 km; Tokenverbruik sum; banners per status.
- Vestigingen add: KvK of another organisation → 400; the created child is Type `Intermediary`.
- Redirects (A present), `BlazorPageRoleAttributesTests`, routes/SEO/help, Playwright: bureau smoke (dashboard → Opdrachtgevers → add via KvK stub → new vacancy step 1 → map mode) at 1440 and 390.

## Success criteria
- A bureau user sees d1/d2/d5/m1 (within the §0 differences) in the same shell as a bedrijfsmanager.
- No page offers a location input. All numbers come from the bureau's own data.

Done → next: `07-opruimen-docs-guards.md`.
