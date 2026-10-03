# 08: Pricing page `/voor-partners` + partner subscription plans (config, no tokens, pilot)

**Stacked.**
- Branch: `cursor/paspoort-partners-8` from `cursor/paspoort-partners-7`.
- ONE PR into `acceptatie`, titled **"feat(partners): pricing page and partner subscription plans"**.
- Rules: see README.
- Gated by `PassportPartnersEnabled`. With the flag OFF, `/voor-partners` returns 404 and is not in the sitemap.

Mockup: `docs/mockups/paspoort-partners/h-prijzen-partners.png` (src `src/h.html`).
- Deviations: the route is `/voor-partners` (section anchor `#prijzen`), not the mockup URL.
- All amounts are **examples from config**, never literals in Razor/C#.
- The Pro bullet "later: kandidaten zien vacatures van hun partner en kiezen zelf" is **text only**.

## Goal
Decisions 14–16. One public page explains the offer to agencies **and** employers: the same plans for both, with copy switched by type. Admins can activate a partner subscription (incl. pilot) that drives `PassportPartner.IsActive`.

There is no online payment and no tokens. **Decided (decision 22): invoicing is manual for now.**

**Plans (indicative, admin-editable):**
| Plan | What the partner gets |
|---|---|
| **Gratis** | Viewing via the candidate's link/QR + authenticity check (05). No code, no logo, no overview. |
| **Partner** | €99–€249 per branch per month. Own code + QR + flyers, co-branded passport, partner portal, logo, branches. |
| **Pro** | From €500 per month, priced on branches and volume. Everything in Partner + more branches/volume + priority support. "Later: kandidaten zien vacatures van hun eigen partner en kiezen zelf" (text only; legal AI-Act check before building). |

**Volume:**
- Bands are on "actieve gedeelde paspoorten" (consented, not revoked links) in broad bands: up to 50 / 150 / 300 / more.
- Never per candidate.
- If Dennis prefers, the bands can be switched off (per-branch pricing only) via the config flag `UseVolumeBands`.

**Pilot:**
- the first 5 partners (Westland), 3 months Partner free
- then 50% discount until 2027-07-01
- in exchange for 2 feedback conversations + a usable quote
- requires a written pilot agreement + data-sharing agreement beforehand: an admin checkbox with date, required before activation
- Decision 22: the partner terms, consent texts and pilot data-sharing agreement need a **legal check before `PassportPartnersEnabled` goes live in production**. The code only stores the agreement dates; it does not block on this.

## Facts
- Existing commercial singleton: `FlexCommercialSettings` (Flex margin, deep analysis, `DefaultAgencyAnnualPriceEuro = 4000`, contact unlock tokens) with admin page `/admin/financien/prijzen`. **Do not change its values or behaviour** (out of scope). Add a separate section/tab.
- `AgencyAnnualSubscription` exists for the €4,000 agency plan. **Decided (decision 22): it stays separate and untouched.** Do not reuse, bundle or reference it.
- Rate limit `public-write` exists (Api). Lead e-mails go via `IEmailService` + template registry.
- Public page style: see `HowLobsyWorks.razor` / `LandingFaq.razor` components.

## Data (migration `AddPassportPartnerPlans`)
**`PassportPartnerPlanSettings`** (singleton, seeded with these defaults):
- `PartnerPricePerBranchMinEuro = 99`, `PartnerPricePerBranchMaxEuro = 249`
- `ProFromEuro = 500`
- `UseVolumeBands = true`, `VolumeBand1 = 50`, `VolumeBand2 = 150`, `VolumeBand3 = 300`
- `PilotEnabled = true`, `PilotMaxPartners = 5`, `PilotFreeMonths = 3`, `PilotDiscountPercent = 50`, `PilotDiscountUntil = 2027-07-01`
- `ShowAmountsAsIndicative = true` (footnote "Voorbeeldbedragen, excl. btw; definitieve prijs in overleg")
- `UpdatedAtUtc`, `UpdatedByUserId`
- Note: `PilotDiscountPercent` is a commercial discount, not a candidate score; the AI-Act DTO guard (07) only covers portal DTOs.

**`PassportPartnerSubscription`:**
- `Id`, `PassportPartnerId`
- `Plan`: `Partner | Pro`
- `Branches`, `VolumeBand` (null when bands are off)
- `AgreedMonthlyEuro` (decimal, admin-entered)
- `IsPilot`, `PilotAgreementSignedAtUtc`, `DataSharingAgreementSignedAtUtc`
- `StartsAtUtc`, `EndsAtUtc` (null), `CancelledAtUtc`, `Notes` (500)
- Rules:
  - at most one active subscription per partner
  - pilot slots: count of `IsPilot` subscriptions ever activated ≤ `PilotMaxPartners`
  - activation requires both agreement dates when `IsPilot`

