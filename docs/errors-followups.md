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
