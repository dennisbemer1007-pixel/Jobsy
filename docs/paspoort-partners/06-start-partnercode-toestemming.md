# 06: Start via partner code (`/p/{code}`) + consent flow

**Stacked.**
- Branch: `cursor/paspoort-partners-6` from `cursor/paspoort-partners-5`.
- ONE PR into `acceptatie`, titled **"feat(partners): start via partner code and consent flow"**.
- Rules: see README.
- Gated by `PassportPartnersEnabled`. With the flag OFF, `/p/{code}` returns 404.

Mockup: `docs/mockups/paspoort-partners/e-start-partnercode-toestemming.png` (src `src/e.html`).
- Three panels: `/p/{code}` start in own language; consent for an uitzendbureau; consent for a werkgever (HR). The result panel has no mockup: use the same card style.
- Deviation: the mockup code `KAS-FLX` is not a valid code (L is not in the alphabet). Use the codes from the seed in tests.

## Goal
Decisions 10–13:
- An agency or employer gives candidates a code or QR (flyer, 07).
- The candidate lands on a co-branded page in their own language, creates a free Lobsy account and **actively chooses** whether to share their passport with that partner.
- Saying no costs nothing: "Nu niet, wel gratis verder".
- Existing candidates can add a code later.

## Facts
- Referral attribution today:
  - Web cookies in `Jobsy.Web/Sales/SalesReferralCookie.cs` (`lobsy_sales_ref`, `lobsy_ambassadeur_ref`)
  - Api `AuthController`: external IdP create ~L455, first login ~L476, `TryAttributeCandidateAsync`
  - email-code sign-up stores `EmailSignInChallenge.ReferralCode` (~L661) and attributes on verify (~L789)
- Candidate onboarding wizard: `/candidate/start` (`OnboardingWizard.razor`), before the first test.
- The cookie list for the privacy page is in `Jobsy.Web/Components/Legal/CookieTable.razor` (+ `PrivacyNl.razor`).
- Partner model and services come from 03: `ResolveCodeAsync`, `AttachByCodeAsync`, consent/revoke, max 3, min age 18.

## Scope

### Landing `/p/{code}` (anonymous, `public-read`)
- Normalize the code (upper-case, strip spaces/dash) and resolve it via 03. Invalid, expired or inactive → friendly page "Deze code werkt niet (meer)", with a link to normal sign-up. Same response for every failure.
- Co-branded hero, per partner type:
  - "Lobsy × {logo}"
  - "{DisplayName} werkt met Lobsy"
  - **Uitzendbureau:** "Maak gratis je DNA-paspoort. Wij gebruiken het om je sneller aan passend werk te helpen."
  - **Werkgever:** "… om je beter te leren kennen voor werk bij ons"
- Three benefit bullets.
- What the partner **will** and **will never** see: passport only; never test answers, raw scores or private preferences; no score/ranking.
- Language picker nl/en/pl/ro/ar. Default: Accept-Language if supported, else nl.
- CTA "Gratis starten" (→ register) and "Ik heb al een account" (→ login).
- **Cookie `lobsy_partner`:**
  - value is the normalized code
  - functional, 30 days, `HttpOnly`, `Secure`, `SameSite=Lax`
  - set only after the code resolves
  - add it to `CookieTable.razor` + privacy text in all languages
- The cookie does **not** create consent. It only pre-selects the partner for the consent screen.

### Carry the code through sign-up
Add `PartnerCode` next to `ReferralCode` in:
- the register request DTO
- the external-IdP request (~L455/L476)
- `EmailSignInChallenge` (new nullable column, max 16; migration `AddPartnerCodeToSignInChallenge`)
- the verify step (~L789)

All of these call the same `AttachByCodeAsync` path.

On account creation (and on first login of an existing candidate who carries the cookie), the API resolves the code and calls `AttachByCodeAsync(candidateId, code, Source.Code)`. This creates the link **without consent**: the candidate's data is not visible to the partner, and the link only counts in aggregates ≥ 5. Never consent automatically.

The Web clears the cookie after the API has attached the link (or the code turned out invalid).

### Consent screen (new step in `OnboardingWizard.razor`, right after account creation, before the first test)
Shown for every link with `ConsentGivenAtUtc`, `RevokedAtUtc` and `ConsentPromptDismissedAtUtc` all null, started < 30 days ago, while the partner is still active. One link at a time. Content:
- partner logo/name/type
- a plain-language summary (B1), all 5 languages:
  - "{DisplayName} ziet alleen je DNA-paspoort (dezelfde inhoud als je PDF)"
  - "Nooit: je testantwoorden, ruwe scores of privévoorkeuren"
  - "Lobsy geeft {DisplayName} geen score, ranking of automatische selectie: een mens praat met jou"
  - "Je kunt dit altijd intrekken onder Delen & toegang"
  - "Na 6 maanden vragen we of het nog klopt"
