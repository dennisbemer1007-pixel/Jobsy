/**
 * Regenerate trial occupations on Acceptatie (replace), then download export.
 *
 * Requires Acceptatie Web with OccupationDayAdmin @bind:event="oninput" and ?ids= prefill
 * (PR #1363 / merge to acceptatie). Without that deploy, Playwright cannot set _pick and
 * RegenerateAsync returns "Kies eerst één beroep." with no POST to the API.
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
const BATCH_SIZE = 15;

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

function idsQuery(ids) {
  return encodeURIComponent(ids.join('\n'));
}

async function regenerateBatch(page, ids, batchIndex) {
  const url = `${BASE}/admin/content/dag-in-het-leven?ids=${idsQuery(ids)}`;
  await page.goto(url, { waitUntil: 'networkidle', timeout: 180_000 });
  await page.waitForSelector('textarea', { timeout: 120_000 });
  const ta = page.locator('textarea').first();
  const val = await ta.inputValue();
  if (!val || !val.includes(ids[0])) {
    throw new Error(
      `Batch ${batchIndex}: textarea empty after ?ids= (Acceptatie Web missing oninput/?ids fix). Got length ${val.length}`,
    );
  }

  const done = page.waitForFunction(
    () => {
      const el = document.querySelector('.state-message');
      const t = el?.textContent?.trim() ?? '';
      return /^\d/.test(t) || /Kies eerst|Fout|error|mislukt|failed/i.test(t);
    },
    { timeout: 25 * 60_000 },
  );

  await page.getByRole('button', { name: /Opnieuw genereren|Regenerate selected/i }).click();
  await done;
  const msg = (await page.locator('.state-message').first().textContent())?.trim() ?? '';
  console.log('batch', batchIndex, msg.slice(0, 200));
  if (/Kies eerst/i.test(msg)) {
    throw new Error(`Batch ${batchIndex}: ${msg}`);
  }
  return { batchIndex, ids, message: msg };
}

const manifest = JSON.parse(await readFile(MANIFEST, 'utf8'));
const occupations = manifest.occupations;
const allIds = occupations.map((o) => o.escoId);
const log = {
  targetCommit: TARGET_COMMIT,
  mode: 'batch-url-ids',
  startedAt: new Date().toISOString(),
  rootCauseNote:
    'Prior run used textarea.fill without Blazor oninput; _pick stayed empty. API merge 3011cf22 is live on Acc API/Web 1b11779.',
  batches: [],
};

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
  for (let i = 0; i < allIds.length; i += BATCH_SIZE) {
    const batch = allIds.slice(i, i + BATCH_SIZE);
    log.batches.push(await regenerateBatch(page, batch, i / BATCH_SIZE + 1));
    await writeFile(LOG, JSON.stringify(log, null, 2));
  }

  await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle' });
  await page.evaluate(() => {
    window.__exportB64 = '';
    const orig = window.jobsyDownload?.bytes?.bind(window.jobsyDownload);
    if (orig) {
      window.jobsyDownload.bytes = async (name, b64, media) => {
        window.__exportB64 = b64;
        return orig(name, b64, media);
      };
    }
  });
  await page.getByRole('button', { name: /JSON downloaden|Download JSON/i }).click();
  for (let i = 0; i < 90; i++) {
    const b64 = await page.evaluate(() => window.__exportB64 || '');
    if (b64) {
      await writeFile(EXPORT_PATH, Buffer.from(b64, 'base64'));
      console.log('Saved export', EXPORT_PATH);
      break;
    }
    await page.waitForTimeout(2000);
  }

  log.finishedAt = new Date().toISOString();
  log.deployedCommit = await deployedCommit();
  await writeFile(LOG, JSON.stringify(log, null, 2));
} finally {
  await browser.close();
}
