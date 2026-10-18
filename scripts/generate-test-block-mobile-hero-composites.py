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

SLUGS = ("hoe-je-werkt", "leuk", "sfeer", "waarden")

# Fractional crop on trimmed *-hero.png (x0, y0, x1, y1) — keeps 1–2 full figures for bottom-right placement.
HERO_REGION = {
    "hoe-je-werkt": (0.62, 0.0, 1.0, 1.0),
    "leuk": (0.0, 0.0, 0.82, 0.92),
    "sfeer": (0.02, 0.0, 0.30, 1.0),
    "waarden": (0.66, 0.34, 1.0, 1.0),
}


def _sample_cream(im: Image.Image) -> tuple[int, int, int]:
    rgb = im.convert("RGB")
    samples = [
        rgb.getpixel((4, 4)),
        rgb.getpixel((rgb.width - 5, 4)),
        rgb.getpixel((4, rgb.height - 5)),
    ]
    return tuple(int(sum(c[i] for c in samples) / len(samples)) for i in range(3))


def load_opaque_figure_from_hero(slug: str) -> Image.Image:
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
    alpha = arr[:, :, 3]
    arr[alpha > 20, 3] = 255
    figure = Image.fromarray(arr, mode="RGBA")

    bbox = figure.getbbox()
    if not bbox:
        raise RuntimeError(f"No opaque pixels in hero crop for {slug}")
    return figure.crop(bbox)


def _apply_text_figure_gradient(canvas: Image.Image, cream: tuple[int, int, int]) -> None:
    """Soft cream fade over the left ~58% so title sits on calm cream, figures stay vivid on the right."""
    import numpy as np

    arr = np.array(canvas.convert("RGBA"))
    w = arr.shape[1]
    fade_end = int(w * 0.58)
    for x in range(fade_end):
        t = 1.0 - (x / max(1, fade_end))
        strength = t * t * 0.72
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


def composite_mobile_hero(slug: str) -> None:
    src = JPG_DIR / f"bg-{slug}.jpg"
    im = Image.open(src)
    cream = _sample_cream(im)

    canvas = Image.new("RGBA", (MOB_W, MOB_H), cream + (255,))
    figure = load_opaque_figure_from_hero(slug)

    target_w = int(MOB_W * 0.45)
    target_h = int(MOB_H * 0.94)
    max_w = int(MOB_W * (0.60 if slug == "leuk" else 0.48))
    scale = min(max_w / figure.width, target_h / figure.height)
    new_w = max(1, int(figure.width * scale))
    new_h = max(1, int(figure.height * scale))
    figure = figure.resize((new_w, new_h), Image.Resampling.LANCZOS)

    margin_x = 8 if slug == "leuk" else 10
    margin_y = 0 if slug == "leuk" else 6
    x = MOB_W - new_w - margin_x
    y = MOB_H - new_h - margin_y
    canvas.paste(figure, (x, y), figure)

    rgb_canvas = canvas.convert("RGB")
    _apply_text_figure_gradient(rgb_canvas, cream)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    dest = OUT_DIR / f"{slug}-mobhero.png"
    rgb_canvas.save(dest, optimize=True)
    print(f"Wrote {dest} ({rgb_canvas.width}x{rgb_canvas.height}) figure={new_w}x{new_h}@{x},{y}")


def main() -> None:
    for slug in SLUGS:
        composite_mobile_hero(slug)


if __name__ == "__main__":
    main()
