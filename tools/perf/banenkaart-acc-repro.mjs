import { chromium } from 'playwright';
import fs from 'fs';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const outDir = '/opt/cursor/artifacts/banenkaart-acc-repro';
fs.mkdirSync(outDir, { recursive: true });

const email = process.env.JOBSY_E2E_CANDIDATE_EMAIL || 'kandidaat@jobsy.local';
const password = process.env.JOBSY_E2E_CANDIDATE_PASSWORD || 'Jobsy123!';

async function login(page) {
  await page.goto(`${baseUrl}/login`, { waitUntil: 'load', timeout: 120000 });
  const cookie = page.locator('#cookie-consent-accept, [data-testid=cookie-accept]').first;
  if (await cookie.count()) await cookie.click({ timeout: 5000 }).catch(() => {});
  await page.fill('input[type=email], input[name=Email]', email);
  await page.fill('input[type=password], input[name=Password]', password);
  await page.click('button[type=submit]');
  await page.waitForURL(/\/(candidate|banenkaart|login)/, { timeout: 60000 });
}

async function diag(page, tag) {
  return page.evaluate(() => {
    const root = document.querySelector('[data-banenkaart-ssr="1"]');
    const styles = Array.from(document.querySelectorAll('link[rel=stylesheet]')).map((l) => l.href);
    const scripts = Array.from(document.querySelectorAll('script[src]')).map((s) => s.src);
    const map = document.querySelector('#job-map');
    const canvas = document.querySelector('#job-map canvas, #job-map .maplibregl-canvas');
    const fab = document.querySelector('[data-testid=kb-map-fab]');
    const nav = document.querySelector('[data-testid=candidate-bottom-nav-ssr]');
    const row = document.querySelector('[data-testid=kb-list-row]');
    const vis = (el) => {
      if (!el) return null;
      const s = getComputedStyle(el);
      const r = el.getBoundingClientRect();
      return {
        display: s.display,
        visibility: s.visibility,
        height: r.height,
        width: r.width,
        classes: el.className,
      };
    };
    return {
      url: location.href,
      rootClasses: root?.className || null,
      mapReady: root?.dataset?.banenkaartMapReady || null,
      hasJobMap: !!window.jobMap,
      hasCatalog: !!window.jobsyBanenkaartCatalog,
      stylesheets: styles.filter((h) => /banenkaart|candidate-no-blazor|auth-chrome|app\.min/.test(h)),
      allStylesCount: styles.length,
      scripts: scripts.filter((h) => /banenkaart|jobMap|maplibre/.test(h)),
      map: vis(map),
      canvas: vis(canvas),
      fab: vis(fab),
      nav: vis(nav),
      firstRow: row
        ? {
            classes: row.className,
            height: row.getBoundingClientRect().height,
            cardInner: !!row.querySelector('.kb-vacancy-card, .vc, .kb-list-row__card'),
          }
        : null,
      showMap: root?.classList.contains('show-map'),
    };
  });
}

for (const [w, h, touch, name] of [
  [1280, 720, false, 'desktop'],
  [390, 844, true, 'mobile'],
]) {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: w, height: h },
    hasTouch: touch,
    isMobile: touch,
    serviceWorkers: 'allow',
  });
  const consoleErrors = [];
  const csp = [];
  const failed404 = [];
  const page = await context.newPage();
  page.on('console', (msg) => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
    if (/Content Security Policy|script-src/i.test(msg.text())) csp.push(msg.text());
  });
  page.on('response', (r) => {
    if (r.status() === 404) failed404.push(r.url());
  });

  await login(page);
  await page.goto(`${baseUrl}/banenkaart`, { waitUntil: 'load', timeout: 120000 });
  await page.waitForTimeout(3000);
  const before = await diag(page, 'load');
  await page.reload({ waitUntil: 'load' });
  await page.waitForTimeout(4000);
  const after = await diag(page, 'hard-reload');
  await page.screenshot({ path: `${outDir}/${name}-after-reload.png` });

  fs.writeFileSync(
    `${outDir}/${name}-report.json`,
    JSON.stringify({ before, after, consoleErrors, csp, failed404: failed404.slice(0, 40) }, null, 2),
  );
  await browser.close();
}

console.log('Wrote reports to', outDir);
