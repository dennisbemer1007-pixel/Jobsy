/**
 * Opleidingen r12 — mobile learn-grow viewport evidence.
 */
import { spawnSync } from 'node:child_process';
import { copyFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/opt/cursor/artifacts/opleidingen-r12';
const testAsset = '/workspace/Jobsy.Tests/TestAssets/leren-groeien-mobiel-390.png';

const consentInitScript = `
  try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
  document.documentElement.classList.add('cookie-consent-known');
`;

async function loginCandidate(page) {
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[name=email]').fill('kandidaat@jobsy.local');
  await page.locator('input[name=password]').fill('Jobsy123!');
  await page.locator('button.login-submit, button[type=submit]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 90_000 });
}

await mkdir(outDir, { recursive: true });
await mkdir(path.dirname(testAsset), { recursive: true });

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ deviceScaleFactor: 1 });
await context.addInitScript(consentInitScript);
const page = await context.newPage();

await loginCandidate(page);
await page.setViewportSize({ width: 390, height: 844 });
await page.goto(`${base}/carriere/leren?r12=1`, { waitUntil: 'networkidle', timeout: 120_000 });
await page.waitForTimeout(1200);

const hero = page.locator('.opl-page .cand-hero--learn-grow').first();
const mobileFile = path.join(outDir, 'leren-groeien-mobiel-390.png');
await page.screenshot({ path: mobileFile, fullPage: false });
const heroBandFile = path.join(outDir, 'leren-groeien-hero-band-390.png');
await hero.screenshot({ path: heroBandFile });
await copyFile(heroBandFile, testAsset);

await browser.close();

const check = spawnSync('python3', ['/workspace/tools/scripts/pixelcheck-hero-r10.py', heroBandFile], {
  encoding: 'utf8',
});
console.log(check.stdout);
if (check.status !== 0) process.exit(check.status ?? 1);
console.log('done', outDir);
