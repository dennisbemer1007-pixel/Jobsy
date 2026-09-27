# Cursor prompt: Banenkaart v3, calmer markers, real isochrones, and a cluster popup that never jumps

## 0. Rules (read first)
- **Branch:** `git fetch origin && git checkout -b cursor/banenkaart-v3 origin/acceptatie`. The code references below are from `origin/acceptatie` @ `12ecd02` (2026-09-27 15:16 CEST). Re-check the line numbers, since they may have moved.
- **Exactly ONE pull request, into `acceptatie`.**
  - Do **not** merge it and do **not** deploy.
  - Do **not** use or trigger shortcut/rule **`123`**.
  - Never push to `acceptatie` or `main`. See `docs/release-flow.md`.
- **Scope: map styling + the map popup only.** That means:
  - travel-time areas (isochrones), cluster and pin styling, and ring labels
  - the cluster/single popup on the banenkaart (card internals, paging, mobile docking), and collapsing the map carousel while the popup is open
  - Do **not** touch filters, matching, the vacancy list, the pins/cards API contracts (apart from the new isochrone endpoint), the header, the bottom nav, or other pages.
  - Do **not** change how vacancies are filtered by travel time (`TravelReach`). This PR only changes what the map *shows*.
- **Reference images** are on branch `docs/banenkaart-v3`, folder `docs/mockups/banenkaart-v3/`. Get them with `git fetch origin docs/banenkaart-v3 && git checkout origin/docs/banenkaart-v3 -- docs/mockups/banenkaart-v3 docs/prompts/banenkaart-v3-prompt.md`, and include that folder in the PR.
  - `b3c-v3-kaart.png`: **the chosen map look ("V3 Sterk")**, map-only state.
  - `b3c-v3-popup.png`: **the chosen look with the docked popup open** (page 1/13) on mobile.
  - `b3-popup-1-pagina1.png`, `b3-popup-2-overgang.png`, `b3-popup-3-pagina2.png`: the **popup behaviour** frames (page 1 → slide transition → page 2 with a photo-less vacancy). These three frames use an older, lighter marker style; use the V3 markers and rings from `b3c-v3-*` for styling. Only take the popup behaviour from these frames.
  - `b3c-overzicht.png`: the current screenshot next to V1/V2/V3, for context only.
  - The mockup rings are drawn as **circles for illustration only**. The implementation must use **real isochrones** (§2).
  - All jobs, counts, wages and positions in the mockups are **examples**.
- **Keep Dennis's popup content design.** That means the same card content and order as the current `buildPopupHtml` (jobMap.js):
  - media on the start side, title, address, travel line
  - the green "xx% Match" chip with the `?` help button (and the wage info popover)
  - the wage "€ 15,10 /uur", then the company and the green **Solliciteer** button
  - Do not redesign it. Only change the frame around it (fixed height, pager row, docking) and remove the skeleton.
- **Design system** (`.cursor/rules/design-system.mdc`):
  - Use tokens (`--brand`, `--surface`, `--muted`, `--border`, `--pearl`, `--success`, …).
  - Font weights are 400/600.
  - Tap targets are at least **44×44px**.
  - Use logical properties so RTL works.
  - Respect `prefers-reduced-motion`.
- **Assets:**
  - Edit the readable sources (`wwwroot/js/jobMap.js`, `wwwroot/css/app.css`) **and** regenerate/mirror the served minified files (`jobMap.min.js`, `app.min.css`) in the same commit.
  - Bump `jobMap.min.js?v=` in **both** `wwwroot/js/app-core.js` (~line 494) and `wwwroot/js/maps-loader.js` (~line 16), and `app.min.css?v=` in `Components/App.razor` (the `<link>` and the `<noscript>` copy).
  - New map-only CSS may go in `wwwroot/css/features/banenkaart.css`, linked in `App.razor` like the other feature files.
