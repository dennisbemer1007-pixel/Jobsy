#!/usr/bin/env node
/**
 * Banenkaart lagen — click + smoothness checklist (desktop 1280 + mobile 390).
 * Writes JSON + markdown to BANENKAART_LAGEN_OUT.
 */
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = process.env.JOBSY_REPO_ROOT ?? path.resolve(__dirname, '../..');
const outDir = process.env.BANENKAART_LAGEN_OUT ?? '/opt/cursor/artifacts/banenkaart-lagen-capture';
const baseUrl = (process.env.BANENKAART_LAGEN_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const banenPath =
  '/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=20&transport=Fiets&minHours=32&maxHours=40';
const vacancyWithPhoto = 'b2000001-0000-0000-0000-000000000001';

async function routeLocalAssets(context) {
  const files = [
    ['jobMap.min.js', path.join(repoRoot, 'Jobsy.Web/wwwroot/js/jobMap.js')],
    ['jobMap.js', path.join(repoRoot, 'Jobsy.Web/wwwroot/js/jobMap.js')],
    ['banenkaart-lagen.css', path.join(repoRoot, 'Jobsy.Web/wwwroot/css/features/banenkaart-lagen.css')],
    ['banenkaart-catalog.js', path.join(repoRoot, 'Jobsy.Web/wwwroot/js/banenkaart-catalog.js')],
    ['banenkaart-map-layers.js', path.join(repoRoot, 'Jobsy.Web/wwwroot/js/banenkaart-map-layers.js')],
    ['banenkaart-map-icons.js', path.join(repoRoot, 'Jobsy.Web/wwwroot/js/banenkaart-map-icons.js')],
    ['jobsyMapLibre.min.js', path.join(repoRoot, 'Jobsy.Web/wwwroot/js/jobsyMapLibre.min.js')],
  ];
  for (const [name, p] of files) {
    if (!fs.existsSync(p)) continue;
    const body = fs.readFileSync(p, 'utf8');
    const type = name.endsWith('.css') ? 'text/css; charset=utf-8' : 'application/javascript; charset=utf-8';
    await context.route(`**/${name}**`, (route) =>
      route.fulfill({ status: 200, contentType: type, body }),
    );
  }
}

async function dismissCookies(page) {
  const btn = page.locator('.cookie-consent button[data-consent="analytics"], .cookie-consent .btn-compact').first();
  if (await btn.count()) await btn.click({ force: true }).catch(() => {});
}

async function gotoBanenkaart(page) {
  await page.goto(`${baseUrl}/__test/sign-in?role=candidate&return=${encodeURIComponent(banenPath)}`, {
    waitUntil: 'load',
  });
  await page.goto(`${baseUrl}${banenPath}`, { waitUntil: 'load' });
  await page.waitForFunction(
    () => document.querySelector('[data-banenkaart-ssr="1"]')?.dataset?.banenkaartSsrBound === '1',
    null,
    { timeout: 90_000 },
  );
  const later = page.locator('[data-testid=kb-start-later]');
  if ((await later.count()) > 0) await later.first().click();
  await dismissCookies(page);
}

async function waitMapReady(page) {
  await page.waitForFunction(
    () => {
      const canvas = document.querySelector('#job-map .maplibregl-canvas');
      if (!canvas) return false;
      const rect = canvas.getBoundingClientRect();
      if (rect.width < 32 || rect.height < 32) return false;
      const map = window.jobMap?.__testGetMap?.();
      return !!(map && typeof map.getZoom === 'function');
    },
    null,
    { timeout: 90_000 },
  );
  await page.evaluate(() => {
    window.jobMap?.invalidate?.();
    window.jobMap?.__testGetMap?.()?.resize?.();
  });
}

function attachObservers(page, bag) {
  bag.consoleErrors = [];
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      bag.consoleErrors.push(msg.text().slice(0, 500));
    }
  });
  page.on('pageerror', (err) => {
    bag.consoleErrors.push(String(err.message || err).slice(0, 500));
  });
}

async function resetLongTasks(page) {
  await page.evaluate(() => {
    window.__jobsyLongTasks = [];
  });
}

async function readLongTasks(page) {
  return page.evaluate(() => (window.__jobsyLongTasks || []).filter((t) => t.duration > 200));
}

