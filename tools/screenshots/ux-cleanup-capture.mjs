/**
 * UX audit before/after capture. Requires a running dev/acc host (never production).
 *
 *   BEFORE_BASE=http://127.0.0.1:5201 OUT=/cursor/stores/self/ux-cleanup/before node tools/screenshots/ux-cleanup-capture.mjs
 *   AFTER_BASE=http://127.0.0.1:5201 OUT=/cursor/stores/self/ux-cleanup/after node tools/screenshots/ux-cleanup-capture.mjs
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || process.env.AFTER_BASE || process.env.BEFORE_BASE || '').replace(/\/$/, '');
const outRoot = process.env.OUT || '/cursor/stores/self/ux-cleanup/after';

const variantQuery = process.env.LANDING_VARIANT === 'on' ? '?_variant=on' : '';

const pages = [
  ['westland', `/westland${variantQuery}`],
  ['lancering', `/lancering${variantQuery}`],
  ['melden', '/melden?type=vacancy&id=00000000-0000-0000-0000-000000000001'],
  ['contact', '/contact'],
  ['info-onderzoek', '/info/onderzoek'],
  ['privacy', '/privacy'],
  ['login', '/login'],
  ['account-maken', '/account-maken'],
  ['banenkaart', `/banenkaart${variantQuery}`],
  ['partner', `/partner${variantQuery}`],
  ['register', `/register${variantQuery}`],
  ['banenkaart-filter-drawer', `/banenkaart${variantQuery}${variantQuery ? '&' : '?'}filters=1`],
  ['banenkaart-mobile-list', `/banenkaart${variantQuery}`],
];

const viewports = [
  ['desktop', 1280, 800],
  ['mobile', 390, 844],
];

if (!base) {
  console.error('Set BASE, AFTER_BASE, or BEFORE_BASE to a reachable host (not lobsy.nl).');
  process.exit(1);
}

const consentInitScript = `
  try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
  document.documentElement.classList.add('cookie-consent-known');
`;

const browser = await chromium.launch({ headless: true });
for (const [name, route] of pages) {
  for (const [tag, w, h] of viewports) {
    const ctx = await browser.newContext({ viewport: { width: w, height: h } });
    await ctx.addInitScript(consentInitScript);
    const page = await ctx.newPage();
    await page.goto(`${base}${route}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
    if (name === 'banenkaart-filter-drawer') {
      const btn = page.locator('[data-testid="kb-filters-button"]').first();
      if (await btn.count()) await btn.click({ timeout: 8000 }).catch(() => {});
      await page.waitForTimeout(500);
    }
    if (name === 'banenkaart-mobile-list' && tag === 'mobile') {
      const list = page.locator('[data-testid="kb-view-lijst"]').first();
      if (await list.count()) await list.click({ timeout: 8000 }).catch(() => {});
      await page.waitForTimeout(500);
    }
    if (name.startsWith('banenkaart')) {
      await page.waitForTimeout(name === 'banenkaart' ? 1500 : 800);
    }
    const file = path.join(outRoot, `${name}-${tag}.png`);
    await mkdir(path.dirname(file), { recursive: true });
    try {
      await page.screenshot({ path: file, fullPage: false, timeout: 60_000 });
    } catch (err) {
      await page.waitForTimeout(2000);
      await page.screenshot({ path: file, fullPage: false, timeout: 60_000 });
    }
    console.log('wrote', file);
    await ctx.close();
  }
}
await browser.close();
