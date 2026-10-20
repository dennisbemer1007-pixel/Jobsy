#!/usr/bin/env python3
"""Full-page start mockup captures (1440 + 390) with hero load gate."""

from __future__ import annotations

import json
import subprocess
import time
from pathlib import Path

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
CACHE = "20261020-start-c"
PREFIX = "start-c"


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
            metrics["desktop_hero"] = desk
            if desk["naturalWidth"] <= 0:
                raise SystemExit(f"desktop hero not loaded: {desk}")
            page.wait_for_timeout(400)
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
            metrics["mobile_hero"] = mob
            if mob["naturalWidth"] <= 0:
                raise SystemExit(f"mobile hero not loaded: {mob}")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / f"{PREFIX}-mobile-390-full.png"), full_page=True)
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, text=True).strip()
    payload = {"commit": sha, "cache": CACHE, **metrics}
    (OUT / f"{PREFIX}-hero-metrics.json").write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    print(f"{PREFIX} full page OK @ {sha}")
    print(json.dumps(payload, indent=2))


if __name__ == "__main__":
    main()