**`PassportPartnerLead`:**
- `Id`, `CompanyName` (120), `ContactName` (80), `Email` (254), `Phone` (30, optional)
- `Type` (`Uitzendbureau | Werkgever`), `Branches` (int), `Message` (500)
- `WantsPilot`, `CreatedAtUtc`, `HandledAtUtc`
- Retention: 12 months after creation (`PassportPartnerLeadRetentionJob`; delete).

**Activation logic:** `PassportPartnerSubscriptionService` (daily job + on save) sets `PassportPartner.IsActive`:
- true while a subscription is active (`StartsAtUtc` ≤ now < `EndsAtUtc ?? ∞`, not cancelled)
- false otherwise
- Admin manual override `ForceActive` stays available for demos and is logged in `AdminAuditLog`.

**When a partner becomes inactive:**
- the portal is closed
- codes stop resolving
- existing links are left untouched. `CanPartnerViewAsync` already denies inactive partners, so do **not** reuse `SuspendedAtUtc`, which belongs to reconfirmation. Access returns automatically if the partner reactivates within 90 days.
- the candidate's sharing screen shows the partner as "Niet actief"
- after 90 days inactive, links are revoked with reason `PartnerEnded` and candidates are e-mailed ("{partner} gebruikt Lobsy niet meer; je paspoort is niet meer gedeeld")

## UI
**Public `/voor-partners`** (anonymous, all 5 languages, `ar` rtl-safe; mockup h):
- hero: "Kandidaten die zichzelf kennen, sneller aan het werk"
- type switch "Ik ben een uitzendbureau / werkgever": same plans, copy variant
- pilot banner while slots remain: "Pilot Westland: 3 maanden gratis voor de eerste {n} partners", with n = remaining slots, shown only if > 0 and `PilotEnabled`
- 3 plan cards with amounts from settings (formatted nl-NL currency)
- 5 rules:
  - alleen eigen kandidaten
  - alleen met toestemming
  - alleen het paspoort
  - geen score/ranking/selectie
  - kandidaat kan altijd intrekken
- FAQ (6–8 items incl. AVG, AI Act, cost for candidates = free, what Gratis means)
- footnote "voorbeeldbedragen"
- lead form (rate limit `public-write`, honeypot, consent checkbox for being contacted): saved as a `PassportPartnerLead`, admin notification e-mail `PassportPartnerLeadReceived`, confirmation shown on the page (no e-mail to the lead in v1)

Other wiring:
- Links from the `/p/{code}` footer ("Ook partner worden?") and the employer nav.
- SEO: indexable **only when the flag is ON**; add to the sitemap conditionally; no JSON-LD prices.

**Admin:**
- `/admin/financien/prijzen`: new tab "Paspoortpartners": edit plan settings, with an audit log entry.
- `/admin/paspoortpartners` (03): tab "Abonnement" per partner (create/cancel, pilot checkboxes, remaining pilot slots); tab "Leads" (list, mark handled, delete).

## Tests
- **Unit:**
  - activation logic (start/end/cancel/override)
  - pilot slot limit
  - pilot requires agreements
  - inactive → no partner access while links stay untouched; 90 days → revoked (`PartnerEnded`) + e-mail queued; reactivation within 90 days restores access
  - amounts rendered from settings (change setting → page changes)
  - no literal "€99"/"249"/"500" in Razor/C# outside the seed (source-scan test)
  - lead retention job
- **API:** lead endpoint rate limit + validation; admin-only settings/subscription endpoints (403 for non-admins).
- **bUnit:** type switch keeps plans identical; pilot banner hidden at 0 slots; footnote present.
- **Playwright 390 + 1440** (soft-skip):
  - `/voor-partners` renders, type switch, lead form submit (test env)
  - flag OFF → 404
  - no horizontal overflow
  - `ar` rtl smoke

## Success criteria
- The public page shows config-driven indicative prices for both types, with the pilot banner and lead form.
- Admins can activate/cancel subscriptions (incl. pilot rules), which drive partner activation.
- There are no tokens, no Mollie, and no per-candidate pricing.
- Release build with 0 warnings, tests green.

## Out of scope
- Online payment/Mollie.
- Automatic invoicing.
- Tokens / `TalentContactRequest`.
- Changes to `FlexCommercialSettings` / `AgencyAnnualSubscription` (€4,000).
- Pro matching (text only).
- Lobsy voor teams pricing (concept, not built).
