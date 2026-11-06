/** Import occupation-day JSON to Acceptatie admin, then download export via jobsyDownload hook. */
import { chromium } from 'playwright';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const EXPORT = path.join(
  path.resolve(path.dirname(new URL(import.meta.url).pathname), '..'),
  'docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json',
);

const consent = `try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}`;

async function login(page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('#login-email').fill('admin@jobsy.local');
  await page.locator('#login-password').fill('Jobsy123!');
  await page.locator('button[type=submit]').click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
}

const browser = await chromium.launch({
  headless: true,
  executablePath: '/opt/google/chrome/chrome',
  args: ['--no-sandbox'],
});
const context = await browser.newContext();
await context.addInitScript(consent);
const page = await context.newPage();
page.setDefaultTimeout(180_000);

try {
  await login(page);
  await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle' });
  await page.locator('input[type=file]').setInputFiles(EXPORT);
  for (let i = 0; i < 60; i++) {
    const msg = (await page.locator('.state-message').first().textContent().catch(() => ''))?.trim() ?? '';
    if (/bijgewerkt|nieuw|gelijk|afgekeurd|updated|inserted/i.test(msg)) {
      console.log('import:', msg);
      break;
    }
    await page.waitForTimeout(3000);
  }

  for (let i = 0; i < 40; i++) {
    const has = await page.evaluate(() => !!(window.jobsyDownload && window.jobsyDownload.bytes));
    if (has) break;
    await page.waitForTimeout(2000);
  }
  await page.evaluate(() => {
    const orig = window.jobsyDownload.bytes.bind(window.jobsyDownload);
    window.jobsyDownload.bytes = async (name, b64, media) => {
      window.__exportB64 = b64;
      return orig(name, b64, media);
    };
  });
  await page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click();
  for (let i = 0; i < 90; i++) {
    const b64 = await page.evaluate(() => window.__exportB64 || '');
    if (b64) {
      await writeFile(EXPORT, Buffer.from(b64, 'base64'));
      console.log('Saved export after import', EXPORT);
      break;
    }
    await page.waitForTimeout(2000);
  }
} finally {
  await browser.close();
}
