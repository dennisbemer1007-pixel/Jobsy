# 04. Languages: nl/en/pl/ro/ar with the recipient's language, RTL, culture-aware dates and numbers

Read `00-README.md` first (§0 Strings, D4, D11). Branch `cursor/emails-4` from `cursor/emails-3`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-4`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - User data (vacancy titles, company names, people's names, addresses) is never translated.
> - nl and en are final. pl, ro and ar are B1 drafts; **pl and ar are flagged for native review before go-live**.

| | |
|---|---|
| Branch | `cursor/emails-4` |
| PR title | `feat(email): 5 mail languages (nl/en/pl/ro/ar), recipient language with nl fallback, RTL + bidi isolation, Europe/Amsterdam dates` |
| PR body starts with | `Stacked on #<PR 03> (cursor/emails-3)` |
| Mockups | `em-d09`/`em-m09` (ar, RTL) |
| Split seam | **04a** = catalog + resolver + formatting + common strings + the move of today's nl copy into keys (04.2–04.5); **04b** = en/pl/ro/ar values + review doc (04.6–04.7) |

## Goal
Every mail goes out in the recipient's language: the one they chose in Lobsy, else Dutch. Arabic mails read right-to-left without scrambling Dutch names or numbers.

## 04.1 Today (verify first)
- `JobsyLanguages` (`Jobsy.Core/Localization/JobsyLanguages.cs`): nl (default), en, pl, ro, ar (`IsRightToLeft`, culture `ar-SA`).
- The user's choice lives in `User.PreferencesJson` → `Preferences.Language` (`CultureState.SetLanguageAsync` → `UpdateMyLanguageAsync`). Web sends `X-Jobsy-Language` on API calls (`JobsyApiAuthHandler` L60). The API reads it in a few controllers (`MeController` L582, `VacanciesController` L1864, …).
- UI strings live in `Jobsy.Web/Localization/UiStrings*.cs`; `LocalizationParityReportTests` + `docs/i18n/untranslated-baseline.txt` guard them. Mail text is not localized at all.

## 04.2 Catalog (`Jobsy.Core/Email/Localization/`)
- `EmailStrings` is a dictionary per language: `EmailStrings.Nl.cs`, `.En.cs`, `.Pl.cs`, `.Ro.cs`, `.Ar.cs`, registered in one `EmailStrings.All`. It lives in Core because the templates and hosted jobs are in Core/Infrastructure.
- Keys:
  - `Email.Common.*`: greeting forms, sign-off, footer labels (`Help`, `Privacy`, `MailSettings`, `Unsubscribe`), `Reason.{ReasonKey}` for every reason in §M, date words
  - `Email.{TemplateKey}.{Subject|Preheader|Eyebrow|Heading|P1…|Facts.*|Steps.*|Cta|Note}`
- Values are plain text with `{0}` placeholders; **no HTML** (guard test). Bold and links come from `EmailArg` in the template (02).
- `EmailText.Format(culture, key, args)` looks up the value in the language, else nl. A missing nl key throws in Testing.
- 04a moves today's nl copy (ported in 02) into `Email.*` keys **unchanged**. The B1 rewrite in 05–07 only edits values.
- The nl literals the renderer still has from 02/03 (footer labels, reason sentences, the "Afmelden voor deze mails" line, each marked `// 04`) move to `Email.Common.*`. A source test fails while any `// 04` marker remains in `Jobsy.Core/Email`.

## 04.3 Recipient language (`IEmailLanguageResolver`, Infrastructure)
`Task<EmailCulture> ResolveAsync(EmailRecipient recipient, CancellationToken)`, where `EmailRecipient` is one of the following. First match wins; unsupported codes normalize via `JobsyLanguages.Normalize`, else nl.
- `User(userId)` → `Preferences.Language`.
- `Requester(language)` → the `X-Jobsy-Language` of the request that triggered the mail. Used when the recipient **is** the requester and may have no account yet: the application OTP, the registration code, the takeover requester, the API-key technical contact.
- `ParentOf(childUserId)` → the child's language (README D11).
- `Address(email)` → the user with that e-mail if one exists, else nl.
- Employer mails triggered by a candidate resolve **each employer recipient's** own language. Jobs resolve per recipient.
- A small helper `HttpContext.GetJobsyLanguage()` in the API reads the header once (replace the copies in the 4 controllers only if trivial; otherwise leave them and note it).
- The mailer (02) takes `EmailCulture` in `EmailSendOptions`. `Compose` methods take `EmailCulture` as their first parameter.

