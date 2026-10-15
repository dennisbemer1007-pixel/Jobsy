# Lobsy mascot asset brief

The public coral mascot is **inline SVG** in `LobsyMascotArt` / `<LobsyMascot />` (viewBox `0 0 128 128`). Art is **traced from the approved reference grid** (`docs/brand/kreeft-set-scharen-reference.png`, regen via `tools/brand/trace_mascot_reference.py`) — never hands or fingers.

## Expressions (production)

| Slug | Dutch | Typical use |
|---|---|---|
| `rustig` | Rustig | Markers, calm empty states, DNA ring centre |
| `uitleggen` | Uitleggen | Coach dock, tips, werkgever wizard, discovery coach |
| `nieuwsgierig` | Nieuwsgierig | “Diep duiken” tile, nieuwsgierige hints |
| `goed-gedaan` | Goed gedaan | Test/DNA result, career climb progress |
| `nadenken` | Nadenken | Loading, errors, empty talent list |
| `juichen` | Juichen | Hero, finish steps, celebrate dialogs |
| `shell` | Oud schild | Ghost shell (`pub-mascot--ghost` filter) |

Manifest: `Jobsy.Web/Media/MascotAssets.cs` (`MascotArtFormat.Inline`, version `20261015-coral-mascot`).

## Variants

- **Full** — default `LobsyMascot` (Tiny 40px … Hero 256px).
- **Avatar** — `Variant="Avatar"`; viewBox crop for chat/coach/PWA icon source.

## Raster (e-mail / OG only)

Optional PNG/WebP under `wwwroot/images/brand/mascot-{64,128,256}.png` generated via `tools/brand/render_mascot_assets.py`. PWA icons + favicon use avatar on navy `#0a2044`.

## Accessibility

Decorative: `Alt=""` → `aria-hidden`. Meaningful: pass translated `Alt` or `Culture[UiStringsMascot.AriaKey(pose)]` (5 languages in `UiStringsMascot`).

## Animation

Pose classes `pub-mascot--pose-*` — soft breathe / claw hint / bounce; disabled under `prefers-reduced-motion`.
