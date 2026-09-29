# 03. Attribution: 30-day cookie, /p/{code}, typed code wins, external login, self-referral guard, admin reassign, click counters

Read `00-README.md` first (§0, §D, §P, D2, D4, D13). Branch `cursor/salesmanager-3` from `cursor/salesmanager-2`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/salesmanager-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Beneficiary is resolved server-side from the signed-in user, no employer contact/candidate data in any portal DTO, money writes are idempotent (§0, §P, §R).

| | |
|---|---|
| Branch | `cursor/salesmanager-3` (from `cursor/salesmanager-2`) |
| PR title | `feat(sales): attribution — 30-day link cookie, /p short link, self-referral guard, admin reassign, click counters` |
| PR body starts with | `Stacked on #<PR 02> (cursor/salesmanager-2)` |
| Mockups | `sm-d2-link-materiaal.png` (info box "Zo tellen we een aanmelding voor jou"), `sm-d1-dashboard.png` ("Van link naar klant" funnel numbers come from here) |
| Split seam | **03a** = cookie + `/p` + resolution + external login + guard (03.2–03.4). **03b** = reassign API + history + click counters + LegalForm (03.5–03.7) |

## Goal
A referral is counted reliably and fairly: a click on a salesmanager's link is remembered for 30 days, a code typed at registration still wins, the attribution survives Microsoft/Google sign-up, nobody can refer themselves, admin can fix a wrong attribution with a reason, and link visits are counted per day without personal data so the funnel is real.

## 03.1 Today (verify first)
- `/partner/{TrackingCode?}` = `Components/Pages/Partner/PartnerSales.razor` (public; L123 builds `/register?ref={code}`); it serves salesmanager **and** BM/IM partner codes.
- `/werven/{code}` + `/ambassadeur/ref/{code}` = `Components/Pages/Ambassadeur/Landing.razor`: sets the 30-day cookie `lobsy_ambassadeur_ref` (also read in `Login.razor` and `Auth/AuthServiceCollectionExtensions.cs`). Since 01.10 these redirect to `/` without a cookie while the role is parked; leave that gate as it is.
- `Register.razor` prefills the code field from `?ref=`; L782–783 pass `salesManagerTrackingCode` / `partnerTrackingCode` from the same field. `CompanyRegistrationService` (~L1530–1600) resolves SM, then ambassadeur (`ApplyAmbassadeurReferralAsync`, skipped while parked since 01.10) codes.
- No cookie for salesmanager links; attribution is lost when the employer registers later or via external login; no self-referral check; no click tracking.

## 03.2 Cookie + short link
- `SalesReferralCookie` (Web): name `lobsy_sales_ref`, value = normalized code only, `Max-Age = AttributionCookieDays` (setting, default 30), `HttpOnly`, `Secure`, `SameSite=Lax`, path `/`. **First click wins:** don't overwrite an existing cookie that holds a still-valid code. Reading goes through one `SalesReferralCookie.TryRead(HttpContext)`; it ignores `lobsy_ambassadeur_ref` and `AM-` values while the role is parked (01.10) and never writes that cookie. Keep the read behind the gate check rather than deleting it, so re-enabling is one switch.
- Set it on `/partner/{code}` (salesmanager **and** partner codes, D2 default) and `/p/{code}`, only when the code is well-formed **and** belongs to an active beneficiary (onboarding complete / active partner). Invalid codes: page renders as today, no cookie.
- `/p/{code}`: minimal endpoint (no page) → count the click (03.7) → 302 to `/partner/{code}` for SM/BM/IM codes, preserving `?b=` (channel). `AM-` codes: 404 while parked (no click counted), like an unknown code. Rate-limited (`public-write` policy or a new `public-redirect` policy with the same limits).
- Cookie list: add `lobsy_sales_ref` to the cookie documentation the privacy/cookie page reads from (same category as the existing `lobsy_ambassadeur_ref` entry, which stays listed while that cookie can still exist in browsers); don't touch the cookie banner. Flag the category choice for Dennis in the PR.

## 03.3 Resolution at registration
- One `SalesAttributionResolver` (Infrastructure) used by **every** employer registration path (form, KvK flow, external login completion, invite-less org creation). Order: **typed code** (the registration field, if well-formed and active) → **cookie** → none. Record `SalesAttributionSource = TypedCode | LinkCookie` and `SalesAttributedAtUtc` on the root (02.3).
- External login: the Web layer reads the cookie before the challenge and passes it through the auth `state`/properties (or reads it again on callback; the cookie survives the round trip because `SameSite=Lax` allows top-level GET navigations). Test both Entra and Google stubs.
- Partner (BM/IM) codes keep today's partner flow (`ReferredByPartnerUserId`, `PartnerAffiliateService`); the resolver only decides **which code** applies and hands partner codes to the existing path unchanged.
- After a successful registration the cookie is deleted.

