/**
 * Opleidingen r8 evidence (1440 + 390). Paspoort = crop groeipunten-blok only.
 */
import { chromium } from 'playwright';
import { mkdir, rm } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/opt/cursor/artifacts/opleidingen-r8';

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

async function shotGrowthBlock(page, growthBlock, name, width) {
  const height = width === 390 ? 844 : 900;
  await page.setViewportSize({ width, height });
  await growthBlock.scrollIntoViewIfNeeded();
  await page.waitForTimeout(600);
  const box = await growthBlock.boundingBox();
  if (!box) {
    throw new Error(`No bounding box for ${name}`);
  }

  const marginX = width === 390 ? 12 : 48;
  const marginY = 16;
  const clipX = Math.max(0, box.x - marginX);
  const clipW = Math.min(width - clipX, box.width + marginX * 2);
  const clipH = Math.min(height - Math.max(0, box.y - marginY), box.height + marginY * 2 + 40);
  const file = path.join(outDir, `${name}.png`);
  await page.screenshot({
    path: file,
    clip: {
      x: clipX,
      y: Math.max(0, box.y - marginY),
      width: clipW,
      height: clipH,
    },
  });
  console.log('saved', file, 'clip', clipW, 'x', clipH);
}

try {
  await rm('/opt/cursor/artifacts/opleidingen-r7', { recursive: true, force: true });
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
await shot(page, 'leren-groeien-desktop-viewport-1440', 1440, false);
await shot(page, 'leren-groeien-mobiel-390', 390, false);

await page.goto(`${base}/candidate/paspoort?tab=tests`, { waitUntil: 'networkidle', timeout: 120_000 });
await page.getByRole('button', { name: /Bekijk groeikansen|View growth offers/i }).click();
const growthBlock = page.locator('.opl-passport-growth').first();
await growthBlock.waitFor({ state: 'visible', timeout: 90_000 });
await page.waitForTimeout(500);
await shotGrowthBlock(page, growthBlock, 'paspoort-tests-groeien-desktop-1440', 1440);
await shotGrowthBlock(page, growthBlock, 'paspoort-tests-groeien-mobiel-390', 390);

await page.goto(`${base}/carriere?stap=2`, { waitUntil: 'networkidle', timeout: 120_000 });
await page.waitForSelector('[data-testid="career-step-learning"]', { timeout: 60_000 }).catch(() => {});
await shot(page, 'carriere-stap-leren-desktop-1440', 1440, true);
await shot(page, 'carriere-stap-leren-mobiel-390', 390, false);

await browser.close();
console.log('done', outDir);
