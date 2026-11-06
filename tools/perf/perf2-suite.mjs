/**
 * Perf ronde 2 — median-of-3 lab metrics (Playwright + Performance API).
 * Usage:
 *   OUT=/cursor/stores/self/perf2/baseline.json \
 *   BASE=http://127.0.0.1:5201 \
 *   node tools/perf/perf2-suite.mjs
 *
 * Env: BASE, OUT, RUNS=3, VACANCY_PATH=/vacancies/..., ACC_BASE=https://acceptatie.lobsy.nl
 */
import { chromium, devices } from 'playwright';
import { mkdir, writeFile, readFile } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const accBase = (process.env.ACC_BASE || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const runs = Math.max(1, parseInt(process.env.RUNS || '3', 10));
const outFile = process.env.OUT || '/cursor/stores/self/perf2/metrics.json';
const vacancyPath = process.env.VACANCY_PATH || '';

const publicPaths = ['/', '/banenkaart', '/login', '/account-maken'];

function median(nums) {
  const s = [...nums].sort((a, b) => a - b);
  const m = Math.floor(s.length / 2);
  return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2;
}

async function dismissChrome(page) {
  const btn = page.locator('.cookie-consent [data-consent="analytics"], .cookie-consent .btn-compact--primary');
  if (await btn.count()) {
    try { await btn.first().click({ timeout: 2500 }); } catch { /* */ }
  }
}

async function measureNavigation(page, url, { mobile, cold }) {
  if (cold) {
    await page.context().clearCookies();
  }
  const t0 = Date.now();
  await page.goto(url, { waitUntil: 'commit', timeout: 120_000 });
  const metrics = await page.evaluate(async () => {
    const nav = performance.getEntriesByType('navigation')[0];
    const waitPaint = () => new Promise((resolve) => {
      if (performance.getEntriesByName('first-contentful-paint').length) {
        resolve();
        return;
      }
      const po = new PerformanceObserver((list) => {
        if (list.getEntriesByName('first-contentful-paint').length) {
          po.disconnect();
          resolve();
        }
      });
      try { po.observe({ type: 'paint', buffered: true }); } catch { resolve(); }
      setTimeout(resolve, 8000);
    });
    const waitLcp = () => new Promise((resolve) => {
      let lcp = 0;
      const po = new PerformanceObserver((list) => {
        for (const e of list.getEntries()) lcp = e.startTime;
      });
      try { po.observe({ type: 'largest-contentful-paint', buffered: true }); } catch { resolve(0); return; }
      setTimeout(() => { po.disconnect(); resolve(lcp); }, 8000);
    });
    const waitCircuit = () => new Promise((resolve) => {
      const start = performance.timeOrigin;
      const check = () => {
        if (document.documentElement.getAttribute('data-lobsy-circuit') === 'ready') {
          resolve(performance.now());
          return;
        }
        if (window.Blazor && window.Blazor._internal) {
          resolve(performance.now());
          return;
        }
        if (performance.now() > 15000) {
          resolve(null);
          return;
        }
        requestAnimationFrame(check);
      };
      check();
    });
    await waitPaint();
    const lcp = await waitLcp();
    const circuitMs = await waitCircuit();
    const resources = performance.getEntriesByType('resource');
    let transfer = 0;
    let reqCount = resources.length + (nav ? 1 : 0);
    for (const r of resources) {
      if (typeof r.transferSize === 'number') transfer += r.transferSize;
      if (nav && typeof nav.transferSize === 'number') transfer += nav.transferSize;
    }
    const fcp = performance.getEntriesByName('first-contentful-paint')[0]?.startTime ?? null;
    return {
      ttfb: nav ? nav.responseStart : null,
      fcp,
      lcp: lcp || null,
      circuitMs,
      domContentLoaded: nav ? nav.domContentLoadedEventEnd : null,
      load: nav ? nav.loadEventEnd : null,
      transfer,
      requests: reqCount,
    };
  });
  metrics.wallMs = Date.now() - t0;
  return metrics;
}

async function measureSpaClick(page, clickFn, contentSelector) {
  const reloads = { count: 0 };
  page.on('framenavigated', (frame) => {
    if (frame === page.mainFrame()) reloads.count += 1;
  });
  const t0 = Date.now();
  await clickFn();
  await page.waitForSelector(contentSelector, { timeout: 45_000 });
  await page.waitForFunction(
    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'
      || !!(window.Blazor && window.Blazor._internal),
    null,
    { timeout: 30_000 },
  ).catch(() => {});
  return { clickToContentMs: Date.now() - t0, fullReloads: Math.max(0, reloads.count - 1) };
}

async function login(page, baseUrl, email, password) {
  await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded', timeout: 90_000 });
  await page.locator('input[name="email"], input[type=email]').first.fill(email);
  await page.locator('input[name="password"], input[type=password"]').first.fill(password);
  await page.locator('button.login-submit, button[type=submit]').first.click();
  await page.waitForURL((u) => !u.pathname.endsWith('/login'), { timeout: 60_000 });
}

async function runProfile(browser, profileName, viewport, throttling, baseUrl, paths, opts) {
  const results = [];
  const context = await browser.newContext({
    ...viewport,
    serviceWorkers: opts.serviceWorkers || 'allow',
  });
  const cdp = await context.newCDPSession(await context.newPage());
  await cdp.send('Network.enable');
  if (throttling) {
    await cdp.send('Network.emulateNetworkConditions', {
      offline: false,
      downloadThroughput: (1.6 * 1024 * 1024) / 8,
      uploadThroughput: (750 * 1024) / 8,
      latency: 150,
    });
    await cdp.send('Emulation.setCPUThrottlingRate', { rate: 4 });
  }
  await cdp.detach();

  for (const path of paths) {
    const slug = path.replace(/\//g, '_') || 'home';
    for (const warm of ['cold', 'warm']) {
      const samples = [];
      for (let i = 0; i < runs; i++) {
        const page = await context.newPage();
        if (throttling) {
          const s = await context.newCDPSession(page);
          await s.send('Network.emulateNetworkConditions', {
            offline: false,
            downloadThroughput: (1.6 * 1024 * 1024) / 8,
            uploadThroughput: (750 * 1024) / 8,
            latency: 150,
          });
          await s.send('Emulation.setCPUThrottlingRate', { rate: 4 });
        }
        const url = `${baseUrl}${path}`;
        const m = await measureNavigation(page, url, { mobile: profileName === 'mobile', cold: warm === 'cold' });
        await dismissChrome(page);
        samples.push(m);
        await page.close();
      }
      const row = {
        base: baseUrl,
        profile: profileName,
        path,
        warm: warm === 'warm',
        ttfb: Math.round(median(samples.map((s) => s.ttfb || 0))),
        fcp: Math.round(median(samples.map((s) => s.fcp || 0))),
        lcp: Math.round(median(samples.map((s) => s.lcp || 0))),
        circuit: Math.round(median(samples.map((s) => s.circuitMs || 0))),
        transfer: Math.round(median(samples.map((s) => s.transfer || 0))),
        requests: Math.round(median(samples.map((s) => s.requests || 0))),
      };
      results.push(row);
      console.log(JSON.stringify(row));
    }
  }
  await context.close();
  return results;
}

async function resolveVacancyPath(baseUrl) {
  if (vacancyPath) return vacancyPath;
  try {
    const res = await fetch(`${baseUrl.replace(':5201', ':5200')}/api/vacancies/pins?take=1`);
    const data = await res.json();
    const id = data?.[0]?.id || data?.items?.[0]?.id;
    if (id) return `/vacancies/${id}`;
  } catch { /* */ }
  return '/banenkaart';
}

async function main() {
  await mkdir(path.dirname(outFile), { recursive: true });
  const vac = await resolveVacancyPath(base);
  const paths = [...publicPaths, vac];

  await chromium.launch().then((b) => b.close());
  const browser = await chromium.launch({ headless: true });

  const all = [];
  all.push(...await runProfile(browser, 'desktop', { viewport: { width: 1440, height: 900 } }, false, base, paths, {}));
  all.push(...await runProfile(browser, 'mobile', devices['Pixel 5'], true, base, paths, {}));
  all.push(...await runProfile(browser, 'desktop', { viewport: { width: 1440, height: 900 } }, false, accBase, paths, {}));
  all.push(...await runProfile(browser, 'mobile', devices['Pixel 5'], true, accBase, paths, {}));

  await browser.close();
  await writeFile(outFile, JSON.stringify({ capturedAt: new Date().toISOString(), vacancyPath: vac, rows: all }, null, 2));
  console.log('wrote', outFile);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
