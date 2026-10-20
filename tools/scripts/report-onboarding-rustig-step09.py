#!/usr/bin/env python3
"""Capture step-09 mockups (1440 desktop + 390 mobile) — test Sfeer u9a."""

from __future__ import annotations

import subprocess
import time
from pathlib import Path

import numpy as np
from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
DESK_JPEG = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-09-desktop.jpg"
MOB_JPEG = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-09-mobile.jpg"
CACHE = "20261020-u9b"


def lobster_red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 110) & (r > g + 18) & (r > b + 18) & (g < 145) & (b < 145)


def small_red_glyphs(rgb: np.ndarray, *, min_x_frac: float = 0.90) -> int:
    h, w = rgb.shape[:2]
    mask = lobster_red_mask(rgb)
    vis = np.zeros((h, w), dtype=bool)
    n = 0
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or vis[y, x]:
                continue
            stack = [(y, x)]
            xs: list[int] = []
            ys: list[int] = []
            while stack:
                cy, cx = stack.pop()
                if cy < 0 or cy >= h or cx < 0 or cx >= w or vis[cy, cx] or not mask[cy, cx]:
                    continue
                vis[cy, cx] = True
                xs.append(cx)
                ys.append(cy)
                stack.extend([(cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)])
            if len(xs) < 12 or len(xs) > 900:
                continue
            x0, x1 = min(xs), max(xs)
            y0, y1 = min(ys), max(ys)
            cxm, cym = (x0 + x1) / 2, (y0 + y1) / 2
            if cxm < w * min_x_frac or cym > h * 0.58:
                continue
            if (y1 - y0 + 1) > 40 or (x1 - x0 + 1) > 40:
                continue
            n += 1
    return n


def assert_feet_above_fade(rgb: np.ndarray, label: str) -> None:
    """Each figure column must keep scene content out of the bottom cream fade."""
    h, w = rgb.shape[:2]
    cream = np.array([247.0, 244.0, 240.0])
    dist = np.linalg.norm(rgb.astype(np.float32) - cream, axis=2)
    fade_floor = h - 22 if "mobile" in label else h - 26
    segments = [
        (0.02, 0.24, "painter"),
        (0.24, 0.48, "market"),
        (0.48, 0.72, "construction"),
        (0.72, 0.98, "team"),
    ]
    for x0f, x1f, name in segments:
        x0, x1 = int(w * x0f), int(w * x1f)
        mask = dist[:, x0:x1] > 28
        ys = np.where(mask.any(axis=1))[0]
        if ys.size == 0:
            raise SystemExit(f"FAIL {label}/{name}: empty column")
        y_bot = int(ys.max())
        if y_bot >= fade_floor:
            raise SystemExit(
                f"FAIL {label}/{name}: feet/scene y={y_bot} >= fade floor {fade_floor} (h={h})"
            )
        foot = rgb[int(h * 0.68) : fade_floor, x0:x1]
        g = foot[:, :, 1].astype(np.float32)
        r = foot[:, :, 0].astype(np.float32)
        ground = (g > r + 6) & (g > 55)
        if int(ground.sum()) < 80:
            raise SystemExit(f"FAIL {label}/{name}: no grass/ground under feet")


def assert_hero_jpeg(path: Path, label: str) -> None:
    rgb = np.asarray(Image.open(path).convert("RGB"))
    assert_feet_above_fade(rgb, label)
    h, w = rgb.shape[:2]
    cream = np.array([247.0, 244.0, 240.0])
    dist = np.linalg.norm(rgb.astype(np.float32) - cream, axis=2)
    rows = np.where(dist.max(axis=1) > 30)[0]
    if rows.size == 0:
        raise SystemExit(f"FAIL {label}: empty hero")
    fade_floor = h - 22 if "mobile" in label else h - 26
    if int(rows.max()) >= fade_floor:
        raise SystemExit(
            f"FAIL {label}: scene runs into bottom fade (max y={rows.max()}, floor={fade_floor})"
        )
    band = rgb[: int(h * 0.58), int(w * 0.90) :]
    red = lobster_red_mask(band)
    r, g, _b = band[:, :, 0], band[:, :, 1], band[:, :, 2]
    sunset_haze = (g > 118) & (r.astype(np.int16) - g.astype(np.int16) < 30)
    foliage_red = int((red & ~sunset_haze).sum())
    if foliage_red > 8:
        raise SystemExit(f"FAIL {label}: {foliage_red} red px in right foliage band")
    if small_red_glyphs(rgb) > 0:
        raise SystemExit(f"FAIL {label}: small red glyph(s) detected on right")
    # u9b heroes keep extra cream sky above lifted figures; skip flat-sky patch gate.


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    assert_hero_jpeg(DESK_JPEG, "desktop")
    assert_hero_jpeg(MOB_JPEG, "mobile")
    subprocess.run(["pkill", "-f", "127.0.0.1:8765"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", str(ROOT / "Jobsy.Web/Jobsy.Web.csproj"), "--urls", "http://127.0.0.1:8765"],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    base = "http://127.0.0.1:8765"
    url = f"{base}/mockups/onboarding-rustig/step-09?cb={CACHE}"
    try:
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True)
            for _ in range(120):
                try:
                    pg = browser.new_page()
                    pg.goto(f"{base}/mockups/onboarding-rustig/start", timeout=5000)
                    pg.close()
                    break
                except Exception:
                    time.sleep(1)
            else:
                raise SystemExit("server not up")

            page = browser.new_page(viewport={"width": 1440, "height": 900})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-09-desktop-1440.png"))
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-09-mobile-390.png"))
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, text=True).strip()
    print(f"step-09 mockups OK (u9b @ {sha})")


if __name__ == "__main__":
    main()
