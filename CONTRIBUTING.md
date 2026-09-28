# Contributing to Lobsy / Jobsy

## One PR = one change

Keep pull requests focused: one feature, one bugfix, or one cleanup prompt. Prefer small reviews over omnibus branches. Target **`acceptatie`**, not `main` (see [`docs/release-flow.md`](docs/release-flow.md)).

## PR checklist (mandatory)

Before asking for review:

1. **Build** — `dotnet build Jobsy.sln` succeeds.
2. **Tests** — `dotnet test Jobsy.Tests/Jobsy.Tests.csproj` green for the area you touched (full suite preferred).
3. **Playwright for UI** — if you change Blazor UX / layout / banenkaart / navigation, run or extend the relevant Playwright tests (CI smoke covers a core set; see [`Jobsy.Tests/README.md`](Jobsy.Tests/README.md)).
4. **Screenshots** — attach before/after (or Acc) screenshots for visible UI changes.
5. **Authz / AVG** — new endpoints have correct `[Authorize]` / `[AllowAnonymous]`; no PII leaks on public surfaces (`SECURITY.md`).
6. **Docs** — update `docs/ROUTES.md` (regenerate) if you add/change `@page` routes; update roles docs if capabilities change.

There is no GitHub PR template file yet; treat the list above as the template.

## Asset `?v=` bump rule

Versioned files under `Jobsy.Web/wwwroot` are cached as immutable. After editing a versioned asset:

1. Bump its `?v=` in `Components/App.razor` and/or the loader that references it (`wwwroot/js/maps-loader.js`, `app-core.js`, …).
2. Refresh the manifest: `python3 Jobsy.Tests/update-asset-versions.py`.
3. Confirm `AssetVersionGuardTests` passes.

## CSS: `app.min.css`

Until cleanup prompt **10** lands a real minifier pipeline, `wwwroot/css/app.min.css` is a maintained mirror of `app.css` (plus feature CSS linked separately). **Do not hand-edit `app.min.css` alone** out of sync with sources. Prefer new styles in `wwwroot/css/features/*.css` linked from `App.razor` with their own `?v=`.

## Where to put components

- Routable pages: `Jobsy.Web/Components/Pages/` (role folders: `Admin/`, `Candidate/`, `Employer/`, …).
- Shared chrome / widgets used across roles: `Jobsy.Web/Components/` (or `Shared/` / feature folders already present).
- Do not park large panels forever inside unrelated page files — extract when the file becomes hard to review (structural moves only in dedicated PRs; see cleanup prompt 13).

## UiStrings

- User-visible copy goes through `Jobsy.Web/Localization/UiStrings` (and partial classes such as `UiStringsExtras`, feature-specific `UiStrings*.cs`).
- Support **nl / en / pl / ro / ar**; missing keys fall back to Dutch.
- Prefer **Lobsy** in user-facing brand strings (not "Jobsy"), except technical identifiers.
- Add keys next to related feature merges; keep parity across languages for new keys.

## EF migrations

- Add migrations in the PR that needs the schema change.
- **Rebase onto latest `acceptatie` before finalizing** so your migration is last in the chain when multiple PRs land.
- Apply locally: `dotnet tool run dotnet-ef database update -p Jobsy.Infrastructure -s Jobsy.Api`.

## Controllers and scope

- New logic → services ([ADR 0002](docs/adr/0002-thin-controllers.md)).
- Respect RegionalManager read-only and company scope ([ADR 0004](docs/adr/0004-roles-and-scope.md), [`docs/security/roles-matrix.md`](docs/security/roles-matrix.md)).

## Useful links

- [`README.md`](README.md) — run / test / branch flow  
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)  
- [`docs/ONBOARDING.md`](docs/ONBOARDING.md)  
- [`docs/ROUTES.md`](docs/ROUTES.md)  
- [`SECURITY.md`](SECURITY.md)  
