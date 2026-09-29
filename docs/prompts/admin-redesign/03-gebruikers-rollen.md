# 03 · Gebruikers & rollen

> Read `00-README.md` first (§0, §IA, D2, D3, D4, D12). Builds on 01/02.

| | |
|---|---|
| Branch | `cursor/admin-redesign-3`, created from `cursor/admin-redesign-2` |
| PR | ONE PR into `acceptatie`, title `feat(admin): user list, detail drawer with sessions and 2FA reset, roles overview`. Body starts with `Stacked on #<PR of 02> (cursor/admin-redesign-2)` |
| Mockups | `ad-d2-gebruikers-2fa.png`, `ad-m2-2fa-resetten.png` |
| Split seam (if too big) | 3a = list + drawer + sessions + 2FA; 3b = Rollen & rechten + Kandidaten + Sales & ambassadeurs tabs |

**Dependency check (README "Dependencies").** Run `git merge-base --is-ancestor c72fa711 HEAD`. It should succeed (the 2FA work is merged at `cb24244d`). If it fails, build the 2FA block behind `AdminCapabilities.MfaResetAvailable = false` and say so in the PR.

## 03.1 Today (verify)
- `Pages/Admin/UsersAdmin.razor` (now at `/admin/gebruikers`): masked list, filters (q, role, companyType, companyId, earlyOnly), 50 per page, `RowActionsMenu` with "Waarom zie ik dit?" (`SupportAccessDialog`) and "2FA resetten" (`LobsyFriendlyDialog`, reason + code for local-password admins), status pills Early / MFA status.
- API `GET api/admin/users` (`AdminController` ~L236): **loads every user row into memory for the aggregates** (`allRows`) → fix in 03.6.
- `POST api/admin/users/{userId}/mfa/reset` (~L555): MFA-verified session or external IdP required, not self, reason 5–500, fresh TOTP for local-password admins, clears secret + recovery codes, bumps `SessionVersion`, `RevokeAllAsync(..."mfa-reset")`, logs `PersonalDataAccessLog` (`user.mfa`, `reset`). **Reuse as-is.**
- Sessions: `IDeviceSessionService.ListAsync/RevokeAsync/RevokeAllAsync(userId, …)` exist; `DeviceSessionsController` is self-only. `UserDeviceSession` stores user agent/device name and `LastUsedAtUtc`, **no IP** (good: don't show IPs; the mockup's masked IP is replaced by device + last used).
- Role changes: `CompanyUsersController.Update` (`PUT api/company-users/{id}`) with `EmployerInviteRules.CanAssignRole` (admin allowed).
- No last-login column on `User`: "Laatst actief" = max `LastUsedAtUtc` over the user's sessions.

