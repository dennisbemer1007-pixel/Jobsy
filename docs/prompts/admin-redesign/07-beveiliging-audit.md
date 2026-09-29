# 07 · Beveiliging & audit

> Read `00-README.md` first (§0, §IA, D3, D4, D11). Builds on 01–06 and **fills the audit slots** left by 02, 03 and 05.

| | |
|---|---|
| Branch | `cursor/admin-redesign-7`, created from `cursor/admin-redesign-6` |
| PR | ONE PR into `acceptatie`, title `feat(admin): admin audit log with writes, security overview, privacy page`. Body starts with `Stacked on #<PR of 06> (cursor/admin-redesign-6)` |
| Mockups | `ad-d5-beveiliging-audit.png`, plus the audit parts of `ad-d1` (Recente beheeracties), `ad-d2` (Activiteit tab), `ad-d4` (meta lines, Wijzigingen panel, "Opslaan en loggen") |
| Split seam (if too big) | 7a = entity + writer + filter + writes + Auditlog page; 7b = 2FA & sessies, Privacy & AVG, slot fills |

## 07.1 Today (verify)
- `PersonalDataAccessLog` (who saw whose personal data; `Resource`, `Action` list/view/export/download/reveal, `Reason`, `SupportAccessGrantId`, `CorrelationId`, `IpHash`), retention `PrivacyConstants.PersonalDataAccessLogRetentionDays` = 730, page `/admin/beveiliging/gegevensinzage` (moved in 01).
- `PlatformLog` (technical, 90 days), page `/admin/beveiliging/systeemlogs`.
- **No record of admin actions** (settings, roles, blocks, token grants, takeovers, mark-paid). The 2FA reset is only in `PersonalDataAccessLog` (`user.mfa`, `reset`).
- `DataRetentionHostedService` purges logs by age.

## 07.2 Data: `AdminAuditEvent` (new table, migration)
- Fields: `Id`, `OccurredAtUtc`, `ActorUserId` (nullable for system), `ActorRole`, `ActorKind` (`admin` / `system` / `self`), `Action` (stable key, see 07.4), `TargetType` (`user`/`company`/`setting`/`vacancy`/`invoice`/`grant`/`export`/…), `TargetId` (string), `TargetLabel` (**masked**, ≤ 200), `Reason` (≤ 500), `DetailsJson` (≤ 4 kB; **non-PII diffs only**, e.g. `{"field":"FreePublishUntil","from":"2026-09-30","to":"2026-10-31"}`), `Result` (`success` / `denied` / `failed`), `CorrelationId`, `IpHash` (same salted hash as the access log; never raw IP). Indexes: `OccurredAtUtc desc`, `(TargetType, TargetId)`, `ActorUserId`, `CorrelationId`.
- **Append-only:** no update/delete endpoints; an EF `SaveChangesInterceptor` throws if an `AdminAuditEvent` entry is `Modified` or `Deleted`. The retention purge uses `ExecuteDeleteAsync` (bypasses tracking) in `DataRetentionHostedService`: `PrivacyConstants.AdminAuditRetentionDays = 2555` (7 years, D11), overridable via `Privacy:AdminAuditRetentionDays` like the access-log setting.

## 07.3 Writing: one writer, one filter
- `Core/Interfaces/IAdminAuditLog` + `Infrastructure/Services/AdminAuditLog` (`WriteAsync(AdminAuditEntry)`; never throws into the request: on DB failure log a warning + Sentry, the business action still succeeds, **except** for the 2FA reset and role changes, where a failed audit write returns 500 before the change is committed, so these two write the audit row in the same `SaveChanges`).
- `[AdminAudit("action.key", TargetType = "user", TargetRouteKey = "userId")]` attribute + global `AdminAuditFilter` (API): after the action runs **as an Admin**, writes one event with `Result` from the status code (2xx success, 401/403 denied, else failed). The action can add `Reason`, `TargetLabel` (masked) and `DetailsJson` through a scoped `IAdminAuditContext`.
- **Reflection test (the guard):** every non-GET action reachable by the Admin role (admin controllers and multi-role controllers like `CompanyUsersController`, `RegistrationController`, `SalesManagersController`, `TokensController`) has `[AdminAudit]` or `[AdminAuditExempt("why")]`. The expected exempt list lives in the test.

## 07.4 Events to write (minimum)
| Action key | Where | Details |
|---|---|---|
| `settings.platform.update` | `SettingsController.UpdatePlatformFeatures` | one event per changed field, from → to |
| `settings.pricing.update` / `settings.pricing.delete` | pricing PUT/DELETE in `SettingsController`, `SalesCommercialController` | entity + id + changed fields (amounts are not PII) |
| `settings.company.update`, `settings.about.update`, `settings.flyer.update`, `settings.integration.update` | `SettingsController` | field names only for credentials (**never** secret values) |
| `user.mfa.reset` | `AdminController.ResetUserMfa` | reason; also on denied (wrong code) with `Result=denied` |
| `user.role.change` | `CompanyUsersController.Update` / invite when caller is Admin | from → to role |
| `user.sessions.revoke`, `user.sessions.revoke-all` | 03 endpoints | count |
| `user.block`, `user.unblock` | 03 endpoints | reason |
| `support-access.grant`, `support-access.revoke` | `AdminController` support-access endpoints | duration, scope, reason |
| `tokens.goodwill.grant` | the endpoint behind `JobsyApiClient.GrantTokensAsync` (used by `GrantTokensDialog`) | amount, company (name not PII) |
| `takeover.approve`, `takeover.reject` | `RegistrationController` when caller is Admin | reason on reject |
| `invoice.mark-paid` | `SalesManagersController.MarkPaid` (`invoices/{invoiceId}/mark-paid`) | invoice number |
| `export.create` | every admin export endpoint (users, purchases, goodwill, audit CSV) | export kind, row count |
| `vacancy.extend`, `vacancy.inactive`, `apikey.deactivate` | `AdminController` | — |
| `privacy.retention.run` (system) | `DataRetentionHostedService` | counts per table |
| `privacy.account.deleted` (self) | `PrivacyController.DeleteAccount` | masked label only ("Kandidaat #{kort-id}") |
| `auth.admin.login-failed` (system) | only if the login path already resolves the account at failure: failed password/2FA attempts for Admin accounts; masked e-mail. If it doesn't, **defer** (no new lookup that could enable enumeration) and say so |

