/** Probe Acceptatie regen: blur textarea so Blazor @bind picks up ids; capture API JWT. */
import { chromium } from 'playwright';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const BASE = (process.env.ACCEPTATIE_WEB || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const MANIFEST = path.join(ROOT, 'docs/mockups/leerling/werelden/data/trial-20-manifest.json');

const consent = `try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}`;

const manifest = JSON.parse(await readFile(MANIFEST, 'utf8'));
const probeId = manifest.occupations.find((o) => o.titleNl === 'bioloog')?.escoId ?? manifest.occupations[0].escoId;

const browser = await chromium.launch({ headless: true, args: ['--no-sandbox'] });
const context = await browser.newContext();
await context.addInitScript(consent);
const page = await context.newPage();

let capturedJwt = null;
page.on('request', (req) => {
  const auth = req.headers()['authorization'];
  if (auth?.startsWith('Bearer eyJ') && !capturedJwt) capturedJwt = auth.slice(7);
});

await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
await page.locator('#login-email, input[name="email"]').first().fill('admin@jobsy.local');
await page.locator('#login-password, input[name="password"]').first().fill('Jobsy123!');
await page.locator('button[type="submit"]').first().click();
await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 120_000 });

await page.goto(`${BASE}/admin/content/dag-in-het-leven`, { waitUntil: 'networkidle', timeout: 180_000 });
await page.waitForSelector('textarea', { timeout: 120_000 });

const ta = page.locator('textarea').first();
await ta.click();
await ta.fill(probeId);
await ta.evaluate((el) => el.dispatchEvent(new Event('change', { bubbles: true })));
await page.keyboard.press('Tab');
await page.waitForTimeout(500);

const domVal = await ta.inputValue();
console.log('textarea value length', domVal.length, 'id match', domVal.includes(probeId));

const regenPromise = page.waitForResponse(
  (r) => r.url().includes('occupation-day-in-life/regenerate') && r.request().method() === 'POST',
  { timeout: 120_000 },
).catch(() => null);

await page.getByRole('button', { name: /Opnieuw genereren|Regenerate selected/i }).click();
const regenRes = await regenPromise;
const msg = (await page.locator('.state-message').first().textContent({ timeout: 30_000 }).catch(() => ''))?.trim() ?? '';
console.log('UI message:', msg);
if (regenRes) {
  console.log('regenerate HTTP', regenRes.status(), (await regenRes.text()).slice(0, 400));
} else {
  console.log('no regenerate HTTP (PickEmpty or client-only?)');
}

console.log('capturedJwt', capturedJwt ? `${capturedJwt.slice(0, 40)}…` : '(none)');

await writeFile('/tmp/trial-20-regen-probe.json', JSON.stringify({ probeId, domVal, msg, capturedJwt: !!capturedJwt }, null, 2));
await browser.close();
