/**
 * PR #755 review round 3 — paspoort stuck tabs + external vacancy layout captures.
 *
 *   LOBSY_BASE_URL=http://127.0.0.1:5201 \
 *   OUT=/cursor/stores/self/artifacts/pr755-r3 \
 *   node tools/screenshots/capture-pr755-r3.mjs
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = (process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/cursor/stores/self/artifacts/pr755-r3';
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
  await page.waitForTimeout(1200);
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

async function scrollUntilPassportTabsStuck(page) {
  const result = await page.evaluate(() => {
    const shell = document.querySelector('.passport-tabs-shell');
    if (!shell) return { ok: false, reason: 'missing-shell' };

    const headerEl = document.querySelector('.app-header');
    const headerH = headerEl?.getBoundingClientRect().height ?? 68;
    const pinTop = headerH;
    const overview = document.querySelector('.passport-overview');
    const root = document.scrollingElement || document.documentElement;

    const scrollToY = (y) => {
      const clamped = Math.max(0, Math.min(y, root.scrollHeight - window.innerHeight));
      root.scrollTop = clamped;
      window.scrollTo(0, clamped);
      return clamped;
    };

    const measure = () => {
      const rect = shell.getBoundingClientRect();
      const h = headerEl?.getBoundingClientRect();
      const headerVisible = h && h.bottom > 4;
      const overviewPast = !overview || overview.getBoundingClientRect().bottom <= pinTop + 8;
      const shellPinned = Math.abs(rect.top - pinTop) <= 4;
      return { rect, overviewPast, shellPinned, headerVisible, headerBottom: h?.bottom ?? 0 };
    };

    let y = scrollToY(0);
    let m = measure();

    if (!(m.overviewPast && m.shellPinned && m.headerVisible)) {
      const anchor = overview ?? shell;
      y = scrollToY(window.scrollY + anchor.getBoundingClientRect().bottom - pinTop - 12);
      m = measure();
    }

    if (!(m.overviewPast && m.shellPinned && m.headerVisible)) {
      const maxY = root.scrollHeight - window.innerHeight;
      for (let tryY = y; tryY <= maxY + 24; tryY += 32) {
        scrollToY(tryY);
        m = measure();
        y = tryY;
        if (m.overviewPast && m.shellPinned && m.headerVisible) break;
      }
    }

    const ok = m.overviewPast && m.shellPinned && m.headerVisible && y >= 40;
    return {
      ok,
      reason: ok ? 'stuck' : 'never-stuck',
      scrollY: y,
      top: m.rect.top,
      headerBottom: m.headerBottom,
      windowScrollY: window.scrollY,
    };
  });

  await setScrollY(page, result.scrollY ?? 0);
  return result;
}

async function scrollTabsToEnd(page) {
  await page.evaluate(() => {
    const tabs = document.querySelector('.passport-tabs');
    if (tabs) tabs.scrollLeft = tabs.scrollWidth;
  });
  await page.waitForTimeout(400);
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

async function capturePassport390(page) {
  await page.goto(`${baseUrl}/candidate/paspoort`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.waitForSelector('.passport-tabs-shell', { timeout: 60_000 });
  await page.waitForTimeout(2000);

  const stuck = await scrollUntilPassportTabsStuck(page);
  console.log('stuck-scroll', stuck);
  await snapViewport(page, path.join(outDir, 'paspoort-tabs-stuck-390.png'));

  await scrollTabsToEnd(page);
  await snapViewport(page, path.join(outDir, 'paspoort-tabs-stuck-scrolled-end-390.png'));
}

async function captureExternalVacancy(page, tag) {
  await page.goto(`${baseUrl}/candidate/external/${extVacancyId}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.waitForSelector('[data-testid="kb-ext-detail"]', { timeout: 60_000 });
  await page.waitForTimeout(1500);
  await setScrollY(page, 0);
  await snapViewport(page, path.join(outDir, `external-vacancy-top-${tag}.png`));
  await scrollExternalToBottom(page);
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

    if (vp.tag === '390') {
      await capturePassport390(page);
    }
    await captureExternalVacancy(page, vp.tag);
    await context.close();
  }

  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
