import { chromium, devices } from 'playwright';

const base = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const out = '/opt/cursor/artifacts/diversiteit-r2';

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const j = await (await fetch(`${base.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`)).json();
    if (j.code) return j.code;
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('no code');
}

async function run(targetStep, slug) {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ ...devices['Pixel 5'], viewport: { width: 390, height: 844 }, locale: 'nl-NL' });
  await ctx.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (e) {}
  });
  const page = await ctx.newPage();
  page.setDefaultTimeout(120000);
  const email = `v390.${Date.now()}@jobsy.local`;
  await page.goto(`${base}/account-maken`);
  await page.locator('input[type=email]').fill(email);
  await page.locator('input[name=firstName]').fill('V');
  await page.locator('button.au-submit, button.login-submit').first().click();
  await page.waitForURL('**/code**');
  await page.locator('input[name=code]').fill(await fetchCode(email));
  await page.locator('button.login-submit').click();
  await page.goto(`${base}/candidate/start`, { waitUntil: 'networkidle' });
  if (await page.locator('.ob-wizard__welcome').count()) {
    await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
  }
  await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]');
  await page.locator('.ob-fields input').first().fill('V');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('#onboarding-home-address').fill('2671GB');
  await page.waitForTimeout(800);
  const sug = page.locator('.kb-address__suggest button, .address-suggestions button').first();
  if (await sug.count()) await sug.click();
  const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birth.count()) await birth.fill('15-06-2000');
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-2-illustrated]');
  await page.locator('.ob-wizard__presets .lobsy-choice-tile, .ob-wizard__presets button').first().click();
  await page.getByTestId('onboarding-next').click();
  await page.waitForSelector('[data-testid=onboarding-step-3-illustrated]');
  if (targetStep === 4) {
    await page.locator('#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button').first().click();
    await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
    await page.getByTestId('onboarding-next').click();
    await page.waitForTimeout(800);
    if (await page.locator('[data-testid=early-reward-nearby]').count()) {
      await page.getByTestId('onboarding-next').click();
    }
    await page.waitForSelector('[data-testid=onboarding-step-4-illustrated]');
    const job = page.locator('[data-testid=onboarding-work-job]').first();
    await job.locator('input').first().fill('Magazijnmedewerker');
    await job.locator('.ob-job-dates__current input[type=checkbox]').check();
  }
  const file = `${out}/onboarding-${slug}-390.png`;
  await page.screenshot({ path: file });
  console.log('saved', file);
  await browser.close();
}

await run(3, 'vervoer');
await run(4, 'werkervaring');
