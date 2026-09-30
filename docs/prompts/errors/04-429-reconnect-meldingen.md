# 04. 429 page, translated reconnect toast, inline block error with support code, no ex.Message to users (helper + ratchet guard)

Read `00-README.md` first (§IA, E2, E7, Dependency G). Branch `cursor/errors-4` from `cursor/errors-3`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/errors-4` from `cursor/errors-3` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/errors-3)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/errors-4`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - The ratchet baseline may only shrink. Never add a new `ex.Message` to UI state.

| | |
|---|---|
| Branch | `cursor/errors-4` |
| PR title | `feat(errors): 429 page with Retry-After, reconnect toast and inline errors in 5 languages with support code, UserFacingError helper and ex.Message ratchet guard` |
| Mockups | `er-d06-kleine-meldingen` (reconnect toast, 429, inline block error) |
| Migration | none |
| Split seam | **04a** = 429 + reconnect + `InlineErrorBlock`; **04b** = `UserFacingError` + ratchet + the page sweep |

## 04.1 Today (verify first)
- **429:**
  - Web `Program.cs` L138 `RejectionStatusCode = 429` with no `OnRejected`, so the body is empty
  - API `Program.cs` L96–97 uses `RateLimitPartitioning.OnRejectedAsync` (`Jobsy.Api/Security/RateLimitPartitioning.cs` L86+), which logs; check what body it writes
- **Reconnect toast** `Components/App.razor` L233–244: "Verbinding herstellen…", "Verbinding verbroken." + "Herladen", "Sessie verlopen." are hard-coded Dutch. `BlazorCircuitGuardTests.cs` L59 asserts the Dutch text.
- **`MainLayout.razor` L42–56 `ErrorBoundary`:** `Circuit.ErrorTitle` "Even iets misgegaan" (+ recover). It shows no code.
- **`ex.Message`:** 292 occurrences in 92 `.razor` files under `Jobsy.Web` (`git grep -c "ex\.Message" -- 'Jobsy.Web/**/*.razor'`), plus a few in `.cs`. Many land in `_error`/`_message` fields shown to users.

## 04.2 429
- **Web:** `options.OnRejected` writes, for HTML requests (same filter as the status pages), a re-execute-like render of `/status/429` (or a direct render via `IRazorComponentResult`/`RazorComponentResult<StatusPage>` with code 429), with `Retry-After` from the limiter's `MetadataName.RetryAfter` (default 60). Non-HTML → an empty 429 with `Retry-After`.
- **Page** (er-d06 middle):
  - h1 `Status.TooMany.Title` "Even rustig aan"
  - lead `Status.TooMany.Lead` "Je deed veel verzoeken achter elkaar. Wacht {n} seconden en probeer het dan opnieuw."
  - the support-code card from 01 (E2: 429 shows a code; logged with the partition name, never the IP)
  - button "Opnieuw proberen" → the original GET path (no auto-refresh loop; an optional `<meta http-equiv="refresh">` with the Retry-After value, at least 30 s)
  - noindex, `no-store`
- **API:** `OnRejectedAsync` writes ProblemDetails `{ type, title: "Too many requests", status: 429, code: "rate_limited", retryAfterSeconds, supportCode }` + `Retry-After`. The Web client maps it to `ApiError.RateLimited(seconds)`.

## 04.3 Reconnect toast
- The three texts come from `Status.Reconnect.Trying` "Verbinding herstellen…", `.Failed` "De verbinding is weg." + `.Reload` "Opnieuw laden", `.Rejected` "Je sessie is verlopen." (App.razor renders statically and can read `CultureState`).
- `dir`/`lang` follow the page. Update `BlazorCircuitGuardTests` L59 to assert the key's nl value via the catalog (not a literal) and add an ar assertion.

