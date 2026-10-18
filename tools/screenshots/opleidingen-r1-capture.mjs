/**
 * Workshops & opleidingen evidence (1440 + 390). Requires API :5200 + Web :5201 Development.
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/opt/cursor/artifacts/opleidingen-r1';

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
  await page.setViewportSize({ width, height: width === 390 ? 844 : 900 });
  await page.waitForTimeout(600);
  const file = path.join(outDir, `${name}.png`);
  await page.screenshot({ path: file, fullPage });
  console.log('saved', file);
}

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext();
await context.addInitScript(consentInitScript);
const page = await context.newPage();

await loginCandidate(page, 'kandidaat@jobsy.local', 'Jobsy123!');

await page.goto(`${base}/carriere/leren`, { waitUntil: 'networkidle', timeout: 120_000 });
await shot(page, 'leren-groeien-desktop-1440', 1440);
await shot(page, 'leren-groeien-mobiel-390', 390);

await page.goto(`${base}/candidate/paspoort?tab=tests`, { waitUntil: 'networkidle', timeout: 120_000 });
const growthBtn = page.getByRole('button', { name: /Hoe jij werkt|sterke punten|groeien/i }).first();
if (await growthBtn.count()) {
  await growthBtn.click();
  await page.waitForTimeout(800);
}
await shot(page, 'paspoort-tests-groeien-desktop-1440', 1440);
await shot(page, 'paspoort-tests-groeien-mobiel-390', 390);

await page.goto(`${base}/carriere?stap=2`, { waitUntil: 'networkidle', timeout: 120_000 });
await shot(page, 'carriere-stap-leren-desktop-1440', 1440);
await shot(page, 'carriere-stap-leren-mobiel-390', 390);

await browser.close();

// Admin (separate session)
const admin = await chromium.launch({ headless: true });
const adminCtx = await admin.newContext();
await adminCtx.addInitScript(consentInitScript);
const adminPage = await adminCtx.newPage();
await loginCandidate(adminPage, 'admin@jobsy.local', 'Jobsy123!');
await adminPage.goto(`${base}/admin/content/opleidingen`, { waitUntil: 'networkidle', timeout: 120_000 });
await shot(adminPage, 'admin-opleidingen-partners-1440', 1440);
await admin.close();

console.log('done', outDir);
