# Error pages — translation review (`Status.*`)

Source: `Jobsy.Web/Localization/UiStringsStatus.cs` (errors stack 01).

**nl and en are final.** The **pl, ro and ar** texts below are B1 drafts written for this stack
and still need a native-speaker pass. The Arabic copy in the mockup `er-m05-404-arabisch-rtl`
was a placeholder; the strings in the catalog are the authoritative draft.

## What a reviewer should check

- B1 level, "je" / informal second person, short sentences.
- Calm and never blaming: the mistake is ours, not the visitor's.
- No technical words — no "server", "exception", "HTTP", "request", "error 500".
- Arabic renders right-to-left; check that the Latin brand name `Lobsy` and the support code
  (`LB-7Q3K`) read correctly inside an RTL line.

## Keys to review

| Key | nl (final) | Needs review in |
|---|---|---|
| `Status.NotFound.Title` | Deze pagina bestaat niet | pl, ro, ar |
| `Status.NotFound.Lead` | Misschien is de link oud, of zit er een typfout in. Geen zorgen, we helpen je verder. | pl, ro, ar |
| `Status.Error.Eyebrow` | Foutje bij ons | pl, ro, ar |
| `Status.Error.Title` | Er ging iets mis | pl, ro, ar |
| `Status.Error.Lead` | Het ligt niet aan jou. Probeer het zo nog eens. Lukt het niet? Stuur ons de code hieronder. | pl, ro, ar |
| `Status.Generic.Title` | Deze pagina kan nu niet geopend worden | pl, ro, ar |
| `Status.Generic.Lead` | Probeer het zo nog eens, of ga terug naar het begin. | pl, ro, ar |
| `Status.Common.Banenkaart` | Banenkaart | pl, ro, ar |
| `Status.Common.Passport` | Mijn Paspoort | pl, ro, ar |
| `Status.Common.FreeTest` | Gratis test | pl, ro, ar |
| `Status.Common.Help` | Hulp | pl, ro, ar |
| `Status.Common.TryAgain` | Probeer opnieuw | pl, ro, ar |
| `Status.Common.MailSupport` | Mail support | pl, ro, ar |
| `Status.Common.CodeLabel` | Foutcode | pl, ro, ar |
| `Status.Common.Copy` / `Status.Common.Copied` | Kopieer / Gekopieerd | pl, ro, ar |
| `Status.Common.SupportLine` | Klopt er iets niet? Mail {0}. | pl, ro, ar |
| `Status.Common.MailSubject` | Foutcode {0} | pl, ro, ar |
| `Status.Nav.*` | header / footer labels | pl, ro, ar |

`Status.NotFound.Chip` is the number `404` in nl/en/pl/ro and Eastern Arabic numerals in ar;
no translation needed beyond confirming the numeral style for Arabic readers.

## Notes

- `{0}` placeholders must stay in every language: `Status.Common.SupportLine` takes the support
  mailbox, `Status.Common.MailSubject` takes the support code.
- The footer copyright line is built in code (`© {year} Lobsy`), not translated.
- Later files in this stack add `Status.*` keys for 403, 410, 429 and maintenance; add them to
  this review when they land.

## errors 04 — 429, reconnect toast, inline errors (nl/en final, pl/ro/ar B1 drafts)

| Key | nl | Review needed |
|---|---|---|
| `Status.TooMany.Eyebrow` / `Status.TooMany.Title` | Even rustig aan | pl, ro, ar |
| `Status.TooMany.Lead` | Je deed veel verzoeken achter elkaar. Wacht {0} seconden en probeer het dan opnieuw. | pl, ro, ar |
| `Status.Reconnect.Trying` | Verbinding herstellen… | pl, ro, ar |
| `Status.Reconnect.Failed` | De verbinding is weg. | pl, ro, ar |
| `Status.Reconnect.Rejected` | Je sessie is verlopen. | pl, ro, ar |
| `Status.Reconnect.Reload` | Opnieuw laden | pl, ro, ar |
| `Status.Inline.Title` | Dit stukje laadt nu niet. | pl, ro, ar |
| `Status.Inline.Retry` | Opnieuw | pl, ro, ar |
| `Status.Inline.Code` | Foutcode {0} | pl, ro, ar |
| `Common.Error.TryAgain` | Dat lukte niet. Probeer het zo nog eens. | pl, ro, ar |
| `Common.Error.Network` | Geen verbinding. Probeer het zo nog eens. | pl, ro, ar |
| `Common.Error.RateLimited` | Even rustig aan. Wacht een momentje en probeer het opnieuw. | pl, ro, ar |
| `Common.Error.NotFound` | Dit kunnen we niet vinden. | pl, ro, ar |
| `Common.Error.Forbidden` | Dit mag met jouw account niet. | pl, ro, ar |
| `Common.Error.Validation` | Controleer wat je hebt ingevuld en probeer het opnieuw. | pl, ro, ar |
| `Common.Error.Maintenance` | We zijn even aan het werk aan Lobsy. Probeer het zo nog eens. | pl, ro, ar |

Notes for translators:

- `{0}` in `Status.TooMany.Lead` is a number of seconds, in `Status.Inline.Code` the support code
  (`LB-7Q3K`); both must stay.
- The reconnect toast is the only copy rendered before the Blazor circuit exists. It is read from
  the catalog with the request language (cookie → `Accept-Language` → nl), so a translation lands
  there without any extra wiring.
- `Common.Error.*` are the fallbacks `UserFacingError` picks when an action fails. They must stay
  short enough to fit a one-line inline card.
