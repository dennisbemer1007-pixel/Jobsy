# 05. Maintenance mode: admin switch, 503 page with Retry-After, and a static fallback page for Cloudflare/Render (plan check first)

Read `00-README.md` first (§IA, E5, E8, Dependencies C and D). Branch `cursor/errors-5` from `cursor/errors-4`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/errors-5` from `cursor/errors-4` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/errors-4)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/errors-5`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Don't change `render.yaml`, Cloudflare or Render dashboards/settings. Dennis does that from `docs/onderhoud.md`. `/healthz` and `/health` must stay untouched (no API call, no 503).

| | |
|---|---|
| Branch | `cursor/errors-5` |
| PR title | `feat(errors): maintenance mode with an admin switch, a 503 page with Retry-After, an admin bypass banner and a static fallback page for Cloudflare/Render` |
| Mockups | `er-d04-onderhoud` (in-app page + static 5-language variant), `er-m04-onderhoud`; the admin switch has no mockup (follow Dependency D) |
| Migration | `AddMaintenanceMode` (2 nullable/default columns on `PlatformFeatureSettings`) |

## Goal
Dennis flips one switch before a risky deploy or migration. Visitors see a calm "we're doing maintenance, back at 14:30" page in their language with a real 503. Admins can still log in and look around. When the app itself is down (not a planned switch), Cloudflare or Render shows a static copy of the same page.

