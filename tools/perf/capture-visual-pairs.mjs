import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const label = process.env.LABEL || 'run';
const outRoot = process.env.OUT || '/cursor/stores/self/perf/visual';

async function dismissChrome(page) {
  const accept = page.locator('.cookie-consent [data-consent="analytics"], .cookie-consent .btn-compact--primary');
  if (await accept.count()) {
    try { await accept.first().click({ timeout: 3000 }); } catch { /* */ }
  }
  const later = page.getByRole('link', { name: 'Later' });
  if (await later.count()) {
    try { await later.first().click({ timeout: 2000 }); } catch { /* */ }
  }
}

async function gotoBanenkaart(page) {
  await page.goto(`${base}/banenkaart`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await dismissChrome(page);
  await page.locator('#job-map canvas').first().waitFor({ state: 'attached', timeout: 120_000 }).catch(() => {});
  await page.waitForTimeout(800);
}

async function setFiltersOpen(page, open) {
  const isOpen = await page.locator('.jobsy-discovery.filters-open').count();
  if (open && !isOpen) {
    const mobileBtn = page.locator('.kb-filter-bar [data-testid="kb-filters-button"]');
    const desktopBtn = page.locator('.discovery-toolbar--desktop [data-testid="kb-filters-button"]');
    const btn = (await mobileBtn.count()) && (await mobileBtn.first().isVisible())
      ? mobileBtn.first()
      : desktopBtn.first();
    await btn.click({ timeout: 10_000 });
    await page.waitForSelector('.jobsy-discovery.filters-open', { timeout: 10_000 });
    await page.waitForTimeout(400);
  } else if (!open && isOpen) {
    const close = page.locator('.filter-sheet__close, [data-testid="kb-filter-close"]').first();
    if (await close.count()) {
      await close.click({ timeout: 8000 });
    } else {
      await page.locator('.filter-sheet-backdrop').first().click({ timeout: 8000 }).catch(() => {});
    }
    await page.waitForTimeout(400);
  }
}

async function shot(page, file) {
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, fullPage: false });
  console.log('wrote', file);
}

async function banenkaartStates(browser, dir) {
  // Desktop 1280x800
  {
    const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 } });
    const page = await ctx.newPage();
    await gotoBanenkaart(page);
    await setFiltersOpen(page, false);
    await shot(page, path.join(dir, 'banenkaart-desktop-drawer-closed.png'));
    await setFiltersOpen(page, true);
    await shot(page, path.join(dir, 'banenkaart-desktop-drawer-open.png'));
    await ctx.close();
  }
  // Mobile 390x844
  {
    const ctx = await browser.newContext({ viewport: { width: 390, height: 844 } });
    const page = await ctx.newPage();
    await gotoBanenkaart(page);
    await setFiltersOpen(page, false);
    await shot(page, path.join(dir, 'banenkaart-mobile-drawer-closed.png'));
    await setFiltersOpen(page, true);
    await shot(page, path.join(dir, 'banenkaart-mobile-drawer-open.png'));
    const listBtn = page.locator('[data-testid="kb-view-toggle"]').getByRole('button', { name: /Lijst|List/i }).first();
    if (await listBtn.count()) {
      await listBtn.click({ timeout: 8000 });
      await page.waitForSelector('.jobsy-discovery.show-list', { timeout: 15_000 });
      await page.waitForTimeout(500);
    }
    await shot(page, path.join(dir, 'banenkaart-mobile-list.png'));
    await ctx.close();
  }
}

async function staticPages(browser, dir) {
  const pages = [
    { path: '/', name: 'home', w: 390, h: 844 },
    { path: '/login', name: 'login', w: 390, h: 844 },
    { path: '/candidate/liked', name: 'candidate-liked-gate', w: 390, h: 844 },
  ];
  for (const p of pages) {
    const ctx = await browser.newContext({ viewport: { width: p.w, height: p.h } });
    const page = await ctx.newPage();
    await page.goto(base + p.path, { waitUntil: 'domcontentloaded', timeout: 120_000 });
    await dismissChrome(page);
    await page.waitForTimeout(1200);
    await shot(page, path.join(dir, `${p.name}-mobile.png`));
    await ctx.close();
  }
}

const dir = path.join(outRoot, label);
await mkdir(dir, { recursive: true });
const browser = await chromium.launch({ headless: true });
await banenkaartStates(browser, dir);
await staticPages(browser, dir);
await browser.close();
console.log('done', dir);
