# 06. Verification: business e-mail or a letter with a code, admin manual check as fallback

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-6` from `cursor/werkgever-aanmelding-5` (or `-5b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No real letters from Development, CI or acceptatie: `StubLetterService` there, Pingen **staging** only when explicitly configured, Pingen production only with production settings. No Pingen keys in code, tests or logs.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-6` |
| PR title | `feat(verification): employers verify by business e-mail (domain match) or a Pingen letter code; admin review queue /admin/werkgeververificatie` |
| PR body starts with | `Stacked on #<PR 05> (cursor/werkgever-aanmelding-5)` |
| Mockups | `wr-d8` (choose), `wr-d9` (letter on its way + code), `wr-m6`, `wr-m7`. The admin queue has no mockup: use the app admin design system (table + detail drawer, like the existing admin companies pages) |
| Split seam | **06a** = domain match + e-mail verification + manual check + admin queue (06.2, 06.3, 06.5, 06.6). **06b** = letters (06.4) |

## Goal
An unverified bedrijfsmanager proves in a minute that they belong to the company (business e-mail), or within a few days (letter to the KvK address). Anything doubtful ends up with an admin, who decides through the same verification pipeline (03).

## 06.1 Today (verify first)
- `Jobsy.Core/Security/VerificationCodes.cs`: `Hash`/`HashWithPepper`, `MatchesHash` (constant time), `MaxFailedAttempts = 5`. Reuse it for every code here.
- `CompanyVerificationService.MarkVerifiedAsync/MarkPendingAsync/MarkRejectedAsync` (03); `DomainMatch` may already exist from 05.7.
- KVK profile with websites + postadres/bezoekadres (04). `FreeMailDomains` (README §A; create it here if 05 didn't).
- QuestPDF is referenced in `Jobsy.Infrastructure` (use it for the letter PDF). `TransactionalEmails` for mail templates.
- Admin pages live under `Jobsy.Web/Components/Pages/Admin/` (dependency E for placement).

## 06.2 Domain match (D14)
- `Jobsy.Core/Rules/DomainMatch.cs`: `RegistrableDomain(string host)` via a public suffix list (package `Nager.PublicSuffix` with the list embedded as a resource, no runtime download; or an equivalent MIT library; add it to central package management), `bool Matches(string emailDomain, IEnumerable<string> kvkWebsites)`: equal registrable domain, or the e-mail domain is a subdomain of a website's registrable domain. Websites are normalized (scheme, `www.`, path, port, IDN → punycode).
- Free-mail domains never match (even if KVK lists e.g. a `hotmail.com` "website").
- Tests: `groenenzorg.nl` vs `https://www.groenenzorg.nl/contact` ✓; `mail.groenenzorg.nl` ✓; `groenenzorg.co.uk` vs `groenenzorg.nl` ✗; `bakkerij.amsterdam` (new gTLD) ✓; `gemeente.denhaag.nl`-style subdomains ✓; `gmail.com` ✗.

## 06.3 Verification by business e-mail (wr-d8 card 1)
- `/register/verifieren` (public theme, noindex; §IA) shows 2 cards + a quiet link "Lukt geen van beide? Vraag een handmatige controle aan". Card 1 is disabled with the explanation "Bij de KVK staat geen website" when there's none (D14).
- `POST api/company-verification/email/start` `{ email }` (bedrijfsmanager of this unverified root; rate limit `verify-start`): the domain must match (else 400 `domain_mismatch` with the expected domain(s) in the message). Sends a 6-digit code to **that** address, valid 10 min, hashed, `MaxFailedAttempts` 5, max 3 sends per hour. The address may differ from the login e-mail (e.g. `info@`); it's stored on the verification record, not on the user.
- `POST …/email/confirm` `{ code }` → `MarkVerifiedAsync(root, BusinessEmail, user)`. 5 wrong codes → the code is dead, a new one can be requested after 15 min.
- Skipped automatically when D15 already verified in 05.

## 06.4 Verification by letter (D3, wr-d9, wr-m7) (06b)
- `Jobsy.Core/Interfaces/ILetterService.cs`: `Task<LetterSendResult> SendAsync(LetterRequest request, CancellationToken)`, `Task<LetterStatus> GetStatusAsync(string providerLetterId, CancellationToken)`. `LetterRequest(Recipient name, AddressLines, PostalCode, City, Country "NL", byte[] Pdf, string Reference)`.
- `PingenLetterService` (Infrastructure, typed `HttpClient`): Pingen API v2 (OAuth2 client credentials; organisation id; upload the PDF, create the letter with `auto_send`, delivery product "cheap"/economy (ask for "fast" only via a setting); check the endpoints and payload against the current Pingen developer docs and their **staging** environment before writing code). Webhook or daily status poll → `Sent`/`Delivered`/`Undeliverable`.
- `StubLetterService` (Development/CI/acceptatie default): stores the generated PDF (which contains the code) in the stub letter store instead of posting it, and the admin queue offers **"Bekijk testbrief"** for stub letters only, so testers can finish the flow without real post. The code itself is never logged. Pingen letters' PDFs are not kept after sending.
- Settings `CompanyVerificationSettings` (options + admin Integraties panel, like the KVK key): `LetterProvider` (Stub|Pingen), `PingenClientId/Secret/OrganisationId` (secrets), `PingenEnvironment` (Staging|Production), `MonthlyLetterCap` 500 (README §A), `LetterValidityDays` 30, `LetterResendAfterDays` 7, `LetterMaxResends` 2.
- Entity `CompanyVerificationLetter` (migration `AddCompanyVerificationLetters`): company id, KvK number, requested by, code hash, `ExpiresAtUtc` (+30 days), `FailedAttempts`, `ResendCount`, `ResendOfLetterId?`, address snapshot (the address the letter went to), provider + provider id, status (Requested/Sent/Delivered/Undeliverable/Used/Expired/Blocked), timestamps.
- Rules (D3): code 8 characters Crockford base32 without 0/O/1/I/L, shown `XXXX-XXXX`, input accepts lowercase, spaces and a missing dash. Address: postadres, else bezoekadres; heel bedrijf → hoofdvestiging, a single vestiging → that vestiging. Max 1 letter per KvK number per 30 days (a resend doesn't count); resend only ≥ 7 days after the last send, max 2; a resend invalidates the earlier code. 5 wrong codes → `Blocked` → the admin queue ("Brief geblokkeerd na 5 pogingen"). The monthly cap reached → the letter option shows "Tijdelijk niet beschikbaar, vraag een handmatige controle aan".
- Letter PDF (QuestPDF, **nl** only, A4, window envelope position): Lobsy logo, the address, date, "Beste {bedrijfsnaam}", 3 short lines (who requested it: name + function, no e-mail), the code large, "Voer de code in op lobsy.nl/register/verifieren/brief of in je dashboard", valid until {date}, and "Heb je dit niet aangevraagd? Mail naar {support}; zonder code gebeurt er niets."
- `POST api/company-verification/letter` (request or resend; `verify-start`), `POST …/letter/confirm` `{ code }` → `MarkVerifiedAsync(root, Letter, user)`. Status `Pending` while a letter is underway (03 keeps everything hidden).
- `/register/verifieren/brief` (wr-d9): "Je brief is onderweg" with the masked address, expected delivery "1–3 werkdagen" from the send date (computed dates, README mockup notes), the code field, "Nog niets ontvangen?" (resend from day 7). The same code entry sits in the dashboard (11).

## 06.5 Manual check (D16)
- `POST api/company-verification/manual` `{ reason, message?, attachmentIds? }` (e.g. a KvK extract upload through the existing upload service, PDF/PNG/JPG ≤ 5 MB). Status `Pending`; the page says "We reageren binnen 2 werkdagen".
- An open manual check postpones the 60-day deletion (03.6).

## 06.6 Admin queue `/admin/werkgeververificatie`
- Placement per dependency E. Tabs: **Handmatig**, **Gemarkeerd** (README §A heuristics), **Brieven** (blocked/undeliverable/cap), and empty tab slots that 07 (Toegang), 09 (Betrokkenheid) and 10 (Waadi) fill.
- Per row: company, KvK number, what KVK says (name, address, SBI, websites), the requester (name, function, e-mail, login method), the registration timestamps, the heuristics that fired, previous rejections for this KvK. Actions: **Goedkeuren** (→ `MarkVerifiedAsync(root, Manual, admin, note)`), **Afwijzen** with a required reason (→ `MarkRejectedAsync`; the e-mail to the user contains the reason), **Brief sturen** (admin sends a letter even over the per-KvK limit).
- After 3 rejections for one KvK number (D16), new registrations for it are flagged automatically.
- Every decision is audited (dependency E: `IAdminAuditLog` or `PlatformLog`).
- Count badge in the admin nav; a daily admin digest e-mail when items are older than 2 working days (reuse any existing admin digest).

## Tests
- `DomainMatchTests` (06.2 list), `FreeMailDomainsTests`.
- E-mail flow: mismatch 400; code hashed in DB; 10-min expiry; 5 wrong → dead; confirm → Verified/BusinessEmail and 03 pipeline effects (vacancies published, welcome token).
- Letter: code alphabet + formatting + lenient input; address choice (postadres > bezoekadres; hoofdvestiging vs vestiging); 1 per KvK per 30 days; resend before day 7 rejected, 3rd resend rejected, resend kills the old code; 5 wrong → Blocked + queue item; cap 500 → blocked + manual offered, alert at 400; expiry 30 days; `StubLetterService` used when not configured; `PingenLetterService` against a fake `HttpMessageHandler` (auth, upload, create, status); PDF renders (QuestPDF smoke test, contains the code).
- Manual + admin: approve → Verified/Manual; reject → Rejected + reason mail; 3 rejections → auto-flag; audit rows written; non-admins 403.
- Playwright: choose → e-mail → code → verified; choose letter → stub → enter code → verified.

## Success criteria
- Both methods verify a company end-to-end via `MarkVerifiedAsync`. No code is ever logged or stored in plain text (stub-letter PDFs excepted). No real letter can leave from Development, CI or acceptatie without explicit Pingen configuration.

Done → next: `07-toegang-aanvragen-eigendom.md`.
