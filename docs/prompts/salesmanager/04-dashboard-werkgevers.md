# 04. Dashboard + Mijn werkgevers (privacy-safe) + top-bar search

Read `00-README.md` first (§0, §IA, §R, §P, D1, D4). Branch `cursor/salesmanager-4` from `cursor/salesmanager-3`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-4` (from `cursor/salesmanager-3`) |
| PR title | `feat(sales): dashboard + Mijn werkgevers — earnings, funnel, privacy-safe employer list and detail` |
| PR body starts with | `Stacked on #<PR 03> (cursor/salesmanager-3)` |
| Mockups | `sm-d1-dashboard.png`, `sm-m1-dashboard.png`, `sm-d3-werkgevers-detail.png` |
| Split seam | **04a** = read services + API + Dashboard (04.2, 04.3). **04b** = Mijn werkgevers list + drawer + search (04.4–04.6) |

## Goal
A salesmanager opens Lobsy Partner and sees at a glance what they earned, what's coming, how their link converts and which employers need attention, and can look at each referred employer without seeing anything they shouldn't (D4).

## 04.1 Today (verify first)
- `SalesManagerHomePanel.razor` + `SalesManagerDashboardService` (L40–60: loads companies with `Name`, `KvkNumber`, `FirstYearSupplierSlot`, `FirstYearStartedAt`, paid-onboarding flag; the ledger with notes that contain purchase amounts; invoices). The ring metric (uninvoiced ÷ (balance + uninvoiced)) is meaningless and goes.
- Org + vestiging are listed twice (fixed by `ISalesEmployerReadService.ListUnitsAsync`, 02.5).

