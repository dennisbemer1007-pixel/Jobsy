# 05. Candidate mails (#1–10): B1 copy, one button, mascot in good news, ParentalConsent in the catalog

Read `00-README.md` first (§0 Copy, §M #1–10, §B, Dependencies A/C/H). Branch `cursor/emails-5` from `cursor/emails-4`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-5`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Copy only changes through `EmailStrings` values (04). Every changed key changes in **all 5 languages** in the same commit.
> - One button per mail; code mails have none. No "Klik hier", no exclamation-mark openers, no em-dashes.

| | |
|---|---|
| Branch | `cursor/emails-5` |
| PR title | `feat(email): candidate mails in B1 with one button, mascot for good news, parental consent in the catalog, code lifetimes from constants` |
| PR body starts with | `Stacked on #<PR 04> (cursor/emails-4)` + the outcome of the dep A/C/H re-check |
| Mockups | `em-d02`/`em-m02` (code mail), `em-d03`/`em-m03` (ApplicationConfirmation), `em-d05`/`em-m05` (EmployerReactionAccepted, good news + mascot) |
| Split seam | **05a** = #1–5 + #10; **05b** = #6–9 |

## 05.0 Re-check dependencies first
Run the A, C and H commands from the README. Write the outcome in the PR body. `EmailLinks.Map` and the employer name source follow the case that applies. Nothing else in this file depends on them.

## 05.1 Code lifetimes from constants (bug "10 minuten")
- `ApplicationsController` L886 `DateTime.UtcNow.AddMinutes(10)` → a new `ApplicationRules.EmailVerificationCodeLifetime = TimeSpan.FromMinutes(10)` (Core). The controller and the mail both read it.
- AccountUnsubscribeVerification already gets `ttlMinutes` from `PrivacyDataService.UnsubscribeCodeTtlMinutes` (L18); keep that, but pass it through `EmailFormat` duration.
- Registration/takeover codes (07) use `CompanyRegistrationService.ActivationTokenTtl`.
- Test: change the constant in a test-only overload → the mail text follows (no literal "10" in any `Email.*` value; guard: regex `\b\d+\s*(minuten|dagen)\b` in nl values fails).

## 05.2 The copy (nl final; en final; pl/ro/ar drafts from these)
Greeting "Hoi {voornaam}," (first name only: `NameParts.FirstName(fullName)`; add the helper if missing; no name → "Hallo,"). Sign-off "Team Lobsy". `{vacature}` and `{bedrijf}` are bold via `EmailArg`. The eyebrow is a short label above h1.

| # | Key | Subject | Preheader | Eyebrow · h1 | Body (short) | Button → target | Note |
|---|---|---|---|---|---|---|---|
| 1 | ApplicationConfirmation | `Je sollicitatie is verstuurd: {vacature}` | `{bedrijf} krijgt je sollicitatie nu te zien.` | Sollicitatie · "Je sollicitatie is verstuurd" | "Je hebt gesolliciteerd op **{vacature}** bij **{bedrijf}**." Facts: Vacature, Bedrijf, Plaats (when known), Verstuurd op (date). Steps: "1 {bedrijf} bekijkt je sollicitatie. 2 Je krijgt een mail als er nieuws is. 3 Je ziet de status altijd in Lobsy." | Bekijk je sollicitatie → `/candidate/applications` | – (stub note **removed**, see 05.3) |
| 2 | ApplicationVerificationCode | `Je code voor je sollicitatie: {code}` | `Vul deze code in om je sollicitatie te versturen.` | Code · "Je code" | "Vul deze code in op Lobsy om je sollicitatie op **{vacature}** te versturen." Code block. "De code werkt {n} minuten." | **none** (code; today's "Terug naar de vacature" button goes) | "Heb je dit niet zelf gedaan? Dan kun je deze mail negeren." |
| 3 | EmployerReactionAccepted | `Goed nieuws over je sollicitatie bij {bedrijf}` | `{bedrijf} wil verder met je.` | Goed nieuws · "{bedrijf} wil verder met je" + mascot | "{bedrijf} heeft je sollicitatie op **{vacature}** geaccepteerd. Ze nemen snel contact met je op. Houd je telefoon en je mail in de gaten." | Bekijk je sollicitatie → `/candidate/applications` | – |
| 4 | EmployerReactionRejected | `Nieuws over je sollicitatie bij {bedrijf}` | `Er zijn nog meer vacatures bij jou in de buurt.` | Sollicitatie · "Deze keer niet" | "Bedankt dat je hebt gesolliciteerd op **{vacature}** bij **{bedrijf}**. Ze kiezen deze keer voor iemand anders. Dat is jammer. Er zijn nog genoeg andere vacatures bij jou in de buurt." | Bekijk andere vacatures → `EmailLinks.Map` | – |
| 5 | EmployerContacting | `{bedrijf} neemt contact met je op` | `Houd je telefoon en je mail in de gaten.` | Goed nieuws · "{bedrijf} neemt contact met je op" + mascot | "{bedrijf} wil met je praten over **{vacature}**. Je hoort snel van ze, via telefoon, mail of WhatsApp." (add `candidateName` + `companyName` parameters; today the mail has neither) | Bekijk je sollicitatie → `/candidate/applications` | – |
| 6 | ApplicationHired | `Gefeliciteerd, je bent aangenomen bij {bedrijf}` | `Je nieuwe baan: {vacature}.` | Gefeliciteerd · "Je bent aangenomen" + mascot | "Je bent aangenomen voor **{vacature}** bij **{bedrijf}**. Veel succes met je nieuwe baan." With withdraw URL: "Heb je nog andere sollicitaties? Trek ze in. Dan weten die werkgevers dat je al werk hebt." | with withdraw URL: **Andere sollicitaties intrekken** → the withdraw-others URL (page, POST there); without: Bekijk je sollicitatie | with withdraw URL: plain link "Bekijk je sollicitatie" |
| 7 | ApplicationFilledElsewhere | `Nieuws over je sollicitatie bij {bedrijf}` | `Er zijn nog meer vacatures bij jou in de buurt.` | Sollicitatie · "Deze vacature is vervuld" | "Bedankt dat je hebt gesolliciteerd op **{vacature}** bij **{bedrijf}**. {bedrijf} heeft iemand anders aangenomen. Er zijn nog genoeg andere vacatures bij jou in de buurt." | Bekijk andere vacatures → `EmailLinks.Map` | – |
| 8 | PushBom (kind O) | `Nieuwe vacature bij jou in de buurt: {vacature}` | `{bedrijf}, {afstand} van jou.` | Vacature in de buurt · "Een vacature bij jou in de buurt" | "Deze vacature past bij jou." Facts: Vacature, Bedrijf, Plaats, Afstand (`EmailFormat.Km`), Reistijd ("{n} min"), wage row when present (label from `wageNote`, value `EmailFormat.Money`). | **Bekijk de vacature** → `/vacancies/{id}` (replaces "Klik hier") | "Zoek je geen werk meer? `Zet je status op Niet beschikbaar`." (link = the existing token URL `/candidate/actions/set-unavailable?t=`, which is a page with a button; keep it). Footer has Mail-instellingen + unsubscribe (03). The word "PushBom" never appears in the mail. |
| 9 | AccountUnsubscribeVerification | `Je code om je account te verwijderen: {code}` | `Vul deze code in om je account te verwijderen.` | Code · "Je code" | "Je hebt gevraagd om je Lobsy-account te verwijderen. Vul deze code in om dat te bevestigen." Code block. "De code werkt {n} minuten." | **none** (today's "Code invoeren" button goes) | "Heb je dit niet zelf gevraagd? Negeer deze mail. Je account blijft dan gewoon bestaan." |
| 10 | ParentalConsent | `{kind} vraagt je toestemming voor Lobsy` | `Bekijk waarvoor {kind} je toestemming vraagt.` | Toestemming · "{kind} vraagt je toestemming" | "Hallo," (the parent's name is unknown). "{kind} is jonger dan {leeftijd} jaar en wil Lobsy gebruiken om te solliciteren. Daarvoor is jouw toestemming nodig." Facts: Naam (first name), Link geldig tot (date). "Op de pagina lees je wat Lobsy doet en geef je toestemming met één knop." | **Toestemming bekijken** → `/toestemming?t=` (01) | "Weet je niet waar dit over gaat? Dan hoef je niets te doen. Zonder toestemming kan {kind} niet solliciteren." |

- Subjects with a code (#2, #9) put the code in the subject only when `MailOptions.CodeInSubject` is true (default **true**; codes are short-lived and the subject shows in lock screens either way; D-extra, see 05.5). The preheader never repeats the code.
- ParentalConsent (01 made it the template `TransactionalEmails.ParentalConsent(baseUrl, childFirstName, confirmUrl, expiresAtUtc)`) now moves to the registry as #10 with reason `ParentAsked`: "Je krijgt deze mail omdat {kind} je e-mailadres heeft opgegeven als ouder of verzorger." Language = the child's (04).
- Reasons (footer), nl: `Applied` "Je krijgt deze mail omdat je via Lobsy hebt gesolliciteerd." · `ApplyCode` "Je krijgt deze mail omdat iemand met dit e-mailadres wil solliciteren via Lobsy." · `NearbyJobs` "Je krijgt deze mail omdat je vacatures in de buurt wilt ontvangen." · `AccountRequest` "Je krijgt deze mail omdat je dit hebt aangevraagd in Lobsy."

## 05.3 Bugs (each gets a regression test)
- "Authenticator stub: verificatie gesimuleerd." (L166) goes. The `authenticatorStubUsed` parameter is removed from the template. If a caller needs the fact, it logs it (`PlatformLog` Debug). Test: no `Email.*` value in any language contains "stub" (case-insensitive).
- "Klik hier" (L388): guard test, no CTA label equals "Klik hier" / "Click here" in any language.
- "Top!", "Wellicht", "Wat een feest!", "— en geniet": gone with the rewrite; the §0 guard (no em-dash U+2014/U+2013 in values) catches regressions.
- ApplicationHired had 2 buttons (primary + secondary + a repeated link in the note): now 1 button + at most 1 note link.
- Code mails (#2, #9) had a button: now none (`EmailRenderer` asserts kind S has 0 `data-lobsy-cta`).
- EmployerContacting had no greeting and no company: fixed with new parameters (update `ApplicationsController` call site).

## 05.4 Dependencies in the copy
- A/C: `EmailLinks.Map` only (#4, #7).
- H present: `{bedrijf}` for candidate mails comes from `IntermediaryPublicIdentity.ForCandidate(...)` (the bureau in hidden mode). Only #5 and #6 may name the opdrachtgever, with the intermediair 03.7 sentence as an extra paragraph key `Email.EmployerContacting.ViaIntermediary` / `Email.ApplicationHired.ViaIntermediary`. H absent: `CompanyName` as today, followups line.
- Landing 03 code mails (A present): register them as kind S, reason `ApplyCode`-style `SignIn` ("…omdat iemand met dit e-mailadres wil inloggen bij Lobsy."), copy like #2 with no button. Don't change landing's sending code beyond calling the mailer.

## 05.5 Small defaults (write them in the PR body)
- Code in subject: on for #2/#9 (and 07's code mails). Reason: the user is waiting for it; the code is short-lived.
- First name only in greetings (never the full name).
- Dates as "30 september 2026" (no weekday).

## Tests
- Snapshot updates for the 10 keys × nl/en/ar (regenerate, and review the diff in the PR).
- `CandidateEmailCopyTests`: exactly 1 CTA for kind E/O; 0 for S; mascot exactly on #3, #5, #6; `Hoi {first name},` greeting with a full name input; no "stub", "Klik hier", em-dash, "PushBom" in any language.
- `ApplicationVerificationCodeLifetimeTests`: the controller expiry and the mail both use `ApplicationRules.EmailVerificationCodeLifetime`.
- ParentalConsent: registry key present, reason `ParentAsked`, language = child's, button target `/toestemming?t=` on `PublicWebBaseUrl`.
- `EmployerContacting` call site passes the candidate first name and company (unit test on the controller path with a fake mailer).

## Success criteria
- The 10 mails render like em-02/03/05 (spacing, button, mascot) in nl desktop + mobile; PNGs in the PR.
- All candidate `Email.*` keys exist in 5 languages; parity green; baseline not grown.

Done → next: `06-werkgever-mails.md`.
