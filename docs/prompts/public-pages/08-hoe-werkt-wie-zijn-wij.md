# 08. Hoe werkt Lobsy (static SSR, sitemap, real pl/ro/ar) + Wie zijn wij (static, 5 languages, support@, admin editor removed)

Read `00-README.md` first (D9, D10, Dependencies A, D, F). Branch `cursor/public-pages-8` from `cursor/public-pages-7`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-8` from `cursor/public-pages-7` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-7)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-8`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - No migration: the `AboutPageSettings` table stays (unused) until a later cleanup; don't drop it here.

| | |
|---|---|
| Branch | `cursor/public-pages-8` |
| PR title | `feat(public): Hoe werkt Lobsy as a static page in 5 languages and in the sitemap; Wie zijn wij as a static page with support@; admin about editor removed` |
| Mockups | `pb-d01-hoe-werkt-lobsy`, `pb-m01-hoe-werkt-lobsy`, `pb-d02-wie-zijn-wij`, `pb-m02-wie-zijn-wij` |
| Migration | none |
| Split seam | **08a** = hoe-werkt-lobsy; **08b** = wie-zijn-wij + admin removal |

## 08.1 Re-check Dependencies A, D and F
Write the cases in the PR.

## 08.2 `/hoe-werkt-lobsy`
- **Today** (`Pages/HowLobsyWorks.razor`):
  - `InteractiveServer prerender: false`, so crawlers and slow phones see "Laden…"
  - anonymous → `HowLobsyRoleGuides.Guest`; staff roles get their guide (Sales/Ambassadeur with a tracking code from the API, Enterprise, Regional, Branch, Intermediary)
  - a candidate or admin is sent to `/home` (L66)
  - not in `PageSeoCatalog.StaticIndexablePaths` (L51–64)
  - `UiStringsHowLobsyRoles.cs` copies nl into pl/ro/ar on purpose
- **New:**
  - `[ExcludeFromInteractiveRouting]`, `@layout PublicLayout`, static SSR. The role is read from the auth cookie on the server.
  - Content (pb-d01):
    - hero "Zo werkt Lobsy. In 4 stappen."
    - audience pills "🙋 Voor jou · 🏢 Voor werkgevers · 🎓 Voor scholen" as links `?voor=jou|werkgevers|scholen` (server-rendered tab, `aria-current`)
    - 4 step cards: Kijk op de kaart → banenkaart · Doe de gratis test → `/ontdek` · Maak een account → `/account-maken` (landing 03) or `/register` today · Solliciteer op jouw tempo
    - "Dit beloven we je" (4 checks)
    - FAQ (3 questions; the age answer from `AgeRulesText`, the price is never hard-coded: reuse a public price endpoint if landing 05 or `docs/tests` 01 added one (`git grep -n "DeepAnalysisPrice" -- Jobsy.Api/Controllers/SiteController.cs Jobsy.Api/Controllers/SettingsController.cs`); otherwise add `GET api/site/prices` → `{ deepAnalysisFromEuroInclVat }` (anonymous, `public-read`, cached 5 min; the lowest per-type price when tests 01 landed, else `FlexCommercialSettings.DeepAnalysisPriceEuro`), shown as "vanaf € 2,99" per culture)
  - Links come from `PublicRoutes` (A present) or today's routes (absent).
  - **Signed in:**
    - no redirect for anyone
    - staff roles see their existing role guide as the selected tab "Voor jou (je rol)", rendered server-side from `HowLobsyRoleGuides`; the Sales/Ambassadeur tracking code is fetched server-side, and when that fails the guide renders without the code
    - a candidate sees the "Voor jou" tab with a button "Naar je start" (`FeatureRoutes.HomeFor` if present, else `/home`)
  - **Werkgevers actief OFF** (F present): no "Voor werkgevers" pill; step 1 becomes "Maak je paspoort" (landing -zw wording); no banenkaart link.
  - **Languages:** new keys `HowLobsy.*` in `UiStringsPublicInfo` with real pl/ro/ar drafts. `UiStringsHowLobsyRoles` gets real pl/ro/ar drafts for the guest guide keys and stops copying nl (the staff guides may stay nl+en; list them in `docs/i18n/public-pages-review.md`). `untranslated-baseline.txt` may not grow; it should **shrink**.
  - **SEO:** add `/hoe-werkt-lobsy` to `StaticIndexablePaths` (ON and OFF), canonical without query (`?voor=` variants canonical to the base).