- **Text:**
  - New user-facing strings (pager aria labels, "N vacatures in beeld", "N vacatures · {place}") go in nl/en/pl/ro/ar.
  - If jobMap.js has no label dictionary yet, pass the labels in from `VacancyDiscovery.razor` through the existing init/interop call. Don't add more hard-coded Dutch.
- **Work in this order** and commit per step (`step 1: …`) so each part can be reviewed and reverted on its own.

---

## Step 1: Cluster popup that never jumps (bug, do this first)

### Known causes (verified on `12ecd02`)
Dennis reports that paging through a cluster ("‹ 2 van 13 ›") jumps and looks poor. Measured on a 390px phone, the Solliciteer/pager buttons move by about **84px** per tap. Causes:
1. **Two different heights.**
   - The skeleton `.map-popup--skeleton .map-popup__media{min-width:112px;min-height:252px}` in `wwwroot/css/app.css` ~**4405** is much taller than the mobile compact card.
   - The mobile card is `@media (max-width:768px) .map-popup__main{min-height:168px;max-height:min(52vh,280px)}` in `app.css` ~**4972**.
   - The desktop cluster rule at ~4328 differs again.
2. **An async cache read on every page.** `renderClusterPage` (`jobMap.js` ~**633–686**) works like this:
   - It renders `buildClusterSingleHtml` (~618–631). That is `buildClusterPagerHtml` (~602–616) plus `skeletonPopupHtml` (~1023) when `job._detailLoaded` is false.
   - It calls `popup.update()`.
   - It then `fetchVacancyCards([id, nextId])` (~1171, cache in `detailCache` ~31/1154).
   - After that it calls `setHTML` again with the real card and `popup.update()` again.
   - So every tap does 2 full re-renders with 2 different heights. The whole innerHTML is replaced, pager included, so the buttons are re-created and focus is lost.
3. **Bottom anchoring.**
   - The cluster popup is a `maplibregl.Popup` anchored at the cluster (`clusterPopupOptions` ~180, `CLUSTER_POPUP_OFFSET` ~158).
   - It opens with `popupFromOpts(opts, ll, skeletonPopupHtml(firstJob))` in `openClusterList` (~1335–1349), then `centerPopupInView`.
   - Any height change moves the top of the popup *and*, because the pager sits above the body, the pager buttons.
   - On mobile the popup also lands over the map carousel (`.highlight-carousel--map`, `VacancyDiscovery.razor:579`; `mapFocusRect` ~915).

### Required behaviour (see `b3-popup-1/2/3` and `b3c-v3-popup.png`)
1. **Fixed-height card.** The cluster popup has one fixed height per breakpoint. There is no `min-height`/`max-height` range, and content never changes it:
   - The mockup uses 44px pager row + 3px progress bar + 152px content = **199px** on mobile.
   - Long titles/addresses/company names get one line with ellipsis.
   - Remove the 252px skeleton rule entirely.
2. **Pager row fixed in place.** The row is built **once** when the popup opens and is never re-rendered while paging:
   - On the start side: "**13 vacatures** · {place}". Place is the municipality/street of the cluster if already available on the pins, otherwise leave it out.
   - On the end side: `‹` · "2 / 13" · `›`, then a close `×`.
   - All four buttons are ≥44×44px. Use tabular numbers so the counter width doesn't change.
   - Under the row goes a thin progress bar (width = page/total).
   - Only the counter text, the progress width and the `disabled` state change. Use `aria-live="polite"` on the counter and aria labels "Vorige vacature" / "Volgende vacature" / "Vacature 2 van 13".
   - Paging keeps keyboard focus on the button you pressed.
3. **Content slides inside the card, with no skeleton.**
   - Only the content viewport below the pager changes. The next card is rendered into a second layer, then both layers animate with `transform: translateX()` + `opacity` (≈180–220ms, ease-out). Direction follows ‹/›.
   - The old layer is removed on `transitionend`.
   - With reduced motion: an instant swap or a 120ms crossfade.
   - Never call `popup.setHTML` for paging. Never render `map-popup--skeleton` for cluster paging.
   - Remove `skeletonPopupHtml` usage from the cluster flow. Keep it only if the single-pin flow (~1308) still needs it, and give that flow the same fixed height too.