## 03.4 Self-referral guard (D2)
- Block attribution (register **without** it, no error for the user) when any is true:
  - the registering user **is** the beneficiary (same user id or same verified e-mail);
  - the company KvK (8 digits) equals the beneficiary profile's `KvkNumber`;
  - the registering e-mail domain equals the beneficiary's e-mail domain **and** that domain is not a freemail domain. Reuse an existing freemail list if one exists (`git grep -n -i "gmail.com" -- Jobsy.Core`); else add `Jobsy.Core/Sales/FreemailDomains.cs` (gmail.com, googlemail.com, hotmail.com, hotmail.nl, outlook.com, outlook.nl, live.com, live.nl, msn.com, icloud.com, me.com, yahoo.com, yahoo.nl, ziggo.nl, kpnmail.nl, kpnplanet.nl, xs4all.nl, planet.nl, home.nl, hetnet.nl, telfort.nl, upcmail.nl, casema.nl, chello.nl, proton.me, protonmail.com).
- Write `PlatformLog` `sales.attribution.self-referral-blocked` (code, rule, company id; no e-mail). Admin can still reassign (03.5).

## 03.5 Admin reassign (API + history)
- `POST api/admin/sales/attribution/{companyId}` (`RequireAdmin` + MFA session): `{ toBeneficiaryUserId | null, reason (5–500) }`. Works on the **root** (a vestiging id resolves to its root). Sets the new beneficiary on the root and every vestiging, `SalesAttributionSource = Admin`, writes `SalesAttributionChange`, audits `sales.attribution.reassign`.
- Effects: only **future** purchases (D2 default). If the unit is not yet activated, the snapshots at activation use the new beneficiary's rates; if it is activated, the window and rates stay, the new beneficiary gets future lines at the snapshotted rates, and the indirect beneficiary is recomputed from the new salesmanager's `ReferredBySalesManagerUserId` (write it into the snapshot).
- `GET api/admin/sales/attribution/{companyId}/history`. UI in 08.

## 03.6 LegalForm (for D4)
- If the KvK lookup (`KvkHandelsregisterService`) response contains the legal form (`rechtsvorm` / `uitgebreideRechtsvorm`), map it to `CompanyLegalForm` at registration and on KvK re-verification (`KvkVerificationRetryHostedService`). If the client doesn't expose it, add the field to its DTO when the API returns it; if the API doesn't return it at all, leave `LegalForm` null and say so in the PR (the portal then shows name only, D4).

## 03.7 Click counters (funnel)
- On `/p/{code}` and `/partner/{code}` (valid active codes only): increment `SalesLinkClickDaily(beneficiary, SalesClock.Today(), channel)` with an upsert. Channel from `?b=` (`qr`, `flyer`, `link`, else `Other`); the QR and flyer URLs built in 05 carry `?b=qr` / `?b=flyer`.
- Not counted: requests with a bot user agent (reuse an existing bot check if the repo has one, else a small `BotUserAgents` list), `HEAD` requests, a second visit from the same browser on the same day (skip when the attribution cookie already holds this code and was set today; store nothing extra).
- **Stored:** beneficiary, date, channel, count. **Never stored:** IP, user agent, referrer, cookie id.
- Retention: delete rows older than 25 months in `DataRetentionHostedService` (§P).
- `ISalesFunnelReadService.GetAsync(beneficiary, period)` → `Visits`, `Registered` (attributed roots, `SalesAttributedAtUtc` in period), `FirstPurchase` (activated in period), `ActiveNow` (a credited purchase in the last 90 days). Used by 04.

## Tests
- Cookie: set on valid active code, not on invalid/inactive; first click wins; typed code beats cookie; cookie deleted after registration; with `AmbassadorsEnabled` off an `AM-` code (typed, `/p/AM-…` or an old `lobsy_ambassadeur_ref` cookie) gives no attribution, no cookie and no click.
- `/p/{code}` 302 targets per code type; `?b=` preserved; rate limit.
- External login (Entra + Google stubs) keeps the attribution.
- Partner BM/IM codes: registration result identical to before (regression test using the existing partner tests).
- Self-referral: each rule blocks; freemail domains don't trigger the domain rule; log row written.
- Reassign: root + vestigingen updated, history row, audit, MFA session required, future purchases only (commission of an earlier purchase unchanged), indirect beneficiary recomputed.
- Click counters: upsert per day/channel, bot and same-day repeat skipped, nothing personal stored (reflection test on the entity: exactly the four columns), retention deletes old rows.

## Success criteria
- `dotnet build` + `dotnet test` green.
- An employer who clicks `/p/SM-K7Q2MP?b=qr`, leaves, and registers 10 days later with Google is attributed to SM-K7Q2MP with source "Via link"; the click shows as one QR visit on that day.
- A salesmanager registering their own company with their own code gets no attribution.

Done → next: `04-dashboard-werkgevers.md`.