## 04.4 Inline block error (`Components/Shared/InlineErrorBlock.razor`)
- For a part of a page that fails (card, list, chart), er-d06 right:
  - small card with ⚠️ (aria-hidden) "Dit stukje laadt nu niet." + button "Opnieuw" (an `EventCallback`) + "Foutcode {LB-XXXX}" when the API error carries a `supportCode` (01.4)
  - `role="status"`, no layout shift beyond the card
- `MainLayout`'s `ErrorBoundary` `ErrorContent` uses the same visual (title `Circuit.ErrorTitle` stays, plus a support code from `SupportCode.GetOrCreate` for the circuit error, logged with the exception once, and tagged in Sentry).

## 04.5 `UserFacingError` + ratchet
- **`Jobsy.Web/Services/UserFacingError.cs`:**
  - `From(Exception ex)` → `(string MessageKey, string? SupportCode)`
  - mapping:
    - `ApiError` with a known `code` → its key (`rate_limited`, `not_found`, `forbidden`, `validation`, `maintenance`, plus any codes pages already map)
    - `HttpRequestException`/timeout → `Common.Error.Network` "Geen verbinding. Probeer het zo nog eens."
    - everything else → `Common.Error.TryAgain` "Dat lukte niet. Probeer het zo nog eens."
  - it logs the exception once with the support code
  - validation messages the API returns **for users** (ProblemDetails `detail` on 400 with a `userMessage` flag, if that pattern exists) stay allowed; check `JobsyApiClient`'s error type and document it
- **Ratchet guard `NoRawExceptionMessageTests`:**
  - counts `ex.Message`, `e.Message`, `exception.Message` (and `.InnerException.Message`) in `Jobsy.Web/**/*.razor` and `Jobsy.Web/**/*.cs` **outside** logging calls (`Log…(`) and outside `UserFacingError.cs`
  - compares per file with `docs/errors/ex-message-baseline.txt` (generated in this PR from the current count **after** the sweep below)
  - a file may go down, never up; a new file must be 0
  - the test prints how to regenerate (`JOBSY_UPDATE_EXMESSAGE_BASELINE=1`), and the PR must justify any regeneration that raises a count (never in this stack)
- **Sweep in this PR (must reach 0):**
  - the status/error pages
  - `Pages/Partner/PartnerSales.razor` (L150, L170; if public-pages 09 didn't already)
  - `Pages/Legal/PrivacyData.razor` (L91; if public-pages 07 didn't)
  - `Pages/Public/*`, `CompanyPublicPage.razor`, `VacancyDetail.razor`, `HowLobsyWorks.razor`
  - `Components/Layout/*`
  - every page with an anonymous route (`[AllowAnonymous]`)
  - Others: as many as fit within the split budget, starting with candidate-facing pages. List what's left in `docs/errors-followups.md` (the baseline file is the list).

## 04.6 Strings
`Status.TooMany.*`, `Status.Reconnect.*`, `Status.Inline.*`, `Common.Error.TryAgain`, `Common.Error.Network` (reuse if they exist), 5 languages.

## 04.7 Tests
- `RateLimitPageTests`: exceed a Web limiter in the test host → 429 HTML with `Retry-After`, noindex, the number of seconds in the text; `Accept: application/json` → no HTML. API → ProblemDetails with `code`, `retryAfterSeconds`, `supportCode`.
- `ReconnectToastTests`: App HTML has the 3 texts in nl and ar (culture cookie), no hard-coded Dutch.
- `InlineErrorBlockTests` (bUnit): code shown when present, retry callback fires, no exception text.
- `UserFacingErrorTests`: mapping table; never returns `ex.Message`.
- `NoRawExceptionMessageTests` (the ratchet) green, with the swept files at 0.

## Success criteria
- 429 has a page (HTML) or ProblemDetails (API), both with `Retry-After`.
- The reconnect toast speaks the user's language.
- No anonymous page and no layout shows `ex.Message`; the ratchet prevents new ones.
- PR body: the before/after `ex.Message` count, the swept file list, screenshots of the toast, 429 and inline block (nl + ar).

Done → next: `05-onderhoud.md`.
