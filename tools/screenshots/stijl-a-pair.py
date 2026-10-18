#!/usr/bin/env python3
"""Side-by-side PNG pairs for stijl-A-r2 (before left, after right)."""
from __future__ import annotations

import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("pip install Pillow", file=sys.stderr)
    raise


def pair(before: Path, after: Path, out: Path) -> None:
    b = Image.open(before).convert("RGB")
    a = Image.open(after).convert("RGB")
    h = max(b.height, a.height)
    bg = (245, 242, 238)

    def pad(im: Image.Image) -> Image.Image:
        if im.height == h:
            return im
        canvas = Image.new("RGB", (im.width, h), bg)
        canvas.paste(im, (0, 0))
        return canvas

    b, a = pad(b), pad(a)
    out.parent.mkdir(parents=True, exist_ok=True)
    canvas = Image.new("RGB", (b.width + a.width, h), bg)
    canvas.paste(b, (0, 0))
    canvas.paste(a, (b.width, 0))
    canvas.save(out, optimize=True)


if __name__ == "__main__":
    pair(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]))
