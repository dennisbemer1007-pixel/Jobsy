# 06. Profiel & gegevens, self-billing consent, safe IBAN change, onboarding `/sales/start`, Hulp & afspraken

Read `00-README.md` first (§0, §IA, §R, §D, §P, D5, D6, Dependencies D). Branch `cursor/salesmanager-6` from `cursor/salesmanager-5`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-6` (from `cursor/salesmanager-5`) |
| PR title | `feat(sales): profile — btw/KOR, self-billing consent, IBAN validation + 2FA step-up + hold, onboarding, help` |
| PR body starts with | `Stacked on #<PR 05> (cursor/salesmanager-5)` + the consent text and the KOR text for Dennis/accountant review |
| Mockups | `sm-d6-profiel-gegevens.png` |
| Split seam | **06a** = `SalesPayoutProfileService` + IBAN rules + step-up + consent + API (06.2–06.5). **06b** = Profiel page + `/sales/start` + Hulp & afspraken (06.6–06.8) |

## Goal
The data that ends up on invoices is correct and safe: the beneficiary chooses 21 % btw or KOR, gives a separate, recorded self-billing consent, and can change their payout IBAN only with a second factor, after which they get a mail and payouts to the new account wait 3 days. New salesmanagers go through one clear onboarding.

## 06.1 Today (verify first)
- `SalesManagerOnboardingService` + `Components/Pages/SalesManager/Onboarding.razor` (now at `/sales/start`): company name, KvK (8 digits), VAT number (NL regex, **mandatory**), address, IBAN (no mod-97). Signing the agreement (`me/sign-agreement`, version `2026-08-28-sm-mediation`) generates the `SM-XXXXXX` code. The agreement text only *mentions* self-billing; there's no separate consent.
- `PUT api/sales-managers/me/profile` changes the IBAN **without re-auth, 2FA or notification**. IBAN is encrypted by `IbanValueConverter` / `IbanProtector` and masked in the UI.
- Ambassadeur twins: `AmbassadeurOnboardingService`, `api/ambassadeurs/me/profile|sign-agreement`, agreement `2026-08-03-ambassadeur-mediation`. Parked since 01.10: leave them untouched (no redesign, no removal).
- TOTP: `Jobsy.Core/Security/TotpAuthenticator.cs`; fresh-code verification pattern in `AdminController` (`mfa/reset`, ~L555).

## 06.2 SalesPayoutProfileService (role-agnostic, one code path)
- Works on `ISalesPayoutProfile` (01.3), never on a concrete profile type or role, so a re-enabled ambassadeur can use it unchanged. Methods: `GetAsync`, `UpdateCompanyAsync` (name, KvK 8 digits, address, postal code, city, country), `SetVatTreatmentAsync` (`Standard21` needs a valid btw-nummer; `SmallBusinessScheme` needs the confirmation "Ik ben aangemeld voor de KOR bij de Belastingdienst." and makes the btw-nummer optional; sets `VatTreatmentChangedAtUtc`), `BeginIbanChangeAsync` / `ConfirmIbanChangeAsync` (06.3), `GiveConsentAsync` / `RevokeConsentAsync` (06.4), `SetEmailPrefsAsync`.
- A VAT treatment change recalculates an open request in `Requested` (07); requests already in a run keep their snapshot.
- Company data changes apply to **future** invoices only (issued invoices keep their snapshot fields).
- The old `me/profile` PUT endpoint on `api/sales-managers` is switched to this service; their IBAN field is **ignored** (use the IBAN endpoints) and the response says so in `warnings`.

## 06.3 IBAN: validation, step-up, notification, hold (D6)
- `Jobsy.Core/Sales/Iban.cs`: `Normalize` (strip spaces, upper-case), `IsValid` (SEPA country list with lengths, mod-97 = 1), `Mask` → `NL•• •••• •••• 4821`, `Last4`. Unit tests with valid/invalid NL, BE, DE numbers and typos.
- `POST api/sales/me/payout-account/change` `{ iban, holderName (2–70) }` → validates, stores a pending change (Data Protection–encrypted in a short-lived server-side record or the existing one-time token store; **never** in the browser), returns `{ method: "totp" | "email" }`:
  - local-password users: `POST …/change/confirm { code }` verifies a **fresh** TOTP with `TotpAuthenticator` (same checks as the admin reset: 6 digits, replay window);
  - external-login users (ADR 0005): a one-time link (30 min) is mailed to the account e-mail; `GET /sales/profiel/iban-bevestigen?token=` confirms.
  - 5 wrong codes → the pending change is dropped; max 3 IBAN changes per 30 days (429 with an nl message).
