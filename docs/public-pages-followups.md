# Public pages: follow-ups

Small items this stack deliberately deferred.

## Cleanup migration (public-pages 08)
- Drop the `AboutPageSettings` table. The entity is `[Obsolete]` and unused since 08: `/wie-zijn-wij`
  is static text in five languages (D10) and the admin editor, `IAboutPageSettingsService`,
  `GET/PUT api/settings/about` and `GET api/site/about` are gone. Nothing reads or writes the row
  any more, so the drop needs no data migration.
- `AdminAuditKeys.SettingsAboutUpdate` stays as a constant: historical audit rows still carry the
  key. Remove it only when those rows are past retention.

## Founder photo (public-pages 08)
- `/wie-zijn-wij` shows an emoji avatar until `wwwroot/images/about/founder.webp` lands. Add the
  file and flip `AboutAssets.HasFounderPhoto`; `AboutPageTests` keeps flag and file in sync.

## Partner pulse row (public-pages 09)
- `/partner` lists the vacancy types plus the carousel highlight, not the pulse highlight:
  `VacancyProductRules` has `HighlightPulseTokens`, but no employer-facing screen sells it (it is an
  admin setting only). Add the row when pulse becomes buyable, so the tariff table keeps matching
  what an employer can actually order.

## Company page engagement form (public-pages 09)
- `/{kvk}` no longer carries the inline "klopt deze claim niet?" form next to the engagement badges.
  The DSA route is the single reporting path now ("Klopt er iets niet op deze pagina? Meld het." →
  `/melden?type=company&id={kvk}`, file 06). If moderation wants a claim-specific reason, add a
  `claim` reason to the report form instead of a second form on the page.

## Dependency fallbacks still open (public-pages 10)

The stack checked dependencies A–H before file 02 and again at 05, 06, 08 and 09. Six were present
and were used directly; two were absent and left a follow-up.

- **E. Auth 03 (`AuthPublicLayout` / `au-theme`) — absent.** Nothing in this stack depends on it, so
  nothing was faked. When auth 03 lands: `/privacy/data` (07) should link "Inloggen" through the same
  `AuthRedirects.ResolveRequestedReturnUrl` helper the auth pages use, so a visitor returns to
  mijn gegevens after signing in instead of to the role home.
- **H. Errors stack (`ErrorLayout` / `SupportCode`) — absent.** The simple `/status/{code}` page of
  file 01 is still the 404 page, and `/{kvk}` (09) renders its body through the shared
  `StatusPageContent`. When the errors stack lands, replace that body with `ErrorLayout` and give the
  company-page 404 and the report-form errors a support code. Do not restyle `/status/{code}` here.

## CI stack and the 10.1 page matrix

- The Development seed has no company in `KvkVerificationStatus.Pending` or `Failed`, and no company
  whose only vacancy is a draft. Those four 404 cases are proven against an in-memory API instead
  (`PublicPagesApiGuardTests`), and the Playwright matrix discovers the verified company from
  `/sitemap.xml` and skips the `/{kvk}` and `/melden?type=company` rows when the stack has none.
  Extend `DemoCompaniesSeeder` with a Pending, a Failed and a draft-only company when a browser-level
  proof of those 404s is wanted.

## Skipped in the 10.2 flows

- **Admin decision "Verwijderen" end to end.** The browser flow stops after the anonymous report is
  accepted. The decision, the resulting 404 on `/{kvk}` and the employer mail are covered without a
  browser by `ContentReportDecisionTests` and `ContentReportMailTests` (file 06); the CI smoke stack
  has no capturing mail sink to assert the mail from the browser.
- **Crafted POST without a waiver → 400 `waiver_required`.** Covered server-side by
  `BedenktijdGuardTests` (file 05). The browser test only proves the pay button stays disabled, and
  skips even that when the checkout offer is not reachable for the seeded candidate.
- **Werkgevers actief OFF (`/partner` and `/{kvk}` → 302 `/`).** Flipping the platform flag would
  change the shared CI stack for every other suite, so the gate is proven by the
  `[RequiresFeature(Employers, FallbackPath = "/")]` reflection tests in `PartnerPageTests` and
  `CompanyPageLayoutTests` (file 09) plus the sitemap test for the flag-off path list.
- **Footer legal line with `Legal__KvkNumber` empty.** That is the CI stack's actual configuration,
  so the matrix asserts the "KvK" label is absent rather than restarting the stack with a second
  configuration.

## Native review pl / ro / ar

- Every `Legal.*`, `Privacy.*`, `Terms.*`, `HowLobsy.*`, `About.*`, `PartnerPage.*`, `CompanyPage.*`
  and `Report.*` key added by this stack is a B1 dev-team draft in pl, ro and ar. The list for the
  reviewers is `docs/i18n/public-pages-review.md`. The Dutch text stays the official version (D3),
  so a translation fix is never a legal change.
