/**
 * Opleidingen r2 evidence (1440 full-page desktop, 390 viewport-height mobile).
 */
import { chromium } from 'playwright';
import { mkdir, rm } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/opt/cursor/artifacts/opleidingen-r2';
const r1Dir = '/opt/cursor/artifacts/opleidingen-r1';

const consentInitScript = `
  try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
  document.documentElement.classList.add('cookie-consent-known');
`;

async function loginCandidate(page, email, password) {
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[name=email]').fill(email);
  await page.locator('input[name=password]').fill(password);
  await page.locator('button.login-submit, button[type=submit]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 90_000 });
}

async function shot(page, name, width, fullPage = true) {
  const height = width === 390 ? 844 : 900;
  await page.setViewportSize({ width, height });
  await page.waitForTimeout(800);
  const file = path.join(outDir, `${name}.png`);
  await page.screenshot({ path: file, fullPage });
  console.log('saved', file, fullPage ? 'full' : 'viewport');
}

try {
  await rm(r1Dir, { recursive: true, force: true });
} catch {
  /* ok */
}

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ deviceScaleFactor: 1 });
await context.addInitScript(consentInitScript);
const page = await context.newPage();

await loginCandidate(page, 'kandidaat@jobsy.local', 'Jobsy123!');

await page.goto(`${base}/carriere/leren`, { waitUntil: 'networkidle', timeout: 120_000 });
await shot(page, 'leren-groeien-desktop-1440', 1440, true);
await shot(page, 'leren-groeien-mobiel-390', 390, false);

await page.goto(`${base}/candidate/paspoort?tab=tests`, { waitUntil: 'networkidle', timeout: 120_000 });
const growthBtn = page.getByRole('button', { name: /Hoe jij werkt|sterke punten|groeien/i }).first();
if (await growthBtn.count()) {
  await growthBtn.click();
  await page.waitForTimeout(800);
}
const growthBlock = page.locator('.opl-passport-growth').first();
if (await growthBlock.count()) {
  await growthBlock.scrollIntoViewIfNeeded();
  await page.waitForTimeout(500);
  await page.setViewportSize({ width: 1440, height: 900 });
  await growthBlock.screenshot({
    path: path.join(outDir, 'paspoort-groeipunten-blok-1440.png'),
  });
  console.log('saved', path.join(outDir, 'paspoort-groeipunten-blok-1440.png'), 'element');
}
await shot(page, 'paspoort-tests-groeien-desktop-1440', 1440, true);
await shot(page, 'paspoort-tests-groeien-mobiel-390', 390, false);

await page.goto(`${base}/carriere?stap=2`, { waitUntil: 'networkidle', timeout: 120_000 });
await page.waitForSelector('[data-testid="career-step-learning"]', { timeout: 60_000 }).catch(() => {});
await shot(page, 'carriere-stap-leren-desktop-1440', 1440, true);
await shot(page, 'carriere-stap-leren-mobiel-390', 390, false);

await browser.close();
console.log('done', outDir);
