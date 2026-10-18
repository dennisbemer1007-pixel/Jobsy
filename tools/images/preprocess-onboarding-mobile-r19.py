#!/usr/bin/env python3
"""R19: mobile hero plates from purpose-made 16:9 sources (top extend, no side crop)."""
from __future__ import annotations

import json
import math
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Jobsy.Web/wwwroot/images/onboarding"
R19_DIR = OUT_DIR / "r19-source"
R19_SLUGS = ("beschikbaarheid", "werkervaring", "opleiding")

MOBILE_W = 780
MOBILE_H = 520
PAGE_BG = (245, 242, 238)
TEXT_FRAC = 0.48


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def column_paper_color(scaled: Image.Image, x: int) -> tuple[int, int, int]:
    """Sample calm paper from the top edge; prefer left text columns."""
    px = scaled.load()
    w, h = scaled.size
    sample_x = min(x, max(0, int(w * 0.42)))
    rows = min(8, h)
    rs = gs = bs = n = 0
    for y in range(rows):
        r, g, b = px[sample_x, y][:3]
        rs += r
        gs += g
        bs += b
        n += 1
    return (rs // n, gs // n, bs // n)


def extend_top_to_mobile(src: Image.Image) -> Image.Image:
    """Scale to full 780px width (no side crop), anchor art to bottom, extend top with paper blend."""
    src = src.convert("RGB")
    w, h = src.size
    scale = MOBILE_W / w
    nh = max(1, int(h * scale + 0.5))
    scaled = src.resize((MOBILE_W, nh), Image.Resampling.LANCZOS)

    canvas = Image.new("RGB", (MOBILE_W, MOBILE_H), PAGE_BG)
    y0 = MOBILE_H - nh
    if y0 <= 0:
        # Source taller than plate: keep bottom MOBILE_H rows only (vertical trim only).
        scaled = scaled.crop((0, nh - MOBILE_H, MOBILE_W, nh))
        canvas.paste(scaled, (0, 0))
        return canvas

    cpx = canvas.load()
    for x in range(MOBILE_W):
        pr, pg, pb = column_paper_color(scaled, x)
        for y in range(y0):
            t = (y + 1) / max(1, y0)
            t = t**0.85
            cpx[x, y] = (
                int(PAGE_BG[0] * (1.0 - t) + pr * t),
                int(PAGE_BG[1] * (1.0 - t) + pg * t),
                int(PAGE_BG[2] * (1.0 - t) + pb * t),
            )
    canvas.paste(scaled, (0, y0))
    return canvas


def audit_mobile_hero(img: Image.Image, *, slug: str) -> dict[str, object]:
    w, h = img.size
    px = img.load()
    fig_start = int(w * 0.55)
    pearl = PAGE_BG
    is_bg = lambda r, g, b: rgb_dist((r, g, b), pearl) < 28  # noqa: E731

    min_fig_x = w
    for y in range(int(h * 0.12), int(h * 0.96)):
        for x in range(fig_start, w):
            r, g, b = px[x, y][:3]
            if not is_bg(r, g, b):
                min_fig_x = min(min_fig_x, x)

    overlap = False
    copy_right = int(w * TEXT_FRAC)
    for y in range(int(h * 0.08), int(h * 0.58)):
        for x in range(fig_start, copy_right):
            r, g, b = px[x, y][:3]
            if not is_bg(r, g, b):
                overlap = True
                break
        if overlap:
            break

    ok = min_fig_x >= fig_start - 1 and not overlap
    return {
        "slug": slug,
        "ok": ok,
        "min_fig_x": min_fig_x,
        "fig_start": fig_start,
        "overlap_left_band": overlap,
    }


def write_webp(jpg: Path) -> None:
    webp = jpg.with_suffix(".webp")
    subprocess.run(
        ["cwebp", "-q", "86", str(jpg), "-o", str(webp)],
        check=True,
        capture_output=True,
    )


def build_r19_mobile_heroes() -> int:
    failed = 0
    manifest: dict[str, object] = {}
    for slug in R19_SLUGS:
        src_path = R19_DIR / f"{slug}.jpg"
        if not src_path.is_file():
            print(f"missing R19 source {src_path}", file=sys.stderr)
            failed += 1
            continue
        hero = extend_top_to_mobile(Image.open(src_path))
        jpg = OUT_DIR / f"{slug}-mobile-hero.jpg"
        hero.save(jpg, quality=90, optimize=True)
        if shutil.which("cwebp"):
            write_webp(jpg)
        audit = audit_mobile_hero(hero, slug=slug)
        manifest[slug] = {"width": MOBILE_W, "height": MOBILE_H, "audit": audit, "source": str(src_path)}
        print("audit", audit)
        if not audit["ok"]:
            failed += 1
        print("wrote", jpg)

    meta = OUT_DIR / "mobile-hero-r19-manifest.json"
    meta.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return failed


def main() -> int:
    R19_DIR.mkdir(parents=True, exist_ok=True)
    failed = build_r19_mobile_heroes()
    if failed:
        print(f"R19 mobile hero build failed for {failed} item(s)", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
