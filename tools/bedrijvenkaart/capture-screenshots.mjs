#!/usr/bin/env node
/**
 * Capture bedrijvenkaart screens for artifact comparison (R2).
 * Usage: node tools/bedrijvenkaart/capture-screenshots.mjs [baseUrl]
 */
import { mkdir, copyFile } from "node:fs/promises";
import { join } from "node:path";
import { chromium } from "../screenshots/node_modules/playwright/index.mjs";

const baseUrl = (process.argv[2] ?? "http://localhost:5201").replace(/\/$/, "");
const outDir = "/opt/cursor/artifacts/bedrijvenkaart-build-r2";
const refDir = "/opt/cursor/artifacts/bedrijvenkaart-build-r1/reference";

const shots = [
  { file: "00-banenkaart-link-desktop.png", path: "/preview/bedrijvenkaart/00-banenkaart-link-desktop", width: 1440 },
  { file: "00-banenkaart-link-mobiel.png", path: "/preview/bedrijvenkaart/00-banenkaart-link-mobiel", width: 390 },
  { file: "01-bedrijvenkaart-desktop.png", path: "/bedrijvenkaart", width: 1440 },
  { file: "01-bedrijvenkaart-mobiel.png", path: "/bedrijvenkaart", width: 390 },
  { file: "02-bedrijfspagina-desktop.png", path: "/preview/bedrijvenkaart/02-bedrijfspagina-desktop", width: 1440 },
  { file: "02-bedrijfspagina-mobiel.png", path: "/preview/bedrijvenkaart/02-bedrijfspagina-mobiel", width: 390 },
  { file: "02b-bedrijfspagina-zonder-vacatures-desktop.png", path: "/preview/bedrijvenkaart/02b-bedrijfspagina-zonder-vacatures-desktop", width: 1440 },
  { file: "02b-bedrijfspagina-zonder-vacatures-mobiel.png", path: "/preview/bedrijvenkaart/02b-bedrijfspagina-zonder-vacatures-mobiel", width: 390 },
  { file: "03-bericht-versturen-popup-desktop.png", path: "/preview/bedrijvenkaart/03-bericht-versturen-popup-desktop", width: 1440 },
  { file: "03-bericht-versturen-popup-mobiel.png", path: "/preview/bedrijvenkaart/03-bericht-versturen-popup-mobiel", width: 390 },
  { file: "03b-verstuurd-modal-desktop.png", path: "/preview/bedrijvenkaart/03b-verstuurd-modal-desktop", width: 1440 },
  { file: "03b-verstuurd-modal-mobiel.png", path: "/preview/bedrijvenkaart/03b-verstuurd-modal-mobiel", width: 390 },
  { file: "03c-weeklimiet-modal-desktop.png", path: "/preview/bedrijvenkaart/03c-weeklimiet-modal-desktop", width: 1440 },
  { file: "03c-weeklimiet-modal-mobiel.png", path: "/preview/bedrijvenkaart/03c-weeklimiet-modal-mobiel", width: 390 },
  { file: "04-sollicitaties-initiatieven-desktop.png", path: "/preview/bedrijvenkaart/04-sollicitaties-initiatieven-desktop", width: 1440, login: "kandidaat@jobsy.local" },
  { file: "04-sollicitaties-initiatieven-mobiel.png", path: "/preview/bedrijvenkaart/04-sollicitaties-initiatieven-mobiel", width: 390, login: "kandidaat@jobsy.local" },
  { file: "04b-automatisch-nu-niet-desktop.png", path: "/preview/bedrijvenkaart/04b-automatisch-nu-niet-desktop", width: 1440, login: "kandidaat@jobsy.local" },
  { file: "04b-automatisch-nu-niet-mobiel.png", path: "/preview/bedrijvenkaart/04b-automatisch-nu-niet-mobiel", width: 390, login: "kandidaat@jobsy.local" },
  { file: "05-w-instellingen-desktop.png", path: "/werkgever/open-voor-initiatief", width: 1440, login: "bedrijfsmanager@jobsy.local" },
  { file: "05b-w-team-toevoegen-modal-desktop.png", path: "/preview/bedrijvenkaart/05b-w-team-toevoegen-modal-desktop", width: 1440 },
  { file: "05c-w-email-ontbreekt-desktop.png", path: "/preview/bedrijvenkaart/05c-w-email-ontbreekt-desktop", width: 1440 },
  { file: "06-w-sollicitaties-desktop.png", path: "/preview/bedrijvenkaart/06-w-sollicitaties-desktop", width: 1440 },
  { file: "06b-w-accepteren-modal-desktop.png", path: "/preview/bedrijvenkaart/06b-w-accepteren-modal-desktop", width: 1440 },
  { file: "06c-w-na-accepteren-desktop.png", path: "/preview/bedrijvenkaart/06c-w-na-accepteren-desktop", width: 1440 },
  { file: "07-w-nu-niet-modal-desktop.png", path: "/preview/bedrijvenkaart/07-w-nu-niet-modal-desktop", width: 1440 },
];

async function demoLogin(page, email) {
  await page.goto(`${baseUrl}/account/login`, { waitUntil: "domcontentloaded" });
  await page.evaluate((addr) => {
    const f = document.createElement("form");
    f.method = "POST";
    f.action = "/account/demo-login";
    const i = document.createElement("input");
    i.type = "hidden";
    i.name = "email";
    i.value = addr;
    f.appendChild(i);
    document.body.appendChild(f);
    f.submit();
  }, email);
  await page.waitForLoadState("networkidle", { timeout: 60_000 }).catch(() => {});
}

await mkdir(outDir, { recursive: true });
try {
  await mkdir(join(outDir, "reference"), { recursive: true });
  for (const name of [
    "00-banenkaart-link-desktop.png",
    "01-bedrijvenkaart-desktop.png",
  ]) {
    try {
      await copyFile(join(refDir, name), join(outDir, "reference", name));
    } catch {
      /* optional */
    }
  }
} catch {
  /* ignore */
}

const browser = await chromium.launch();
for (const shot of shots) {
  const context = await browser.newContext({ viewport: { width: shot.width, height: 900 } });
  const page = await context.newPage();
  try {
    if (shot.login) {
      await demoLogin(page, shot.login);
    }
    await page.goto(`${baseUrl}${shot.path}`, { waitUntil: "networkidle", timeout: 120_000 });
    await page.waitForTimeout(shot.path.includes("bedrijvenkaart") ? 1500 : 500);
    await page.screenshot({ path: join(outDir, shot.file), fullPage: true });
    console.log("saved", shot.file);
  } catch (e) {
    console.warn("skip", shot.file, e.message);
  } finally {
    await context.close();
  }
}
await browser.close();

console.log("Artifacts:", outDir);
