#!/usr/bin/env python3
"""Capture banenkaart map state in first 3s after load (logged-in candidate)."""
from __future__ import annotations

import asyncio
import json
import os
from pathlib import Path

from playwright.async_api import async_playwright

OUT = Path(os.environ.get("JOBSY_ARTIFACTS_DIR", "/opt/cursor/artifacts"))
OUT.mkdir(parents=True, exist_ok=True)


async def capture_viewport(base: str, name: str, width: int, height: int, mobile: bool) -> dict:
    shots: list[str] = []
    metrics: list[dict] = []

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

        await page.goto(base + "/login", wait_until="domcontentloaded", timeout=120_000)
        await page.evaluate(
            "() => { document.documentElement.classList.add('cookie-consent-known'); }"
        )
        await page.locator("input[name='email'], input[type=email]").first.fill(
            os.environ.get("JOBSY_E2E_CANDIDATE_EMAIL", "kandidaat@jobsy.local")
        )
        await page.locator("input[name='password'], input[type='password']").first.fill(
            os.environ.get("JOBSY_E2E_CANDIDATE_PASSWORD", "Jobsy123!")
        )
        await page.locator("button[type=submit]").first.click()
        await page.wait_for_url(lambda url: "/login" not in url.lower(), timeout=120_000)

        await page.goto(base + "/banenkaart", wait_until="commit", timeout=120_000)

        prev_ms = 0
        for ms in (0, 250, 500, 1000, 1500, 2000, 3000):
            if ms > prev_ms:
                await page.wait_for_timeout(ms - prev_ms)
            prev_ms = ms
            snap = await page.evaluate(
                """() => {
                  const canvas = document.querySelector('#job-map canvas');
                  const stage = document.querySelector('.map-stage');
                  const boot = document.getElementById('jobsy-map-boot');
                  let defer = false;
                  try {
                    defer = !!(boot && JSON.parse(boot.textContent).deferPinsWarmup);
                  } catch (e) {}
                  const jm = window.jobMap;
                  const ml = jm && jm.__testGetMap ? jm.__testGetMap() : null;
                  let center = null;
                  let zoom = null;
                  if (ml && typeof ml.getCenter === 'function') {
                    const c = ml.getCenter();
                    center = [c.lng, c.lat];
                    zoom = ml.getZoom();
                  }
                  return {
                    deferPinsWarmup: defer,
                    hasCanvas: !!canvas,
                    stageLive: !!(stage && stage.classList.contains('is-live')),
                    mapCreateCount: jm && jm.__testGetMapCreateCount ? jm.__testGetMapCreateCount() : null,
                    center: center,
                    zoom: zoom,
                    pinCount: jm && jm.__testGetPinCount ? jm.__testGetPinCount() : null,
                  };
                }"""
            )
            snap["tMs"] = ms
            metrics.append(snap)
            path = OUT / f"banenkaart-load-{name}-{ms}ms.png"
            box = await page.evaluate(
                """() => {
                  const el = document.querySelector('.map-stage');
                  if (!el) return null;
                  const r = el.getBoundingClientRect();
                  return { x: r.x, y: r.y, width: r.width, height: r.height };
                }"""
            )
            if box and box["width"] > 10 and box["height"] > 10:
                await page.screenshot(path=str(path), clip=box)
            else:
                await page.screenshot(path=str(path), full_page=False)
            shots.append(str(path))

        await browser.close()

    centers = [m.get("center") for m in metrics if m.get("center")]
    jumps = []
    for i in range(1, len(centers)):
        if centers[i] and centers[i - 1]:
            a, b = centers[i - 1], centers[i]
            if abs(a[0] - b[0]) > 0.02 or abs(a[1] - b[1]) > 0.02:
                jumps.append({"fromMs": metrics[i - 1]["tMs"], "toMs": metrics[i]["tMs"], "from": a, "to": b})

    return {
        "viewport": name,
        "screenshots": shots,
        "metrics": metrics,
        "cameraJumps": jumps,
        "maxMapCreateCount": max(
            (m["mapCreateCount"] for m in metrics if m.get("mapCreateCount") is not None),
            default=0,
        ),
    }


async def main() -> int:
    base = (os.environ.get("JOBSY_E2E_BASE_URL") or "http://127.0.0.1:5201").rstrip("/")
    results = []
    for name, w, h, m in [("desktop", 1280, 900, False), ("mobile", 390, 844, True)]:
        results.append(await capture_viewport(base, name, w, h, m))
    summary_path = OUT / "banenkaart-load-jump-summary.json"
    summary_path.write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(summary_path.read_text())
    return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
