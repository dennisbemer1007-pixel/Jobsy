# 07. Werkgevers actief OFF: the -zw variant on "/", gates, SEO/sitemap/cache per flag, "Past dit beroep?", clipped chips fixed

Read `00-README.md` first. Branch `cursor/landing-7` from `cursor/landing-6` (or `-6b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Re-run Dependencies check A first.** OFF output never links to or mentions vacancies, the map, employers, Match, `/register`, `/partner` or `/westland`.

| | |
|---|---|
| Branch | `cursor/landing-7` |
| PR title | `feat(landing): werkgevers-uit variant on "/" (server-rendered), flag-aware SEO/sitemap/cache, "Past dit beroep?"` |
| PR body starts with | `Stacked on #<PR 06> (cursor/landing-6)` + "Dependencies A at 07: present/absent" |
| Mockups | `lp-d1-landing-zw.png`, `lp-m1-hero-zw.png` (the fixed chips), `lp-d4-test-resultaat-zw.png`, `lp-m3-resultaat-zw.png`; source `zw_scene.py`, `w_land*.py` (`zw=True` branches), `css_zw.py` |
| Split seam | **07a** = variant content + test-flow copy + chips fix (07.3, 07.5–07.7). **07b** = FeatureRoutes amendment, gates, SEO/sitemap/JSON-LD/cache (07.2, 07.4) |

## Goal
When Dennis switches "Werkgevers actief" OFF, "/" is still a complete, honest landing page, about discovering yourself, the passport and "Past dit beroep?". It renders on the server with no flash of the ON version, and search engines, the sitemap and caches follow the switch within minutes.

## 07.1 Today (verify first)
- **Re-run check A** (`git grep -n "interface IFeatureFlags" origin/acceptatie -- Jobsy.Core`). If it flipped to present since 01: `git merge origin/acceptatie` into this branch (normal merge, say so), replace `AlwaysOnEmployersSwitch` with the `IFeatureFlags` adapter, and delete the items that landed from `docs/feature-flags-landing-followup.md`.
- On the werkgevers-actief code (`origin/cursor/werkgevers-actief` @ `8b25411f` for reference; don't branch from it):
  - `FeatureRoutes.HomeFor(user, FeatureFlagSnapshot)`: ON anonymous "/", ON candidate "/", admin `/home`; OFF anonymous **`/ontdek`**, candidate `/candidate/profile`, employer-only `/access-denied?reason=employers-off`.
  - `FeatureRouteGate` (Routes.razor) navigates pages with `[RequiresFeature]` to `FallbackPath` or `HomeFor`.
  - `docs/feature-flags.md` lists "/" and `/banen` under "Gated Blazor pages (Employers)".
  - `FeatureFlagFoundationTests` asserts `HomeFor(null, on) == "/"`; `WerkgeversUitPlaywrightTests` covers OFF.
- After 05/06: `Landing.razor` + `Components/Landing/*` with `Variant`; `LandingVariantResolver`; `Landing:ForceVariant` for dev/CI.

## 07.2 Amend werkgevers-actief (D3, D20): only when A is present
- `FeatureRoutes.HomeFor`: **OFF anonymous → "/"** (was `/ontdek`); **ON candidate → `/banenkaart`** (was "/"). The rest is unchanged.
- `Banenkaart.razor` (was `Home.razor`, gated since werkgevers-actief) keeps `[RequiresFeature(PlatformFeature.Employers, FallbackPath = "/")]`. `Landing.razor` has **no** `RequiresFeature`. The `/banen` endpoint gets `.RequireFeature(…)` with a 302 to "/" when OFF.
- `docs/feature-flags.md`: in "Gated Blazor pages", replace `/` by `/banenkaart` and add `/werkgevers` (08). Add a "Changed by landing 07" note with the two `HomeFor` changes and the reason ("/" is the landing page in both variants; the canonical stays "/").
- Update `FeatureFlagFoundationTests` (`HomeFor(null, off) == "/"`, `HomeFor(candidate, on) == "/banenkaart"`) and any Playwright expectation of "/" → `/ontdek`.
- **A absent:** don't touch `FeatureRoutes` (it doesn't exist). Make the equivalent gates with `EmployersGate` (01.8) and keep `docs/feature-flags-landing-followup.md` accurate (it already lists these changes).

## 07.3 The -zw variant on "/" (§V OFF column)
- `LandingVariantResolver` → `Zw` when employers are OFF (or `ForceVariant=zw` in dev/CI). The page renders the OFF column of §V in the **same** components via the `.Zw` keys and `Variant` switches. There's no separate page, no client-side swap, and no ON markup hidden with CSS (the OFF HTML must not contain ON-only text or links; test).
- **Hero OFF** (lp-d1-zw / lp-m1-zw): the `LandingHeroPassport` illustration (inline SVG/CSS: "Mijn Paspoort" card with the 4-part wheel "Dit ben jij · 4 kanten", 4 legend rows with %, chips Samenwerker · Mensen helpen · Warm team, "Voorbeeld" pill), `LobsyMascot Waving` + `Shell` ghost "je oude schild" + bubble "Kijk, dit ben ik! ✨", and the floating cards "✅ Past dit beroep bij mij? · Verpleegkundige · 82% fit" and "🧭 Ontdekkingsreis · Stap 3 van 6 · Hier voel je je thuis". Meta row: "⏱️ 20 vragen · 3 minuutjes" · "🔒 Alleen jij ziet je antwoorden" · "🪪 Gratis paspoort". **No number** (D7).
- **Wat is Lobsy OFF:** step 2 "Zie wat bij je past: Welke beroepen passen bij jou? En wat heb je nog nodig voor je droombaan?" (chips Functiefit · Droombaan-check); step 3 "…Van jou, voor jou." (chips Paspoort · Bewijzen).
- **Kreeft OFF:** "De juiste rots → In Lobsy: Past dit beroep?", copy "…Welk beroep is jouw rots?".
- **Wat je krijgt OFF:** h2 "Van “wie ben ik?” naar “dit past bij mij”."; tiles Mijn Paspoort (big; tabs 🪪 Mijn Paspoort · 🧬 Mijn DNA · 🧪 Mijn tests · ✅ **Past dit beroep?** · 🪜 Carrière · 📜 Bewijzen; "Dit ben jij" story + 4 rows), Past dit beroep bij mij? (3 beroepen with %), Droombaan-check (Nu → Volgende stap → Droombaan), Ontdekkingsreis, Jouw rapport. No banenkaart, no Match.
- **Voor wie OFF:** Jij, op zoek naar je richting (→ `/ontdek`) · Nieuw in Nederland ("Kies je taal" opens the language menu) · Even vastgelopen ("Zo werkt het" → `/hoe-werkt-lobsy`) · School (→ `/scholen` once 08 lands).
- **Privacy OFF:** "Alleen jij kijkt mee". **FAQ OFF:** the "gratis" answer without "banenkaart"; "Ik werk op een school. Hoe begin ik?". **Footer OFF:** §V.
- Header OFF: the anchors `/#wat-je-krijgt` and `/#ontdekkingsreis` (make sure those ids exist in the OFF markup).
- `landing-stats` isn't called when OFF (and the API endpoint is gated `.RequireFeature` when A is present).

## 07.4 SEO, sitemap, JSON-LD, cache per flag (§S)
- `PageSeoCatalog["/"]` picks `Landing.Seo.Title(.Zw)` / `Landing.Seo.Description(.Zw)` from the variant. Canonical "/" in both.
- `SeoEndpoints` sitemap OFF: drop `/banenkaart`, `/werkgevers`, `/partner`, `/westland`, `/vacancies/*` and company/vestiging URLs; keep `/`, `/ontdek`, `/scholen`, `/hoe-werkt-lobsy`, `/wie-zijn-wij`, the legal pages. robots OFF: `Disallow` nothing new (gated pages redirect), but make sure no sitemap line points at a gated URL.
- JSON-LD on "/" OFF: `WebSite` + `Organization` + `FAQPage` (OFF questions). The `WebSite` `SearchAction` (points at `/banenkaart?q=`) is **omitted** when OFF. No `JobPosting` anywhere (werkgevers-actief already strips vacancy pages).
- Headers: "/" `X-Lobsy-Variant: zw`; `/sitemap.xml` + `/robots.txt` `Cache-Control: public, max-age=300` + `ETag` = hash(content + flag). Test that toggling the flag (test double) changes the ETag and content.
- `hreflang` unchanged (both variants share URLs).

## 07.5 Test flow OFF (lp-d4-zw, lp-m3-zw)
- Every "Werkgevers zien ze nooit" / "…je antwoorden nooit" in `/ontdek` becomes "Alleen jij ziet ze" / "Alleen jij ziet je antwoorden" (`GratisDna.*.Zw` keys; the page asks `IEmployersSwitch` once at init). The same goes for the start checkbox text.
- `gd-jobs-teaser` isn't rendered OFF.
- Sign-up card unlock list OFF: "🧬 Je volledige paspoort met Mijn DNA" · "✅ “Past dit beroep bij mij?” voor elk beroep" · "🪜 Je droombaan-check en je volgende stap" · "🧭 De Ontdekkingsreis, stap voor stap".
- The locked passport teaser tab reads "Past dit beroep?" OFF.

## 07.6 "Past deze baan?" → "Past dit beroep?" (D17, Dependencies B)
- **B present** (`Passport.Tab.Fit` exists): add `Passport.Tab.Fit.Zw` = "Past dit beroep?" (en "Does this occupation suit you?"; pl/ro/ar in B1), and make the passport tab component choose the key through `LandingText.For` or an equivalent `IEmployersSwitch` check. bUnit test both variants.
- **B absent:** only the landing/test illustration labels (`Landing.Get.Passport.TabFit(.Zw)`, `GratisDna.Locked.TabFit(.Zw)`), plus the follow-up line in `docs/feature-flags-landing-followup.md`.

## 07.7 Fix: clipped chips on the mobile -zw hero passport card
- Dennis flagged that on mobile (-zw hero) the chips on the right of the passport card were cut off. The final mockup `lp-m1-hero-zw.png` shows the fix: the card is 230 px wide (at 390), the wheel is 78 px without centre text, and the chip column has `min-width: 0` and chips never wider than the column (`max-width: 100%`, `white-space: nowrap` at 11 px). The card sits at `inset-inline-start` ≈ 36 % with a 3° tilt.
- Implement it responsively, not with fixed pixels: the card width is `min(230px, 100% - 2 × 12px - mascot overlap)`, and the chip column is `flex: 1 1 auto; min-width: 0`. At ≤ 360 px the chips wrap to 2 lines rather than overflow. The rotated bounding box must stay inside the viewport.
- The ON hero (map disc) gets the same "inside the viewport" rule.
- Playwright test at 320, 360, 390 and 414 px (both variants, nl + ar): every element inside `.pub-landing__hero-scene` has `getBoundingClientRect()` within `[12, viewport − 12]` horizontally, and for each chip `scrollWidth <= clientWidth` (no clipped text).

## Tests
- `LandingVariantTests` (bUnit, OFF): every §V OFF section; the OFF HTML contains none of `/banenkaart`, `/banen`, `/vacancies`, `/werkgevers`, `/register`, `/partner`, `/westland`, `/candidate/match`, "vacature", "werkgever" (nl; and the en equivalents in en), except inside the explicit school/privacy sentences that say "alleen jij" (keep an allow-list of exact keys).
- Server rendering: the first response HTML for OFF is already the -zw variant (no ON text in the HTML at all). Playwright with `ForceVariant=zw` (CI) and, when A is present, with the real flag toggled via the admin API.
- `FeatureRoutes` tests per 07.2 (A present) or the `EmployersGate` tests (A absent): `/banenkaart` OFF → 302 "/", `/banen` OFF → 302 "/".
- Sitemap/robots/JSON-LD/ETag per 07.4.
- GratisDna OFF copy, no jobs teaser, OFF unlock list, "Past dit beroep?" labels.
- The chips test (07.7).
- `WerkgeversUitPlaywrightTests` updated (A present) so its "/" expectation is the -zw landing.

## Success criteria
- With the switch OFF, "/" matches lp-d1-zw / lp-m1-zw within the spec's differences, with **no flash**: the first HTML byte is already OFF.
- `curl -s /sitemap.xml` OFF contains no `/banenkaart`, `/werkgevers`, `/vacancies/`; ON contains them. The ETag changes on toggle.
- The mobile -zw hero shows every chip fully at 320–414 px.
- The `FeatureRoutes` change is either applied and tested (A present) or written down precisely in `docs/feature-flags-landing-followup.md` (A absent).

Done → next: `08-werkgevers-scholen-paginas.md`.
