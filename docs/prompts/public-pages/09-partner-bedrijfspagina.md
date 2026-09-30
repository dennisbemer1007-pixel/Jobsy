# 09. /partner in B1 (excl. btw, "Gratis", jargon out, werkgevers-actief gate) + /{kvk} company page in the public layout

Read `00-README.md` first (§IA, D4, D5, Dependencies A, F, H) and `01-hotfix-juridisch-kvk.md` §01.3. Branch `cursor/public-pages-9` from `cursor/public-pages-8`.

> **Rules (same as README §0, repeated on purpose):**
> - Branch `cursor/public-pages-9` from `cursor/public-pages-8` (stacked). ONE PR into `acceptatie`; the body starts with `Stacked on #<prev PR> (cursor/public-pages-8)`.
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only `cursor/public-pages-9`; no force-push.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - Never show `ex.Message`, stack traces, placeholders or internal ids to visitors.
> - Don't change token prices, Mollie amounts, `SalesCommercialSettings` semantics, `VacancyDiscovery` internals or `jobMap*.js`. Display only.

| | |
|---|---|
| Branch | `cursor/public-pages-9` |
| PR title | `feat(public): partner page in B1 with prices excl. btw and "Gratis", gated by werkgevers actief; company page /{kvk} in the public layout with vestiging tabs and report link` |
| Mockups | `pb-d06-partner`, `pb-m06-partner`, `pb-d07-bedrijfspagina`, `pb-m07-bedrijfspagina` |
| Migration | none |
| Split seam | **09a** = partner; **09b** = company page |

## 09.1 Re-check Dependencies A, F and H
Write the cases in the PR.

## 09.2 `/partner` (+ `/partner/{code}`)
- **Today** (`Pages/Partner/PartnerSales.razor`, 209 lines):
  - InteractiveServer prerender true, `ex.Message` at L150/L170
  - rows "{n} tokens ≈ € {PriceEuro}" (L60–72) incl. "0 tokens ≈ € 0,00" for free types
  - "Pulse: … tokens" (L59)
  - `Partner.Usp1` "Reistijd-matching i.p.v. landelijke spill", `Partner.Usp2` "Funda-model: banenkaart + highlight-carrousel" (`UiStringsExtras.cs` L435–436); the same jargon in `PartnerFlyerPdfService.cs` L119–120
  - share subject "Lobsy — partneraanbod Westland & Den Haag" (L187)
- **VAT check first (D5 extra):** find out whether `VacancyTypeCosts[].PriceEuro`, `BaseTokenValueEuro` and package prices are what Mollie charges incl. btw (`TokenVatPricing`: "Token pack prices are charged incl. 21% BTW"; follow `TokenPurchaseCheckout` creation).
  - **Incl.:** show the excl. amount from `TokenVatPricing.SplitInclVatEuros(x).ExVatCents` as the main number, and "€ {incl} incl. btw" muted next to it.
  - **Already excl.:** show as-is and add the incl. amount the same way.
  - **Unclear:** stop and report (draft PR). Never change stored prices.
