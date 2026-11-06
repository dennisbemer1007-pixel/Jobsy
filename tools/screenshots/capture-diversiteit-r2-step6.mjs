import { chromium } from 'playwright';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const out = '/opt/cursor/artifacts/diversiteit-r2/onboarding-step-6-droombaan-1440.png';

async function fetchCode(email) {
  for (let i = 0; i < 40; i++) {
    const j = await (await fetch(`${baseUrl.replace(':5201', ':5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`)).json();
    if (j.code) return j.code;
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('no code');
}

const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'nl-NL' });
await ctx.addInitScript(() => {
  try {
    localStorage.setItem('Jobsy.CookieConsent', 'necessary');
  } catch (e) {}
});
const page = await ctx.newPage();
page.setDefaultTimeout(120000);
const email = `s6.${Date.now()}@jobsy.local`;
await page.goto(`${baseUrl}/account-maken`);
await page.locator('input[type=email]').fill(email);
await page.locator('input[name=firstName]').fill('S6');
await page.locator('button.au-submit, button.login-submit').first().click();
await page.waitForURL('**/code**');
await page.locator('input[name=code]').fill(await fetchCode(email));
await page.locator('button.login-submit').click();
await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'networkidle' });
if (await page.locator('.ob-wizard__welcome').count()) {
  await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
}
await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]');
await page.locator('.ob-fields input').first().fill('S6');
await page.locator('.ob-fields input').nth(1).fill('Test');
await page.locator('#onboarding-home-address').fill('2671GB');
await page.waitForTimeout(800);
const sug = page.locator('.kb-address__suggest button, .address-suggestions button').first();
if (await sug.count()) await sug.click();
const birth = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
if (await birth.count()) await birth.fill('15-06-2000');
await page.getByTestId('onboarding-next').click();
await page.waitForSelector('[data-testid=onboarding-step-2-illustrated]', { timeout: 120000 });
await page.locator('.ob-wizard__presets .lobsy-choice-tile, .ob-wizard__presets button').first().click();
await page.getByTestId('onboarding-next').click();
await page.waitForSelector('[data-testid=onboarding-step-3-illustrated]');
await page.locator('#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button').first().click();
await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
await page.getByTestId('onboarding-next').click();
await page.waitForTimeout(1000);
if (await page.locator('[data-testid=early-reward-nearby]').count()) {
  await page.getByTestId('onboarding-next').click();
  await page.waitForTimeout(800);
}
await page.waitForSelector('[data-testid=onboarding-step-4-illustrated]');
await page.getByTestId('onboarding-next').click();
await page.waitForSelector('[data-testid=onboarding-step-5-illustrated]');
await page.locator('.ob-wizard__education-grid .lobsy-choice-tile').filter({ hasText: 'HBO' }).first().click();
await page.getByTestId('onboarding-next').click();
await page.waitForSelector('[data-testid=onboarding-step-6-illustrated]');
await page.screenshot({ path: out });
console.log('saved', out);
await browser.close();
