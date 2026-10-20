#!/usr/bin/env python3
"""Full-page start mockup captures (1440 + 390)."""

from __future__ import annotations

import subprocess
import time
from pathlib import Path

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
CACHE = "20261020-start-b"


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

            for w, h, label in ((1440, 900, "desktop-1440"), (390, 844, "mobile-390")):
                page = browser.new_page(viewport={"width": w, "height": h})
                page.goto(url, wait_until="networkidle")
                page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
                page.wait_for_timeout(600)
                page.screenshot(path=str(OUT / f"start-b-{label}-full.png"), full_page=True)
                page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, text=True).strip()
    print(f"start-b full page OK @ {sha}")


if __name__ == "__main__":
    main()
