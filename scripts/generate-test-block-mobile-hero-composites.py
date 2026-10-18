#!/usr/bin/env python3
"""Pre-composite mobile test-block hero bands (390×200 logical, 780×400 asset)."""
from __future__ import annotations

from collections import deque
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
JPG_DIR = ROOT / "Jobsy.Web/wwwroot/images/tests"
OUT_DIR = JPG_DIR / "figures"

MOB_W = 780
MOB_H = 400

SLUGS = ("hoe-je-werkt", "leuk", "sfeer", "waarden")

HERO_REGION = {
    # r9: drop leftmost partial figure — no hard vertical slice at text column.
    "hoe-je-werkt": (0.72, 0.0, 1.0, 1.0),
    "leuk": (0.0, 0.0, 0.82, 0.92),
    # r9: skip window-frame band above/left of desk scene.
    "sfeer": (0.10, 0.14, 0.34, 1.0),
    "waarden": (0.66, 0.34, 1.0, 1.0),
}

# Approximate mobile --test-block-hero-cream (matches tests.css color-mix intent).
BLOCK_HERO_RGB = {
    "hoe-je-werkt": (232, 238, 246),
    "leuk": (248, 236, 230),
    "sfeer": (241, 234, 226),
    "waarden": (236, 242, 236),
}


def _sample_block_color(slug: str) -> tuple[int, int, int]:
    src = JPG_DIR / f"bg-{slug}.jpg"
    if src.is_file():
        rgb = Image.open(src).convert("RGB")
        pts = [(6, 6), (rgb.width - 7, 6), (6, rgb.height - 7)]
        rs = gs = bs = 0
        for px, py in pts:
            r, g, b = rgb.getpixel((px, py))
            rs += r
            gs += g
            bs += b
        n = len(pts)
        sampled = (rs // n, gs // n, bs // n)
        target = BLOCK_HERO_RGB.get(slug, sampled)
        # Blend sampled JPG corners with token so blocks stay on-brand.
        return (
            int(sampled[0] * 0.35 + target[0] * 0.65),
            int(sampled[1] * 0.35 + target[1] * 0.65),
            int(sampled[2] * 0.35 + target[2] * 0.65),
        )
    return BLOCK_HERO_RGB[slug]


def _matches_bg(rgb, bg: tuple[int, int, int], tol: int) -> bool:
    return max(abs(int(rgb[0]) - bg[0]), abs(int(rgb[1]) - bg[1]), abs(int(rgb[2]) - bg[2])) <= tol


def _flood_key_figure(arr, bg: tuple[int, int, int], tol: int = 30):
    """Remove background connected to image edges; keep people/plants fully opaque."""
    import numpy as np

    h, w = arr.shape[:2]
    rgb = arr[:, :, :3]
    match = np.zeros((h, w), dtype=bool)
    for y in range(h):
        for x in range(w):
            if _matches_bg(rgb[y, x], bg, tol):
                match[y, x] = True

    bg_mask = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if match[y, x]:
                bg_mask[y, x] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if match[y, x] and not bg_mask[y, x]:
                bg_mask[y, x] = True
                q.append((x, y))

    while q:
        x, y = q.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and not bg_mask[ny, nx] and match[ny, nx]:
                bg_mask[ny, nx] = True
                q.append((nx, ny))

    arr[bg_mask, 3] = 0
    person = (~bg_mask) & (arr[:, :, 3] > 0)
    arr[person, 3] = 255
    delta = np.abs(rgb.astype(np.int16) - np.array(bg, dtype=np.int16)).max(axis=2)
    person_core = person & (delta > 46)
    return arr, person_core


def _dilate_bool(mask, r: int):
    import numpy as np

    if r <= 0:
        return mask
    out = mask.copy()
    h, w = mask.shape
    ys, xs = np.where(mask)
    for y, x in zip(ys, xs):
        y0, y1 = max(0, y - r), min(h, y + r + 1)
        x0, x1 = max(0, x - r), min(w, x + r + 1)
        out[y0:y1, x0:x1] = True
    return out


def _remove_alpha_islands(arr, person_core, min_area: int = 96):
    """Drop tiny speckles not attached to the main figure."""
    import numpy as np

    alpha = arr[:, :, 3]
    h, w = alpha.shape
    fg = alpha > 48
    visited = np.zeros((h, w), dtype=bool)
    anchor = _dilate_bool(person_core, 10)

    for sy in range(h):
        for sx in range(w):
            if not fg[sy, sx] or visited[sy, sx]:
                continue
            q: deque[tuple[int, int]] = deque([(sx, sy)])
            visited[sy, sx] = True
            pixels: list[tuple[int, int]] = []
            touches_anchor = anchor[sy, sx]
            while q:
                x, y = q.popleft()
                pixels.append((x, y))
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if 0 <= nx < w and 0 <= ny < h and fg[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        if anchor[ny, nx]:
                            touches_anchor = True
                        q.append((nx, ny))
            if not touches_anchor and len(pixels) < min_area:
                for x, y in pixels:
                    arr[y, x, 3] = 0


def _trim_partial_left_columns(arr, person_core, max_cols: int = 48, min_col_fill: float = 0.68):
    """Remove a hard vertical slice from a partially visible person at the left edge."""
    import numpy as np

    h = arr.shape[0]
    alpha = arr[:, :, 3]
    for x in range(min(max_cols, arr.shape[1])):
        col = alpha[:, x] > 48
        if col.sum() == 0:
            break
        if col.sum() / h >= min_col_fill:
            break
        alpha[:, x] = 0
        person_core[:, x] = False


def _antialias_fringe(arr, person_core):
    """1–2px soft edge on plants/ground; person_core stays fully opaque."""
    import numpy as np
    from PIL import Image, ImageFilter

    a = arr[:, :, 3].astype(np.float32)
    pil_a = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), mode="L")
    blurred = np.array(pil_a.filter(ImageFilter.GaussianBlur(radius=1.15)), dtype=np.float32)
    fringe = (a > 0) & ~person_core
    merged = np.where(person_core, 255.0, np.where(fringe, np.minimum(a, blurred), a))
    merged[merged < 36] = 0
    arr[:, :, 3] = np.clip(merged, 0, 255).astype(np.uint8)
    arr[person_core, 3] = 255


