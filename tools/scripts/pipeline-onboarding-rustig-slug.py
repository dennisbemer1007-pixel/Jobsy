#!/usr/bin/env python3
"""One slug: band from 16x9 source → finalize → lobster tune → gate."""

from __future__ import annotations

import importlib.util
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "artifacts/onboarding-rustig-panorama-raw"
ASSETS = Path("/opt/cursor/artifacts/assets")
REF = RAW / "step-02-source-16x9-u6n.jpg"

TOP, BOT = 0.12, 0.92
MARGIN_CONF = 0.45
COUNT_CONF = 0.35

MIN_PERSONS = {
    "step-02": 2,
    "step-04": 2,
    "step-05": 1,
    "step-06": 1,
    "step-07": 1,
    "step-08": 2,
    "step-09": 1,
    "step-10": 2,
    "kompas": 1,
}

LOBSTER_RANGES: dict[str, tuple[tuple[float, float], tuple[float, float]]] = {
    "step-02": ((0.88, 0.98), (0.42, 0.58)),
    "step-04": ((0.82, 0.99), (0.42, 0.62)),
    "step-05": ((0.06, 0.22), (0.42, 0.62)),
    "step-06": ((0.65, 0.92), (0.42, 0.62)),
    "step-07": ((0.30, 0.55), (0.40, 0.60)),
    "step-08": ((0.08, 0.25), (0.42, 0.62)),
    "step-09": ((0.65, 0.92), (0.42, 0.62)),
    "step-10": ((0.15, 0.35), (0.42, 0.62)),
    "kompas": ((0.40, 0.60), (0.42, 0.62)),
}


def load_yolo():
    from ultralytics import YOLO

    w = ROOT / "yolov8n.pt"
    return YOLO(str(w if w.is_file() else "yolov8n.pt"))


def load_lob():
    spec = importlib.util.spec_from_file_location(
        "lob", ROOT / "tools/scripts/onboarding_rustig_lobster_layout.py"
    )
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def persons_band(model, band: Image.Image) -> list[tuple[float, float, float]]:
    res = model(np.asarray(band), verbose=False)[0]
    out = []
    if res.boxes is None:
        return out
    h = band.size[1]
    for b in res.boxes:
        if int(b.cls[0]) != 0:
            continue
        c = float(b.conf[0])
        y1, y2 = float(b.xyxy[0][1]) / h, float(b.xyxy[0][3]) / h
        out.append((y1, y2, c))
    return out


def pick_y0(model, src: Path, min_strong: int) -> int:
    im = Image.open(src).convert("RGB")
    w, h = im.size
    crop_h = int(w / 6)
    best = None
    for y0 in range(0, max(1, h - crop_h), 2):
        band = im.crop((0, y0, w, y0 + crop_h)).resize((2280, 340), Image.Resampling.LANCZOS)
        fr = persons_band(model, band)
        strong = [(y1, y2, c) for y1, y2, c in fr if c >= COUNT_CONF]
        if len(strong) < min_strong:
            continue
        scored = [(y1, y2, c) for y1, y2, c in fr if c >= MARGIN_CONF]
        if not scored:
            continue
        if not all(TOP <= y1 and y2 <= BOT for y1, y2, _ in scored):
            continue
        score = sum(y2 for _, y2, _ in scored)
        if best is None or score < best[0]:
            best = (score, y0)
    if best is not None:
        return best[1]
    # fallback: minimize feet clip
    best2 = None
    for y0 in range(0, max(1, h - crop_h), 2):
        band = im.crop((0, y0, w, y0 + crop_h)).resize((2280, 340), Image.Resampling.LANCZOS)
        fr = persons_band(model, band)
        strong = [(y1, y2, c) for y1, y2, c in fr if c >= COUNT_CONF]
        if len(strong) < min_strong:
            continue
        scored = [(y1, y2, c) for y1, y2, c in fr if c >= MARGIN_CONF]
        if not scored:
            continue
        worst = min(min(y1 - TOP, BOT - y2) for y1, y2, _ in scored)
        if best2 is None or worst > best2[0]:
            best2 = (worst, y0)
    if best2 is None:
        raise SystemExit(f"{src.name}: no y0 slice with detectable persons")
    print(f"WARN {src.name}: best-effort y0={best2[1]} margin={best2[0]:.3f}")
    return best2[1]


def save_band(src: Path, dst: Path, y0: int) -> None:
    im = Image.open(src).convert("RGB")
    w, h = im.size
    crop_h = int(w / 6)
    band = im.crop((0, y0, w, y0 + crop_h)).resize((2400, 400), Image.Resampling.LANCZOS)
    dst.parent.mkdir(parents=True, exist_ok=True)
    band.save(dst, quality=95, optimize=True)


