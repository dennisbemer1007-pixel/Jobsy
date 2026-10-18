#!/usr/bin/env node
/**
 * Capture bedrijvenkaart preview screens for artifact comparison.
 * Usage: node tools/bedrijvenkaart/capture-screenshots.mjs [baseUrl]
 */
import { mkdir, copyFile } from "node:fs/promises";
import { join } from "node:path";
import { chromium } from "playwright";

const baseUrl = process.argv[2] ?? "http://localhost:5201";
const outDir = "/opt/cursor/artifacts/bedrijvenkaart-build-r1";

const shots = [
  { file: "00-banenkaart-link-desktop.png", path: "/preview/bedrijvenkaart/01-desktop", width: 1440 },
  { file: "00-banenkaart-link-mobiel.png", path: "/preview/bedrijvenkaart/01-mobiel", width: 390 },
  { file: "01-bedrijvenkaart-desktop.png", path: "/bedrijvenkaart", width: 1440 },
  { file: "01-bedrijvenkaart-mobiel.png", path: "/bedrijvenkaart", width: 390 },
  { file: "05-w-instellingen-desktop.png", path: "/werkgever/open-voor-initiatief", width: 1440 },
];

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();
for (const shot of shots) {
  const page = await browser.newPage({ viewport: { width: shot.width, height: 900 } });
  try {
    await page.goto(`${baseUrl}${shot.path}`, { waitUntil: "networkidle", timeout: 120_000 });
    await page.screenshot({ path: join(outDir, shot.file), fullPage: true });
    console.log("saved", shot.file);
  } catch (e) {
    console.warn("skip", shot.file, e.message);
  } finally {
    await page.close();
  }
}
await browser.close();

console.log("Artifacts:", outDir);