- On confirm (one transaction): store the IBAN + holder, `IbanChangedAtUtc = now`, `IbanPayoutHoldUntilUtc = SalesClock.EndOfLocalDayUtc(now + IbanChangeHoldDays)` (setting, default 3), refresh `MaskedIban` on an open `Requested` request, `PlatformLog` `sales.iban.changed` (user id, last 4 old/new), and send `SalesMail.IbanChanged` (always, not subject to preferences): "Je uitbetaalrekening is gewijzigd naar {masked}. Was jij dit niet? Neem direct contact op met Lobsy." with time (Europe/Amsterdam).
- The **first** IBAN during onboarding needs no step-up and has no hold (there's no earlier account to protect).
- Admin never sees the full IBAN (masked only); viewing the masked account in admin writes `PersonalDataAccessLog` (`sales.payout-account`, `view`).

## 06.4 Self-billing consent (D5)
- Consent text `Sales.SelfBilling.ConsentText` (B1, reviewable), version constant `SalesSelfBilling.CurrentVersion = "2026-10-self-billing-v1"`. Core content: Lobsy makes the invoices for the commission on your behalf; you agree with this way of invoicing; you check each invoice and tell Lobsy within 14 days if something is wrong; you tell Lobsy when your btw situation changes; you can stop this at any time, after which payouts need a manual invoice (via Lobsy support).
- `GiveConsentAsync` writes `SalesSelfBillingConsent` (version, SHA-256 of the exact rendered text, time). `RevokeConsentAsync` sets `RevokedAtUtc` (outline-danger + confirm dialog) and blocks new payout requests (07 blocker).
- Existing onboarded beneficiaries have **no** consent yet: the dashboard todo (04) and the wallet blocker (07) ask for it; nothing else is blocked.
- When the version changes later, the latest consent with an older version counts as missing.

## 06.5 API
- `GET api/sales/me/profile` (company, VAT treatment, masked IBAN + holder + hold-until, agreement version + signed date, consent status, email prefs, 2FA status), `PUT api/sales/me/profile/company`, `PUT …/vat`, `PUT …/email-prefs`, `POST …/consent`, `POST …/consent/revoke`, the IBAN endpoints (06.3), `GET api/sales/me/agreement.pdf` (the signed agreement text as PDF, existing text), `GET api/sales/me/self-billing-consent.pdf`.
- All beneficiary-from-user; rights-matrix rows added (foreign user impossible by construction; werkgever/candidate 403; admin 403 on `me`).

## 06.6 Profiel & gegevens `/sales/profiel` (`sm-d6`)
- Header "Profiel & gegevens", lead "Deze gegevens staan op je facturen. Houd ze goed bij."
- One card with setting rows (label + help on the left, controls on the right):
  - **Bedrijf** ("Staat op elke factuur die Lobsy voor je maakt."): Bedrijfsnaam, KvK-nummer, Land, Adres (postcode, plaats). Save button per section.
  - **Btw** ("Bepaalt of er btw op je factuur komt."): two radio cards "Ik ben btw-plichtig" (21 % btw op de factuur; btw-nummer field) and "Ik gebruik de KOR" (Kleineondernemersregeling: geen btw op de factuur; confirmation checkbox).
  - **Uitbetaalrekening** ("Hier maakt Lobsy je geld naartoe over."): IBAN masked, Op naam van, "Wijzigen" (drawer with IBAN + name → step-up). Warning note "Veilig wijzigen: voor een nieuwe IBAN vragen we je 2FA-code. Je krijgt een mail. De eerste uitbetaling naar een nieuwe rekening wacht {n} dagen." When a hold is active: "Uitbetalingen naar deze rekening kunnen vanaf {datum}."
  - **Afspraken** ("Wat je met Lobsy hebt afgesproken."): Samenwerking (agreement name + version + PDF), Self-billing ("Akkoord op {datum}" + PDF, or primary "Toestemming geven" when missing; "Intrekken" as a quiet link), Commissie ("25 % · 10 % · 5 % over 3 jaar per werkgever", or 20 % in year 1 for a recommended salesmanager).
  - **Beveiliging** ("Je ziet geld en een rekeningnummer. Daarom is 2FA verplicht."): Tweestapsverificatie status pill + "Beheren" (existing 2FA page), Meldingen per mail (three switches: nieuwe werkgever, commissie beschikbaar, uitbetaling) + save.
- Mobile: rows stack; radio cards full width.

## 06.7 Onboarding `/sales/start`
- 3 steps with a stepper (desktop left rail, mobile top): **1 Gegevens** (company, KvK, address, btw choice + number) → **2 Uitbetaalrekening** (IBAN + holder, validated) → **3 Afspraken** (agreement text scroll + checkbox "Ik ga akkoord met de samenwerking", **separate** consent block + checkbox "Ik geef toestemming voor self-billing") → **Klaar** ("Je code is {code}. Deel je link." + buttons to `/sales/link` and `/sales`).
- The code is generated on finishing step 3, as today (6 chars from the existing alphabet; keep the `SM-` format). Both agreement signature and consent are stored separately.
- Private persons: the KvK field is required; help text "Lobsy werkt samen met ondernemers. Je hebt een KvK-nummer nodig." (D5).

## 06.8 Hulp & afspraken `/sales/hulp`
- FAQ (accordion, `Sales.Help.*`): Hoe tel je een aanmelding voor mij? · Wanneer is mijn commissie beschikbaar? · Hoe en wanneer krijg ik geld? · Wat is self-billing? · Ik gebruik de KOR, wat nu? · Wat zie ik van werkgevers (en wat niet)? · Een klant vraagt geld terug, wat gebeurt er? · Mijn IBAN wijzigen.
- Downloads: agreement PDF, consent PDF. Contact: the existing support/contact flow.

## Tests
- IBAN rules; step-up TOTP (valid, wrong ×5, replay), e-mail link (expiry, single use); rate limit; hold date; notification mail always sent; no full IBAN in any API response (JSON scan test on every `api/sales/me/*` response).
- VAT: 21 % requires a valid btw-nummer; KOR requires the confirmation; change recalculates a `Requested` request.
- Consent: version + hash stored; revoke blocks; older version = missing.
- Onboarding: three steps, code generated once, KvK required.
- bUnit: profile sections, radio cards, IBAN drawer states, consent flow; `SalesPortalNoInlineStyleTests` allow-list shrinks by the moved onboarding page.

## Success criteria
- `dotnet build` + `dotnet test` green.
- The seed salesmanager gives self-billing consent, switches to KOR, changes the IBAN with a TOTP code, gets the mail, and sees "Uitbetalingen naar deze rekening kunnen vanaf {vandaag + 3}".
- Changing the IBAN through any old endpoint is impossible.

Done → next: `07-wallet-uitbetalen.md`.