- **New page** (pb-d06), `[ExcludeFromInteractiveRouting]` static SSR, `PublicLayout`:
  - hero "Vind personeel dichtbij." + "Lobsy laat je vacature zien aan mensen in de buurt, op fiets-, OV- of autotijd. Je betaalt alleen als je een vacature plaatst."
  - buttons "Bedrijf registreren" (`/register`, with `?ref={code}` as today) and "Bekijk de tarieven" (`#tarieven`)
  - mascot (landing 02 if present)
  - 3 cards: Kandidaten dichtbij · Betaal per vacature · Gratis start-highlight ({StartHighlightBonusTokens})
  - **Tarieven** `#tarieven`:
    - "1 token = € {x} excl. btw"
    - rows per vacancy type: label · tokens · amount; `CostTokens == 0` → "—" and **"Gratis"**
    - Highlight row "Highlight ({HighlightCarouselDays} dagen)"; "Pulse" is renamed "Extra zichtbaarheid (Pulse)" only if it's a real product; if no UI sells it, drop the row and say so
    - line "Alle prijzen zijn exclusief btw." + "Pakketten met korting vind je na het aanmelden."
  - **Delen:** WhatsApp · Mail (01's `MailtoLink`) · Flyer (pdf) as pill buttons; they work without JS (plain links; the flyer is a GET download link). Salescode line when a valid code is present.
  - USP strings rewritten in B1 (`PartnerPage.*` in `UiStringsPublicInfo`, 5 languages); remove `Partner.Usp1/2` jargon, same fix in `PartnerFlyerPdfService` L119–120.
  - The share subject becomes "Lobsy: personeel vinden dichtbij" (no region).
- **Gate (F present):** OFF → 302 `/` for `/partner` and `/partner/{code}`; removed from sitemap when OFF.
- **Errors:** catalog load failure → the page renders without the Tarieven section plus `PartnerPage.RatesUnavailable` "De tarieven laden nu niet. Probeer het later nog eens." No `ex.Message`.
- **SEO:** `/partner` index (ON), `/partner/{code}` noindex + canonical (from 01, keep).

## 09.3 `/{kvk}` company page
- **Today:** after 01 the data rules are right, but the page uses its own map chrome (`jobsy-logo`, `LanguageSelector`, `AuthHeader` at L28–33), and an unknown number shows "niet gevonden" inside the map layout.
- **New** (pb-d07), `@layout PublicLayout`, InteractiveServer **prerender** (map needs JS; the list is in the first HTML):
  - breadcrumb "Banenkaart › {name}"
  - header card: logo (only an uploaded logo; else an emoji tile from the category) + name + "{city} · {n} vacatures" + chip **"KvK gecontroleerd"** (always true for rendered pages after 01)
  - vestiging tabs "Alle vestigingen ({n})" + one per branch with a public vacancy (links to `/{kvk}/{vestiging}`, `aria-current`)
  - vacancy cards (existing `JobCard`/vacancy card component, same data as the banenkaart)
  - map on the right (desktop) / below the list (mobile), loaded **after** the list (lazy init on first render, same `jobMap` API)
  - "Klopt er iets niet op deze pagina? Meld het." → `/melden?type=company&id={kvk}` (06)
- **Not found** (unknown, unverified, no public vacancy, blocked): the response is 404 and renders the shared status page:
  - H present: the `docs/errors` ErrorLayout 404
  - H absent: 01's `/status/404` content, rendered inline (the same component)
  - never the map chrome
- **JSON-LD:** `Organization` with name, `address.addressLocality` = city, logo, url; origin from config (01). No `JobPosting` duplication (the vacancy pages have their own).
- **Gate (F present):** OFF → 302 `/`.
- **SEO:** index only when 200; canonical = its own path; vestiging pages canonical to themselves.

## 09.4 Tests
- `PartnerPageTests`: static HTML (no interactive root); a 0-token type shows "Gratis" and never "€ 0,00"; the amounts use the excl./incl. rule from 09.2 (test with a known catalog: € 12,50 incl → "€ 10,33"); "exclusief btw" line present; no "spill", "Funda", "Westland & Den Haag"; catalog failure → page 200 without the rates section and no exception text.
- `PartnerGateTests` (F present): OFF → 302 `/`; sitemap without `/partner`.
- `CompanyPageLayoutTests`: 200 page uses `PublicLayout` (`.pub-theme` root), shows city and "KvK gecontroleerd", no street address; vestiging tabs only for branches with vacancies; unknown kvk → 404 with the status page and no map script.
- `CompanyJsonLdTests`: origin = configured public origin even when the request Host differs.
- Screenshots nl desktop/mobile + ar mobile for both pages.

## Success criteria
- `/partner` shows excl. btw amounts, "Gratis" for free types, and no jargon; it's gated when werkgevers actief is OFF.
- `/{kvk}` renders in the public layout, and every not-found case is a real 404 with the shared status page.
- PR body: the VAT finding (incl./excl.) with file refs, dependency cases, screenshots.

Done → next: `10-e2e-rapport.md`.
