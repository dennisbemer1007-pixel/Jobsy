/**
 * PR #755 live captures — full app (API + Web), Sanne de Vries, real chrome.
 *
 *   LOBSY_BASE_URL=http://127.0.0.1:5201 \
 *   OUT=/cursor/stores/self/artifacts/pr755 \
 *   node tools/screenshots/capture-pr755-live.mjs
 */
import { chromium } from 'playwright';
import { mkdir, cp } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = (process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/cursor/stores/self/artifacts/pr755';
const mirrorDir = process.env.MIRROR_OUT || '/opt/cursor/artifacts/oude-kreeft/pr755-live';
const extVacancyId = process.env.PR755_EXT_VACANCY_ID || 'eeeeeeee-2222-2222-2222-222222222222';

const email = process.env.PR755_EMAIL || 'sanne@jobsy.local';
const password = process.env.PR755_PASSWORD || 'Jobsy123!';
const onboardingEmail = process.env.PR755_ONBOARDING_EMAIL || 'onboarding.e2e@jobsy.local';

const viewports = [
  { tag: '390', width: 390, height: 844 },
  { tag: '1440', width: 1440, height: 900 },
];

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

async function snap(page, file, { fullPage = false, scrollY = 0 } = {}) {
  if (scrollY) {
    await page.evaluate((y) => {
      const main = document.querySelector('.app-main');
      if (main && 'scrollTop' in main) main.scrollTop = y;
      else window.scrollTo(0, y);
    }, scrollY);
    await page.waitForTimeout(400);
  }
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, fullPage, timeout: 120_000 });
  console.log('saved', file);
}

async function scrollToBottom(page) {
  await page.evaluate(() => {
    const main = document.querySelector('.app-main');
    const root = main || document.scrollingElement || document.documentElement;
    root.scrollTop = root.scrollHeight;
    window.scrollTo(0, document.body.scrollHeight);
  });
  await page.waitForTimeout(600);
}

async function main() {
  await mkdir(outDir, { recursive: true });
  await mkdir(mirrorDir, { recursive: true });

  const browser = await chromium.launch({ headless: true });

  for (const vp of viewports) {
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height },
      locale: 'nl-NL',
      hasTouch: vp.width < 500,
      isMobile: vp.width < 500,
    });
    await context.addInitScript(consentInitScript);
    const page = await context.newPage();

    // Public landing (logo-E mascot)
    await page.goto(`${baseUrl}/`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
    await page.waitForTimeout(2000);
    await snap(page, path.join(outDir, `landing-top-${vp.tag}.png`));
    await scrollToBottom(page);
    await snap(page, path.join(outDir, `landing-bottom-${vp.tag}.png`), { fullPage: true });

    await login(page);

    const routes = [
      ['external-vacancy', `/candidate/external/${extVacancyId}`, 'kb-ext-detail'],
      ['paspoort', '/candidate/paspoort', 'passport-page'],
      ['zoeken', '/banenkaart', 'jobsy-discovery'],
      ['sollicitaties', '/candidate/applications', 'kb-apps'],
      ['carriere', '/carriere', 'career-page'],
      ['reis', '/candidate/ontdekkingsreis', 'candidate-hub'],
    ];

    for (const [name, route, marker] of routes) {
      await page.goto(`${baseUrl}${route}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
      await page.waitForTimeout(name === 'zoeken' ? 3500 : 1800);
      try {
        await page.locator(`[data-testid="${marker}"], .${marker.split('-')[0]}`).first.waitFor({ timeout: 8000 });
      } catch {
        /* page-specific markers optional */
      }
      await snap(page, path.join(outDir, `${name}-top-${vp.tag}.png`));

      if (name === 'paspoort' && vp.tag === '390') {
        await page.evaluate(() => {
          const shell = document.querySelector('.passport-tabs-shell');
          const headerH = document.querySelector('.app-header')?.getBoundingClientRect().height ?? 56;
          const overview = document.querySelector('.passport-overview');
          const root = document.scrollingElement || document.documentElement;
          const anchor = overview ?? shell;
          if (!anchor) return;
          const y = Math.max(
            0,
            Math.min(
              window.scrollY + anchor.getBoundingClientRect().bottom - headerH - 12,
              root.scrollHeight - window.innerHeight,
            ),
          );
          root.scrollTop = y;
          window.scrollTo(0, y);
        });
        await page.waitForTimeout(500);
        await snap(page, path.join(outDir, `paspoort-tabs-stuck-${vp.tag}.png`));
        await page.evaluate(() => {
          const root = document.scrollingElement || document.documentElement;
          root.scrollTop = 0;
          window.scrollTo(0, 0);
        });
        await page.waitForTimeout(300);
        await snap(page, path.join(outDir, `paspoort-tabs-unstuck-${vp.tag}.png`));
      }

      await scrollToBottom(page);
      await snap(page, path.join(outDir, `${name}-bottom-${vp.tag}.png`), { fullPage: true });
    }

    // Onboarding step 1 (fresh e2e account; Sanne is usually past onboarding)
    await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
    await page.locator('input[name="email"], input[type="email"]').first().fill(onboardingEmail);
    await page.locator('input[name="password"], input[type="password"]').first().fill(password);
    await page.locator('form.au-form button[type="submit"], button.login-submit, button[type="submit"]').first().click();
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 90_000 }).catch(() => {});
    await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
    await page.waitForTimeout(2000);
    await snap(page, path.join(outDir, `onboarding-first-step-top-${vp.tag}.png`));
    await scrollToBottom(page);
    await snap(page, path.join(outDir, `onboarding-first-step-bottom-${vp.tag}.png`), { fullPage: true });

    await context.close();
  }

  await browser.close();

  // Mirror for VM artifacts path
  try {
    const { execSync } = await import('node:child_process');
    execSync(`mkdir -p "${mirrorDir}" && cp -r "${outDir}/." "${mirrorDir}/"`, { stdio: 'inherit' });
  } catch (e) {
    console.warn('mirror copy skipped', e.message);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
