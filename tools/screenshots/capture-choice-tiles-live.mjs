import { chromium } from 'playwright';
import path from 'path';

const base = process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5201';
const outDir = process.env.OUT_DIR || '/opt/cursor/artifacts/screenshots';
const password = process.env.JOBSY_E2E_PASSWORD || 'Jobsy123!';
const email = process.env.JOBSY_E2E_EMAIL || 'kandidaat@jobsy.local';

async function login(page) {
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  if (await page.locator('input[name="email"]').count()) {
    await page.fill('input[name="email"]', email);
    await page.fill('input[name="password"]', password);
    await Promise.all([
      page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 60_000 }),
      page.click('button.login-submit'),
    ]);
  }
}

async function waitJourneyReady(page) {
  await page.waitForFunction(
    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready',
    null,
    { timeout: 90_000 },
  );
}

async function resetScroll(page) {
  await page.evaluate(() => {
    window.scrollTo(0, 0);
    const stage = document.querySelector('.journey-stage');
    if (stage) stage.scrollTop = 0;
    const main = document.querySelector('.app-main');
    if (main) main.scrollTop = 0;
  });
  await page.waitForTimeout(350);
}

async function shot(page, name) {
  await resetScroll(page);
  const file = path.join(outDir, name);
  await page.screenshot({ path: file, fullPage: false });
  console.log('wrote', file);
}

const browser = await chromium.launch();
const shots = [
  {
    name: 'choice-tiles-discovery-stap6-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=6`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
      for (const label of ['Nachtdiensten', 'Lawaai']) {
        await page.locator('.lobsy-choice-tile', { hasText: label }).first().click();
      }
    },
  },
  {
    name: 'choice-tiles-discovery-stap6-360x740.png',
    width: 360,
    height: 740,
    mobile: true,
    async prepare(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=6`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
    },
  },
  {
    name: 'choice-tiles-discovery-stap6-1440x900.png',
    width: 1440,
    height: 900,
    mobile: false,
    async prepare(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=6`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
    },
  },
  {
    name: 'choice-tiles-discovery-stap5-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=5`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
    },
  },
  {
    name: 'choice-tiles-onboarding-step2-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare(page) {
      await page.goto(`${base}/candidate/start?stap=2`, { waitUntil: 'domcontentloaded' });
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
    },
  },
  {
    name: 'choice-tiles-discovery-stap3-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=3`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
    },
  },
];

for (const s of shots) {
  const context = await browser.newContext({
    viewport: { width: s.width, height: s.height },
    isMobile: s.mobile,
    hasTouch: s.mobile,
  });
  await context.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (e) {}
  });
  const page = await context.newPage();
  page.setDefaultTimeout(90_000);
  await login(page);
  await s.prepare(page);
  await shot(page, s.name);
  await context.close();
}

await browser.close();
