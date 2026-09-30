# 07. Cleanup, docs and final guards: terminology, dead code, roles matrix, SECURITY, functional spec, Playwright

Read `00-README.md` first. Branch `cursor/intermediair-7` from `cursor/intermediair-6` (or `-6b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/intermediair-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Location only from KvK (no free location field anywhere), the opdrachtgever's identity never leaves the server in hidden mode, and server-side authorization per §R.

| | |
|---|---|
| Branch | `cursor/intermediair-7` |
| PR title | `chore(intermediair): terminology guards, dead code removal, docs and role smoke tests` |
| PR body starts with | `Stacked on #<PR 06> (cursor/intermediair-6)` |
| Mockups | none (review all `im-*` against the final screens and list remaining differences in the PR) |
| Split seam | none |

## Goal
Leave no trace of the old model:
- no "Klant", "inhuurend bedrijf" or "Aangeboden door" in the UI
- no membership-based client code and no free-location affordances
- docs that describe the real rules
- guards so none of it comes back

## 07.1 Terminology guard
`IntermediairTerminologyTests`: scan `UiStrings*.cs` (nl values) and the `.razor` markup of intermediary/candidate-facing components for the "Instead of" column of the README §0 table ("Klant(en)" as a nav/label for opdrachtgevers, "inhuurend bedrijf", "eindklant", "end-client", "gemaskeerd", "Aangeboden door"). Allow-list with a reason per entry (e.g. candidate "klantenservice"-type words).

## 07.2 Dead code
- Delete:
  - `IntermediaryVacancyRules.ResolvePublicDisplay` (if still present)
  - `ValidateEndClientKvk` (replaced by the writer's checks), and move its messages to keys
  - the old `_hideClientAddressOnMap` code
  - `RegisterIntermediaryClientFromKvkAsync`, and `EnsureActorMembershipAsync` if now unused
  - `Nav.Clients` keys
  - the `[Obsolete]` `OfferedByLabel` **only** if no consumer remains; else keep it and list it as deferred
- Keep the 410 for `api/companies/intermediary-clients/from-kvk` until the release after this stack. Add a CHANGELOG entry that it will be removed.
- `ManualLatitude/ManualLongitude` (01.4): remove from the API if no client sends them; else keep them ignored and list it.

## 07.3 Final guards (add what's still missing)
- **Free-location guard** (01, 02): also scans `Jobsy.Web/Components/**/Intermediair/**` and the vacancy form for inputs bound to address/postcode/city/lat/lng.
- **Location-write guard** (02): only `IntermediaryVacancyWriter` writes `Location` for intermediary vacancies.
- **Membership guard:** no code path adds `UserCompany` for an Intermediary user on a non-`Intermediary` company. Test via the service layer + a source grep for `new UserCompany` call sites, each reviewed and listed.
- **Canary suite** (03): runs in CI as part of `dotnet test`. Make sure it isn't marked `Skip` or category-excluded.
- **Centroid/zero guard** (01): literal `52.1326`/`5.2913` and `new GeoPoint(0, 0)` forbidden in Api/Infrastructure except the map-view fallback in `VacancyMapViewCalculator` (allow-listed).

## 07.4 Docs
- **`docs/security/roles-matrix.md`:** Intermediary becomes a first-class row (answer the open question at ~L58: "Kept as first-class role, see docs/prompts/intermediair"). Add the §R rights, the "no membership on opdrachtgevers" rule and the identity firewall.
- **`SECURITY.md`** intermediary section: the link model, reveal rule, canary test, KvK-only location, uitleenregistratie gate.
- **`docs/FUNCTIONELE_SPECIFICATIES_INTERMEDIAIR_SALES_KPI.md`:** update masking (pin at the bureau vestiging, 25 km), ownership (bureau), money (bureau), the client-performance definition (02.9) and KvK refresh/holds. Mark the replaced paragraphs as "Vervangen door docs/prompts/intermediair (30-09-2026)".
- **`docs/TESTSCENARIOS_PER_ROL.md` + `docs/testscenarios-per-rol.csv`:** Intermediair scenarios:
  - add opdrachtgever via KvK (incl. several vestigingen, KvK down, non-HQ geocode fail)
  - hidden vs real vacancy (> 25 km)
  - candidate view before/after Uitgenodigd
  - weekly refresh address change / deregistration
  - review threshold
  - uitleenregistratie gate
  - Kandidaatinzichten
  - same-KvK werkgever sees nothing
- **`docs/ROUTES.md`** (all new routes + redirects), **CSV import help** (`VacancyCsvSchema` docs), **external API docs** (`intermediaryClientId`, deprecation of the legacy company id), **`CHANGELOG.md`**.

## 07.5 Playwright role smoke
- **Bureau:**
  - login → dashboard (role chip + scope chip visible)
  - Opdrachtgevers → add via KvK stub (non-HQ vestiging) → detail shows "Uit KvK" without inputs
  - new vacancy hidden → publish (lender verified in the seed)
- **Candidate:**
  - the map shows the bureau pin and the "Uitzendbureau" pill
  - search for the opdrachtgever name finds nothing
  - apply → (bureau invites) → the application detail shows the werklocatie
- **Same-KvK werkgever:** the vacancies list shows no bureau vacancy.
- Desktop 1440 + mobile 390. Say in the PR if you couldn't run it.

## Tests
The guards above, the terminology test, the docs freshness tests (`RoutesDocFreshnessTests`), and the Playwright smoke if possible.

## Success criteria
- All guards green. Docs describe the built behaviour.
- The PR lists every remaining mockup difference and every deferred item (e.g. the NAU/Wtta provider, removal of the 410 endpoint).

Done → stack complete. Report file → branch → PR → status, plus anything deferred.