def _feather_ground_only(arr, person_mask, bg: tuple[int, int, int], ground_frac: float = 0.24):
    """Fade only low-contrast ground/plants in the bottom band — never person pixels."""
    import numpy as np

    h, w = arr.shape[:2]
    y0 = int(h * (1.0 - ground_frac))
    rgb = arr[:, :, :3]
    bg_arr = np.array(bg, dtype=np.int16)

    for y in range(y0, h):
        t_row = (y - y0) / max(1, h - y0 - 1)
        for x in range(w):
            if person_mask[y, x]:
                continue
            if arr[y, x, 3] == 0:
                continue
            delta = np.abs(rgb[y, x].astype(np.int16) - bg_arr).max()
            if delta > 52:
                continue
            fade = t_row * t_row
            arr[y, x, 3] = int(255 * (1.0 - fade * 0.85))


def load_cutout_figure(slug: str, block_bg: tuple[int, int, int]) -> tuple[Image.Image, Image.Image]:
    import numpy as np

    hero_path = OUT_DIR / f"{slug}-hero.png"
    if not hero_path.is_file():
        raise FileNotFoundError(f"Missing {hero_path}; run generate-test-block-hero-figures.py first")

    hero = Image.open(hero_path).convert("RGBA")
    bbox = hero.getbbox()
    if not bbox:
        raise RuntimeError(f"Empty hero figure for {slug}")
    hero = hero.crop(bbox)

    x0f, y0f, x1f, y1f = HERO_REGION.get(slug, (0.0, 0.0, 1.0, 1.0))
    w, h = hero.size
    region = (
        int(w * x0f),
        int(h * y0f),
        max(int(w * x0f) + 1, int(w * x1f)),
        max(int(h * y0f) + 1, int(h * y1f)),
    )
    figure = hero.crop(region)
    arr = np.array(figure, copy=True)
    arr, person_core = _flood_key_figure(arr, block_bg, tol=32)
    if slug == "hoe-je-werkt":
        _trim_partial_left_columns(arr, person_core)
    _remove_alpha_islands(arr, person_core)
    _feather_ground_only(arr, person_core, block_bg)
    _antialias_fringe(arr, person_core)

    person_mask = person_core
    figure = Image.fromarray(arr, mode="RGBA")
    mask = Image.fromarray((person_mask.astype(np.uint8) * 255), mode="L")
    bbox = figure.getbbox()
    if not bbox:
        raise RuntimeError(f"No opaque pixels in hero crop for {slug}")
    figure = figure.crop(bbox)
    mask = mask.crop(bbox)
    return figure, mask


def _apply_text_figure_gradient(canvas: Image.Image, block_bg: tuple[int, int, int]) -> None:
    import numpy as np

    arr = np.array(canvas.convert("RGBA"))
    w = arr.shape[1]
    fade_end = int(w * 0.58)
    for x in range(fade_end):
        t = 1.0 - (x / max(1, fade_end))
        strength = t * t * 0.55
        if strength <= 0.01:
            continue
        for y in range(arr.shape[0]):
            r, g, b, a = arr[y, x]
            if a < 255:
                continue
            nr = int(r * (1 - strength) + block_bg[0] * strength)
            ng = int(g * (1 - strength) + block_bg[1] * strength)
            nb = int(b * (1 - strength) + block_bg[2] * strength)
            arr[y, x] = (nr, ng, nb, 255)
    canvas.paste(Image.fromarray(arr, mode="RGBA"))


