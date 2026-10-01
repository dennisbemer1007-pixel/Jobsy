# Errors stack — follow-ups for other stacks

Written while building `docs/prompts/errors/01-errorlayout-404-500.md` against
`origin/acceptatie` @ `e27b47cb`. Each line names the stack that should pick the item up.

## public-pages 01

- **Reuse what errors 01 built; do not recreate it.** `Pages/Status/StatusPage.razor`
  (`/status/{Code:int}`), `Localization/UiStringsStatus.cs` and the HTML-only
  `UseStatusCodePagesWithReExecute` filter (`Jobsy.Web/Hosting/ErrorPagesExtensions.cs`) already
  exist, as does the 404 status for an unknown or non-public vacancy.
- **Legal footer line.** Dependency B was absent: there is no `LegalIdentityProvider` yet, so
  `ErrorChromeProvider` renders `© {year} Lobsy`. When public-pages adds the provider, switch
  `ErrorChromeProvider.Build` to its **cached** value (`TryGetCached`) — the error pages may
  never fetch it.

## landing 01 / 02 / 04

- Dependency A was present, so `ErrorLayout` already wraps its content in `.pub-theme` and uses
  `pub-btn`, `pub-card`, `pub-chip`, `LobsyMascot` and `PublicRoutes`. Nothing to retrofit; if
  the public header/footer gain data-free variants, `ErrorLayout` may reuse those pieces.

## banenkaart / discovery

- **404 search field is missing on purpose.** Errors 01 §01.3 only adds "Wat zoek je?" when the
  banenkaart accepts a free-text query parameter. `Banenkaart.razor` has no
  `[SupplyParameterFromQuery]` text parameter at `e27b47cb`, so the field was left out. When the
  banenkaart gains one, add a GET form on `/status/404` that submits to it.

## werkgevers actief (`IEmployersSwitch`)

- Dependency F was present. The 404 page already follows E3 OFF: with employers off the primary
  action becomes "Mijn Paspoort" → `/ontdek` instead of "Banenkaart" → `/banenkaart`.

## Carried into later errors files

- 403 (`02`), 410 (`03`), 429 (`04`) and maintenance 503 (`05`) currently fall through to the
  generic `/status/{code}` copy. Each file replaces that branch with its own content.
  **403 is done as of `02`** (`cursor/errors-2`): a signed-in user missing the required role now
  gets a real 403 at the requested URL via `OnRedirectToAccessDenied` (sets a bare 403, no
  redirect) → the existing `UseHtmlStatusCodePages` re-execute → `StatusPage.razor`'s
  `Code == 403` branch, which renders the shared `AccessDeniedView` component.

## auth 03 (`/account/switch`, Dependency E was absent)

- `02` added `reason=switch` handling to `/account/logout`
  (`Jobsy.Web/Auth/AuthServiceCollectionExtensions.cs`): signs out as today, then redirects to
  `/login?returnUrl={safe local}` via `AuthRedirects.SafeLocalUrl`. The "Inloggen met een ander
  account" button on the 403 page (`AccessDeniedView.razor`) posts to this endpoint.
- If auth adds its own `/account/switch` path or a redesigned login landing, point
  `AccessDeniedView`'s switch-account form at that instead of duplicating the logic, and keep the
  `returnUrl` round-trip (open-redirect safe, local paths only).

## errors 04 (429, reconnect, inline errors, `ex.Message` ratchet)

- **`ex.Message` is still on 109 files (322 uses).** `docs/errors/ex-message-baseline.txt` *is* the
  to-do list: every line is a file that still assigns an exception message to UI state. The ratchet
  only lets counts shrink, so any stack touching one of those pages can pick a few off by injecting
  `UserFacingError` and calling `Describe(ex)`. Error pages, public pages, layouts and the pages
  errors 04 swept are at 0 and must stay there.
  - swept in 04: the two action pages (`SetUnavailableAction`, `WithdrawOthersAction`),
    `CompanyPublicPage`, `VacancyDetail`, `Partner/PartnerSales`, `RegisterToegang`,
    `Sales/RecommendObject`, `Legal/PrivacyData`, `Candidate/HowLobsyWorks`, the candidate pages
    (`Applications`, `CandidateTalentContacts`, `CareerDashboard`, `DiscoveryJourney`,
    `OnboardingWizard`, `CandidateKompas`, `CourseSuggestionBlock`, `TrainingOffersBlock`),
    `RoleFitCheckSession` and `CandidateProfileEditor`.
  - left on purpose: `Pages/Leerling/LeerlingEiland.razor` classifies a 400 body (`name_rejected`)
    from `HttpRequestException.Message` and shows a catalog key, and
    `Services/HomeDashboardLoad.cs` / `Security/DataProtectionSetup.cs` use the message for
    transient-error classification and a boot-time console line. None of them reach a visitor.
