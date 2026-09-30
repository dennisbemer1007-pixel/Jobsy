# 10. Intermediair: same wizard and verification, plus the uitleenregistratie (Waadi) check

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-10` from `cursor/werkgever-aanmelding-9`.

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Re-check README Dependencies **D** (uitleenregistratie service) and **G** (intermediair data model) first and say in the PR which case applied. Never build a second Waadi/uitleen service next to `ILenderRegistrationCheck`, and never scrape the KvK Waadi page.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-10` |
| PR title | `feat(register): intermediaries register through the new wizard and verification; uitleenregistratie check via ILenderRegistrationCheck` |
| PR body starts with | `Stacked on #<PR 09> (cursor/werkgever-aanmelding-9)`, then the D and G cases |
| Mockups | none of its own: the wr-d1…d9 screens with intermediair copy (see 10.3) |
| Split seam | **10a** = wizard + verification path (10.2, 10.3). **10b** = the uitleenregistratie service/gate/admin (10.4, 10.5) only in the D "Only in the intermediair spec" case |

## Goal
An uitzendbureau (SBI 78) registers exactly like an employer: same search, account, e-mail/letter verification and the same "invisible until verified" rule. On top of that, it can only **publish** once Lobsy has confirmed its uitleenregistratie (Waadi today, Wtta later), through the one shared service the intermediair stack defines.

## 10.1 Today (verify first)
- `CompanyRegistrationService` + `KvkSbiClassification` (tests `KvkSbiClassificationTests`): SBI 78 forces `RegistrationScope.BranchOnly` and `UserRole.Intermediary`. After 05 the wizard still routes SBI 78 through that path.
- `MfaPolicy.IsRequired` includes `Intermediary`.
- Dependencies D: `git grep -n "interface ILenderRegistrationCheck" origin/acceptatie -- Jobsy.Core`, and read `origin/docs/intermediair:docs/prompts/intermediair/04-kvk-verversing-uitleenregistratie.md` §04.5–04.7 + its README §D (`LenderRegistration`) and D23.
- Dependencies G: `git grep -n "class IntermediaryClient\b" origin/acceptatie -- Jobsy.Core/Entities`.

## 10.2 Wizard path for SBI 78
- Step 1 (search) is identical. When the selected company's SBI codes classify as intermediair: step 2 shows one card **"Wij zijn een uitzend- of detacheringsbureau"** (`Wa.Intermediary.*`) with the vestigingen list (the user ticks the bureau vestiging(en) they manage; the first ticked one is the primary) instead of the two scope cards. The role is `Intermediary` (today's rule), and there's no "heel bedrijf" bedrijfsmanager scope for bureaus in this stack. Extra bureau vestigingen are added later under Organisatie › Vestigingen (intermediair D17).
- A company with SBI 78 **and** other SBI codes: ask "Hoe gebruik je Lobsy?" with two choices, **als werkgever** (normal path) or **als intermediair**. Default to intermediair only when 78 is the main SBI activity.
- Step 3 (account) and the code step are identical, including Microsoft/Google, the referral resolver and MFA setup (`MfaPolicy` requires it for Intermediary).
- Step 4 (Over je bedrijf, 08/09) is offered too, with bureau copy ("Zo werken wij als bureau"); these are the bureau's own profile, never an opdrachtgever's.

## 10.3 Verification (same as employers)
- Ownership verification is exactly 06: business e-mail with a domain match (D15 instant verification applies), or the letter, or the manual check. `MarkVerifiedAsync` → the bureau's company becomes public (02) per the same rule.
- Copy differences only (keys `Wa.Intermediary.*`, 5 languages): "Verifieer je bureau", and on the done screen and banner (11): "Na verificatie controleren we ook je uitleenregistratie (Waadi). Tot die tijd kun je opdrachtgevers toevoegen en vacatures als concept opslaan."

## 10.4 Uitleenregistratie check (Dependencies D)
- **In code on acceptatie:** keep its single registration call site (`StartForNewBureauAsync` after an SBI-78 activation). If 05 moved activation code, make sure the call still happens exactly once per new bureau from the new wizard (move it, don't duplicate it). Nothing else to build.
- **Only in the intermediair spec (expected):** implement intermediair **04.5** exactly as written there (`Jobsy.Core/Interfaces/ILenderRegistrationCheck.cs`, `ILenderRegistrationProvider`, `WaadiKvkProvider`, `WttaNauProvider` disabled, `AdminManual`, entity `LenderRegistration` with history rows, the one-shot backfill giving existing bureaus `NotChecked` without blocking already-live vacancies) and **04.6** (the publish gate for vacancies with `IntermediaryCompanyId != null`: 409 `lender_registration_pending` before any token spend; drafts allowed). Copy names, paths, statuses and error codes from that file; don't rename anything. Add to `docs/werkgever-aanmelding-followups.md`: "intermediair 04b: `ILenderRegistrationCheck`, providers, entity and the gate exist (werkgever-aanmelding 10); only add the admin tab on `/admin/intermediairs` and move the Waadi decision there."
- **Neither:** the README fallback shape under the same name.
- Order of gates for a bureau vacancy: company not verified → 403 `company_unverified` (03); verified but uitleenregistratie not `Verified` → 409 `lender_registration_pending`. Both before any token spend (test).
- The 60-day cleanup (03.6) is about ownership verification only; a verified bureau waiting for the Waadi decision is never deleted.

## 10.5 Admin
- `/admin/werkgeververificatie` tab **Waadi**: if the intermediair stack's `/admin/intermediairs` "Uitleenregistratie" tab exists, this tab is only a count + a link to it. Otherwise it lists bureaus with `Pending`/`NotChecked` state: bureau, KvK number, the KvK Waadi-check deep link (from `WaadiKvkProvider`), fields Referentie, Bron (Waadi via KvK / Wtta / Handmatig), Geldig tot (optional), Notitie, and **Bevestigen** / **Afwijzen** (note required) → `RecordDecisionAsync`. Audited (dependency E); an in-app notification to the bureau users.
- Heuristic flag (README §A) for the ownership queue: an SBI-78 registration with a free-mail contact address.

## Tests
- Wizard: SBI 78 only → the intermediair card, role `Intermediary`, MFA setup required; SBI 78 + others → the choice, default by the main activity; "als werkgever" → the normal scope cards.
- Verification: the same e-mail/letter/manual tests pass for a bureau (parameterize the 06 tests by role).
- Uitleenregistratie: `StartForNewBureauAsync` called exactly once per new bureau from the new wizard and never for employers; gate order (403 before 409); no token spend on either; drafts allowed; admin decision + audit + notification; `ValidUntil` expiry → not publishable (only if implemented here).
- G Present: registration never reads or writes `IntermediaryClient` rows (guard test).
- `KvkSbiClassificationTests` stay green.

## Success criteria
- A new bureau can register, verify, add opdrachtgevers and save drafts through the same wizard, is invisible until verified, and can publish only after the uitleenregistratie is confirmed, with exactly one uitleen service in the codebase.

Done → next: `11-dashboard-banner-afronding.md`.
