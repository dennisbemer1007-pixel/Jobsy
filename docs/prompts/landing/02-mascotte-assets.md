# 02. Mascot: one `LobsyMascot` component, asset manifest with fallback, and the asset contract (no art)

Read `00-README.md` first. Branch `cursor/landing-2` from `cursor/landing-1` (or `-1b`).

> **Rules (same as README §0, repeated on purpose):**
> - Never merge, never deploy, never use rule `123` (`.cursor/rules/shortcut-123.mdc`).
> - Never push to `main` or `acceptatie`; push only this file's `cursor/landing-*` branch; no force-push.
> - ONE stacked PR into `acceptatie`.
> - Red tests or an unmet success criterion: push, open the PR as **draft**, stop and report. Don't start the next file.
> - **Don't generate, draw, trace or AI-generate mascot art** in this stack. Only the contract, the component and the fallback.

| | |
|---|---|
| Branch | `cursor/landing-2` |
| PR title | `feat(landing): LobsyMascot component + mascot asset contract (fallback to current mascot)` |
| PR body starts with | `Stacked on #<PR 01> (cursor/landing-1)` |
| Mockups | `lp-d1-landing.png` (hero, kreeft-visie story scene and the 6 mini scenes), `lp-d1-landing-zw.png` (hero with "je oude schild"), `lp-d2-test-start.png`, `lp-m1-hero.png`, `lp-m1-hero-zw.png`; sources `w_common.py` (`mascot()`, `ghost()`, `SCENES`), `w_hero.py`, `zw_scene.py` |
| Split seam | none (small file) |

## Goal
Every public page shows the mascot through **one** component. Today it renders the existing mascot, with CSS transforms approximating each pose exactly as the mockups do. When a designer delivers the art, it's a **file swap**: drop the files in one folder, flip one manifest entry, and no markup changes. The contract says exactly which files to deliver.

## 02.1 Today (verify first)
- `Jobsy.Web/Media/BrandImages.cs`: `MascotWebp64/128/256`, `MascotPng128`, `MascotSrcSet56`, version `MascotVersion = "20260828-mascot"`. Files in `wwwroot/images/brand/`: `mascot-64.webp|png`, `mascot-128.webp|png`, `mascot-256.webp`. The largest is **256 px**, one pose (front, claws up). That's too small for a 230 CSS-px hero on dpr-2 screens.
- `Components/LobsyCoachAvatar.razor` and `LobsyLogo.razor` use `BrandImages`. `App.razor` ~L150–184 has a script that swaps mascot/logo WebP → PNG on error.
- In the mockups the mascot appears as: hero (waving), story scene (celebrating, with a dark ghost of the old shell), 6 mini scenes (diving, listening/sitting, sitting on a rock, claws/celebrating mirrored, growing small→big, walking/waving), test start (waving), and a small home marker on the map tile.

## 02.2 The asset contract: write `docs/brand/mascot-assets.md`
Write this as a standalone brief a designer can follow without reading code. It contains exactly:

### Poses (required unless marked optional)
| Pose slug | Description | Used in |
|---|---|---|
| `default` | Today's pose: front view, both claws up, friendly smile. The pin tip at the bottom stays. | map-tile home marker, small inline uses, fallback source for every pose |
| `waving` | Front/three-quarter view, one claw raised and waving, the other relaxed; open smile | landing hero ON + OFF (desktop + mobile), test start (lp-d2), "Je eigen weg" tile, sign-up page |
| `diving` | Body tilted ~120–130°, claws forward as if swimming down; a few bubbles are **not** part of the art (the scene draws them) | "Diep duiken" tile |
| `sitting` | Sitting upright and calm, claws resting, antennae up and alert (listening) | "Antennes" and "De juiste rots" tiles, test question screens (small, optional placement) |
| `celebrating` | Both claws high, eyes happy, a little jump; no confetti in the art | kreeft-visie story scene, "Je scharen" and "Van zacht naar sterk" tiles, test result "Dit ben jij" (lp-d4/m3) |
| `shell` *(optional)* | Only the empty old shell, same outline as `default`, one flat colour | "je oude schild" ghost. Without it, the ghost is `default` with CSS `brightness(0)` + low opacity, as in the mockups |

