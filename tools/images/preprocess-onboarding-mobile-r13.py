#!/usr/bin/env python3
"""R13 mobile hero: bg without any humans; opaque pre-composited figure plates."""
from __future__ import annotations

import importlib.util
import json
import math
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "Jobsy.Web/wwwroot/images/onboarding"
SEAM = Path(__file__).resolve().parent / "preprocess-onboarding-hero-seam.py"
SLUGS = ("kennismaken", "beschikbaarheid", "vervoer", "werkervaring", "opleiding", "droombaan")

PAGE_BG = (245, 242, 238)
MOBILE_W = 780
MOBILE_H = 480
TEXT_FRAC = 0.55
BRIDGE_FRAC = 0.08
FIG_FRAC_W = 0.45
FIG_FRAC_H = 0.92
HUMAN_DILATE = 12
INPAINT_RADIUS = 16

FIGURE_SCENE_BOX: dict[str, tuple[float, float, float, float]] = {
    "kennismaken": (0.38, 0.05, 0.99, 0.98),
    "beschikbaarheid": (0.35, 0.08, 0.99, 0.98),
    "vervoer": (0.55, 0.05, 0.99, 0.98),
    "werkervaring": (0.38, 0.02, 0.99, 0.98),
    "opleiding": (0.38, 0.05, 0.99, 0.98),
    "droombaan": (0.42, 0.02, 0.99, 0.98),
}

# Manual figure crops on desktop JPG (left, top, right, bottom) — people + immediate ground.
MANUAL_FIGURE_BBOX: dict[str, tuple[float, float, float, float]] = {
    # Desktop art: handshake pair sits left-of-center — crop must include both.
    "kennismaken": (0.06, 0.04, 0.58, 0.98),
    # People cluster on the left; calendar/desk props stay on bg only.
    "beschikbaarheid": (0.0, 0.04, 0.56, 0.98),
    "vervoer": (0.58, 0.10, 0.99, 0.96),
    "werkervaring": (0.58, 0.0, 0.99, 0.98),
    "opleiding": (0.0, 0.02, 0.58, 0.98),
    # Dream-job figure is on the far left of the desktop hero.
    "droombaan": (0.0, 0.02, 0.40, 0.98),
}

FIGURE_DESCRIPTIONS: dict[str, str] = {
    "kennismaken": "Twee personen (vrouw en man) geven elkaar een hand — volledig zichtbaar, rechtsonder.",
    "beschikbaarheid": "Groep van vier personen (o.a. met baby) bij bureau — volledig zichtbaar, rechtsonder.",
    "vervoer": "Vrouw bij halte en bestuurder/chauffeur met klembord naast bus/van — volledig zichtbaar, rechtsonder.",
    "werkervaring": "Diverse werkende personen; man in pak met koffer prominent rechts — volledig zichtbaar.",
    "opleiding": "Groep (o.a. verpleger, afgestudeerde, vakman) met boeken — volledig zichtbaar, rechtsonder.",
    "droombaan": "Man in overalls, peinzend, met droompad — volledig zichtbaar, rechtsonder.",
}


def load_seam():
    spec = importlib.util.spec_from_file_location("seam", SEAM)
    mod = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(mod)
    return mod