async function runViewportChecklist(browser, width, height, mobile, label) {
  const bag = { viewport: label, steps: [], consoleErrors: [], longTasksPanZoom: [] };
  const context = await browser.newContext({
    viewport: { width, height },
    hasTouch: mobile,
    isMobile: mobile,
    serviceWorkers: 'block',
  });
  await routeLocalAssets(context);
  const page = await context.newPage();
  attachObservers(page, bag);

  await page.addInitScript(() => {
    window.__jobsyLongTasks = [];
    try {
      new PerformanceObserver((list) => {
        for (const e of list.getEntries()) {
          if (e.duration > 200) {
            window.__jobsyLongTasks.push({ duration: Math.round(e.duration), name: e.name || 'longtask' });
          }
        }
      }).observe({ entryTypes: ['longtask'] });
    } catch (_) {}
  });

  const step = async (name, fn) => {
    try {
      await fn();
      bag.steps.push({ name, ok: true });
    } catch (e) {
      bag.steps.push({ name, ok: false, error: String(e.message || e).slice(0, 300) });
    }
  };

  await gotoBanenkaart(page);
  await waitMapReady(page);

  await step('Filters open', async () => {
    const btn = page.locator('[data-testid=kb-filters-button], [data-testid=kb-filter-drawer-toggle]').first();
    await btn.click({ timeout: 15_000 });
    await page.locator('[data-testid=kb-filter-sheet], [data-testid=kb-filter-backdrop]').first().waitFor({
      state: 'visible',
      timeout: 10_000,
    });
    const close = page.locator('[data-testid=kb-filter-close], [data-testid=kb-filter-cancel]').first();
    if (await close.count()) await close.click();
  });

  await step('Match button', async () => {
    const m = page.locator('[data-testid=kb-match-button]').first();
    await m.click({ timeout: 10_000 });
    await page.waitForTimeout(400);
  });

  await step('Kaart view', async () => {
    await page.locator('[data-testid=kb-view-kaart]').first().click({ timeout: 10_000 });
    await page.waitForFunction(() => document.querySelector('.jobsy-discovery--lagen')?.classList.contains('show-map'));
  });

  await step('Lijst view', async () => {
    await page.locator('[data-testid=kb-view-lijst]').first().click({ timeout: 10_000 });
    await page.waitForFunction(() => document.querySelector('.jobsy-discovery--lagen')?.classList.contains('show-list'));
  });

  await step('Kaart view (return)', async () => {
    await page.locator('[data-testid=kb-view-kaart]').first().click({ timeout: 10_000 });
    await waitMapReady(page);
  });

  if (mobile) {
    await step('Uitleg kaart', async () => {
      await page.locator('[data-testid=kb-map-legbtn]').click({ timeout: 10_000 });
      await page.waitForFunction(() => document.body.classList.contains('s-legend'), null, { timeout: 15_000 });
      await page.locator('[data-testid=kb-map-legend-done], [data-testid=kb-map-legend-close]').first().click();
      await page.waitForFunction(() => !document.body.classList.contains('s-legend'), null, { timeout: 10_000 });
    });
  }

  await resetLongTasks(page);
  await step('Map zoom in/out', async () => {
    const zin = page.locator('.maplibregl-ctrl-zoom-in').first();
    const zout = page.locator('.maplibregl-ctrl-zoom-out').first();
    await zin.click({ timeout: 10_000 });
    await page.waitForTimeout(350);
    await zout.click({ timeout: 10_000 });
    await page.waitForTimeout(350);
  });
  bag.longTasksPanZoom = await readLongTasks(page);

  await resetLongTasks(page);
  await step('Map pan (drag)', async () => {
    const canvas = page.locator('#job-map .maplibregl-canvas').first();
    const box = await canvas.boundingBox();
    if (!box) throw new Error('no canvas box');
    await page.mouse.move(box.x + box.width * 0.5, box.y + box.height * 0.45);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width * 0.42, box.y + box.height * 0.5, { steps: 8 });
    await page.mouse.up();
    await page.waitForTimeout(400);
  });
  const panTasks = await readLongTasks(page);
  bag.longTasksPanZoom = bag.longTasksPanZoom.concat(panTasks);

  await step('Pin popup open', async () => {
    const ok = await page.evaluate((vid) => window.jobMap?.__testOpenVacancyById?.(vid), vacancyWithPhoto);
    if (!ok) throw new Error('__testOpenVacancyById failed');
    const pop = page.locator(
      '.maplibregl-popup.kb-popup-lagen-pop:visible, [data-lagen-fcw]:visible, .fcw.kb-lagen-fcw:visible',
    ).first();
    await pop.waitFor({ state: 'visible', timeout: 15_000 });
    if (!mobile) {
      const scrimVisible = await page.evaluate(
        () =>
          !!document.querySelector('body.s-legend .scrim.kb-map-legend-scrim') ||
          (() => {
            const s = document.querySelector('.scrim.kb-map-legend-scrim');
            return s && !s.hidden && getComputedStyle(s).display !== 'none' && !document.body.classList.contains('s-legend');
          })(),
      );
      if (scrimVisible) throw new Error('page dimming scrim visible without legend');
    }
  });

  await step('€ wage control', async () => {
    const btn = page.locator('[data-lagen-wage]').first();
    if (mobile) {
      await btn.click({ force: true });
    } else {
      await page.evaluate(() => {
        const btn = document.querySelector('[data-lagen-wage]');
        btn?.dispatchEvent(new MouseEvent('mouseenter', { bubbles: true }));
      });
    }
    await page.waitForFunction(
      () => {
        const tip = document.querySelector('.fcw.kb-lagen-fcw .tip, [data-lagen-fcw] .tip');
        return tip && !tip.hasAttribute('hidden');
      },
      null,
      { timeout: 8000 },
    );
  });

  await step('Heart (save)', async () => {
    const heart = page.locator('[data-lagen-fcw] .hrt, .fcw.kb-lagen-fcw .hrt').first();
    await heart.click({ timeout: 10_000 });
    await page.waitForTimeout(200);
  });

  await step('Bekijk vacature + back', async () => {
    const link = page.locator('[data-lagen-fcw] .fgo, .fcw.kb-lagen-fcw .fgo').first();
    await link.click({ timeout: 10_000 });
    await page.waitForLoadState('load', { timeout: 20_000 });
    await page.goBack({ waitUntil: 'load', timeout: 20_000 });
    await waitMapReady(page);
  });

  await step('Cluster open + paging', async () => {
    await page.evaluate((vid) => window.jobMap?.__testOpenVacancyById?.(vid), vacancyWithPhoto).catch(() => {});
    await page.locator('[data-lagen-close]').first().click({ timeout: 5000 }).catch(() => {});
    await page.waitForTimeout(300);
    const opened = await page.evaluate(async () => window.jobMap?.debugOpenLargestCluster?.());
    if (!opened) throw new Error('debugOpenLargestCluster failed');
    await page.locator('[data-lagen-fcw]:visible, .fcw.kb-lagen-fcw:visible').first().waitFor({ timeout: 15_000 });
    const next = page.locator('[data-lagen-next]').first();
    if (await next.count()) {
      await next.click();
      await page.waitForTimeout(350);
      const prev = page.locator('[data-lagen-prev]').first();
      if (await prev.count()) await prev.click();
    }
  });

  await step('Popup close (X)', async () => {
    const x = page.locator('[data-lagen-close]').first();
    await x.click({ timeout: 10_000 });
    await page.waitForFunction(
      () => !document.querySelector('[data-lagen-fcw]:visible, .fcw.kb-lagen-fcw:visible, .maplibregl-popup.kb-popup-lagen-pop'),
      null,
      { timeout: 10_000 },
    );
  });

  if (mobile) {
    await step('Bottom nav items', async () => {
      const items = page.locator('.bottom-nav .bottom-nav__item, .bottom-nav a.bottom-nav__item');
      const n = await items.count();
      if (n < 3) throw new Error(`expected bottom nav links, got ${n}`);
      for (let i = 0; i < n; i++) {
        await gotoBanenkaart(page);
        await waitMapReady(page);
        await items.nth(i).click({ timeout: 10_000 });
        await page.waitForLoadState('load');
      }
    });
  }

  bag.pass = bag.steps.every((s) => s.ok) && bag.consoleErrors.length === 0 && bag.longTasksPanZoom.length === 0;
  await context.close();
  return bag;
}