### Style
- Same character as today's `mascot-256.webp`: the purple lobster whose body ends in a map-pin tip, with red/orange claws and big eyes. Keep proportions, colours and line weight consistent across poses.
- Colours: sample from the current mascot. No text, no background, no drop shadow in the art (the theme adds shadows and scenes).
- Canvas: **square 1:1**, transparent. The character is centred horizontally with ~6 % padding. The pin tip (lowest point) sits at **94 % of the height** in every pose except `diving`, so poses swap without layout shift.
- Must read well at 40 px (simple shapes, no fine detail that vanishes).

### Formats (either option A or B per pose; A preferred)
- **A. SVG** (preferred): one file per pose. `viewBox="0 0 512 512"`, no embedded raster (`<image>`), no `<script>`, no `<foreignObject>`, no external references, no fonts/text, ≤ **30 KB** gzipped, optimised with SVGO (keep `viewBox`).
- **B. Raster:** **WebP and AVIF**, square, exported at **64, 128, 256 and 512 px** wide (512 = the 2× file for the 256 px hero display size). Budgets: 512 ≤ 40 KB WebP / ≤ 30 KB AVIF; 256 ≤ 16 KB WebP / ≤ 12 KB AVIF. Plus one **PNG 256** per pose as a fallback for old browsers and e-mail.

