import { chromium } from 'playwright';

const baseUrl = 'http://127.0.0.1:5201';
const email = `debug.${Date.now()}@jobsy.local`;
const browser = await chromium.launch({ headless: true });
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
await page.goto(`${baseUrl}/account-maken`, { waitUntil: 'domcontentloaded' });
await page.locator('input[type=email]').fill(email);
await page.locator('input[name=firstName]').fill('Debug');
await page.locator('button.login-submit, button.au-submit[type=submit]').first().click();
await page.waitForURL('**/code**', { timeout: 60000 });
let code = null;
for (let i = 0; i < 40; i++) {
  const r = await fetch(`${baseUrl.replace('5201','5200')}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`);
  const j = await r.json();
  if (j.code) { code = j.code; break; }
  await new Promise(r => setTimeout(r, 500));
}
console.log('code', code);
await page.locator('input[name=code]').fill(code ?? '000000');
await page.locator('button.login-submit').click();
await page.waitForTimeout(3000);
console.log('after code url', page.url());
await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'domcontentloaded' });
await page.waitForSelector('.ob-wizard__welcome, [data-testid^=onboarding-step-]', { timeout: 90000 });
if (await page.locator('.ob-wizard__welcome').count() > 0) {
  await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
}
try {
  await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]', { timeout: 90000 });
  console.log('step1 illustrated visible');
} catch (e) {
  console.log('step1 illustrated NOT visible', e.message);
}
console.log('has ob-fields', await page.locator('.ob-fields input').count());
await page.screenshot({ path: '/tmp/debug-onboarding.png', fullPage: true });
await browser.close();