4. **Prefetch the whole cluster.**
   - When a cluster is tapped, start `fetchVacancyCards` for **all** leaf ids at once. The cards endpoint caps at **25 ids** per request (`VacanciesController.ParseCardIds` ~1820), so send parallel batches of 25.
   - Show page 1 as soon as its card is in; that is the only moment a short wait is allowed.
   - During that first wait, show the pin data already present (title/company/match from the pins GeoJSON) inside the same fixed frame. That's not a shimmer skeleton.
   - After that, paging always renders from `detailCache` synchronously. If a page's card is still missing when tapped, show the pin data in the fixed frame and swap only the *text/image* in place when the card arrives. The frame height and button positions stay the same.
   - A failed card shows the existing "Vacature niet beschikbaar / Opnieuw proberen" content inside the same fixed frame.
5. **Photo-less vacancies** get a same-size media tile: navy gradient with a lock/briefcase icon, as in `b3-popup-3-pagina2.png`. They never get a narrower or taller layout. Images use fixed `width/height` + `object-fit:cover`, and preload the next 2 pages' images.
6. **Mobile docking (≤768px).**
   - The popup is a **docked sheet**: `position:absolute` inside `.map-pane`, 8px side insets, sitting just above the bottom nav (respect `env(safe-area-inset-bottom)`). It is not anchored to the cluster.
   - Use a plain DOM element, or keep `maplibregl.Popup` for desktop only.
   - When it opens:
     - **Pan the map** (not zoom) only if the tapped cluster would be hidden behind the sheet. Keep the cluster in the free band between the filter row and the sheet; update `mapFocusRect`/`overlayFitPadding` for the sheet height.
     - Give the tapped cluster a **selected state**: darker green plus a halo.
     - **Collapse the map carousel** into a small centred chip "**N vacatures in beeld**" (with a chevron) at the top of the map. Tapping the chip closes the popup and restores the carousel.
   - × closes the popup, as do tapping the map background or Escape. Closing restores the carousel.
   - The sheet never covers the filter row, and the map controls stay reachable.
   - Desktop (>768px) may keep the anchored popup, but with the same fixed-height card, fixed pager and slide behaviour.
7. **Optional swipe:** horizontal swipe on the content viewport pages as well, with snap and a 30% threshold. It must not start a map pan (`touch-action: pan-y` on the viewport). The arrows stay.
8. **Tests:**
   - Update `Jobsy.Tests/VacancyMapProxyAndCardTests.cs:123`, which currently asserts `map-popup--skeleton` exists in the JS. Assert instead that cluster paging does not render it.
   - Add the Playwright checks in §4.

## Step 2: Real isochrones instead of circles

**Current:** the "rings" are circles.
- `drawTravelRings` (`jobMap.js` ~1560) builds `circlePolygon(lat,lng,radius)` (~1463) from `metersPerMinute` (`CRUISE_KM_H` / `ROAD_CIRCUITY`, ~58–71; kept in sync with `Jobsy.Core/Rules/TravelReach.cs`).
- The minutes come from `ringMinutes` (~1351).
- The labels are a symbol layer at bearing 125° (`placeTravelRingLabels` ~1500).

**Required:** draw real travel-time areas.
1. **Backend endpoint.**
   - Add `GET /api/travel/isochrones?lat=&lng=&mode=Fiets|Auto|Lopend|OV&minutes=10,20,30` in `Jobsy.Api`, proxied through `Jobsy.Web/Hosting/VacancyMapProxyEndpoints.cs` like `/api/vacancies/pins`.
   - It returns a GeoJSON FeatureCollection with one `Polygon/MultiPolygon` per minute value (`properties.minutes`), largest first.
   - Put it behind a new interface `IIsochroneService` in Infrastructure, next to `OsrmRoutingService`.