def assert_no_hard_block_seams(path: Path, block_bg: tuple[int, int, int]) -> None:
    import numpy as np

    arr = np.array(Image.open(path).convert("RGB"))
    h, w = arr.shape[:2]
    lum = (
        0.299 * arr[:, :, 0].astype(np.float32)
        + 0.587 * arr[:, :, 1].astype(np.float32)
        + 0.114 * arr[:, :, 2].astype(np.float32)
    )

    def near_block(px) -> bool:
        return _matches_bg(px, block_bg, 24)

    x_start = max(1, int(w * 0.22))
    for x in range(x_start, w):
        seam = 0
        for y in range(h):
            if abs(lum[y, x] - lum[y, x - 1]) <= 8:
                continue
            if near_block(arr[y, x - 1]) and near_block(arr[y, x]):
                seam += 1
        if seam >= max(28, int(h * 0.22)):
            raise RuntimeError(f"{path.name}: vertical block seam at x={x} ({seam} rows)")

    y_start = max(1, int(h * 0.08))
    x_lo, x_hi = int(w * 0.22), int(w * 0.92)
    for y in range(y_start, h):
        seam = 0
        for x in range(x_lo, x_hi):
            if abs(lum[y, x] - lum[y - 1, x]) <= 8:
                continue
            if near_block(arr[y - 1, x]) and near_block(arr[y, x]):
                seam += 1
        if seam >= max(36, int((x_hi - x_lo) * 0.28)):
            raise RuntimeError(f"{path.name}: horizontal block seam at y={y} ({seam} cols)")


def assert_person_mask_opaque(mask_path: Path) -> None:
    import numpy as np

    m = np.array(Image.open(mask_path).convert("L"))
    ys, xs = np.where(m > 64)
    if len(xs) == 0:
        raise RuntimeError(f"{mask_path.name}: empty person mask")
    # Source mask is 255 on person; after resize it should remain fully opaque in composite check file.
    if (m[m > 64] < 255).any():
        raise RuntimeError(f"{mask_path.name}: person mask has non-opaque person pixels")


def composite_mobile_hero(slug: str) -> None:
    block_bg = _sample_block_color(slug)
    canvas = Image.new("RGBA", (MOB_W, MOB_H), block_bg + (255,))
    figure, person_mask = load_cutout_figure(slug, block_bg)

    target_w = int(MOB_W * 0.45)
    target_h = int(MOB_H * 0.94)
    max_w = int(MOB_W * (0.60 if slug == "leuk" else 0.48))
    scale = min(max_w / figure.width, target_h / figure.height)
    new_w = max(1, int(figure.width * scale))
    new_h = max(1, int(figure.height * scale))
    figure = figure.resize((new_w, new_h), Image.Resampling.LANCZOS)
    person_mask = person_mask.resize((new_w, new_h), Image.Resampling.NEAREST)

    import numpy as np

    fig_arr = np.array(figure, copy=True)
    pm = np.array(person_mask) > 127
    fig_arr[pm, 3] = 255
    figure = Image.fromarray(fig_arr, mode="RGBA")
    if pm.any() and (fig_arr[:, :, 3][pm] < 255).any():
        bad = int((fig_arr[:, :, 3][pm] < 255).sum())
        raise RuntimeError(f"{slug}: {bad} person pixels have alpha<255 before paste")

    cutout_dest = OUT_DIR / f"{slug}-mobhero-figure-cutout.png"
    mask_dest = OUT_DIR / f"{slug}-mobhero-person-mask.png"
    figure.save(cutout_dest, optimize=True)
    Image.fromarray((pm.astype(np.uint8) * 255), mode="L").save(mask_dest, optimize=True)

    margin_x = 16 if slug == "hoe-je-werkt" else (8 if slug == "leuk" else 10)
    margin_y = 0 if slug == "leuk" else 6
    x = MOB_W - new_w - margin_x
    y = MOB_H - new_h - margin_y
    canvas.paste(figure, (x, y), figure)

    _apply_text_figure_gradient(canvas, block_bg)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    dest = OUT_DIR / f"{slug}-mobhero.png"

    rgb_canvas = canvas.convert("RGB")
    rgb_canvas.save(dest, optimize=True)

    assert_no_hard_block_seams(dest, block_bg)
    assert_person_mask_opaque(mask_dest)
    print(f"Wrote {dest} ({rgb_canvas.width}x{rgb_canvas.height}) figure={new_w}x{new_h}@{x},{y} bg={block_bg}")


def main() -> None:
    for slug in SLUGS:
        composite_mobile_hero(slug)


if __name__ == "__main__":
    main()