- **`ApiError` is only wired on `ExportPrivacyDataAsync`.** The other `JobsyApiClient` methods still
  throw `InvalidOperationException(ExtractMessage(body))`, which puts the API body in reach of a
  page. Migrating a method means switching its callers to `UserFacingError` in the same change,
  otherwise the visitor sees `"API call failed with code …"`.
- **Panels still use `PanelErrorBoundary` / `panel-inline-error`.** The new `InlineErrorBlock`
  (`err-inline`, support code, retry callback) is the intended visual for a part of a page that
  fails; panels can migrate when their stack next touches them.
- **429 body for non-HTML Web requests** is ProblemDetails (`code`, `retryAfterSeconds`,
  `supportCode`) instead of the empty body §04.2 allows, so JS callers keep a machine-readable
  answer. The HTML path is an empty 429 that `UseHtmlStatusCodePages` re-executes to `/status/429`.
- **Extra head tags on error pages** go through the `error-head` section
  (`ErrorLayout.ErrorHeadSection`), never a second `<HeadContent>`: that would replace the layout's
  `noindex` meta.

## errors 05 (maintenance 503)

- **Dennis still has to decide the edge story.** `docs/onderhoud.md` §2 has the Cloudflare plan
  check with a checklist. Nothing in Cloudflare, Render or `render.yaml` was touched.
  - The most important finding: **Cloudflare Error Pages do not apply to HTTP 500/501/503/505**, so
    the legacy "5XX Errors" page would never fire on Lobsy's own 503. A **Custom Error Rule** is
    needed instead, which also requires a paid plan (Pro and up).
- **The maintenance switch is its own panel, not a catalog row.**
  `Components/Admin/Sections/MaintenancePanel.razor` sits above `PlatformSettingsEditor` on
  `/admin/instellingen` because it carries an end time and an internal note that
  `PlatformSettingDescriptor` cannot express. If `PlatformSettingsCatalog` ever grows a composite
  kind, the panel can fold into it; the audit actions `maintenance.on` / `maintenance.off` must
  stay distinct from `settings.platform.update`.
- **The admin banner lives in five layouts** (`AdminLayout`, `MainLayout`, `PublicLayout`,
  `WerkgeverLayout`, `SalesLayout`). A new layout that an admin can reach should include
  `<Jobsy.Web.Components.Errors.MaintenanceAdminBanner />` near the top.
- **`UserFacingError` now maps `ApiError.Maintenance` to `Status.Maintenance.Short`** instead of
  04's `Common.Error.Maintenance`, so the toast and the 503 page say the same thing.
  `Common.Error.Maintenance` is kept in the catalog as a fallback key.
- **Propagation is 15 s by design** (`MaintenanceRules.StatePollSeconds`). A stack that needs the
  switch to be instant should add a push path rather than shortening the poll.

## errors 06 (E2E, HTTP guards, stack end)

This is the stack's closing list: what the browser suite could not run here, which dependencies
stayed absent, and what is left for other stacks or for Dennis.

### Dependency outcomes over the whole stack

| | Dependency | Outcome | Carried follow-up |
|---|---|---|---|
| A | Public theme (`docs/landing` 01/02/04) | **present** at 01, still present at the 03 and 05 re-checks | none — `ErrorLayout` uses `.pub-theme`, `pub-*`, `LobsyMascot` and `PublicRoutes` |
| B | Public-pages hotfix (`docs/public-pages` 01) | **absent** — 01 created `StatusPage.razor`, `UiStringsStatus` and the re-execute filter | public-pages must reuse them, and switch `ErrorChromeProvider` to `LegalIdentityProvider.TryGetCached()` once that provider exists (see the public-pages 01 section above) |
| C | Emails (`AmsterdamTime`) | **present** | none — the maintenance end time uses it |
| D | Admin redesign | **present** at 01, still present at the 05 re-check | none — the switch is an `AdminToggleRow` with a danger `AdminImpactNote` on `/admin/instellingen` |
| E | Auth 03 (`/account/switch`) | **absent** — 02 added `reason=switch` to `/account/logout` | auth should point `AccessDeniedView`'s switch form at its own path when it has one (see the auth 03 section above) |
| F | Werkgevers actief (`IEmployersSwitch`) | **present** at 01, still present at the 02 and 03 re-checks | none — the 404 primary action follows E3 OFF, 403 handles `reason=employers-off` |
| G | Tests stack | n/a (nothing to check) | the 04 ratchet baseline already includes their progress |

