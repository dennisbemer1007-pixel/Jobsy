#!/usr/bin/env python3
"""R15–R19: mobile hero plates (R19 purpose-made sources for steps 2/4/5; cover for 1/3/6)."""
from __future__ import annotations

import hashlib
import importlib.util
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
R19_SCRIPT = ROOT / "tools/images/preprocess-onboarding-mobile-r19.py"
SLUGS = ("kennismaken", "beschikbaarheid", "vervoer", "werkervaring", "opleiding", "droombaan")

MOBILE_W = 780
MOBILE_H = 520
PAGE_BG = (245, 242, 238)

# Source crop (fraction L,T,R,B) before cover-scale to MOBILE_W×MOBILE_H.
CROP: dict[str, tuple[float, float, float, float]] = {
    "kennismaken": (0.0, 0.0, 0.95, 1.0),
    "beschikbaarheid": (0.0, 0.05, 0.44, 0.98),
    "vervoer": (0.28, 0.0, 1.0, 1.0),
    "opleiding": (0.30, 0.02, 0.60, 0.98),
    "droombaan": (0.0, 0.0, 0.72, 1.0),
}

ANCHOR_X: dict[str, float] = {
    "kennismaken": 0.84,
    "beschikbaarheid": 0.78,
    "vervoer": 0.82,
    "werkervaring": 0.62,
    "opleiding": 0.52,
    "droombaan": 0.84,
}

FLIP_H = frozenset({"beschikbaarheid", "opleiding", "droombaan", "kennismaken"})


def rgb_dist(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def crop_frac(img: Image.Image, box: tuple[float, float, float, float]) -> Image.Image:
    w, h = img.size
    l, t, r, b = box
    return img.crop((int(w * l), int(h * t), int(w * r), int(h * b)))


def cover_to(img: Image.Image, tw: int, th: int, *, anchor_x: float = 0.72, anchor_y: float = 0.55) -> Image.Image:
    w, h = img.size
    scale = max(tw / w, th / h)
    nw, nh = int(w * scale + 0.5), int(h * scale + 0.5)
    scaled = img.resize((nw, nh), Image.Resampling.LANCZOS)
    x0 = int((nw - tw) * anchor_x)
    y0 = int((nh - th) * anchor_y)
    x0 = max(0, min(nw - tw, x0))
    y0 = max(0, min(nh - th, y0))
    return scaled.crop((x0, y0, x0 + tw, y0 + th))


def wash_left_pearl(img: Image.Image, frac: float = 0.5) -> None:
    px = img.load()
    w, h = img.size
    x_end = int(w * frac)
    for y in range(h):
        for x in range(x_end):
            t = 1.0 - (x / max(1, x_end))
            t = t * t
            r, g, b = px[x, y][:3]
            px[x, y] = (
                int(PAGE_BG[0] * t + r * (1.0 - t)),
                int(PAGE_BG[1] * t + g * (1.0 - t)),
                int(PAGE_BG[2] * t + b * (1.0 - t)),
            )


def audit_mobile_hero(img: Image.Image, *, slug: str) -> dict[str, object]:
    w, h = img.size
    px = img.load()
    fig_start = int(w * 0.55)
    pearl = PAGE_BG
    is_bg = lambda r, g, b: rgb_dist((r, g, b), pearl) < 26  # noqa: E731

    min_fig_x = w
    for y in range(int(h * 0.12), int(h * 0.96)):
        for x in range(fig_start, w):
            r, g, b = px[x, y][:3]
            if not is_bg(r, g, b):
                min_fig_x = min(min_fig_x, x)

    overlap = False
    copy_right = int(w * 0.5)
    for y in range(int(h * 0.08), int(h * 0.55)):
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


R19_SLUGS = frozenset({"beschikbaarheid", "werkervaring", "opleiding"})


def _r19_module():
    spec = importlib.util.spec_from_file_location("ob_mobile_r19", R19_SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Cannot load {R19_SCRIPT}")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def build_mobile_hero(slug: str) -> Image.Image:
    if slug in R19_SLUGS:
        r19 = _r19_module()
        src_path = R19_DIR / f"{slug}.jpg"
        if not src_path.is_file():
            raise FileNotFoundError(f"Missing R19 source: {src_path}")
        return r19.extend_top_to_mobile(Image.open(src_path))

    src = Image.open(OUT_DIR / f"{slug}.jpg").convert("RGB")
    patch = crop_frac(src, CROP[slug])
    if slug in FLIP_H:
        patch = patch.transpose(Image.Transpose.FLIP_LEFT_RIGHT)

    ax = ANCHOR_X.get(slug, 0.82)
    hero = cover_to(patch, MOBILE_W, MOBILE_H, anchor_x=ax, anchor_y=0.52)
    if slug == "werkervaring":
        wash_left_pearl(hero, 0.50)
    return hero


def write_webp(jpg: Path) -> None:
    webp = jpg.with_suffix(".webp")
    subprocess.run(
        ["cwebp", "-q", "86", str(jpg), "-o", str(webp)],
        check=True,
        capture_output=True,
    )


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    manifest: dict[str, dict[str, int | object]] = {}
    audits: list[dict[str, object]] = []
    failed = 0
    for slug in SLUGS:
        hero = build_mobile_hero(slug)
        jpg = OUT_DIR / f"{slug}-mobile-hero.jpg"
        hero.save(jpg, quality=90, optimize=True)
        if not shutil.which("cwebp"):
            print("cwebp is required to build mobile-hero.webp assets", file=sys.stderr)
            failed += 1
            continue
        webp = jpg.with_suffix(".webp")
        write_webp(jpg)
        if slug in R19_SLUGS:
            _r19_module().sync_companion_mobile_assets(slug, jpg)
            audit = _r19_module().audit_mobile_hero(hero, slug=slug)
        else:
            audit = audit_mobile_hero(hero, slug=slug)
        audits.append(audit)
        print("audit", audit)
        if not audit["ok"]:
            failed += 1
        jpg_md5 = hashlib.md5(jpg.read_bytes()).hexdigest()
        webp_md5 = hashlib.md5(webp.read_bytes()).hexdigest() if webp.is_file() else ""
        manifest[slug] = {
            "width": MOBILE_W,
            "height": MOBILE_H,
            "audit": audit,
            "mobile_hero_jpg_md5": jpg_md5,
            "mobile_hero_webp_md5": webp_md5,
            "mobile_hero_webp_bytes": webp.stat().st_size if webp.is_file() else 0,
        }
        print("wrote", jpg, webp, f"webp_md5={webp_md5}")

    meta = OUT_DIR / "mobile-hero-r20-manifest.json"
    meta.write_text(json.dumps({"steps": manifest, "audits": audits}, indent=2) + "\n", encoding="utf-8")
    if failed:
        print(f"R20 pixel audit failed for {failed} slug(s)", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
