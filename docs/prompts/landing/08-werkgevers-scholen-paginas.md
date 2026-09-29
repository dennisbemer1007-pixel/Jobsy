# 08. Public audience pages `/werkgevers` and `/scholen`; audience cards wired

Read `00-README.md` first. Branch `cursor/landing-8` from `cursor/landing-7` (or `-7b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Only claims that are true today (or clearly marked "binnenkort"). No prices, numbers or customer names that don't come from real data.

| | |
|---|---|
| Branch | `cursor/landing-8` |
| PR title | `feat(landing): public /werkgevers and /scholen pages; audience cards link to them` |
| PR body starts with | `Stacked on #<PR 07> (cursor/landing-7)` |
| Mockups | the "Voor wie" cards in `lp-d1-landing.png` (werkgever, school, partner) and `lp-d1-landing-zw.png` (school); page layout follows the landing sections (no dedicated mockup) |
| Split seam | none |

## Goal
The short cards on the landing page lead somewhere useful: employers find a clear "Bedrijf registreren" path, and schools find what Lobsy will offer them and how to get in touch. Candidates are never pulled into the company registration.

## 08.1 Today (verify first)
- `/register` (KvK company registration), `/login`, `/partner` (`PartnerSales`), `/hoe-werkt-lobsy`, `/wie-zijn-wij` exist. No `/werkgevers` or `/scholen` route (no conflict with `/{KvkNumber:regex(^\d{8}$)}`).
- `PlatformLegalIdentity` has placeholder constants (e.g. `"[CONTACT E-MAIL PRIVACY]"`); there's no real contact e-mail in the code.
- Dependencies E: `SchoolsEnabled` (scholen stack) present or absent.
- `PublicNavCatalog` has `/werkgevers` and `/scholen` with `IsAvailable = false`; `LandingForWhom` renders those cards without links until they're available (05.6).

## 08.2 `/werkgevers` (ON only)
- `Pages/Public/Werkgevers.razor`: `@page "/werkgevers"`, `PublicLayout`, static SSR, `[ExcludeFromInteractiveRouting]`, `[NoBlazorRuntime]`, anonymous. OFF → **server** 302 "/" (`[RequiresFeature(PlatformFeature.Employers, FallbackPath = "/")]` when A is present, else `EmployersGate`); make sure the static-SSR path answers a real 302, not a client navigation.
- Sections (reuse the landing's `pub-` blocks; ≤ 1 screen per section):
  1. Hero: h1 "Vind mensen die écht bij je team passen", sub "Niet alleen op papier. Lobsy matcht op werk-DNA en reistijd.", CTAs **Bedrijf registreren** → `/register` (`data-kpi="LandingCtaAudience"` target `employer-register`) + **Inloggen** → `/login`. `LobsyMascot Sitting Large`.
  2. Hoe het werkt, 3 steps: Vacatures plaatsen · Matches op DNA en reistijd · Werken met tokens (one plain sentence each, describing what exists today).
  3. Wat kandidaten zien en wat jij ziet (privacy): "Kandidaten kiezen zelf wat ze delen. Je ziet geen testantwoorden."
  4. 3 FAQ `<details>`: "Wat kost het?" ("Je werkt met tokens. De tarieven zie je na registratie." No amounts unless they come from an existing public setting), "Hoe snel kan ik starten?", "Werkt Lobsy met mijn ATS?" (only if an integration exists: `git grep -n "Ats" -- Jobsy.Api/Controllers`; otherwise omit the question).
  5. Closing CTA: Bedrijf registreren.
- SEO: indexable (ON), `Hreflang = true`, title/description `Werkgevers.Seo.*`. Sitemap ON only.

## 08.3 `/scholen` (always)
- `Pages/Public/Scholen.razor`: `@page "/scholen"`, same attributes, available in both variants.
- Sections:
  1. Hero: h1 "Help leerlingen ontdekken wat bij ze past", sub, `LobsyMascot Sitting Large`.
  2. 3 blocks: Tests voor je klas · Inzicht per groep ("vanaf 5 leerlingen") · Hulp bij studiekeuze.
  3. Privacy: "Leerlingen gebruiken een code. Lobsy bewaart geen leerlingnamen." (from `docs/scholen` D3). Show it only as a "binnenkort" promise while the school product isn't live.
  4. Contact CTA:
     - add `PlatformLegalIdentity.SchoolsEmail = "[CONTACT E-MAIL SCHOLEN]"` (a placeholder, like the others)
     - while the value is still a placeholder (starts with `[`), **hide** the mailto button and show "Interesse? Neem contact op via Wie zijn wij." → `/wie-zijn-wij`
     - with a real value: **Mail ons** → `mailto:{SchoolsEmail}?subject=Lobsy voor scholen`
     - test both
  - **Dependencies E:** `SchoolsEnabled` absent or false → a "Lobsy voor scholen start binnenkort" pill in the hero and no portal link. True → an extra quiet link "Inloggen voor scholen" → the scholen stack's login route (`/leerling` for pupils is **not** linked here; only staff login `/login`).
- SEO: indexable (both variants), hreflang, `Scholen.Seo.*`. In the sitemap in both variants.

## 08.4 Wire the cards and nav
- `PublicNavCatalog`: flip `/werkgevers` (ON) and `/scholen` (both) to `IsAvailable = true`.
- `LandingForWhom`: Werkgever → `/werkgevers`, School → `/scholen`, Partner → `/partner` (ON; the werkgevers-actief gate covers OFF). The OFF School card → `/scholen`.
- Footer ON "Werkgever registreren" → `/register` (unchanged) and "Werkgevers" → `/werkgevers`.
- `docs/ROUTES.md`, `PageHelpDocs` (short help for both pages), `BlazorPageRoleAttributesTests`, `CHANGELOG.md`. `docs/feature-flags.md` (A present) or the follow-up doc (A absent): `/werkgevers` gated.

## Tests
- bUnit: both pages in nl/en/ar; `/werkgevers` CTA → `/register`; `/scholen` placeholder → no mailto + "Wie zijn wij" link, real value → mailto; "binnenkort" pill per Dependencies E.
- Endpoint: `/werkgevers` OFF → 302 "/" (server), ON → 200; `/scholen` 200 in both.
- `LandingForWhom` links ON/OFF; OFF HTML still has no `/werkgevers` (07 test stays green).
- Perf guard from 05 extended to both pages (no map/Blazor assets). `PageSeoTests`, sitemap ON/OFF, `LocalizationParityReportTests`.

## Success criteria
- Every card in "Voor wie" (both variants) leads to a working page, or `/partner`.
- A candidate never reaches `/register` from these pages without clicking a button labelled "Bedrijf registreren".
- No placeholder e-mail is ever rendered as a link.

Done → next: `09-talen-rtl.md`.
