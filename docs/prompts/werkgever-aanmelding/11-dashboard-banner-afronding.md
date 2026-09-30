# 11. Dashboard banner, checklist and visibility panel; docs, privacy text and full E2E

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-11` from `cursor/werkgever-aanmelding-10` (or `-10b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Re-check README Dependencies **C** (werkgever shell) first. The banner is app design system (not the public theme), and the privacy text change is copy only (no new processing beyond what 03–10 built).

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-11` |
| PR title | `feat(werkgever): "Nog niet zichtbaar voor kandidaten" banner, verification checklist and visibility panel; docs, privacy text and end-to-end tests for employer sign-up` |
| PR body starts with | `Stacked on #<PR 10> (cursor/werkgever-aanmelding-10)`, then the C case |
| Mockups | `wr-d11` (banner, checklist, "Klaar" vacancy row, visibility panel) |
| Split seam | **11a** = banner + checklist + panel + code entry + suggestions (11.2–11.5). **11b** = docs + privacy + E2E (11.6–11.8) |

## Goal
From the first minute in the dashboard, an unverified employer sees in plain words that candidates can't see them yet, why, and the one or two steps to fix it. The stack ends documented and covered end-to-end.

## 11.1 Today (verify first)
- Dependencies C: `WerkgeverLayout`/`WgPageShell`/`WerkgeverNav` (werkgever-redesign) or today's `MainLayout` + `Components/Home/EmployerHomePanel.razor` (`Pages/Home.razor`).
- 03: `CompanyVerificationRules`, "klaar" vacancies, `company_unverified` 403s. 06: `/register/verifieren`, letter status + `…/letter/confirm`. 07: access requests (Te doen). 10: lender state for bureaus.
- `docs/ROUTES.md`, `CHANGELOG.md`, `Pages/Legal/Privacy.razor`, `docs/i18n/README.md`, the Playwright suites in `Jobsy.Tests/*PlaywrightTests.cs`.

## 11.2 Banner (every employer page while unverified)
- `CompanyVerificationBanner` (Present case: in the `WgPageShell` `Banner` slot, added if missing; Absent case: rendered by `MainLayout` for employer roles, see README C), only for the bedrijfsmanager/vestigingsmanager/intermediair of an **unverified/pending/rejected** root company:
  - title **"Nog niet zichtbaar voor kandidaten"** + a pill **"Niet geverifieerd"** (or "Brief onderweg" / "In controle bij Lobsy" / "Afgewezen: {reden}")
  - one line: "Je bedrijf, vacatures en bedrijfspagina verschijnen pas op Lobsy als we weten dat je echt bij {bedrijf} hoort."
  - the primary button per state: **Verifieer nu** (→ `/register/verifieren`), **Code uit de brief invoeren** (inline, 11.4), or none (pending manual check, "We reageren vóór {datum}")
  - dismissible per session, never permanently; `role="status"`, no layout shift (reserved height).
- After verification: one-time success banner "Je bedrijf is zichtbaar voor kandidaten 🎉" with the number of vacancies that went live (03.5), then gone.
- Bureaus (10) with verified ownership but pending uitleenregistratie: the banner "Je bureau is zichtbaar. Publiceren kan zodra we je uitleenregistratie hebben bevestigd." (no verify button).

## 11.3 Checklist and visibility panel (wr-d11)
- Checklist card on the dashboard (werkgever 02 Te doen items when present; else a card in `EmployerHomePanel`): 1 Account aangemaakt ✓, 2 **Bedrijf verifiëren**, 3 Over je bedrijf (branche · cultuur · betrokkenheid; each ✓ when filled), 4 Eerste vacature klaarzetten, 5 Collega uitnodigen. Each links to the right page via `EmployerLinks`. It disappears when all are done and the company is verified.
- Visibility panel "Wat ziet een kandidaat?": rows Bedrijfspagina, Vacatures op de kaart, Matches, Zoekmachines, each with "Nog verborgen" / "Zichtbaar", derived **only** from `PublicVisibility` (02) so it can never disagree with reality. Plus the locked items: "Sollicitaties bekijken", "Tokens kopen", "Kandidaatinzichten": "Na verificatie".
- The vacancy list shows "Klaar · gaat live na verificatie" (03.4) and the locked nav items show a small lock + tooltip, not hidden.

## 11.4 Code entry from the dashboard
- The banner/checklist includes the letter code field (same component and endpoint as `/register/verifieren/brief`, 06.4) when a letter is underway, with the attempts left and "Nog niets ontvangen?" from day 7.

