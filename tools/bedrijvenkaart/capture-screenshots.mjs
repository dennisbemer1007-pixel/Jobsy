#!/usr/bin/env node
/**
 * Bedrijvenkaart captures: requires API+Web with Seed:Enabled (see run-r*-capture.sh).
 */
import { mkdir, writeFile } from "node:fs/promises";
import { join } from "node:path";
import { chromium } from "../screenshots/node_modules/playwright/index.mjs";

const baseUrl = (process.argv[2] ?? "http://localhost:5201").replace(/\/$/, "");
const apiUrl = (process.argv[3] ?? "http://127.0.0.1:5200").replace(/\/$/, "");
const outDir =
  process.env.BEDRIFJKART_CAPTURE_DIR ?? "/opt/cursor/artifacts/bedrijvenkaart-build-r13/live";
const captureOnly = process.env.CAPTURE_ONLY ?? "";
const captureSet = process.env.CAPTURE_SET ?? "";
const r14Files = new Set([
  "01-bedrijvenkaart-desktop.png",
  "01-bedrijvenkaart-mobiel-top.png",
  "02-bedrijfspagina-desktop.png",
  "02-bedrijfspagina-mobiel-top.png",
]);
const r16Files = new Set([
  "01-bedrijvenkaart-desktop.png",
  "01-bedrijvenkaart-mobiel-top.png",
  "01-bedrijvenkaart-mobiel-scroll.png",
  "02-bedrijfspagina-desktop.png",
  "02-bedrijfspagina-mobiel-top.png",
  "02-bedrijfspagina-mobiel-scroll.png",
]);
const r17Files = r16Files;
const r18Files = r16Files;
const r19Files = r16Files;
const r20Files = r16Files;
const r21Files = r16Files;
const r15Files = new Set([
  "01-bedrijvenkaart-desktop.png",
  "01-bedrijvenkaart-mobiel-top.png",
  "01-bedrijvenkaart-mobiel-scroll.png",
  "02-bedrijfspagina-desktop.png",
  "02-bedrijfspagina-mobiel-top.png",
  "02-bedrijfspagina-mobiel-scroll.png",
]);
const password = "Jobsy123!";
const candidateEmail = "sanne@jobsy.local";
const employerEmail = "enterprise@jobsy.local";

const pendingInitiative = "88888888-8888-8888-8888-888888888803";
const acceptedInitiative = "88888888-8888-8888-8888-888888888801";

const forbidden = ["deze pagina bestaat niet", "404", "not-found-view", "common.change", "initiative.panel.changeteam"];

