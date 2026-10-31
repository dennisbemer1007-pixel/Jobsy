#!/usr/bin/env python3
"""Resize Dennis category watercolours into mockup web assets (~400px)."""
from pathlib import Path

from PIL import Image

SRC_DIR = Path("/cursor/stores/bot/jobsy-job-art")
OUT_DIR = Path(__file__).resolve().parent / "src" / "img" / "jobs"
CARD_MAX = 400
HERO_SIZE = (960, 380)

# Source filename in bot store → category slug (beroepsgroep illustration)
CATEGORY_SOURCES = (
    ("zorg-dieren", "dierenverzorger.jpg"),
    ("onderzoek", "bioloog.jpg"),
    ("creatief", "grafisch-ontwerper.jpg"),
    ("buiten-groen", "hovenier.jpg"),
    ("helpen", "brandweer.jpg"),
)


def _save_card(im: Image.Image, path: Path) -> None:
    im = im.convert("RGB")
    im.thumbnail((CARD_MAX, CARD_MAX), Image.Resampling.LANCZOS)
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path, "JPEG", quality=86, optimize=True)


def _cover_crop(im: Image.Image, tw: int, th: int, cx: float, cy: float) -> Image.Image:
    im = im.convert("RGB")
    w, h = im.size
    tr = tw / th
    sr = w / h
    if sr > tr:
        nh = h
        nw = int(h * tr)
    else:
        nw = w
        nh = int(w / tr)
    left = int(cx * w - nw / 2)
    top = int(cy * h - nh / 2)
    left = max(0, min(w - nw, left))
    top = max(0, min(h - nh, top))
    return im.crop((left, top, left + nw, top + nh)).resize((tw, th), Image.Resampling.LANCZOS)


def main() -> None:
    for slug, src_name in CATEGORY_SOURCES:
        im = Image.open(SRC_DIR / src_name)
        _save_card(im, OUT_DIR / f"{slug}.jpg")
        print("card", OUT_DIR / f"{slug}.jpg")

    for slug, src_name in CATEGORY_SOURCES:
        hero_src = Image.open(SRC_DIR / src_name)
        hero = _cover_crop(hero_src, HERO_SIZE[0], HERO_SIZE[1], 0.5, 0.42)
        out = OUT_DIR / f"{slug}-hero.jpg"
        hero.save(out, "JPEG", quality=88, optimize=True)
        print("hero", out)


if __name__ == "__main__":
    main()
