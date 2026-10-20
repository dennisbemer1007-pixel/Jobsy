#!/usr/bin/env python3
"""Capture step-02 mockups (1440 desktop + 390 mobile). No lobster gate."""

from __future__ import annotations

import io
import subprocess
import sys
import time
from pathlib import Path

from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")


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
            page.goto(f"{base}/mockups/onboarding-rustig/step-02", wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-02-desktop-1440.png"))
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(f"{base}/mockups/onboarding-rustig/step-02", wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "step-02-mobile.png"))
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    print("step-02 mockups OK")


if __name__ == "__main__":
    main()