### File names and location
`Jobsy.Web/wwwroot/images/brand/mascot/`
- A: `mascot-{pose}.svg`
- B: `mascot-{pose}-{width}.webp`, `mascot-{pose}-{width}.avif`, `mascot-{pose}-256.png` (width ∈ 64, 128, 256, 512)
- Examples: `mascot-waving.svg`, `mascot-celebrating-512.avif`, `mascot-diving-128.webp`.
- Lowercase, no spaces, no version in the name (the version goes in the manifest's `?v=`).

### Display sizes (CSS px, square) and placement map
| Placement | Page / mockup | Pose | Size token | CSS px desktop / mobile | Priority |
|---|---|---|---|---|---|
| Hero ON | `/` lp-d1, lp-m1 | `waving` | `Hero` | 230 / 150 | **LCP candidate: preload, `fetchpriority=high`, eager** |
| Hero OFF | `/` lp-d1-zw, lp-m1-zw | `waving` + `shell` ghost | `Hero` | 200 / 150 | preload |
| Story scene | `/#kreeft` | `celebrating` + `shell` ghost | `Large` | 200 / 140 | lazy |
| Mini scenes (6) | `/#kreeft` tiles | diving, sitting, sitting, celebrating (mirrored), default ×3 (growing 38/56/82), waving | `Small`/`Medium` | 38–82 | lazy |
| Map tile home marker | `/#wat-je-krijgt` (ON) | `default` | `Tiny` | 40 | lazy |
| Test start | `/ontdek` lp-d2 | `waving` | `Large` | 220 / 120 | eager |
| Test result | `/ontdek` lp-d4, lp-m3 | `celebrating` | `Medium` | 128 / 96 | eager |
| Sign-up | `/account-maken` | `waving` | `Medium` | 128 / 96 | eager |
| `/werkgevers`, `/scholen` hero | 08 | `sitting` | `Large` | 200 / 120 | eager |

### Delivery checklist (for the designer)
1. Deliver all required poses in A or B (mixing per pose is fine).
2. Check the 40 px rendering of `default` and `waving`.
3. Put the files in the folder above via a PR against a `docs/*` or `cursor/*` branch, **or** send them to Dennis; a developer does the swap (02.5).

## 02.3 `MascotAssets` manifest (`Jobsy.Web/Media/MascotAssets.cs`)
- `enum MascotPose { Default, Waving, Diving, Sitting, Celebrating, Shell }`.
- `enum MascotArtFormat { Fallback, Svg, Raster }`.
- `record MascotArt(MascotPose Pose, MascotArtFormat Format, string Version)`, one static entry per pose. **Every entry starts as `Fallback`.**
- `MascotAssets.Resolve(pose)` → a `MascotSource` with: `Src`, `SrcSet` (WebP widths), `AvifSrcSet` (null unless raster), `IsVector`, `IsFallback`, `FallbackTransformClass`.
  - Fallback = `BrandImages.MascotWebp*` srcset (64/128/256) + `FallbackTransformClass = "pub-mascot--fb-{pose}"`.
  - `Shell` without art = Default fallback + class `pub-mascot--ghost`.

## 02.4 `LobsyMascot` component (`Components/Brand/LobsyMascot.razor`)
- Parameters: `Pose` (default `Default`), `Size` (`Tiny` 40, `Small` 64, `Medium` 128, `Large` 200, `Hero` 256: the CSS box, overridable per breakpoint by the page's CSS via `--pub-mascot-size`), `Mirror` (bool), `Priority` (bool: eager + `fetchpriority="high"`, else `loading="lazy" decoding="async"`), `Alt` (default empty = decorative, `aria-hidden="true"`), `Class`.
- Output:
  - **SVG:** `<img src="…svg?v=" width height alt>`
  - **Raster:** `<picture><source type="image/avif" srcset sizes><img src="…-256.webp" srcset sizes width height alt></picture>`
  - **Fallback:** `<img>` with today's WebP srcset + `pub-mascot--fb-{pose}` class
  - Always with intrinsic `width`/`height` (no CLS) and `sizes` derived from `Size`.
- CSS (in `public-theme.css`): `.pub-mascot` box + `--mirror` (`scaleX(-1)`, flips back under RTL), `--ghost` (`filter: brightness(0); opacity: .09`), and the fallback pose approximations from the mockup sources, e.g. `--fb-diving { transform: rotate(128deg); }`, `--fb-celebrating` (small bounce keyframe, off under `prefers-reduced-motion`), `--fb-sitting`, `--fb-waving` (gentle wave keyframe on the whole image, reduced-motion off). These classes are applied **only** when `IsFallback`.
- `LobsyMascot.PreloadLink(pose, size)`: a static helper returning the `<link rel="preload" as="image" imagesrcset imagesizes fetchpriority="high">` markup for `HeadContent` (05 uses it for the hero).
- The component works in static SSR and interactive modes (no JS).
- Leave `LobsyCoachAvatar`, `LobsyLogo`, e-mail images and `BrandImages` as they are (the app uses them). Public pages use **only** `LobsyMascot`.

## 02.5 The swap procedure (document in `docs/brand/mascot-assets.md` → "For developers")
1. Put the delivered files in `wwwroot/images/brand/mascot/`.
2. In `MascotAssets.cs`, set that pose's `Format` to `Svg` or `Raster` and bump its `Version` (`YYYYMMDD-mascot-{pose}`).
3. Run `dotnet test --filter MascotAssets`: the guard checks files, sizes and SVG safety.
4. No `.razor` changes. The fallback transform class disappears automatically.

## Tests
- `MascotAssetsGuardTests`:
  - every `Svg` entry has `mascot-{pose}.svg` that parses as XML, has a `viewBox`, contains no `<script>`, `<image>`, `<foreignObject>`, `href="http` or `xlink:href="http`, and is ≤ 30 KB gzipped
  - every `Raster` entry has all 4 widths in WebP + AVIF + the PNG 256, within the budgets, and square (read the header dimensions)
  - `Fallback` entries need no files (passes today)
- `LobsyMascotBunitTests`: fallback markup (srcset of today's files, `pub-mascot--fb-waving`, width/height, `aria-hidden`, lazy vs `Priority`); with a test manifest set to Svg/Raster → `<img …svg>` / `<picture>` with an AVIF source and **no** fallback class; `Mirror` class; `Alt` makes it non-decorative.
- `PublicMascotUsageGuardTests`: no `.razor` under `PublicLayout` pages or `Components/Public/**` references `mascot-*.webp|png` or `BrandImages.Mascot*` directly (only `LobsyMascot`).
- CSS guard from 01 stays green (fallback classes live under `.pub-theme`).

## Success criteria
- `docs/brand/mascot-assets.md` exists and fully answers: which poses, which formats, which sizes, which file names, where each is used, how to deliver, how to swap.
- `<LobsyMascot Pose="MascotPose.Waving" Size="MascotSize.Hero" Priority="true" />` renders today's mascot with the waving approximation, no CLS, and with a preload link helper.
- Adding a fake SVG in a test manifest switches the output with zero markup changes.
- No new image files are added in this PR.

Done → next: `03-kandidaat-account.md`.
