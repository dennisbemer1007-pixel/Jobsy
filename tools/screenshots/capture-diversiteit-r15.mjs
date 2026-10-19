import { chromium, devices } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.JOBSY_ILLUSTRATIONS_OUT ?? '/opt/cursor/artifacts/diversiteit-r15';
fs.mkdirSync(outDir, { recursive: true });

async function fetchCode(email) {
  const apiBase = baseUrl.replace(':5201', ':5200');
  for (let i = 0; i < 40; i++) {
    const r = await fetch(`${apiBase}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`);
    const j = await r.json();
    if (j.code) return j.code;
    await new Promise((res) => setTimeout(res, 500));
  }
  throw new Error('No OTP for ' + email);
}

async function dismissCookies(page) {
  const btn = page.locator('.cookie-consent button[data-consent="analytics"], .cookie-consent .btn-compact').first();
  if (await btn.count()) {
    await btn.click({ force: true }).catch(() => {});
    await page.waitForTimeout(300);
  }
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
  await dismissCookies(page);
}

async function beginWizard(page) {
  await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('.ob-wizard__welcome, [data-testid^=onboarding-step-], .ob-wizard__loading', {
    timeout: 120000,
  });
  await page.waitForFunction(
    () =>
      document.querySelector('.ob-wizard__welcome') ||
      document.querySelector('[data-testid^="onboarding-step-"]'),
    { timeout: 120000 },
  );
  if (await page.locator('.ob-wizard__welcome').count()) {
    await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
    await page.waitForTimeout(800);
  }
  await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]', { timeout: 90000 });
  await dismissCookies(page);
}

async function fillStep1(page) {
  await page.locator('.ob-fields input').first().fill('Illus');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('#onboarding-home-address').fill('2671GB');
  await page.waitForTimeout(500);
  const s = page.locator('.kb-address__suggest button, .address-suggestions button').first();
  if (await s.count()) await s.click();
  const birthText = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birthText.count()) await birthText.fill('15-06-2000');
  else {
    const birthDate = page.locator('[data-testid=onboarding-birth-date] input[type=date]').first();
    if (await birthDate.count()) await birthDate.fill('2000-06-15');
  }
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
  await page.waitForSelector('[data-testid=onboarding-step-2-illustrated]', { timeout: 90000 });
}

async function fillStep2(page) {
  await page.locator('.ob-wizard__presets .lobsy-choice-tile').first().click();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
}

async function fillStep3(page) {
  await page.locator('#onboarding-field-transport .lobsy-choice-tile').first().click();
  await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
  if (await page.locator('[data-testid=early-reward-nearby]').count()) {
    await page.getByTestId('onboarding-next').click({ force: true });
    await page.waitForTimeout(800);
  }
}

async function fillStep4(page) {
  const job = page.locator('[data-testid=onboarding-work-job]').first();
  await job.locator('input').first().fill('Verkoopmedewerker');
  await job.locator('input').nth(1).fill('Bakkerij Jansen');
  await job.locator('.ob-job-dates__current input[type=checkbox]').check();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
}

async function fillStep5(page) {
  await page.locator('.ob-wizard__education-grid .lobsy-choice-tile').filter({ hasText: 'HBO' }).first().click();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
}

const mobileSteps = [
  [1, 'kennismaken', null],
  [2, 'beschikbaarheid', fillStep1],
  [3, 'vervoer', fillStep2],
  [4, 'werkervaring', fillStep3],
  [5, 'opleiding', fillStep4],
  [6, 'droombaan', fillStep5],
];

async function captureHeroCloseup(page, step, slug) {
  const hero = page.locator('[data-testid=onboarding-mobile-hero]');
  await hero.waitFor({ timeout: 60000 });
  const file = path.join(outDir, `${String(step).padStart(2, '0')}-${slug}-hero-closeup-2x.png`);
  await hero.screenshot({ path: file, scale: 'device' });
  console.log('saved', file);
}

async function captureMobile390(browser) {
  const ctx = await browser.newContext({
    ...devices['Pixel 5'],
    viewport: { width: 390, height: 844 },
    locale: 'nl-NL',
  });
  const page = await ctx.newPage();
  await register(page, `illus.r15.m.${Date.now()}@jobsy.local`);
  await beginWizard(page);
  for (const [step, slug, prep] of mobileSteps) {
    if (prep) await prep(page);
    await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`, { timeout: 90000 });
    const file = path.join(outDir, `${String(step).padStart(2, '0')}-${slug}-mobiel-390.png`);
    await page.screenshot({ path: file, fullPage: false });
    console.log('saved', file);
    await captureHeroCloseup(page, step, slug);
  }
  await ctx.close();
}

async function captureDesktopStep(browser, step, slug, prepChain) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'nl-NL' });
  const page = await ctx.newPage();
  await register(page, `illus.r15.d${step}.${Date.now()}@jobsy.local`);
  await beginWizard(page);
  for (const prep of prepChain) {
    if (prep) await prep(page);
  }
  await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`, { timeout: 90000 });
  const file = path.join(outDir, `${String(step).padStart(2, '0')}-${slug}-desktop-1440.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log('saved', file);
  await ctx.close();
}

const browser = await chromium.launch({ headless: true });
await captureMobile390(browser);
await captureDesktopStep(browser, 1, 'kennismaken', []);
await captureDesktopStep(browser, 6, 'droombaan', [fillStep1, fillStep2, fillStep3, fillStep4, fillStep5]);
await browser.close();
