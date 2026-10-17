/**
 * PR #710 review evidence: Entra dummy login + banenkaart with employers ON.
 * Usage:
 *   BASE=http://127.0.0.1:5201 \
 *   OUT=/cursor/stores/self/perf2/screenshots \
 *   node tools/perf/perf2-capture-review.mjs
 *
 * Start Web with Entra dummy for Microsoft button:
 *   Authentication__Entra__ClientId=e2e-dummy Authentication__Entra__ClientSecret=e2e-dummy-secret
 */
import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const out = process.env.OUT || '/cursor/stores/self/perf2/screenshots';

async function shot(page, file) {
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, type: 'jpeg', quality: 88, fullPage: false });
  console.log('wrote', file);
}

async function dismissCookies(page) {
  const btn = page.locator(
    '.cookie-consent [data-consent=analytics], .cookie-consent [data-consent=necessary]',
  ).first();
  await btn.click({ timeout: 8000 }).catch(() => {});
}

async function mapMetrics(page) {
  const t0 = Date.now();
  await page.goto(`${base}/banenkaart`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await dismissCookies(page);
  await page.waitForFunction(
    () => !document.body.innerText.includes('binnenkort'),
    null,
    { timeout: 90_000 },
  );
  const tNoSoon = Date.now() - t0;
  await page.locator('#job-map canvas').first().waitFor({ state: 'attached', timeout: 90_000 });
  const tMapCanvas = Date.now() - t0;
  const vacancySel =
    '.job-card, [data-testid="vacancy-card"], .banenkaart-list__item, .pub-vacancy-card';
  await page.waitForFunction(
    (sel) => document.querySelectorAll(sel).length > 0,
    vacancySel,
    { timeout: 90_000 },
  ).catch(() => {});
  const vacancyCount = await page.locator(vacancySel).count();
  const tVacancies = Date.now() - t0;
  const hasMap = await page.locator('#job-map canvas').count();
  const bodySnippet = (await page.innerText('body')).slice(0, 200);
  return {
    hasMap,
    vacancyCount,
    msDomContentLoaded: tNoSoon,
    msMapCanvas: tMapCanvas,
    msVacanciesListed: tVacancies,
    bodySnippet,
  };
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  for (const [w, h, tag] of [[1440, 900, 'desktop'], [390, 844, 'mobile']]) {
    const ctx = await browser.newContext({ viewport: { width: w, height: h } });
    const page = await ctx.newPage();
    await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 60_000 });
    const blazor = await page.evaluate(() => !!document.querySelector('script[src*="blazor.web.js"]'));
    const ms = await page.innerText('body');
    await shot(page, path.join(out, `after-local-login-${tag}-entra-dummy.jpg`));
    console.log(JSON.stringify({ tag, blazor, hasMicrosoft: /microsoft/i.test(ms), hasOrEmail: /e-mail/i.test(ms) }));
    await ctx.close();
  }

  const metrics = [];
  for (const [w, h, tag] of [[1440, 900, 'desktop'], [390, 844, 'mobile']]) {
    const ctx = await browser.newContext({ viewport: { width: w, height: h } });
    const page = await ctx.newPage();
    const m = await mapMetrics(page);
    metrics.push({ tag, ...m });
    await shot(page, path.join(out, `after-local-banenkaart-${tag}-employers-on.jpg`));
    await ctx.close();
  }
  await writeFile(path.join(out, 'banenkaart-load-metrics.json'), JSON.stringify(metrics, null, 2));
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
