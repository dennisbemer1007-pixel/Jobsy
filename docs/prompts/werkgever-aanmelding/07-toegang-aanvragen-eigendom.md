# 07. Already on Lobsy: access requests, ownership transfer, claiming companies without users

Read `00-README.md` first. Branch `cursor/werkgever-aanmelding-7` from `cursor/werkgever-aanmelding-6` (or `-6b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/werkgever-aanmelding-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show who manages a company (names, e-mails, phone numbers) to the requester or on any public/anonymous response.

| | |
|---|---|
| Branch | `cursor/werkgever-aanmelding-7` |
| PR title | `feat(companies): access requests to the bedrijfsmanager with admin escalation; ownership transfer needs a letter + admin; claim companies without users; intermediary links survive` |
| PR body starts with | `Stacked on #<PR 06> (cursor/werkgever-aanmelding-6)` |
| Mockups | `wr-d10` (access request), `wr-m2` (in-use row); admin tab Toegang follows the 06 queue |
| Split seam | **07a** = access requests + inbox + escalation (07.2–07.4). **07b** = ownership transfer + claim + intermediary guard (07.5–07.7) |

## Goal
Someone whose company is already on Lobsy can't become a second, unknown owner. They ask the people who manage it, and an admin steps in when nobody answers. Real ownership changes are rare and need strong proof.

## 07.1 Today (verify first)
- `EstablishmentTakeoverRequest` entity, `CompanyRegistrationService.ApproveTakeoverAsync` (~L622), inbox `/employer/takeovers` (`Pages/Employer/Takeovers.razor`; `RoleNavCatalog`, `EnterpriseNavItems`, `EmailLayout` link ~L86). Under werkgever-redesign it's `/werkgever/overnames` (dependency C: use `EmployerLinks`).
- `ApproveTakeoverAsync` → `RevokePriorEmployerAccessAsync` (~L1117) removes every **employer** user's membership in the taken-over companies. `JobsyRoles.IsEmployer` **includes `Intermediary`**, and `CompaniesController.RegisterIntermediaryClientFromKvk` (~L171) gives the intermediary a `UserCompany` membership on the client company, so today a takeover silently cuts the intermediary off.
- `CompanyUsersController` (invites/memberships) is the normal way to add someone to a company.
- The wizard (05.4/05.8) links here: `/register/toegang?kvk=&vestiging=`.

## 07.2 Access request (wr-d10)
- Entity `CompanyAccessRequest` (migration `AddCompanyAccessRequests`): target company id (root or vestiging), KvK number, requested vestiging ids, requested role (`BranchManager`/Vestigingsmanager, `RegionalManager`/Regiomanager, `EnterpriseManager`/Bedrijfsmanager), requester name, function, e-mail, phone?, message (≤ 500), e-mail confirmation code hash + `EmailConfirmedAtUtc`, status (`AwaitingEmail`, `Open`, `Escalated`, `Approved`, `Rejected`, `Expired`, `Withdrawn`), `DecidedBy`, `DecidedAtUtc`, `DecisionReason`, `ReminderSentAtUtc`, `EscalatedAtUtc`.
- `/register/toegang` (public theme, noindex): shows the company name + the chosen vestiging(en) (public KVK data only) and "Deze vestiging wordt al beheerd op Lobsy". Form: name, function, e-mail, phone (optional), requested role, message. The 6-digit code confirms the e-mail (same code rules as 05). Rate limit `access-request` (README §A); max 1 open request per e-mail + company.
- `POST api/access-requests` (anonymous), `POST api/access-requests/{id}/confirm`, `POST …/{id}/withdraw` (requester, via a signed link in the mail).
- After confirmation: status `Open`; e-mail + in-app notification to every bedrijfsmanager of the root (fallback: the managers of the requested vestiging). The requester gets "Je aanvraag is verstuurd. {bedrijfsnaam} beslist; na 5 werkdagen kijkt Lobsy mee." No manager names are shown.

## 07.3 Inbox and decision
- Extend the takeover inbox into **"Toegangsverzoeken"** (same page, two sections: Toegang / Overname), reached through `EmployerLinks` (dependency C). Each row: requester, function, e-mail, requested role + vestigingen, message, age. Actions: **Toegang geven** (the manager may lower the role or narrow the vestigingen; not raise above their own rights), **Afwijzen** (optional reason).
- Approve → create the user (if new) with the role and memberships through the same code path as a `CompanyUsersController` invite. The requester gets an invite e-mail to set up their login (Microsoft/Google/password, MFA per `MfaPolicy`). Reject → an e-mail with the reason, if given.
- Requests count as a Te doen item in the employer dashboard (11 shows them).

## 07.4 Reminder and escalation (D8)
- `AccessRequestEscalationHostedService` (daily 09:00 Europe/Amsterdam): day **3** (working days, Mon–Fri, no holiday calendar) → a reminder to the bedrijfsmanagers; working day **5** → status `Escalated`, it appears in `/admin/werkgeververificatie` tab **Toegang**. Admin can approve (same path, audited), reject, or contact the company (show the managers' contact details to the **admin** only). After 30 days without a decision → `Expired` + an e-mail to the requester.
- `WorkingDays.AddWorkingDays(DateOnly, int)` in `Jobsy.Core/Rules` (Mon–Fri) with tests; 03/06 reuse it.

## 07.5 Ownership transfer (D8)
- "Ik ben de eigenaar en er beheert iemand anders" (05.8) → the requester registers normally (05) but the company can't be taken over by that path. They get a transfer request (reuse `EstablishmentTakeoverRequest` with a new `Kind = OwnershipTransfer` or a flag) that requires **both**:
  1. a **letter verification** (06.4) to the KvK address of the company, with the code entered by the requester, and
  2. **admin approval** in the queue (tab Toegang, "Eigendomsoverdracht"), with the current managers notified by e-mail when the letter is requested ("Er is een eigendomsoverdracht aangevraagd voor {bedrijf}. Klopt dit niet? Reageer binnen 7 dagen.").
- Only then `ApproveTakeoverAsync` runs (which revokes the prior access via `RevokePriorEmployerAccessAsync`), audited. The e-mail domain (D14) alone is never enough for a transfer.
- Remove the old direct employer-to-employer takeover start from the wizard. Pending old requests stay decidable in the inbox (don't delete data).

## 07.6 Claiming companies without users (D8)
- (With Dependencies G Present, client links aren't companies, so this concerns only companies whose managers are all deactivated or legacy shells.) A company with **no active employer members other than intermediaries** (e.g. created by an intermediair as a client, or all managers deactivated) can be claimed through the normal wizard. The registrant becomes the owner **after** verification (06): until then they're `Pending` members and the company's public state doesn't change (a client company that is `Verified` through its intermediary stays visible).
- `IsInUse`/"Al op Lobsy" (04) is false for such companies, so the wizard offers them as free.

## 07.7 Intermediaries keep their client link
- Re-check README Dependencies **G** first. **Present:** clients are `IntermediaryClient` links with no membership, so nothing below applies except a guard test that access requests, claims and takeovers never read or write `IntermediaryClient` rows or intermediary vacancies. **Absent:** do the following.
- `RevokePriorEmployerAccessAsync`: exclude users with `UserRole.Intermediary` (and their memberships) from revocation, both for takeovers and claims. Vacancies with `IntermediaryCompanyId` stay linked. Notify the intermediary: "{bedrijf} beheert nu zelf een account op Lobsy; jullie koppeling blijft bestaan."
- A company verified by its own owner keeps its own method; its `IntermediaryClient` method (02/03) is replaced by the stronger one.

## Tests
- Access request: e-mail code required; rate limit; no PII of managers in any anonymous response (serialize + assert); notifications to the bedrijfsmanagers; approve creates a membership with the (possibly lowered) role via the invite path; a manager can't grant a role above their own; reject mails the reason.
- Escalation with a fake clock: reminder on working day 3, escalation on working day 5 across a weekend, expiry at 30 days. `WorkingDaysTests`.
- Transfer: a request without a letter can't be approved; with the letter but no admin it can't either; with both, the prior access is revoked, **except** intermediaries; current managers are mailed on the letter request.
- Claim: a company with only intermediary members is claimable; ownership only after verification; the intermediary keeps its membership and vacancy links.
- The existing takeover tests stay green or are updated to the new rules (list them).

## Success criteria
- No path lets an unverified newcomer take over a company that has active managers. Access requests always reach a person (bedrijfsmanager or admin). No intermediary loses its client link through a takeover or claim.

Done → next: `08-branche-cultuur-waarden.md`.
