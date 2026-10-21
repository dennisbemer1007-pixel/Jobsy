#!/usr/bin/env python3
"""Logged-in candidate: time to map canvas + pins HTTP fetch count."""
from __future__ import annotations

import asyncio
import json
import os
import sys
import time

from playwright.async_api import async_playwright


async def measure(base_url: str, width: int, height: int, mobile: bool) -> dict:
    pins_urls: list[str] = []
    map_view_urls: list[str] = []
    discover_urls: list[str] = []

    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        context = await browser.new_context(
            viewport={"width": width, "height": height},
            is_mobile=mobile,
            has_touch=mobile,
        )
        await context.add_init_script(
            "try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}"
        )
        page = await context.new_page()

        def on_req(req):
            u = req.url
            if "/api/vacancies/pins" in u:
                pins_urls.append(u)
            if "/api/vacancies/map-view" in u:
                map_view_urls.append(u)
            if "/api/vacancies/discover" in u:
                discover_urls.append(u)

        page.on("request", on_req)

        await page.goto(base_url + "/login", wait_until="domcontentloaded", timeout=120_000)
        await page.locator("input[name='email'], input[type=email]").first.fill(
            os.environ.get("JOBSY_E2E_CANDIDATE_EMAIL", "kandidaat@jobsy.local")
        )
        await page.locator("input[name='password'], input[type='password']").first.fill(
            os.environ.get("JOBSY_E2E_CANDIDATE_PASSWORD", "Jobsy123!")
        )
        await page.locator("button[type=submit]").first.click()
        await page.wait_for_url(
            lambda url: "/login" not in url.lower(),
            timeout=120_000,
        )

        t0 = time.perf_counter()
        await page.goto(base_url + "/banenkaart", wait_until="domcontentloaded", timeout=120_000)
        await page.wait_for_function(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            timeout=120_000,
        )
        await page.wait_for_timeout(2000)
        ms = (time.perf_counter() - t0) * 1000

        net_pins = await page.evaluate(
            "() => (window.jobMap && window.jobMap.__testGetPinsNetworkFetchCount)"
            " ? window.jobMap.__testGetPinsNetworkFetchCount() : -1"
        )

        await browser.close()

    return {
        "firstRenderMs": round(ms, 1),
        "pinsRequests": len(pins_urls),
        "discoverRequests": len(discover_urls),
        "mapViewRequests": len(map_view_urls),
        "pinsNetworkFetchCount": net_pins,
    }


async def main() -> int:
    base = (os.environ.get("JOBSY_E2E_BASE_URL") or "http://127.0.0.1:5201").rstrip("/")
    label = os.environ.get("JOBSY_BANENKAART_PERF_LABEL", "run")
    out = {"label": label, "baseUrl": base, "viewports": {}}
    for name, w, h, m in [("desktop", 1280, 900, False), ("mobile", 390, 844, True)]:
        out["viewports"][name] = await measure(base, w, h, m)
    print(json.dumps(out, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
