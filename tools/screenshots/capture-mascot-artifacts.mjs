import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const artifacts = process.env.CURSOR_ARTIFACTS_DIR || "/opt/cursor/artifacts";
await mkdir(artifacts, { recursive: true });
const scriptDir = path.dirname(fileURLToPath(import.meta.url));

const browser = await chromium.launch();
const page = await browser.newPage();

const overview = path.join(artifacts, "mascot-overview.html");
await page.goto(`file://${overview}`);
await page.setViewportSize({ width: 900, height: 700 });
await page.screenshot({ path: path.join(artifacts, "mascot-overview-6-poses.png"), fullPage: true });

const livePreview = path.join(artifacts, "mascot-app-live-preview.html");
const inApp = path.join(scriptDir, "mascot-in-app-preview.html");
const appPreview = (await import("node:fs/promises")).access(livePreview).then(() => livePreview).catch(() => inApp);
await page.goto(`file://${await appPreview}`);
await page.setViewportSize({ width: 1280, height: 800 });
await page.screenshot({ path: path.join(artifacts, "mascot-app-desktop-1280x800.png"), fullPage: true });
await page.setViewportSize({ width: 390, height: 844 });
await page.screenshot({ path: path.join(artifacts, "mascot-app-mobile-390x844.png"), fullPage: true });

await browser.close();
console.log("Saved mascot screenshots to", artifacts);
