import { chromium } from "playwright";
import { fileURLToPath } from "url";
import path from "path";
import fs from "fs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const directions = ["A", "B", "C"];
const steps = [
  { id: "step-1", file: "step-1.html", label: "Stap 1 — Even kennismaken" },
  { id: "step-1-suggestions", file: "step-1-suggestions.html", label: "Stap 1 — Adresuggesties" },
  { id: "step-3", file: "step-3.html", label: "Stap 3 — Vervoer" },
  { id: "step-5", file: "step-5.html", label: "Stap 5 — Opleiding" },
  { id: "step-7", file: "step-7.html", label: "Stap 7 — Toestemming" },
  { id: "step-8", file: "step-8.html", label: "Stap 8 — Mini-test" },
];

const MOBILE = { suffix: "mobile-390x844", width: 390, height: 844 };

function writeOverview(dir, letter) {
  const shots = steps
    .map(
      (s) => `<div class="shot"><h2>${s.label}</h2><img src="${s.id}-${MOBILE.suffix}.png" alt="" /></div>`
    )
    .join("\n    ");

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
    h1 { margin: 0 0 6px; font-size: 1.35rem; font-weight: 700; }
    p.lead { margin: 0 0 24px; color: #5a6a7d; font-size: 0.9375rem; }
    .grid {
      display: grid;
      grid-template-columns: repeat(3, 390px);
      gap: 20px;
      justify-content: center;
    }
    .shot h2 {
      margin: 0 0 8px;
      font-size: 0.8125rem;
      font-weight: 700;
      text-align: center;
      line-height: 1.25;
      min-height: 2.5em;
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
  <p class="lead">Stappen 1, 1 (suggesties), 3, 5, 7 en 8 · desktop blijft huidige app</p>
  <div class="grid">
    ${shots}
  </div>
</body>
</html>`;
  fs.writeFileSync(path.join(dir, "overview.html"), html, "utf8");
}

function removeLegacyDesktopPngs(dir) {
  for (const name of fs.readdirSync(dir)) {
    if (name.includes("desktop-1280x800")) {
      fs.unlinkSync(path.join(dir, name));
    }
  }
}

async function main() {
  const browser = await chromium.launch();
  const agentStore = "/cursor/stores/self/onboarding-ontwerp";

  for (const letter of directions) {
    const dir = path.join(root, letter);
    removeLegacyDesktopPngs(dir);
    writeOverview(dir, letter);

    const context = await browser.newContext({
      viewport: { width: MOBILE.width, height: MOBILE.height },
      deviceScaleFactor: 2,
    });
    const page = await context.newPage();

    for (const step of steps) {
      const filePath = path.join(dir, step.file);
      await page.goto(`file://${filePath}`, { waitUntil: "networkidle" });
      await page.waitForTimeout(200);
      const outPath = path.join(dir, `${step.id}-${MOBILE.suffix}.png`);
      await page.screenshot({ path: outPath, fullPage: false });
      console.log("Wrote", outPath);
    }
    await context.close();

    const overviewContext = await browser.newContext({
      viewport: { width: 1280, height: 1900 },
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
