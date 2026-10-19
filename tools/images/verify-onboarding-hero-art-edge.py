#!/usr/bin/env python3
"""Verify hero art left-edge smoothness on JPG assets or 1440 PNGs (column jump <= 6)."""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image

PAGE_BG = (245, 242, 238)
HERO_TOP = 0
HERO_BOTTOM = 240
MAX_JUMP = 6
MAIN_X_START = 280
MAIN_X_END_TRIM = 300


def _looks_like_page_bg(rgb: tuple[int, int, int], tolerance: int = 14) -> bool:
    r, g, b = rgb
    return (
        abs(r - PAGE_BG[0]) <= tolerance
        and abs(g - PAGE_BG[1]) <= tolerance
        and abs(b - PAGE_BG[2]) <= tolerance
    )


def max_jump_at_columns(path: Path, sample_ys: tuple[int, ...], x_start: int, x_end: int) -> tuple[int, int, int]:
    img = Image.open(path).convert("RGB")
    w, h = img.size
    px = img.load()
    max_jump = 0
    worst_x = 0
    worst_y = 0
    x_start = max(0, min(x_start, w - 2))
    x_end = max(x_start + 1, min(x_end, w - 1))
    for y in sample_ys:
        yy = min(max(0, y), h - 1)
        for x in range(x_start, x_end):
            c1 = px[x, yy]
            c2 = px[x + 1, yy]
            jump = max(abs(c1[i] - c2[i]) for i in range(3))
            if jump > max_jump:
                max_jump = jump
                worst_x = x
                worst_y = yy
    return max_jump, worst_x, worst_y


def find_art_left_edge(path: Path, y: int) -> int | None:
    """Left edge of hero illustration on row y (non-cream pixels connected to the right edge)."""
    img = Image.open(path).convert("RGB")
    w, _h = img.size
    px = img.load()
    yy = min(max(HERO_TOP, y), min(HERO_BOTTOM, img.size[1]) - 1)
    seen: set[tuple[int, int]] = set()
    stack = [(w - 1, yy)]
    while stack:
        x, row = stack.pop()
        if x < 0 or x >= w or row != yy or (x, row) in seen:
            continue
        if _looks_like_page_bg(px[x, row][:3], tolerance=3):
            continue
        seen.add((x, row))
        stack.extend([(x - 1, row), (x + 1, row)])
    if not seen:
        return None
    return min(x for x, _ in seen)


def check_asset(path: Path) -> tuple[bool, str]:
    img = Image.open(path)
    h = img.size[1]
    ys = (h // 2, h - 6)
    mj, wx, wy = max_jump_at_columns(path, ys, 0, min(32, img.size[0] - 1))
    ok = mj <= MAX_JUMP
    return ok, f"{path.name}: asset left-edge max_jump={mj} at x={wx} y={wy} {'OK' if ok else 'FAIL'}"


def check_png(path: Path) -> tuple[bool, str]:
    img = Image.open(path)
    w = img.size[0]
    y_mid = (HERO_TOP + HERO_BOTTOM) // 2
    y_bottom = HERO_BOTTOM - 4
    lines = []
    ok = True
    for label, y in (("mid", y_mid), ("bottom", y_bottom)):
        art_left = find_art_left_edge(path, y)
        if art_left is None:
            lines.append(f"{path.name} art-left@{label}: seam fully blended to --bg OK")
            continue
        # Only the seam column step (cream page bg → art), not interior illustration edges.
        x0 = max(0, art_left - 1)
        x1 = min(w - 2, art_left + 1)
        mj, wx, wy = max_jump_at_columns(path, (y,), x0, x1)
        line_ok = mj <= MAX_JUMP
        ok = ok and line_ok
        lines.append(
            f"{path.name} art-left@{label} x≈{art_left}: max_jump={mj} at x={wx} y={wy} {'OK' if line_ok else 'FAIL'}"
        )
    return ok, "\n".join(lines)


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: verify-onboarding-hero-art-edge.py [--png] <files...>", file=sys.stderr)
        return 2
    args = sys.argv[1:]
    png_mode = False
    if args[0] == "--png":
        png_mode = True
        args = args[1:]
    failed = 0
    for arg in args:
        p = Path(arg)
        ok, msg = check_png(p) if png_mode else check_asset(p)
        print(msg)
        if not ok:
            failed += 1
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