def rgb_delta(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def dilate_mask(mask: list[list[bool]], radius: int) -> list[list[bool]]:
    h = len(mask)
    w = len(mask[0])
    out = [[False] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if not mask[y][x]:
                continue
            for dy in range(-radius, radius + 1):
                for dx in range(-radius, radius + 1):
                    if dx * dx + dy * dy <= radius * radius:
                        ny, nx = y + dy, x + dx
                        if 0 <= ny < h and 0 <= nx < w:
                            out[ny][nx] = True
    return out


def looks_like_ghost_figure(r: int, g: int, b: int, palette, seam) -> bool:
    d = rgb_delta((r, g, b), PAGE_BG)
    if d < 14 or d > 88:
        return False
    if seam.looks_like_scenery(r, g, b) and g >= r + 8:
        return False
    spread = max(r, g, b) - min(r, g, b)
    if spread > 55:
        return False
    return True


def build_human_removal_mask(img: Image.Image, seam, slug: str) -> list[list[bool]]:
    w, h = img.size
    palette = seam.sample_edge_palette(img)
    px = img.load()
    protect = seam.build_figure_protect_mask(img, palette)
    human = dilate_mask(protect, HUMAN_DILATE)
    lf, tf, rf, bf = FIGURE_SCENE_BOX[slug]
    rx0, ry0 = int(w * lf), int(h * tf)
    rx1, ry1 = min(w - 1, int(w * rf)), min(h - 1, int(h * bf))
    for y in range(h):
        for x in range(int(w * 0.78)):
            r, g, b = px[x, y][:3]
            if protect[y][x] or seam.is_figure_pixel(r, g, b, palette) or is_human_tone(r, g, b):
                human[y][x] = True
                continue
            if rx0 <= x <= rx1 and ry0 <= y <= ry1 and looks_like_ghost_figure(r, g, b, palette, seam):
                human[y][x] = True
    return dilate_mask(human, 6)


def is_decor_plant(r: int, g: int, b: int) -> bool:
    return g >= r + 10 and g >= b + 8 and g > 70


def is_bg_scenery_pixel(r: int, g: int, b: int, palette, seam) -> bool:
    if is_decor_plant(r, g, b):
        return True
    if rgb_delta((r, g, b), PAGE_BG) <= 20:
        return True
    return seam.looks_like_soft_background(r, g, b, palette) or seam.looks_like_scenery(r, g, b)


def is_subject_pixel(r: int, g: int, b: int, palette, seam) -> bool:
    if is_decor_plant(r, g, b):
        return False
    if seam.looks_like_scenery(r, g, b):
        return False
    if rgb_delta((r, g, b), PAGE_BG) > 24:
        return True
    return seam.is_figure_pixel(r, g, b, palette) or is_human_tone(r, g, b)


def figure_bbox_in_scene(img: Image.Image, _seam, slug: str) -> tuple[int, int, int, int]:
    w, h = img.size
    lf, tf, rf, bf = MANUAL_FIGURE_BBOX[slug]
    return int(w * lf), int(h * tf), int(w * rf), int(h * bf)


def is_cream_pixel(r: int, g: int, b: int) -> bool:
    return abs(r - PAGE_BG[0]) <= 8 and abs(g - PAGE_BG[1]) <= 8 and abs(b - PAGE_BG[2]) <= 8


def figure_alpha(r: int, g: int, b: int, palette, seam) -> int:
    """Opaque subject pixels; transparent only flat cream / soft peach wash."""
    if is_cream_pixel(r, g, b):
        return 0
    if is_decor_plant(r, g, b) and rgb_delta((r, g, b), PAGE_BG) < 35:
        return 0
    if seam.looks_like_soft_background(r, g, b, palette) and rgb_delta((r, g, b), PAGE_BG) < 28:
        return 0
    return 255


def feather_alpha_edges(img: Image.Image, radius: int = 2) -> Image.Image:
    if radius <= 0:
        return img
    alpha = img.split()[3]
    from PIL import ImageFilter

    soft = alpha.filter(ImageFilter.GaussianBlur(radius))
    out = img.copy()
    out.putalpha(soft)
    return out


def scrub_humans_to_bg(img: Image.Image, mask: list[list[bool]], seam) -> Image.Image:
    """Keep only cream/peach/plants on bg; subjects become flat cream/peach fills."""
    w, h = img.size
    px = img.load()
    palette = seam.sample_edge_palette(img)
    peach = palette[-1] if palette else PAGE_BG
    out = img.copy()
    opx = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y][:3]
            keep = is_bg_scenery_pixel(r, g, b, palette, seam) and not mask[y][x]
            if keep:
                continue
            opx[x, y] = peach if x > int(w * 0.48) else PAGE_BG
    return out


def cover_resize_right(img: Image.Image, tw: int, th: int) -> Image.Image:
    w, h = img.size
    scale = max(tw / w, th / h)
    nw, nh = int(w * scale + 0.5), int(h * scale + 0.5)
    scaled = img.resize((nw, nh), Image.Resampling.LANCZOS)
    x0 = max(0, nw - tw)
    y0 = max(0, nh - th)
    return scaled.crop((x0, y0, x0 + tw, y0 + th))


def apply_text_and_bridge(img: Image.Image) -> None:
    w, h = img.size
    px = img.load()
    text_end = int(w * TEXT_FRAC)
    bridge_end = min(w, int(w * (TEXT_FRAC + BRIDGE_FRAC)) + 20)
    for y in range(h):
        for x in range(w):
            if x < text_end:
                px[x, y] = PAGE_BG
                continue
            if x >= bridge_end:
                continue
            t = (x - text_end) / max(1, bridge_end - text_end)
            t = t * t * (3.0 - 2.0 * t)
            r, g, b = px[x, y][:3]
            px[x, y] = (
                int(PAGE_BG[0] + (r - PAGE_BG[0]) * t),
                int(PAGE_BG[1] + (g - PAGE_BG[1]) * t),
                int(PAGE_BG[2] + (b - PAGE_BG[2]) * t),
            )


def scrub_left_zone(img: Image.Image, frac: float = TEXT_FRAC) -> None:
    px = img.load()
    w, h = img.size
    x_end = int(w * frac)
    for y in range(h):
        for x in range(x_end):
            px[x, y] = PAGE_BG


def deghost_left_and_bridge(img: Image.Image) -> None:
    """Remove inpaint ghosts (mid-tone silhouettes) from text + bridge band."""
    px = img.load()
    w, h = img.size
    x_end = int(w * 0.72)
    for y in range(h):
        for x in range(x_end):
            r, g, b = px[x, y][:3]
            d = rgb_delta((r, g, b), PAGE_BG)
            if d <= 12:
                continue
            px[x, y] = PAGE_BG


def is_human_tone(r: int, g: int, b: int) -> bool:
    if r < 95 or g < 60 or b < 45:
        return False
    if r >= g - 20 and r >= b - 10 and (r - b) > 15:
        return True
    return False


def build_figure_content_mask(img: Image.Image, seam, slug: str) -> list[list[bool]]:
    """Opaque figure plate: humans + connected clothing/ground, no peach wash."""
    w, h = img.size
    palette = seam.sample_edge_palette(img)
    px = img.load()
    seed = [[False] * w for _ in range(h)]
    lf, tf, rf, bf = FIGURE_SCENE_BOX[slug]
    rx0, ry0 = int(w * lf), int(h * tf)
    rx1, ry1 = min(w - 1, int(w * rf)), min(h - 1, int(h * bf))

    protect = seam.build_figure_protect_mask(img, palette)
    for y in range(h):
        for x in range(w):
            if protect[y][x]:
                seed[y][x] = True

    for y in range(ry0, ry1 + 1):
        for x in range(rx0, rx1 + 1):
            r, g, b = px[x, y][:3]
            if seam.is_figure_pixel(r, g, b, palette) or is_human_tone(r, g, b):
                seed[y][x] = True

    content = [[False] * w for _ in range(h)]
    queue: list[tuple[int, int, int]] = []
    for y in range(h):
        for x in range(w):
            if seed[y][x]:
                content[y][x] = True
                queue.append((x, y, 0))

    max_dist = 32
    head = 0
    while head < len(queue):
        x, y, dist = queue[head]
        head += 1
        if dist >= max_dist:
            continue
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if nx < rx0 or ny < ry0 or nx > rx1 or ny > ry1 or content[ny][nx]:
                continue
            r, g, b = px[nx, ny][:3]
            if seam.looks_like_soft_background(r, g, b, palette):
                continue
            content[ny][nx] = True
            queue.append((nx, ny, dist + 1))

    return content


def bbox_of_mask(mask: list[list[bool]]) -> tuple[int, int, int, int] | None:
    h = len(mask)
    w = len(mask[0])
    xs: list[int] = []
    ys: list[int] = []
    for y in range(h):
        for x in range(w):
            if mask[y][x]:
                xs.append(x)
                ys.append(y)
    if not xs:
        return None
    return min(xs), min(ys), max(xs), max(ys)


def process_figure_plate(slug: str, seam) -> tuple[int, int, float]:
    src = OUT_DIR / f"{slug}.jpg"
    img = Image.open(src).convert("RGB")
    w, h = img.size
    palette = seam.sample_edge_palette(img)
    px = img.load()
    x0, y0, x1, y1 = figure_bbox_in_scene(img, seam, slug)

    crop_w = x1 - x0 + 1
    crop_h = y1 - y0 + 1
    crop = Image.new("RGBA", (crop_w, crop_h), (0, 0, 0, 0))
    cpx = crop.load()
    for cy in range(crop_h):
        for cx in range(crop_w):
            sx, sy = x0 + cx, y0 + cy
            r, g, b = px[sx, sy][:3]
            a = figure_alpha(r, g, b, palette, seam)
            cpx[cx, cy] = (r, g, b, a)

    alpha_box = bbox_of_mask([[cpx[cx, cy][3] >= 128 for cx in range(crop_w)] for cy in range(crop_h)])
    if alpha_box is not None:
        ax0, ay0, ax1, ay1 = alpha_box
        crop = crop.crop((ax0, ay0, ax1 + 1, ay1 + 1))
        crop_w, crop_h = crop.size

    crop = feather_alpha_edges(crop, radius=1)

    plate_w = max(1, int(MOBILE_W * FIG_FRAC_W))
    plate_h = max(1, int(MOBILE_H * FIG_FRAC_H))
    scale = min(plate_w / crop_w, plate_h / crop_h)
    nw, nh = max(1, int(crop_w * scale + 0.5)), max(1, int(crop_h * scale + 0.5))
    scaled = crop.resize((nw, nh), Image.Resampling.LANCZOS)
    plate = Image.new("RGBA", (plate_w, plate_h), (0, 0, 0, 0))
    paste_x = plate_w - nw
    paste_y = plate_h - nh
    plate.paste(scaled, (paste_x, paste_y), scaled)

    plate.save(OUT_DIR / f"{slug}-mobile.png", optimize=True)
    plate.save(OUT_DIR / f"{slug}-mobile.webp", format="WEBP", quality=92, method=6, lossless=False)

    opaque = sum(1 for _r, _g, _b, a in plate.getdata() if a >= 128)
    strong = sum(
        1
        for r, g, b, a in plate.getdata()
        if a >= 128 and rgb_delta((r, g, b), PAGE_BG) > 40
    )
    hero_right_area = plate_w * MOBILE_H
    ratio = strong / max(1, hero_right_area)
    print(
        f"{slug}-mobile: plate {plate_w}x{plate_h} opaque={opaque} strong_vs_cream={strong} "
        f"strong_ratio={ratio:.2%}"
    )
    return strong, plate_w * plate_h, ratio


def process_mobile_bg(slug: str, seam) -> None:
    src = OUT_DIR / f"{slug}.jpg"
    img = Image.open(src).convert("RGB")
    w, h = img.size
    palette = seam.sample_edge_palette(img)
    px = img.load()
    human = build_human_removal_mask(img, seam, slug)
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y][:3]
            if is_subject_pixel(r, g, b, palette, seam):
                human[y][x] = True
    cleaned = scrub_humans_to_bg(img, human, seam)
    mobile = cover_resize_right(cleaned, MOBILE_W, MOBILE_H)
    apply_text_and_bridge(mobile)
    scrub_left_zone(mobile, TEXT_FRAC)
    deghost_left_and_bridge(mobile)
    pxm = mobile.load()
    peach_ref = PAGE_BG
    for y in range(MOBILE_H):
        for x in range(int(MOBILE_W * TEXT_FRAC), MOBILE_W):
            r, g, b = pxm[x, y][:3]
            d = rgb_delta((r, g, b), PAGE_BG)
            if d < 14 or is_decor_plant(r, g, b):
                if is_decor_plant(r, g, b):
                    peach_ref = (r, g, b)
                continue
            if r > 228 and g > 228 and b > 225:
                pxm[x, y] = peach_ref
                continue
            if d < 90:
                pxm[x, y] = peach_ref
    mobile.save(OUT_DIR / f"{slug}-mobile-bg.png", optimize=True)
    mobile.save(OUT_DIR / f"{slug}-mobile-bg.webp", format="WEBP", quality=88, method=6)
    print(f"{slug}-mobile-bg: saved {MOBILE_W}x{MOBILE_H}")


