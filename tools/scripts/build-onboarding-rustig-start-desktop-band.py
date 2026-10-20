#!/usr/bin/env python3
"""Start desktop hero: wide v3 source, vertical slice 0.42–0.92, 2280×340 band."""

from __future__ import annotations

import importlib.util
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
WIDE_SRC = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-source-16x9-startf-seamless.jpg"
DEFAULT_SRC = WIDE_SRC
FALLBACK = Path("/opt/cursor/artifacts/assets/start-desktop-raw-v3.jpg")
OUT = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/start-desktop.jpg"
VERIFY = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-band-verify.png"
DESK = (2280, 340)
FADE_BOTTOM = 14
SLICE_Y0_FRAC = 0.42
SLICE_Y1_FRAC = 0.92
V3_SIZE = (1280, 720)
# Manual anchors on start-desktop-raw-v3 (1280×720), from art review.
V3_ANCHORS = {
    "gate_top": (0.38, 0.49),
    "sign_bottom": (0.42, 0.64),
    "walker_hat": (0.62, 0.56),
    "walker_feet": (0.62, 0.85),
    "fence_bottom": (0.30, 0.81),
}


def load_fin():
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    m = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(m)
    return m


def center_x0(wide_w: int) -> int:
    return (wide_w - V3_SIZE[0]) // 2


