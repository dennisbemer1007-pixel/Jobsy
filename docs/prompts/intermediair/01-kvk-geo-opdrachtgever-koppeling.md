# 01. KvK geo + opdrachtgever link: PDOK geocoder, no (0,0), `IntermediaryClient`, KvK-only add, review queue

Read `00-README.md` first. Branch `cursor/intermediair-1` from `origin/acceptatie`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/intermediair-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Location only from KvK (no free location field anywhere), the opdrachtgever's identity never leaves the server in hidden mode, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/intermediair-1` |
| PR title | `feat(intermediair): KvK-only opdrachtgever link with PDOK geocoding, no (0,0) coordinates, admin review queue` |
| PR body starts with | `Stacked on: none (first in the stack)`, then which case of Dependencies A–G applied |
| Mockups | `im-d5-opdrachtgever-toevoegen-kvk.png` (drawer), `im-d2-opdrachtgevers.png` (the "Gegevens uit KvK" read-only pattern only) |
| Split seam | **01a** = geocoder + KvK mapping fix + (0,0) repair + registration fallback (01.2–01.4). **01b** = `IntermediaryClient` + `IIntermediaryContext` + API + admin queue + drawer + matrix (01.5–01.10) |

## Goal
Every KvK-derived location on Lobsy has real coordinates, never (0,0) and never the NL centroid. A bureau can add an opdrachtgever **only** by KvK number + KvK vestiging + declaration. The server re-fetches everything from KvK and geocodes it via PDOK. The result is stored as a separate `IntermediaryClient` link, **without** touching any `Company` row or membership. Vacancies keep using today's path in this file; 02 switches them over.

## 01.1 Today (verify first)
- `KvkHandelsregisterService.MapVestigingen` (~L319) sets `Latitude/Longitude = isHq ? hqGeo : 0`: every non-hoofdvestiging gets (0,0). `KvkServiceStub` returns coordinates for all, so tests never saw it.
- Every consumer copies these into `Company.Location` without a check:
  - `CompaniesController.RegisterIntermediaryClientFromKvk` (~L171)
  - the employer vestiging add (~L102)
  - `AdminController` from-kvk (~L180)
  - `CompanyRegistrationService`
- `CompanyRegistrationService` (~L1770) falls back to `ManualLatitude ?? 52.1326`, `ManualLongitude ?? 5.2913` when KvK is down; `Register.razor` (~L638) sends the same centroid.
- `IGeocodingClient` / `NominatimGeocodingClient` lives in **Jobsy.Web** (candidate suggestions). There is no server-side geocoder in Api/Infrastructure.
- `KvkVerificationRetryHostedService` retries KvK verification for pending companies.
- `ResolveIntermediaryOrganizationIdAsync` is duplicated in `VacanciesController` (~L2359) and `VacancyCsvImportController` (~L447): primary company of Type `Intermediary` → `ParentCompanyId ?? Id`, else the first accessible Intermediary company.

## 01.2 Server-side geocoder (PDOK, D6, Dependencies G)
- `Jobsy.Core/Interfaces/IAddressGeocoder.cs`: `Task<GeocodeResult?> GeocodeNlAddressAsync(NlAddress address, CancellationToken ct)`.
  - `NlAddress(Postcode, HouseNumber, HouseLetter?, Addition?, Street?, City?)`
  - `GeocodeResult(double Latitude, double Longitude, string Municipality, string Postcode, string HouseNumber, string Source = "pdok")`
- `Jobsy.Infrastructure/Geo/PdokLocatieserverGeocoder.cs`:
  - Request `GET …/search/v3_1/free?q={postcode} {huisnummer}{toevoeging}&fq=type:adres&rows=1&fl=centroide_ll,gemeentenaam,postcode,huisnummer,huisletter,huisnummertoevoeging`.
  - Accept only when postcode **and** huisnummer (and huisletter/toevoeging when KvK has them) match exactly. No fuzzy matches, no street-only matches.
  - Parse `centroide_ll` `POINT(lon lat)`. Validate the NL bounding box (lat 50.7–53.7, lng 3.2–7.3), and never return (0,0).
  - Named `HttpClient`, timeout 5 s, one retry on 5xx/timeout.
  - `IMemoryCache` per normalized address for 30 days; a negative result is cached for 1 hour.
  - Log without the full address: postcode 4 digits + outcome.
