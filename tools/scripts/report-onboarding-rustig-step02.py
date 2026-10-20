#!/usr/bin/env python3
"""Capture step-02 mockups (1440 desktop + 390 mobile). No lobster gate."""

from __future__ import annotations

import io
import subprocess
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
HERO_JPEG = ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-panorama/step-02-desktop.jpg"


def lobster_red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 110) & (r > g + 18) & (r > b + 18) & (g < 145) & (b < 145)


def assert_framing(desk_jpeg: Path) -> None:
    rgb = np.asarray(Image.open(desk_jpeg).convert("RGB"), dtype=np.float32)
    h, w = rgb.shape[:2]
    cream = np.array([247.0, 244.0, 240.0])
    dist = np.linalg.norm(rgb - cream, axis=2)
    rows = np.where(dist.max(axis=1) > 30)[0]
    if rows.size == 0 or rows.max() < h * 0.94:
        raise SystemExit(f"FAIL: scene bottom too high (max y={rows.max() if rows.size else 0}, need >={h*0.94:.0f})")
    if dist[int(h * 0.88) :, : int(w * 0.22)].max() < 35:
        raise SystemExit("FAIL: left foreground (bike path) looks clipped")
    red = int(
        lobster_red_mask(rgb.astype(np.uint8))[: int(h * 0.58), int(w * 0.88) :].sum()
    )
    if red > 0:
        raise SystemExit(f"FAIL: {red} lobster-red pixels in foliage (x>=0.88, y<58%)")


def right_edge_4x(desk_jpeg: Path) -> Path:
    im = Image.open(desk_jpeg).convert("RGB")
    w, h = im.size
    x0 = int(w * 0.90)
    crop = im.crop((x0, 0, w, h))
    zoom = crop.resize((crop.size[0] * 4, crop.size[1] * 4), Image.Resampling.LANCZOS)
    zoom = zoom.filter(ImageFilter.UnsharpMask(radius=1, percent=60, threshold=2))
    out = OUT / "step-02-right-edge-4x.png"
    zoom.save(out)
    red = int(lobster_red_mask(np.asarray(im.crop((x0, 0, w, h)))).sum())
    if red > 0:
        raise SystemExit(f"FAIL: {red} lobster-red pixels in desktop x=0.90–1.0")
    return out


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    subprocess.run(["pkill", "-f", "127.0.0.1:8765"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", str(ROOT / "Jobsy.Web/Jobsy.Web.csproj"), "--urls", "http://127.0.0.1:8765"],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    base = "http://127.0.0.1:8765"
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
            page.goto(
                f"{base}/mockups/onboarding-rustig/step-02?cb=20261020-u6s",
                wait_until="networkidle",
            )
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-02-desktop-1440.png"))
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(
                f"{base}/mockups/onboarding-rustig/step-02?cb=20261020-u6s",
                wait_until="networkidle",
            )
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-02-mobile.png"))
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    assert_framing(HERO_JPEG)
    print("step-02 mockups OK (framing + no red right)")


if __name__ == "__main__":
    main()
