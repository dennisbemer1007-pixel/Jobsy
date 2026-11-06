import { chromium } from 'playwright';
import path from 'node:path';
import fs from 'node:fs';

const base = process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5201';
const outDirs = [
  '/cursor/stores/self/homepage-ac-v2',
  '/opt/cursor/artifacts/screenshots',
];

for (const dir of outDirs) {
  fs.mkdirSync(dir, { recursive: true });
}

const url = `${base}/?lang=nl`;

async function dismissAndWait(page) {
  await page.goto(url, { waitUntil: 'networkidle', timeout: 120_000 });
  await page.waitForSelector('.pub-landing[data-variant="on"]', { timeout: 90_000 });
  await page.waitForSelector('[data-testid="landing-hero-passport"]', { timeout: 90_000 });
  await page.waitForTimeout(800);
}

async function shot(page, name, opts = {}) {
  for (const dir of outDirs) {
    const file = path.join(dir, name);
    await page.screenshot({ path: file, ...opts });
    console.log('wrote', file);
  }
}

const browser = await chromium.launch();

// Desktop 1440x900
{
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  await context.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
      document.documentElement.classList.add('cookie-consent-known');
    } catch (e) {}
  });
  const page = await context.newPage();
  await dismissAndWait(page);
  await shot(page, 'homepage-ac-v2-desktop-1440x900-first.png');
  await shot(page, 'homepage-ac-v2-desktop-1440x900-full.png', { fullPage: true });
  await context.close();
}

// Mobile 390x844
{
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true,
    hasTouch: true,
  });
  await context.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
      document.documentElement.classList.add('cookie-consent-known');
    } catch (e) {}
  });
  const page = await context.newPage();
  await dismissAndWait(page);
  await shot(page, 'homepage-ac-v2-mobile-390x844-first.png');
  const passport = page.locator('[data-testid="landing-hero-passport"]');
  await passport.scrollIntoViewIfNeeded();
  await page.waitForTimeout(400);
  await shot(page, 'homepage-ac-v2-mobile-390x844-passport.png');
  await page.goto(url, { waitUntil: 'networkidle', timeout: 120_000 });
  await page.waitForSelector('.pub-landing', { timeout: 90_000 });
  await page.waitForTimeout(500);
  await shot(page, 'homepage-ac-v2-mobile-390x844-full.png', { fullPage: true });
  await context.close();
}

await browser.close();
