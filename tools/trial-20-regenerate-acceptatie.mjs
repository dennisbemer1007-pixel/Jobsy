/**
 * Regenerate all trial occupations on Acceptatie (replace), download export.
 */
import { chromium } from 'playwright';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const TARGET_COMMIT = (process.env.ACCEPTATIE_COMMIT || '3011cf22').slice(0, 8);
const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const MANIFEST = path.join(ROOT, 'docs/mockups/leerling/werelden/data/trial-20-manifest.json');
const EXPORT_PATH = path.join(ROOT, 'docs/mockups/leerling/werelden/data/occupation-day-in-life.nl.json');
const LOG = path.join(ROOT, 'docs/mockups/leerling/werelden/data/trial-20-regen-log.json');
const WAIT_PER_OCC_MS = Number(process.env.REGEN_WAIT_MS || 360_000);

const consent = `try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}`;

async function deployedCommit() {
  const res = await fetch(`${BASE}/`, { redirect: 'follow' });
  const html = await res.text();
  const m = html.match(/lobsy-commit" content="([a-f0-9]+)"/i);
  return m?.[1]?.slice(0, 8) ?? '';
}

async function waitForDeploy(maxMs = 45 * 60_000) {
  const start = Date.now();
  while (Date.now() - start < maxMs) {
    const sha = await deployedCommit();
    console.log('acceptatie commit', sha || '(unknown)', 'want', TARGET_COMMIT);
    if (sha.startsWith(TARGET_COMMIT)) return sha;
    await new Promise((r) => setTimeout(r, 30_000));
  }
  throw new Error(`Acceptatie not on ${TARGET_COMMIT} after ${maxMs / 60000} min`);
}

async function login(page) {
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('#login-email, input[name="email"]').first().fill('admin@jobsy.local', { timeout: 60_000 });
  await page.locator('#login-password, input[name="password"]').first().fill('Jobsy123!');
  await page.locator('button[type="submit"]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });
}

async function openAdmin(page) {
  await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle', timeout: 180_000 });
  await page.waitForSelector('textarea', { timeout: 120_000 });
}

async function regenerateOne(page, id, title) {
  await openAdmin(page);
  await page.locator('textarea').first().fill(id);
  await page.getByRole('button', { name: /Opnieuw genereren|Regenerate selected/i }).click();
  await page.waitForTimeout(WAIT_PER_OCC_MS);
  await openAdmin(page);
  const msg =
    (await page.locator('.state-message').first().textContent({ timeout: 15_000 }).catch(() => ''))?.trim() ?? '';
  console.log(title, msg.slice(0, 100));
  return { id, title, message: msg };
}

const manifest = JSON.parse(await readFile(MANIFEST, 'utf8'));
const occupations = manifest.occupations;
const log = { targetCommit: TARGET_COMMIT, waitPerOccMs: WAIT_PER_OCC_MS, startedAt: new Date().toISOString(), items: [] };

if (process.env.SKIP_DEPLOY_WAIT !== '1') {
  await waitForDeploy();
}

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext({ acceptDownloads: true });
await context.addInitScript(consent);
const page = await context.newPage();
page.setDefaultTimeout(120_000);

try {
  await login(page);
  for (const occ of occupations) {
    log.items.push(await regenerateOne(page, occ.escoId, occ.titleNl));
    await writeFile(LOG, JSON.stringify(log, null, 2));
  }

  await openAdmin(page);
  const downloadPromise = page.waitForEvent('download', { timeout: 180_000 });
  await page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click();
  await (await downloadPromise).saveAs(EXPORT_PATH);
  console.log('Saved export', EXPORT_PATH);

  log.finishedAt = new Date().toISOString();
  log.deployedCommit = await deployedCommit();
  await writeFile(LOG, JSON.stringify(log, null, 2));
} finally {
  await browser.close();
}
