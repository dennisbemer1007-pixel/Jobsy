/**
 * Cookie banner + static auth pages at 390 and 1440 (PR #710 review).
 *   BASE=http://127.0.0.1:5201 OUT=/cursor/stores/self/perf2/screenshots \
 *   node tools/perf/perf2-static-auth-screenshots.mjs
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const out = process.env.OUT || '/cursor/stores/self/perf2/screenshots';

const paths = [
  { slug: 'login', path: '/login' },
  { slug: 'account-maken', path: '/account-maken' },
  { slug: 'account-maken-code', path: '/account-maken/code?email=review%40example.test' },
];

async function shot(page, file) {
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, type: 'jpeg', quality: 88, fullPage: false });
  console.log('wrote', file);
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  for (const [w, h, tag] of [
    [390, 844, 'mobile'],
    [1440, 900, 'desktop'],
  ]) {
    const ctx = await browser.newContext({ viewport: { width: w, height: h } });
    for (const { slug, path: p } of paths) {
      const page = await ctx.newPage();
      await page.goto(`${base}${p}`, { waitUntil: 'domcontentloaded', timeout: 60_000 });
      const blazor = await page.evaluate(
        () => !!document.querySelector('script[src*="blazor.web.js"]'),
      );
      const titleSize = await page.evaluate(() => {
        const t = document.querySelector('.cookie-consent__title');
        if (!t) return null;
        return parseFloat(getComputedStyle(t).fontSize);
      });
      const cookieCss = await page.evaluate(
        () => [...document.querySelectorAll('link[rel=stylesheet]')].some((l) =>
          String(l.href).includes('cookie-consent.css')),
      );
      console.log(JSON.stringify({ slug, tag, blazor, cookieCss, titleSize }));
      await shot(page, path.join(out, `cookie-banner-${slug}-${tag}-visible.jpg`));
      await page.locator('.cookie-consent [data-consent=necessary]').first().click({ timeout: 8000 }).catch(() => {});
      await page.waitForSelector('.cookie-consent', { state: 'detached', timeout: 8000 }).catch(() => {});
      await shot(page, path.join(out, `cookie-banner-${slug}-${tag}-dismissed.jpg`));
      await page.close();
    }
    await ctx.close();
  }
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
