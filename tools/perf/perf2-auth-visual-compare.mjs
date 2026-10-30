/**
 * Before (acceptatie) vs after (local) static auth pages — 390 & 1440.
 *   BASE=http://127.0.0.1:5201 ACC_BASE=https://acceptatie.lobsy.nl \
 *   OUT=/cursor/stores/self/perf2/screenshots \
 *   JOBSY_CAPTURE_EMAIL=1 node tools/perf/perf2-auth-visual-compare.mjs
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const local = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const acc = (process.env.ACC_BASE || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const out = process.env.OUT || '/cursor/stores/self/perf2/screenshots';
const captureEmail = process.env.JOBSY_CAPTURE_EMAIL === '1';

async function shot(page, file) {
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, type: 'jpeg', quality: 88, fullPage: false });
  console.log('wrote', file);
}

async function freshContext(browser, w, h) {
  const ctx = await browser.newContext({ viewport: { width: w, height: h } });
  await ctx.addInitScript(() => {
    try {
      localStorage.removeItem('Jobsy.CookieConsent');
    } catch (e) {}
    document.documentElement.classList.remove('cookie-consent-known');
  });
  return ctx;
}

async function captureCodeStep(page, origin) {
  const email = `perf2.visual.${Date.now()}@example.test`;
  await page.goto(`${origin}/account-maken`, { waitUntil: 'domcontentloaded', timeout: 90_000 });
  await page.locator('.cookie-consent [data-consent=necessary]').first().click({ timeout: 8000 }).catch(() => {});
  await page.locator('input[name=email]').fill(email);
  await page.locator('button.au-submit, button.login-submit').first().click();
  await page.waitForURL('**/account-maken/code**', { timeout: 90_000 });
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const pages = [
    { slug: 'login', path: '/login', codeFlow: false },
    { slug: 'account-maken', path: '/account-maken', codeFlow: false },
    { slug: 'account-maken-code', path: null, codeFlow: true },
  ];

  for (const [w, h, tag] of [[390, 844, 'mobile'], [1440, 900, 'desktop']]) {
    for (const { slug, path: pagePath, codeFlow } of pages) {
      for (const [origin, prefix] of [[acc, 'before-acc'], [local, 'after-local']]) {
        if (codeFlow && !captureEmail && origin === local) {
          console.log('skip code step local — set JOBSY_CAPTURE_EMAIL=1');
          continue;
        }
        if (codeFlow && origin === acc) {
          continue;
        }

        const ctx = await freshContext(browser, w, h);
        const page = await ctx.newPage();
        if (codeFlow) {
          await captureCodeStep(page, origin);
        } else {
          await page.goto(`${origin}${pagePath}`, { waitUntil: 'domcontentloaded', timeout: 90_000 });
        }

        const metrics = await page.evaluate(() => {
          const email = document.querySelector('input[name=email], #login-email');
          const provider = document.querySelector('.provider-btn, .au-provider');
          const cookie = document.querySelector('.cookie-consent');
          const code = document.querySelector('input[name=code]');
          const style = email ? getComputedStyle(email) : null;
          return {
            hasCookie: !!cookie,
            emailWidth: email ? email.getBoundingClientRect().width : 0,
            inputMinHeight: style ? parseFloat(style.minHeight) || parseFloat(style.height) : 0,
            providerMinHeight: provider ? parseFloat(getComputedStyle(provider).minHeight) : 0,
            hasCodeInput: !!code,
          };
        });
        console.log(JSON.stringify({ prefix, slug, tag, ...metrics }));
        await shot(page, path.join(out, `${prefix}-${slug}-${tag}.jpg`));
        await ctx.close();
      }
    }
  }
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