- **Checkbox 1 (required to link, unchecked by default):** "Ik deel mijn DNA-paspoort met {DisplayName}".
- **Checkbox 2 (optional, unchecked):** "{DisplayName} mag contact met mij opnemen via e-mail/telefoon" → `ContactConsentAtUtc`.
- **Checkbox 3 (required to link):** "Ik ben 18 jaar of ouder". Also enforced server-side through the 03 min-age rule; a known DOB under 18 → refusal text, no link.
- Link to the partner terms (03 terms text) + Lobsy privacy statement.
- Buttons:
  - **"Delen en verder"**: calls `POST api/me/passport-partners/{linkId}/consent` with version `PartnerShareConsentVersion`. Consent only happens on this click.
  - **"Nu niet, wel gratis verder"**: equal visual weight, no dark patterns. Sets `ConsentPromptDismissedAtUtc` and nothing else. The copy under the buttons says: "{DisplayName} ziet niet wie je bent. Alleen een totaal aantal als dat 5 of meer is."
- **Result panel:**
  - Shared → "Gedeeld met {DisplayName}. Je paspoort wordt sterker met elke test", then continue to the first test.
  - Declined → "Je gaat gratis verder. Je kunt later een code toevoegen".
- Max-3 reached → explain, link to Delen & toegang to revoke one first. Nothing is overwritten.

### Existing candidates
- Already signed in and opening `/p/{code}` → the same consent screen as a page (`/candidate/paspoort/delen/code/{code}`).
- "Code toevoegen" on the sharing screen (05 placeholder): input `XXX-XXX` with live format validation (alphabet, length), then the consent screen.

### Reconfirmation (6 months; job from 03)
- Banner on `/candidate/paspoort` and on the sharing screen from 14 days before `ReconfirmDueAtUtc`: "Wil je nog delen met {DisplayName}?" with **"Ja, blijf delen"** (re-stamps consent version, +6 months) or **"Stop met delen"** (revoke).
- Suspended links show "Gepauzeerd: bevestig om weer te delen".
- The reminder e-mail is the 03 template `PartnerConsentReconfirmReminder`, sent by the 03 job.

### Emails
- `PassportPartnerShared`: to the candidate, a confirmation listing what is shared + how to revoke.
- No e-mail to the partner per candidate in v1. The partner sees new candidates in the portal (07).

## API
- `GET api/passport-partners/codes/{code}` (03)
- `POST api/me/passport-partners` + `POST api/me/passport-partners/{linkId}/consent` (03)
- `GET api/me/passport-partners/pending-consent` (links needing the screen)
- `POST api/me/passport-partners/{linkId}/dismiss`
- Server-side checks: flag ON, code valid, partner active, max 3, min age, consent version current.

## Tests
- **Unit:**
  - code normalization
  - consent screen shown only for undecided links < 30 days old
  - consent requires checkbox 1 + 3
  - "Nu niet" only sets `ConsentPromptDismissedAtUtc`, and the partner cannot see the candidate individually
  - max-3 refusal
  - under-18 refusal
  - contact consent optional
  - reconfirm extends +6 months; suspended → reconfirm reactivates
- **API:**
  - `PartnerCode` stored for email-code and IdP sign-up
  - sign-up creates only an unconsented link; nothing becomes visible to the partner without the consent call
  - flag OFF → 404/`FeatureDisabled`
- **bUnit:** consent screen renders unchecked boxes; the "Nu niet" button is present with equal style class; Uitzendbureau vs Werkgever copy.
- **Playwright 390 + 1440** (soft-skip, seeded partner code from 09 or a test fixture):
  - `/p/{code}` → register → consent → first test
  - decline path
  - invalid code page
  - language switch to pl shows Polish copy
  - `ar` renders with `dir="rtl"` without overflow
  - cookie `lobsy_partner` present after landing and cleared after sign-up
- **i18n parity.**

## Success criteria
- A new candidate can go from flyer QR to a shared passport in one flow, with explicit opt-in.
- Declining is just as easy and fully free. Nothing becomes visible to a partner without the consent call.
- Existing candidates can add codes. Reconfirmation works.
- Release build with 0 warnings, tests green.

## Out of scope
- The partner portal (07).
- Flyers (07).
- Pricing (08).
- Partner e-mails per candidate.
- Codes for the talent pool.
- Bulk imports of candidates by partners (never in v1).