2. **Provider.**
   - Use **Valhalla** `/isochrone` with `costing` = `bicycle` / `auto` / `pedestrian`, `contours=[{time:10},{time:20},{time:30}]`, `polygons=true`, `denoise≈0.5`, `generalize≈50`.
   - The base URL goes in config as `Routing:IsochroneBaseUrl`, defaulting to the public FOSSGIS instance `https://valhalla1.openstreetmap.de`. That instance is fair-use, so it must be cached; self-hosting stays possible by changing the URL.
   - I checked on 2026-09-27: a bike request for Monster (52.022, 4.172) with 10/20/30 min returned 3 polygons in ~1s.
   - Use a named `HttpClient` with a timeout of about 6s, set a `User-Agent`, and add a mock implementation for tests/dev (reuse the `MockRoutingService` pattern).
3. **Caching.**
   - Round the origin to about 100m (3 decimals). The key is `(lat3, lng3, mode, minutes)`.
   - Cache in `IMemoryCache` plus the existing distributed/DB cache if there is one, for at least 7 days.
   - Rate-limit the endpoint (`public-read` policy).
   - The client caches per origin+mode, so filter toggles don't refetch.
4. **Fallback.**
   - If the provider fails or times out, and for **OV** (no transit isochrones on the public instance), fall back to the current circle polygons (`circlePolygon` via `ringRadiusForMinutes`).
   - Draw the circles first and swap in the isochrones when they arrive (`setData`, no flash). Log fallbacks.
   - Respect the existing `radiusKm` cap by clipping to the cap circle, or just keep the cap circle as the outer bound.
5. **Which rings.**
   - Always show **3 areas**. For a chosen max ≤30, show 10/20/30. Otherwise use `ringMinutes(max)`.
   - The area equal to the filter's `travelOptions.maxMinutes` is the **chosen** one. When the chosen time is 10 or 20, the larger areas are still drawn, lighter (see the styling below).
   - Keep the fit logic (`fitToOriginRings` ~1650) based on the chosen area's bounds.

## Step 3: Map styling "V3 Sterk" (see `b3c-v3-kaart.png`)
1. **Layer order:** base map → isochrone fills → isochrone halo/lines → ring labels → clusters/pins → selected. The ring layers must never capture clicks.
2. **Isochrone fills:** a graduated fill, darkest inside.
   - Stack the three polygons with `fill-color:#2563eb` and `fill-opacity` **0.07** (30 min), **0.11** (20), **0.16** (10). Alternatively use donut bands with the equivalent result: ≈0.30 / 0.17 / 0.07.
   - Replace the current single `fill-opacity:0.16` on every ring (~1601).
3. **Lines:**
   - `#1d4ed8`, 3px, opacity 1.
   - The **chosen** area is 5px on top of a white 8px halo (opacity 0.8).
   - Remove the old `outer` 3.2/2.4 rule.
4. **Base map:** mute it a little (the mockup uses `saturate(.28) brightness(1.04) contrast(.92)`) so the rings dominate.
   - Do this by adjusting the style's paint properties after load (desaturate background/landuse/water/road colours), **not** with a CSS filter on the canvas, which would also mute the markers.
   - The 3D toggle keeps working.
5. **Ring labels:** pill labels **on** each area's edge.
   - Use HTML `maplibregl.Marker`s with `pointer-events:none`, or a symbol layer with a pill image.
   - Normal labels: white background, 1.5px `#1d4ed8` border, 12px/600 `#1d4ed8` text "10 min".
   - The chosen label: filled `#1d4ed8`, white 13px/600 text with the transport icon (bike/car/walk/OV) plus "20 min", a 2px white outline and a soft shadow.
   - Placement: take the point on the polygon boundary along a preferred bearing per ring. The mockup uses about 30° (10 min), 190° (20), 160° (30).
   - Then nudge the label along the edge until it doesn't overlap a cluster, the map controls, the carousel/chip, or the docked sheet.
   - Keep the existing text "min fietsen/rijden/lopen/OV" in the aria/title.
