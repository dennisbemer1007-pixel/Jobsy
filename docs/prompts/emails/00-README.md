# Lobsy e-mails: security hotfix + a clean redesign of all transactional mails (Cursor run book)

Cursor: **read this file completely**, then **execute the files below strictly in order**, one at a time. Each file is one PR.

> **Rules (repeated in every file):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only the current file's `cursor/emails-*` branch; no force-push.
> - ONE PR per file into `acceptatie` (01 standalone, 02+ stacked).
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - No plaintext passwords, API keys or reusable secrets in any mail, log, API response or preview. No tracking pixels, no open/click tracking, no link rewriting.

**What this stack builds.** Dennis approved the phase-1 review, the em-* mockups, all six defaults and a standalone security hotfix on 30-09 ("Alles is akkoord").
- **01 is a standalone security hotfix** that can run first and on its own:
  - (a) **Parental consent** is confirmed only by a **POST** from a website page. GET shows the page only, so link scanners can't confirm it. The link is built from the configured public site URL (not `Request.Host`), and the mail names the child's first name.
  - (b) **No temporary passwords or API keys in any mail.** Invites get a single-use, expiring **set-password link**. API credentials get a single-use **reveal-once link**.
  - (c) The **support-access mail** is HTML-escaped and shows its expiry in Europe/Amsterdam time.
- **One new layout for every mail** (em-d01). It is Outlook-proof (ghost table, VML pill button) and has dark mode, a preheader with filler, the right meta tags and `lang`/`dir`. The logo mark is in every mail; the small mascot appears only in good-news mails. The footer has the reason per mail type, Hulp · Privacy (· Mail-instellingen for optional mails) and the company name, address and KvK number from config.
- **A real plain-text part and proper headers:**
  - `From: Lobsy <hallo@mail.lobsy.nl>` and `Reply-To: support@lobsy.nl`, both via config.
  - `List-Unsubscribe` + One-Click **only** on the 3 optional notifications, with mail settings behind them.
- **5 languages** (nl, en, pl, ro, ar). Each mail goes out in the recipient's language, with nl as fallback; ar is right-to-left. pl and ar get a native review before go-live.
- **All 31 mails rewritten in B1 Dutch with one button each.** That is the 28 catalog mails plus the 3 hard-coded ones (AccountLockout, ParentalConsent, SupportAccessRequested), which move into the catalog. Every bug from the review is fixed (§B).
- **Admin:**
  - an HTML preview per mail and language, using fake data only
  - limited "send all"
  - guard tests that catch bare HTML mails
  - snapshot tests that don't pin inline CSS

## Order

