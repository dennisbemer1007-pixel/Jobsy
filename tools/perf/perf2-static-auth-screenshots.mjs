/**
 * Cookie banner + static auth pages at 390 and 1440 (PR #710 review).
 * Clears consent before each page; code step via real email POST when JOBSY_CAPTURE_EMAIL=1.
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const out = process.env.OUT || '/cursor/stores/self/perf2/screenshots';
const captureEmail = process.env.JOBSY_CAPTURE_EMAIL === '1';

async function shot(page, file) {
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, type: 'jpeg', quality: 88, fullPage: false });
  console.log('wrote', file);
}

async function gotoSlug(page, slug) {
  if (slug === 'login') {
    await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 60_000 });
    return;
  }
  if (slug === 'account-maken') {
    await page.goto(`${base}/account-maken`, { waitUntil: 'domcontentloaded', timeout: 60_000 });
    return;
  }
  if (slug === 'account-maken-code') {
    if (!captureEmail) {
      throw new Error('Set JOBSY_CAPTURE_EMAIL=1 for account-maken-code captures.');
    }
    const email = `perf2.banner.${Date.now()}@example.test`;
    await page.goto(`${base}/account-maken`, { waitUntil: 'domcontentloaded', timeout: 60_000 });
    await page.locator('input[name=email]').fill(email);
    await page.locator('button.au-submit').first().click();
    await page.waitForURL('**/account-maken/code**', { timeout: 90_000 });
  }
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const slugs = ['login', 'account-maken', 'account-maken-code'];

  for (const [w, h, tag] of [
    [390, 844, 'mobile'],
    [1440, 900, 'desktop'],
  ]) {
    for (const slug of slugs) {
      const ctx = await browser.newContext({ viewport: { width: w, height: h } });
      await ctx.addInitScript(() => {
        try {
          localStorage.removeItem('Jobsy.CookieConsent');
        } catch (e) {}
        document.documentElement.classList.remove('cookie-consent-known');
      });
      const page = await ctx.newPage();
      await gotoSlug(page, slug);
      const blazor = await page.evaluate(
        () => !!document.querySelector('script[src*="blazor.web.js"]'),
      );
      const titleSize = await page.evaluate(() => {
        const t = document.querySelector('.cookie-consent__title');
        if (!t) return null;
        return parseFloat(getComputedStyle(t).fontSize);
      });
      const hasBanner = await page.locator('.cookie-consent').count();
      console.log(JSON.stringify({ slug, tag, blazor, hasBanner, titleSize }));
      await shot(page, path.join(out, `cookie-banner-${slug}-${tag}-visible.jpg`));
      await page.locator('.cookie-consent [data-consent=necessary]').first().click({ timeout: 8000 }).catch(() => {});
      await page.waitForSelector('.cookie-consent', { state: 'detached', timeout: 8000 }).catch(() => {});
      await shot(page, path.join(out, `cookie-banner-${slug}-${tag}-dismissed.jpg`));
      await ctx.close();
    }
  }
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