6. **Clusters** (`PIN_LAYER_CLUSTERS` / `PIN_LAYER_CLUSTER_COUNT`, ~2037–2068):
   - Filled green `#16a34a` with a **white count** (Noto Sans Bold 13–14px) and a 2px white stroke.
   - Add a **soft halo**: a second circle layer below with radius +5px, `#16a34a` at 0.22 opacity, blur 0.4. That's calmer than today's glow.
   - Radius steps: 15 (<10), 17 (10–29), 20 (≥30).
   - **Selected cluster:** `#0f5f2e` with a 5px halo at 0.30 (feature-state `selected`).
7. **Single pins** (`PIN_LAYER_UNCLUSTERED` ~2071): keep the colour logic (match colour / featured / selected coral), but make them smaller and flatter: radius 7 (selected 10), 2px white stroke, no glow except for featured.
8. **Hit targets:** keep clicks working on **all** clusters and pins.
   - Add transparent hit layers (radius ≥22px) under the visible circles, or use `queryRenderedFeatures` with a 12px bbox, so small pins still hit 44px.
   - Keep the `lastClusterTapAt` race guard (~775, ~2150).

## Step 4: Checks (all must pass before the PR is ready)
Run these with Playwright at **360, 390 and 430px** width (mobile emulation, touch) and at 1280px desktop, on seeded data with at least one cluster of ≥13 vacancies and one photo-less vacancy:
1. **Zero layout shift while paging.**
   - Open the cluster and tap › 12 times, then ‹ 12 times.
   - After each tap (wait for `transitionend`), record the `getBoundingClientRect()` of `‹`, `›`, `×` and the popup root.
   - **Required: 0px movement** of every button and a **constant popup height** at all 3 widths.
2. **Zero skeleton frames.** Install a `MutationObserver` before opening the cluster and count inserted `.map-popup--skeleton` / `[aria-busy=true]` nodes during paging. **Required: 0.**
3. **Prefetch.** After opening a cluster of n vacancies, count network requests to `/api/vacancies/cards`: at most ⌈n/25⌉ during open, and **0** during paging.
4. **Smoothness (60fps).**
   - Under CDP CPU throttling 4×, record `requestAnimationFrame` deltas during 5 page transitions and one map pan with rings visible.
   - Required: median frame ≤17ms and no frame >50ms.
   - Animate only `transform`/`opacity`. No layout-triggering properties.
5. **Everything clickable.**
   - At each width, use `queryRenderedFeatures` to get every visible cluster and pin, click its projected centre, and assert that the correct popup opens: a cluster opens the pager, a pin opens its card.
   - Include pins next to a ring label and next to the controls.
6. **Docking and carousel.**
   - On mobile the popup's bottom is directly above the bottom nav, and it doesn't overlap the filter row.
   - The carousel is hidden while the popup is open and the "N vacatures in beeld" chip is visible.
   - The tapped cluster stays visible above the sheet.
   - Closing restores the carousel.
7. **Isochrones.**
   - With the mock provider: 3 polygons drawn and the chosen line is wider.
   - With the provider failing: the circles fallback is drawn and there's no console error.
   - OV uses the fallback.
   - Cache: a second request for the same origin doesn't hit the provider.
8. Add screenshots at 390px of the map-only state and the popup-open state to the PR description, next to `b3c-v3-kaart.png` and `b3c-v3-popup.png`.
9. `dotnet build` and `dotnet test` must be green. Also run the existing `BanenkaartMapReusePlaywrightTests`.

## Done means
One PR into `acceptatie`, titled "Banenkaart v3: V3 markers, real isochrones, stable docked cluster popup", with steps 1–3 as separate commits and the §4 results (numbers) in the description. Not merged and not deployed.
