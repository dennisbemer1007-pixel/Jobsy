/** Regenerate missing trial occupations (sync admin API via Blazor). */
import { chromium } from 'playwright';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';

const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const EXPORT_PATH = path.join(ROOT, 'docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json');

const MISSING = [
  '2eac08c2-a81a-46fc-8d75-eb0e0f3e0f6d',
  'eda0d957-3c3c-4139-b89a-a18bc9e18897',
];

const consent = `try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}`;

async function login(page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[type="email"], input[name="email"]').first().fill('admin@jobsy.local');
  await page.locator('input[type="password"], input[name="password"]').first().fill('Jobsy123!');
  await page.locator('button[type="submit"]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
}

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ acceptDownloads: true });
await context.addInitScript(consent);
const page = await context.newPage();

try {
  await login(page);
  await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle', timeout: 180_000 });
  await page.waitForSelector('textarea', { timeout: 120_000 });

  for (const id of MISSING) {
    await page.locator('textarea').first().fill(id);
    const regen = page.getByRole('button', { name: /Opnieuw genereren|Regenerate selected/i });
    await regen.click();
    await page.waitForTimeout(90_000);
    const status = await page.locator('.admin-day-msg, .provider-msg, p').allTextContents().catch(() => []);
    console.log('after', id, status.slice(-5).join(' | '));
    await page.reload({ waitUntil: 'networkidle' });
  }

  const downloadPromise = page.waitForEvent('download', { timeout: 300_000 });
  await page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click();
  const download = await downloadPromise;
  await download.saveAs(EXPORT_PATH);
  console.log('Saved', EXPORT_PATH);
} finally {
  await browser.close();
}
