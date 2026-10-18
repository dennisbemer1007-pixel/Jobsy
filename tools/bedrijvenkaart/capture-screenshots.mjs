#!/usr/bin/env node
/**
 * Bedrijvenkaart R9 captures: requires API+Web with Seed:Enabled (see run-r9-capture.sh).
 */
import { mkdir, writeFile } from "node:fs/promises";
import { join } from "node:path";
import { chromium } from "../screenshots/node_modules/playwright/index.mjs";

const baseUrl = (process.argv[2] ?? "http://localhost:5201").replace(/\/$/, "");
const apiUrl = (process.argv[3] ?? "http://127.0.0.1:5200").replace(/\/$/, "");
const outDir = "/opt/cursor/artifacts/bedrijvenkaart-build-r9/live";
const password = "Jobsy123!";
const candidateEmail = "sanne@jobsy.local";
const employerEmail = "enterprise@jobsy.local";

const pendingInitiative = "88888888-8888-8888-8888-888888888803";
const acceptedInitiative = "88888888-8888-8888-8888-888888888801";

const forbidden = ["deze pagina bestaat niet", "404", "not-found-view", "common.change", "initiative.panel.changeteam"];

const shots = [
  { file: "00-banenkaart-link-desktop.png", path: "/banenkaart", width: 1440, role: "candidate", expect: "Bedrijvenkaart" },
  { file: "00-banenkaart-link-mobiel.png", path: "/banenkaart", width: 390, role: "candidate", expect: "Bedrijvenkaart" },
  { file: "01-bedrijvenkaart-desktop.png", path: "/bedrijvenkaart", width: 1440, role: "candidate", expect: "Bedrijven" },
  { file: "01-bedrijvenkaart-mobiel.png", path: "/bedrijvenkaart", width: 390, role: "candidate", expect: "Bedrijven" },
  { file: "02-bedrijfspagina-desktop.png", path: "/12345678", width: 1440, role: "candidate", expect: "Korenaar" },
  { file: "02-bedrijfspagina-mobiel.png", path: "/12345678", width: 390, role: "candidate", expect: "Korenaar" },
  { file: "02b-bedrijfspagina-zonder-vacatures-desktop.png", path: "/12345678/0003", width: 1440, role: "anonymous", expect: "Korenaar" },
  { file: "02b-bedrijfspagina-zonder-vacatures-mobiel.png", path: "/12345678/0003", width: 390, role: "anonymous", expect: "Korenaar" },
  { file: "03-bericht-versturen-popup-desktop.png", path: "/12345678?demoKnock=1", width: 1440, role: "candidate", expect: "Klop aan" },
  { file: "03-bericht-versturen-popup-mobiel.png", path: "/12345678?demoKnock=1", width: 390, role: "candidate", expect: "Klop aan" },
  { file: "03b-verstuurd-modal-desktop.png", path: "/12345678?demoSent=1", width: 1440, role: "candidate", expect: "Verstuurd" },
  { file: "03b-verstuurd-modal-mobiel.png", path: "/12345678?demoSent=1", width: 390, role: "candidate", expect: "Verstuurd" },
  { file: "03c-weeklimiet-modal-desktop.png", path: "/12345678?demoWeekLimit=1", width: 1440, role: "candidate", expect: "week" },
  { file: "03c-weeklimiet-modal-mobiel.png", path: "/12345678?demoWeekLimit=1", width: 390, role: "candidate", expect: "week" },
  { file: "04-sollicitaties-initiatieven-desktop.png", path: "/candidate/applications/initiatieven", width: 1440, role: "candidate", expect: "Bakkerij" },
  { file: "04-sollicitaties-initiatieven-mobiel.png", path: "/candidate/applications/initiatieven", width: 390, role: "candidate", expect: "Bakkerij" },
  { file: "04b-automatisch-nu-niet-desktop.png", path: "/candidate/applications/initiatieven/automatisch-nu-niet", width: 1440, role: "candidate", expect: "Automatisch" },
  { file: "04b-automatisch-nu-niet-mobiel.png", path: "/candidate/applications/initiatieven/automatisch-nu-niet", width: 390, role: "candidate", expect: "Automatisch" },
  { file: "05-w-instellingen-desktop.png", path: "/werkgever/open-voor-initiatief", width: 1440, role: "employer", expect: "Open voor initiatief" },
  { file: "05b-w-team-toevoegen-modal-desktop.png", path: "/werkgever/open-voor-initiatief?demoTeamAdd=1", width: 1440, role: "employer", expect: "Team" },
  { file: "05c-w-email-ontbreekt-desktop.png", path: "/werkgever/open-voor-initiatief?demoEmailMissing=1", width: 1440, role: "employer", expect: "e-mail" },
  {
    file: "06-w-sollicitaties-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${pendingInitiative}`,
    width: 1440,
    role: "employer",
    expect: "OPEN",
  },
  {
    file: "06b-w-accepteren-modal-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${pendingInitiative}&demoAccept=1`,
    width: 1440,
    role: "employer",
    expect: "Accepteren",
  },
  {
    file: "06c-w-na-accepteren-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${acceptedInitiative}`,
    width: 1440,
    role: "employer",
    expect: "Sanne",
  },
  {
    file: "07-w-nu-niet-modal-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${pendingInitiative}&demoDecline=1`,
    width: 1440,
    role: "employer",
    expect: "Nu niet",
  },
];