def pick_slice(im: Image.Image) -> Image.Image:
    w, h = im.size
    y0, y1 = int(h * SLICE_Y0_FRAC), int(h * SLICE_Y1_FRAC)
    crop = im.crop((0, y0, w, y1))
    cw, ch = crop.size
    target_ratio = DESK[0] / DESK[1]
    have_ratio = cw / ch
    if abs(have_ratio - target_ratio) > 0.02:
        # Center-crop width to exact 6.7:1 before resize (no stretch).
        need_w = int(round(ch * target_ratio))
        if need_w <= cw:
            x0 = (cw - need_w) // 2
            crop = crop.crop((x0, 0, x0 + need_w, ch))
        else:
            need_h = int(round(cw / target_ratio))
            y_off = max(0, (ch - need_h) // 2)
            crop = crop.crop((0, y_off, cw, y_off + need_h))
    return crop


def anchor_bbox_on_band(wide_w: int, wide_h: int) -> dict[str, list[int]]:
    x0 = center_x0(wide_w)
    sy0 = SLICE_Y0_FRAC * wide_h
    sy1 = SLICE_Y1_FRAC * wide_h
    sh = sy1 - sy0
    out: dict[str, list[int]] = {}
    pts: dict[str, tuple[float, float]] = {}
    for name, (ax, ay) in V3_ANCHORS.items():
        px = x0 + ax * V3_SIZE[0]
        py = ay * V3_SIZE[1]
        pts[name] = (px, py)
        bx = int(px / wide_w * DESK[0])
        by = int((py - sy0) / sh * DESK[1])
        out[name] = [bx, by]
    # Derived boxes from anchor points (band px).
    hat = out["walker_hat"]
    feet = out["walker_feet"]
    person = [
        max(0, hat[0] - int(DESK[0] * 0.06)),
        max(0, hat[1] - 8),
        min(DESK[0], feet[0] + int(DESK[0] * 0.06)),
        min(DESK[1] - 1, feet[1]),
    ]
    gt = out["gate_top"]
    sb = out["sign_bottom"]
    sign = [
        max(0, gt[0] - int(DESK[0] * 0.04)),
        max(0, gt[1] - 4),
        min(DESK[0], sb[0] + int(DESK[0] * 0.08)),
        min(DESK[1] - 1, sb[1] + 6),
    ]
    return {
        "anchor_points_band_px": out,
        "person_bbox_px": person,
        "sign_bbox_px": sign,
        "v3_center_x0_px": x0,
    }


def yolo_person_bbox(im: Image.Image) -> tuple[int, int, int, int] | None:
    try:
        from ultralytics import YOLO

        wpath = ROOT / "yolov8n.pt"
        model = YOLO(str(wpath if wpath.is_file() else "yolov8n.pt"))
        res = model(np.asarray(im), verbose=False)[0]
        if res.boxes is None:
            return None
        w = im.size[0]
        best = None
        for b in res.boxes:
            if int(b.cls[0]) != 0:
                continue
            if float(b.conf[0]) < 0.35:
                continue
            x0, y0, x1, y1 = (int(v) for v in b.xyxy[0].tolist())
            cx = (x0 + x1) / 2
            if cx < w * 0.45 or cx > w * 0.82:
                continue
            hpx = y1 - y0
            if hpx < 55:
                continue
            area = (x1 - x0) * hpx
            if best is None or area > best[0]:
                best = (area, x0, y0, x1, y1)
        return None if best is None else best[1:]
    except Exception:
        return None


def blue_jacket_present(im: Image.Image, person: list[int]) -> bool:
    """Walker region should contain saturated blue (jacket/backpack)."""
    x0, y0, x1, y1 = person
    sub = np.asarray(im.crop((x0, y0, x1, y1)))
    if sub.size == 0:
        return False
    r, g, b = sub[:, :, 0], sub[:, :, 1], sub[:, :, 2]
    blue = (b > 95) & (b > r + 12) & (b > g + 6)
    return float(blue.mean()) > 0.02


def save_verify_overlay(band_pre: Image.Image, metrics: dict) -> None:
    vis = band_pre.copy().convert("RGBA")
    draw = ImageDraw.Draw(vis)
    for name, pt in metrics.get("anchor_points_band_px", {}).items():
        draw.ellipse((pt[0] - 4, pt[1] - 4, pt[0] + 4, pt[1] + 4), fill=(255, 0, 0, 200))
        draw.text((pt[0] + 6, pt[1] - 6), name, fill=(200, 0, 0, 255))
    for key, color in (("person_bbox_px", (0, 180, 0)), ("sign_bbox_px", (0, 80, 220))):
        bb = metrics.get(key)
        if bb:
            draw.rectangle(bb, outline=(*color, 255), width=3)
    yolo = metrics.get("yolo_person_bbox_px")
    if yolo:
        draw.rectangle(yolo, outline=(255, 140, 0, 255), width=2)
    VERIFY.parent.mkdir(parents=True, exist_ok=True)
    vis.convert("RGB").save(VERIFY, optimize=True)


def apply_bottom_fade(im: Image.Image, fin) -> Image.Image:
    w, h = im.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = overlay.load()
    cream = tuple(int(c) for c in fin.CREAM)
    fade = FADE_BOTTOM
    for y in range(h):
        if y < h - fade:
            continue
        for x in range(w):
            t = (y - (h - fade)) / max(1, fade - 1)
            a = int(255 * min(1.0, t**1.05))
            if a:
                px[x, y] = (*cream, a)
    return Image.alpha_composite(im.convert("RGBA"), overlay).convert("RGB")


def compose_band(src: Image.Image, fin) -> tuple[Image.Image, dict]:
    ww, wh = src.size
    band_pre = pick_slice(src).resize(DESK, Image.Resampling.LANCZOS)
    band = apply_bottom_fade(band_pre, fin)
    mapped = anchor_bbox_on_band(ww, wh)
    person = mapped["person_bbox_px"]
    sign = mapped["sign_bbox_px"]
    yolo = yolo_person_bbox(band_pre)
    fade_start_y = DESK[1] - FADE_BOTTOM
    slice_crop = pick_slice(src)
    metrics = {
        "band_px": list(DESK),
        "fade_start_y_px": fade_start_y,
        "source_size_px": [ww, wh],
        "source_slice_y_frac": [SLICE_Y0_FRAC, SLICE_Y1_FRAC],
        "slice_crop_px": list(slice_crop.size),
        "slice_crop_ratio": round(slice_crop.size[0] / slice_crop.size[1], 4),
        "anchor_points_band_px": mapped["anchor_points_band_px"],
        "person_bbox_px": person,
        "sign_bbox_px": sign,
        "yolo_person_bbox_px": list(yolo) if yolo else None,
        "v3_center_x0_px": mapped["v3_center_x0_px"],
    }
    feet_y = person[3]
    metrics["person_feet_y_px"] = feet_y
    metrics["person_feet_above_fade_px"] = fade_start_y - feet_y
    metrics["sign_bottom_y_px"] = sign[3]
    metrics["sign_above_fade_px"] = fade_start_y - sign[3]
    cy = (person[1] + person[3]) / 2
    metrics["person_cy_pct"] = round(cy / DESK[1], 4)
    metrics["walker_blue_ok"] = blue_jacket_present(band_pre, person)
    save_verify_overlay(band_pre, metrics)
    metrics["verify_overlay"] = str(VERIFY.relative_to(ROOT))
    return band, metrics


def main() -> None:
    wide_script = ROOT / "tools/scripts/build-onboarding-rustig-start-wide-source.py"
    if not WIDE_SRC.is_file() or "--rebuild-wide" in sys.argv:
        import subprocess

        subprocess.check_call([sys.executable, str(wide_script)], cwd=ROOT)
    src = Path(sys.argv[1]) if len(sys.argv) > 1 and not sys.argv[1].startswith("-") else DEFAULT_SRC
    if not src.is_file():
        src = FALLBACK
    if src == FALLBACK:
        import subprocess

        subprocess.check_call([sys.executable, str(wide_script), str(FALLBACK)], cwd=ROOT)
        src = WIDE_SRC
    fin = load_fin()
    band, metrics = compose_band(Image.open(src).convert("RGB"), fin)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    band.save(OUT, quality=92, optimize=True)
    metrics_path = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-band-metrics.json"
    metrics_path.write_text(json.dumps(metrics, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {OUT}")
    print(json.dumps(metrics, indent=2))
    if metrics.get("person_feet_above_fade_px", 0) < 8:
        raise SystemExit("FAIL: walker feet in asset fade zone (anchor)")
    if metrics.get("sign_above_fade_px", 0) < 4:
        raise SystemExit("FAIL: sign in asset fade zone (anchor)")
    if not metrics.get("yolo_person_bbox_px"):
        raise SystemExit("FAIL: YOLO did not detect walker in band (visual gate)")
    yolo = metrics["yolo_person_bbox_px"]
    fade_limit = metrics["fade_start_y_px"] - 4
    if yolo[3] > fade_limit:
        raise SystemExit(f"FAIL: YOLO feet y={yolo[3]} too low vs fade {fade_limit}")


if __name__ == "__main__":
    main()
