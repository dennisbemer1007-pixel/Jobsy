/**
 * PR #755 review round 2 — paspoort sticky tabs + external vacancy bottom (390×844 viewport).
 *
 *   LOBSY_BASE_URL=http://127.0.0.1:5201 \
 *   OUT=/cursor/stores/self/artifacts/pr755-r2 \
 *   node tools/screenshots/capture-pr755-r2.mjs
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = (process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/cursor/stores/self/artifacts/pr755-r2';
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

/** Document scroll (passport page does not use .app-main overflow). */
async function setScrollY(page, y) {
  await page.evaluate((scrollY) => {
    const root = document.scrollingElement || document.documentElement;
    root.scrollTop = scrollY;
    window.scrollTo(0, scrollY);
    const main = document.querySelector('.app-main');
    if (main && main.scrollHeight > main.clientHeight + 4) {
      main.scrollTop = scrollY;
    }
  }, y);
  await page.waitForTimeout(450);
}

async function scrollUntilPassportTabsStuck(page) {
  const result = await page.evaluate(() => {
    const shell = document.querySelector('.passport-tabs-shell');
    if (!shell) {
      return { ok: false, reason: 'missing-shell' };
    }

    const headerEl = document.querySelector('.app-header');
    const headerH = headerEl?.getBoundingClientRect().height ?? 68;
    const stickyTop =
      parseFloat(getComputedStyle(shell).top) ||
      parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--app-header-height')) ||
      headerH;
    const pinTop = Number.isFinite(stickyTop) && stickyTop > 10 ? stickyTop : headerH;

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
      const overviewPast =
        !overview || overview.getBoundingClientRect().bottom <= pinTop + 8;
      const shellPinned = Math.abs(rect.top - pinTop) <= 4;
      return { rect, overviewPast, shellPinned };
    };

    let y = scrollToY(0);
    let { rect, overviewPast, shellPinned } = measure();

    if (!(overviewPast && shellPinned)) {
      const anchor = overview ?? shell;
      const anchorBottom = anchor.getBoundingClientRect().bottom;
      y = scrollToY(window.scrollY + anchorBottom - pinTop - 12);
      ({ rect, overviewPast, shellPinned } = measure());
    }

    if (!(overviewPast && shellPinned)) {
      const maxY = root.scrollHeight - window.innerHeight;
      for (let tryY = y; tryY <= maxY + 24; tryY += 32) {
        scrollToY(tryY);
        ({ rect, overviewPast, shellPinned } = measure());
        y = tryY;
        if (overviewPast && shellPinned) {
          break;
        }
      }
    }

    const ok = overviewPast && shellPinned && y >= 40;
    return {
      ok,
      reason: ok ? 'stuck' : 'never-stuck',
      scrollY: y,
      top: rect.top,
      headerH: pinTop,
      windowScrollY: window.scrollY,
    };
  });

  await setScrollY(page, result.scrollY ?? 0);
  return result;
}

async function auditPassportTabs(page) {
  return page.evaluate(() => {
    const shell = document.querySelector('.passport-tabs-shell');
    const tabs = document.querySelector('.passport-tabs');
    const bottomNav = document.querySelector('.bottom-nav');
    if (!shell || !tabs) {
      return { ok: false, reason: 'missing-shell-or-tabs' };
    }

    const shellBox = shell.getBoundingClientRect();
    const tabsBox = tabs.getBoundingClientRect();
    const navBox = bottomNav?.getBoundingClientRect();

    if (navBox && shellBox.bottom > navBox.top - 2) {
      return { ok: false, reason: 'tabs-overlap-bottom-nav' };
    }

    const pos = getComputedStyle(shell).position;
    if (pos !== 'sticky') {
      return { ok: false, reason: `shell-position-${pos}` };
    }

    const bg = getComputedStyle(shell).backgroundColor;
    if (!bg || bg === 'rgba(0, 0, 0, 0)' || bg === 'transparent') {
      return { ok: false, reason: 'shell-bg-transparent' };
    }

    const lastTab = tabs.querySelector('.passport-tab:last-child');
    if (lastTab) {
      const b = lastTab.getBoundingClientRect();
      const scrollable = tabs.scrollWidth > tabs.clientWidth + 2;
      const atEnd = tabs.scrollLeft >= tabs.scrollWidth - tabs.clientWidth - 4;
      if (b.right > tabsBox.right + 2 && b.left < tabsBox.right - 4 && !scrollable) {
        return { ok: false, reason: 'last-tab-clipped-without-scroll' };
      }
      if (atEnd && b.right > tabsBox.right + 2) {
        return { ok: false, reason: 'last-tab-clipped-at-scroll-end' };
      }
    }

    const fade = getComputedStyle(shell, '::after').content;
    const hasFadePseudo = shell.matches('.passport-page--illustrated .passport-tabs-shell')
      || document.querySelector('.passport-page--illustrated .passport-tabs-shell');

    return {
      ok: true,
      reason: 'ok',
      scrollLeft: tabs.scrollLeft,
      scrollWidth: tabs.scrollWidth,
      clientWidth: tabs.clientWidth,
      shellTop: shellBox.top,
      hasIllustratedFade: Boolean(hasFadePseudo),
      fadeContent: fade,
    };
  });
}

async function scrollTabsToEnd(page) {
  await page.evaluate(() => {
    const tabs = document.querySelector('.passport-tabs');
    if (!tabs) {
      return;
    }
    tabs.scrollLeft = tabs.scrollWidth;
  });
  await page.waitForTimeout(400);
}

async function scrollExternalDetailToActions(page) {
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

async function main() {
  await mkdir(outDir, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    locale: 'nl-NL',
    hasTouch: true,
    isMobile: true,
  });
  await context.addInitScript(consentInitScript);
  const page = await context.newPage();

  await login(page);

  await page.goto(`${baseUrl}/candidate/paspoort`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.waitForSelector('.passport-tabs-shell', { timeout: 60_000 });
  await page.waitForTimeout(2000);

  await setScrollY(page, 0);
  await snapViewport(page, path.join(outDir, 'paspoort-top-390.png'));

  const stuck = await scrollUntilPassportTabsStuck(page);
  console.log('stuck-scroll', stuck);
  if (!stuck.ok) {
    console.warn('WARNING: could not confirm sticky tabs position', stuck.reason);
  }

  const auditStuck = await auditPassportTabs(page);
  console.log('audit-stuck', auditStuck);
  await snapViewport(page, path.join(outDir, 'paspoort-tabs-stuck-390.png'));

  await scrollTabsToEnd(page);
  const auditEnd = await auditPassportTabs(page);
  console.log('audit-tabs-end', auditEnd);
  await snapViewport(page, path.join(outDir, 'paspoort-tabs-stuck-scrolled-end-390.png'));

  await page.goto(`${baseUrl}/candidate/external/${extVacancyId}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.waitForSelector('[data-testid="kb-ext-detail"]', { timeout: 60_000 });
  await page.waitForTimeout(1500);
  await scrollExternalDetailToActions(page);
  await snapViewport(page, path.join(outDir, 'external-vacancy-bottom-390.png'));

  const sourceRow = await page.locator('[data-testid="kb-ext-source"]').count();
  console.log('kb-ext-source visible:', sourceRow > 0);

  await context.close();
  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