## 08.3 `/wie-zijn-wij`
- **Today** (`Pages/Legal/WieZijnWij.razor`):
  - InteractiveServer prerender true + a refetch, so the loading state flickers
  - the body is HTML from `AboutPageSettingsService` (DB, admin `/admin/about`, `HtmlSanitize`), Dutch only; the h1 ignores the admin title
  - the default text has the privacy placeholder (fixed in 01), "Via chat weet je sneller" (no public chat) and "stoffige uitzendbureau-vibes"
  - live content uses `privacy@lobsy.nl`
- **New** (pb-d02), static SSR, `PublicLayout`:
  - hero "Hoi! Wij zijn Lobsy." + lead
  - 3 story cards: Waarom een kreeft? · Begonnen in het Westland · Voor twee kanten
  - founder card: "Dennis, oprichter" + one paragraph from `About.Founder.Text`. Photo: `wwwroot/images/about/founder.webp` if the file exists (asset manifest), else the emoji avatar. The mockup's "Foto: Dennis levert aan" is mockup-only.
  - contact card: "Vraag of idee? Mail ons, we reageren binnen 2 werkdagen." + `mailto:{SupportEmail}` (D9) + `LegalIdentityCard` without the privacy row
  - All text in `About.*` (`UiStringsPublicInfo`), 5 languages. No HTML from the DB.
  - **Known difference with the mockup:** note 1 in pb-d02 suggests an admin-editable founder paragraph. Dennis chose D10 (no admin text), so the paragraph is a string key.
- **Removed:**
  - `Pages/Admin/AboutPageAdmin.razor` and its nav item (`AdminNavItems.cs` L16 `Nav.AboutPage`)
  - `RoleNavCatalog.cs` L19 entry
  - `PageHelpDocs.cs` L450 and `PageSeoCatalog.cs` L185 entries
  - `SettingsController` `GET/PUT about` (L401–408), `SiteController` `GET about` (L28)
  - `IAboutPageSettingsService` + `AboutPageSettingsService` + DI + the `PlatformSettingsSeeder` about part
  - the Web client methods
- `/admin/about` answers **301 → `/admin`** (D absent) or is dropped from admin-redesign's "Pagina's & flyer" tabs (D present; the flyer tab stays, per `AdminNav.cs` naming).
- `AboutPageSettings` entity + table stay. Mark the entity `[Obsolete("Unused since public-pages 08; drop in a cleanup migration")]` and add the drop to `docs/public-pages-followups.md`.
- Update `Sprint6AdminSuiteTests` (it references the about settings): remove or rewrite the about parts, keep the rest.

## 08.4 Tests
- `HowLobsyPageTests`: anonymous GET returns full content in the HTML (no "Laden…"), no Blazor interactive root; candidate GET → 200 (no redirect); SalesManager sees the sales guide; `?voor=werkgevers` selects the tab; OFF variant hides employer content (test double).
- `SitemapTests`: `/hoe-werkt-lobsy` present.
- `HowLobsyStringsTests`: guest keys in pl/ro/ar differ from nl.
- `AboutPageTests`: static HTML contains the 3 cards, `mailto:support@lobsy.nl` (config default) and no `privacy@` link; ar renders RTL.
- `AboutAdminRemovedTests`: `/admin/about` → 301; `api/settings/about` → 404; `git grep -n "IAboutPageSettingsService"` is empty in the code.
- `RoutesDocFreshnessTests`, `PageHelpDocsTests`, `PageSeoTests`, `BlazorPageRoleAttributesTests`, `LocalizationParityReportTests` green.

## Success criteria
- Both pages render complete, static HTML in 5 languages (ar RTL). `/hoe-werkt-lobsy` is in the sitemap and never redirects.
- There's no admin-editable about text anymore; contact is `support@lobsy.nl` from config.
- PR body: dependency cases, screenshots nl desktop/mobile + ar mobile of both pages, the removed endpoints.

Done → next: `09-partner-bedrijfspagina.md`.
