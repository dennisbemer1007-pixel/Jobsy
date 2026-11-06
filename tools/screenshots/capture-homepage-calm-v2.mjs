import { chromium } from 'playwright';
import path from 'node:path';
import fs from 'node:fs';

const base = process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5202';
const outDir = '/cursor/stores/self/homepage-calm-v2';
fs.mkdirSync(outDir, { recursive: true });

const url = `${base}/?lang=nl`;

async function dismissAndWait(page) {
  await page.goto(url, { waitUntil: 'networkidle', timeout: 120_000 });
  await page.waitForSelector('.pub-landing--calm', { timeout: 90_000 });
  await page.waitForTimeout(800);
}

async function shot(page, name, opts = {}) {
  const file = path.join(outDir, name);
  await page.screenshot({ path: file, ...opts });
  console.log('wrote', file);
}

async function closeUp(page, selector, name) {
  const loc = page.locator(selector).first();
  await loc.scrollIntoViewIfNeeded();
  await page.waitForTimeout(350);
  const box = await loc.boundingBox();
  if (!box) throw new Error(`no box for ${selector}`);
  await shot(page, name, { clip: box });
}

const cookieInit = () => {
  try {
    localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    document.documentElement.classList.add('cookie-consent-known');
  } catch (e) {}
};

const browser = await chromium.launch();

{
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await context.addInitScript(cookieInit);
  const page = await context.newPage();
  await dismissAndWait(page);
  await shot(page, 'homepage-calm-v2-desktop-1440-first.png');
  await shot(page, 'homepage-calm-v2-desktop-1440-full.png', { fullPage: true });
  await closeUp(page, '[data-testid="landing-hero-passport"]', 'homepage-calm-v2-desktop-1440-passport.png');
  await closeUp(page, '#banenkaart', 'homepage-calm-v2-desktop-1440-banenkaart.png');
  await closeUp(page, '#voor-werkgevers', 'homepage-calm-v2-desktop-1440-employers.png');
  await context.close();
}

{
  const context = await browser.newContext({ viewport: { width: 1024, height: 900 } });
  await context.addInitScript(cookieInit);
  const page = await context.newPage();
  await dismissAndWait(page);
  await shot(page, 'homepage-calm-v2-1024-full.png', { fullPage: true });
  await context.close();
}

{
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true,
    hasTouch: true,
  });
  await context.addInitScript(cookieInit);
  const page = await context.newPage();
  await dismissAndWait(page);
  await shot(page, 'homepage-calm-v2-mobile-390-first.png');
  await shot(page, 'homepage-calm-v2-mobile-390-full.png', { fullPage: true });
  await closeUp(page, '[data-testid="landing-hero-passport"]', 'homepage-calm-v2-mobile-390-passport.png');
  await closeUp(page, '#banenkaart', 'homepage-calm-v2-mobile-390-banenkaart.png');
  await closeUp(page, '#voor-werkgevers', 'homepage-calm-v2-mobile-390-employers.png');
  await context.close();
}

await browser.close();
