#!/usr/bin/env python3
"""Crop test-block hero figure PNGs from bottom-band JPG art (no source frame)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
JPG_DIR = ROOT / "Jobsy.Web/wwwroot/images/tests"
OUT_DIR = JPG_DIR / "figures"

SLUGS = ("hoe-je-werkt", "leuk", "sfeer", "waarden")

CROP_START = {
    "hoe-je-werkt": 0.52,
    "leuk": 0.52,
    "sfeer": 0.52,
    "waarden": 0.34,
}


def _cream_mask(arr, ref, tol: int = 26):
    import numpy as np

    diff = np.abs(arr.astype(int) - ref.astype(int)).max(axis=2)
    return diff <= tol


def crop_figure(slug: str) -> None:
    import numpy as np

    src = JPG_DIR / f"bg-{slug}.jpg"
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    ref = np.array(im.convert("RGB").getpixel((0, 0)), dtype=np.uint8)

    bottom = im.crop((0, int(h * 0.44), w, h))
    bw, bh = bottom.size
    # Figures sit on the right in the mobile hero reference.
    crop_start = CROP_START.get(slug, 0.52)
    right = bottom.crop((int(bw * crop_start), 0, bw, bh))
    arr = np.array(right)
    rgb = arr[:, :, :3]
    cream = _cream_mask(rgb, ref, tol=28)
    alpha = np.where(cream, 0, 255).astype(np.uint8)
    arr[:, :, 3] = alpha

    ys, xs = np.where(alpha > 0)
    if len(xs) == 0:
        raise RuntimeError(f"No opaque pixels for {slug}")
    pad = 4
    x0 = max(0, int(xs.min()) - pad)
    y0 = max(0, int(ys.min()) - pad)
    x1 = min(arr.shape[1], int(xs.max()) + pad + 1)
    y1 = min(arr.shape[0], int(ys.max()) + pad + 1)
    trimmed = arr[y0:y1, x0:x1]

    out = Image.fromarray(trimmed, mode="RGBA")
    # Shave a 1px rim so the exported PNG never reads as a JPG stamp box.
    alpha = out.split()[-1]
    alpha = alpha.point(lambda a: 0 if a < 245 else a)
    out.putalpha(alpha)
    target_h = 360
    scale = target_h / out.height
    target_w = max(1, int(out.width * scale))
    out = out.resize((target_w, target_h), Image.Resampling.LANCZOS)
    padded = Image.new("RGBA", (target_w + 8, target_h + 8), (0, 0, 0, 0))
    padded.paste(out, (4, 4), out)
    out = padded

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    dest = OUT_DIR / f"{slug}-hero.png"
    out.save(dest, optimize=True)
    print(f"Wrote {dest} ({out.width}x{out.height})")


def main() -> None:
    for slug in SLUGS:
        crop_figure(slug)


if __name__ == "__main__":
    main()
