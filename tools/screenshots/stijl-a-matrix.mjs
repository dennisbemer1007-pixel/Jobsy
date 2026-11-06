/**
 * Stijl A before/after matrix (local demo users only).
 *
 *   BEFORE_BASE=http://127.0.0.1:5202 AFTER_BASE=http://127.0.0.1:5201 \
 *   OUT=/opt/cursor/artifacts/stijl-A-r2 node tools/screenshots/stijl-a-matrix.mjs
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const beforeBase = (process.env.BEFORE_BASE || '').replace(/\/$/, '');
const afterBase = (process.env.AFTER_BASE || process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outRoot = process.env.OUT || '/opt/cursor/artifacts/stijl-A-r4';
const pairPy = path.join(path.dirname(new URL(import.meta.url).pathname), 'stijl-a-pair.py');

const creds = {
  candidate: { email: 'kandidaat@jobsy.local', password: 'Jobsy123!' },
  employer: { email: 'ondernemer@jobsy.local', password: 'Jobsy123!' },
  admin: { email: 'admin@jobsy.local', password: 'Jobsy123!' },
};

/** @type {[string, string, number, number, 'anon'|'candidate'|'employer'|'admin'][]} */
const shots = [
  ['start', '/', 390, 844, 'anon'],
  ['carriere', '/carriere', 1440, 900, 'candidate'],
  ['carriere', '/carriere', 390, 844, 'candidate'],
  ['werkgever-binnenkort', '/werkgevers/binnenkort', 1440, 900, 'employer'],
  ['werkgever-vacature', '/werkgever/vacatures/nieuw', 390, 844, 'employer'],
  ['paspoort', '/candidate/paspoort', 390, 844, 'candidate'],
  ['sollicitaties', '/candidate/applications', 390, 844, 'candidate'],
  ['vacature', `/vacancies/${process.env.STIJL_A_VACANCY_ID || '11111111-1111-1111-1111-111111111111'}`, 390, 844, 'candidate'],
  ['werkgever-sollicitaties', '/werkgever/sollicitaties', 1440, 900, 'employer'],
];

const consentInitScript = `
  try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
  document.documentElement.classList.add('cookie-consent-known');
`;

async function loginOn(page, base, role) {
  const c = creds[role];
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[name="email"], input[type="email"]').first().fill(c.email, { timeout: 60_000 });
  await page.locator('input[name="password"], input[type="password"]').first().fill(c.password);
  await page.locator('form.au-form button[type="submit"], button[type="submit"]').first().click();
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 60_000 }).catch(() => {});
  await page.waitForTimeout(900);
}

async function capture(base, subdir, name, route, w, h, role, browser) {
  const ctx = await browser.newContext({ viewport: { width: w, height: h } });
  await ctx.addInitScript(consentInitScript);
  const page = await ctx.newPage();
  if (role !== 'anon') await loginOn(page, base, role);
  await page.goto(`${base}${route}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.waitForTimeout(route === '/' ? 2200 : 1400);
  const file = path.join(outRoot, subdir, `${name}.png`);
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, fullPage: false, timeout: 60_000 });
  await ctx.close();
  console.log('wrote', file);
  return file;
}

const browser = await chromium.launch({ headless: true });
for (const [name, route, w, h, role] of shots) {
  const slug = `${name}-${w}x${h}`;
  let beforeFile = '';
  if (beforeBase) {
    beforeFile = await capture(beforeBase, 'before', slug, route, w, h, role, browser);
  }
  const afterFile = await capture(afterBase, 'after', slug, route, w, h, role, browser);
  if (beforeBase) {
    const pairOut = path.join(outRoot, 'pairs', `${slug}.png`);
    execFileSync('python3', [pairPy, beforeFile, afterFile, pairOut], { stdio: 'inherit' });
  }
}
await browser.close();
