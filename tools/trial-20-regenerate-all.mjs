/** Regenerate each trial occupation on Acceptatie (one sync call per id). */
import { chromium } from 'playwright';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const MANIFEST = path.join(ROOT, 'docs/mockups/leerling/werelden/data/trial-20-manifest.json');
const EXPORT_PATH = path.join(ROOT, 'docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json');
const LOG = path.join(ROOT, 'docs/mockups/leerling/werelden/data/trial-20-regen-log.json');

const consent = `try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}`;

async function login(page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[type="email"], input[name="email"]').first().fill('admin@jobsy.local');
  await page.locator('input[type="password"], input[name="password"]').first().fill('Jobsy123!');
  await page.locator('button[type="submit"]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
}

const manifest = JSON.parse(await readFile(MANIFEST, 'utf8'));
const ids = manifest.occupations.map((o) => o.escoId);
const log = { startedAt: new Date().toISOString(), items: [] };

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ acceptDownloads: true });
await context.addInitScript(consent);
const page = await context.newPage();

try {
  await login(page);
  await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle', timeout: 180_000 });

  for (const id of ids) {
    const title = manifest.occupations.find((o) => o.escoId === id)?.titleNl ?? id;
    await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle', timeout: 180_000 });
    await page.locator('textarea').first().fill(id);
    const regen = page.getByRole('button', { name: /Opnieuw genereren|Regenerate selected/i });
    await regen.click();
    await page.waitForTimeout(120_000);
    const msg = await page.locator('.state-message').first().textContent().catch(() => '');
    log.items.push({ id, title, message: (msg || '').trim().slice(0, 200) });
    console.log(title, (msg || '').trim().slice(0, 80));
  }

  await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle' });
  const downloadPromise = page.waitForEvent('download', { timeout: 120_000 });
  await page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click();
  await (await downloadPromise).saveAs(EXPORT_PATH);
  log.finishedAt = new Date().toISOString();
  await writeFile(LOG, JSON.stringify(log, null, 2));
  console.log('Saved export', EXPORT_PATH);
} finally {
  await browser.close();
}
