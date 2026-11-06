import { chromium } from 'playwright';
import path from 'node:path';

const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const EXPORT = path.join(
  path.resolve(path.dirname(new URL(import.meta.url).pathname), '..'),
  'docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json',
);

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ acceptDownloads: true });
await context.addInitScript(() => {
  try {
    localStorage.setItem('Jobsy.CookieConsent', 'necessary');
  } catch {}
});
const page = await context.newPage();
await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
await page.locator('#login-email, input[name="email"]').first().fill('admin@jobsy.local');
await page.locator('#login-password, input[name="password"]').first().fill('Jobsy123!');
await page.locator('button[type="submit"]').first().click();
await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle', timeout: 180_000 });
await page.waitForSelector('textarea', { timeout: 120_000 });
const [download] = await Promise.all([
  page.waitForEvent('download', { timeout: 300_000 }),
  page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click(),
]);
await download.saveAs(EXPORT);
console.log('Saved', EXPORT);
await browser.close();
