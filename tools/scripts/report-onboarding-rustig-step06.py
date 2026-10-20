#!/usr/bin/env python3
"""Capture step-06 mockups (1440 desktop + 390 mobile) — illustrated u6-aquarel."""

from __future__ import annotations

import importlib.util
import subprocess
import time
from pathlib import Path

import numpy as np
from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
DESK_JPEG = (
    ROOT / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/step-06-droom-professions-u6-aquarel.jpg"
)
MOB_JPEG = (
    ROOT
    / "Jobsy.Web/wwwroot/images/onboarding/rustig-mockup/step-06-droom-professions-u6-aquarel-mobile.jpg"
)
CACHE = "20261020-u6-aquarel-v7"


def assert_illustration(path: Path, label: str) -> None:
    rgb = np.asarray(Image.open(path).convert("RGB"))
    h, w = rgb.shape[:2]
    cream = np.array([247.0, 244.0, 240.0])
    dist = np.linalg.norm(rgb.astype(np.float32) - cream, axis=2)
    if (dist > 28).sum() < w * h * 0.08:
        raise SystemExit(f"FAIL {label}: illustration too empty")
    rows = np.where(dist.max(axis=1) > 30)[0]
    if rows.size == 0:
        raise SystemExit(f"FAIL {label}: no figure content")
    if rows.max() < h * 0.55:
        raise SystemExit(f"FAIL {label}: feet may be clipped (max y={rows.max()})")


def assert_desktop_hero_metrics(page, desk_path: Path) -> None:
    spec = importlib.util.spec_from_file_location(
        "fin", ROOT / "tools/scripts/finalize-onboarding-rustig-panoramas.py"
    )
    fin = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(fin)
    metrics = page.evaluate(
        """() => {
          const main = document.querySelector('.ob-rustig__main');
          const img = document.querySelector('[data-testid=ob-rustig-hero] img');
          if (!main || !img) return null;
          const mr = main.getBoundingClientRect();
          const ir = img.getBoundingClientRect();
          return { mainW: mr.width, imgW: ir.width, imgH: ir.height };
        }"""
    )
    if not metrics:
        raise SystemExit("FAIL desktop: hero metrics unavailable")
    if abs(metrics["mainW"] - metrics["imgW"]) > 6:
        raise SystemExit(
            f"FAIL desktop: hero width {metrics['imgW']:.0f}px != column {metrics['mainW']:.0f}px"
        )
    if metrics["imgH"] < 280 or metrics["imgH"] > 345:
        raise SystemExit(f"FAIL desktop: hero height {metrics['imgH']:.0f}px outside 280-345")
    if metrics["imgH"] / metrics["imgW"] < 0.26:
        raise SystemExit("FAIL desktop: hero looks letterboxed (height/width too small)")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    assert_illustration(DESK_JPEG, "desktop-art")
    assert_illustration(MOB_JPEG, "mobile-art")
    subprocess.run(["pkill", "-f", "127.0.0.1:8765"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", str(ROOT / "Jobsy.Web/Jobsy.Web.csproj"), "--urls", "http://127.0.0.1:8765"],
        cwd=ROOT,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    base = "http://127.0.0.1:8765"
    url = f"{base}/mockups/onboarding-rustig/step-06?cb={CACHE}"
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
            assert_desktop_hero_metrics(page, DESK_JPEG)
            page.screenshot(path=str(OUT / "step-06-desktop-1440.png"))
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-06-mobile.png"))
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, text=True).strip()
    print(f"step-06 mockups OK (u6-aquarel-v7 @ {sha})")


if __name__ == "__main__":
    main()
