#!/usr/bin/env python3
"""Mobile hero figure PNGs: tight crop on people, transparent background."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC_DIR = ROOT / "Jobsy.Web/wwwroot/images/candidate-illustrated"
OUT_DIR = SRC_DIR / "mob-figures"

PEARL = (247, 244, 240)
MARGIN = 20


def rgb_step(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    return max(abs(a[0] - b[0]), abs(a[1] - b[1]), abs(a[2] - b[2]))


def is_background(px: tuple[int, int, int]) -> bool:
    if rgb_step(PEARL, px) <= 44:
        return True
    if px[0] >= 248 and px[1] >= 228 and px[2] >= 210 and px[0] - px[2] <= 48:
        return True
    if px[0] >= 250 and px[1] >= 235 and px[2] >= 198 and px[0] - px[2] >= 16:
        return True
    return False


def is_skin(px: tuple[int, int, int]) -> bool:
    r, g, b = px
    if r < 90 or sum(px) > 700:
        return False
    return r >= 130 and g >= 90 and b >= 70 and r >= g >= b - 25


def is_person_pixel(px: tuple[int, int, int]) -> bool:
    if is_background(px):
        return False
    if is_skin(px):
        return True
    r, g, b = px
    if sum(px) > 710:
        return False
    if r < 95 and g < 95 and b < 110:
        return True
    if b > r + 12 and b > 105 and r < 130:
        return True
    if 35 < r < 130 and 55 < g < 175 and 90 < b < 220:
        return True
    if 150 < r < 240 and 80 < g < 190 and 60 < b < 160 and rgb_step(PEARL, px) > 55:
        return True
    return False


def people_bbox(img: Image.Image, x0: int, x1: int) -> tuple[int, int, int, int] | None:
    w, h = img.size
    x0 = max(0, x0)
    x1 = min(w, x1)
    minx, miny, maxx, maxy = w, h, 0, 0
    hits = 0
    for y in range(h):
        for x in range(x0, x1):
            if is_person_pixel(img.getpixel((x, y))):
                minx = min(minx, x)
                maxx = max(maxx, x)
                miny = min(miny, y)
                maxy = max(maxy, y)
                hits += 1
    if hits < 800:
        return None
    return (
        max(0, minx - MARGIN),
        max(0, miny - MARGIN),
        min(w - 1, maxx + MARGIN),
        min(h - 1, maxy + MARGIN),
    )


# Tuned boxes (x0,y0,x1,y1) on source wide art — people only, full heads, no landscape/props-only
SCENES: dict[str, dict] = {
    "werkervaring-profession-wide.jpg": {
        "out": "werkervaring-mob-figures.png",
        # Right: professional with briefcase, head to knees
        "box": (1588, 72, 2078, 719),
    },
    "vervoer-profession-wide.jpg": {
        "out": "vervoer-mob-figures.png",
        # Single cyclist (left band), full bike + head margin
        "box": (430, 120, 730, 708),
        "feather": {"left": 72, "bottom": 40, "right": 14, "top": 14},
    },
    "beschikbaarheid-profession-wide.jpg": {
        "out": "beschikbaarheid-mob-figures.png",
        # Two men (baby + watch), full width of group (Instellingen)
        "box": (0, 214, 469, 719),
        "feather": {"left": 80, "bottom": 44, "right": 14, "top": 14},
    },
    "finale-hero.jpg": {
        "out": "kompas-mob-figures.png",
        # Onboarding finale — woman with compass, head to feet (Kompas mob hero)
        "box": (460, 311, 939, 719),
        "feather": {"left": 72, "bottom": 40, "right": 14, "top": 18},
    },
    "droombaan-profession-wide.jpg": {
        "out": "droombaan-mob-figures.png",
        # 2–3 professions on the right band, include heads
        "box": (1995, 0, 2295, 640),
    },
    "opleiding.jpg": {
        "out": "opleiding-mob-figures.png",
        "box": (8, 168, 720, 719),
    },
}


def trim_alpha(im: Image.Image) -> Image.Image:
    bbox = im.getbbox()
    return im.crop(bbox) if bbox else im


def feather_rgba(im: Image.Image, *, left: int = 36, bottom: int = 28, right: int = 10, top: int = 10) -> Image.Image:
    """Soft alpha at crop edges so mob hero figures blend into pearl (ref737-style)."""
    out = im.copy()
    w, h = out.size
    opx = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = opx[x, y]
            if a == 0:
                continue
            factor = 1.0
            if left > 0 and x < left:
                factor = min(factor, x / left)
            if right > 0 and x >= w - right:
                factor = min(factor, (w - 1 - x) / right)
            if bottom > 0 and y >= h - bottom:
                factor = min(factor, (h - 1 - y) / bottom)
            if top > 0 and y < top:
                factor = min(factor, y / top)
            factor = max(0.0, min(1.0, factor))
            opx[x, y] = (r, g, b, int(a * factor))
    return out


def extract_scene(src: Path, cfg: dict) -> dict:
    img = Image.open(src).convert("RGB")
    w, h = img.size
    box = tuple(cfg["box"])
    if "search" in cfg:
        sx0, sx1 = int(w * cfg["search"][0]), int(w * cfg["search"][1])
        detected = people_bbox(img, sx0, sx1)
        if detected:
            box = detected
    crop_rgb = img.crop(box)
    cw, ch = crop_rgb.size
    rgba = Image.new("RGBA", (cw, ch))
    opx = rgba.load()
    opaque = 0
    for y in range(ch):
        for x in range(cw):
            rgb = crop_rgb.getpixel((x, y))
            if is_background(rgb):
                opx[x, y] = (*rgb, 0)
            else:
                opx[x, y] = (*rgb, 255)
                opaque += 1
    rgba = trim_alpha(rgba)
    feather = cfg.get("feather", {})
    rgba = feather_rgba(
        rgba,
        left=int(feather.get("left", 56)),
        bottom=int(feather.get("bottom", 40)),
        right=int(feather.get("right", 14)),
        top=int(feather.get("top", 14)),
    )
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    out = OUT_DIR / cfg["out"]
    rgba.save(out, optimize=True)
    ow, oh = rgba.size
    return {"source": src.name, "box": box, "out_size": [ow, oh], "opaque_pixels": opaque, "aspect": round(oh / max(ow, 1), 3)}


def main() -> None:
    manifest: dict[str, object] = {}
    for src_name, cfg in SCENES.items():
        src = SRC_DIR / src_name
        if not src.is_file():
            raise SystemExit(f"Missing source: {src}")
        manifest[cfg["out"]] = extract_scene(src, cfg)
        m = manifest[cfg["out"]]
        print(f"{cfg['out']}: box={m['box']} size={m['out_size']} aspect={m['aspect']}")

    (OUT_DIR / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