## 04.4 Formatting (`EmailFormat`, Core)
- `Date(DateTime utc, EmailCulture c)` → Europe/Amsterdam (`AmsterdamTime` from 01), pattern `d MMMM yyyy`; `DateTime` → `d MMMM yyyy, HH:mm`. With a time, add the zone words from `Email.Common.TimeZoneNl` ("Nederlandse tijd" / "Dutch time" / …). **Never UTC.**
- Culture objects: `CultureInfo(JobsyLanguages.ToCultureName(code))`, **cloned**. For `ar` set `DateTimeFormat.Calendar = new GregorianCalendar()` (ar-SA defaults to Um Al-Qura) and keep Latin digits (`NumberFormat.DigitSubstitution` stays None; don't use native digits). A test asserts `30 سبتمبر 2026` and not a Hijri date.
- `Money(decimal, c)` → `€ 12,50` style per culture with the EUR symbol; `Km(double, c)` → 1 decimal with the culture's separator + "km".
- Durations from constants (`"{0} minuten"` via a key with the number from the owning constant; 05–07 use it).

## 04.5 RTL and bidi
- The renderer (02) already switches `dir`/alignment. In 04:
  - Wrap every user-data value in `<bdi>` in HTML (names, titles, company, e-mail, URLs shown as text). A template-level `EmailArg` flag `Isolate` defaults to true for user data.
  - In the **text part and the subject/preheader**, wrap user-data values for RTL languages in U+2068 FIRST STRONG ISOLATE … U+2069 POP DIRECTIONAL ISOLATE. Only for `ar`; other languages get no control characters.
  - The facts table swaps label/value columns in RTL, the steps number sits on the right, and the mascot sits at the inline-start (match em-d09).
  - The logo row mirrors, and "Lobsy" stays LTR (`dir="ltr"` span).
  - Codes (`data-lobsy-otp`) are `dir="ltr"`.

## 04.6 Translations
- en: final, plain British English (matches `en-GB`), same tone rules (short, "you", no jargon).
- pl, ro, ar: B1 drafts. Keep sentences short and literal. No idioms. Use the same terms the app uses (check `UiStrings` for "sollicitatie", "vacature", "werkgever" in each language and reuse those words).
- `docs/i18n/emails-review.md` (03 created it for the UI strings; extend it): one table per language (key, nl, draft). The header says: **"pl and ar: native review required before go-live (Dennis, 30-09)"**, "ro: spot-check by a native speaker recommended". List who reviewed (empty for now).
- `LocalizationParityReportTests` gets a sibling `EmailStringsParityTests`:
  - every key exists in all 5 languages
  - the placeholder sets are identical per key
  - no value contains `<` or `>`
  - no value is identical to nl except an allow-list (brand words, "Lobsy", e-mail addresses)
  - a baseline file `docs/i18n/email-untranslated-baseline.txt` that may not grow (0 at the end of this file)

## 04.7 Language-specific checks
- Subject length: nl/en ≤ 60 characters with sample data; warn (not fail) above 70 for pl/ro/ar in the test output.
- Arabic greeting uses "مرحبًا {0}،" (Arabic comma); the sign-off is "فريق Lobsy". Polish uses the informal "Cześć {0}," for candidates and "Dzień dobry," for others. Romanian uses "Salut, {0}," / "Bună ziua,". en uses "Hi {0}," / "Hello {0},".

## Tests
- `EmailLanguageResolverTests`: user preference wins; requester header; parent = child's language; address lookup; unsupported code → nl; employer recipients resolve their own language, not the candidate's.
- `EmailFormatTests`: Amsterdam summer/winter (a UTC 22:30 on 30-09 shows 1 oktober 00:30); ar Gregorian and Latin digits; money/km per culture.
- `EmailRendererRtlTests`: ar has `dir="rtl"`, `lang="ar"`; user values are in `<bdi>`; the text part has U+2068/U+2069 around user values only for ar; the code is LTR.
- `EmailStringsParityTests` (04.6).
- Snapshots (02) gain `{key}.en.txt` and `{key}.ar.txt` for every key (nl stays). Regenerate with the env var.
- A mail triggered from a request with `X-Jobsy-Language: pl` for an anonymous applicant arrives in pl; the same mail for a user with `Preferences.Language = ro` arrives in ro.

## Success criteria
- Every registry key composes in all 5 languages with no nl fallback used (test counts fallbacks = 0).
- The PR shows ApplicationConfirmation in nl, en and ar (desktop + mobile) and ar dark.
- `docs/i18n/emails-review.md` exists with the pl/ar native-review flag.

Done → next: `05-kandidaat-mails.md`.
