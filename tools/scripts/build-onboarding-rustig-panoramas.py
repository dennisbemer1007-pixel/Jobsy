#!/usr/bin/env python3
"""Build thin-band panoramic headers for onboarding-rustig PO mockups (no figure crop)."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
WWW = ROOT / "Jobsy.Web/wwwroot"
OUT = WWW / "images/mockups/onboarding-rustig-panorama"

CREAM = (247, 244, 240)
PEACH = (231, 205, 182)

DESKTOP = (1920, 160)
MOBILE = (780, 110)

# slug -> source path relative to wwwroot
SOURCES: dict[str, str] = {
    "start": "images/candidate-illustrated/kennismaken.jpg",
    "step-01": "images/candidate-illustrated/kennismaken.jpg",
    "step-02": "images/candidate-illustrated/beschikbaarheid-profession-wide.jpg",
    "step-03": "images/mockups/onboarding-rustig-panorama/vervoer-panorama-source.jpg",
    "step-04": "images/candidate-illustrated/werkervaring-profession-wide.jpg",
    "step-05": "images/candidate-illustrated/opleiding.jpg",
    "step-06": "images/candidate-illustrated/droombaan-profession-wide.jpg",
    "step-07": "images/candidate-illustrated/wie-ben-jij-profession-wide.jpg",
    "step-08": "images/tests/bg-hoe-je-werkt.jpg",
    "step-09": "images/tests/bg-leuk.jpg",
    "step-10": "images/tests/bg-sfeer.jpg",
    "step-11": "images/tests/bg-waarden.jpg",
    "kompas": "images/candidate-illustrated/kompas-hero-wide.jpg",
}


def decor_strip(width: int, height: int) -> Image.Image:
    strip = Image.new("RGB", (width, height))
    px = strip.load()
    for x in range(width):
        t = (x / max(1, width - 1)) ** 0.92
        for y in range(height):
            r = int(CREAM[0] + (PEACH[0] - CREAM[0]) * t)
            g = int(CREAM[1] + (PEACH[1] - CREAM[1]) * t)
            b = int(CREAM[2] + (PEACH[2] - CREAM[2]) * t)
            px[x, y] = (r, g, b)
    return strip


def apply_edge_fades(im: Image.Image) -> Image.Image:
    """Soft cream fade at bottom and side edges."""
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    for x in range(w):
        for y in range(h):
            alpha = 0
            # bottom fade (last 38%)
            if y > h * 0.62:
                t = (y - h * 0.62) / (h * 0.38)
                alpha = max(alpha, int(255 * min(1.0, t**1.1)))
            # side fades (first/last 8% width)
            if x < w * 0.08:
                t = 1.0 - x / (w * 0.08)
                alpha = max(alpha, int(220 * t))
            elif x > w * 0.92:
                t = (x - w * 0.92) / (w * 0.08)
                alpha = max(alpha, int(220 * t))
            if alpha:
                overlay.putpixel((x, y), (CREAM[0], CREAM[1], CREAM[2], alpha))
    base = im.convert("RGBA")
    return Image.alpha_composite(base, overlay).convert("RGB")


def build_band(src: Path, out_size: tuple[int, int]) -> Image.Image:
    ow, oh = out_size
    scene = Image.open(src).convert("RGB")
    sw, sh = scene.size

    # Fit entire scene inside band height (never crop figures).
    scale = oh / sh
    nw, nh = max(1, int(sw * scale)), oh
    if nw > ow:
        scale = ow / sw
        nw, nh = ow, max(1, int(sh * scale))

    scaled = scene.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (ow, oh), CREAM)

    if nw < ow:
        gap = ow - nw
        left = gap // 2
        right = gap - left
        if left > 0:
            canvas.paste(decor_strip(left, oh), (0, 0))
        if right > 0:
            canvas.paste(decor_strip(right, oh), (left + nw, 0))
        paste_x = left
    else:
        paste_x = 0

    paste_y = oh - nh
    canvas.paste(scaled, (paste_x, paste_y))
    return apply_edge_fades(canvas)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for slug, rel in SOURCES.items():
        src = WWW / rel
        if not src.is_file():
            raise SystemExit(f"missing source for {slug}: {src}")
        desk = build_band(src, DESKTOP)
        mob = build_band(src, MOBILE)
        desk_path = OUT / f"{slug}-desktop.jpg"
        mob_path = OUT / f"{slug}-mobile.jpg"
        desk.save(desk_path, quality=91, optimize=True)
        mob.save(mob_path, quality=91, optimize=True)
        print(f"{slug}: {desk.size} / {mob.size} <- {rel}")


if __name__ == "__main__":
    main()
