#!/usr/bin/env python3
"""Mobile ref hero: single figure (hijab) bottom-right, max 45% width, opaque on cream."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Jobsy.Web/wwwroot/images/candidate/ref-full.jpg"
OUT = ROOT / "Jobsy.Web/wwwroot/images/candidate/ref-full-mobile.jpg"

CREAM = (251, 245, 236)
MOB_W, MOB_H = 1200, 720
MAX_FIG_FRAC = 0.45


def build() -> None:
    im = Image.open(SRC).convert("RGB")
    w, _h = im.size
    # Right crop: woman with hijab + table, exclude second woman on the left.
    crop = im.crop((int(w * 0.52), 0, w, im.height))
    canvas = Image.new("RGB", (MOB_W, MOB_H), CREAM)
    max_fig_w = int(MOB_W * MAX_FIG_FRAC)
    scale = min(max_fig_w / crop.width, MOB_H / crop.height)
    fig_w = max(1, int(crop.width * scale))
    fig_h = max(1, int(crop.height * scale))
    fig = crop.resize((fig_w, fig_h), Image.Resampling.LANCZOS)
    paste_x = MOB_W - fig_w
    paste_y = MOB_H - fig_h
    canvas.paste(fig, (paste_x, paste_y))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, quality=92, optimize=True)
    print(f"Wrote {OUT} figure {fig_w}x{fig_h} at ({paste_x},{paste_y})")


if __name__ == "__main__":
    build()