def tune_lobster(lob, slug: str, variant: str, cx_range, cy_range) -> tuple[float, float]:
    pan = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama"
    im = Image.open(pan / f"{slug}-{variant}.jpg")
    model = load_yolo()
    res = model(np.asarray(im.convert("RGB")), verbose=False)[0]
    persons = []
    if res.boxes is not None:
        for b in res.boxes:
            if int(b.cls[0]) != 0:
                continue
            persons.append(tuple(int(v) for v in b.xyxy[0]))
    best = None
    desk, mob = lob.PLACEMENTS[slug]
    for cx in cx_range:
        for cy in cy_range:
            if variant == "desktop":
                lob.PLACEMENTS[slug] = ((float(cx), float(cy)), mob)
            else:
                lob.PLACEMENTS[slug] = (desk, (float(cx), float(cy)))
            lb = lob.lobster_bbox_pixels(im, slug, variant)
            if not persons:
                gap = 999.0
            else:
                gap = min(lob.min_box_gap(lb, p) for p in persons)
            if gap >= 15 and (best is None or gap > best[0]):
                best = (gap, float(cx), float(cy))
    if best is None:
        cx, cy = (cx_range[len(cx_range) // 2], cy_range[len(cy_range) // 2])
        print(f"WARN {slug} {variant} lobster fallback {cx},{cy}")
        return float(cx), float(cy)
    _, cx, cy = best
    return (float(cx), float(cy))


def source_for(slug: str) -> Path:
    for p in [
        ASSETS / f"{slug}-source-16x9-u6o-v3.jpg",
        ASSETS / f"{slug}-source-16x9-u6o-v2.jpg",
        ASSETS / f"{slug}-source-16x9-u6o.jpg",
        RAW / f"{slug}-source-16x9-u6n.jpg",
        RAW / f"{slug}-source-16x9-u6o-v3.jpg",
        RAW / f"{slug}-desktop-raw.jpg",
        RAW / f"{slug}-source-16x9-u6m-v2.jpg",
    ]:
        if p.is_file():
            return p
    raise SystemExit(f"no 16x9 source for {slug}")


def main() -> None:
    slug = sys.argv[1]
    skip_source = "--skip-source" in sys.argv
    skip_band = "--skip-band" in sys.argv
    if skip_source and slug == "step-02":
        pass
    elif slug == "step-02" and not skip_source:
        src = RAW / "step-02-source-16x9-u6n.jpg"
        if src.is_file():
            model = load_yolo()
            y0 = pick_y0(model, src, MIN_PERSONS["step-02"])
            save_band(src, RAW / f"{slug}-desktop-raw.jpg", y0)
    elif not skip_source and not skip_band:
        src = source_for(slug)
        model = load_yolo()
        y0 = pick_y0(model, src, MIN_PERSONS.get(slug, 1))
        save_band(src, RAW / f"{slug}-desktop-raw.jpg", y0)
        print(f"{slug}: band from {src.name} y0={y0}")

    subprocess.run(
        [sys.executable, str(ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"), str(RAW), slug],
        check=True,
        cwd=ROOT,
    )
    lob = load_lob()
    skip_tune = skip_source and slug == "step-02"
    if slug in LOBSTER_RANGES and not skip_tune:
        (cx0, cx1), (cy0, cy1) = LOBSTER_RANGES[slug]
        cxr = np.arange(cx0, cx1 + 0.001, 0.02)
        cyr = np.arange(cy0, cy1 + 0.001, 0.02)
        d = tune_lobster(lob, slug, "desktop", cxr, cyr)
        m = tune_lobster(lob, slug, "mobile", cxr, cyr)
        lob.PLACEMENTS[slug] = (d, m)
        layout_path = ROOT / "tools/scripts/onboarding_rustig_lobster_layout.py"
        lines = []
        in_placements = False
        for line in layout_path.read_text().splitlines():
            if line.startswith("PLACEMENTS:"):
                in_placements = True
            elif in_placements and line.startswith("}"):
                in_placements = False
            if in_placements and line.strip().startswith(f'"{slug}":'):
                lines.append(f'    "{slug}": (({d[0]:.3f}, {d[1]:.3f}), ({m[0]:.3f}, {m[1]:.3f})),')
            else:
                lines.append(line)
        layout_path.write_text("\n".join(lines) + "\n")

    subprocess.run(
        [sys.executable, str(ROOT / "tools/scripts/composite-onboarding-rustig-lobsters.py"), slug],
        check=True,
        cwd=ROOT,
    )
    subprocess.run(
        [sys.executable, str(ROOT / "tools/scripts/report-onboarding-rustig-hero.py"), slug],
        check=True,
        cwd=ROOT,
    )


if __name__ == "__main__":
    main()
