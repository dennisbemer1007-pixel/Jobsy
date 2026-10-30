/**
 * PR #710 review evidence: Entra dummy login + banenkaart with employers ON.
 * Usage:
 *   BASE=http://127.0.0.1:5201 \
 *   ACC_BASE=https://acceptatie.lobsy.nl \
 *   OUT=/cursor/stores/self/perf2/screenshots \
 *   node tools/perf/perf2-capture-review.mjs
 */
import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const accBase = (process.env.ACC_BASE || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const out = process.env.OUT || '/cursor/stores/self/perf2/screenshots';

/** Desktop list cards/rows; mobile map mode shows count before cards mount. */
const VACANCY_COUNT_SEL =
  '[data-testid="kb-mobile-result-count"], [data-testid="kb-desktop-result-count"]';

async function waitVacanciesVisible(page) {
  await page.waitForFunction(
    () => {
      if (document.querySelector('[data-vacancy-id]')) {
        return true;
      }

      const countEl = document.querySelector(
        '[data-testid="kb-mobile-result-count"], [data-testid="kb-desktop-result-count"]',
      );
      if (!countEl) {
        return false;
      }

      const body = document.body.innerText || '';
      if (/vacatures?\s*laden|loading/i.test(body)) {
        return false;
      }

      const text = (countEl.textContent || '').trim();
      const match = text.match(/(\d+)\s*(vacatures?|jobs?|vacancies)/i);
      return match !== null && Number(match[1]) > 0;
    },
    null,
    { timeout: 90_000 },
  );
}

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

async function mapMetrics(page, origin) {
  const t0 = Date.now();
  await page.goto(`${origin}/banenkaart`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await dismissCookies(page);
  await page.waitForFunction(
    () => !document.body.innerText.includes('binnenkort'),
    null,
    { timeout: 90_000 },
  );
  const tNoSoon = Date.now() - t0;
  await page.locator('#job-map canvas').first().waitFor({ state: 'attached', timeout: 90_000 });
  const tMapCanvas = Date.now() - t0;
  await waitVacanciesVisible(page);
  const tVacancies = Date.now() - t0;
  const vacancyDomCount = await page.locator('[data-vacancy-id]').count();
  const countText = await page.locator(VACANCY_COUNT_SEL).first().innerText().catch(() => '');
  const hasMap = await page.locator('#job-map canvas').count();
  const bodySnippet = (await page.innerText('body')).slice(0, 200);
  return {
    origin,
    hasMap,
    vacancyDomCount,
    resultCountText: countText.trim(),
    vacancyVisibleSignal: '[data-vacancy-id] or kb-*-result-count with number',
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

  const local = [];
  const acceptatie = [];
  for (const [w, h, tag] of [[1440, 900, 'desktop'], [390, 844, 'mobile']]) {
    for (const [origin, bucket, prefix] of [
      [base, local, 'after-local'],
      [accBase, acceptatie, 'after-acc'],
    ]) {
      const ctx = await browser.newContext({ viewport: { width: w, height: h } });
      const page = await ctx.newPage();
      const m = await mapMetrics(page, origin);
      bucket.push({ tag, ...m });
      if (origin === base) {
        await shot(page, path.join(out, `${prefix}-banenkaart-${tag}-employers-on.jpg`));
      }
      await ctx.close();
    }
  }

  const payload = {
    measuredAt: new Date().toISOString(),
    vacancyVisibleSignal: '[data-vacancy-id] or result-count with vacatures/jobs',
    local,
    acceptatie,
  };
  await writeFile(path.join(out, 'banenkaart-load-metrics.json'), JSON.stringify(payload, null, 2));
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
