/**
 * PR #755 review round 4 — external vacancy apply width + 1440 load wait.
 *
 *   LOBSY_BASE_URL=http://127.0.0.1:5201 \
 *   OUT=/cursor/stores/self/artifacts/pr755-r4 \
 *   node tools/screenshots/capture-pr755-r4.mjs
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = (process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/cursor/stores/self/artifacts/pr755-r4';
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
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 90_000 }).catch(() => {});
  await page.waitForFunction(
    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready',
    null,
    { timeout: 90_000 },
  ).catch(() => {});
  await page.waitForTimeout(800);
}

async function snapViewport(page, file) {
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, fullPage: false, timeout: 120_000 });
  console.log('saved', file);
}

async function setScrollY(page, y) {
  await page.evaluate((scrollY) => {
    const root = document.scrollingElement || document.documentElement;
    root.scrollTop = scrollY;
    window.scrollTo(0, scrollY);
  }, y);
  await page.waitForTimeout(450);
}

async function waitForExternalVacancyDetail(page) {
  await page.locator('[data-testid="kb-ext-detail"] .kb-ext__title').waitFor({
    state: 'visible',
    timeout: 120_000,
  });
  await page.locator('[data-testid="kb-ext-detail"] .kb-ext__title').filter({ hasNotText: /^Laden/i }).waitFor({
    timeout: 120_000,
  });
  await page.locator('[data-testid="kb-ext-detail-actions"]').waitFor({ timeout: 60_000 });
}

async function scrollExternalToBottom(page) {
  await page.evaluate(() => {
    const actions = document.querySelector('[data-testid="kb-ext-detail-actions"]');
    const root = document.scrollingElement || document.documentElement;
    if (actions) {
      const y = window.scrollY + actions.getBoundingClientRect().top - window.innerHeight * 0.55;
      root.scrollTop = Math.max(0, y);
      window.scrollTo(0, Math.max(0, y));
    } else {
      root.scrollTop = root.scrollHeight;
      window.scrollTo(0, document.body.scrollHeight);
    }
  });
  await page.waitForTimeout(500);
}

async function captureExternalVacancy(page, tag) {
  await page.goto(`${baseUrl}/candidate/external/${extVacancyId}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await waitForExternalVacancyDetail(page);
  await setScrollY(page, 0);
  await snapViewport(page, path.join(outDir, `external-vacancy-top-${tag}.png`));
  await scrollExternalToBottom(page);
  const metrics = await page.evaluate(() => {
    const card = document.querySelector('.kb-ext__card')?.getBoundingClientRect();
    const apply = document.querySelector('[data-testid="kb-ext-apply-cta"]')?.getBoundingClientRect();
    const coach = document.querySelector('#lobsy-coach-btn, .lobsy-coach-dock__btn')?.getBoundingClientRect();
    const notInterested = document.querySelector('[data-testid="kb-ext-not-interested"]')?.getBoundingClientRect();
    return {
      cardW: card?.width,
      applyW: apply?.width,
      applyRight: apply?.right,
      coachLeft: coach?.left,
      notInterestedRight: notInterested?.right,
      coachGap: coach && notInterested ? coach.left - notInterested.right : null,
    };
  });
  console.log(`metrics-${tag}`, metrics);
  await snapViewport(page, path.join(outDir, `external-vacancy-bottom-${tag}.png`));
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });

  for (const vp of [
    { tag: '390', width: 390, height: 844, mobile: true },
    { tag: '1440', width: 1440, height: 900, mobile: false },
  ]) {
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height },
      locale: 'nl-NL',
      hasTouch: vp.mobile,
      isMobile: vp.mobile,
    });
    await context.addInitScript(consentInitScript);
    const page = await context.newPage();
    await login(page);
    await captureExternalVacancy(page, vp.tag);
    await context.close();
  }

  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
