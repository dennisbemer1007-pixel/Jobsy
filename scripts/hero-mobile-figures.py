#!/usr/bin/env python3
"""Extract opaque mobile hero figure PNGs from wide band art (no cream ghosting)."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC_DIR = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated"
OUT_DIR = SRC_DIR / "mob-figures"

PEARL = (247, 244, 240)

# Source wide file -> output PNG (crop box x0,y0,x1,y1 in source pixels; tuned per scene)
SCENES: dict[str, dict] = {
    "werkervaring-profession-wide.jpg": {
        "out": "werkervaring-mob-figures.png",
        "box": (1180, 80, 2559, 719),
    },
    "vervoer-profession-wide.jpg": {
        "out": "vervoer-mob-figures.png",
        "box": (1180, 120, 2559, 719),
    },
    "beschikbaarheid-profession-wide.jpg": {
        "out": "beschikbaarheid-mob-figures.png",
        "box": (1180, 40, 2559, 719),
    },
    "kompas-hero-wide.jpg": {
        "out": "kompas-mob-figures.png",
        "box": (1380, 80, 2559, 719),
    },
    "droombaan-profession-wide.jpg": {
        "out": "droombaan-mob-figures.png",
        "box": (1320, 140, 2559, 719),
    },
    "opleiding.jpg": {
        "out": "opleiding-mob-figures.png",
        "box": (520, 80, 1279, 719),
    },
}


def rgb_step(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    return max(abs(a[0] - b[0]), abs(a[1] - b[1]), abs(a[2] - b[2]))


def is_background(px: tuple[int, int, int]) -> bool:
    if rgb_step(PEARL, px) <= 42:
        return True
    # peach sky / sand wash
    if px[0] >= 248 and px[1] >= 228 and px[2] >= 210 and px[0] - px[2] <= 45:
        return True
    if px[0] >= 250 and px[1] >= 235 and px[2] >= 200 and px[0] - px[2] >= 18:
        return True
    return False


def extract_figures(src: Path, box: tuple[int, int, int, int], out: Path) -> dict:
    img = Image.open(src).convert("RGBA")
    crop = img.crop(box)
    w, h = crop.size
    px = crop.load()
    opaque = 0
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            rgb = (r, g, b)
            if is_background(rgb):
                px[x, y] = (r, g, b, 0)
            else:
                px[x, y] = (r, g, b, 255)
                opaque += 1
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    crop.save(out, optimize=True)
    return {"width": w, "height": h, "opaque_pixels": opaque, "box": box}


def main() -> None:
    manifest: dict[str, object] = {}
    for src_name, cfg in SCENES.items():
        src = SRC_DIR / src_name
        if not src.is_file():
            raise SystemExit(f"Missing source: {src}")
        out = OUT_DIR / cfg["out"]
        manifest[cfg["out"]] = extract_figures(src, tuple(cfg["box"]), out)
        print(f"Wrote {out.relative_to(ROOT)} ({manifest[cfg['out']]['opaque_pixels']} px)")

    manifest_path = OUT_DIR / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Manifest: {manifest_path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
