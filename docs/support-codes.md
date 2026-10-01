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
| API errors | `supportCode` in the ProblemDetails body, next to `traceId` |

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

## Reading a code back to a visitor

Codes are case-insensitive to say out loud but are always written in capitals. If a visitor
reads a `0`, `1`, `I`, `L`, `O` or `U`, it is a misheard character — the alphabet has none of
them, so ask them to spell the neighbouring characters again.
