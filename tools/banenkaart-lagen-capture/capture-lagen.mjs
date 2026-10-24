#!/usr/bin/env node
/**
 * Standalone banenkaart lagen captures (no dotnet test). Requires host + mockup refs.
 */
import { chromium } from 'playwright';
import { spawn, execFileSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = process.env.JOBSY_REPO_ROOT ?? path.resolve(__dirname, '../..');
const outDir = process.env.BANENKAART_LAGEN_OUT ?? '/opt/cursor/artifacts/banenkaart-lagen-capture';
const mockRoot =
  process.env.BANENKAART_MOCKUP_ROOT ??
  path.join(outDir, 'mockup-ref/docs/mockups/kandidaat/banenkaart/lagen');
const baseUrl = (process.env.BANENKAART_LAGEN_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const banenPath =
  '/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=20&transport=Fiets&minHours=32&maxHours=40';
const vacancyWithPhoto = 'b2000001-0000-0000-0000-000000000001';
const capturesDir = path.join(outDir, 'captures');
const sideBySidePy = path.join(__dirname, 'side_by_side.py');

const DEADLINE_MS = Number(process.env.BANENKAART_CAPTURE_DEADLINE_MS ?? 9 * 60 * 1000);
const deadline = Date.now() + DEADLINE_MS;

function assertTime() {
  if (Date.now() > deadline) throw new Error('Capture deadline exceeded (10 min budget)');
}

function sideBySide(mockRel, appFile, outName) {
  const mock = path.join(mockRoot, mockRel);
  const out = path.join(outDir, outName);
  execFileSync('python3', [sideBySidePy, mock, appFile, out], { stdio: 'inherit' });
}

async function routeLocalAssets(context) {
  const files = [
    ['jobMap.min.js', path.join(repoRoot, 'Jobsy.Web/wwwroot/js/jobMap.min.js')],
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
  if (await btn.count()) {
    await btn.click({ force: true }).catch(() => {});
  }
}

async function gotoBanenkaart(page) {
  assertTime();
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
  await page.evaluate(async () => {
    try {
      if (window.caches?.keys) {
        const keys = await caches.keys();
        await Promise.all(keys.map((k) => caches.delete(k)));
      }
    } catch (_) {}
  });
  await page.reload({ waitUntil: 'load' });
  await page.waitForFunction(
    () => document.querySelector('[data-banenkaart-ssr="1"]')?.dataset?.banenkaartSsrBound === '1',
    null,
    { timeout: 90_000 },
  );
  if ((await later.count()) > 0) await later.first().click();
  await dismissCookies(page);
}

async function waitReady(page) {
  assertTime();
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
  await page.waitForTimeout(400);
  try {
    await page.waitForFunction(
      () => {
        const n = window.jobMap?.__testGetRenderedPinCount?.();
        return typeof n === 'number' && n >= 3;
      },
      null,
      { timeout: 60_000 },
    );
  } catch {
    /* pins may still open popup via list id */
  }
}

async function waitForClusterLayer(page) {
  for (let z = 0; z < 4; z++) {
    const ok = await page.evaluate(() => {
      const map = window.jobMap?.__testGetMap?.();
      if (!map) return false;
      const layers = ['jobsy-pins-clusters-hit', 'jobsy-pins-clusters'];
      for (const layer of layers) {
        if (!map.getLayer(layer)) continue;
        const feats = map.queryRenderedFeatures({ layers: [layer] }) || [];
        if (feats.length) return true;
      }
      return false;
    });
    if (ok) return;
    await page.evaluate(() => {
      const map = window.jobMap?.__testGetMap?.();
      if (map && typeof map.setZoom === 'function') {
        map.setZoom(Math.max(9, map.getZoom() - 1.5));
      }
    });
    await page.waitForTimeout(800);
  }
  throw new Error('No cluster features on map after zoom-out attempts');
}

async function openVacancyById(page, id) {
  await page.evaluate((vid) => window.jobMap?.__testOpenVacancyById?.(vid), id);
  await page.locator('.maplibregl-popup:visible, .fcwrap.kb-lagen-fcwrap:visible, .fcw.kb-lagen-fcw:visible').first().waitFor({
    state: 'visible',
    timeout: 15_000,
  });
}

async function measurePhotoMs(page) {
  const t0 = Date.now();
  await page.waitForFunction(
    () => {
      const img = document.querySelector('[data-lagen-photo-slot] img');
      return img && img.complete && img.naturalWidth > 0;
    },
    null,
    { timeout: 20_000 },
  );
  const client = await page.evaluate(() => window.jobMap?.__testGetLastPopupPhotoMetrics?.());
  return {
    clickToPaintMs: Date.now() - t0,
    fetchMs: client?.ms ?? null,
    bytes: client?.bytes ?? null,
    kb: client?.kb ?? null,
    variant: client?.variant ?? null,
  };
}

async function openDesktopWageTooltip(page) {
  await page.evaluate(() => {
    const fcw = document.querySelector('[data-lagen-fcw], .fcw.kb-lagen-fcw');
    if (!fcw) return;
    fcw.classList.add('lagen-wage-open');
    const btn = fcw.querySelector('[data-lagen-wage]');
    const tip = fcw.querySelector('.tip');
    if (btn) {
      btn.classList.add('on');
      btn.setAttribute('aria-expanded', 'true');
    }
    if (tip) tip.removeAttribute('hidden');
  });
}

async function openMobileWageTooltip(page) {
  await openDesktopWageTooltip(page);
}

async function openLargestCluster(page) {
  const ok = await page.evaluate(async () => window.jobMap?.debugOpenLargestCluster?.());
  if (!ok) throw new Error('debugOpenLargestCluster failed');
  await page.locator('.maplibregl-popup:visible, .fcwrap.kb-lagen-fcwrap:visible, .fcw.kb-lagen-fcw:visible').first().waitFor({
    state: 'visible',
    timeout: 15_000,
  });
}

async function clusterGoToPage(page, targetPage) {
  for (let i = 0; i < 30; i++) {
    const text = await page.locator('[data-lagen-counter]').first().textContent().catch(() => '');
    const m = text && text.match(/(\d+)\s*van\s*(\d+)/i);
    if (m && Number(m[1]) === targetPage) return;
    if (!m || Number(m[1]) >= targetPage) break;
    const next = page.locator('[data-cluster-next]').first();
    if (!(await next.count())) break;
    await next.click();
    await page.waitForTimeout(350);
  }
}

async function shot(page, subdir, name) {
  const dir = path.join(capturesDir, subdir);
  fs.mkdirSync(dir, { recursive: true });
  const file = path.join(dir, name);
  await page.screenshot({ path: file, fullPage: false });
  return file;
}

async function runViewport(browser, width, height, mobile, slug, fn) {
  assertTime();
  const context = await browser.newContext({
    viewport: { width, height },
    hasTouch: mobile,
    isMobile: mobile,
    serviceWorkers: 'block',
  });
  await routeLocalAssets(context);
  const page = await context.newPage();
  page.setDefaultTimeout(30_000);
  await gotoBanenkaart(page);
  await fn(page, slug);
  await context.close();
}

const photoMetrics = [];

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  if (!fs.existsSync(path.join(mockRoot, 'desktop/02-banenkaart-lagen-desktop-1440.png'))) {
    throw new Error(`Mockups missing under ${mockRoot}. Run run-capture.sh to extract c8ee9298.`);
  }

  const browser = await chromium.launch({ headless: true, args: ['--disable-dev-shm-usage', '--no-sandbox'] });
  try {
  // 1 desktop map+list
  await runViewport(browser, 1280, 800, false, 'desktop-1280-map-list', async (page) => {
    await waitReady(page);
    await page.waitForTimeout(800);
    const app = await shot(page, 'desktop-1280-map-list', '01-map-list.png');
    sideBySide('desktop/02-banenkaart-lagen-desktop-1440.png', app, 'side-by-side-01-desktop-map-list.png');
  });

  // 2 desktop pin popup + photo timing
  await runViewport(browser, 1280, 800, false, 'desktop-1280-popup', async (page) => {
    await waitReady(page);
    const clickStart = Date.now();
    await openVacancyById(page, vacancyWithPhoto);
    const metrics = await measurePhotoMs(page);
    metrics.clickToImageMs = Date.now() - clickStart;
    metrics.profile = 'desktop-popup';
    photoMetrics.push(metrics);
    await page.waitForTimeout(300);
    const app = await shot(page, 'desktop-1280-popup', '02-popup.png');
    sideBySide('desktop/03-banenkaart-lagen-desktop-1440-enkel.png', app, 'side-by-side-02-desktop-popup.png');
  });

  // 3 desktop cluster page 3
  await runViewport(browser, 1280, 800, false, 'desktop-1280-cluster', async (page) => {
    await waitReady(page);
    await waitForClusterLayer(page);
    await openLargestCluster(page);
    await clusterGoToPage(page, 3);
    await page.waitForTimeout(400);
    const app = await shot(page, 'desktop-1280-cluster', '03-popup-cluster.png');
    sideBySide('desktop/04-banenkaart-lagen-desktop-1440-cluster.png', app, 'side-by-side-03-desktop-cluster.png');
  });

  // 4 desktop € tooltip
  await runViewport(browser, 1280, 800, false, 'desktop-1280-uurloon', async (page) => {
    await waitReady(page);
    await openVacancyById(page, vacancyWithPhoto);
    await openDesktopWageTooltip(page);
    await page.waitForTimeout(350);
    const app = await shot(page, 'desktop-1280-uurloon', '04-uurloon-hover.png');
    sideBySide('extra/desktop-1440-enkel-uurloon-hover.png', app, 'side-by-side-04-desktop-uurloon.png');
  });

  // 5 mobile map
  await runViewport(browser, 390, 844, true, 'mobile-390-map', async (page) => {
    await waitReady(page);
    await page.waitForTimeout(600);
    const app = await shot(page, 'mobile-390-map', '01-map.png');
    sideBySide('mobile/01-banenkaart-lagen-mobiel-390.png', app, 'side-by-side-05-mobile-map.png');
  });

  // 6 mobile uitleg
  await runViewport(browser, 390, 844, true, 'mobile-390-uitleg', async (page) => {
    await waitReady(page);
    await page.locator('[data-testid=kb-map-legbtn]').click();
    await page.waitForFunction(() => document.body.classList.contains('s-legend'), null, { timeout: 30_000 });
    const app = await shot(page, 'mobile-390-uitleg', '02-uitleg.png');
    sideBySide('mobile/02-banenkaart-lagen-mobiel-390-uitleg.png', app, 'side-by-side-06-mobile-uitleg.png');
  });

  // 7 mobile vacancy card + photo
  await runViewport(browser, 390, 844, true, 'mobile-390-vacature', async (page) => {
    await waitReady(page);
    const clickStart = Date.now();
    await openVacancyById(page, vacancyWithPhoto);
    const metrics = await measurePhotoMs(page);
    metrics.clickToImageMs = Date.now() - clickStart;
    metrics.profile = 'mobile-vacature';
    photoMetrics.push(metrics);
    await page.waitForTimeout(300);
    const app = await shot(page, 'mobile-390-vacature', '03-vacature.png');
    sideBySide('mobile/04-banenkaart-lagen-mobiel-390-enkel.png', app, 'side-by-side-07-mobile-vacature.png');
  });

  // 8 mobile cluster page 3
  await runViewport(browser, 390, 844, true, 'mobile-390-cluster', async (page) => {
    await waitReady(page);
    await waitForClusterLayer(page);
    await openLargestCluster(page);
    await clusterGoToPage(page, 3);
    await page.waitForTimeout(400);
    const app = await shot(page, 'mobile-390-cluster', '04-cluster.png');
    sideBySide('mobile/05-banenkaart-lagen-mobiel-390-cluster.png', app, 'side-by-side-08-mobile-cluster.png');
  });

  // 9 mobile € tooltip
  await runViewport(browser, 390, 844, true, 'mobile-390-uurloon', async (page) => {
    await waitReady(page);
    await openVacancyById(page, vacancyWithPhoto);
    await openMobileWageTooltip(page);
    await page.waitForTimeout(350);
    const app = await shot(page, 'mobile-390-uurloon', '05-uurloon-tap.png');
    sideBySide('extra/mobiel-390-enkel-uurloon-tik.png', app, 'side-by-side-09-mobile-uurloon.png');
  });

  fs.writeFileSync(path.join(outDir, 'popup-photo-metrics.json'), JSON.stringify(photoMetrics, null, 2));
  } finally {
    await browser.close();
  }

  fs.writeFileSync(
    path.join(outDir, 'REPORT.md'),
    `# Banenkaart lagen capture (standalone Playwright)\n\nBase: ${baseUrl}\n\n## Popup photo timing\n\n\`\`\`json\n${JSON.stringify(photoMetrics, null, 2)}\n\`\`\`\n\n## Side-by-sides\n\n${fs
      .readdirSync(outDir)
      .filter((f) => f.startsWith('side-by-side-') && f.endsWith('.png'))
      .sort()
      .map((f) => `- ${path.join(outDir, f)}`)
      .join('\n')}\n`,
  );
  console.log('Done.', outDir);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