## 11.5 Suggested new KvK vestigingen (D7)
- A weekly job (reuse the intermediair stack's KvK refresh pattern or `KvkVerificationRetryHostedService`'s; keep within the KvK budget from 04.3) compares the vestigingen of each verified root that registered as "heel bedrijf" with KVK. New, free vestigingen become a Te doen item "Nieuwe vestiging bij KVK: {adres}. Toevoegen?" with **Toevoegen** (the normal add-vestiging path; it inherits `Verified`/`InheritedFromOrganization`) and **Niet nu** (hidden for 90 days; table `DismissedVestigingSuggestion` (company id, KvK vestigingsnummer, until), migration `AddDismissedVestigingSuggestions`). Never added automatically.

## 11.6 Docs
- `docs/ROUTES.md`: every route/endpoint from README §IA (with the auth, render mode and SEO columns).
- `CHANGELOG.md`: one entry for the stack (user-facing summary, nl).
- `docs/werkgever-aanmelding.md` (new, short): the flow, the verification methods, the gates, the letter settings (Pingen, cap), the jobs (reminders, cleanup, escalation, suggestions), the admin queue, and how to test with the stubs.
- `docs/werkgever-aanmelding-followups.md`: collect every follow-up line from 01–10 (dependency fallbacks, SBB, native translation review, intermediair 04b), each with the file that created it.
- `docs/i18n/werkgever-aanmelding-review.md`: all new keys (`Wa.*`, `WaVerify.*`, `WaAccess.*`, `WaProfile.*`, `WaEngage.*`, `WaBanner.*`, `AdminWa.*`) with nl + the 4 translations, flagged "machine translation, native review needed" for pl/ro/ar (en reviewed by Dennis).

## 11.7 Privacy text (copy only)
- `Pages/Legal/Privacy.razor` (and its localized strings): a short section "Werkgevers die zich aanmelden" covering: the KVK data we fetch (public register data, cached ≤ 24 h), the contact e-mail and the confirmation code, the verification letter (Pingen as processor, the KvK address, no letter content kept beyond the audit), the access requests (shared with the company's managers), the engagement claims (public, with labels), the reminders and the deletion of unverified registrations after 60 days. Link the processor list if one exists. Add Pingen to it (name, purpose "versturen verificatiebrief", location EU/CH, per their DPA) and mark it "check with Dennis before release" in the PR.

## 11.8 End-to-end (Playwright, stubs only)
- Desktop 1440 + mobile 390, nl:
  1. Employer, business e-mail match: `/register` → search "groen" → heel bedrijf (untick one) → account (password) → code → MFA → step 4 (all three) → **verified at once** (D15) → dashboard without banner → the company page and a published vacancy are public.
  2. Employer, free-mail + letter: … → unverified → create a vacancy → "Klaarzetten" → banner + checklist + panel "Nog verborgen" → the company page 404 anonymously → request letter (stub) → enter the code in the dashboard → the success banner → the vacancy is on the map within ≤ 60 s.
  3. Already registered: search an in-use company → "Vraag toegang aan" → code → the bedrijfsmanager approves in the inbox → the requester signs in with access.
  4. Bureau (SBI 78): register → verify by e-mail → public → publish attempt → 409 `lender_registration_pending` → admin confirms → publish works.
  5. Gates: an unverified manager calling the token checkout and applicants API directly → 403 `company_unverified`.
- Axe (or the existing a11y helper) on every wizard step and the banner: no serious violations. Screenshots attached to the PR.

## Tests
- Banner: shown per state for the right roles only; never for verified companies or candidates; the success banner once; dismiss per session.
- Panel rows equal `PublicVisibility` for the same company (property test over the 4 statuses).
- Checklist completion rules; `EmployerLinks` targets both route sets (C Present/Absent).
- Suggestions job: a new free vestiging → one Te doen item; "Niet nu" hides it 90 days; never auto-added; the KvK budget respected.
- The E2E scenarios above; `LocalizationParityReportTests` + `LocalizationTests` green.

## Success criteria
- Every unverified employer page says clearly why candidates can't see them and how to fix it, and the panel can't contradict the real visibility. The five E2E scenarios pass on desktop and mobile.

Done → end of stack. Report the table file → branch → PR number → status (green or draft/red), the Dependencies outcome (A–G, and whether A, B, C, D or G flipped at a re-check point), the backfill counts from 02 (companies/vacancies public before = after), whether the SBB check was built (09.6), and anything deferred (native review of PL/RO/AR, the Pingen DPA/privacy check, intermediair 04b leftovers, the landing/salesmanager/redesign follow-ups).
