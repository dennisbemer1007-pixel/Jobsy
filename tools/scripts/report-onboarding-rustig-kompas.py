#!/usr/bin/env python3
"""Capture kompas mockups (1440 + 390, top and scroll)."""

from __future__ import annotations

import subprocess
import time
from pathlib import Path

from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
OUT = Path("/opt/cursor/artifacts/onboarding-rustig-mockups")
CACHE = "20261020-kompas-b"


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
    url = f"{base}/mockups/onboarding-rustig/kompas?cb={CACHE}"
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
            page.wait_for_timeout(500)
            page.screenshot(path=str(OUT / "kompas-a-desktop-1440-top.png"))
            page.evaluate("window.scrollTo(0, Math.min(document.body.scrollHeight, 920))")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "kompas-a-desktop-1440-mid.png"))
            page.evaluate("window.scrollTo(0, document.body.scrollHeight)")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "kompas-a-desktop-1440-bottom.png"))
            page.close()

            page = browser.new_page(viewport={"width": 390, "height": 844})
            page.goto(url, wait_until="networkidle")
            page.wait_for_selector("[data-testid=ob-rustig-hero]", timeout=30_000)
            page.wait_for_timeout(500)
            page.screenshot(path=str(OUT / "kompas-a-mobile-390-top.png"))
            page.evaluate("window.scrollTo(0, Math.min(document.body.scrollHeight, 900))")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "kompas-a-mobile-390-mid.png"))
            page.evaluate("window.scrollTo(0, document.body.scrollHeight)")
            page.wait_for_timeout(400)
            page.screenshot(path=str(OUT / "kompas-a-mobile-390-bottom.png"))
            page.close()
            browser.close()
    finally:
        proc.terminate()
        proc.wait(timeout=30)
    sha = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, text=True).strip()
    print(f"kompas mockups OK (kompas-a @ {sha})")


if __name__ == "__main__":
    main()
