# 03. Public display + privacy: one identity resolver, hidden really hidden, reveal at "Uitgenodigd", canary leak tests

Read `00-README.md` first. Branch `cursor/intermediair-3` from `cursor/intermediair-2` (or `-2b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/intermediair-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Location only from KvK (no free location field anywhere), the opdrachtgever's identity never leaves the server in hidden mode, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/intermediair-3` |
| PR title | `fix(intermediair): opdrachtgever stays hidden everywhere a candidate looks; reveal only from Uitgenodigd` |
| PR body starts with | `Stacked on #<PR 02> (cursor/intermediair-2)` |
| Mockups | `im-d4-kandidaatweergave.png` (both modes), `im-d3-…` (preview card), `im-m2-…` |
| Split seam | **03a** = resolver + DTO/record + map/list/detail/JSON-LD/company page + test rewrite (03.2–03.5). **03b** = search, assistant, Lobsy-cv, applications + reveal, e-mail/push, canary suite (03.6–03.9) |

## Goal
A candidate or anonymous visitor never learns who or where the opdrachtgever is for a hidden vacancy: not the name, KvK, address, company page or coordinates, and nothing derived from them (distance, travel time, sort order, radius). In real mode they see the opdrachtgever name + KvK address "via {bureau}". The candidate's own application reveals the opdrachtgever and werklocatie from "Uitgenodigd" (D4).

## 03.1 Today (verify first)
- `IntermediaryVacancyRules.ResolvePublicDisplay` returns name/address/logo/lat/lng/`OfferedByLabel` ("Aangeboden door X"). Its callers:
  - `VacancyDiscoveryIndex` (~L145)
  - `VacanciesController` (~L2069)
  - `VacancyTextSearch`
  - `LobsyCvModelFactory`
  - `CandidateApplicationLocation`
  - `VacancyProductService` (~L1024)
  - `AssistantChatService`
- The comment "Pin always follows the vacancy workplace" and the test `ResolvePublicDisplay_masked_uses_vacancy_coords_over_intermediary_hq` encode the leak (B1).
- Cards render `OfferedByLabel ?? CompanyName`:
  - `Components/Discovery/VacancyCard.razor` (~L73/107)
  - `Pages/Candidate/Vacancies.razor` (~L286)
  - `CompanyPublicPage.razor` (~L175/357)
- `VacancyDetail.razor` (~L144) links the displayed name to `/{KvkNumber}/{Vestigingsnummer}`; JSON-LD via `StructuredData.JobPosting` (~L759) takes company name/address/lat/lng from the DTO.
- `VacancyListItemDto` has `CompanyId`, `KvkNumber`, `Vestigingsnummer`, `IntermediaryCompanyId`, `ShowClientAddressOnMap`, `OfferedByLabel`. After 02, `CompanyId`/KvK are the bureau's.

## 03.2 `IntermediaryPublicIdentity` (Core, pure; the only resolver)
`Jobsy.Core/Rules/IntermediaryPublicIdentity.cs` replaces `ResolvePublicDisplay` (delete the old method once every caller moved):
- **Input:** vacancy, owner bureau vestiging (`vacancy.Company`), bureau org, link (may be null for non-intermediary vacancies), and an optional viewer context `(ApplicationStatus? ownApplicationStatus)`.
- **Output:** `PublicVacancyIdentity(DisplayName, DisplayAddress, DisplayLogoUrl, Latitude, Longitude, Badge (None | Uitzendbureau | ViaBureau), BureauName, BureauPublicPath, RegionLabel?, Revealed?)`:
  - **Non-intermediary vacancy:** exactly today's company fields, Badge None.
  - **Hidden:**
    - name/address/logo = owner bureau vestiging, coordinates = `vacancy.Location` (= owner location after 02)
    - Badge `Uitzendbureau`, `RegionLabel = "Regio {link.Municipality}"`, `BureauPublicPath` = the bureau vestiging's `/{kvk}/{vestigingsnummer}`
  - **Real:**
    - name = link name, address = link address, logo = **none** (the opdrachtgever has no logo on Lobsy), coordinates = `vacancy.Location` (= link location)
    - Badge `ViaBureau` with `BureauName`, `BureauPublicPath` = the bureau's page
  - **`Revealed`** (only when the viewer context says own application ∈ {`EmployerContacting`, `Hired`} and the mode is hidden): `{ ClientName, ClientAddress }`. Otherwise null. It is never part of list/discovery/search outputs.
- **Rewrite the leaking test:** `ResolvePublicDisplay_masked_uses_vacancy_coords_over_intermediary_hq` → `Hidden_uses_bureau_vestiging_coordinates_never_the_werklocatie`. It asserts lat/lng equal the owner and differ from the link, and adds a test where `vacancy.Location` was (wrongly) left at the link location: the resolver still returns the owner location (defence in depth), and a warning is logged.
- Remove the misleading doc comment. Rename the XML docs to D5/D12 wording.

## 03.3 Public DTOs and the discovery record (B2)
- **`VacancyDiscoveryRecord` + `VacancyListItemDto` (public/candidate shapes):**
  - `CompanyId`/`KvkNumber`/`Vestigingsnummer` = the **owner bureau vestiging** in both modes (automatic after 02; assert in tests)
  - add `Badge`, `BureauName`, `BureauPublicPath`, `RegionLabel`
  - `OfferedByLabel` is kept for one release as a computed value ("via {bureau}" / "Uitzendbureau") and marked `[Obsolete]`
  - **no field ever holds link data other than the displayed name/address in real mode**
- **Bureau-only DTOs** (employer vacancy list/detail, applicants) get `IntermediaryClientSummary? Client { Id, Name, KvkNumber, Vestigingsnummer, Municipality, ShowClientLocation, DistanceKm }`. They're served only from employer-authorized endpoints.
- **Reflection guard `PublicDtoLeakGuardTests`:** public DTO types (discovery, list, detail, search result, company page, LobsyCv, candidate application list/detail, assistant result) contain no property of type `IntermediaryClient*` and no property named `IntermediaryClientId`, `ClientKvk*`, `ClientAddress*`.

## 03.4 Map, list, detail, JSON-LD
- **Cards** (`VacancyCard`, `Candidate/Vacancies.razor`, `CompanyPublicPage`) render `DisplayName` + the shared `IntermediaryBadge` component (`Components/Discovery/IntermediaryBadge.razor`: Uitzendbureau pill / "via {bureau}" pill linking to `BureauPublicPath`). Hidden mode shows the place as "{bureau plaats} (vestiging bureau)" (d4).
- **Travel line:**
  - hidden: "{n} min {mode} tot de vestiging" plus, on the detail page, the row "Reistijd: {n} min {mode} tot de vestiging van het bureau"
  - real: "{n} min {mode}" / "naar de werklocatie"
  - the value is computed from `vacancy.Location` as today (= owner in hidden mode), so no change to `TravelReach`
- **`VacancyDetail.razor`:**
  - the company line links to `BureauPublicPath` in **both** modes (D22), never to a client path
  - rows (d4):
    - Werkgever = DisplayName + badge
    - Werklocatie = hidden "Regio {gemeente} · adres volgt als het bureau je uitnodigt" / real "{plaats} (adres uit KvK)"
    - Reistijd as above
    - Bedrijfspagina = "Pagina van {bureau}"
- **JSON-LD** `JobPosting`:
  - `hiringOrganization` = the bureau (hidden) or the opdrachtgever name (real). Add `"sameAs"`/`url` only for the bureau.
  - `jobLocation` = `DisplayAddress` + the pin (hidden → the bureau vestiging).
  - Test both modes, including that the client name/postcode don't appear in the hidden JSON-LD.
- **Map pin label/popup** (discovery JSON): DisplayName + badge text. The popup never shows a client field. Check `jobMap` popup templates for `companyName` usage and feed them the display values only.
- **OG/share** (`VacancyImageUrls`/share service, `ShareCount` paths) and the **sitemap**: title/description use DisplayName. Test hidden mode.

## 03.5 Company pages
- The bureau's public page lists its vacancies with the display rules above (both modes). No other company page ever lists a bureau vacancy (test from 02 stays, plus the real-mode case).

## 03.6 Text search, matching, assistant, Lobsy-cv
- **`VacancyTextSearch`:** index DisplayName/DisplayAddress only. A search for the hidden opdrachtgever's name, KvK number, street or postcode returns **no** hidden vacancy (test). Real mode may match the opdrachtgever name.
- **Matching explanations / `ProfileVacancyMatchCalculator` / `MatchScoreCalculator`:** any text that names the company or distance uses the display values; distances come from `vacancy.Location` (already the pin).
- **`AssistantChatService`:** tool results and prompts use `IntermediaryPublicIdentity` (list shape, no reveal). Test: a hidden vacancy's client marker string never enters the prompt payload.
- **`LobsyCvModelFactory`** (the candidate's cv shows applied vacancies): list shape, no reveal.

## 03.7 Candidate applications + reveal (D4)
- **`api/me/applications`** (list; `MeController` + `CandidateApplicationLocation.ForPublicCard`): list shape, **no reveal** (city only, as today, but the bureau's city in hidden mode).
- **`api/me/applications/{id}`** (detail): with the viewer context = own status.
  - `EmployerContacting` / `Hired` → `Revealed { ClientName, ClientAddress }`. The UI shows the card **"Werklocatie"**: "Je werkt voor {opdrachtgever} · {adres} · via {bureau}" + "Solliciteren en contact lopen via {bureau}."
  - Other statuses → no reveal; the card reads "Het bureau vertelt je waar je gaat werken als het je uitnodigt."
  - `Rejected`/`Withdrawn`/`FilledElsewhere` after an invite → the reveal disappears again.
  - No `PersonalDataAccessLog` row for the reveal: it is company data, not personal data.
- **Candidate-facing e-mails and push** (`TransactionalEmails`, `VacancyEngagementReminderHostedService`, pushbericht texts, application status mails):
  - use the resolver's list shape
  - the "Uitgenodigd" e-mail may name the opdrachtgever in hidden mode **only** in the text that goes to that one invited candidate ("{bureau} nodigt je uit voor werk bij {opdrachtgever} in {plaats}")
  - every other mail/push uses the bureau
  - the e-mail catalog preview (`EmailCatalogService`) uses fake data

## 03.8 Wording
The "Aangeboden door X" label disappears from the UI (replaced by the badge). The strings go to `UiStringsIntermediair.cs` (`ImPublic.*`).

## 03.9 Canary leak suite
`Jobsy.Tests/Intermediair/ClientIdentityCanaryTests.cs` (WebApplicationFactory):
- **Seed:** a hidden vacancy whose link has unique markers: name `ZZCANARYNAAM`, KvK `99887766`, vestigingsnummer `000099887766`, street `Canarylaan 1`, postcode `9999 ZZ`, gemeente shown only as the region, and coordinates at a unique spot 20 km from the bureau.
- **Call as anonymous and as a candidate (with a Pending application):**
  - discovery / map JSON (all radius and travel filters), list, detail (HTML + JSON-LD), search (queries with each marker)
  - the bureau's and the same-KvK werkgever's company pages
  - share/OG endpoint, sitemap
  - assistant chat (stub LLM capturing the prompt)
  - LobsyCv, `api/me/applications` (+ `{id}`)
  - rendered candidate e-mails/push for that vacancy
- **Assert** no marker string and no coordinate within 50 m of the werklocatie appears, and results sorted by distance equal the order computed from the bureau location.
- **Positive controls:**
  - the same candidate moved to `EmployerContacting` sees `ZZCANARYNAAM` and the street in `api/me/applications/{id}` only
  - the bureau sees everything in its own endpoints
  - in real mode the name appears publicly, but KvK/vestigingsnummer still don't

## Tests
- Resolver unit tests: all modes × viewer statuses; the rewritten test (03.2).
- DTO guard, canary suite, JSON-LD both modes, search negatives, bUnit for `IntermediaryBadge` and the detail rows (both modes), candidate application detail reveal (bUnit + API).

## Success criteria
- The canary suite is green; the old leaking test is gone and its replacement asserts the opposite.
- The screens match d4 (within the spec differences in §0).

Done → next: `04-kvk-verversing-uitleenregistratie.md`.