function toMarkdown(reports) {
  const lines = ['## Click checklist (Playwright)', ''];
  for (const r of reports) {
    lines.push(`### ${r.viewport}`);
    lines.push(`- **Overall:** ${r.pass ? 'PASS' : 'FAIL'}`);
    lines.push('- **Steps:**');
    for (const s of r.steps) {
      lines.push(`  - ${s.ok ? '✓' : '✗'} ${s.name}${s.error ? ` — ${s.error}` : ''}`);
    }
    if (r.consoleErrors.length) {
      lines.push('- **Console errors:**');
      for (const e of r.consoleErrors.slice(0, 8)) lines.push(`  - \`${e}\``);
    } else {
      lines.push('- **Console errors:** none');
    }
    if (r.longTasksPanZoom.length) {
      lines.push('- **Long tasks >200ms (pan/zoom):**');
      for (const t of r.longTasksPanZoom.slice(0, 8)) lines.push(`  - ${t.duration}ms ${t.name}`);
    } else {
      lines.push('- **Long tasks >200ms (pan/zoom):** none');
    }
    lines.push('');
  }
  return lines.join('\n');
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true, args: ['--disable-dev-shm-usage', '--no-sandbox'] });
  try {
    const desktop = await runViewportChecklist(browser, 1280, 800, false, 'Desktop 1280×800');
    const mobile = await runViewportChecklist(browser, 390, 844, true, 'Mobile 390×844');
    const reports = [desktop, mobile];
    const jsonPath = path.join(outDir, 'click-checklist.json');
    fs.writeFileSync(jsonPath, JSON.stringify({ baseUrl, at: new Date().toISOString(), reports }, null, 2));
    fs.writeFileSync(path.join(outDir, 'click-checklist.md'), toMarkdown(reports));
    const allPass = reports.every((r) => r.pass);
    console.log(allPass ? 'Click checklist: PASS' : 'Click checklist: FAIL', jsonPath);
    if (!allPass) process.exit(1);
  } finally {
    await browser.close();
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
