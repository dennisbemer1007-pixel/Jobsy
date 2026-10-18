#!/usr/bin/env python3
"""Pre-composite mobile test-block hero bands (390×200 logical, 780×400 asset)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
JPG_DIR = ROOT / "Jobsy.Web/wwwroot/images/tests"
OUT_DIR = JPG_DIR / "figures"

# Logical hero size at 2× for crisp background-image.
MOB_W = 780
MOB_H = 400

# Matches :root --bg (#f5f2ee); mobile heroes use background-color: var(--bg).
PAGE_CREAM = (245, 242, 238)

SLUGS = ("hoe-je-werkt", "leuk", "sfeer", "waarden")

# Fractional crop on trimmed *-hero.png (x0, y0, x1, y1) — keeps 1–2 full figures for bottom-right placement.
HERO_REGION = {
    "hoe-je-werkt": (0.62, 0.0, 1.0, 1.0),
    "leuk": (0.0, 0.0, 0.82, 0.92),
    "sfeer": (0.02, 0.0, 0.30, 1.0),
    "waarden": (0.66, 0.34, 1.0, 1.0),
}

# Min 60px logical fade at 2× asset.
FEATHER_PX = 140


def _luminance(rgb) -> float:
    r, g, b = rgb[:3]
    return 0.299 * r + 0.587 * g + 0.114 * b


def _near_cream(rgb, cream: tuple[int, int, int], tol: int = 22) -> bool:
    return max(abs(int(rgb[0]) - cream[0]), abs(int(rgb[1]) - cream[1]), abs(int(rgb[2]) - cream[2])) <= tol


def assert_no_hard_cream_seams(path: Path, cream: tuple[int, int, int] = PAGE_CREAM) -> None:
    """Fail when a straight cream/cream luminance step >8 appears (figure PNG box edge)."""
    import numpy as np

    im = Image.open(path).convert("RGB")
    arr = np.array(im)
    h, w = arr.shape[:2]
    lum = (
        0.299 * arr[:, :, 0].astype(np.float32)
        + 0.587 * arr[:, :, 1].astype(np.float32)
        + 0.114 * arr[:, :, 2].astype(np.float32)
    )

    x_start = max(1, int(w * 0.22))
    for x in range(x_start, w):
        seam = 0
        for y in range(h):
            if abs(lum[y, x] - lum[y, x - 1]) <= 8:
                continue
            if _near_cream(arr[y, x - 1], cream) and _near_cream(arr[y, x], cream):
                seam += 1
        if seam >= max(28, int(h * 0.22)):
            raise RuntimeError(f"{path.name}: vertical cream seam at x={x} ({seam} rows, ΔL>8)")

    y_start = max(1, int(h * 0.08))
    x_lo, x_hi = int(w * 0.22), int(w * 0.92)
    for y in range(y_start, h):
        seam = 0
        for x in range(x_lo, x_hi):
            if abs(lum[y, x] - lum[y - 1, x]) <= 8:
                continue
            if _near_cream(arr[y - 1, x], cream) and _near_cream(arr[y, x], cream):
                seam += 1
        if seam >= max(36, int((x_hi - x_lo) * 0.28)):
            raise RuntimeError(f"{path.name}: horizontal cream seam at y={y} ({seam} cols, ΔL>8)")


def load_opaque_figure_from_hero(slug: str, canvas_cream: tuple[int, int, int]) -> Image.Image:
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
    rgb = arr[:, :, :3]
    ref_corners = np.array(
        [rgb[0, 0], rgb[0, -1], rgb[-1, 0], rgb[-1, -1]], dtype=np.int16
    )
    local_cream = ref_corners.mean(axis=0).astype(np.uint8)
    canvas = np.array(canvas_cream, dtype=np.int16)

    diff_canvas = np.abs(rgb.astype(np.int16) - canvas).max(axis=2)
    diff_local = np.abs(rgb.astype(np.int16) - local_cream.astype(np.int16)).max(axis=2)
    keyed = (diff_canvas <= 30) | (diff_local <= 32)
    arr[keyed, 3] = 0
    arr[~keyed & (arr[:, :, 3] > 24), 3] = 255

    figure = Image.fromarray(arr, mode="RGBA")
    bbox = figure.getbbox()
    if not bbox:
        raise RuntimeError(f"No opaque pixels in hero crop for {slug}")
    return figure.crop(bbox)


def _feather_figure_alpha(figure: Image.Image, left_px: int, top_px: int) -> Image.Image:
    import numpy as np

    arr = np.array(figure, copy=True)
    alpha = arr[:, :, 3]
    ys, xs = np.where(alpha > 0)
    if len(xs) == 0:
        return figure
    x0, y0 = int(xs.min()), int(ys.min())

    def smoothstep(t: float) -> float:
        t = max(0.0, min(1.0, t))
        return t * t * (3.0 - 2.0 * t)

    h, w = arr.shape[:2]
    for x in range(w):
        dist_left = x - x0
        if dist_left < left_px:
            t = smoothstep(dist_left / max(1, left_px))
            arr[:, x, 3] = (alpha[:, x].astype(np.float32) * t).astype(np.uint8)
    alpha = arr[:, :, 3]
    for y in range(h):
        dist_top = y - y0
        if dist_top < top_px:
            t = smoothstep(dist_top / max(1, top_px))
            arr[y, :, 3] = (arr[y, :, 3].astype(np.float32) * t).astype(np.uint8)

    return Image.fromarray(arr, mode="RGBA")


def _apply_text_figure_gradient(canvas: Image.Image, cream: tuple[int, int, int], fade_end_ratio: float = 0.62) -> None:
    """Soft cream fade over the left band so title sits on calm cream; figures stay vivid on the right."""
    import numpy as np

    arr = np.array(canvas.convert("RGBA"))
    w = arr.shape[1]
    fade_end = int(w * fade_end_ratio)
    for x in range(fade_end):
        t = 1.0 - (x / max(1, fade_end))
        strength = t * t * 0.68
        if strength <= 0.01:
            continue
        for y in range(arr.shape[0]):
            r, g, b, a = arr[y, x]
            if a < 255:
                continue
            nr = int(r * (1 - strength) + cream[0] * strength)
            ng = int(g * (1 - strength) + cream[1] * strength)
            nb = int(b * (1 - strength) + cream[2] * strength)
            arr[y, x] = (nr, ng, nb, 255)
    canvas.paste(Image.fromarray(arr, mode="RGBA"))


def _feather_figure_into_canvas(
    canvas: Image.Image,
    figure: Image.Image,
    x: int,
    y: int,
    cream: tuple[int, int, int],
) -> None:
    """Paste figure and dissolve left/top margins into page cream over FEATHER_PX."""
    import numpy as np

    canvas.paste(figure, (x, y), figure)
    arr = np.array(canvas.convert("RGBA"))
    fig = np.array(figure)
    fh, fw = fig.shape[:2]

    def smoothstep(t: float) -> float:
        t = max(0.0, min(1.0, t))
        return t * t * (3.0 - 2.0 * t)

    cream_arr = np.array(cream, dtype=np.float32)
    for fy in range(fh):
        cy = y + fy
        if cy >= arr.shape[0]:
            continue
        for fx in range(fw):
            cx = x + fx
            if cx >= arr.shape[1]:
                continue
            fa = fig[fy, fx, 3]
            if fa < 8:
                continue
            dist_l = fx
            dist_t = fy
            t_l = smoothstep(dist_l / FEATHER_PX) if dist_l < FEATHER_PX else 1.0
            t_t = smoothstep(dist_t / max(1, FEATHER_PX // 2)) if dist_t < FEATHER_PX // 2 else 1.0
            t = min(t_l, t_t)
            fr, fg, fb = fig[fy, fx, :3].astype(np.float32)
            blend = t * (fa / 255.0)
            base = arr[cy, cx, :3].astype(np.float32)
            out = base * (1.0 - blend) + np.array([fr, fg, fb]) * blend
            # Pull keyed fringe toward cream
            if t < 0.98 and max(abs(fr - cream[0]), abs(fg - cream[1]), abs(fb - cream[2])) < 36:
                out = base * (1.0 - blend) + cream_arr * blend
            arr[cy, cx, :3] = np.clip(out, 0, 255).astype(np.uint8)
            arr[cy, cx, 3] = 255

    canvas.paste(Image.fromarray(arr, mode="RGBA"))


def _smooth_figure_left_margin(
    canvas: Image.Image, fig_x: int, fig_y: int, fig_h: int, cream: tuple[int, int, int]
) -> None:
    """Extra dissolve for the strip left of the figure (removes visible PNG box edge)."""
    import numpy as np

    arr = np.array(canvas.convert("RGBA"))
    x_start = max(0, fig_x - FEATHER_PX)
    x_end = min(arr.shape[1], fig_x + 24)
    y_start = max(0, fig_y - 8)
    y_end = min(arr.shape[0], fig_y + fig_h + 8)
    cream_arr = np.array(cream, dtype=np.float32)

    for x in range(x_start, x_end):
        t = (x - x_start) / max(1, x_end - x_start)
        strength = (1.0 - t) * 0.96
        if strength <= 0.02:
            continue
        for y in range(y_start, y_end):
            p = arr[y, x, :3].astype(np.float32)
            if max(abs(p[0] - cream[0]), abs(p[1] - cream[1]), abs(p[2] - cream[2])) > 48:
                continue
            out = p * (1.0 - strength) + cream_arr * strength
            arr[y, x, :3] = np.clip(out, 0, 255).astype(np.uint8)

    canvas.paste(Image.fromarray(arr, mode="RGBA"))


def composite_mobile_hero(slug: str) -> None:
    cream = PAGE_CREAM
    canvas = Image.new("RGBA", (MOB_W, MOB_H), cream + (255,))
    figure = load_opaque_figure_from_hero(slug, cream)

    target_w = int(MOB_W * 0.45)
    target_h = int(MOB_H * 0.94)
    max_w = int(MOB_W * (0.60 if slug == "leuk" else 0.48))
    scale = min(max_w / figure.width, target_h / figure.height)
    new_w = max(1, int(figure.width * scale))
    new_h = max(1, int(figure.height * scale))
    figure = figure.resize((new_w, new_h), Image.Resampling.LANCZOS)
    figure = _feather_figure_alpha(figure, FEATHER_PX, FEATHER_PX // 2)

    margin_x = 18 if slug == "hoe-je-werkt" else (8 if slug == "leuk" else 10)
    margin_y = 0 if slug == "leuk" else 6
    x = MOB_W - new_w - margin_x
    y = MOB_H - new_h - margin_y
    _feather_figure_into_canvas(canvas, figure, x, y, cream)
    _smooth_figure_left_margin(canvas, x, y, new_h, cream)

    _apply_text_figure_gradient(canvas, cream)

    rgb_canvas = canvas.convert("RGB")
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    dest = OUT_DIR / f"{slug}-mobhero.png"
    rgb_canvas.save(dest, optimize=True)
    assert_no_hard_cream_seams(dest, cream)
    print(f"Wrote {dest} ({rgb_canvas.width}x{rgb_canvas.height}) figure={new_w}x{new_h}@{x},{y}")


def main() -> None:
    for slug in SLUGS:
        composite_mobile_hero(slug)


if __name__ == "__main__":
    main()
