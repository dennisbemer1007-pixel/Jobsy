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

const pages = [
  ['westland', '/westland'],
  ['lancering', '/lancering'],
  ['melden', '/melden?type=vacancy&id=00000000-0000-0000-0000-000000000001'],
  ['contact', '/contact'],
  ['info-onderzoek', '/info/onderzoek'],
  ['privacy', '/privacy'],
  ['login', '/login'],
  ['account-maken', '/account-maken'],
  ['banenkaart', '/banenkaart'],
  ['partner', '/partner'],
  ['register', '/register'],
  ['banenkaart-filter-drawer', '/banenkaart?filters=1'],
  ['banenkaart-mobile-list', '/banenkaart'],
];

const viewports = [
  ['desktop', 1280, 800],
  ['mobile', 390, 844],
];

if (!base) {
  console.error('Set BASE, AFTER_BASE, or BEFORE_BASE to a reachable host (not lobsy.nl).');
  process.exit(1);
}

async function dismiss(page) {
  const accept = page.locator('.cookie-consent [data-consent="analytics"], .cookie-consent .btn-compact--primary');
  if (await accept.count()) {
    try { await accept.first().click({ timeout: 3000 }); } catch { /* */ }
  }
}

const browser = await chromium.launch({ headless: true });
for (const [name, route] of pages) {
  for (const [tag, w, h] of viewports) {
    const ctx = await browser.newContext({ viewport: { width: w, height: h } });
    const page = await ctx.newPage();
    await page.goto(`${base}${route}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
    await dismiss(page);
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
    const file = path.join(outRoot, `${name}-${tag}.png`);
    await mkdir(path.dirname(file), { recursive: true });
    await page.screenshot({ path: file, fullPage: false });
    console.log('wrote', file);
    await ctx.close();
  }
}
await browser.close();