## 05.1 First: plan check (E1), then re-check Dependencies C and D
- Do the Cloudflare/Render plan check from 05.7 **before** writing code, and put the findings (date, URLs, plan-dependent options) at the top of the PR body. This is a check only; nothing gets configured.
- **C (docs/emails `AmsterdamTime`):** present → use it for the expected end time. Absent → a small `TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam")` helper in `Jobsy.Core/Time/` (don't copy the emails one).
- **D (docs/admin-redesign):**
  - present → an `AdminToggleRow` in "Platforminstellingen → Functies" (`/admin/instellingen`) with an `AdminImpactNote` (danger) "Iedereen behalve admins ziet de onderhoudspagina.", audit via `IAdminAuditLog`
  - absent → a section "Onderhoud" at the top of `/admin/settings` (`SettingsAdmin.razor`) with the same fields, audit via a `PlatformLog` row `maintenance.on|off` (Info, "Onderhoudsmodus aan/uit door {admin}"), plus a follow-up line
- Write the outcome in the PR.

## 05.2 Data + API
- **`PlatformFeatureSettings`** (singleton, `Jobsy.Core/Entities/PlatformFeatureSettings.cs`) gets:
  - `bool MaintenanceEnabled` (default false)
  - `DateTime? MaintenanceExpectedEndUtc`
  - `string? MaintenanceNote` (max 200, admin-only, never shown publicly)
  - Migration `AddMaintenanceMode`, snapshot updated.
- **`GET api/site/status`**, anonymous, cached 5 s, allowed during maintenance: `{ maintenance: bool, expectedEndUtc?: string }`.
- **Admin `PUT api/admin/settings/maintenance`** `{ enabled, expectedEndUtc? }`, admin role only, audited.
- **API `MaintenanceApiMiddleware`**, after auth. When enabled, non-admin requests get 503 ProblemDetails `{ code: "maintenance", expectedEndUtc, supportCode }` + `Retry-After`. Allowed through:
  - `/health`, `api/site/status`
  - `api/auth/*` (so admins can log in)
  - `api/admin/*` for admins
  - any request with an admin principal
- **Web client:** `ApiError.Maintenance` → `UserFacingError` key `Status.Maintenance.Short` (04.5).

## 05.3 Web `MaintenanceMiddleware`
- Reads the state through a singleton `MaintenanceState` that polls `api/site/status` every **15 s** (background `PeriodicTimer`, with a timeout).
  - If the API is unreachable, the **last known** state is kept; on startup it defaults to off.
  - It never blocks a request on the API.
- When on, for **non-admin HTML** requests: render `/status/503` (via the 01 renderer) with **503** + `Retry-After` (E8), `no-store`, noindex.
- **Allow-list:**
  - `/login`, `/account/*`, `/status/*`
  - static files / `_framework` / `_content` / `css` / `js` / `img`
  - `/healthz`, `/robots.txt`
  - `/api/auth*` if proxied
  - `/_blazor` for authenticated admins only
- Non-HTML non-admin → an empty 503 + `Retry-After`.
- **Admins** (role check on the cookie principal) pass through and see a thin red banner at the top of every layout: "Onderhoudsmodus staat AAN. Bezoekers zien de onderhoudspagina." + a link to the switch. The banner key is `Admin.Maintenance.Banner`; admin UI can stay nl + en.
- **Retry-After (E8):** expected end in the future → the seconds until then, clamped 60–3600. Otherwise → 300. `/healthz` always answers 200.

## 05.4 Page (er-d04 in-app variant, er-m04)
- `StatusPage` code 503, variant `maintenance`:
  - `LobsyMascot` (wrench pose if it exists, else the default) or the 01 fallback SVG
  - h1 `Status.Maintenance.Title` "We zijn even aan het klussen"
  - lead: "Lobsy is zo terug." Only when an end time is set (E8): "We verwachten terug te zijn om {HH:mm}" (Amsterdam time, formatted per culture, plus the day if it's not today)
  - a support line: "Vragen? Mail support" (E9 `mailto:`)
- No login button for visitors; a small "Beheerder? Inloggen" link to `/login`.
- 5 languages, ar RTL. No auto-refresh script; a `<meta http-equiv="refresh" content="{Retry-After}">` capped at 300 is allowed.

## 05.5 Admin switch (no mockup; Dependency D decides the shell)
- **Fields:**
  - toggle "Onderhoudsmodus"
  - optional "Verwacht klaar om" (date + time, Amsterdam, converted to UTC)
  - optional internal note
- **Turning it on:** a confirm dialog "Bezoekers zien vanaf nu de onderhoudspagina. Jij blijft erin." The UI shows "Actief sinds {time} door {admin}".
- **Turning it off:** no confirm.
- **Propagation:** changes take up to 15 s. The UI says so ("Binnen 15 seconden overal actief").

## 05.6 Static fallback page `ops/maintenance/`
- **`ops/maintenance/index.html`:**
  - one self-contained file, ≤ **100 KB**
  - inline CSS + inline SVG mascot/logo, **no scripts**, no external fonts/images
  - all **5 languages** stacked (nl first, then en, pl, ro, ar with `dir="rtl"` on its block), each with a `lang` attribute
  - text: "We zijn even aan het klussen. Lobsy is zo terug." + `support@lobsy.nl`
  - `<meta name="robots" content="noindex">`
- **`ops/maintenance/cloudflare-500.html`:** the same, plus Cloudflare's required token `::CLOUDFLARE_ERROR_500S_BOX::` placed in a visually hidden-but-present element. Verify the exact token name and requirement in the Cloudflare docs **at build time** and write the doc URL in `docs/onderhoud.md`.
- **Tests:** size, no `<script`, all 5 `lang` values present, token present in the Cloudflare variant.

## 05.7 `docs/onderhoud.md`: runbook + plan check (a check, not an action)
- **Planned maintenance:** switch on in admin → wait 15 s → deploy/migrate → check → switch off.
- **Plan capabilities** (the 05.1 check): look them up again in the current docs and write the date checked + URLs.
  - **Cloudflare Custom Error Pages** (500-class/1xxx) need a **paid plan**. Our notes from 2026-09-30: Pro and up; Free has none; some origin-error variants are Enterprise-only.
    - Paid → upload `cloudflare-500.html` (hosted at a public URL, e.g. Render static site or Cloudflare Pages) under Custom Pages / Error Pages.
    - Free → options: (a) a Cloudflare Worker that serves the page on origin 5xx; (b) rely on Render maintenance mode only; (c) nothing.
  - **Render maintenance mode** (paid web services): dashboard toggle or blueprint `maintenanceMode: { enabled, uri }`. The `uri` must be a page hosted **outside** the service (e.g. a Render static site from `ops/maintenance/`). Render answers 503.
- **Dennis decides and configures;** list the exact clicks. Cursor doesn't touch `render.yaml`, the dashboards or DNS.
- **Unplanned outage:** what the visitor sees per plan option.

## 05.8 Strings
`Status.Maintenance.*` (5 languages), `Admin.Maintenance.*` (nl + en).

## 05.9 Tests
- `MaintenanceMiddlewareTests` (Web test host with a fake `MaintenanceState`):
  - off → normal
  - on → `/` and `/vacatures` give 503 + `Retry-After` + noindex + `no-store`, and the page text is in the cookie language
  - `/login`, `/healthz`, `/css/app.css`, `/status/404` pass
  - an admin principal passes and the banner shows
  - an API outage keeps the last state
- **Retry-After cases:** end in 2 min → 120; end in 3 h → 3600; no end → 300; end in the past → 300.
- `MaintenanceApiTests`: 503 ProblemDetails for non-admin; `/health`, `api/site/status`, `api/auth/login` pass; admin passes; `PUT` requires admin and writes an audit/PlatformLog entry.
- The existing `BlazorReconnectAndHealthzTests` stay green (`/healthz` unchanged).
- `MaintenanceStaticPageTests` (05.6).

## Success criteria
- One switch puts the public site into a real 503 maintenance page within 15 s; admins keep working with a visible banner.
- Health checks never go to 503.
- The static page exists in both variants; `docs/onderhoud.md` has the plan check with dates, URLs and a clear "Dennis decides" list.
- PR body: the dependency outcome, screenshots (page nl + ar, admin switch, banner) and the plan check summary.

Done → next: `06-e2e-rapport.md`.
