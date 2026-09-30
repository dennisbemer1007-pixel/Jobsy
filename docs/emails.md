# Transactional e-mails — how to add or change a mail

Stack files `01`–`09` under `docs/emails/` (branch `docs/emails`). This page is the operator guide for day-to-day changes.

## Anatomy

1. **Registry** — `EmailTemplateRegistry` (`Jobsy.Core/Email/EmailTemplateRegistry.cs`): key, category, audience, `EmailKind` (Essential / Optional / Security), reason key, mascot (`GoodNews`), parked flag, `RequiresEmployers`.
2. **Composer** — `TransactionalEmails` partials: build an `EmailDocument` (heading, blocks, optional CTA or OTP, culture). Always finish with `Finish(doc, baseUrl)` so `EmailRenderer` + `EmailBrand` apply.
3. **Strings** — `EmailStrings` (nl/en/pl/ro/ar). New keys need all five languages; track native review in `docs/i18n/emails-review.md`. Do not grow `docs/i18n/email-untranslated-baseline.txt` without review.
4. **Links** — only via `EmailLinks` / `JobsyPublicUrl`. Token value in previews: `voorbeeld`. Never put passwords, API keys, or tracking query params in mail.
5. **Send path** — `ITransactionalMailer` adds Reply-To, optional List-Unsubscribe (kind O only), tags; Resend/SMTP via Integraties. Test sends from admin use `[Test] `, `X-Lobsy-Test: 1`, no List-Unsubscribe.

## Admin preview

Route `/admin/content/emails` (Admin). Fake data only (`EmailSampleContext.ForPreview`). Languages × light/dark × 600/375. Test send to the signed-in admin (or `Mail:TestRecipientAllowList`). Daily cap `Mail:TestDailyCap` (default 100). Send-all: 15 min cooldown, ≤ 1 msg/s.

## Snapshots & guards

- Update structural snapshots: `JOBSY_UPDATE_EMAIL_SNAPSHOTS=1 dotnet test --filter EmailSnapshotTests`.
- Unit matrix: `EmailRenderMatrixTests` (every key × 5 languages).
- Playwright renders: `EmailRenderPlaywrightTests` (nl/ar × 600/375 × light/dark) → `artifacts/playwright-email/`.
- Layout/security guards: `EmailLayoutGuardTests`, `NoSecretsInMailsTests`, `TransactionalMailerTests`.

## Deliverability

See `docs/email-deliverability.md` (From `hallo@mail.lobsy.nl`, Reply-To `support@lobsy.nl`, DNS/DKIM/DMARC, no link rewriting / tracking pixels).

## Legal footer

`Mail:LegalName` (default Lobsy), `Mail:LegalAddress`, `Mail:KvkNumber`. Empty address/KvK → name only until Dennis fills Integraties / env.
