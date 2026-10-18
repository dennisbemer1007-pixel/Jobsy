#!/usr/bin/env node
/**
 * Bedrijvenkaart R4 captures: logged-in demo users, stable routes, cookie consent, CSS checks.
 * Usage: node tools/bedrijvenkaart/capture-screenshots.mjs [baseUrl]
 */
import { mkdir } from "node:fs/promises";
import { join } from "node:path";
import { chromium } from "../screenshots/node_modules/playwright/index.mjs";

const baseUrl = (process.argv[2] ?? "http://localhost:5201").replace(/\/$/, "");
const outDir = "/opt/cursor/artifacts/bedrijvenkaart-build-r4/live";
const password = "Jobsy123!";
const candidateEmail = "sanne@jobsy.local";
const employerEmail = "enterprise@jobsy.local";

const pendingInitiative = "88888888-8888-8888-8888-888888888803";
const acceptedInitiative = "88888888-8888-8888-8888-888888888801";

const shots = [
  { file: "00-banenkaart-link-desktop.png", path: "/banenkaart", width: 1440, role: "candidate" },
  { file: "00-banenkaart-link-mobiel.png", path: "/banenkaart", width: 390, role: "candidate" },
  { file: "01-bedrijvenkaart-desktop.png", path: "/bedrijvenkaart", width: 1440, role: "candidate" },
  { file: "01-bedrijvenkaart-mobiel.png", path: "/bedrijvenkaart", width: 390, role: "candidate" },
  { file: "02-bedrijfspagina-desktop.png", path: "/12345678", width: 1440, role: "candidate" },
  { file: "02-bedrijfspagina-mobiel.png", path: "/12345678", width: 390, role: "candidate" },
  { file: "02b-bedrijfspagina-zonder-vacatures-desktop.png", path: "/12345678/3", width: 1440, role: "anonymous" },
  { file: "02b-bedrijfspagina-zonder-vacatures-mobiel.png", path: "/12345678/3", width: 390, role: "anonymous" },
  { file: "03-bericht-versturen-popup-desktop.png", path: "/12345678?demoKnock=1", width: 1440, role: "candidate" },
  { file: "03-bericht-versturen-popup-mobiel.png", path: "/12345678?demoKnock=1", width: 390, role: "candidate" },
  { file: "03b-verstuurd-modal-desktop.png", path: "/12345678?demoSent=1", width: 1440, role: "candidate" },
  { file: "03b-verstuurd-modal-mobiel.png", path: "/12345678?demoSent=1", width: 390, role: "candidate" },
  { file: "03c-weeklimiet-modal-desktop.png", path: "/12345678?demoWeekLimit=1", width: 1440, role: "candidate" },
  { file: "03c-weeklimiet-modal-mobiel.png", path: "/12345678?demoWeekLimit=1", width: 390, role: "candidate" },
  { file: "04-sollicitaties-initiatieven-desktop.png", path: "/candidate/applications/initiatieven", width: 1440, role: "candidate" },
  { file: "04-sollicitaties-initiatieven-mobiel.png", path: "/candidate/applications/initiatieven", width: 390, role: "candidate" },
  { file: "04b-automatisch-nu-niet-desktop.png", path: "/candidate/applications/initiatieven/automatisch-nu-niet", width: 1440, role: "candidate" },
  { file: "04b-automatisch-nu-niet-mobiel.png", path: "/candidate/applications/initiatieven/automatisch-nu-niet", width: 390, role: "candidate" },
  { file: "05-w-instellingen-desktop.png", path: "/werkgever/open-voor-initiatief", width: 1440, role: "employer" },
  { file: "05b-w-team-toevoegen-modal-desktop.png", path: "/werkgever/open-voor-initiatief?demoTeamAdd=1", width: 1440, role: "employer" },
  { file: "05c-w-email-ontbreekt-desktop.png", path: "/werkgever/open-voor-initiatief?demoEmailMissing=1", width: 1440, role: "employer" },
  {
    file: "06-w-sollicitaties-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${pendingInitiative}`,
    width: 1440,
    role: "employer",
  },
  {
    file: "06b-w-accepteren-modal-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${pendingInitiative}&demoAccept=1`,
    width: 1440,
    role: "employer",
  },
  {
    file: "06c-w-na-accepteren-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${acceptedInitiative}`,
    width: 1440,
    role: "employer",
  },
  {
    file: "07-w-nu-niet-modal-desktop.png",
    path: `/werkgever/sollicitaties/initiatieven?initiative=${pendingInitiative}&demoDecline=1`,
    width: 1440,
    role: "employer",
  },
];

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
  if (returnPath && returnPath !== "/home") {
    await page.goto(`${baseUrl}${returnPath}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
  }
}

async function assertCss(page) {
  for (const href of ["css/app.min.css", "css/features/bedrijvenkaart.css"]) {
    const res = await page.request.get(`${baseUrl}/${href}`);
    if (res.status() !== 200) {
      console.warn("CSS not 200:", href, res.status());
    }
  }
}

async function waitReady(page, path) {
  await page.waitForLoadState("domcontentloaded", { timeout: 60_000 }).catch(() => {});
  if (path.includes("bedrijvenkaart") || path.includes("12345678")) {
    await page.waitForSelector(".bk-page, .pp-company, .pub-theme", { timeout: 60_000 }).catch(() => {});
  }
  if (path.includes("werkgever/sollicitaties")) {
    await page.waitForSelector(".bk-wg-apps__split, .wg-app, .wg-card", { timeout: 60_000 }).catch(() => {});
  }
  if (path.includes("/initiatieven")) {
    await page.waitForSelector(".kb-apps__layout--initiative, .bk-wg-apps__split", { timeout: 60_000 }).catch(() => {});
  }
  await page.waitForTimeout(path.includes("demo") ? 1200 : 800);
}

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();
const roles = ["anonymous", "candidate", "employer"];
for (const role of roles) {
  const roleShots = shots.filter((s) => s.role === role);
  if (roleShots.length === 0) {
    continue;
  }

  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await acceptCookies(context);
  const page = await context.newPage();
  if (role !== "anonymous") {
    await login(page, role === "employer" ? employerEmail : candidateEmail, roleShots[0].path);
  }

  for (const shot of roleShots) {
    try {
      if (shot.width !== 1440) {
        await page.setViewportSize({ width: shot.width, height: 900 });
      } else {
        await page.setViewportSize({ width: 1440, height: 900 });
      }
      await assertCss(page);
      await page.goto(`${baseUrl}${shot.path}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
      await waitReady(page, shot.path);
      await page.screenshot({ path: join(outDir, shot.file), fullPage: true });
      console.log("saved", shot.file);
    } catch (e) {
      console.warn("skip", shot.file, e.message);
    }
  }

  await context.close();
  await new Promise((r) => setTimeout(r, 1500));
}
await browser.close();

console.log("Artifacts:", outDir);