- `AddressGeocoderStub` (deterministic coordinates for the stub KvK addresses; `null` for a postcode starting with `0000` so tests can hit the failure path). Selected like `KvkServiceStub` in `DependencyInjection.cs` (`Geo:Pdok:Enabled`).
- A small Core helper `KvkAddressParser.TryParse(string formattedAddress, out NlAddress)` for KvK's `VolledigAdres` ("Kassenweg 4, 2675 LK Honselersdijk"). Prefer the structured KvK address fields when the DTO has them (check `KvkVestigingDto` / `Adressen`); the parser is only the fallback.

## 01.3 No more (0,0) from KvK (B6)
- `KvkEstablishmentResult`:
  - `Latitude/Longitude` become `double?`; `MapVestigingen` returns `null` instead of `0` when KvK has no geo for that vestiging
  - add `bool IsHoofdvestiging`, and `Postcode?` / `HouseNumber?` if the DTO has them
  - update `KvkServiceStub`
- New `IKvkEstablishmentGeoResolver` (Infrastructure):
  - KvK geo when present, inside the NL box and non-zero
  - else PDOK
  - returns `(GeoPoint Location, string Municipality, CompanyLocationSource Source)` or **null**
- Use it in **every** place that turns a KvK establishment into a location: the employer vestiging add, admin from-kvk, registration activation, and the new link API (01.7). When it returns null:
  - **Opdrachtgever link:** 422 `kvk_location_unresolved` (D6, "can't be added").
  - **Employer vestiging / registration:** keep today's flow but set `Company.LocationSource = Unknown`. Never write (0,0) or the centroid. The map and discovery skip `Unknown` companies' vacancies until fixed; `KvkVerificationRetryHostedService` retries the geo resolve too.
- `Company.LocationSource` (`Unknown = 0`, `Kvk = 1`, `Pdok = 2`), migration default `Kvk` for rows with a sane location. The repair below sets the others.

## 01.4 Repair existing rows + registration fallback (B6, D24)
- One-shot `CompanyLocationRepairHostedService` (pattern: `IbanEncryptionMigrationHostedService`; idempotent; runs once per deploy until nothing is left). For every `Company` with |lat| < 0.0001 && |lng| < 0.0001 or exactly (52.1326, 5.2913):
  - resolve via KvK establishment (if `KvkEstablishmentId`) → PDOK on `Address`
  - on success, set `Location` + `LocationSource` and refresh the vacancies of that company whose `Location` equals the old value (discovery refresh)
  - on failure, `LocationSource = Unknown`
  - write one `PlatformLog` summary (counts only)
- `CompanyRegistrationService` manual-address path (~L1770) and `Register.razor` (~L638):
  - remove the centroid default
  - the server geocodes the typed registration address via `IAddressGeocoder`
  - on failure: `LocationSource = Unknown`, and for SBI-78 registrations a flag so 02/04's publish gate returns 409 `vestiging_location_unverified`
  - the client no longer sends lat/lng for a manual address (remove `ManualLatitude/ManualLongitude` from the web client call; keep the API fields for one release, **ignored**, with an `[Obsolete]` note)

## 01.5 `IntermediaryClient` entity (§D)
- Entity + EF config + migration exactly as §D.
  - `Location` is `geometry(Point, 4326)` like `Company.Location`, with a GIST index.
  - Unique `(IntermediaryOrganizationId, KvkEstablishmentId)`, index `(IntermediaryOrganizationId, Status)`.
  - FK `IntermediaryOrganizationId → Companies` (`Restrict`); **no** FK to the opdrachtgever.
- Setting `IntermediaryMonthlyClientReviewThreshold` (default 20, 1–500) in the platform settings (Dependencies D).