## 03.2 List `/admin/gebruikers` (`ad-d2`, left part)
- Header: `h1` "Alle gebruikers", lead "Persoonsgegevens zijn standaard gemaskeerd (AVG). Inzien kan alleen via support-toegang." Actions: "Export (geanonimiseerd)" only if an anonymised export exists (else omit), primary "Gebruiker uitnodigen" only if an admin invite flow exists (sales/ambassadeur invites live in their tabs; don't invent a generic one).
- `AdminTabs` by role group with counts from the aggregates: Alle · Kandidaten · Werkgevers (Enterprise/Regional/Branch/Intermediary) · Sales & partners (SalesManager/Ambassadeur) · Beheerders. URL `?tab=`.
- `AdminFilterBar`: search (name/e-mail/ID, server-side), chips Rol · 2FA (Aan / Niet ingesteld / Via Microsoft/Google) · Status (Actief / Geblokkeerd) · Organisatie (existing `companyId`), "Kolommen" optional (skip if it needs new persistence). Add server params `mfa` and `active` to `GET api/admin/users`.
- `AdminDataTable` columns: checkbox · Gebruiker (initials avatar, masked name 600, masked e-mail muted) · Rol · Organisatie · 2FA (pill with lock icon + text) · Laatst actief · Status (dot + text) · row actions. 25/50/100 per page, `AdminPager` "1–25 van 5.325".
- Row click (or Enter) opens the drawer; `?open=<id>` deep link (used by global search, 01.5).
- **Bulk bar** (`AdminBulkBar`, ≥ 640 only): "Sessies beëindigen" and "Blokkeren" (each opens one confirm dialog with a required reason and runs the per-user endpoint for each id, server-side in one call: `POST api/admin/users/bulk/{action}` with ids ≤ 100; result "4 gelukt · 1 overgeslagen (jezelf)"). "Uitnodiging opnieuw" only if a resend endpoint exists; else omit.
- Mobile (< 640): stacked rows (name, role, 2FA pill), no checkboxes.

## 03.3 Detail drawer (`AdminDrawer`, 460 px; full-screen sheet < 900)
Header: avatar, masked name (`h2`), "{rol} · {organisatie} · {vestiging}", pills (max 2): status + 2FA, plus a muted "ID {kort}". Close ×. Tabs: **Overzicht · Rollen · Beveiliging · Activiteit** (Activiteit = **slot until 07**, not rendered).
- **Overzicht:** kv-list (masked e-mail, masked phone if present, rol, organisatie(s) via memberships, aangemaakt, laatst actief, early adapter yes/no). Links: "Organisatie openen" → `/admin/organisaties?open=<id>`.
- **Rollen:** current role + company memberships (read-only list). "Rol wijzigen" only for employer-side roles, reusing `PUT api/company-users/{id}` (same `CanAssignRole` rule); Admin/SalesManager/Ambassadeur role is not assignable here (no new privilege path). Confirm dialog with required reason (the reason is stored from 07 on; before 07 it's sent but only logged at info level).
- **Beveiliging** (`ad-d2` right):
  - **Tweestapsverificatie** box: "Aan · authenticator-app · ingesteld op {datum}" / "Niet ingesteld" / "Via Microsoft/Google (2FA bij je IdP)". Button **"2FA resetten"** (outline danger) only when enrolled and not yourself. Help text under it: "Na een reset stelt de gebruiker bij de volgende login opnieuw 2FA in. Je geeft een reden op en bevestigt met je eigen 2FA-code. De actie komt in het auditlog en de gebruiker krijgt een e-mail."
    - The **confirm dialog** replaces the current inline `LobsyFriendlyDialog` block and is one component `MfaResetDialog.razor` used by the drawer **and** the row menu (move the existing logic from `UsersAdmin.razor`, don't copy it). Layout as `ad-m2`: title "2FA resetten voor {gemaskeerde naam}?", lead "De authenticator wordt ontkoppeld en alle {n} sessies worden beëindigd. Bij de volgende login stelt de gebruiker 2FA opnieuw in.", **Reden (verplicht)** textarea (5–500, counter), **Jouw 2FA-code** (6 separate digit boxes, `inputmode="numeric"`, `autocomplete="one-time-code"`, paste fills all) only for local-password admins (external: a muted line "Bevestigd via je Microsoft/Google-sessie"), note box "Komt in het auditlog met jouw naam en reden. {naam} krijgt hierover een e-mail.", buttons Annuleren · **2FA resetten** (filled danger, disabled until valid). Errors from the API are shown inline (403 "Bevestig eerst je authenticator." etc.).
    - **User e-mail:** the copy promises an e-mail. If the reset endpoint doesn't send one yet, add a transactional template `MfaResetByAdmin` ("Je tweestapsverificatie is gereset door Lobsy-support. Log opnieuw in om 2FA in te stellen. Was jij dit niet? Neem contact op.") sent from the endpoint after success, **no admin name, no reason** in the mail. Register it in `TransactionalEmails` + mail-test catalog.
  - **Actieve sessies** list: device/browser, "nu actief" or last used, "Beëindigen" per row; header action "Alle sessies beëindigen". New endpoints in `AdminController` (`RequireAdmin`): `GET api/admin/users/{id}/sessions`, `POST api/admin/users/{id}/sessions/{sessionId}/revoke`, `POST api/admin/users/{id}/sessions/revoke-all` (body `{ reason }` required 5–500). They call `IDeviceSessionService` (reason prefix `admin:`), refuse your **own current** session, and log `PersonalDataAccessLog` (`user.sessions`, `list` / `revoke`).
  - **Persoonsgegevens** box: masked kv + note "Gemaskeerd volgens AVG. Volledige gegevens nodig voor support? Vraag tijdelijke toegang aan (15 min, met reden). Dit wordt gelogd en de gebruiker krijgt bericht." Button "Support-toegang aanvragen" opens the existing `SupportAccessDialog` with **15 min pre-selected** (D4: change `SupportAccessService.DefaultDurationMinutes` to 15 and the dialog's default; keep 60/240 selectable). With an active grant the box shows unmasked values + "Nog {m} min · Nu intrekken" (existing revoke).
  - Footer: "Account blokkeren" (outline danger) · "Wachtwoordlink sturen" (only if a reset-link endpoint exists for local accounts; else omit) · "Sluiten".
- **Blokkeren / deblokkeren:** new `POST api/admin/users/{id}/block` and `/unblock` (reason required). Block = `IsActive = false` + `RevokeAllAsync(bumpSessionVersion: true)`. Refuse self and refuse blocking the **last active admin**. Log `PersonalDataAccessLog` (`user.status`, `block`/`unblock`).

## 03.4 Rollen & rechten `/admin/gebruikers/rollen`
- Read-only overview, no new permissions model: one row per `UserRole` with NL name, count (aggregates), "2FA verplicht" (from `MfaPolicy.IsRequired`), a one-line description (new strings), and "Wat mag deze rol?" linking to the section in `docs/security/roles-matrix.md` rendered as a small static table (or the help panel). Flip `IsAvailable`.

## 03.5 Kandidaten `/admin/kandidaten` and Sales & ambassadeurs tabs
- `/admin/kandidaten`: the same list component with `tab=kandidaten` preset and a candidate column set (Laatst actief, tests gedaan if the users DTO already has it, else omit). Breadcrumb "Kandidaten & tests › Kandidaten". Flip `IsAvailable`.
- `/admin/gebruikers/sales`: the tab host from 01 (Salesmanagers · Ambassadeurs). Add the enterprise header/lead, cross-link "Uitbetalingen staan bij Financiën › Uitbetalingen & btw" (D2). No functional changes.

## 03.6 Performance fix in `GET api/admin/users`
- Replace the in-memory `allRows` aggregates with SQL `GroupBy`/`CountAsync` (by role, active/inactive, top 20 companies, 12 week buckets). Same DTO, same numbers. Add a test with a fixed data set that the aggregates are identical to the old algorithm.

## Tests
- API: sessions list/revoke/revoke-all (403 non-admin, own current session refused, reason required, access-log rows); block/unblock (self refused, last-admin refused, sessions revoked); bulk endpoint (≤ 100 ids, per-id result, skips self); `mfa`/`active` filters; aggregates parity.
- MFA reset: existing `MfaForcedEnrollmentTests` stay green; add: the new e-mail is sent once after success with no admin name/reason; not sent on failure.
- `SupportAccessService` default 15; 60/240 still accepted.
- bUnit: list tabs + counts, filter chips → query, bulk bar appears on selection, drawer tabs, `MfaResetDialog` (button disabled until reason ≥ 5 and 6 digits; external admin sees no code field; paste fills boxes), Persoonsgegevens masked → unmasked with an active grant.
- Playwright `AdminUsersPlaywrightTests`: open `/admin/gebruikers?open=<seed user>` → drawer Beveiliging tab visible at 1440; at 390 the drawer is a full-screen sheet and the reset dialog buttons are ≥ 44 px.

## Success criteria
- List, filters, tabs, bulk actions and the drawer work with masked data; `?open=` deep links work.
- 2FA reset goes through one dialog component and the existing endpoint; reason + own code (local) enforced server-side; user gets the e-mail.
- Admin can see and end a user's sessions; blocking ends sessions and can't lock out the last admin.
- Support access defaults to 15 min.
- `GET api/admin/users` no longer loads all users into memory.
- Build + tests green; PR body complete.

## Done → next
Push, open the PR, note its number. Continue with **`04-organisaties.md`**.
