import { chromium } from "playwright";
import { fileURLToPath } from "url";
import path from "path";
import fs from "fs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const directions = ["A", "B", "C"];
const steps = [
  { id: "step-1", file: "step-1.html" },
  { id: "step-3", file: "step-3.html" },
  { id: "step-8", file: "step-8.html" },
];

const viewports = [
  { suffix: "mobile-390x844", width: 390, height: 844 },
  { suffix: "desktop-1280x800", width: 1280, height: 800 },
];

function writeOverview(dir, letter) {
  const html = `<!DOCTYPE html>
<html lang="nl">
<head>
  <meta charset="utf-8" />
  <title>Richting ${letter} — mobile overzicht</title>
  <style>
    * { box-sizing: border-box; }
    body {
      margin: 0;
      padding: 28px 20px 40px;
      font-family: "Segoe UI", Helvetica Neue, Arial, sans-serif;
      background: #e8e4de;
      color: #122033;
    }
    h1 {
      margin: 0 0 6px;
      font-size: 1.35rem;
      font-weight: 700;
    }
    p.lead {
      margin: 0 0 24px;
      color: #5a6a7d;
      font-size: 0.9375rem;
    }
    .row {
      display: flex;
      flex-wrap: wrap;
      gap: 20px;
      justify-content: center;
      align-items: flex-start;
    }
    .shot {
      width: 390px;
      flex-shrink: 0;
    }
    .shot h2 {
      margin: 0 0 8px;
      font-size: 0.875rem;
      font-weight: 700;
      text-align: center;
    }
    .shot img {
      width: 390px;
      height: 844px;
      object-fit: cover;
      object-position: top;
      border-radius: 16px;
      border: 1px solid #ddd5cc;
      box-shadow: 0 12px 32px rgba(15, 45, 92, 0.14);
      background: #fffcfa;
    }
  </style>
</head>
<body>
  <h1>Richting ${letter} — mobile (390×844)</h1>
  <p class="lead">Stap 1 · Stap 3 · Stap 8 naast elkaar</p>
  <div class="row">
    <div class="shot"><h2>Stap 1 — Even kennismaken</h2><img src="step-1-mobile-390x844.png" alt="" /></div>
    <div class="shot"><h2>Stap 3 — Vervoer</h2><img src="step-3-mobile-390x844.png" alt="" /></div>
    <div class="shot"><h2>Stap 8 — Mini-test</h2><img src="step-8-mobile-390x844.png" alt="" /></div>
  </div>
</body>
</html>`;
  fs.writeFileSync(path.join(dir, "overview.html"), html, "utf8");
}

async function main() {
  const browser = await chromium.launch();
  const agentStore = "/cursor/stores/self/onboarding-ontwerp";

  for (const letter of directions) {
    const dir = path.join(root, letter);
    writeOverview(dir, letter);

    for (const vp of viewports) {
      const context = await browser.newContext({
        viewport: { width: vp.width, height: vp.height },
        deviceScaleFactor: 2,
      });
      const page = await context.newPage();

      for (const step of steps) {
        const filePath = path.join(dir, step.file);
        const url = `file://${filePath}`;
        await page.goto(url, { waitUntil: "networkidle" });
        await page.waitForTimeout(200);
        const outName = `${step.id}-${vp.suffix}.png`;
        const outPath = path.join(dir, outName);
        await page.screenshot({ path: outPath, fullPage: false });
        console.log("Wrote", outPath);
      }
      await context.close();
    }

    const overviewContext = await browser.newContext({
      viewport: { width: 1280, height: 920 },
      deviceScaleFactor: 2,
    });
    const overviewPage = await overviewContext.newPage();
    await overviewPage.goto(`file://${path.join(dir, "overview.html")}`, {
      waitUntil: "networkidle",
    });
    await overviewPage.screenshot({
      path: path.join(dir, "overview-mobile-sheet.png"),
      fullPage: true,
    });
    console.log("Wrote overview for", letter);
    await overviewContext.close();
  }

  await browser.close();

  for (const letter of directions) {
    const src = path.join(root, letter);
    const dest = path.join(agentStore, letter);
    fs.mkdirSync(dest, { recursive: true });
    for (const name of fs.readdirSync(src)) {
      if (name.endsWith(".png") || name === "README.md") {
        fs.copyFileSync(path.join(src, name), path.join(dest, name));
      }
    }
  }
  console.log("Copied PNGs + README to agent store:", agentStore);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