## 01.6 `IIntermediaryContext` (one place for "which bureau am I")
- `Jobsy.Core/Interfaces/IIntermediaryContext.cs` + Infrastructure implementation:
  - `GetBureauAsync(ClaimsPrincipal user, CancellationToken)` → `IntermediaryBureau(Guid OrganizationId, IReadOnlyList<BureauVestiging> Vestigingen, Guid PrimaryVestigingId)` or null.
  - `BureauVestiging(Guid CompanyId, string Name, string Address, GeoPoint Location, CompanyLocationSource Source)`.
  - Organisation = the user's Type-`Intermediary` company's `ParentCompanyId ?? Id`, resolved from the DB (never from claims alone). Vestigingen = the organisation itself if it is Type `Intermediary`, plus its Type-`Intermediary` children the user can access.
  - `RequireBureauAsync` throws `ForbiddenCompanyAccessException` for non-Intermediary users (Admin acting "on behalf" passes `bureauId` explicitly on admin endpoints only).
- Replace both copies of `ResolveIntermediaryOrganizationIdAsync` with it (same result for today's data; test).

## 01.7 API `api/intermediary/clients` (`IntermediaryClientsController`, D2, D9, D18–D20)
`[Authorize(Roles = Intermediary)]`, every action resolves the bureau via `IIntermediaryContext`; foreign link id → **404**.

| Method | Route | Behaviour |
|---|---|---|
| `GET` | `api/intermediary/clients?status=&q=` | Own links. `q` matches name/KvK/municipality. Per row: live vacancies, applications (30 d), default map mode. The counts are 0 until 02 wires vacancies |
| `GET` | `api/intermediary/clients/{id}` | Detail incl. the KvK snapshot, `LastKvkCheckAtUtc`, distance in km to each bureau vestiging |
| `POST` | `api/intermediary/clients/lookup` `{ kvkNumber }` | `[EnableRateLimiting]` (new policy `intermediary-kvk`, 20/min per user). Returns the handelsnaam, SBI and establishments `{ vestigingsnummer, name, address, isHoofdvestiging, alreadyLinked }`. **No coordinates.** KvK down → 503 `{ code: "kvk_unavailable", message: "KvK is nu niet bereikbaar. Probeer het later opnieuw. Een adres met de hand invoeren kan niet." }` |
| `POST` | `api/intermediary/clients` `{ kvkNumber, vestigingsnummer, declarationAccepted, declarationTextVersion }` | Server re-fetches the establishment from KvK (never trusts client data), resolves geo (01.3), then creates the link. Errors: 400 `declaration_required`; 404 `kvk_establishment_not_found`; 409 `already_linked` (an Archived link is reactivated instead, with the new declaration); 422 `kvk_location_unresolved`; 503 `kvk_unavailable`. Status `Active`, or `PendingReview` when this is the bureau's ≥ (threshold+1)-th link created in the current Europe/Amsterdam calendar month (D19; count `CreatedAtUtc` incl. reactivations). Returns 201 + `{ status, reviewNote }` |
| `POST` | `api/intermediary/clients/{id}/refresh` | Re-fetch this link from KvK now (same code path as 04's weekly job; 04 fills in the change handling, here it updates the snapshot + `LastKvkCheckAtUtc`). Rate limit 1 per 10 min per link → 429 |
| `PUT` | `api/intermediary/clients/{id}/default-map-mode` `{ showClientLocation }` | Default for new vacancies (D3). Validation of the 25 km rule arrives in 02 |
| `POST` | `api/intermediary/clients/{id}/archive` | 409 `client_has_live_vacancies` once 02 wires vacancies (D25) |

- **Request DTOs have no name/address/postcode/city/latitude/longitude/location properties.** Reflection guard `IntermediaryRequestDtoGuardTests`: scans every request type used by `api/intermediary/*`, `VacanciesController` write actions, `CreateVacancyRequest`, CSV import DTOs and the external vacancy API. It fails on those property names (allow-list: `CompanyName` on non-intermediary DTOs that already exist, listed explicitly with a comment).
- Creating/reactivating/archiving writes `PlatformLog` `intermediary.client.created|reactivated|archived` (ids + KvK number, no user data).
- The old `POST api/companies/intermediary-clients/from-kvk` **stays unchanged in this file** (the current vacancy form uses it); 02 removes it.

## 01.8 Admin review queue (D19)
- `/admin/intermediairs` (new page, `RequireAdmin`), tab **Opdrachtgevers ter controle**:
  - table Bureau · Opdrachtgever (KvK, vestiging, gemeente) · Toegevoegd op/door · Links deze maand · SBI
  - actions **Goedkeuren** / **Afwijzen** (reason required, shown to the bureau)
- API: `GET api/admin/intermediary/clients?status=PendingReview`, `POST …/{id}/approve`, `POST …/{id}/reject { reason }`. Audit (Dependencies D). In-app notification to the bureau users (`IUserNotificationService`) both ways, and to admins on a new pending link.
- Admin nav item per §IA.

## 01.9 Drawer component "Opdrachtgever toevoegen" (d5)
`Components/Werkgever/Intermediair/OpdrachtgeverToevoegenDrawer.razor` (shell-independent; `EntDrawer` per Dependencies C). 06 places it on the Opdrachtgevers page; 02 opens it from the vacancy form.
- **Field** KvK-nummer (8 digits, `inputmode="numeric"`) + **Zoeken** → lookup.
- **Result card:** handelsnaam, "{n} vestigingen in KvK · SBI {code} {omschrijving}".
- **"Welke vestiging is de werklocatie?"**: one radio card per establishment (title "Hoofdvestiging · {plaats}" or "Vestiging {plaats}", address, vestigingsnummer).
  - Already-linked vestigingen are disabled with the pill "Al je opdrachtgever".
  - Preselect **only** when exactly one selectable establishment exists or a hoofdvestiging exists.
- **Declaration checkbox** (D20 text, required). Primary **"Opdrachtgever toevoegen"**, disabled until a vestiging is chosen and the box is ticked.
- **Info box, always visible:** "Lukt het ophalen uit KvK niet? Probeer het later opnieuw. Een adres met de hand invoeren kan niet, zodat de werklocatie altijd klopt."
- **Errors inline:** 503 as that box in warning style; 422 "We konden de werklocatie van deze vestiging niet bepalen. Kies een andere vestiging of probeer het later opnieuw."; PendingReview → success state with the review line (D19).
- **No address, postcode, city or coordinate inputs.** The bUnit test asserts the drawer renders exactly one text input (the KvK number).

## 01.10 Rights matrix foundation
- `Jobsy.Tests/Intermediair/IntermediairRightsMatrix.cs` (data-driven, §R), with rows for 01.7/01.8: own bureau, other bureau (404 on ids), same-KvK werkgever BM (403), candidate (403), anonymous (401), admin.
- `WebApplicationFactory` fixtures for two bureaus + one werkgever with the same KvK vestiging as a bureau link.

## Tests
- `PdokLocatieserverGeocoderTests` (recorded JSON fixtures): exact match accepted; mismatching huisnummer/toevoeging rejected; (0,0) and out-of-NL rejected; timeout → null; cache hit.
- `KvkHandelsregisterServiceTests`: non-HQ vestiging → `Latitude == null`; HQ keeps the geo. `KvkEstablishmentGeoResolverTests`: KvK → PDOK → null.
- Employer vestiging add / admin from-kvk / registration: a non-HQ vestiging ends with the PDOK location and `LocationSource = Pdok`; geocode failure → `Unknown`, never (0,0).
- `CompanyLocationRepairHostedServiceTests`: (0,0) and centroid rows repaired; idempotent; unresolved → `Unknown`.
- Link API: happy path; server ignores any extra JSON fields (e.g. `address`, `latitude`); declaration required; already linked / reactivation; 503/422 paths; 21st link in a month → `PendingReview` (Amsterdam month boundary test at 31-10 23:30 CET vs 01-11 00:30); foreign id 404; no `UserCompany` or `Company` row created or changed (assert counts before/after).
- Admin approve/reject + notifications + audit. Matrix rows. `IntermediaryRequestDtoGuardTests`. bUnit drawer (one text input, disabled "Al je opdrachtgever", KvK-failure box).
- `IIntermediaryContext` equals the old helper on today's seeded data (`DemoUsersSeeder` intermediary).

## Success criteria
- No code path writes (0,0) or (52.1326, 5.2913) into `Company.Location` or `IntermediaryClient.Location` (test + a grep guard for the literal centroid in Api/Infrastructure).
- A bureau adds an opdrachtgever with only a KvK number, a vestiging choice and the declaration. Nothing else is typed, and no company row or membership is touched.
- The 21st new opdrachtgever in a month lands in the admin queue.

Done → next: `02-vacature-eigendom-scheiding.md`.
