#!/usr/bin/env python3
"""Desktop ref hero: illustration-only crop (no left cream/leaves ghost), full height."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Jobsy.Web/wwwroot/images/candidate/ref-full.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/candidate/ref-full-desktop.jpg"


def build() -> None:
    im = Image.open(SRC).convert("RGB")
    w, h = im.size
    crop = im.crop((int(w * 0.38), 0, w, h))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    crop.save(OUT, quality=92, optimize=True)
    print(f"Wrote {OUT} {crop.size}")


if __name__ == "__main__":
    build()
