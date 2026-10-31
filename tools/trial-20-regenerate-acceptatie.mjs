/**
 * Background regen on Acceptatie: ?ids= all trial ESCO ids, Opnieuw genereren, poll until idle, export.
 */
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
  await page.locator('#login-email, input[name="email"]').first().fill('admin@jobsy.local');
  await page.locator('#login-password, input[name="password"]').first().fill('Jobsy123!');
  await page.locator('button[type="submit"]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
}

async function waitForAdminReady(page) {
  await page.waitForSelector('textarea', { timeout: 120_000 });
  for (let i = 0; i < 90; i++) {
    const body = await page.locator('body').innerText();
    if (body.includes('beroepen hebben een Nederlandse dag')) return body;
    await page.waitForTimeout(2000);
  }
  throw new Error('Admin day page did not load status');
}

function idsQuery(ids) {
  return encodeURIComponent(ids.join('\n'));
}

const manifest = JSON.parse(await readFile(MANIFEST, 'utf8'));
const allIds = manifest.occupations.map((o) => o.escoId);
const log = { startedAt: new Date().toISOString(), ids: allIds, mode: 'background-start-replace' };

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ acceptDownloads: true });
await context.addInitScript(consent);
const page = await context.newPage();

try {
  await login(page);
  const url = `${BASE}/admin/content/dag-in-het-leven?ids=${idsQuery(allIds)}`;
  await page.goto(url, { waitUntil: 'networkidle', timeout: 180_000 });
  await waitForAdminReady(page);
  const val = await page.locator('textarea').first().inputValue();
  if (!val.includes(allIds[0])) throw new Error('textarea missing ids');

  await page.getByRole('button', { name: /Opnieuw genereren|Regenerate selected/i }).click();
  await page.waitForTimeout(5000);
  log.kickoffMessage = (await page.locator('p.state-message').first().textContent())?.trim() ?? '';

  const deadline = Date.now() + 150 * 60_000;
  while (Date.now() < deadline) {
    await page.reload({ waitUntil: 'networkidle' });
    const body = await waitForAdminReady(page);
    const running = /Bezig|Running|loopt/i.test(body) || body.includes('deze run');
    const summary = (await page.locator('p.state-message').first().textContent())?.trim() ?? '';
    const runLine = body.match(/(\d+)[^\n]*mislukt[^\n]*/i)?.[0] ?? '';
    console.log(new Date().toISOString(), running ? 'RUNNING' : 'idle', summary.slice(0, 80), runLine.slice(0, 60));
    log.lastPoll = { at: new Date().toISOString(), running, summary, runLine };
    await writeFile(LOG, JSON.stringify(log, null, 2));
    if (!running && /gelukt|mislukt/i.test(summary)) break;
    if (!running && /OpenAI|sleutel|key/i.test(body + summary)) break;
    await page.waitForTimeout(30_000);
  }

  const client = await page.context().newCDPSession(page);
  await client.send('Browser.setDownloadBehavior', {
    behavior: 'allow',
    downloadPath: '/tmp/acc-occupation-export',
    eventsEnabled: true,
  });
  const [download] = await Promise.all([
    page.waitForEvent('download', { timeout: 300_000 }),
    page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click(),
  ]);
  await download.saveAs(EXPORT_PATH);
  console.log('Saved export', EXPORT_PATH);
  log.finishedAt = new Date().toISOString();
  await writeFile(LOG, JSON.stringify(log, null, 2));
} finally {
  await browser.close();
}