## 07.5 Auditlog `/admin/beveiliging` (`ad-d5`)
- `h1` "Beveiliging & audit", lead "Wie deed wat, wanneer en waarom. Het auditlog kan niet worden aangepast en wordt 7 jaar bewaard." Action "Exporteren (CSV)" (writes `export.create`).
- `AdminTabs` (URL-synced, each tab is also its own sidebar item): **Auditlog** · Gegevensinzage · 2FA & sessies · Privacy & AVG · Systeemlogs.
- **API:** `GET api/admin/audit?from&to&action&actor&result&targetType&targetId&q&page&pageSize` (`RequireAdmin`; `q` matches `TargetLabel`, `CorrelationId`, `Reason`). `GET api/admin/audit/export` (CSV, same filters, max 50k rows).
- Table: Tijd (mono, Europe/Amsterdam) · Door (name of the admin; admins see each other's names, this is not candidate PII) · Actie (pill by category: danger = 2FA/verwijderd/login mislukt, warn = support/sessies, info = instellingen/tokens/rollen, line = export/systeem) · Object · reden (object 600 + muted reason, ellipsis with `title`) · Resultaat (dot + text) · Correlatie (mono, copy button). Row click opens `AdminDrawer` with all fields + `DetailsJson` rendered as a from → to list.
- Right column: **Actieve support-toegang** (existing `GET api/admin/support-access?activeOnly=true`, "Nog {m} min", "Nu intrekken" = existing revoke) · **Privacy & AVG** summary (masking on, retention run time, last run) · **Beheerders** (count with 2FA / total from the users aggregates + `MfaPolicy`; failed logins 24 h only if `auth.admin.login-failed` is built).
- Global search (01.5) gets a 5th group **Correlatie** (exact `CorrelationId` match in audit + access log).

## 07.6 Other tabs
- **Gegevensinzage:** restyle the existing access-log page with `AdminDataTable` + filters (actor, subject, resource, action, period); no data change.
- **2FA & sessies** `/admin/beveiliging/2fa`: per role in `MfaPolicy`: enrolled / not enrolled / via IdP (counts from the users query, server-side); table of privileged users **without** 2FA (masked, "Laatst actief") → opens the 03 drawer; recent `user.mfa.reset` and `support-access.*` events (audit query). Flip `IsAvailable`.
- **Privacy & AVG** `/admin/beveiliging/privacy`: read-only facts: "Persoonsgegevens standaard gemaskeerd: Aan", retention periods from `PrivacyConstants` (+ configured overrides) as a table, last `privacy.retention.run` with counts, recent `privacy.account.deleted`. No request queue (README Scope). Flip `IsAvailable`.
- **Systeemlogs:** restyle `LoggingAdmin` with `AdminDataTable` + level/category filters; no data change.

## 07.7 Fill the slots
- **02 Dashboard:** "Recente beheeracties" card (last 4 events, two columns ≥ 1024), link "Auditlog →".
- **03 Drawer:** "Activiteit" tab = audit events where `TargetType=user` and `TargetId` = this user, plus their `PersonalDataAccessLog` rows as "Ingezien door …" (masked subject is the user themselves; actor names shown).
- **05 Functies:** per-row meta "Laatst gewijzigd door {admin} · {datum}" (latest `settings.platform.update` for that field), right-hand **Wijzigingen** panel (last 10 settings events), save bar button "**Opslaan en loggen**" with a reason field (required for `ConfirmOnChange`/danger entries, optional otherwise), lead appends "Elke wijziging wordt met reden gelogd in het auditlog."
- Flip `IsAvailable` for Auditlog.

## Tests
- Interceptor: modifying/deleting an `AdminAuditEvent` throws; retention purge removes rows older than the configured days and keeps newer ones.
- Filter: success/denied/failed mapping; not written for non-admin callers on multi-role controllers; reflection guard (07.3).
- One test per row of 07.4 that the event is written with the expected action, masked label and no secrets (assert credential values never appear in `DetailsJson`).
- 2FA reset: success writes one `success` event in the same transaction; wrong code writes `denied`; audit failure → 500 and no reset.
- `GET api/admin/audit`: filters, paging, 403 non-admin; export writes `export.create`.
- bUnit: audit table pills with text, drawer from → to list, 05 meta line + Wijzigingen panel, 03 Activiteit tab, dashboard card.
- Playwright: change a setting with a reason → it appears at the top of the Auditlog and in the Wijzigingen panel.

## Success criteria
- Every admin write leaves exactly one audit event (guarded by the reflection test); no PII in clear text, no secrets.
- Auditlog, Gegevensinzage, 2FA & sessies, Privacy & AVG and Systeemlogs are reachable under Beveiliging & audit.
- The slots in the dashboard, user drawer and Functies are filled.
- Build + tests green; PR body complete. This is the last file: report the whole table (file → branch → PR → status) and the deferred list.
