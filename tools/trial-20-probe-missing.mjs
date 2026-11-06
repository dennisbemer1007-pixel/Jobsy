import { chromium } from 'playwright';

const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const IDS = ['2eac08c2-a81a-46fc-8d75-eb0e0f3e0f6d', 'eda0d957-3c3c-4139-b89a-a18bc9e18897'];

const consent = `try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}`;
const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext();
await context.addInitScript(consent);
const page = await context.newPage();
await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
await page.locator('input[type="email"], input[name="email"]').first().fill('admin@jobsy.local');
await page.locator('input[type="password"], input[name="password"]').first().fill('Jobsy123!');
await page.locator('button[type="submit"]').first().click();
await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle' });

for (const id of IDS) {
  await page.locator('textarea').first().fill(id);
  await page.getByRole('button', { name: /Test 1 beroep|Test 1 occupation/i }).click();
  await page.waitForTimeout(45_000);
  const msg = await page.locator('.state-message, p.state-message').first().textContent().catch(() => '');
  console.log(id, msg?.trim());
  await page.reload({ waitUntil: 'networkidle' });
}
await browser.close();
