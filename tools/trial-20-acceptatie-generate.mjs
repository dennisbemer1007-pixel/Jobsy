/**
 * Approved 20-occupation trial on Acceptatie (OpenAI runs server-side).
 */
import { chromium } from 'playwright';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';

const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const MANIFEST = path.join(ROOT, 'docs/mockups/leerling/werelden/data/trial-20-manifest.json');
const OUT_DIR = path.join(ROOT, 'docs/mockups/leerling/werelden/data');
const EXPORT_PATH = path.join(OUT_DIR, 'occupation-day-in-life.nl.json');
const LOG_PATH = path.join(OUT_DIR, 'trial-20-run-log.json');

const consent = `try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}`;

async function login(page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[type="email"], input[name="email"]').first().fill('admin@jobsy.local');
  await page.locator('input[type="password"], input[name="password"]').first().fill('Jobsy123!');
  await page.locator('button[type="submit"]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
  await page.waitForTimeout(2000);
}

async function openAdmin(page) {
  await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle', timeout: 180_000 });
  await page.waitForSelector('textarea', { timeout: 120_000 });
}

async function ensureNotRunning(page) {
  const stopBtn = page.getByRole('button', { name: /^Stoppen$|^Stop$/i });
  for (let i = 0; i < 8; i++) {
    const running = await page.getByText(/^Bezig:/).isVisible().catch(() => false);
    if (!running) return;
    if (await stopBtn.isEnabled().catch(() => false)) {
      await stopBtn.click();
    }
    await page.waitForTimeout(4000);
    await page.reload({ waitUntil: 'networkidle' });
  }
  const still = await page.getByText(/^Bezig:/).isVisible().catch(() => false);
  if (still) throw new Error('Batch still marked running on Acceptatie; stop via admin UI first.');
}

async function waitBatchDone(page, maxMs = 50 * 60_000) {
  const start = Date.now();
  while (Date.now() - start < maxMs) {
    const running = await page.getByText(/^Bezig:/).isVisible().catch(() => false);
    if (!running) return;
    await page.waitForTimeout(20_000);
    await page.reload({ waitUntil: 'networkidle' });
  }
  throw new Error('Batch timeout');
}

const manifest = JSON.parse(await readFile(MANIFEST, 'utf8'));
const ids = manifest.occupations.map((o) => o.escoId).join('\n');
const startedAt = new Date().toISOString();

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ acceptDownloads: true });
await context.addInitScript(consent);
const page = await context.newPage();

try {
  await login(page);
  await openAdmin(page);
  await ensureNotRunning(page);
  const startBtn = page.getByRole('button', { name: /Genereer ontbrekende dagen|Generate missing days/i });
  await startBtn.waitFor({ state: 'visible', timeout: 60_000 });
  if (!(await startBtn.isEnabled())) {
    throw new Error('Start disabled after stop.');
  }
  await page.locator('textarea').first().fill(ids);
  await startBtn.click();
  await page.waitForTimeout(5000);
  await waitBatchDone(page);

  const downloadPromise = page.waitForEvent('download', { timeout: 180_000 });
  await page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click();
  const download = await downloadPromise;
  await mkdir(OUT_DIR, { recursive: true });
  await download.saveAs(EXPORT_PATH);
  await writeFile(
    LOG_PATH,
    JSON.stringify({ startedAt, finishedAt: new Date().toISOString(), exportPath: EXPORT_PATH }, null, 2),
  );
  console.log('Saved export', EXPORT_PATH);
} finally {
  await browser.close();
}
