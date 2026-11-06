import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = '/opt/cursor/artifacts/diversiteit-r2';
fs.mkdirSync(outDir, { recursive: true });

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const j = await (await fetch(`${baseUrl.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`)).json();
    if (j.code) return j.code;
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('no code');
}

async function register(page) {
  const email = `r2.${Date.now()}@jobsy.local`;
  await page.goto(`${baseUrl}/account-maken`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type=email]').fill(email);
  await page.locator('input[name=firstName]').fill('R2');
  await page.locator('button.au-submit, button.login-submit').first().click();
  await page.waitForURL('**/code**');
  await page.locator('input[name=code]').fill(await fetchCode(email));
  await page.locator('button.login-submit').click();
  await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'networkidle' });
  if (await page.locator('.ob-wizard__welcome').count()) {
    await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
  }
  await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]');
}

async function nextFrom1(page) {
  await page.locator('.ob-fields input').first().fill('R2');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('#onboarding-home-address').fill('2671GB');
  await page.waitForTimeout(600);
  const s = page.locator('.kb-address__suggest button').first();
  if (await s.count()) await s.click();
  const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birth.count()) await birth.fill('15-06-2000');
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-2-illustrated]');
}

async function nextFrom2(page) {
  await page.locator('.ob-wizard__presets .lobsy-choice-tile').first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-3-illustrated]');
}

async function nextFrom3(page) {
  await page.locator('#onboarding-field-transport .lobsy-choice-tile').first().click();
  await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForTimeout(800);
  if (await page.locator('[data-testid=early-reward-nearby]').count()) {
    await page.getByTestId('onboarding-next').click();
  }
  await page.waitForSelector('[data-testid=onboarding-step-4-illustrated]');
}

async function nextFrom4(page) {
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-5-illustrated]');
}

async function nextFrom5(page) {
  const hbo = page.locator('.ob-wizard__education-grid .lobsy-choice-tile').filter({ hasText: 'HBO' }).first();
  if (await hbo.count()) await hbo.click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-6-illustrated]');
}

const slugs = ['kennismaken', 'beschikbaarheid', 'vervoer', 'werkervaring', 'opleiding', 'droombaan'];
const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'nl-NL' });
await ctx.addInitScript(() => {
  try {
    localStorage.setItem('Jobsy.CookieConsent', 'necessary');
  } catch (e) {}
});
const page = await ctx.newPage();
page.setDefaultTimeout(120000);
await register(page);
for (let step = 1; step <= 6; step++) {
  await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`);
  const file = path.join(outDir, `onboarding-step-${step}-${slugs[step - 1]}-1440.png`);
  await page.screenshot({ path: file });
  console.log('saved', file);
  if (step === 1) await nextFrom1(page);
  else if (step === 2) await nextFrom2(page);
  else if (step === 3) await nextFrom3(page);
  else if (step === 4) await nextFrom4(page);
  else if (step === 5) await nextFrom5(page);
}
await browser.close();
