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
