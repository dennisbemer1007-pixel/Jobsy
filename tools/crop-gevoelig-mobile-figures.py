#!/usr/bin/env python3
"""Crop opaque mobile figure PNGs for gevoelig hero (390). No gradient over figures."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / "Jobsy.Web/wwwroot/images/candidate"

# (left, top, right, bottom) in source pixels — figure clusters only
CROPS: dict[str, tuple[int, int, int, int]] = {
    "werk-full.jpg": (500, 35, 1275, 715),
    "ref-full.jpg": (430, 25, 1275, 715),
}


def _cream_alpha(r: int, g: int, b: int) -> int:
    """Alpha 0 for cream/peach backdrop; opaque for illustration ink."""
    if r > 228 and g > 218 and b > 198 and (r - b) < 45:
        return 0
    if r > 240 and g > 230 and b > 215:
        return 0
    return 255


def crop_figures(src_name: str, out_name: str, box: tuple[int, int, int, int]) -> None:
    src = ROOT / src_name
    out = ROOT / out_name
    im = Image.open(src).convert("RGBA")
    cropped = im.crop(box)
    px = cropped.load()
    w, h = cropped.size
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            px[x, y] = (r, g, b, _cream_alpha(r, g, b))
    cropped.save(out, optimize=True)
    print(f"Wrote {out} ({w}x{h})")


def main() -> None:
    crop_figures("werk-full.jpg", "werk-mobile-figures.png", CROPS["werk-full.jpg"])
    crop_figures("ref-full.jpg", "ref-mobile-figures.png", CROPS["ref-full.jpg"])


if __name__ == "__main__":
    main()