| # | File | Branch | Branches from | PR into |
|---|---|---|---|---|
| 01 | `01-hotfix-beveiliging.md`: **standalone hotfix** (not stacked). Parental consent via website POST (GET = page only, public base URL, child's first name). Single-use expiring links replace temporary passwords (set-password) and mailed API keys (reveal once). Support-access mail escaped + Europe/Amsterdam expiry. Tests that required the password in the mail updated | `cursor/emails-hotfix` | `origin/acceptatie` | `acceptatie` |
| 02 | `02-layout-fundament.md`: `EmailDocument` block model + one renderer (HTML **and** text), new layout (ghost table, VML button, dark mode, preheader filler, meta, `lang`/`dir`), `MailOptions` brand/legal config, hosted PNG assets (no CID), one `EmailLinks` helper with the dependency cases, the template registry (31 keys, kind, reason, mascot), central `ITransactionalMailer`, bare-HTML guard, structural snapshot tests | `cursor/emails-2` | `cursor/emails-hotfix` (or `origin/acceptatie` when PR 01 is merged) | `acceptatie` |
| 03 | `03-verzending-headers-afmelden.md`: text part on Resend + SMTP, From/Reply-To via config, List-Unsubscribe + One-Click (RFC 8058) for optional mails only, `EmailOptOut` + `/mail/afmelden` + `/account/mail-instellingen`, idempotency key, tags, no tracking, deliverability doc for Dennis | `cursor/emails-3` | `cursor/emails-2` | `acceptatie` |
| 04 | `04-talen-rtl.md`: `EmailStrings` (nl/en/pl/ro/ar), `IEmailLanguageResolver` (recipient language → nl), RTL + bidi isolation, culture-aware dates (Europe/Amsterdam, Gregorian), numbers and money, parity tests, review list (pl/ar native review) | `cursor/emails-4` | `cursor/emails-3` | `acceptatie` |
| 05 | `05-kandidaat-mails.md`: the 10 candidate mails rewritten B1 with one button (incl. ParentalConsent into the catalog), mascot in good news, bugs "Klik hier", stub note, hard-coded 10 minuten | `cursor/emails-5` | `cursor/emails-4` | `acceptatie` |
| 06 | `06-werkgever-mails.md`: the 9 employer mails rewritten (incl. invite + API-key link mails), bugs double escape, hard-coded 14 dagen, UTC dates, old routes | `cursor/emails-6` | `cursor/emails-5` | `acceptatie` |
| 07 | `07-registratie-account-intern.md`: the 12 registration, takeover, sales/ambassadeur, account and internal mails (incl. AccountLockout + SupportAccessRequested into the catalog), jargon out ("Microsoft Entra", "SBI"), lockout copy that doesn't promise a missing reset | `cursor/emails-7` | `cursor/emails-6` | `acceptatie` |
| 08 | `08-admin-voorbeeld.md`: admin HTML/text preview per mail × language × light/dark × 600/375 with fake data only (no DB vacancy), test send limited, "send all" limited and paced | `cursor/emails-8` | `cursor/emails-7` | `acceptatie` |
| 09 | `09-e2e-render-rapport.md`: render matrix tests (every key × 5 languages), Playwright render checks 600/375 light/dark, E2E flows (invite → password, consent, key reveal, one-click unsubscribe), docs, stack-end report | `cursor/emails-9` | `cursor/emails-8` | `acceptatie` |

If a file is too big for one reviewable PR (> ~1.500 changed lines excluding tests/migrations/resources/snapshots), split it into `a`/`b` at the seam the file names. The next file then branches from the **last** sub-branch (e.g. `cursor/emails-2b`).

## Pointer prompt (the only prompt needed; it runs 01 … 09)
```
Run the Lobsy e-mails stack. First: git fetch origin && git show origin/docs/emails:docs/prompts/emails/00-README.md — read it completely.
Then read and execute each file in docs/prompts/emails/ on that branch strictly in the order the README's table lists (01 … 09; a/b splits where a file allows it), one file = one PR.
File 01 is a standalone security hotfix: it branches from origin/acceptatie, is not stacked, and its PR body starts with "Standalone hotfix (not stacked)". If PR 01 already exists (the hotfix was run on its own), don't redo it: reuse its branch.
File 02 branches from cursor/emails-hotfix (or from origin/acceptatie if PR 01 is already merged); every later file branches from the previous file's branch (stacked). Each opens ONE PR into acceptatie whose body starts with "Stacked on #<prev PR>".
Before 02, run the dependency checks in the README's "Dependencies" section and follow the fallback it prescribes for each one; say in PR 02 which case applied. Re-run the checks the README names at 05, 06, 07 and 08.
Build and test after each file; if tests fail or a success criterion can't be met, push, open that PR as draft, stop and report — don't start the next file.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. No passwords, API keys, tracking pixels or link rewriting in any mail.
At the end report: file → branch → PR number → status, plus the dependency cases, what Dennis still has to provide (legal address, KvK, DNS/Resend, support@ inbox) and anything deferred.
```

### Pointer prompt: hotfix 01 only
```
Run only the security hotfix from the Lobsy e-mails stack. First: git fetch origin && git show origin/docs/emails:docs/prompts/emails/00-README.md (read "How to run" and §0) and git show origin/docs/emails:docs/prompts/emails/01-hotfix-beveiliging.md — read it completely.
Execute only file 01: branch cursor/emails-hotfix from origin/acceptatie (not stacked on anything), ONE PR into acceptatie whose body starts with "Standalone hotfix (not stacked)".
Fix (a) parental consent: confirm only via POST from the website page /toestemming (GET never mutates; the old API GET link redirects there), link from the configured PublicWebBaseUrl, the mail names the child's first name; (b) no plaintext temporary passwords or API keys in any mail or API response: single-use expiring set-password links (/account/wachtwoord-instellen) and a reveal-once API-key link (/koppeling/sleutel), re-invites never overwrite an existing password, and update the tests that required the password in the mail; (c) HTML-escape the support-access mail and show the expiry in Europe/Amsterdam.
Build and test; if red or a success criterion can't be met, push, open the PR as draft, stop and report.
Never merge, never deploy, never use rule 123, never push to main or acceptatie, no force-pushes. Don't start file 02.
Report: branch → PR number → status, the new routes/endpoints, the migration name, the test list, and anything deferred.
```

## How to run
1. `git fetch origin`. Read this file, `.cursor/rules/design-system.mdc`, `docs/ROUTES.md`, `docs/i18n/README.md`, `docs/release-flow.md` and `SECURITY.md`.
2. **File 01** stands alone. Run it as described in the file, whether or not the rest of the stack runs.
3. Before 02, run the **Dependencies** checks below and note the outcome (it goes into PR 02).
4. For each file in the order above:
   1. Read the whole file.
   2. Create its branch from the "Branches from" column:
      - File 01: `git checkout -b cursor/emails-hotfix origin/acceptatie`.
      - File 02: `git checkout -b cursor/emails-2 cursor/emails-hotfix` (or `origin/acceptatie` if PR 01 is merged).
      - Later files: `git checkout -b <branch> <previous branch>` with the previous branch pushed.
   3. Implement **only** that file's scope, plus the shared rules below.
   4. Run `dotnet build` and `dotnet test`, plus the Playwright suites the file names if you can. Everything must be green and the file's **success criteria** must hold.
   5. Small, clear commits. Push (`git push -u origin <branch>`, only `cursor/*` branches). Open **ONE PR into `acceptatie`** with the title from the file. The body starts with `Standalone hotfix (not stacked)` (01) or `Stacked on #<prev PR> (<prev branch>)` (02+), then the PR body items from §0.
   6. Note the PR number, go on to the next file.
5. **Stop and report** when tests fail and you can't fix them inside the file's scope, when a success criterion can't be met, or when the code contradicts this spec in a way you can't resolve safely. Push what you have, open that PR as **draft** with the failure described, and don't continue.
6. At the end, report the table file → branch → PR number → status (green or draft/red), plus anything deferred.
7. **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
   - Migrations: 01 adds `AddOneTimeLinks`, 03 adds `AddEmailOptOuts`. No other file adds a migration. Never regenerate or edit a lower file's migration.
   - If `acceptatie` moves during the run: don't rebase. Only when a conflict blocks you (or a dependency check flips at a re-check point), `git merge origin/acceptatie` into the current branch (a normal merge commit) and say so in the PR body.

---

## §0. Shared rules (every file)
- **Branches:** 01 standalone, 02+ stacked (see above). **ONE PR per file, always into `acceptatie`.** The diff includes lower PRs until they merge; say which commits are this file's own.
- **Never** merge, deploy, or use rule `123`. **Never** push to `main` or `acceptatie`. No force-pushes.
- **Stop on red.** Run `dotnet build` + `dotnet test` after each file. If red and not fixable in scope: stop, push, open a draft PR with the failure, report.
- Code references are from `origin/acceptatie` @ `a611db40` (2026-09-29 18:29 CEST). Re-check line numbers before editing.
- **Mockups:** branch `docs/emails`, folder `docs/mockups/emails/`. Read with `git fetch origin docs/emails && git show origin/docs/emails:docs/mockups/emails/<file> > /tmp/<file>` and open `/tmp/<file>`. Don't commit mockups to code branches.
  - Desktop (600 px mail column, 2x): `em-d00-huidig-sollicitatie.png` (today, for comparison), `em-d01-basis-layout.png` (with numbered notes), `em-d02-verificatiecode.png`, `em-d03-sollicitatie-verstuurd.png`, `em-d04-nieuwe-sollicitatie.png`, `em-d05-reactie-geaccepteerd.png`, `em-d06-uitnodiging.png`, `em-d07-overnameverzoek.png`, `em-d08-donker.png`, `em-d09-arabisch-rtl.png`.
  - Mobile (375 px, 2x): `em-m00` … `em-m09`, same names.
  - Mail HTML without the inbox frame: `html/em-*.mail.html`. The layout pieces are in `em_layout.py` (tokens pre-mixed to hex, VML button, ghost table, preheader filler, mobile + dark CSS). Use them for exact spacing, sizes and colours, **not** as code to paste: the implementation is the renderer in 02.
  - "Voorbeelddata": Alex, Sanne van Dijk, Joris Bakker, Bakkerij De Gouden Korrel, Markt 12 Delft, the code 123456, 86 %, 2,4 km, all dates, "Voorbeeldstraat 1, 2611 AA Delft" and "KvK 00000000". The inbox strip and the "Voorbeelddata" pill are mockup-only.
  - **Where a mockup and this spec differ, this spec wins.** Known differences:
    - **CTA URLs** in the mockups (`/sollicitaties`, `/werkgever/kandidaten`, `/uitnodiging/accepteren`) are illustrative. The real targets come from `EmailLinks` (02) and §M.
    - **"hulp@lobsy.nl"** in em-07 → the configured support address (`support@lobsy.nl`).
    - **em-04 "Match 86 %"**: show the match % the employer already sees for that application in the sollicitaties list; no score → omit the row. Never show the candidate's name, photo or contact details in the mail.
    - **em-06** says "Geldig tot 7 oktober 2026": that's the computed link expiry (7 days, 01).
    - **Footer legal line** is `MailOptions` data (02). Until Dennis provides the address and KvK, the footer shows only the legal name (never the placeholder text).
    - **em-09 Arabic copy** is a draft. The final ar strings come from 04 plus native review.
- **Design (mail HTML, not the app design system):** the renderer (02) is the only place with markup and inline styles. Tokens as hex, from `Jobsy.Web` tokens + the warm landing tints: bg `#f5f2ee`, card `#fffcfa`, text `#122033`, muted `#5a6a7d`, border `#ddd5cc`, brand/CTA `#0f2d5c`, tints peach `#fee7df`, sun `#f1e5c3`, sky `#e7eef7`, mint `#ecfdf3`, badge coral `#f54a1b`. Dark: bg `#0d1726`, card `#15233a`, text `#eef2f7`, muted `#a9b6c6`, border `#2a3a52`, CTA `#fffcfa` with text `#0f2d5c`. Body 16 px / 1.55, h1 26 px (24 px mobile), footer 13 px (never below 13), CTA 50 px high pill, full width under 620 px. System font stack with Inter first (no web-font import).
- **Copy:** B1 Dutch, short sentences, "je", no em-dashes, no jargon ("Microsoft Entra" → "Microsoft", no "SBI", no "tokens" in candidate mails, no "stub"). Greeting "Hoi {voornaam}," for candidates, "Hallo {voornaam}," for everyone else; no name → "Hallo,". Sign-off "Team Lobsy". The subject says what happened, has no "— Lobsy" suffix and is ≤ 60 characters where the data allows. The preheader is its own sentence and never repeats the subject.
- **One button per mail.** Code mails (the 4 OTP mails) have **no** button: the code is the action. A mail may also have one plain-text link in its note, only for a different, safe secondary action (e.g. "Zet je status op Niet beschikbaar").
- **Mascot** only in good-news mails: EmployerReactionAccepted, EmployerContacting, ApplicationHired, RegistrationCredentials, TakeoverApproved, and werkgever-aanmelding's `CompanyVerified` if present. The logo mark is in every mail.
- **Strings:** from 04 on, all mail text via `EmailStrings` (`Jobsy.Core/Email/Localization/`), keys `Email.{TemplateKey}.*` and `Email.Common.*`, in **nl, en, pl, ro, ar** from the file that adds them. nl and en are final. pl/ro/ar are B1 drafts listed in `docs/i18n/emails-review.md`; **pl and ar need a native review before go-live** (Dennis, 30-09). Resource values never contain HTML. `docs/i18n/untranslated-baseline.txt` may not grow (the app UI strings added for new pages go through `UiStrings` as usual).
- **Security and privacy (every file):**
  - Every tokenized link (set-password, key reveal, consent, unsubscribe, set-unavailable, withdraw-others) is single-purpose. Its token is 256-bit random or data-protection signed, stored hashed where it's a DB token, and never logged.
  - **GET never mutates.** Every action behind a mail link is a POST from a page.
  - Rate limits on every anonymous endpoint. PlatformLog rows redact recipients (`EmailServiceStub.RedactEmail`).
  - Mail HTML is built only by the renderer, which HTML-escapes every value.
- **Docs and guards to update when routes change:** `docs/ROUTES.md` (`RoutesDocFreshnessTests`), `Seo/PageSeoCatalog.cs` (`PageSeoTests`, new pages are noindex), `Help/PageHelpDocs.cs` (`PageHelpDocsTests`), `BlazorPageRoleAttributesTests`, `CHANGELOG.md`.
- **Must NOT touch:**
  - `LocalAuthCredential` password rules, lockout rules (`LoginLockoutRules`), MFA pages and `MfaEnforcementMiddleware` (only reuse them)
  - `VerificationCodes` hashing/pepper and OTP lifetimes (only read the constants)
  - Resend/SMTP credential storage in Integraties (`IIntegrationCredentialService`)
  - the web-push notification texts (only mail)
  - the other stacks' in-progress branches (`cursor/landing-*`, `cursor/werkgever-redesign-*`, `cursor/werkgever-aanmelding-*`, `cursor/salesmanager-*`, `cursor/kandidaat-banen-*`, `cursor/admin-redesign-*`, `cursor/intermediair-*`): never branch from or merge them
- **PR description:**
  - what changed and why
  - for mail changes: the PNG render of each changed mail (nl, desktop 600 + mobile 375; plus ar and dark for 02 and 04)
  - the new/changed URL + endpoint list
  - test list
  - "Out of scope / deferred"
- **Playwright in CI:** every new Playwright test class goes into **both** filter lists in `.github/workflows/pr-tests.yml` (excluded from the unit step, included in the smoke step), like the existing classes.

---

## §M. Mail inventory (the contract; 31 keys)
Kind: **E** = essential (always sent, no unsubscribe), **O** = optional notification (opt-out, List-Unsubscribe One-Click, "Mail-instellingen" in the footer), **S** = security/code (essential, no button). Button targets use `EmailLinks` (02); "(dep B)" = `/werkgever/...` when werkgever-redesign 01 landed, else today's URL.

| # | Key | To | Kind | Mascot | Button (nl) → target | Reason key | File |
|---|---|---|---|---|---|---|---|
| 1 | ApplicationConfirmation | candidate | E | – | Bekijk je sollicitatie → `/candidate/applications` | `Applied` | 05 |
| 2 | ApplicationVerificationCode | candidate/applicant | S | – | none (code) | `ApplyCode` | 05 |
| 3 | EmployerReactionAccepted | candidate | E | ✓ | Bekijk je sollicitatie → `/candidate/applications` | `Applied` | 05 |
| 4 | EmployerReactionRejected | candidate | E | – | Bekijk andere vacatures → map (dep A/C) | `Applied` | 05 |
| 5 | EmployerContacting | candidate | E | ✓ | Bekijk je sollicitatie → `/candidate/applications` | `Applied` | 05 |
| 6 | ApplicationHired | candidate | E | ✓ | Andere sollicitaties intrekken → withdraw-others link (when a token URL exists), else Bekijk je sollicitatie | `Applied` | 05 |
| 7 | ApplicationFilledElsewhere | candidate | E | – | Bekijk andere vacatures → map | `Applied` | 05 |
| 8 | PushBom | candidate | **O** | – | Bekijk de vacature → `/vacancies/{id}`; note link "Zet je status op Niet beschikbaar" | `NearbyJobs` | 05 |
| 9 | AccountUnsubscribeVerification | user | S | – | none (code) | `AccountRequest` | 05 |
| 10 | ParentalConsent (new in catalog) | parent/guardian | E | – | Toestemming bekijken → `/toestemming?t=` (01) | `ParentAsked` | 01, 05 |
| 11 | EmployerNewApplication | employer | E | – | Bekijk de sollicitatie → sollicitaties (dep B) | `ManagesVacancies` | 06 |
| 12 | CandidateWithdrawn | employer | E | – | Bekijk je sollicitaties → sollicitaties (dep B) | `ManagesVacancies` | 06 |
| 13 | CandidateWithdrawnOtherJob | employer | E | – | Bekijk je sollicitaties → sollicitaties (dep B) | `ManagesVacancies` | 06 |
| 14 | PendingApproval | bedrijfsmanager | E | – | Aanvraag bekijken → tokens (dep B) | `ManagesCompany` | 06 |
| 15 | VacancyEngagementReminder | employer | **O** | – | Vacature verbeteren → vacancy edit (dep B) | `ManagesVacancies` | 06 |
| 16 | DraftVacancyCleanupWarning | employer | E | – | Concept bekijken → vacancy edit (dep B) | `ManagesVacancies` | 06 |
| 17 | CompanyReEngagement | employer | **O** | – | Naar je dashboard → employer home (dep B) | `ManagesCompany` | 06 |
| 18 | CompanyApiKeyCredentials | technical contact | E | – | API-sleutel ophalen → `/koppeling/sleutel?t=` (01) | `ApiKeyRequested` | 01, 06 |
| 19 | UserInvite | invitee | E | – | Uitnodiging accepteren → `/account/wachtwoord-instellen?t=` (01) | `Invited` | 01, 06 |
| 20 | RegistrationActivation | registrant | S | – | none (code) | `Registering` | 07 |
| 21 | RegistrationCredentials | new bedrijfsmanager | E | ✓ | Naar je dashboard, or Kies je wachtwoord (no password chosen, 01) | `Registered` | 01, 07 |
| 22 | TakeoverEmailVerification | requester | S | – | none (code) | `TakeoverRequested` | 07 |
| 23 | TakeoverRequest | current manager | E | – | Bekijk het verzoek → takeovers inbox (dep B/E) | `YouManage` | 07 |
| 24 | TakeoverSubmitted | requester | E | – | Verzoek intrekken → signed withdraw link (dep E), else Hoe werkt Lobsy? → `/hoe-werkt-lobsy` | `TakeoverRequested` | 07 |
| 25 | TakeoverApproved | requester | E | ✓ | Kies je wachtwoord (new user, 01) or Inloggen → `/login` | `TakeoverRequested` | 01, 07 |
| 26 | TakeoverRejected | requester | E | – | Neem contact op → `mailto:` support | `TakeoverRequested` | 07 |
| 27 | SalesManagerInvite | new salesmanager | E | – | Uitnodiging accepteren → set-password (01), then onboarding (dep D) | `Invited` | 01, 07 |
| 28 | AmbassadeurInvite | new ambassadeur | E | – | Uitnodiging accepteren → set-password (01), then onboarding; **suppressed** while `AmbassadorsEnabled` is off (dep D) | `Invited` | 01, 07 |
| 29 | AccountLockout (new in catalog) | user | E | – | Neem contact op → `mailto:` support | `Security` | 07 |
| 30 | SupportAccessRequested (new in catalog) | other admins | E | – | Bekijk de toegang → `/admin/personal-data-access-log` | `AdminNotice` | 01, 07 |
| 31 | MailTest | admin | E | – | Naar e-mails → admin e-mails page (dep G) | `AdminNotice` | 07 |

Mails added by other stacks (werkgever-aanmelding 03/06/07, landing 03, admin-redesign 03) join this registry with the same rules when they exist on `acceptatie` at the file that touches their audience (Dependencies E, A, G). Tests assert **at least** these 31 keys, not an exact count.

## §IA. Routes and endpoints (the contract for all files)

| Route / endpoint | What | Who | Render | SEO | Built in |
|---|---|---|---|---|---|
| `/toestemming?t=` | Parental consent page: child's first name, what the consent covers, expiry, button "Ik geef toestemming" (POST) | anonymous (token) | static SSR form + antiforgery | noindex | 01 |
| `GET api/parental-consent/preview?token=` | valid?, child's first name, expiry. Never mutates | anonymous, rate-limited `otp-verify` | — | — | 01 |
| `POST api/parental-consent/confirm` `{ token }` | the only path that sets `ParentalConsentAt` | anonymous (called by Web), rate-limited | — | — | 01 |
| `GET api/parental-consent/confirm?token=` (legacy links) | 302 → `{PublicWebBaseUrl}/toestemming?t=…`, no mutation | anonymous | — | — | 01 |
| `/account/wachtwoord-instellen?t=` | Set your password (invite / no password chosen); also "Log in met Google of Microsoft" | anonymous (token) | static SSR form | noindex | 01 |
| `GET api/account/setup-link?token=` / `POST api/account/setup-password` | preview (masked e-mail, expiry) / set the password, single use | anonymous, rate-limited `auth` | — | — | 01 |
| `/koppeling/sleutel?t=` | API key reveal page: company name, "Toon de sleutel" (POST), then the key once, copy button, `Cache-Control: no-store` | anonymous (token) | static SSR form | noindex | 01 |
| `POST api/company-api-keys/reveal` `{ token }` | creates + activates the key, deactivates older keys, returns the plaintext **once** | anonymous (token), rate-limited `auth` | — | — | 01 |
| `/mail/afmelden?t=` (GET page + POST) | One-click unsubscribe target (RFC 8058 POST from mailbox providers) and a page with a confirm button and "Toch weer aanzetten" | anonymous (signed token) | minimal API endpoint + static SSR page | noindex | 03 |
| `/account/mail-instellingen` | toggles for the optional mails the user can receive; list of "always sent" mails | authenticated | static SSR form | noindex | 03 |
| `GET/PUT api/me/email-preferences` | read/update opt-outs | authenticated | — | — | 03 |
| `/admin/mail-test` (or `/admin/content/emails` when admin-redesign 01 landed, dep G) | preview + test send | Admin | InteractiveServer | private | 08 |
| `GET api/settings/email-templates/{key}/preview?lang=&theme=light\|dark` | `{ subject, preheader, html, text, kind, language }` from fake data | Admin | — | — | 08 |

## §B. Review bugs → where fixed (each gets a regression test)

| Bug (origin/acceptatie @ a611db40) | Fixed in |
|---|---|
| Parental consent `GET api/parental-consent/confirm` mutates (`ParentalConsentController` L20–45); link scanners confirm consent for a minor | 01 |
| Consent link built from `Request.Scheme/Host` → API host (`MeController` L951); mail doesn't name the child | 01 |
| Temporary passwords mailed (UserInvite, SalesManagerInvite, AmbassadeurInvite, RegistrationCredentials/TakeoverApproved legacy path) and echoed in Development responses/UI (`Branches.razor` L330, `Regions.razor` L254, `SalesManagersAdmin.razor` L224/L270, `AmbassadeursAdmin.razor` L223) | 01 |
| Re-inviting an existing user overwrites their password (`CompanyUsersController` L360–362) | 01 |
| Plaintext API key in the mail (`TransactionalEmails.CompanyApiKeyCredentials` L759, `CompanyApiKeyService.EmailCredentialsAsync` L175) | 01 |
| Support-access mail unescaped (`grant.Reason`, admin e-mail), no layout, UTC `:u` expiry (`SupportAccessService` ~L236) | 01 (escape + time), 07 (catalog) |
| `baseUrl: null` at 8 call sites → links always `https://lobsy.nl`, also on acceptatie (`AmbassadeurInviteService` L141, `SalesManagerInviteService` L174, `CompanyApiKeyService` L213, `CompanyRegistrationService` L885/L1271/L1333, `PrivacyDataService` L719, `IntegrationHealthStub` L86) | 01 for the mails it touches, 02 for the rest (`EmailLinks` requires a base URL) |
| `<html lang="nl">` hard-coded, no `dir` (`EmailLayout` L128) | 02 (+04 languages) |
| No `color-scheme` meta / dark CSS, no `<style>`, no media queries; `max-width` ignored by Outlook (no ghost table); `border-radius`/`overflow` on tables; buttons not bulletproof (no VML); `font-weight:650` | 02 |
| Preheader without filler + no `mso-hide`; most preheaders repeat the subject | 02 (renderer) + 05–07 (copy) |
| 11 px Dutch-only footer, no address/KvK, no privacy/help/preferences link; "Hyper-lokaal matchen" tagline | 02 |
| No `x-apple-disable-message-reformatting` / `format-detection` meta | 02 |
| Logo via hosted URL on Resend but CID on SMTP (two renderings) | 02 (hosted PNG everywhere, `EmailLogoEmbedder` removed) |
| 3 mails are bare HTML outside the catalog/layout (AccountLockout `AuthController` L101, ParentalConsent `MeController` L952, support access); the guard only greps `EmailLayout.Wrap(` | 02 (guard), 05/07 (content) |
| Tests pin inline CSS (`display:inline-block;padding:12px 22px`) | 02 |
| No plain-text part (Resend `CreateResendRequest` L375, SMTP `BodyBuilder` L165) | 03 |
| No `List-Unsubscribe`/`-Post`, no Reply-To, `noreply` From example in `MailOptions`, no tags/idempotency | 03 |
| No localization at all; `FormatEuro`/`FormatKm` fixed to nl-NL; UTC dates (`DraftVacancyCleanupWarning`) | 04 (+06) |
| PushBom button "Klik hier" (L388); "Top!", "Wellicht", "Wat een feest!"; "Authenticator stub" note to users (L166) | 05 |
| "10 minuten" hard-coded (L185/L528/L580) while the lifetime lives in `ApplicationsController` L886 | 05 (+07) |
| Double escape of the role label (L691) | 06 |
| "14 dagen" hard-coded next to the constants (L432, L467, L478, L485) | 06 |
| 2–3 buttons per mail (VacancyEngagementReminder, Sales/AmbassadeurInvite, ApplicationHired) | 05–07 |
| Old routes (`/branch/*`, `/employer/*`, `/salesmanager/onboarding`, map `/`) | 02 (`EmailLinks`) + 05–07 |
| "Microsoft Entra", "SBI", "(onvoldoende tokens)" jargon | 06/07 |
| AccountLockout promises "kies een nieuw wachtwoord" but there is no reset flow | 07 (copy), deferred flow (D-extra 9) |
| Admin "send all" sends all templates in a row to any address; preview uses a live DB vacancy (`EmailCatalogService.BuildContextAsync`) | 08 |

## Decisions (Dennis "Alles is akkoord" 30-09 on the six defaults + the hotfix; extra defaults marked *extra*)
- **D1. Sender:** `From: Lobsy <hallo@mail.lobsy.nl>` (a Resend-verified subdomain, not no-reply), `Reply-To: support@lobsy.nl`, one sender name for every audience, both via config (`MailOptions`). *(Dennis, 30-09)*
- **D2. Mascot** only in good-news mails (§0 list); the 36 px logo mark in every mail. *(Dennis, 30-09)*
- **D3. Dark mode:** yes. Own dark palette (§0), `color-scheme`/`supported-color-schemes`, `[data-ogsc]`/`[data-ogsb]` overrides for Outlook, a light CTA on dark. *(Dennis, 30-09)*
- **D4. Languages:** nl, en, pl, ro, ar from one resource catalog. The recipient's language, else nl; `lang`/`dir` set; ar right-to-left. pl and ar get a native review before go-live. *(Dennis, 30-09)*
- **D5. Footer:** the reason per mail type; Hulp · Privacy (· Mail-instellingen for optional mails); legal name, address and KvK from config. Unsubscribe + `List-Unsubscribe` One-Click only for the 3 optional notifications. *(Dennis, 30-09)*
- **D6. No tracking pixels, no open/click tracking, no link rewriting.** Single-use expiring links replace temporary passwords and the mailed API key. Parental consent only via a POST from the website. *(Dennis, 30-09)*
- **D7. Link lifetimes:** set-password 7 days (invites, TakeoverApproved/RegistrationCredentials without a chosen password), API-key reveal 72 hours, parental consent 7 days (unchanged). A new link for the same purpose and user invalidates the older unused ones. *extra*
- **D8. Hulp** = `mailto:{SupportAddress}` (default `support@lobsy.nl`). Privacy = `/privacy`. Mail-instellingen = `/account/mail-instellingen`. *extra*
- **D9. Deferred:** a "wachtwoord vergeten" flow (password reset by mail) is **not** in this stack. 07 makes the lockout mail honest; the reset flow reuses 01's `OneTimeLinks` later. *extra*
- **D10. Existing temporary passwords** already mailed stay valid (no forced change). New invites from 01 on never create a password. *extra*
- **D11. Parent language:** the child's language, else nl. Company contacts without a user account: the language of the request that triggered the mail, else nl. Employer mails triggered by a candidate use the **employer's** language. *extra*
- **D12. Optional categories:** exactly PushBom, VacancyEngagementReminder, CompanyReEngagement. Opt-outs are stored per normalized e-mail address (hashed), so they also work for contacts without an account. *extra*
- **D13. Images:** hosted PNG only (logo mark 72×72 shown at 36, mascot 128×128 shown at 64) under `/images/email/` with a `?v=` version; `alt="Lobsy"` for the logo, `alt=""` for the decorative mascot. No CID, no WebP/SVG in mails. *extra*
- **D14. Legal footer placeholders:** `MailOptions.LegalName` default "Lobsy", `LegalAddress` and `KvkNumber` empty. Empty parts are omitted (never placeholder text in a real mail). **Dennis still has to provide the registered name, address and KvK number** (flagged in 02 and the stack-end report). *extra*

## Dependencies (check before 02; say in PR 02 which case applied)
- **A. Landing (`docs/landing` 02, 03, 04).** Check: `git grep -n "PublicRoutes.Banenkaart\|class LobsyMascot\|AccountCreate\|account-maken" origin/acceptatie -- Jobsy.Web Jobsy.Core`.
  - **Present:** `EmailLinks.Map` = `/banenkaart`. The mascot PNG comes from landing 02's `Celebrating` pose export if a PNG exists, else the current `mascot-128.png`. Landing 03's candidate sign-up/sign-in code mails join the registry as kind S (05).
  - **Absent:** `EmailLinks.Map` = `/` (today's map). Add a line to `docs/emails-followups.md`: "landing 04: switch `EmailLinks.Map` to `PublicRoutes.Banenkaart`; landing 03: register its code mails in the email registry." Never create landing's names.
  - Re-check at 05.
- **B. Werkgever shell (`docs/werkgever-redesign` 01).** Check: `git grep -n "class WerkgeverNav\|class EmployerLinks\|WerkgeverLegacyHrefTests" origin/acceptatie -- Jobsy.Web Jobsy.Core Jobsy.Tests`.
  - **Present:** `EmailLinks` employer targets use the new URLs (`/werkgever/sollicitaties`, `/werkgever/vacatures/nieuw?edit={id}`, `/werkgever/tokens`, `/werkgever/overnames`, `/werkgever/koppelingen?tab=api`, `/werkgever/organisatie/team`, `/werkgever`), taken from `EmployerLinks`/`WerkgeverNav` if they expose them. `WerkgeverLegacyHrefTests` must stay green.
  - **Absent:** today's URLs (`/branch/applicants`, `/branch/vacancies/new?edit=`, `/employer/tokens`, `/employer/takeovers`, `/employer/company`, `/employer/users`, `/home`) are kept in one place, `EmailLinks.Employer*`. The redesign's 301s will keep them working, and its legacy-href guard then only has to fix `EmailLinks`. Add a line to `docs/emails-followups.md`.
  - Re-check at 06 and 07.
- **C. Kandidaat banen (`docs/kandidaat-banen`).** Check: `git grep -n "class KbRoutes\|KbRoutes.Map" origin/acceptatie -- Jobsy.Web`. Candidate URLs don't change there (`/candidate/applications`, `/candidate/liked`, `/vacancies/{id}`). **Present:** `EmailLinks.Map` uses `KbRoutes.Map`'s value when it is a constant. **Absent:** A decides. Re-check at 05.
- **D. Salesmanager (`docs/salesmanager` 01).** Check: `git grep -n "SalesLegacyRoutes\|AmbassadorsEnabled" origin/acceptatie -- Jobsy.Web Jobsy.Core Jobsy.Infrastructure`.
  - **Present:** the post-password redirect for SalesManager is `/sales/start`. AmbassadeurInvite is **suppressed** by the mailer while `AmbassadorsEnabled` is off (log "suppressed: ambassadors disabled"); the preview still shows it with a "Geparkeerd" label.
  - **Absent:** `/salesmanager/onboarding` and `/ambassadeur/onboarding`; AmbassadeurInvite is sent as today.
  - Re-check at 07.
- **E. Werkgever-aanmelding (`docs/werkgever-aanmelding` 03, 06, 07).** Check: `git grep -n "CompanyVerificationReminder\|CompanyVerified\|CompanyAccessRequest\b\|/register/toegang" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure Jobsy.Web`.
  - **Present:** its mails (verification reminder/verified/deleted, business-e-mail code, reject-reason, access request + reminder + decision, ownership-transfer notice) join the registry: kind E (codes S), the new layout, their own copy checked against §0 B1 rules, and the 5 languages if not already there. TakeoverRequest/TakeoverSubmitted follow its "Toegangsverzoeken" inbox and signed withdraw link. `CompanyVerified` gets the mascot. 07's invite path (access approved) uses 01's set-password link.
  - **Absent:** only the 31 keys; takeover mails target today's takeovers inbox.
  - Re-check at 06 and 07.
- **F. Werkgevers actief switch (`docs/mijn-paspoort` 01).** Check: `git grep -n "RequiresEmployers\|IEmployersSwitch" origin/acceptatie -- Jobsy.Core Jobsy.Infrastructure`.
  - **Present:** keep `RequiresEmployers` on the registry metadata (02 moves it, doesn't drop it). The central mailer (02) is where the suppression happens.
  - **Absent:** add `RequiresEmployers` to the registry with the list from paspoort 01 §g (all candidate/employer/registration mails; not MailTest, AccountUnsubscribeVerification, ParentalConsent, AccountLockout, SupportAccessRequested). No switch → nothing is suppressed; add the line to `docs/emails-followups.md`.
- **G. Admin redesign (`docs/admin-redesign` 01, 03, 07).** Check: `git grep -n "class AdminNav\b\|AdminNav.cs\|MfaResetByAdmin\|interface IAdminAuditLog" origin/acceptatie -- Jobsy.Web Jobsy.Core Jobsy.Infrastructure`.
  - **Present:** the page lives where admin-redesign 01 moved it, `/admin/content/emails` ("E-mails & meldingen"; `/admin/mail-test` 301s there), and `EmailLinks.AdminEmails` points there. `MfaResetByAdmin` joins the registry (kind E, reason `Security`). Test sends write `IAdminAuditLog` rows when 07 landed.
  - **Absent:** keep `/admin/mail-test` and today's nav item, with "E-mails" as its label, and write `PlatformLog` rows. Add a line to `docs/emails-followups.md`.
  - Re-check at 08.
- **H. Intermediair (`docs/intermediair` 03).** Check: `git grep -n "IntermediaryPublicIdentity" origin/acceptatie -- Jobsy.Core`.
  - **Present:** candidate mails (05) take the employer name from `IntermediaryPublicIdentity`. In hidden mode that is the bureau's name. Only EmployerContacting/ApplicationHired to that one candidate may name the opdrachtgever ("{bureau} nodigt je uit voor werk bij {opdrachtgever} in {plaats}"), as intermediair 03.7 says. The preview uses fake data.
  - **Absent:** today's `CompanyName`. Add a line to `docs/emails-followups.md`.
  - Re-check at 05.
- **I. Scholen (`docs/scholen` D14).** It reuses "the invite-token pattern → set password". That pattern **is** 01's `IOneTimeLinkService` + `/account/wachtwoord-instellen`; nothing to check here. Say so in PR 01 so the scholen stack reuses it.
- **Recommended landing order:** 01 as soon as possible (security), then landing 01–04 and werkgever-redesign 01 before 05–07 if they're close. Not a hard requirement: every case above has a fallback. Never branch from an unmerged branch of another stack.