## 04.2 Read services + API
- `GET api/sales/me/dashboard?period=month|year|all` → `SalesDashboardDto`: `Available`, `Pending`, `EarnedInPeriod`, `EarnedPrevComparable` (null when no data), `ActiveEmployers`, `TotalEmployers`, `NewEmployersThisMonth`, `NextRunDate` (`SalesClock.FirstWorkdayOfMonth` of next month, or this month's if still ahead), `Monthly[12]` (month, amount ex VAT, `IsCurrent`), `Funnel` (03.7), `Todos[]`, `TopEmployers[5]` (by commission this year).
- `GET api/sales/me/employers?q=&status=&year=&page=` → paged `SalesEmployerDto` rows; `GET api/sales/me/employers/{companyId}` → `SalesEmployerDetailDto`. `companyId` may be a root or vestiging id; the API resolves the root and checks `SalesBeneficiary.CanSeeCompany` (404 when not own, to avoid confirming existence).
- `SalesEmployerDto` (Core contracts, guarded by 01.9): `CompanyId` (root), `DisplayName`, `Place?` (D4 rule), `BranchCount`, `AttributedOn`, `Source` (label), `Status` (`NoPurchase` "Nog geen aankoop" / `Active` "Actief" (credited purchase ≤ 90 days) / `Quiet` "Stil · {n} dagen" / `Ended` "Afgelopen" (after the window)), `CommissionYear?` + `CurrentRate?`, `YearProgress` (0–1 within the current commission year), `CommissionThisYear`, `LastPurchaseOn?`.
- `SalesEmployerDetailDto`: the row + `CommissionTotal`, `PurchaseCount`, `Years[3]` (start, end, rate), `Timeline[]` (Aangemeld (source) · start-highlight received (if `PendingStartHighlightBonus` was consumed) · Eerste aankoop (package label) · Jaar 2 begint · Jaar 3 begint · Commissie stopt; future items muted), `Lines[]` (date, `PackageLabel`, purchase amount ex VAT, own commission, state label). `PackageLabel` = the `SalesPackage` name when the checkout came from a sales package, else "{n} tokens" from the checkout pack size; never free-text notes.
- Place: the city part of the root's `Address` via an existing address parser if present (`git grep -n "ParseCity\|City(" -- Jobsy.Core`), else the text after the postal code; omitted when `LegalForm` is null or `Eenmanszaak` (D4).
- Ambassadeur: the same endpoints work for companies attributed to the ambassadeur (their rate line shows the tier %); candidate numbers come in 09.

## 04.3 Dashboard `/sales` (`sm-d1`, `sm-m1`)
- Header: `h1` "Goedemorgen/Goedemiddag/Goedenavond, {voornaam}" (Europe/Amsterdam), lead "Zo gaat het met je verkoop. Bedragen zijn excl. btw." Right: segmented period (Maand · Dit jaar · Alles, URL `?periode=`), secondary "Kopieer mijn link" (copies `/p/{code}`, toast "Link gekopieerd").
- KPI row (4, `EntKpiCard`): hero **Beschikbaar om uit te betalen** (dark, amount, "excl. btw · volgende uitbetaalronde {d MMMM}", primary "Uitbetaling aanvragen" → `/sales/wallet/uitbetalen`, disabled with the reason as help text when below the minimum or blocked (07)); **Verdiend {periode}** with delta "+38 % t.o.v. 2025" (hidden without data); **In behandeling** ("vrij na {CommissionHoldDays} dagen"); **Actieve werkgevers** "9 van 12", "+2 deze maand".
- "Je commissie per maand": 12 bars ending at the current month, current month highlighted (`--brand-deep`), the others `--border`-toned; pill "€ {sum} in 12 maanden"; legend "Uitbetaald of beschikbaar" / "Deze maand (deels in behandeling)"; link "Alle mutaties" → `/sales/wallet`. Accessible: a visually hidden table with the same numbers.
- "Van link naar klant" (03.7): 4 steps Bezoeken via je link → Aangemeld (% of visits) → Eerste aankoop (% of registered) → Nog actief ("aankoop < 90 dagen"), with the period label. Hidden for beneficiaries with no visits and no registrations yet (empty state: "Deel je link om te beginnen." + button to `/sales/link`).
- "Te doen" (max 5, count pill, most urgent first), each with one action:
  - "{naam} heeft nog niets gekocht" (registered ≥ 7 days, no purchase) → drawer; sub-line "Aangemeld op {d} · tip: bel over de gratis start-highlight".
  - "{naam} gaat naar jaar {n}" (next commission year within 60 days) → drawer; "Vanaf {d} krijg je {p} % in plaats van {q} %".
  - "Factuur {nr} is betaald" (last 14 days) → "Download".
  - "Geef toestemming voor self-billing" / "Vul je uitbetaalrekening in" (blocking payouts; 06) → `/sales/profiel`.
  - "{naam} is stil sinds {n} dagen" (> 90 days) → drawer.
- "Beste werkgevers" (compact rank list, not a table): rank, name, place/status sub-line, amount right-aligned; "Alle {n}" → `/sales/werkgevers`.
- Mobile (`sm-m1`): "Hoi {voornaam}", role + code line, hero card with In behandeling / Verdiend {jaar} and the primary button, 2 small KPIs (Actieve werkgevers, Aanmeldingen), 6-month bars, Te doen (2), "Deel je link" card with "Delen" (Web Share API, fallback copy).

## 04.4 Mijn werkgevers `/sales/werkgevers` (`sm-d3` left)
- Header "Mijn werkgevers", lead "{n} werkgevers via jouw link of code · {m} actief".
- KPIs: Aangemeld (sinds start) · Eerste aankoop ("{p} % van aanmeldingen") · Stil (> 90 dagen, "even bellen?") · Gem. per werkgever (commission this year / active).
- `EntFilterBar`: search (name/place), Status, Commissiejaar (1/2/3/Afgelopen). URL-synced.
- `EntDataTable` columns: Werkgever (name + place sub-line; "+{n} vestigingen" when `BranchCount > 0`), Aangemeld, Status (pill + label), Commissiejaar (progress bar + "Jaar {n} · {p} %"), Commissie {jaar} (right), Laatste aankoop. 25 per page, `EntPager`. Row click / Enter opens the drawer (`?open={companyId}`).
- Empty state: "Nog geen werkgevers. Deel je link of geef je code aan een werkgever." + "Naar mijn link".

## 04.5 Detail drawer (`sm-d3` right, `EntDrawer` 560 px; full-screen sheet < 900)
- Title name, sub-line "{plaats} · {bron} · sinds {d}".
- 3 mini KPIs: Jouw commissie totaal · Aankopen · Nu ("Jaar 1 · 25 %" or "Afgelopen").
- "Commissiejaren": three segments with the current one filled to `YearProgress`, labels "Jaar 1 · 25 % · t/m {d}".
- "Zo ging het": the timeline (04.2), future steps muted with a clock icon.
- "Commissie per aankoop": table with two columns only (Aankoop (excl. btw) with the date + package sub-line · Jij krijgt with the state pill under the amount), newest first, max 10 + "Toon alles".
- Privacy note (success tone): "Je ziet bedrijfsnaam, plaats en je eigen commissie. Geen contactpersonen, kandidaten of vacatures."
- Footer: secondary "Vraag Lobsy om hulp" (opens the existing support/contact flow prefilled with "Vraag over werkgever {naam}"; if there's no contact flow, a `mailto:` to the configured support address with that subject), and **no** "Bekijk openbare vacatures" button (D4: no vacancy details in the portal; this is a spec-wins difference with the mockup).

## 04.6 Top-bar search
- The search field in `SalesLayout` searches **own** employers only (`GET api/sales/me/employers?q=&page=1`, max 8 results, debounce 250 ms), keyboard: Ctrl K focuses, ↑/↓, Enter opens the drawer on `/sales/werkgevers?open=`. Placeholder "Zoek een werkgever…". Hidden for ambassadeurs without attributed companies.

## Tests
- Services: dashboard numbers on a fixed data set (earned/pending/available, 12 months ending at the current month, deltas hidden without data, next run date on 29-09-2026 = 1 oktober 2026); funnel percentages; todos rules and ordering; top 5.
- Privacy: `SalesEmployerDto`/`DetailDto` reflection guard passes; place hidden for `Eenmanszaak` and unknown; foreign company → 404; werkgever and candidate → 403; package labels never contain ledger notes.
- Org + 1 vestiging → one row with "+1 vestiging", count 1.
- bUnit: KPI hero disabled state + reason, period switch → URL, empty states, drawer tabs/keyboard, search keyboard flow.
- Playwright (if you can): `/sales` at 1440 and 390 match the mockup layout (no horizontal scroll at 390).

## Success criteria
- `dotnet build` + `dotnet test` green.
- The seed salesmanager sees their dashboard and employers with real numbers; nothing in the rendered HTML contains a KvK number, address, contact e-mail/phone or candidate data.
- Rights-matrix rows for the new endpoints are added and green.

Done → next: `05-link-materiaal.md`.
