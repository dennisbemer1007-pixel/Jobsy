# Email strings review (file 04)

**pl and ar: native review required before go-live (Dennis, 30-09)**  
**ro: spot-check by a native speaker recommended**

Reviewed by: _(empty — pending)_

Languages: nl + en are final. pl / ro / ar are B1 drafts. User data (names, vacancy titles, company names) is never translated.

## Reviewers

| Language | Reviewer | Date | Status |
|---|---|---|---|
| pl | | | pending native review |
| ar | | | pending native review |
| ro | | | spot-check recommended |
| en | | | final (British English) |
| nl | | | final |

## Notes

- Keys live in `Jobsy.Core/Email/Localization/EmailStrings.*.cs`.
- Parity is guarded by `EmailStringsParityTests` and `docs/i18n/email-untranslated-baseline.txt` (must not grow).
- Arabic uses Gregorian calendar + Latin digits; RTL + `<bdi>` / U+2068…U+2069 isolation for user data.
- Full key tables for translators: regenerate from the catalogs when preparing a review pass (admin preview in file 08).

## Sample keys (ApplicationConfirmation)

| Key | nl | en | pl | ro | ar |
|---|---|---|---|---|---|
| Subject | Sollicitatie bevestigd: {0} | Application confirmed: {0} | Aplikacja potwierdzona: {0} | Candidatură confirmată: {0} | تم تأكيد الطلب: {0} |
| Heading | Sollicitatie verstuurd! | Application sent! | Aplikacja wysłana! | Candidatură trimisă! | تم إرسال الطلب! |
| Cta | Bekijk mijn sollicitaties | View my applications | Zobacz moje aplikacje | Vezi candidaturile mele | عرض طلباتي |
