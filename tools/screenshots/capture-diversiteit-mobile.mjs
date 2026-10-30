import { chromium, devices } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = '/opt/cursor/artifacts/diversiteit';

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const r = await fetch(`${baseUrl.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`);
    const j = await r.json();
    if (j.code) return j.code;
    await new Promise((res) => setTimeout(res, 500));
  }
  throw new Error('No OTP');
}

async function dismissCookies(page) {
  const btn = page.locator('button[data-consent="analytics"], button:has-text("Accepteer cookies")').first();
  if (await btn.count()) {
    await btn.click({ force: true }).catch(() => {});
    await page.waitForTimeout(300);
  }
}

async function registerAndReachStep(page, targetStep) {
  const email = `mob.${Date.now()}@jobsy.local`;
  await page.goto(`${baseUrl}/account-maken`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type=email]').fill(email);
  await page.locator('input[name=firstName]').fill('Mob');
  await page.locator('button.login-submit, button.au-submit[type=submit]').first().click();
  await page.waitForURL('**/code**', { timeout: 60000 });
  await page.locator('input[name=code]').fill(await fetchCode(email));
  await page.locator('button.login-submit').click();
  await page.waitForURL((u) => !u.toString().includes('/code'), { timeout: 90000 });
  await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'networkidle', timeout: 120000 });
  await page.waitForSelector('.ob-wizard__welcome, [data-testid^=onboarding-step-]', { timeout: 120000 });
  if (await page.locator('.ob-wizard__welcome').count()) {
    await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
  }
  await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]', { timeout: 120000 });
  await dismissCookies(page);
  if (targetStep === 1) return;
  await page.locator('.ob-fields input').first().fill('Mob');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('#onboarding-home-address').fill('2671GB');
  await page.waitForTimeout(600);
  const s = page.locator('.kb-address__suggest button').first();
  if (await s.count()) await s.click();
  const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birth.count()) await birth.fill('15-06-2000');
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-2-illustrated]', { timeout: 120000 });
  if (targetStep === 2) return;
  await page.locator('.ob-wizard__presets .lobsy-choice-tile').first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-3-illustrated]', { timeout: 120000 });
  if (targetStep === 3) return;
  await page.locator('#onboarding-field-transport .lobsy-choice-tile').first().click();
  await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForTimeout(800);
  if (await page.locator('[data-testid=early-reward-nearby]').count()) {
    await page.getByTestId('onboarding-next').click();
    await page.waitForTimeout(800);
  }
  await page.waitForSelector('[data-testid=onboarding-step-4-illustrated]', { timeout: 120000 });
}

const browser = await chromium.launch({ headless: true });
for (const [step, slug] of [
  [1, 'kennismaken'],
  [3, 'vervoer'],
  [4, 'werkervaring'],
]) {
  const ctx = await browser.newContext({ ...devices['Pixel 5'], viewport: { width: 390, height: 844 }, locale: 'nl-NL' });
  await ctx.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (e) {}
  });
  const page = await ctx.newPage();
  page.setDefaultTimeout(120000);
  await registerAndReachStep(page, step);
  if (step === 4) {
    const job = page.locator('[data-testid=onboarding-work-job]').first();
    await job.locator('input').first().fill('Magazijnmedewerker');
    await job.locator('.ob-job-dates__current input[type=checkbox]').check();
  }
  const file = path.join(outDir, `onboarding-${slug}-390.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log('saved', file);
  await ctx.close();
}
await browser.close();
