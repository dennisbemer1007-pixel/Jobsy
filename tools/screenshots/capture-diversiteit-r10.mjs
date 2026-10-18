import { chromium, devices } from 'playwright';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = '/opt/cursor/artifacts/diversiteit-r10';
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
  return { browser, page };
}

async function waitForHeroIllustration(page) {
  const img = page.locator('[data-testid=onboarding-illustration-img]');
  await img.waitFor({ state: 'visible', timeout: 30000 });
  await page.waitForFunction(
    () => {
      const el = document.querySelector('[data-testid=onboarding-illustration-img]');
      return el instanceof HTMLImageElement && el.complete && el.naturalWidth > 0;
    },
    { timeout: 30000 },
  );
  await page.waitForTimeout(300);
}

async function registerToWizard(page) {
  const email = `r10.${Date.now()}@jobsy.local`;
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
  await waitForHeroIllustration(page);
}

async function resolveHomeAddress(page, postcode = '2671GB') {
  const address = page.locator('#onboarding-home-address, .ob-fields input[inputmode=text], .ob-fields input[autocomplete=postal-code]').first();
  await address.click();
  await address.fill('');
  await address.pressSequentially(postcode, { delay: 40 });
  await page.waitForTimeout(1200);
  const suggestion = page.locator('button.address-suggestions__item, .address-suggestions__item, .kb-address__suggest button').first();
  for (let attempt = 0; attempt < 45; attempt++) {
    if (await suggestion.count()) {
      await suggestion.click({ timeout: 15000 });
      break;
    }
    if (await page.locator('[data-testid=onboarding-nearby-panel-desktop], [data-testid=onboarding-nearby-panel-mobile]').count()) break;
    await page.waitForTimeout(1000);
  }
  await page.waitForTimeout(1200);
}

async function waitForNearbyCount(page) {
  try {
    await page.locator('.ob-nearby-panel--desktop .ob-nearby-panel__count').first().waitFor({ state: 'visible', timeout: 90000 });
  } catch {
    /* ok */
  }
  await page.waitForTimeout(400);
}

async function fillStep1(page) {
  await page.locator('.ob-fields input').first().fill('Sanne');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await resolveHomeAddress(page);
  const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birth.count()) await birth.fill('15-06-2000');
  await waitForNearbyCount(page);
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
  if (await page.locator('[data-testid=early-reward-nearby]').count()) {
    await page.getByTestId('onboarding-next').click();
  }
  await page.waitForSelector('[data-testid=onboarding-step-4-illustrated]', { timeout: 120000 });
}

async function fillStep4(page) {
  const job = page.locator('[data-testid=onboarding-work-job]').first();
  if (await job.count()) {
    await job.locator('input').first().fill('Kassamedewerker');
    const current = job.locator('.ob-job-dates__current input[type=checkbox]');
    if (await current.count()) await current.check();
  }
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

const slugs = { 1: 'kennismaken', 3: 'vervoer', 4: 'werkervaring', 6: 'droombaan' };

for (const step of [1, 3, 4, 6]) {
  const { browser, page } = await newSession({ viewport: { width: 1440, height: 900 } });
  await registerToWizard(page);
  await advanceToStep(page, step);
  await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`);
  await waitForHeroIllustration(page);
  if (step === 3) await waitForNearbyCount(page);
  if (step === 6) {
    await page.locator('[data-testid=onboarding-dream-suggest-fit]').waitFor({ timeout: 60000 });
    await page.waitForTimeout(500);
  }
  await page.screenshot({ path: path.join(outDir, `onboarding-step-${step}-${slugs[step]}-1440.png`) });
  console.log('saved step', step, '1440');
  await browser.close();
}

for (const [step, slug, fileSuffix] of [
  [1, 'kennismaken', 'onboarding-step-1-kennismaken-390.png'],
  [3, 'vervoer', 'onboarding-vervoer-390.png'],
  [6, 'droombaan', 'onboarding-droombaan-390.png'],
]) {
  const { browser, page } = await newSession({ ...devices['Pixel 5'], viewport: { width: 390, height: 844 } });
  await registerToWizard(page);
  await advanceToStep(page, step);
  await waitForHeroIllustration(page);
  if (step === 3) {
    await page.getByTestId('onboarding-transport-heading').waitFor({ state: 'visible', timeout: 30000 });
  }
  if (step === 6) {
    await page.locator('[data-testid=onboarding-dream-suggest-fit]').waitFor({ timeout: 60000 });
  }
  await page.screenshot({ path: path.join(outDir, fileSuffix) });
  console.log('saved', slug, '390');
  await browser.close();
}