def verify_bg_ghost(img: Image.Image) -> tuple[float, bool]:
    px = img.load()
    w, h = img.size
    x_end = int(w * TEXT_FRAC)
    total = x_end * h
    ghost = 0
    for y in range(h):
        for x in range(x_end):
            r, g, b = px[x, y][:3]
            d = rgb_delta((r, g, b), PAGE_BG)
            if 15 <= d <= 60:
                ghost += 1
    ratio = ghost / max(1, total)
    return ratio, ratio <= 0.02


def verify_composite_right_strength(slug: str) -> tuple[float, bool]:
    bg = Image.open(OUT_DIR / f"{slug}-mobile-bg.png").convert("RGB")
    fg = Image.open(OUT_DIR / f"{slug}-mobile.png").convert("RGBA")
    plate_w, plate_h = fg.size
    comp = bg.copy()
    comp.paste(fg, (MOBILE_W - plate_w, MOBILE_H - plate_h), fg)
    px = comp.load()
    rx0 = int(MOBILE_W * TEXT_FRAC)
    strong = total = 0
    for y in range(MOBILE_H):
        for x in range(rx0, MOBILE_W):
            total += 1
            if rgb_delta(px[x, y][:3], PAGE_BG) > 40:
                strong += 1
    ratio = strong / max(1, total)
    return ratio, ratio >= 0.25


