import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const label = process.env.LABEL || 'run';
const outRoot = process.env.OUT || '/cursor/stores/self/perf';
const runs = Math.max(1, parseInt(process.env.RUNS || '3', 10));

const viewports = [
  { w: 1440, h: 900, tag: 'desktop' },
  { w: 390, h: 844, tag: 'mobile' },
];

function median(nums) {
  const s = [...nums].sort((a, b) => a - b);
  const m = Math.floor(s.length / 2);
  return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2;
}

async function measureViewportOnce(browser, vp) {
  const context = await browser.newContext({ viewport: { width: vp.w, height: vp.h } });
  const page = await context.newPage();
  const t0 = Date.now();
  await page.goto(`${base}/banenkaart`, { waitUntil: 'domcontentloaded', timeout: 120_000 });

  const accept = page.locator('.cookie-consent [data-consent="analytics"], .cookie-consent .btn-compact--primary');
  if (await accept.count()) {
    try { await accept.first().click({ timeout: 3000 }); } catch { /* */ }
  }
  const later = page.getByRole('link', { name: 'Later' });
  if (await later.count()) {
    try { await later.first().click({ timeout: 2000 }); } catch { /* */ }
  }

  await page.locator('#job-map canvas').first().waitFor({ state: 'attached', timeout: 120_000 });
  const markersMs = Date.now() - t0;
  const pinCount = await page.evaluate(() =>
    window.jobMap?.__testGetPinCount?.() ?? -1,
  );

  const mobileBtn = page.locator('.kb-filter-bar [data-testid="kb-filters-button"]');
  const desktopBtn = page.locator('.discovery-toolbar--desktop [data-testid="kb-filters-button"]');
  const btn = (await mobileBtn.count()) && (await mobileBtn.first().isVisible())
    ? mobileBtn.first()
    : desktopBtn.first();
  if (await btn.count()) {
    try {
      await btn.click({ timeout: 8000 });
      await page.waitForTimeout(400);
    } catch { /* */ }
  }

  const shot = path.join(outRoot, 'after', `banenkaart-${vp.tag}-filters-${label}.png`);
  await mkdir(path.dirname(shot), { recursive: true });
  await page.screenshot({ path: shot, fullPage: false });

  await context.close();
  return { markersMs, pinCount, screenshot: shot };
}

const browser = await chromium.launch({ headless: true });
const results = [];
for (const vp of viewports) {
  const samples = [];
  let lastShot = '';
  let pinCount = -1;
  for (let i = 0; i < runs; i++) {
    const r = await measureViewportOnce(browser, vp);
    samples.push(r.markersMs);
    lastShot = r.screenshot;
    pinCount = r.pinCount;
  }
  results.push({
    ...vp,
    markersMsMedian: median(samples),
    markersMsRuns: samples,
    pinCount,
    screenshot: lastShot,
  });
}
await browser.close();

const summary = { base, label, runs, capturedAtUtc: new Date().toISOString(), results };
await writeFile(
  path.join(outRoot, `banenkaart-markers-${label}.json`),
  JSON.stringify(summary, null, 2),
);
console.log(JSON.stringify(summary, null, 2));