### `ex.Message` at the end of the stack

`docs/errors/ex-message-baseline.txt` holds **109 files / 322 uses**, down from **377** at
`acceptatie` when 04 measured it. The error pages, the public pages, the layouts and the pages 04
swept are at 0 and the ratchet keeps them there. The file *is* the to-do list: a stack touching one
of those pages can pick a few off with `UserFacingError.Describe(ex)`.

### Native review pl / ro / ar

`docs/i18n/errors-review.md` lists every `Status.*` key with its Dutch source. **nl and en are
final; pl, ro and ar are B1 drafts** written during this stack and still need a native pass. The
maintenance page in `ops/maintenance/` carries the same five languages as literal text, so a
correction there has to be applied in both places.

### Browser rows that skip themselves

`StatusPagesPlaywrightTests` soft-skips without `JOBSY_E2E_BASE_URL`, and individual rows skip when
their precondition is missing. Each reason is appended to
`artifacts/playwright-errors/skipped.txt` so a CI run says out loud what it did not cover.

| Row | Needs | Falls back to |
|---|---|---|
| 500 page (matrix, copy button) | `Errors__EnableTestThrow=true` **and** `Errors__ForceHandler=true`; Development only, never on Render. Both are set by `.github/scripts/start-ci-stack.sh`. | `ErrorPageTests` / `StatusPagesHttpTests` in process |
| 410 page (matrix) | `JOBSY_E2E_CLOSED_VACANCY_ID` — the Development seed has no vacancy that is guaranteed closed, so the id must be passed in | `ClosedVacancyPageTests` + `StatusPagesHttpTests` against the in-process API |
| 403 page (matrix, switch account) | a candidate session (`JOBSY_E2E_CANDIDATE_EMAIL` / `_PASSWORD`, default the seed account) | `ForbiddenStatusTests` / `ForbiddenViewTests` |
| Maintenance flow | an **admin** session; admins need MFA since auth 02, so a scripted password login cannot get in | `MaintenanceMiddlewareTests` / `MaintenanceApiTests` cover the 503, the allow-list, the admin bypass and `Retry-After` |
| Reconnect toast | a live Blazor circuit that actually drops | `ReconnectToastTests` asserts the five languages in the markup |
| Inline block error | a dashboard card that already uses `InlineErrorBlock` (the panels still use `PanelErrorBoundary`) | `InlineErrorBlockTests` |
| 429 page (matrix) | uses the direct `/status/429` route, which is a real 429 with `Retry-After`. Exhausting the live limiter needs a CI-only permit value that does not exist yet. | `RateLimitPageTests` exhausts the real limiter in process |

### Smaller things 06 found and fixed

- **An unknown vacancy answered 404 with a bare "Vacature niet gevonden." line** inside the normal
  app shell — a real status code but not the friendly page §IA promises, and no `<h1>`. The 404 hero
  moved into `Components/Errors/NotFoundView.razor` (the same pattern as `ClosedVacancyView` for
  410) and is now used by both `/status/404` and `VacancyDetail`.
- **The API's 410 body had no machine-readable marker.** `ClosedVacancyDto` now carries
  `code: "vacancy_closed"` next to the minimal public fields, so a caller can tell this 410 from
  any other one without parsing prose.
- **There was no way to see the real 500 page in a browser.** `ErrorPagesExtensions.UseTestThrowPath`
  adds `/__test/throw`, gated on `Errors:EnableTestThrow` **and** a Development host. Off by
  default, never set on Render.

### Still open for other stacks

- **A CI-only rate-limit permit value** would let the browser suite see a 429 the way a visitor
  gets one (hitting the limiter) instead of the direct route.
- **A seeded closed vacancy** in the Development seed would make the 410 browser row run without an
  environment variable.
- **A reachable inline-error card.** Once one dashboard card uses `InlineErrorBlock`, the
  route-intercept flow in 06.2 runs for real.
