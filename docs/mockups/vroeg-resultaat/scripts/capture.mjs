import { chromium } from "playwright";
import { fileURLToPath } from "url";
import path from "path";
import fs from "fs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const agentStore = "/cursor/stores/self/vroeg-resultaat";

const variants = [
  { id: "A", mobile: "A/early-reward.html" },
  { id: "B", mobile: "B/early-reward.html", desktop: "B/desktop-1440.html" },
  { id: "C", mobile: "C/early-reward.html" },
];

const MOBILE = { width: 390, height: 844 };
const DESKTOP = { width: 1440, height: 900 };

async function shot(page, filePath, outPath, viewport, scale) {
  await page.setViewportSize(viewport);
  await page.goto(`file://${filePath}`, { waitUntil: "networkidle" });
  await page.waitForTimeout(250);
  await page.screenshot({ path: outPath, fullPage: false, scale: scale === 2 ? "device" : "css" });
}

async function main() {
  fs.mkdirSync(agentStore, { recursive: true });
  const browser = await chromium.launch();

  for (const v of variants) {
    const mobileHtml = path.join(root, v.mobile);
    const dir = path.join(root, v.id);

    for (const [suffix, scale] of [
      ["mobile-390x844", 1],
      ["mobile-390x844@2x", 2],
    ]) {
      const context = await browser.newContext({
        viewport: MOBILE,
        deviceScaleFactor: scale,
      });
      const page = await context.newPage();
      const out = path.join(dir, `${suffix}.png`);
      await page.goto(`file://${mobileHtml}`, { waitUntil: "networkidle" });
      await page.waitForTimeout(250);
      await page.screenshot({ path: out, fullPage: false });
      console.log("Wrote", out);
      await context.close();
    }

    if (v.desktop) {
      const desktopHtml = path.join(root, v.desktop);
      const context = await browser.newContext({
        viewport: DESKTOP,
        deviceScaleFactor: 1,
      });
      const page = await context.newPage();
      const out = path.join(dir, "desktop-1440x900.png");
      await page.goto(`file://${desktopHtml}`, { waitUntil: "networkidle" });
      await page.waitForTimeout(250);
      await page.screenshot({ path: out, fullPage: false });
      console.log("Wrote", out);
      await context.close();
    }
  }

  const overviewContext = await browser.newContext({
    viewport: { width: 1280, height: 920 },
    deviceScaleFactor: 2,
  });
  const overviewPage = await overviewContext.newPage();
  await overviewPage.goto(`file://${path.join(root, "overview.html")}`, {
    waitUntil: "networkidle",
  });
  await overviewPage.waitForTimeout(300);
  const overviewOut = path.join(root, "overview-mobile-variants.png");
  await overviewPage.screenshot({ path: overviewOut, fullPage: true });
  console.log("Wrote", overviewOut);
  await overviewContext.close();

  await browser.close();

  for (const name of fs.readdirSync(root)) {
    const full = path.join(root, name);
    if (name.endsWith(".png")) {
      fs.copyFileSync(full, path.join(agentStore, name));
    }
  }
  for (const v of variants) {
    const dir = path.join(root, v.id);
    fs.mkdirSync(path.join(agentStore, v.id), { recursive: true });
    for (const f of fs.readdirSync(dir)) {
      if (f.endsWith(".png")) {
        fs.copyFileSync(path.join(dir, f), path.join(agentStore, v.id, f));
      }
    }
  }
  console.log("Copied PNGs to", agentStore);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
