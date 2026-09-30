# 04. KVK search by name or number, profile on select, websites + postal address, SBI → branche

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-4` from `cursor/werkgever-aanmelding-3`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never call KVK from tests or CI: the stub and recorded fixtures only. The API key stays where it is (Admin → Integraties / `Kvk__ApiKey`).

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-4` |
| PR title | `feat(kvk): search companies by name or KvK number (Zoeken API), cached profile on select with websites and postal address, SBI → branche map` |
| PR body starts with | `Stacked on #<PR 03> (cursor/werkgever-aanmelding-3)` |
| Mockups | `wr-d1`, `wr-d2`, `wr-m1` (search + results), `wr-d3` (company card with website), `wr-d5` (SBI box) |
| Split seam | **04a** = service + endpoints + cache + stub (04.2–04.4). **04b** = SBI map + intermediair lookup (04.5, 04.6) |

## Goal
The wizard can find a company by typing either its KvK number or its name (with an optional place). It costs as little as possible, and the data needed for verification (website domain, postal address) comes along.

## 04.1 Today (verify first)
- `Jobsy.Core/Interfaces/IKvkService.cs`: `GetByKvkNumberAsync`, `GetEstablishmentsAsync`, `LookupEstablishmentsAsync` (status Ok/NotFound/Unavailable). `KvkCompanyResult` (KvkNumber, Name, Address, SbiCodes), `KvkEstablishmentResult` (…, `IsInUse`, SbiCodes). **No name search.**
- `KvkHandelsregisterService`: `v1/basisprofielen/{kvk}?geoData=true`, `v1/basisprofielen/{kvk}/vestigingen`; `v2/zoeken?kvkNummer=` is used only by the admin connection test (~L186). It parses `SbiActiviteiten` and addresses (`PreferBezoek`), but no websites and no postadres.
- `KvkServiceStub` (12345678 employer, 55667788 intermediair SBI 78) and `KvkServiceStubTests`, `KvkHandelsregisterServiceTests`, `KvkSbiClassificationTests`, `KvkApiBaseUrlTests`.
- Endpoints: `KvkController` `api/kvk/{kvk}` + `/{kvk}/establishments` (anonymous, `public-write`; `IsInUse` forced false), `RegistrationController` `GET api/registration/kvk/{kvk}/establishments` (with `IsInUse`).

