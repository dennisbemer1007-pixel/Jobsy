import { chromium, devices } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = '/opt/cursor/artifacts/diversiteit-r3';
fs.mkdirSync(outDir, { recursive: true });

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const j = await (await fetch(`${baseUrl.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`)).json();
    if (j.code) return j.code;
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('no OTP');
}

async function newSession(viewport) {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ ...viewport, locale: 'nl-NL' });
  await ctx.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (e) {}
  });
  const page = await ctx.newPage();
  page.setDefaultTimeout(120000);
  return { browser, ctx, page };
}

async function registerToWizard(page) {
  const email = `r3.${Date.now()}@jobsy.local`;
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

async function fillStep1(page) {
  await page.locator('.ob-fields input').first().fill('Sanne');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('#onboarding-home-address').fill('2671GB');
  await page.waitForTimeout(800);
  const sug = page.locator('.kb-address__suggest button, .address-suggestions button').first();
  if (await sug.count()) await sug.click();
  const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birth.count()) {
    await birth.fill('15-06-2000');
  } else {
    const date = page.locator('[data-testid=onboarding-birth-date] input[type=date]').first();
    if (await date.count()) await date.fill('2000-06-15');
  }
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
  await page.waitForTimeout(1000);
  if (await page.locator('[data-testid=early-reward-nearby]').count()) {
    await page.getByTestId('onboarding-next').click();
    await page.waitForTimeout(800);
  }
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

const slugs = ['kennismaken', 'beschikbaarheid', 'vervoer', 'werkervaring', 'opleiding', 'droombaan'];

// Desktop 1440: steps 1–6 in one flow
{
  const { browser, page } = await newSession({ viewport: { width: 1440, height: 900 } });
  await registerToWizard(page);
  for (let step = 1; step <= 6; step++) {
    await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`);
    await page.screenshot({
      path: path.join(outDir, `onboarding-step-${step}-${slugs[step - 1]}-1440.png`),
    });
    console.log('saved step', step, '1440');
    if (step === 1) await fillStep1(page);
    else if (step === 2) await fillStep2(page);
    else if (step === 3) await fillStep3(page);
    else if (step === 4) await fillStep4(page);
    else if (step === 5) await fillStep5(page);
  }
  await browser.close();
}

async function mobileReach(page, targetStep) {
  await registerToWizard(page);
  if (targetStep === 1) return;
  await fillStep1(page);
  if (targetStep === 2) return;
  await fillStep2(page);
  if (targetStep === 3) return;
  await fillStep3(page);
  if (targetStep === 4) return;
  await fillStep4(page);
  if (targetStep === 5) return;
  await fillStep5(page);
}

for (const [step, slug] of [
  [3, 'vervoer'],
  [4, 'werkervaring'],
  [6, 'droombaan'],
]) {
  const { browser, page } = await newSession({ ...devices['Pixel 5'], viewport: { width: 390, height: 844 } });
  await mobileReach(page, step);
  if (step === 4) {
    const job = page.locator('[data-testid=onboarding-work-job]').first();
    await job.locator('input').first().fill('Magazijnmedewerker');
    await job.locator('.ob-job-dates__current input[type=checkbox]').check();
  }
  await page.screenshot({ path: path.join(outDir, `onboarding-${slug}-390.png`) });
  console.log('saved', slug, '390');
  await browser.close();
}
