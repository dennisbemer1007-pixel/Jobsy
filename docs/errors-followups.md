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