def verify_figure_strength(ratio: float) -> bool:
    return ratio >= 0.07


def main() -> int:
    seam = load_seam()
    report: dict[str, object] = {"descriptions": FIGURE_DESCRIPTIONS, "steps": {}}
    failed = 0

    for slug in SLUGS:
        process_mobile_bg(slug, seam)
        _strong, _total, ratio = process_figure_plate(slug, seam)
        bg = Image.open(OUT_DIR / f"{slug}-mobile-bg.png").convert("RGB")
        ghost_ratio, ghost_ok = verify_bg_ghost(bg)
        fig_ok = verify_figure_strength(ratio)
        comp_ratio, comp_ok = verify_composite_right_strength(slug)
        ok = ghost_ok and fig_ok and comp_ok
        print(
            f"{slug} verify: ghost_left={ghost_ratio:.2%} ({'OK' if ghost_ok else 'FAIL'}) "
            f"plate_strong={ratio:.2%} ({'OK' if fig_ok else 'FAIL'}) "
            f"composite_right={comp_ratio:.2%} ({'OK' if comp_ok else 'FAIL'})"
        )
        report["steps"][slug] = {
            "ghost_left_ratio": ghost_ratio,
            "figure_strong_ratio": ratio,
            "composite_right_ratio": comp_ratio,
            "ghost_ok": ghost_ok,
            "figure_ok": fig_ok,
            "composite_ok": comp_ok,
            "person": FIGURE_DESCRIPTIONS[slug],
        }
        if not ok:
            failed += 1

    report_path = OUT_DIR / "mobile-r13-verify.json"
    report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Wrote {report_path}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
