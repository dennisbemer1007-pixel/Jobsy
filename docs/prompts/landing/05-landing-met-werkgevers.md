# 05. "/" becomes the landing page (Werkgevers actief ON): static SSR, no MapLibre, one real number

Read `00-README.md` first. Branch `cursor/landing-5` from `cursor/landing-4` (or `-4b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - "/" loads **no** MapLibre, `jobMap*.js`, tiles, pin/vacancy-list API calls, `blazor.web.js` or SignalR. Guard tests enforce it.

| | |
|---|---|
| Branch | `cursor/landing-5` |
| PR title | `feat(landing): new "/" landing page (werkgevers ON) — static SSR, warm public theme, no map JS` |
| PR body starts with | `Stacked on #<PR 04> (cursor/landing-4)` |
| Mockups | `lp-d1-landing.png` (desktop, all sections), `lp-m1-hero.png` (mobile, full page); sources `w_land.py`, `w_land2.py`, `w_hero.py`, `w_common.py`, `css_warm.py`, `css_lp.py` |
| Split seam | **05a** = page shell, redirects, hero, Wat is Lobsy, kreeft-visie, perf plumbing (05.2–05.4, 05.8). **05b** = Wat je krijgt, Voor wie, Privacy, FAQ + JSON-LD, closing CTA, footer (05.5–05.7) |

## Goal
Anonymous visitors on "/" get a fast, warm, static page that explains Lobsy in 30 seconds and sends them to the free test. There's no live map: a static illustration and one real number do the job. Signed-in users never see it; they go straight to their home.

## 05.1 Today (verify first)
- After 04: `Pages/Banenkaart.razor` has `@page "/banenkaart"` **and** `@page "/"`. `LegacyMapQuery` exists. `PublicLayout`, `PublicNavCatalog`, `LobsyMascot`, `IEmployersSwitch`, `LandingVariantResolver`, `PublicRoutes` and the static cookie banner exist (01, 02).
- `App.razor`: inline critical `<style>` with a few tokens (L24–81); `app.min.css` render-blocking for real browsers when `LoadBlazorRuntime`; the feature CSS list L86–97 + `<noscript>` L99; `blazor.web.js` and `app-core.js` deferred; `PageRenderMode` null for `[ExcludeFromInteractiveRouting]`.
- `StructuredData.WebsiteAndOrganization` is emitted on the home page by `PageSeoHead` (`IsHome`).
- Sizes: `app-core.js` ≈ 8 KB gz; `app.min.css` ≈ 64 KB gz.

## 05.2 Page, routes and server redirects
- Remove `@page "/"` from `Banenkaart.razor`. New `Pages/Landing.razor`: `@page "/"`, `@layout PublicLayout`, `[AllowAnonymous]`, `[ExcludeFromInteractiveRouting]`, `[NoBlazorRuntime]` (new attribute, `Jobsy.Web/Components/NoBlazorRuntimeAttribute.cs`).
- `App.razor`: when the endpoint metadata has `NoBlazorRuntimeAttribute`, don't emit `blazor.web.js` or the `blazor-boot.js` bits. Keep `app-core.js` (deferred) and add `js/landing.js` (deferred, ≤ 4 KB gz: mobile menu, `<details>` niceties, and the KPI beacon stub that 10 fills). Use the CSP nonce as the other scripts do.
- **`LandingRedirectMiddleware`** (runs before Blazor, only for path "/" GET/HEAD):
  1. signed-in user → **302** `FeatureRoutes.HomeFor(user)` when Dependencies A is present, else today's post-login logic with candidate → `/banenkaart`
  2. `LegacyMapQuery.IsMapDeepLink(query)` → **301** `/banenkaart?{same query}` (old shared links keep working)
  3. otherwise → the landing page
- `PublicNavCatalog`: the "/" logo link and anchors work in both desktop and mobile menus.
- Remove the temporary "/" → `/banenkaart` canonical from 04. `PageSeoCatalog["/"] = Public("Landing.Seo.Title", "Landing.Seo.Description")`, indexable, `Hreflang = true`.
- Response headers for "/": `Cache-Control: no-cache, private`, `Vary: Cookie`, `X-Lobsy-Variant: on` (§S). Test.

## 05.3 Sections (ON variant; one component tree, `Variant` parameter everywhere)
Components under `Components/Landing/`, each `Variant`-aware from the start (07 fills the `.Zw` copy and illustrations). Copy (nl) comes **verbatim** from `lp-d1-landing.png` / `w_land*.py`; en is final copy; pl/ro/ar are B1 translations (review list in 09). Section ids per §V.
1. **`LandingHero`** (`#top`): eyebrow chip "👋 Hoi! Gratis en zonder account" · h1 "Soms moet je uit je schild **groeien**." (the accent word via a span with the coral class; the key holds a `{0}` placeholder, not HTML) · sub · CTAs "Doe de gratis test →" (`/ontdek`, `data-kpi="LandingCtaTest"`) + "Inloggen" (`/login`, `data-kpi="LandingCtaLogin"`) · meta row "⏱️ 20 vragen · 3 minuutjes" · "🔒 Werkgevers zien je antwoorden niet" · the map link (05.4) · "Lobsy is er voor jou als je…" chips (werk zoekt / net in Nederland bent / op school zit / even vastzit → `/ontdek?voor=werk|nieuw|school|vast`, `data-kpi="LandingCtaTest"` + target).
   - Illustration `LandingHeroMapDisc` (inline SVG, **no** tiles/JS): the sand disc with roads, 15/30-minute rings, a "je oude schild" ghost, two job bubbles ("🚲 Zorgmedewerker · 12 min fietsen · Naaldwijk", "🚌 Monteur · 24 min met de bus · Delft") and the "🦞 Dit ben jij" card with 3 chips. All with a small `pub-pill-sample` "Voorbeeld". `LobsyMascot Pose=Waving Size=Hero Priority=true` + speech bubble "Hoi! Zin om te groeien? 👋".
   - Mobile (≤ 640, lp-m1-hero): text first, then the scene scaled to 390 wide (every element inside the viewport with ≥ 12 px margin at 360 and 390).
2. **`LandingWhatIs`** (`#wat-is-lobsy`): eyebrow + h2 "Geen cv-site. Lobsy kijkt eerst naar jóu." + lead + 3 step cards (Ontdek jezelf · Zie wat bij je past · Laat zien wat je kunt) with their chips.
3. **`LandingKreeft`** (`#kreeft`): wave edge, h2 "Jij bent de kreeft.", the story block + scene (`LobsyMascot Celebrating Large` + `Shell` ghost + bubble "Te krap? Tijd om te groeien! 🌱"), 6 tiles with `LandingMiniScene` (dive, antenna, rock, claw, grow, path; the inline SVG from `w_common.py SCENES`) and an "In Lobsy: …" line each. The tiles aren't links (they describe features).

## 05.4 The one real number (D7)
- API: `GET api/public/landing-stats` (`[AllowAnonymous]`, rate-limited like other public reads). Returns `{ activeVacancies }`: the count of active, publicly visible vacancies (the same visibility rules as the map index; reuse its query/filter, don't reimplement visibility). `IMemoryCache` 10 min.
- Web: `LandingStatsClient` with a **300 ms timeout** and its own 10-minute cache. On failure/timeout it returns null (never blocks or fails the page).
- Display (ON only): hero meta link "🗺️ Nu ruim **{n}** vacatures op de banenkaart" → `/banenkaart` (`data-kpi="LandingCtaMap"`), n = rounded **down** to tens, culture-formatted (`1.230` / `1,230`). If null or < **25** → fallback text "🗺️ Of kijk eerst op de banenkaart" (same link).
- No other live numbers on the page (the map tile's "38 banen" is illustration copy with the "Voorbeeld" pill).

## 05.5 `LandingWhatYouGet` (`#wat-je-krijgt`)
- h2 "Van “wie ben ik?” naar “hier wil ik werken”." + note "Alle voorbeelden: Voorbeeld".
- Tiles (static illustrations, inline SVG/CSS, no JS):
  - **De banenkaart** (big): travel-mode pills 🚲/🚌/🚗 (decorative), "Binnen 30 minuten van huis: 38 banen", 3 job rows, travel rings, `LobsyMascot Default Tiny` home marker
  - **Mijn Paspoort**: tabs Mijn DNA · Mijn tests · Past deze baan? · Carrière · Bewijzen, plus chips
  - **Ontdekkingsreis**: 4 steps
  - **Match**: "⭐ Jouw top-match 91% · Teamleider logistiek"
  - **Jouw rapport**: "€ {price} eenmalig · na je gratis tests" from `FlexCommercialSettings.DeepAnalysisPriceEuro` via the existing public settings/price API (find it: `git grep -n "DeepAnalysisPriceEuro" -- Jobsy.Api`); if unavailable, the price line is hidden
- **`LandingFeatureAvailability`** (05.5a): "Binnenkort" pills on Mijn Paspoort / Ontdekkingsreis show only while their pages don't exist. It's a static class with candidate route templates per feature (e.g. Paspoort: `/paspoort`, `/candidate/paspoort`, `/candidate/passport`; Ontdekkingsreis: `/ontdekkingsreis`, `/candidate/ontdekkingsreis`), checked once against the app's `@page` routes (reflection over `RouteAttribute` in the Web assembly). A test prints which matched.

## 05.6 `LandingForWhom`, `LandingPrivacy`
- **Voor wie** (`#voor-wie`), 4 short cards per §V with 3 bullets each and one link:
  - Kandidaat → `/ontdek`
  - Werkgever → `/werkgevers`
  - School → `/scholen`
  - Partner → `/partner`
  - each with `data-kpi="LandingCtaAudience"` + target
  - `/werkgevers` and `/scholen` don't exist until 08: render those two links only when `PublicNavCatalog` says `IsAvailable` (08 flips them), and keep the cards visible without a link until then.
- **Privacy** (`#privacy`): 4 cards (Eerst op jouw apparaat · Werkgevers zien niets zonder jou · Wissen kan altijd · Veilig en eerlijk), copy from the mockup.

## 05.7 `LandingFaq`, closing CTA, footer, JSON-LD
- FAQ (`#faq`): 7 questions as `<details>`/`<summary>` (no JS; the first one open), help card "Hulp nodig? Lees hoe Lobsy werkt." → `/hoe-werkt-lobsy` (no chat, README §0).
- Answers are short and true today. Examples: "Is Lobsy echt gratis?" (price from the same setting, no hard-coded amount in the key: `{0}`), "Heb ik een account nodig voor de test?", "Wat gebeurt er met mijn antwoorden?" (7 days on your device, merged on sign-up, wipe button), "Ik spreek nog niet goed Nederlands…" (5 languages), "Ik ben werkgever of school. Hoe begin ik?", "Waarom een kreeft?", "Hoe werkt Lobsy?".
- `StructuredData.FaqPage(IEnumerable<(q, a)>)` → `FAQPage` JSON-LD from exactly the rendered keys and culture; emitted on "/" next to `WebsiteAndOrganization`. Test that the JSON-LD questions equal the visible ones.
- Closing CTA "Klaar om uit je schild te groeien?" + "Doe de gratis test" / "Inloggen".
- Footer from 01 (`PublicFooter`, ON columns).

## 05.8 Performance plumbing (D13; budgets are enforced in 10)
- **CSS:** new `features/landing.css` (`pub-landing__…`) + `public-theme.css`, both small (together ≤ **25 KB gz**, a test measures it). Keep `app.min.css` as it loads today **only if** the Lighthouse run in 05.9 meets the budget. If not, load `app.min.css` non-blocking on `[NoBlazorRuntime]` pages (`media="print"` swap, as the crawler path does) and put the tokens the public theme needs into the inline critical `<style>` from one generated source (`PublicThemeTokens`, a test compares it with `app.css :root` so they never drift). Say which option you took and why, with numbers.
- **Images:** only the hero mascot is eager + preloaded (`LobsyMascot.PreloadLink`); everything else is `loading="lazy"`. All illustration art is inline SVG/CSS. No web fonts beyond what the app already loads; no emoji font.
- **HTML:** ≤ **60 KB gz** for "/" (test). No hidden duplicate of the page for mobile: one DOM, responsive CSS.
- **No map:** guard test `LandingPerformanceGuardTests` (source + rendered HTML): `Landing.razor` and every component under `Components/Landing/` don't reference `VacancyDiscovery`, `jobMap`, `jobsyMaps`, `maplibre`, `openfreemap`, `leaflet` or `api/vacancies`; the rendered "/" HTML contains none of those strings and no `blazor.web.js`.
- `data-kpi` attributes on every CTA (§K). 10 wires them.

## 05.9 Re-check the tests left on "/" in 04
Every test 04 listed as "home on /" now hits the landing. Update each to either assert the landing (static) or move to `/banenkaart`. `BlazorReconnectAndHealthzTests` and anything that needs a circuit must not use "/".

## Tests
- `LandingRedirectMiddlewareTests`: anonymous → 200; signed-in candidate → 302 `/banenkaart` (ON); employer/admin → their home; `/?company={guid}` and `/?q=zorg` → 301 `/banenkaart?…`; HEAD works; headers per §S.
- `LandingBunitTests` (ON): all §V ON sections and ids; hero CTAs + `data-kpi`; the number shown ≥ 25 and hidden < 25 / null; audience links hidden until available; FAQ JSON-LD equals the visible questions; no `/register` link anywhere on the page; exactly one `.cookie-consent`; `dir="rtl"` for ar.
- `LandingPerformanceGuardTests` (see 05.8) + the CSS/HTML size budgets.
- Playwright (`LandingPlaywrightTests`, CI stack):
  - desktop 1440 and mobile 390 screenshots
  - no requests to `maplibre*`, `jobMap*`, `tiles.openfreemap.org`, `api/vacancies*`, `_blazor`
  - hero CTA → `/ontdek`
  - the menu opens and closes with the keyboard
  - at 360 and 390 no element is wider than the viewport (`scrollWidth === clientWidth`) and every hero element sits within 12 px margins
  - cookie banner visible on first visit and never covering the hero CTA at 360×640
- `PageSeoTests` ("/" indexable, canonical "/", hreflang 5 + x-default), `RoutesDocFreshnessTests`, `PageHelpDocsTests`, `BlazorPageRoleAttributesTests`, `LocalizationParityReportTests`, `AssetVersionGuardTests`.
- A local Lighthouse mobile run on "/" (numbers in the PR body; 10 automates it).

## Success criteria
- "/" (anonymous) matches lp-d1 / lp-m1 within the spec's differences, in nl and en, and renders correctly in ar (RTL).
- The rendered page loads no map/Blazor assets. Local Lighthouse mobile: LCP ≤ 2.5 s, TBT ≤ 200 ms, CLS ≤ 0.1.
- Signed-in users and old map deep links never see the landing.

Done → next: `06-gratis-test-warm.md`.
