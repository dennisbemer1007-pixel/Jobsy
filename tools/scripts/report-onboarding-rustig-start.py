#!/usr/bin/env python3
"""Start mockup: desktop full page + hero 2x zoom; mobile full (unchanged asset)."""

from __future__ import annotations

import json
import shutil
import subprocess
import time
from pathlib import Path

from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
METRICS_SRC = ROOT / "artifacts/onboarding-rustig-panorama-raw/start-desktop-band-metrics.json"
CACHE = "20261020-start-e"
PREFIX = "start-e"


def save_hero_2x(page, path: Path) -> None:
    hero = page.locator("[data-testid=ob-rustig-hero-desk]")
    shot = OUT / f"{PREFIX}-hero-desk-shot.png"
    hero.screenshot(path=str(shot))
    im = Image.open(shot)
    zoom = im.resize((im.size[0] * 2, im.size[1] * 2), Image.Resampling.LANCZOS)
    zoom.save(path, optimize=True)


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
    url = f"{base}/mockups/onboarding-rustig/start?cb={CACHE}"
    metrics: dict = {}
    if METRICS_SRC.is_file():
        metrics["band_build"] = json.loads(METRICS_SRC.read_text(encoding="utf-8"))
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
            page.wait_for_selector("[data-testid=ob-rustig-hero-desk]", timeout=30_000)
            page.wait_for_function(
                """() => {
                  const img = document.querySelector('[data-testid=ob-rustig-hero-desk]');
                  return img && img.complete && img.naturalWidth > 0;
                }""",
                timeout=30_000,
            )
            desk = page.locator("[data-testid=ob-rustig-hero-desk]").evaluate(
                "el => ({ src: el.currentSrc || el.src, naturalWidth: el.naturalWidth, naturalHeight: el.naturalHeight })"
            )
            metrics["desktop_hero_render"] = desk
            save_hero_2x(page, OUT / f"{PREFIX}-hero-desktop-2x.png")
            page.screenshot(path=str(OUT / f"{PREFIX}-desktop-1440-full.png"), full_page=True)
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero-mob]", timeout=30_000)
            page.wait_for_function(
                """() => {
                  const img = document.querySelector('[data-testid=ob-rustig-hero-mob]');
                  return img && img.complete && img.naturalWidth > 0;
                }""",
                timeout=30_000,
            )
            mob = page.locator("[data-testid=ob-rustig-hero-mob]").evaluate(
                "el => ({ src: el.currentSrc || el.src, naturalWidth: el.naturalWidth, naturalHeight: el.naturalHeight })"
            )
            metrics["mobile_hero_render"] = mob
            page.screenshot(path=str(OUT / f"{PREFIX}-mobile-390-full.png"), full_page=True)
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, text=True).strip()
    payload = {"commit": sha, "cache": CACHE, **metrics}
    (OUT / f"{PREFIX}-hero-metrics.json").write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    if METRICS_SRC.is_file():
        shutil.copy(METRICS_SRC, OUT / f"{PREFIX}-band-metrics.json")
    print(f"{PREFIX} OK @ {sha}")
    print(json.dumps(payload, indent=2))


if __name__ == "__main__":
    main()