async function assertApiReady() {
  const res = await fetch(`${apiUrl}/api/public/companies/12345678`);
  if (!res.ok) {
    throw new Error(`API public company not ready (${res.status}). Run run-r5-capture.sh first.`);
  }
  const body = await res.text();
  if (!body.includes("Korenaar") && !body.includes("Bakkerij")) {
    throw new Error("API seed missing Bakkerij De Korenaar (KVK 12345678).");
  }
}

function pageTextFails(text) {
  const lower = text.toLowerCase();
  return forbidden.some((f) => lower.includes(f));
}

async function acceptCookies(context) {
  await context.addInitScript(() => {
    try {
      localStorage.setItem("Jobsy.CookieConsent", "analytics");
      document.cookie = "Jobsy.CookieConsent=" + encodeURIComponent("analytics") + "; Path=/; SameSite=Lax; Max-Age=31536000";
      document.documentElement.classList.add("cookie-consent-known");
    } catch {
      /* ignore */
    }
  });
}

async function login(page, email, returnPath = "/home") {
  const returnUrl = encodeURIComponent(returnPath);
  await page.goto(`${baseUrl}/login?returnUrl=${returnUrl}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
  const emailInput = page.locator('input[name="email"]');
  if (await emailInput.count()) {
    await emailInput.fill(email);
    await page.fill('input[name="password"]', password);
    await Promise.all([
      page.waitForURL((u) => !u.pathname.includes("/login"), { timeout: 90_000 }),
      page.click("button.login-submit"),
    ]);
  }
}

async function waitReady(page, path) {
  await page.waitForLoadState("networkidle", { timeout: 90_000 }).catch(() => {});
  if (path.includes("bedrijvenkaart")) {
    await page.waitForSelector(".bk-page, .bk-shell", { timeout: 90_000 });
  } else if (path.includes("12345678")) {
    await page.waitForSelector(".pp-company, .pub-theme", { timeout: 90_000 });
  }
  if (path.includes("demoKnock")) {
    await page.waitForSelector(".bk-side-panel[role='dialog']", { timeout: 90_000 });
    await page.waitForSelector(".bk-side-panel__textarea", { timeout: 90_000 });
  }
  if (path.includes("demoSent")) {
    await page.waitForSelector(".bk-modal[role='dialog'], .bk-side-panel[role='dialog']", { timeout: 90_000 });
  }
  if (path.includes("demoWeekLimit")) {
    await page.waitForSelector(".bk-modal[role='dialog']", { timeout: 90_000 });
  }
  if (path.includes("werkgever/sollicitaties/initiatieven")) {
    await page.waitForSelector(".bk-wg-apps__split", { timeout: 90_000 });
  }
  if (path.includes("/candidate/applications/initiatieven")) {
    await page.waitForSelector(".kb-apps__initiative-card, .kb-apps__layout--initiative", { timeout: 90_000 });
  } else if (path.includes("/candidate/applications")) {
    await page.waitForSelector(".kb-apps", { timeout: 90_000 });
  }
  await page.waitForTimeout(path.includes("demo") ? 1500 : 900);
}

await assertApiReady();
await mkdir(outDir, { recursive: true });

const manifest = [];
const browser = await chromium.launch();
const roles = ["anonymous", "candidate", "employer"];

for (const role of roles) {
  const roleShots = shots.filter((s) => s.role === role);
  if (roleShots.length === 0) continue;

  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await acceptCookies(context);
  const page = await context.newPage();

  if (role !== "anonymous") {
    await login(page, role === "employer" ? employerEmail : candidateEmail, roleShots[0].path);
  }

  for (const shot of roleShots) {
    await page.setViewportSize({ width: shot.width, height: 900 });
    await page.goto(`${baseUrl}${shot.path}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
    await waitReady(page, shot.path);
    const text = await page.locator("body").innerText();
    const fail404 = pageTextFails(text);
    const failExpect = shot.expect && !text.toLowerCase().includes(shot.expect.toLowerCase());
    if (fail404 || failExpect) {
      manifest.push({
        file: shot.file,
        path: shot.path,
        ok: false,
        fail404,
        failExpect: failExpect ? shot.expect : null,
        snippet: text.slice(0, 200).replace(/\s+/g, " "),
      });
      throw new Error(
        `Capture gate failed for ${shot.file}: 404=${fail404} expect=${shot.expect} snippet=${text.slice(0, 120)}`,
      );
    }
    const outPath = join(outDir, shot.file);
    await page.screenshot({ path: outPath, fullPage: true });
    manifest.push({ file: shot.file, path: shot.path, ok: true });
    console.log("saved", shot.file);
  }

  await context.close();
  await new Promise((r) => setTimeout(r, 1200));
}

await browser.close();
await writeFile(join(outDir, "capture-manifest.json"), JSON.stringify(manifest, null, 2));
console.log("Artifacts:", outDir);
