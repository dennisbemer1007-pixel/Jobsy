/**
 * Opleidingen r9 — leren hero 390 + 1440 viewport evidence.
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/opt/cursor/artifacts/opleidingen-r9';

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

async function shot(page, name, width) {
  const height = width === 390 ? 844 : 900;
  await page.setViewportSize({ width, height });
  await page.waitForTimeout(1200);
  if (width === 390) {
    await page
      .waitForFunction(
        () => {
          const hero = document.querySelector('.opl-page .cand-hero--learn-grow');
          const media = document.querySelector('.opl-page .cand-hero--learn-grow .cand-hero__media');
          return (
            hero &&
            hero.offsetHeight <= 265 &&
            media &&
            getComputedStyle(media).display === 'none'
          );
        },
        { timeout: 15_000 },
      )
      .catch(() => {});
  }
  const file = path.join(outDir, `${name}.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log('saved', file);
}

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ deviceScaleFactor: 1 });
await context.addInitScript(consentInitScript);
const page = await context.newPage();

await loginCandidate(page);
await page.setViewportSize({ width: 390, height: 844 });
await page.goto(`${base}/carriere/leren`, { waitUntil: 'networkidle', timeout: 120_000 });
await shot(page, 'leren-groeien-mobiel-390', 390);

const hero = page.locator('.opl-page .cand-hero--learn-grow').first();
const search = page.locator('.opl-search--below').first();
const heroBox = await hero.boundingBox();
const searchBox = await search.boundingBox();
console.log('390 hero height', heroBox?.height, 'search y', searchBox?.y);

await shot(page, 'leren-groeien-desktop-viewport-1440', 1440);

await browser.close();
console.log('done', outDir);
