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
