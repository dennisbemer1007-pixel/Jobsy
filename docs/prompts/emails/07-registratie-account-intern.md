# 07. Registration, takeover, sales/ambassadeur, account and internal mails (#20–31)

Read `00-README.md` first (§0 Copy, §M #20–31, §B, D9, Dependencies B/D/E/G). Branch `cursor/emails-7` from `cursor/emails-6`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-7`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Copy only changes through `EmailStrings` values (04), in all 5 languages in the same commit.
> - Don't touch `LoginLockoutRules`, `LocalAuthCredential` rules, MFA, or OTP lifetimes: only read them.
> - Never promise a flow that doesn't exist (no "kies een nieuw wachtwoord" link until the reset flow exists, D9).

| | |
|---|---|
| Branch | `cursor/emails-7` |
| PR title | `feat(email): registration, takeover, invite, account and admin mails in B1 with one button; lockout and support-access mails in the catalog; jargon out` |
| PR body starts with | `Stacked on #<PR 06> (cursor/emails-6)` + the outcome of the dep B/D/E/G re-check |
| Mockups | `em-d02` (code mails), `em-d06` (invite), `em-d07`/`em-m07` (TakeoverRequest) |
| Split seam | **07a** = registration + takeover (#20–26); **07b** = invites, account, internal (#27–31) |

## 07.0 Re-check dependencies first
Run B, D, E and G from the README; write the outcome in the PR body.

## 07.1 The copy (nl final; en final; pl/ro/ar drafts)
Greeting "Hallo {voornaam}," (else "Hallo,"). Code mails (#20, #22) have **no** button and put the code in the subject (05.5).

| # | Key | Subject | Preheader | Eyebrow · h1 | Body (short) | Button → target |
|---|---|---|---|---|---|---|
| 20 | RegistrationActivation (S) | `Je code voor Lobsy: {code}` | `Vul deze code in om je bedrijf aan te melden.` | Code · "Je code" | "Vul deze code in om **{vestiging}** aan te melden op Lobsy." Code block. "De code werkt {n} minuten." (`CompanyRegistrationService.ActivationTokenTtl`). The role and "SBI" go. | **none** (today's "Code invoeren" goes) |
| 21 | RegistrationCredentials | `Je Lobsy-account is klaar` | `Je kunt nu je eerste vacature plaatsen.` | Welkom · "Je account is klaar" + mascot | "**{vestiging}** staat op Lobsy. Je kunt nu je eerste vacature plaatsen." The "eerste token gratis" sentence stays **only** if the current registration flow still grants it (check `CompanyRegistrationService`; else drop it). Facts: Bedrijf, Je e-mailadres. "Je kunt ook inloggen met Google of Microsoft met dit e-mailadres." | Password chosen: **Naar je dashboard** → `EmailLinks.EmployerHome`. No password chosen (01): **Kies je wachtwoord** → set-password link |
| 22 | TakeoverEmailVerification (S) | `Je code voor je verzoek: {code}` | `Vul deze code in om je verzoek te versturen.` | Code · "Je code" | "**{vestiging}** staat al op Lobsy. Vul deze code in. Dan sturen we je verzoek naar de beheerder." Code block. "De code werkt {n} minuten." | **none** |
| 23 | TakeoverRequest | `{naam} wil toegang tot {vestiging}` | `Keur het verzoek goed of wijs het af.` | Verzoek · "Iemand wil toegang tot {vestiging}" | "**{naam}** ({e-mail}) wil {vestiging} ook beheren op Lobsy." Facts: Naam, E-mail, Vestiging, KvK-vestigingsnummer (when known), Aangevraagd op. "Ken je deze persoon niet? Wijs het verzoek dan af." | **Bekijk het verzoek** → `EmailLinks.EmployerTakeovers` (dep E present: its "Toegangsverzoeken" inbox) |
| 24 | TakeoverSubmitted | `Je verzoek voor {vestiging} is verstuurd` | `De beheerder beslist. Je krijgt een mail.` | Verzoek · "Je verzoek is verstuurd" | "**{vestiging}** heeft al een beheerder. We hebben je verzoek naar die persoon gestuurd. Je krijgt een mail zodra die beslist." | dep E present: **Verzoek intrekken** → its signed withdraw page. Absent: **Hoe werkt Lobsy?** → `EmailLinks.HowLobsyWorks` |
| 25 | TakeoverApproved | `Je hebt nu toegang tot {vestiging}` | `Log in en ga verder.` | Goedgekeurd · "Je hebt toegang" + mascot | "Je verzoek voor **{vestiging}** is goedgekeurd. Vacatures, tokens en geschiedenis blijven bij de vestiging." (org sentence when `hasOrganization`). | New user (01): **Kies je wachtwoord** → set-password link. Existing login: **Inloggen** → `/login` |
| 26 | TakeoverRejected | `Je verzoek voor {vestiging} is afgewezen` | `Denk je dat dit niet klopt? Neem contact op.` | Verzoek · "Je verzoek is afgewezen" | "De beheerder van **{vestiging}** heeft je verzoek afgewezen. Denk je dat dit niet klopt? Neem contact met ons op." | **Neem contact op** → `mailto:{SupportAddress}` (today's "Opnieuw registreren" goes) |
| 27 | SalesManagerInvite | `Je bent uitgenodigd als salesmanager bij Lobsy` | `Kies een wachtwoord en begin.` | Uitnodiging · "Welkom als salesmanager" | "Je bent uitgenodigd als salesmanager bij Lobsy." Steps: "1 Kies je wachtwoord. 2 Vul je bedrijfsgegevens in. 3 Onderteken de overeenkomst. Dan krijg je je eigen code." ("KvK/BTW/NAW" and "trackingcode" wording goes.) | **Uitnodiging accepteren** → set-password link; after the password the redirect is `/sales/start` (dep D present) or `/salesmanager/onboarding` (absent). The second button goes |
| 28 | AmbassadeurInvite | `Je bent uitgenodigd als ambassadeur bij Lobsy` | `Kies een wachtwoord en begin.` | Uitnodiging · "Welkom als ambassadeur" | Same as #27 with "ambassadeur". **Suppressed** by the mailer while `AmbassadorsEnabled` is off (dep D present): log "suppressed: ambassadors disabled", the invite service returns a clear error to the admin UI ("Ambassadeurs staan uit"). | **Uitnodiging accepteren** → set-password link, redirect `/ambassadeur/onboarding` |
| 29 | AccountLockout (new in catalog) | `Je Lobsy-account is tijdelijk geblokkeerd` | `Je kunt over {duur} weer inloggen.` | Beveiliging · "Je account is even geblokkeerd" | "Er is {pogingen} keer een verkeerd wachtwoord ingevuld. Daarom is je account {duur} geblokkeerd (`LoginLockoutRules.LockoutDuration(FailedLoginCount)`, formatted "15 minuten" / "1 uur" / "2 uur" / "4 uur"). Daarna kun je weer inloggen. Was jij dit niet? Neem dan contact met ons op." Facts: Geblokkeerd tot (time, Amsterdam). | **Neem contact op** → `mailto:{SupportAddress}` |
| 30 | SupportAccessRequested (new in catalog) | `Supporttoegang aangevraagd door {admin}` | `Toegang tot {doel} tot {tijd}.` | Beheer · "Er is supporttoegang aangevraagd" | 01's content in the new layout: who, for which user/company, reason, "Geldig tot {tijd} (Nederlandse tijd)". All values escaped by the renderer. | **Bekijk de toegang** → `EmailLinks.AdminPersonalDataAccessLog` (`/admin/personal-data-access-log`) |
| 31 | MailTest | `Testmail van Lobsy` | `Als je dit leest, werkt het versturen van mail.` | Test · "Dit is een testmail" | "Als je dit leest, werkt het versturen van mail vanaf {omgeving} ({provider}: Resend of SMTP). Verstuurd op {tijd}." | **Naar e-mails** → `EmailLinks.AdminEmails` (dep G: `/admin/content/emails`, else `/admin/mail-test`) |

- Reasons (footer), nl: `Registering` "Je krijgt deze mail omdat je een bedrijf aanmeldt op Lobsy." · `Registered` "Je krijgt deze mail omdat je een bedrijf hebt aangemeld op Lobsy." · `TakeoverRequested` "Je krijgt deze mail omdat je toegang hebt gevraagd tot {vestiging}." · `YouManage` "Je krijgt deze mail omdat je {vestiging} beheert op Lobsy." · `Invited` (06) · `Security` "Je krijgt deze mail om je account te beschermen." · `AdminNotice` "Je krijgt deze mail omdat je beheerder bent van Lobsy."
- **TakeoverRequest** shows the requester's name and e-mail on purpose: the manager needs them to decide. It is the only mail that names another person besides the recipient; add it to the allow-list of the PII test from 06.

## 07.2 Bugs and jargon (each gets a regression test)
- "Microsoft Entra" → "Microsoft" (L528, L548, L558, L644, UserInvite in 06). Guard: no value contains "Entra".
- "SBI" and the role label in #20 go (the registrant doesn't need them). Guard: no value contains "SBI".
- Hard-coded "10 minuten" (L528, L580) → `ActivationTokenTtl` via `EmailFormat` (05.1 regex guard).
- "— Lobsy" subject suffixes (#20–26) go (§0).
- The note "Inloggen via Lobsy (https://…)" that repeats the button URL goes (#21, #25, TakeoverRequest's inbox URL note).
- **AccountLockout** (`AuthController` ~L100): bare HTML `new EmailMessage(` → the mailer with the registry key, the recipient's language and the real duration. The copy no longer tells people to "kies een nieuw wachtwoord" (there is no reset flow, D9). The lockout must still never depend on mail delivery: keep the try/catch. Also check the other lockout path(s) (`git grep -n "LockoutUntil = " -- Jobsy.Api Jobsy.Infrastructure`) and send the same key there if they mail.
- **SupportAccessRequested**: the registry entry replaces 01's direct template call; content unchanged except for the new layout and languages (admin's language, D11 rule "User").
- Two buttons in #27/#28 → one. Code mails #20/#22 had a button → none.

## 07.3 Senders
- `CompanyRegistrationService`, `SalesManagerInviteService`, `SalesManagerApplicationService`, `AmbassadeurInviteService`, `AuthController`, `SupportAccessService`, `IntegrationHealthStub` (MailTest) all send through `ITransactionalMailer` (02 guard enforces it).
- Language: registrant/requester via `Requester(language)` (the web request header); managers and admins via `User(userId)` (04).
- MailTest takes the environment name and the active provider from the existing health data (no secrets).

## 07.4 Dependencies
- D present: the post-password redirect target is carried by the `SetPassword` link's `ReturnPath` (01 stores it). AmbassadeurInvite suppression as in the table; the admin preview (08) labels it "Geparkeerd".
- E present: #23/#24 follow its inbox and withdraw link; its access-approved invite uses the set-password link (01).
- G present: `MfaResetByAdmin` joins the registry (kind E, reason `Security`): subject `Je tweestapsverificatie is gereset`, body "Een beheerder heeft je tweestapsverificatie gereset. Je stelt hem opnieuw in als je inlogt. Was dit niet de bedoeling? Neem contact met ons op.", button **Inloggen** → `/login`. Absent: skip, followups line.

## Tests
- Snapshot updates for #20–31 × nl/en/ar.
- `AccountEmailCopyTests`: one CTA for E, zero for S; mascot on #21/#25 only; no "Entra" or "SBI" in any value, no "tokens" in candidate mails, no subject suffix "— Lobsy"; no repeated-URL note.
- `AccountLockoutEmailTests`: lockout sends the registry key with a duration that matches `LoginLockoutRules` for counts 5, 6, 7, 9 (15 min, 30 min, 1 uur, 4 uur); failing mail still locks; no "nieuw wachtwoord" in any language.
- `AmbassadeurInviteSuppressionTests` (dep D present): suppressed + admin error; absent: sent as normal.
- `MailTestTests`: button target follows dep G; no secret values in the body (assert against the configured Resend key/SMTP password in the test config).

## Success criteria
- All 31 registry keys now use the B1 copy, 1 button (0 for S), and 5 languages; `EmailStringsParityTests` baseline 0.
- `git grep -n "new EmailMessage(" -- Jobsy.Api Jobsy.Infrastructure Jobsy.Core` only finds the mailer (02 guard).

Done → next: `08-admin-voorbeeld.md`.
