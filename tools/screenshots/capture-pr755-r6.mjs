/**
 * PR #755 r6 — external vacancy coach guard + scroll captures.
 *
 *   LOBSY_BASE_URL=http://127.0.0.1:5201 \
 *   OUT=/cursor/stores/self/artifacts/pr755-r6 \
 *   node tools/screenshots/capture-pr755-r6.mjs
 */
import { chromium } from 'playwright';
import { mkdir, stat, readFile } from 'node:fs/promises';
import path from 'node:path';
import { createHash } from 'node:crypto';

const baseUrl = (process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/cursor/stores/self/artifacts/pr755-r6';
const extVacancyId = process.env.PR755_EXT_VACANCY_ID || 'eeeeeeee-2222-2222-2222-222222222222';
const email = process.env.PR755_EMAIL || 'sanne@jobsy.local';
const password = process.env.PR755_PASSWORD || 'Jobsy123!';

const consentInitScript = `
  try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
  document.documentElement.classList.add('cookie-consent-known');
`;

async function login(page) {
  await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[name="email"], input[type="email"]').first().fill(email, { timeout: 60_000 });
  await page.locator('input[name="password"], input[type="password"]').first().fill(password);
  await page.locator('form.au-form button[type="submit"], button.login-submit, button[type="submit"]').first().click();
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 90_000 });
  await page.locator('[data-lobsy-circuit="ready"]').waitFor({ timeout: 90_000 });
}

async function waitForDetail(page) {
  await page.locator('[data-testid="kb-ext-detail"] .kb-ext__title').waitFor({ state: 'visible', timeout: 120_000 });
  await page.locator('[data-testid="kb-ext-detail-actions"]').waitFor({ state: 'visible', timeout: 60_000 });
}

async function setScrollY(page, y) {
  await page.evaluate((scrollY) => {
    const root = document.scrollingElement || document.documentElement;
    root.scrollTop = scrollY;
    window.scrollTo(0, scrollY);
  }, y);
  await page.waitForTimeout(500);
}

async function snap(page, file, label) {
  const title = ((await page.locator('.kb-ext__title').textContent()) || '').trim();
  if (!title || /^laden/i.test(title)) {
    throw new Error(`${label}: still loading (${title})`);
  }
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, fullPage: false, timeout: 120_000 });
  const buf = await readFile(file);
  const sha = createHash('sha256').update(buf).digest('hex').slice(0, 12);
  console.log('saved', file, 'bytes', buf.length, 'sha', sha, 'scrollY', await page.evaluate(() => window.scrollY));
  return { sha, size: buf.length };
}

async function capture390(page) {
  await page.goto(`${baseUrl}/candidate/external/${extVacancyId}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('[data-lobsy-circuit="ready"]').waitFor({ timeout: 90_000 });
  await waitForDetail(page);

  await setScrollY(page, 0);
  const top = await snap(page, path.join(outDir, 'external-vacancy-top-390.png'), 'top-390');

  await setScrollY(page, 320);
  const coachHidden = await page.evaluate(() => {
    const dock = document.querySelector('.lobsy-coach-dock');
    return dock?.classList.contains('lobsy-coach-dock--suppressed') ?? false;
  });
  console.log('scroll320 coach suppressed', coachHidden);
  const mid = await snap(page, path.join(outDir, 'external-vacancy-scroll320-390.png'), 'scroll320-390');

  await page.evaluate(() => {
    const root = document.scrollingElement || document.documentElement;
    const maxY = Math.max(0, root.scrollHeight - window.innerHeight);
    root.scrollTop = maxY;
    window.scrollTo(0, maxY);
  });
  await page.waitForTimeout(600);
  const bottom = await snap(page, path.join(outDir, 'external-vacancy-bottom-390.png'), 'bottom-390');

  for (const [nameA, nameB, metaA, metaB] of [
    ['top', 'mid', top, mid],
    ['top', 'bottom', top, bottom],
    ['mid', 'bottom', mid, bottom],
  ]) {
    if (metaA.sha === metaB.sha) throw new Error(`${nameA} and ${nameB} PNG identical`);
  }
}

async function capture1440(page) {
  await page.goto(`${baseUrl}/candidate/external/${extVacancyId}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('[data-lobsy-circuit="ready"]').waitFor({ timeout: 90_000 });
  await waitForDetail(page);
  await setScrollY(page, 0);
  const top = await snap(page, path.join(outDir, 'external-vacancy-top-1440.png'), 'top-1440');
  await page.evaluate(() => {
    const root = document.scrollingElement || document.documentElement;
    root.scrollTop = root.scrollHeight;
    window.scrollTo(0, root.scrollHeight);
  });
  await page.waitForTimeout(500);
  const bottom = await snap(page, path.join(outDir, 'external-vacancy-bottom-1440.png'), 'bottom-1440');
  if (top.sha === bottom.sha) throw new Error('1440 top and bottom identical');
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });

  {
    const ctx = await browser.newContext({ viewport: { width: 390, height: 844 }, locale: 'nl-NL', hasTouch: true, isMobile: true });
    await ctx.addInitScript(consentInitScript);
    const page = await ctx.newPage();
    await login(page);
    await capture390(page);
    await ctx.close();
  }

  {
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'nl-NL' });
    await ctx.addInitScript(consentInitScript);
    const page = await ctx.newPage();
    await login(page);
    await capture1440(page);
    await ctx.close();
  }

  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
