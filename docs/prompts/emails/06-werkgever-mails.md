# 06. Employer mails (#11–19): B1 copy, one button, double escape, "14 dagen", UTC dates, old routes

Read `00-README.md` first (§0 Copy, §M #11–19, §B, Dependencies B/E). Branch `cursor/emails-6` from `cursor/emails-5`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/emails-6`; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Copy only changes through `EmailStrings` values (04), in all 5 languages in the same commit.
> - No candidate PII in employer mails (no candidate name, photo, phone, e-mail, address). The employer sees the candidate in Lobsy after logging in.
> - Numbers of days come from the rule constants, never literals.

| | |
|---|---|
| Branch | `cursor/emails-6` |
| PR title | `feat(email): employer mails in B1 with one button, day counts from rules, Amsterdam dates, employer links via EmailLinks, no double escape` |
| PR body starts with | `Stacked on #<PR 05> (cursor/emails-5)` + the outcome of the dep B/E re-check |
| Mockups | `em-d04`/`em-m04` (EmployerNewApplication), `em-d06`/`em-m06` (UserInvite with set-password link) |
| Split seam | **06a** = #11–14 + #18–19; **06b** = #15–17 (the three jobs) |

## 06.0 Re-check dependencies first
Run B and E from the README; write the outcome in the PR body. B decides every employer URL in `EmailLinks.Employer*` (02 made them one place). E adds the werkgever-aanmelding mails to the registry (see 06.5).

## 06.1 The copy (nl final; en final; pl/ro/ar drafts)
Greeting "Hallo {voornaam}," when the recipient is a known user, else "Hallo,". Employer mails are per recipient (04: each recipient's language and first name).

| # | Key | Subject | Preheader | Eyebrow · h1 | Body (short) | Button → target |
|---|---|---|---|---|---|---|
| 11 | EmployerNewApplication | `Nieuwe sollicitatie op {vacature}` | `Bekijk de sollicitatie en reageer snel.` | Nieuwe sollicitatie · "Iemand heeft gesolliciteerd" | "Er is een nieuwe sollicitatie op **{vacature}**." Facts: Vacature, Vestiging, Ontvangen op (date+time, Amsterdam), Match ("{n} %", **only** when the application has a `MatchPercent` that the employer already sees in the sollicitaties list, else no row). "Reageer snel. Kandidaten haken af als ze lang niets horen." | Bekijk de sollicitatie → `EmailLinks.EmployerApplications(applicationId)` |
| 12 | CandidateWithdrawn | `Sollicitatie ingetrokken: {vacature}` | `Je hoeft niets meer te doen voor deze sollicitatie.` | Sollicitatie · "Een sollicitatie is ingetrokken" | "Een kandidaat heeft de sollicitatie op **{vacature}** ingetrokken. Je hoeft niets te doen." | Bekijk je sollicitaties → `EmailLinks.EmployerApplications()` |
| 13 | CandidateWithdrawnOtherJob | `Sollicitatie ingetrokken: {vacature}` | `De kandidaat heeft ander werk gevonden.` | Sollicitatie · "Een sollicitatie is ingetrokken" | "Een kandidaat heeft de sollicitatie op **{vacature}** ingetrokken, omdat die ander werk heeft gevonden. Je hoeft niets te doen." | Bekijk je sollicitaties → same |
| 14 | PendingApproval | `Vacature wacht op je goedkeuring: {vacature}` | `Keur de vacature goed om hem te publiceren.` | Goedkeuring · "Een vacature wacht op jou" | "De vacature **{vacature}** van **{vestiging}** wil online. Er zijn niet genoeg tokens om hem direct te publiceren. Jij kunt de aanvraag goedkeuren." ("(onvoldoende tokens)" goes) | Aanvraag bekijken → `EmailLinks.EmployerTokens` (B present: the new tokens/approvals URL) |
| 15 | VacancyEngagementReminder (kind O) | `Je vacature staat {n} dagen online: {vacature}` | `{views} keer bekeken, {applications} sollicitaties.` | Even checken · "Hoe gaat het met je vacature?" | "**{vacature}** staat {n} dagen online (`OpenDaysBeforeReminder`)." Facts: In zoekresultaten, Bekeken, Gedeeld, Bewaard, Sollicitaties. "Tip: {tip}" (06.3). "Pas je de vacature aan vóór de einddatum? Dan verlengen we hem met {m} dagen (`GoodwillExtendDays`)." | **Vacature verbeteren** → `EmailLinks.EmployerVacancyEdit(id)` (the Highlight and PushBom buttons and the "tokens vereist" note go; the edit page already offers both) |
| 16 | DraftVacancyCleanupWarning | `Je concept {vacature} wordt op {datum} verwijderd` | `Publiceer het concept of laat het verwijderen.` | Concept · "Je concept wordt binnenkort verwijderd" | "Je concept **{vacature}** voor **{vestiging}** staat al {n} dagen klaar (`WarningAfterDays`) en is nog niet gepubliceerd. Doe je niets? Dan verwijderen we het op **{datum}** (Amsterdam date of `deleteOnUtc`; the `DeleteAfterWarningDays` constant, not "14"). Gepubliceerde vacatures blijven altijd bewaard." | Concept bekijken → `EmailLinks.EmployerVacancyEdit(id)` |
| 17 | CompanyReEngagement (kind O) | `Plaats je volgende vacature op Lobsy` | `Je account en je gegevens staan nog klaar.` | Lobsy · "Klaar voor je volgende vacature?" | "Hallo team **{bedrijf}**," "Het is een tijd stil bij jullie op Lobsy. Je account staat nog klaar. Een nieuwe vacature plaats je in een paar minuten." Steps: "1 Log in. 2 Maak een vacature. 3 Publiceer hem." (the KPI list "CSV Batch Import / Externe API / Tokens" goes) | Naar je dashboard → `EmailLinks.EmployerHome` |
| 18 | CompanyApiKeyCredentials | `Haal je API-sleutel op voor {bedrijf}` | `De link werkt tot {datum}.` | Koppeling · "Je API-sleutel staat klaar" | "Hallo," "Je kunt de API-sleutel voor **{bedrijf}** één keer bekijken. Bewaar hem daarna op een veilige plek." Facts: Bedrijf, Link geldig tot (date+time, Amsterdam), Documentatie (`{api}/swagger` as text, not a button). "Als je de sleutel ophaalt, stopt de oude sleutel met werken." | **API-sleutel ophalen** → `/koppeling/sleutel?t=` (01) |
| 19 | UserInvite | `Je bent uitgenodigd voor {bedrijf} op Lobsy` | `Kies een wachtwoord en begin.` | Uitnodiging · "Je bent uitgenodigd" | "{uitnodiger} nodigt je uit als **{rol}** bij **{bedrijf}**." Facts: Rol, Bedrijf, Je e-mailadres, Link geldig tot. "Je kunt ook inloggen met Google of Microsoft met dit e-mailadres." Promoted candidate: "Je oude sollicitaties blijven zichtbaar." Existing user with a login (01): "Log in zoals je gewend bent." | **Uitnodiging accepteren** → set-password link (01), or **Inloggen** → `/login` for an existing user with a login |

- Reasons (footer), nl: `ManagesVacancies` "Je krijgt deze mail omdat je vacatures beheert voor {bedrijf} op Lobsy." · `ManagesCompany` "Je krijgt deze mail omdat je {bedrijf} beheert op Lobsy." · `ApiKeyRequested` "Je krijgt deze mail omdat iemand van {bedrijf} een API-sleutel voor jou heeft aangevraagd." · `Invited` "Je krijgt deze mail omdat {uitnodiger} je heeft uitgenodigd." (`{uitnodiger}` = the inviter's first name, or `{bedrijf}` when unknown).
- Never put the candidate's name in #11–13. Test asserts it with a sample candidate "Sanne van Dijk" whose name must not appear.
- UserInvite needs `inviterFirstName` and `companyName` parameters; update `CompanyUsersController` (01 already changed the password part; don't touch that again).

## 06.2 Bugs (each gets a regression test)
- **Double escape (L691):** `Heading($"Uitnodiging — {Escape(roleLabel)}")` escapes twice because `Heading` escapes. The renderer (02) escapes once; test: a role label `R&D` renders as `R&amp;D` once in HTML (never `&amp;amp;`) and as `R&D` in the text part and subject.
- **"14 dagen" literals (L432, L467, L478, L485):** gone; values use `{0}` with `OpenDaysBeforeReminder`, `WarningAfterDays`, `DeleteAfterWarningDays`, `GoodwillExtendDays`. Test: change a constant via the template parameter → the mail follows; the §05.1 regex guard covers "dagen".
- **UTC dates:** `deleteOnUtc.ToString("dd-MM-yyyy")` (L466) → `EmailFormat.Date` (Amsterdam). Test: `deleteOnUtc` 2026-10-13 22:30 UTC → "14 oktober 2026".
- **Old routes:** every employer URL comes from `EmailLinks.Employer*`. Test: `git grep`-style source test, no `"/branch/` or `"/employer/` literal in `Jobsy.Core/Email` outside `EmailLinks.cs`.
- **Jargon:** "(onvoldoende tokens)", "CSV Batch Import", "Externe API", "API-key" (→ "API-sleutel") gone from mail text. "tokens" may stay in #14 (employers know the word), never in candidate mails.
- **Buttons:** VacancyEngagementReminder 3 → 1; CompanyApiKeyCredentials 2 → 1 (the swagger URL is a fact row); guard test counts 1.

## 06.3 The engagement tip
- `VacancyEngagementReminderRules.BuildHeuristicTip` returns Dutch sentences. Add `EngagementTip BuildHeuristicTipKind(...)` (enum: `LowVisibility`, `ViewsNoApplications`, `ManyViewsFewApplications`, `NotShared`, `LowClickThrough`, `General`) and localize each as `Email.VacancyEngagementReminder.Tip.{Kind}` (B1, ≤ 2 sentences, no "PushBom"/"Highlight" names; "Maak de titel concreter" style).
- Keep `BuildHeuristicTip` only if something else calls it (grep); then make it return the nl text from the same key.

## 06.4 Job senders
- The three hosted jobs (`VacancyEngagementReminderHostedService`, `DraftVacancyCleanupHostedService`, `CompanyReengagementHostedService`) send per recipient via the mailer (02) with the recipient's language (04) and the optional-mail opt-out check (03) for #15 and #17. #16 is kind E (a deletion warning is always sent).
- Each keeps its idempotency key (03: `{key}:{entityId}:{period}`) so a restart doesn't double-send.

## 06.5 Werkgever-aanmelding mails (only when dep E is present)
For each of its mails (verification reminder, verified, deleted, business-e-mail code, reject reason, access request + reminder + decision, ownership-transfer notice): registry entry (kind E; codes S), reason key (`Registered`/`YouManage`/`TakeoverRequested`-style, add new ones when none fit), the §0 copy rules, one button, and 5 languages if they don't have them yet. `CompanyVerified` gets the mascot. Don't change their triggers or timing. Absent: nothing to do; followups line.

## Tests
- Snapshot updates for #11–19 × nl/en/ar.
- `EmployerEmailCopyTests`: one CTA each; no candidate PII (sample name absent); match row only when `MatchPercent` is set; no `/branch/`/`/employer/` outside `EmailLinks`; `R&D` escape test; day counts follow the constants; Amsterdam date test.
- `EngagementTipTests`: each branch of the heuristic → the right kind; every kind has a key in 5 languages.
- Job tests: the opt-out check suppresses #15/#17 and not #16; per-recipient language (one nl manager, one en manager → two languages).
- `UserInviteTests`: inviter first name + company in the mail; existing-user variant has the Inloggen button and no link.

## Success criteria
- #11 and #19 match em-04 and em-06 (desktop + mobile PNGs in the PR).
- No literal day or minute counts in any `Email.*` value; no double escape anywhere (renderer test over all keys: no `&amp;amp;`).

Done → next: `07-registratie-account-intern.md`.
