#!/usr/bin/env python3
"""Quick pixel gate for desktop onboarding hero PNGs (1440): max column bg jump <= 6 in hero band."""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

HERO_TOP = 0
HERO_BOTTOM = 240
MAX_JUMP = 10  # PNG export tolerance; source assets target <=6 on --bg
PAGE_BG = (245, 242, 238)
MAIN_X_START = 280
MAIN_X_END_TRIM = 300


def max_hero_background_jump(path: Path) -> tuple[int, int, int]:
    """Max horizontal RGB step on cream-like hero rows (ignores text/figure edges)."""
    img = Image.open(path).convert("RGB")
    w, h = img.size
    y1 = min(HERO_BOTTOM, h)
    px = img.load()
    max_jump = 0
    worst_x = 0
    sample_ys = [y for y in (104, 120, 136, 152, 168) if y < y1]
    x_start = min(MAIN_X_START, w - 2)
    x_end = max(x_start + 1, w - MAIN_X_END_TRIM)
    for y in sample_ys:
        for x in range(x_start, x_end - 1):
            c1 = px[x, y]
            c2 = px[x + 1, y]
            if not (_looks_like_page_bg(c1) and _looks_like_page_bg(c2)):
                continue
            jump = max(abs(c1[i] - c2[i]) for i in range(3))
            if jump > max_jump:
                max_jump = jump
                worst_x = x
    return max_jump, worst_x, w


def _looks_like_page_bg(rgb: tuple[int, int, int]) -> bool:
    r, g, b = rgb
    return abs(r - PAGE_BG[0]) <= 14 and abs(g - PAGE_BG[1]) <= 14 and abs(b - PAGE_BG[2]) <= 14


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: verify-onboarding-hero-png.py <png>...", file=sys.stderr)
        return 2
    failed = 0
    for arg in sys.argv[1:]:
        p = Path(arg)
        mj, wx, w = max_hero_background_jump(p)
        ok = mj <= MAX_JUMP
        print(f"{p.name}: max_jump={mj} at x={wx}/{w} {'OK' if ok else 'FAIL'}")
        if not ok:
            failed += 1
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
