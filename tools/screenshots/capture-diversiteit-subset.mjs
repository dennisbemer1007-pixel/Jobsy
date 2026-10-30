import { chromium, devices } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = '/opt/cursor/artifacts/diversiteit';
fs.mkdirSync(outDir, { recursive: true });

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const r = await fetch(`${baseUrl.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`);
    const j = await r.json();
    if (j.code) return j.code;
    await new Promise((res) => setTimeout(res, 500));
  }
  throw new Error('No OTP');
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

async function captureSteps(page, suffix, steps) {
  for (const { step, slug, prep } of steps) {
    if (prep) await prep(page);
    await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`, { timeout: 90000 });
    const file = path.join(outDir, `onboarding-${slug}-${suffix}.png`);
    await page.screenshot({ path: file, fullPage: false });
    console.log('saved', file);
  }
}

const browser = await chromium.launch({ headless: true });

for (const [suffix, ctxOpts] of [
  ['1440', { viewport: { width: 1440, height: 900 } }],
  ['390', { ...devices['Pixel 5'], viewport: { width: 390, height: 844 } }],
]) {
  const ctx = await browser.newContext({ ...ctxOpts, locale: 'nl-NL' });
  const page = await ctx.newPage();
  page.setDefaultTimeout(120000);
  await register(page, `div.${Date.now()}@jobsy.local`);
  await beginWizard(page);

  await captureSteps(page, suffix, [
    { step: 1, slug: 'kennismaken', prep: null },
    { step: 2, slug: 'beschikbaarheid', prep: fillStep1 },
    { step: 3, slug: 'vervoer', prep: fillStep2 },
    {
      step: 4,
      slug: 'werkervaring',
      prep: async (p) => {
        await fillStep3(p);
        const job = p.locator('[data-testid=onboarding-work-job]').first();
        await job.locator('input').first().fill('Magazijnmedewerker');
        await job.locator('.ob-job-dates__current input[type=checkbox]').check();
      },
    },
  ]);
  await ctx.close();
}

await browser.close();
