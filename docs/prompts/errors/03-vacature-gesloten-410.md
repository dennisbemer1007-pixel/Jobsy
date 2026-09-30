# 03. Closed vacancy: 410 page with max 3 similar vacancies nearby

Read `00-README.md` first (§IA, E4, Dependencies A and F). Branch `cursor/errors-3` from `cursor/errors-2`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/errors-3` from `cursor/errors-2` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/errors-2)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/errors-3`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Don't change `VacancyDiscovery` internals or the employer/admin view of non-public vacancies. Reuse the discovery query, don't write a new ranking.

| | |
|---|---|
| Branch | `cursor/errors-3` |
| PR title | `feat(errors): closed vacancies answer 410 with a friendly page and up to 3 similar vacancies nearby` |
| Mockups | `er-d05-vacature-gesloten` |
| Migration | none |

## Goal
Someone who opens an old vacancy link (WhatsApp, Google, a flyer) sees that this job is closed, and gets up to three similar jobs nearby. Crawlers see 410 and drop the URL.

## 03.1 Re-check Dependencies A and F
Write the cases in the PR.

## 03.2 Today (verify first)
- `VacanciesController.GetById` (L562+): public → 200. Non-public → 200 only for an employer with access or an admin; otherwise **404** for both unknown and closed.
- `VacancyVisibilityRules.IsPubliclyVisible` = `Status == Active && StartDate <= today && EndDate >= today`.
- `Vacancy` has `PublishedAtUtc?` (L43) and `ClosedAtUtc?` (L48). `VacancyStatus`: Draft, Active, Archived, PendingApproval, Fulfilled.

## 03.3 API
- **Closed** = the vacancy exists, `PublishedAtUtc != null`, it's not publicly visible now, and `Status` is `Archived`, `Fulfilled`, or `Active` with `EndDate < today`. Everything else that isn't visible (Draft, PendingApproval, Active with a future start, never published) stays **404**.
- For an anonymous viewer, or a signed-in one without access, `GetById` returns **410** with `ClosedVacancyDto { id, title, city?, categoryId?, categoryLabel? }`:
  - no company name (intermediary hidden mode, `IntermediaryPublicIdentity`), no dates, no contact, no description
  - the owner/admin path is unchanged (200 with the full DTO)
  - if public-pages 06 added `Company.PublicPageBlockedAtUtc` and it's set → 404
- `GET api/vacancies/{id}/similar?limit=3` (anonymous, `public-read`):
  - only for a closed vacancy; else 404
  - reuse the discovery query/index with origin = the closed vacancy's `Location`
  - filter: same `CategoryId` if set, publicly visible, within **25 km**, nearest first, `limit` clamped to 1–3
  - no category → nearest publicly visible within 10 km
  - response = the list item shape the banenkaart uses; empty list is fine
- The Web client maps 410 to a typed result (`VacancyLookup.Closed(dto)`), not an exception.

## 03.4 Page (er-d05)
- **Status:** `VacancyDetail.razor`, on `Closed`: set status **410** during prerender (same pattern as the 404), `noindex`, render `<ClosedVacancyView>` in `ErrorLayout`'s content style (the page keeps its route; render the view inside `ErrorLayout` via a `LayoutComponentBase` switch or by rendering the same `err-` blocks; pick the smaller change and say which).
- **Content:**
  - eyebrow "Vacature gesloten", h1 `Status.Gone.Title` "Deze vacature is gesloten"
  - lead `Status.Gone.Lead` "“{title}” in {city} staat niet meer open. Misschien past een van deze banen bij je." (without city: "“{title}” staat niet meer open.")
  - up to 3 vacancy cards (existing card component, travel time if the visitor has an origin like the banenkaart, else distance). The list is in the prerendered HTML (the API call is part of the page's normal load, not the error layout).
  - none found → "We vonden nu geen vergelijkbare banen in de buurt."
  - buttons "Bekijk de banenkaart" (primary) and "Doe de gratis test"
- **SEO:** noindex, no JSON-LD `JobPosting` for closed vacancies (check `VacancyDetail`'s JSON-LD path). The sitemap already only lists public vacancies (`SiteController` crawl-index L59); keep it.
- **Werkgevers actief OFF** (F present): vacancies 302 to `/` before this logic; nothing to do.
- **Strings:** `Status.Gone.*` in 5 languages.

## 03.5 Tests
- `ClosedVacancyApiTests`:
  - Archived, Fulfilled and expired-Active published vacancies → 410 with only `id/title/city/category*`
  - Draft, PendingApproval, never-published, future-start → 404
  - the owner still gets 200
  - `similar` returns ≤ 3 public vacancies of the same category within 25 km, nearest first; for a public vacancy → 404
- `ClosedVacancyPageTests`: `/vacancies/{closedId}` → **410**, HTML has the title, city, ≤ 3 cards, `noindex`, no `JobPosting` JSON-LD; `/vacancies/{draftId}` → 404.
- `ClosedVacancyPrivacyTests`: the 410 JSON and HTML never contain the company name of an intermediary-hidden vacancy (seed one).

## Success criteria
- Old links to closed vacancies answer 410 with a useful page. Unknown and never-public ones answer 404.
- No new ranking logic: the similar list reuses the discovery query.
- PR body: status table, screenshots nl desktop/mobile + ar mobile.

Done → next: `04-429-reconnect-meldingen.md`.
