# Lobsy mascot asset brief

Standalone delivery brief for the Lobsy lobster mascot. Designers do not need to read application code. New art is a **file swap**: drop files in one folder, flip one manifest entry — no markup changes.

Today every pose falls back to the existing front-view mascot (`wwwroot/images/brand/mascot-{64,128,256}.webp`). CSS transforms approximate poses until real art arrives. **Do not generate art in the landing stack** — deliver files separately.

## Poses (required unless marked optional)

| Pose slug | Description | Used in |
|---|---|---|
| `default` | Today's pose: front view, both claws up, friendly smile. The pin tip at the bottom stays. | Map-tile home marker, small inline uses, fallback source for every pose |
| `waving` | Front/three-quarter view, one claw raised and waving, the other relaxed; open smile | Landing hero ON + OFF (desktop + mobile), test start (`/ontdek`), "Je eigen weg" tile, sign-up page |
| `diving` | Body tilted ~120–130°, claws forward as if swimming down; bubbles are **not** part of the art (the scene draws them) | "Diep duiken" tile |
| `sitting` | Sitting upright and calm, claws resting, antennae up and alert (listening) | "Antennes" and "De juiste rots" tiles; optional small placement on test question screens |
| `celebrating` | Both claws high, eyes happy, a little jump; no confetti in the art | Kreeft-visie story scene, "Je scharen" / "Van zacht naar sterk" tiles, test result "Dit ben jij" |
| `shell` *(optional)* | Only the empty old shell, same outline as `default`, one flat colour | "je oude schild" ghost. Without it, the ghost is `default` with CSS `brightness(0)` + low opacity |

## Style

- Same character as today's `mascot-256.webp`: purple lobster whose body ends in a map-pin tip, red/orange claws, big eyes. Keep proportions, colours and line weight consistent across poses.
- Sample colours from the current mascot. No text, no background, no drop shadow in the art (the public theme adds shadows and scenes).
- Canvas: **square 1:1**, transparent. Character centred horizontally with ~6 % padding. The pin tip (lowest point) sits at **94 % of the height** in every pose except `diving`, so poses can swap without layout shift.
- Must read well at 40 px (simple shapes; no fine detail that vanishes).

## Formats (option A or B per pose; A preferred)

Mixing A and B across poses is fine.

### A. SVG (preferred)

- One file per pose.
- `viewBox="0 0 512 512"`.
- No embedded raster (`<image>`), no `<script>`, no `<foreignObject>`, no external references, no fonts/text.
- ≤ **30 KB** gzipped, optimised with SVGO (keep `viewBox`).

### B. Raster

- **WebP and AVIF**, square, exported at **64, 128, 256 and 512** px wide (512 = 2× for a 256 CSS-px hero).
- Budgets: 512 ≤ 40 KB WebP / ≤ 30 KB AVIF; 256 ≤ 16 KB WebP / ≤ 12 KB AVIF.
- Plus one **PNG 256** per pose as fallback for old browsers and e-mail.

## File names and location

Folder: `Jobsy.Web/wwwroot/images/brand/mascot/`

| Format | Pattern | Example |
|---|---|---|
| A (SVG) | `mascot-{pose}.svg` | `mascot-waving.svg` |
| B (raster) | `mascot-{pose}-{width}.webp` / `.avif`, plus `mascot-{pose}-256.png` | `mascot-celebrating-512.avif`, `mascot-diving-128.webp` |

- Width ∈ `{64, 128, 256, 512}`.
- Lowercase, no spaces, **no version in the file name** (version goes in the manifest `?v=`).

## Display sizes and placement

CSS box sizes are tokens on `<LobsyMascot Size="…">`. Pages may override via `--pub-mascot-size`.

| Placement | Page / mockup | Pose | Size token | CSS px desktop / mobile | Priority |
|---|---|---|---|---|---|
| Hero ON | `/` lp-d1, lp-m1 | `waving` | `Hero` | 230 / 150 | **LCP: preload, `fetchpriority=high`, eager** |
| Hero OFF | `/` lp-d1-zw, lp-m1-zw | `waving` + `shell` ghost | `Hero` | 200 / 150 | preload |
| Story scene | `/#kreeft` | `celebrating` + `shell` ghost | `Large` | 200 / 140 | lazy |
| Mini scenes (6) | `/#kreeft` tiles | diving, sitting, sitting, celebrating (mirrored), default ×3 (growing 38/56/82), waving | `Small` / `Medium` | 38–82 | lazy |
| Map tile home marker | `/#wat-je-krijgt` (ON) | `default` | `Tiny` | 40 | lazy |
| Test start | `/ontdek` lp-d2 | `waving` | `Large` | 220 / 120 | eager |
| Test result | `/ontdek` lp-d4, lp-m3 | `celebrating` | `Medium` | 128 / 96 | eager |
| Sign-up | `/account-maken` | `waving` | `Medium` | 128 / 96 | eager |
| `/werkgevers`, `/scholen` hero | audience pages | `sitting` | `Large` | 200 / 120 | eager |

Size tokens → default CSS box / intrinsic attributes: `Tiny` 40, `Small` 64, `Medium` 128, `Large` 200, `Hero` 256.

## Delivery checklist (designer)

1. Deliver all **required** poses in A or B (optional: `shell`).
2. Check 40 px rendering of `default` and `waving`.
3. Put files in `Jobsy.Web/wwwroot/images/brand/mascot/` via a PR against a `docs/*` or `cursor/*` branch, **or** send them to Dennis for a developer to swap.

## For developers (swap procedure)

1. Put the delivered files in `wwwroot/images/brand/mascot/`.
2. In `Jobsy.Web/Media/MascotAssets.cs`, set that pose's `Format` to `Svg` or `Raster` and bump its `Version` (`YYYYMMDD-mascot-{pose}`).
3. Run `dotnet test --filter MascotAssets`: the guard checks files, size budgets and SVG safety.
4. **No `.razor` changes.** Fallback transform classes disappear automatically when `Format` is no longer `Fallback`.

Public pages render the mascot only through `<LobsyMascot />`. Do not reference `BrandImages.Mascot*` or `mascot-*.webp|png` directly under `PublicLayout` pages or `Components/Public/**`. Logged-in components (`LobsyCoachAvatar`, `LobsyLogo`, e-mail embeds) keep using `BrandImages` unchanged.
