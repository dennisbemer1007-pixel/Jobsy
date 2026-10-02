# Support codes (`LB-XXXX`)

When something breaks, the visitor sees a short code instead of technical detail. Support can
find the matching error with that code alone — no screenshots, no "what did you click" guessing.

## What a code looks like

`LB-` plus four characters from `23456789ABCDEFGHJKMNPQRSTVWXYZ`. The characters `0`, `1`, `I`,
`L`, `O` and `U` are left out so a code survives being read out over the phone. The value is
random per error: it is never derived from a user id, a session or a path.

The generator lives in `Jobsy.Core/Diagnostics/SupportCodeGenerator.cs` and is shared by the web
app (`Jobsy.Web/Diagnostics/SupportCode.cs`) and the API (`ExceptionHandlingMiddleware`).
One request produces at most one code: the web side caches it on `HttpContext.Items`.

## Where a visitor sees it

| Surface | Where |
|---|---|
| 500 page (`/Error`) | the "Foutcode" card, plus the `Mail support` link (`subject=Foutcode LB-XXXX`) |
| 429 page (`/status/429`) | the same "Foutcode" card (`Components/Errors/SupportCodeCard.razor`) |
| 503 onderhoudspagina (`/status/503`) | **no code** — maintenance is planned, not a failure, so there is nothing for support to look up. The API's 503 ProblemDetails still carries one (see below). |
| Inline block errors | "Foutcode {LB-XXXX}" under "Dit stukje laadt nu niet." (`InlineErrorBlock`) |
| Circuit error boundary | the same line under "Even iets misgegaan" (`CircuitErrorBoundary`) |
| API errors | `supportCode` in the ProblemDetails body, next to `traceId` |
| API 429 | `supportCode` next to `code: "rate_limited"` and `retryAfterSeconds` |
| API 503 | `supportCode` next to `code: "maintenance"`, `retryAfterSeconds` and `expectedEndUtc` |
| Inline block errors in a page | `supportCode` from the failing API call, shown by `InlineErrorBlock` |

## How support finds the error

1. **Sentry** — search `support_code:LB-7Q3K`. Both the web app and the API set that tag on the
   scope before the error is captured.
2. **Render logs** — search the service logs for the raw code. The web app logs one line per
   failed request:

   ```
   Unhandled error LB-7Q3K 0HN7K1QF6E2PB:00000003 /vacancies/{Id:guid} 7f1c…
   ```

   The API logs `Unhandled exception LB-7Q3K for GET /api/… → 500`.

## What is logged, and what is not

Logged with the code:

- the ASP.NET request id (the same value Sentry records)
- the **route template** of the failing endpoint (e.g. `/vacancies/{Id:guid}`), or the path
  without its query string when the request matched no endpoint
- the user id claim, when the visitor was signed in

Never logged by the error page, and never shown to the visitor:

- query strings, form bodies or headers
- e-mail addresses, names or any other personal data
- the exception message, the stack trace or the request id (the visitor only gets the code)

## How long a code can be looked up (retention)

A code is only useful while the matching log line still exists. Ask the visitor to mail it the
same day; after that the trail depends on where you look.

| Where | How long | Notes |
|---|---|---|
| Render service logs (live tail + search) | the window of the current Render plan — **confirm it in the dashboard**, it is the shortest of the three | after that window the code has no matching request line any more |
| Sentry issues (tag `support_code`) | the event retention of the current Sentry plan | the issue (what broke, how often) outlives the individual event; the tag sits on the event |
| `PlatformLogs` in the database | until the retention job removes the row | these rows carry no e-mail address or name on purpose, so the code is the only handle |
| Log drain | **not configured today** | set one up if support wants a window longer than Render's |

Practical rule: ask the visitor to mail the code the same day. Once Render's window has passed,
only the Sentry issue is left and the individual request is gone. Don't promise a specific number
of days without checking the two dashboards first — both plans can change.

## Rate limits (429)

A rate-limited request is logged once, with the code, the limiter **policy** name
(`public-redirect`, `auth`, …), the route template and the wait in seconds. The visitor's IP is
never in that line: the Web partition key *is* an IP, so the key itself is not logged either.

```
Rate limit rejected LB-7Q3K policy=public-redirect route=/p/{code} retryAfter=60s
```

## No exception text for visitors

`Jobsy.Web/Services/UserFacingError.cs` is the only place that turns an exception into something a
visitor may read. It returns a catalog key (`Common.Error.*`) plus the support code, and logs the
exception once. The ratchet `NoRawExceptionMessageTests` counts `ex.Message` per file against
`docs/errors/ex-message-baseline.txt`: counts may only shrink, a file that is not in the baseline
must be at 0, and the error pages, public pages and layouts are already at 0.

## Reading a code back to a visitor

Codes are case-insensitive to say out loud but are always written in capitals. If a visitor
reads a `0`, `1`, `I`, `L`, `O` or `U`, it is a misheard character — the alphabet has none of
them, so ask them to spell the neighbouring characters again.
