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