const allShots = [
  { file: "00-banenkaart-link-desktop.png", path: "/banenkaart", width: 1440, role: "candidate", expect: "Bedrijvenkaart" },
  { file: "00-banenkaart-link-mobiel.png", path: "/banenkaart", width: 390, role: "candidate", expect: "Bedrijvenkaart" },
  { file: "01-bedrijvenkaart-desktop.png", path: "/bedrijvenkaart", width: 1440, role: "candidate", expect: "Bedrijven" },
  { file: "01-bedrijvenkaart-mobiel-top.png", path: "/bedrijvenkaart", width: 390, role: "candidate", expect: "Bedrijven", scrollY: 0 },
  { file: "01-bedrijvenkaart-mobiel-scroll.png", path: "/bedrijvenkaart", width: 390, role: "candidate", expect: "Bedrijven", scrollY: 720 },
  { file: "02-bedrijfspagina-desktop.png", path: "/12345678", width: 1440, role: "candidate", expect: "Korenaar" },
  { file: "02-bedrijfspagina-mobiel-top.png", path: "/12345678", width: 390, role: "candidate", expect: "Korenaar", scrollY: 0 },
  { file: "02-bedrijfspagina-mobiel-scroll.png", path: "/12345678", width: 390, role: "candidate", expect: "Korenaar", scrollY: 880 },
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

const shots = captureOnly
  ? allShots.filter((s) => s.file === captureOnly)
  : captureSet === "r14"
    ? allShots.filter((s) => r14Files.has(s.file))
    : captureSet === "r15"
      ? allShots.filter((s) => r15Files.has(s.file))
      : captureSet === "r16"
        ? allShots.filter((s) => r16Files.has(s.file))
        : captureSet === "r17"
          ? allShots.filter((s) => r17Files.has(s.file))
          : captureSet === "r18"
            ? allShots.filter((s) => r18Files.has(s.file))
            : captureSet === "r19"
              ? allShots.filter((s) => r19Files.has(s.file))
              : captureSet === "r20"
                ? allShots.filter((s) => r20Files.has(s.file))
                : captureSet === "r21"
                  ? allShots.filter((s) => r21Files.has(s.file))
                  : allShots;

if (shots.length === 0) {
  throw new Error(`CAPTURE_ONLY=${captureOnly} matched no shots`);
}

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
    const isMobileViewport = shot.width <= 420;
    const viewportHeight = isMobileViewport ? 844 : 900;
    await page.setViewportSize({ width: shot.width, height: viewportHeight });
    await page.goto(`${baseUrl}${shot.path}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
    await waitReady(page, shot.path);
    if (typeof shot.scrollY === "number") {
      await page.evaluate((y) => window.scrollTo(0, y), shot.scrollY);
      await page.waitForTimeout(350);
    }
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
    if (shot.file.startsWith("01-bedrijvenkaart") || shot.file.startsWith("02-bedrijfspagina")) {
      const gutterProbe = await page.evaluate(() => {
        const root =
          document.querySelector(".bk-page") ?? document.querySelector(".pub-theme.pp-company");
        if (!root) {
          return { ok: false, reason: "missing .bk-page / .pp-company" };
        }
        const styles = getComputedStyle(root);
        const gutter = styles.getPropertyValue("--bk-gutter").trim();
        const padL = parseFloat(styles.paddingLeft) || 0;
        const padR = parseFloat(styles.paddingRight) || 0;
        const frame = document.querySelector(".bk-page__frame");
        const framePad = frame ? parseFloat(getComputedStyle(frame).paddingLeft) || 0 : padL;
        const effective = Math.max(padL, framePad);
        return {
          ok: effective >= 12,
          gutter,
          padL,
          padR,
          framePad,
          cssHref: [...document.querySelectorAll('link[rel="stylesheet"]')]
            .map((l) => l.href)
            .find((h) => h.includes("bedrijvenkaart.css")),
        };
      });
      if (!gutterProbe.ok) {
        throw new Error(
          `Gutter check failed for ${shot.file}: ${JSON.stringify(gutterProbe)} (bedrijvenkaart.css must load with --bk-gutter)`,
        );
      }
      manifest.push({ file: shot.file, path: shot.path, gutter: gutterProbe });
    }

    const outPath = join(outDir, shot.file);
    await page.screenshot({ path: outPath, fullPage: !isMobileViewport });
    manifest.push({ file: shot.file, path: shot.path, ok: true, viewport: `${shot.width}x${viewportHeight}` });
    console.log("saved", shot.file);
  }

  await context.close();
  await new Promise((r) => setTimeout(r, 1200));
}

async function captureHeroZooms(browserInstance) {
  for (const spec of [
    { file: "01-hero-bedrijvenkaart-desktop-2x.png", path: "/bedrijvenkaart", width: 1440, selector: ".bk-page .bk-cand-hero" },
    { file: "01-hero-bedrijvenkaart-mobiel-2x.png", path: "/bedrijvenkaart", width: 390, selector: ".bk-page .bk-cand-hero" },
  ]) {
    const context = await browserInstance.newContext({
      viewport: { width: spec.width, height: spec.width <= 420 ? 844 : 900 },
      deviceScaleFactor: 2,
    });
    await acceptCookies(context);
    const page = await context.newPage();
    await login(page, candidateEmail, spec.path);
    await page.goto(`${baseUrl}${spec.path}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
    await waitReady(page, spec.path);
    const hero = page.locator(spec.selector).first();
    await hero.waitFor({ state: "visible", timeout: 45_000 });
    await hero.screenshot({ path: join(outDir, spec.file) });
    manifest.push({ file: spec.file, path: spec.path, ok: true, zoom: "2x" });
    console.log("saved", spec.file);
    await context.close();
  }
}

async function captureCompanyHeaderZooms(browserInstance) {
  for (const spec of [
    { file: "02-company-header-desktop-2x.png", path: "/12345678", width: 1440, selector: ".pp-company__hero" },
    { file: "02-company-header-mobiel-2x.png", path: "/12345678", width: 390, selector: ".pp-company__hero" },
  ]) {
    const context = await browserInstance.newContext({
      viewport: { width: spec.width, height: spec.width <= 420 ? 844 : 900 },
      deviceScaleFactor: 2,
    });
    await acceptCookies(context);
    const page = await context.newPage();
    await login(page, candidateEmail, spec.path);
    await page.goto(`${baseUrl}${spec.path}`, { waitUntil: "domcontentloaded", timeout: 120_000 });
    await waitReady(page, "Korenaar");
    const hero = page.locator(spec.selector).first();
    await hero.waitFor({ state: "visible", timeout: 45_000 });
    await hero.screenshot({ path: join(outDir, spec.file) });
    manifest.push({ file: spec.file, path: spec.path, ok: true, zoom: "2x" });
    console.log("saved", spec.file);
    await context.close();
  }
}

if (captureSet === "r15" || captureSet === "r16" || captureSet === "r17" || captureSet === "r18" || captureSet === "r19" || captureSet === "r20") {
  await captureHeroZooms(browser);
}

if (captureSet === "r21") {
  await captureCompanyHeaderZooms(browser);
}

await browser.close();
await writeFile(join(outDir, "capture-manifest.json"), JSON.stringify(manifest, null, 2));
console.log("Artifacts:", outDir);
