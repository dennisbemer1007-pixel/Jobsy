# 02. Geen toegang (403): real status at the requested URL, return URL kept, account shown, switch account

Read `00-README.md` first (§IA, E6, Dependencies E and F). Branch `cursor/errors-2` from `cursor/errors-1`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/errors-2` from `cursor/errors-1` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/errors-1)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/errors-2`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Don't weaken `AuthRedirects.ResolveRequestedReturnUrl` / `SafeLocalUrl`. Only local return URLs.

| | |
|---|---|
| Branch | `cursor/errors-2` |
| PR title | `feat(errors): Geen toegang as a real 403 at the requested URL with the account name, "Naar mijn start" and "Inloggen met een ander account"` |
| Mockups | `er-d03-geen-toegang`, `er-m03-geen-toegang` |
| Migration | none |

## Goal
A signed-in user without the right role stays on the URL they asked for, gets a 403, sees which account they're using, and can switch account and come back to the same page.

## 02.1 Re-check Dependencies E and F
Write the cases in the PR.

## 02.2 Today (verify first)
- `Components/Routes.razor` `NotAuthorized`: an authenticated user → `<RedirectToAccessDenied />` (`Components/RedirectToAccessDenied.razor`), which does `Navigation.NavigateTo("/access-denied", forceLoad: true)`. So the original URL is lost and the final response is **200**.
- `Pages/AccessDenied.razor` (19 lines): `Access.Title` / `Access.Lead`, links `/home` and `/login`, `login-card` markup under `MainLayout`.
- The cookie option `AccessDeniedPath = "/access-denied"` (`AuthServiceCollectionExtensions.cs` L55) is used for endpoint `[Authorize]` failures.
- `/account/logout` (L527) accepts GET/POST and redirects to `/` (or session-expired with `returnUrl`).

## 02.3 Behaviour
- **Router path:** replace `RedirectToAccessDenied` for authenticated users with an in-place render: `NotAuthorized` renders `<AccessDeniedView ReturnUrl="{current path+query}" Reason="role" />` inside `ErrorLayout`.
  - For the static SSR / prerender pass, set `Response.StatusCode = 403` (when `!Response.HasStarted`), the same pattern as `MarkNotFound`.
  - Interactive navigations (client-side, no HTTP response) render the same view without a status, which is fine.
  - Keep `RedirectToAccessDenied.razor` only if other code uses it; otherwise delete it.
- **Cookie `AccessDeniedPath`** (endpoint `[Authorize]`): keep the path, but `/access-denied` renders the same view with **403** and reads `ReturnUrl` from the query (`returnUrl`, which ASP.NET appends as `ReturnUrl`; accept both, then `AuthRedirects.SafeLocalUrl`).
- **`reason=` variants:**
  - `role` (default) "Deze pagina is niet voor jouw account."
  - `employers-off` (F present): "Werkgevers kunnen Lobsy nu even niet gebruiken. We laten het je weten als het weer kan." (no switch-account button, "Naar mijn start" only if a candidate home exists; else "Naar de voorpagina")
  - unknown → `role`.

## 02.4 Page (er-d03)
- eyebrow chip "🔒 Geen toegang", h1 `Status.Forbidden.Title` "Deze pagina is niet voor jouw account", lead `Status.Forbidden.Lead` "Je bent ingelogd, maar dit deel van Lobsy hoort bij een andere rol."
- **Account card:** "Je bent ingelogd als **{naam}**" + the masked e-mail (`d***@example.nl`, same masking as `EmailServiceStub.RedactEmail`) + the role label. All from the cookie claims (no API call). No claims for a name → only the masked e-mail.
- **Buttons:**
  - **"Naar mijn start"** (primary) → `FeatureRoutes.HomeFor(user)` if present, else `AuthRedirects.PostLoginUrl(null)` (today `/home`)
  - **"Inloggen met een ander account"** (secondary) → a POST form (antiforgery) to the switch path (Dependency E): logout, then `/login?returnUrl={ReturnUrl}`
- Small line: "Denk je dat je hier wel moet kunnen komen? Mail {SupportEmail}."
- **Strings:** `Status.Forbidden.*` in 5 languages. Keep `Access.*` keys only if used elsewhere; otherwise remove them (parity test).

## 02.5 Switch account (Dependency E absent)
- `/account/logout` POST with `reason=switch&returnUrl=…`: sign out as today (device session, cookies), then 302 to `/login?returnUrl={SafeLocalUrl(returnUrl)}`. A GET with `reason=switch` does the same (bookmarks), but the page uses POST.
- Never an open redirect: an absolute or `//` URL → `/login` without `returnUrl` (test).

## 02.6 Tests
- `ForbiddenStatusTests`:
  - a candidate GETs an admin page (`/admin`) → **403**, the HTML has `Status.Forbidden.Title`, noindex, and the URL is unchanged (no 302)
  - the same for an endpoint `[Authorize(Roles=…)]` → `/access-denied?ReturnUrl=…` → 403
  - anonymous → still 302 to `/login?returnUrl=` (unchanged)
- `ForbiddenViewTests` (bUnit): the account card uses claims only (the API client is a throwing fake); the masked e-mail; the form posts to the switch path with the return URL.
- `SwitchAccountTests`: POST logout `reason=switch&returnUrl=/admin/users` → signed out, 302 `/login?returnUrl=%2Fadmin%2Fusers`; `returnUrl=https://evil.example` → 302 `/login`.
- `EmployersOffReasonTests` (F present): `reason=employers-off` copy, no switch button.
- `BlazorPageRoleAttributesTests`, `RoutesDocFreshnessTests` green.

## Success criteria
- No forceLoad redirect to `/access-denied` for authenticated users remains. The requested URL answers 403 with the new page.
- Switching account returns to the requested page after login (E2E in 06).
- PR body: dependency cases E/F, status table, screenshots nl desktop/mobile + ar mobile.

Done → next: `03-vacature-gesloten-410.md`.
