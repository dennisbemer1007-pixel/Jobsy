import { chromium, devices } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = '/opt/cursor/artifacts/diversiteit-r12';
fs.mkdirSync(outDir, { recursive: true });

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const j = await (await fetch(`${baseUrl.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`)).json();
    if (j.code) return j.code;
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('no OTP');
}

async function newSession() {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ ...devices['Pixel 5'], viewport: { width: 390, height: 844 }, locale: 'nl-NL' });
  await ctx.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (e) {}
  });
  const page = await ctx.newPage();
  page.setDefaultTimeout(120000);
  return { browser, page };
}

async function registerToWizard(page) {
  const email = `r12.${Date.now()}@jobsy.local`;
  await page.goto(`${baseUrl}/account-maken`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type=email]').fill(email);
  await page.locator('input[name=firstName]').fill('Sanne');
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

async function resolveHomeAddress(page) {
  const address = page.locator('#onboarding-home-address, .ob-fields input[inputmode=text]').first();
  await address.click();
  await address.fill('');
  await address.pressSequentially('2671GB', { delay: 40 });
  await page.waitForTimeout(1200);
  const suggestion = page.locator('button.address-suggestions__item, .address-suggestions__item, .kb-address__suggest button').first();
  for (let attempt = 0; attempt < 45; attempt++) {
    if (await suggestion.count()) {
      await suggestion.click({ timeout: 15000 });
      break;
    }
    await page.waitForTimeout(1000);
  }
  await page.waitForTimeout(800);
}

async function fillStep1(page) {
  await page.locator('.ob-fields input').first().fill('Sanne');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await resolveHomeAddress(page);
  const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birth.count()) await birth.fill('15-06-2000');
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-2-illustrated]');
}
async function fillStep2(page) {
  await page.locator('.ob-wizard__presets .lobsy-choice-tile, .ob-wizard__presets button').first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-3-illustrated]');
}
async function fillStep3(page) {
  await page.locator('#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button').first().click();
  await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
  await page.getByTestId('onboarding-next').click();
  if (await page.locator('[data-testid=early-reward-nearby]').count()) await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-4-illustrated]', { timeout: 120000 });
}
async function fillStep4(page) {
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-5-illustrated]');
}
async function fillStep5(page) {
  await page.locator('.ob-wizard__education-grid .lobsy-choice-tile').filter({ hasText: 'HBO' }).first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-6-illustrated]', { timeout: 120000 });
}
async function advanceToStep(page, target) {
  if (target === 1) return;
  await fillStep1(page);
  if (target === 2) return;
  await fillStep2(page);
  if (target === 3) return;
  await fillStep3(page);
  if (target === 4) return;
  await fillStep4(page);
  if (target === 5) return;
  await fillStep5(page);
}

const slugs = { 1: 'kennismaken', 2: 'beschikbaarheid', 3: 'vervoer', 4: 'werkervaring', 5: 'opleiding', 6: 'droombaan' };

for (const step of [1, 2, 3, 4, 5, 6]) {
  const { browser, page } = await newSession();
  await registerToWizard(page);
  await advanceToStep(page, step);
  await page.getByTestId(`onboarding-step-${step}-illustrated`).waitFor({ state: 'visible' });
  await page.waitForTimeout(800);
  if (step === 6) await page.locator('[data-testid=onboarding-dream-suggest-fit]').waitFor({ timeout: 60000 });
  await page.screenshot({ path: path.join(outDir, `onboarding-step-${step}-${slugs[step]}-390.png`) });
  console.log('saved', step, slugs[step]);
  await browser.close();
}
