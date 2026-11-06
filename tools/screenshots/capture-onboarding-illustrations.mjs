import { chromium, devices } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.JOBSY_ILLUSTRATIONS_OUT ?? '/cursor/stores/self/steps';
fs.mkdirSync(outDir, { recursive: true });

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const r = await fetch(`${baseUrl.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`);
    const j = await r.json();
    if (j.code) return j.code;
    await new Promise((res) => setTimeout(res, 500));
  }
  throw new Error('No OTP for ' + email);
}

async function register(page, email) {
  await page.goto(`${baseUrl}/account-maken`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type=email]').fill(email);
  await page.locator('input[name=firstName]').fill('Illus');
  await page.locator('button.login-submit, button.au-submit[type=submit]').first().click();
  await page.waitForURL('**/code**', { timeout: 60000 });
  const code = await fetchCode(email);
  await page.locator('input[name=code]').fill(code);
  await page.locator('button.login-submit').click();
  await page.waitForURL((u) => !u.toString().includes('/code'), { timeout: 90000 });
}

async function beginWizard(page) {
  if (!page.url().includes('/candidate/start')) {
    await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'domcontentloaded' });
  }
  await page.waitForSelector('.ob-wizard__welcome, [data-testid^=onboarding-step-]', { timeout: 90000 });
  if (await page.locator('.ob-wizard__welcome').count()) {
    await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
  }
  await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]', { timeout: 90000 });
}

async function fillStep1(page) {
  await page.locator('.ob-fields input').first().fill('Illus');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('#onboarding-home-address').fill('2671GB');
  await page.waitForTimeout(500);
  const s = page.locator('.kb-address__suggest button').first();
  if (await s.count()) await s.click();
  const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birth.count()) await birth.fill('15-06-2000');
  await page.getByTestId('onboarding-next').click();
  await page.waitForTimeout(800);
}

async function fillStep2(page) {
  await page.locator('.ob-wizard__presets .lobsy-choice-tile').first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForTimeout(800);
}

async function fillStep3(page) {
  await page.locator('#onboarding-field-transport .lobsy-choice-tile').first().click();
  await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForTimeout(800);
  if (await page.locator('[data-testid=early-reward-nearby]').count()) {
    await page.getByTestId('onboarding-next').click();
    await page.waitForTimeout(800);
  }
}

async function captureFlow(browser, viewport, slugSuffix) {
  const ctx = await browser.newContext({ ...viewport, locale: 'nl-NL' });
  const page = await ctx.newPage();
  const email = `illus.${Date.now()}@jobsy.local`;
  await register(page, email);
  await beginWizard(page);

  const shots = [
    [1, 'kennismaken', null],
    [2, 'beschikbaarheid', fillStep1],
    [3, 'vervoer', fillStep2],
    [4, 'werkervaring-ingevuld', async (p) => {
      await fillStep3(p);
      const job = p.locator('[data-testid=onboarding-work-job]').first();
      await job.locator('input').first().fill('Verkoopmedewerker');
      await job.locator('input').nth(1).fill('Bakkerij Jansen');
      await job.locator('.ob-job-dates__current input[type=checkbox]').check();
      await job.locator('.ob-job-duties textarea').fill('Brood en taart verkopen.');
    }],
    [5, 'opleiding-dropdown', async (p) => {
      await p.getByTestId('onboarding-next').click();
      await p.waitForTimeout(800);
      await p.locator('.ob-wizard__education-grid .lobsy-choice-tile').filter({ hasText: 'HBO' }).first().click();
    }],
  ];

  for (const [step, slug, prep] of shots) {
    if (prep) await prep(page);
    await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`, { timeout: 90000 });
    const file = path.join(outDir, `${String(step).padStart(2, '0')}-${slug}-${slugSuffix}.png`);
    await page.screenshot({ path: file, fullPage: false });
    console.log('saved', file);
  }
  await ctx.close();
}

const browser = await chromium.launch({ headless: true });
await captureFlow(browser, { viewport: { width: 1440, height: 900 } }, 'desktop-1440');
await captureFlow(browser, { ...devices['Pixel 5'], viewport: { width: 390, height: 844 } }, 'mobiel-390');
await captureFlow(browser, { viewport: { width: 360, height: 800 }, isMobile: true, hasTouch: true }, 'mobiel-360');
await browser.close();