## 04.2 Service
- `IKvkService.SearchAsync(KvkSearchQuery query, CancellationToken)` → `KvkSearchResult(Status, IReadOnlyList<KvkSearchHit> Hits, int Total)`.
  - `KvkSearchQuery(string Text, string? Place, int Page = 1)`. If `Text` is exactly 8 digits (after stripping spaces/dots), search by `kvkNummer`; otherwise by `naam` (min **3** characters after trim, else a validation error), plus `plaats` when given. `resultatenPerPagina` = 10. Only active registrations (don't send `inclusiefInactieveRegistraties`).
  - `KvkSearchHit(KvkNumber, Name, Place, Type (Rechtspersoon|Hoofdvestiging|Nevenvestiging), VestigingCount?, IsOnLobsy)`. Group hits per KvK number: one row per company with the vestiging count, as in wr-d2. `IsOnLobsy` = a company with that KvK number has at least one active member (boolean only; computed from the Lobsy DB, never shown with names).
  - Map the `v2/zoeken` response fields exactly as the KVK docs/test environment return them (write a recorded fixture from the KVK **test** API into `Jobsy.Tests/Fixtures/kvk/zoeken-*.json` once, by hand, and parse from that in tests).
- `IKvkService.GetProfileAsync(string kvk, CancellationToken)` → `KvkCompanyProfile`: the existing company data + vestigingen (with `IsInUse`) + **`Websites`** (from the basisprofiel `_embedded.hoofdvestiging.websites`; check the exact path in the fixture) + **`LegalForm`** (if the response has `rechtsvorm`/`uitgebreideRechtsvorm`; salesmanager 03 may already map it, so reuse it) + **`PostalAddress`** and **`VisitingAddress`** per vestiging (`adressen` types `postadres` / `bezoekadres`). It's one basisprofiel + one vestigingen call, **only when a company is selected** (D10). Vestigingsprofiel calls (€0,02 each) aren't made in the wizard.
- `KvkServiceStub`: add search data with name hits (a fictional "Groen & Zorg Thuiszorg B.V." 90123456 with 3 vestigingen incl. website `groenenzorg.nl` and a postadres, a stichting, a VOF, a second BV) plus today's 12345678 / 55667788 unchanged. One stub vestiging `IsInUse` for 07.

## 04.3 Cache and limits (D10)
- `IMemoryCache`: search results 24 h per normalized (text, place, page); profiles 24 h per KvK number. `IsOnLobsy`/`IsInUse` are **not** cached (computed per request from the DB). Keep the cache within the KVK terms of use (nothing stored beyond 24 h, nothing persisted to the DB from search).
- Named rate-limit policy `kvk-search` (README §A: 30/min per IP) on the search endpoint; `public-write` stays on the others.
- A `KvkUsageCounter` writing a small table `KvkUsageDaily` (date, call type zoeken/basisprofiel/vestigingen, count; no query text; migration `AddKvkUsageDaily`) with a small line on the existing admin KVK integration page ("Vandaag: 312 zoekopdrachten · 41 profielen"), plus an admin warning at 80 % of a configurable monthly budget (`Kvk:MonthlyProfileBudget`, default 50.000 → about €1.000).

## 04.4 Endpoints
- `GET api/kvk/search?q=&plaats=&pagina=` (anonymous, `kvk-search`) → `{ status, total, hits[] }`. 400 for < 3 characters. Unavailable → `status = Unavailable` (the wizard offers the manual path, 05).
- `GET api/registration/kvk/{kvk}/profile` (anonymous, `public-write`) → the profile incl. vestigingen with `IsInUse`, websites (domain only, e.g. `groenenzorg.nl`) and a **masked** postal address line for the letter preview (street + house number + postcode + place is fine: it's public KVK data).
- Keep the old establishments endpoints working (other callers: the intermediair lookup, `CompaniesController.from-kvk`, `AdminController`).

## 04.5 SBI → branche map (D12)
- `Jobsy.Core/Rules/SbiWorkTypeMap.cs`: `IReadOnlyList<string> Map(IEnumerable<string> sbiCodes)` → distinct `WorkTypeLabels` values in SBI order, max 4. Prefix rules (longest prefix wins):

  | SBI prefix | Branche |
  |---|---|
  | 55, 56 | Horeca |
  | 47 | Winkel |
  | 49, 50, 51, 52, 53 | Logistiek |
  | 01.1, 01.2, 01.3, 81.3 | Tuinbouw |
  | 86, 87, 88 | Zorg |
  | 69–75, 82, 84, 64–66, 62, 63 | Kantoor |
  | 41, 42, 43 | Bouw |
  | 81.2 | Schoonmaak |
  | 10–33 | Productie |

  78 (uitzendbureaus) maps to nothing (intermediair, 10). Unknown codes map to nothing (the user picks).
- Unit tests per row + the fixture company (`88101` → Zorg, `81210` → Schoonmaak).

## 04.6 Intermediair client lookup gets name search
- If README Dependencies **G** is Present, put the name search into the intermediair stack's "Opdrachtgever toevoegen" drawer (KvK-only add) instead and leave `CreateVacancy.razor` alone. Otherwise:
- `Pages/Branch/CreateVacancy.razor` "Zoek vestigingen" (intermediair adds a client) and `POST api/companies/intermediary-clients/from-kvk` (~L171) accept a KvK number today. Put `WaKvkSearchBox` there (05 creates it; in this PR make a minimal in-app variant `KvkSearchField.razor` in the **app** design system that 05's public component wraps or reuses its logic via one `KvkSearchClient` service). Selecting a hit then loads the vestigingen as today. No change to how client companies are created (10 handles their verification).

## Tests
- `KvkSearchTests` (from fixtures): 8 digits → number search; "gr" → validation error; name + place → the query string contains `naam` and `plaats`; grouping per KvK with the vestiging count; `IsOnLobsy` true only with an active member; cache hit on repeat (handler call count); Unavailable on 5xx/timeouts.
- Profile: websites, legal form, postadres/bezoekadres parsed from the fixture; exactly 2 KVK calls per profile, 0 on a cache hit.
- Endpoint: rate limit `kvk-search` (31st call in a minute → 429), 400 for short queries, no owner PII in any response (serialize and assert no e-mail/name fields besides the KVK company name).
- Intermediair lookup: typing a name shows hits; selecting one loads the vestigingen as before.
- Stub: name search returns the fictional companies; the old stub numbers behave as before.
- The existing KvK tests stay green.

## Success criteria
- A name search of "groen en zorg" + "Utrecht" (stub) returns the grouped hits of wr-d2. Selecting one returns the profile with website and postal address in ≤ 2 KVK calls, and 0 calls within 24 h.

Done → next: `05-wizard-zoeken-vestiging-account.md`.
