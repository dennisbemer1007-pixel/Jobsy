/**
 * PR #755 r5 — verified external vacancy captures (load + scroll guards).
 *
 *   LOBSY_BASE_URL=http://127.0.0.1:5201 \
 *   OUT=/cursor/stores/self/artifacts/pr755-r5 \
 *   node tools/screenshots/capture-pr755-r5.mjs
 */
import { chromium } from 'playwright';
import { mkdir, stat, readFile } from 'node:fs/promises';
import path from 'node:path';
import { createHash } from 'node:crypto';

const baseUrl = (process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.OUT || '/cursor/stores/self/artifacts/pr755-r5';
const mirrorDir = process.env.MIRROR_OUT || '/opt/cursor/artifacts/pr755-r5';
const extVacancyId = process.env.PR755_EXT_VACANCY_ID || 'eeeeeeee-2222-2222-2222-222222222222';
const email = process.env.PR755_EMAIL || 'sanne@jobsy.local';
const password = process.env.PR755_PASSWORD || 'Jobsy123!';

const consentInitScript = `
  try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
  document.documentElement.classList.add('cookie-consent-known');
`;

async function login(page) {
  const t0 = Date.now();
  await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[name="email"], input[type="email"]').first().fill(email, { timeout: 60_000 });
  await page.locator('input[name="password"], input[type="password"]').first().fill(password);
  await page.locator('form.au-form button[type="submit"], button.login-submit, button[type="submit"]').first().click();
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 90_000 });
  await page.locator('[data-lobsy-circuit="ready"]').waitFor({ timeout: 90_000 });
  console.log('login-ms', Date.now() - t0);
}

async function assertDetailLoaded(page, label) {
  const detail = page.locator('[data-testid="kb-ext-detail"]');
  await detail.waitFor({ state: 'visible', timeout: 120_000 });

  if (await page.locator('[data-testid="kb-ext-detail"] > .state-message').isVisible()) {
    await page.locator('[data-testid="kb-ext-detail"] > .state-message').waitFor({
      state: 'detached',
      timeout: 120_000,
    });
  }

  const title = page.locator('[data-testid="kb-ext-detail"] .kb-ext__title');
  await title.waitFor({ state: 'visible', timeout: 120_000 });
  const titleText = ((await title.textContent()) || '').trim();
  if (titleText.length < 2 || /^laden$/i.test(titleText)) {
    throw new Error(`${label}: invalid title "${titleText}"`);
  }

  await page.locator('[data-testid="kb-ext-detail-actions"]').waitFor({ state: 'visible', timeout: 60_000 });
  return titleText;
}

async function scrollWindowToActionsBottom(page) {
  const result = await page.evaluate(() => {
    const root = document.scrollingElement || document.documentElement;
    const apply = document.querySelector('[data-testid="kb-ext-apply-cta"]');
    const actions = document.querySelector('[data-testid="kb-ext-detail-actions"]');
    const target = apply || actions;
    if (!target) {
      return { ok: false, reason: 'missing-actions' };
    }

    const maxY = Math.max(0, root.scrollHeight - window.innerHeight);
    let bestY = 0;
    let bestScore = -Infinity;

    for (let y = 0; y <= maxY + 32; y += 32) {
      root.scrollTop = y;
      window.scrollTo(0, y);
      const rect = target.getBoundingClientRect();
      const inLowerHalf = rect.top >= window.innerHeight * 0.32 && rect.bottom <= window.innerHeight * 0.92;
      const score = inLowerHalf ? rect.top : -Math.abs(rect.top - window.innerHeight * 0.45);
      if (score > bestScore) {
        bestScore = score;
        bestY = y;
      }
    }

    root.scrollTop = bestY;
    window.scrollTo(0, bestY);

    const applyRect = apply?.getBoundingClientRect();
    const scrollY = window.scrollY;
    const ok = scrollY >= 120 && applyRect && applyRect.top >= window.innerHeight * 0.25;
    return {
      ok,
      reason: ok ? 'ok' : `scrollY-${scrollY}-applyTop-${applyRect?.top ?? 'na'}`,
      scrollY,
      applyTop: applyRect?.top,
    };
  });

  await page.waitForTimeout(500);
  return result;
}

async function snapViewport(page, file, expect) {
  await assertDetailLoaded(page, expect);
  await mkdir(path.dirname(file), { recursive: true });
  await page.screenshot({ path: file, fullPage: false, timeout: 120_000 });
  const st = await stat(file);
  const hash = createHash('sha256').update(await readFile(file)).digest('hex').slice(0, 12);
  console.log('saved', file, 'bytes', st.size, 'sha', hash);
  return { size: st.size, sha: hash };
}

async function captureViewport(page, tag) {
  const navT0 = Date.now();
  await page.goto(`${baseUrl}/candidate/external/${extVacancyId}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('[data-lobsy-circuit="ready"]').waitFor({ timeout: 90_000 });

  const title = await assertDetailLoaded(page, `load-${tag}`);
  const loadMs = Date.now() - navT0;
  console.log(`load-${tag}`, { loadMs, title });

  await page.evaluate(() => {
    const root = document.scrollingElement || document.documentElement;
    root.scrollTop = 0;
    window.scrollTo(0, 0);
  });
  await page.waitForTimeout(350);

  const topFile = path.join(outDir, `external-vacancy-top-${tag}.png`);
  const topMeta = await snapViewport(page, topFile, `top-${tag}`);

  const scroll = await scrollWindowToActionsBottom(page);
  console.log(`scroll-bottom-${tag}`, scroll);
  if (!scroll.ok) {
    throw new Error(`bottom scroll failed for ${tag}: ${scroll.reason}`);
  }

  const bottomFile = path.join(outDir, `external-vacancy-bottom-${tag}.png`);
  const bottomMeta = await snapViewport(page, bottomFile, `bottom-${tag}`);

  if (topMeta.sha === bottomMeta.sha) {
    throw new Error(`${tag}: top and bottom PNG identical (${topMeta.sha})`);
  }

  const metrics = await page.evaluate(() => {
    const card = document.querySelector('.kb-ext__card')?.getBoundingClientRect();
    const apply = document.querySelector('[data-testid="kb-ext-apply-cta"]')?.getBoundingClientRect();
    const coach = document.querySelector('#lobsy-coach-btn, .lobsy-coach-dock__btn')?.getBoundingClientRect();
    const notInterested = document.querySelector('[data-testid="kb-ext-not-interested"]')?.getBoundingClientRect();
    return {
      scrollY: window.scrollY,
      cardW: card?.width,
      applyW: apply?.width,
      applyTop: apply?.top,
      coachGap: coach && notInterested ? coach.left - notInterested.right : null,
    };
  });
  console.log(`metrics-${tag}`, metrics);

  return { topMeta, bottomMeta, loadMs };
}

async function main() {
  await mkdir(outDir, { recursive: true });
  await mkdir(mirrorDir, { recursive: true }).catch(() => {});

  const browser = await chromium.launch({ headless: true });
  const report = { consoleErrors: [], loadTimes: {} };

  for (const vp of [
    { tag: '390', width: 390, height: 844, mobile: true },
    { tag: '1440', width: 1440, height: 900, mobile: false },
  ]) {
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height },
      locale: 'nl-NL',
      hasTouch: vp.mobile,
      isMobile: vp.mobile,
    });
    await context.addInitScript(consentInitScript);
    const page = await context.newPage();
    page.on('console', (msg) => {
      if (msg.type() === 'error' || msg.type() === 'warning') {
        report.consoleErrors.push({ vp: vp.tag, type: msg.type(), text: msg.text() });
      }
    });
    page.on('pageerror', (err) => report.consoleErrors.push({ vp: vp.tag, type: 'pageerror', text: String(err) }));

    await login(page);
    const cap = await captureViewport(page, vp.tag);
    report.loadTimes[vp.tag] = cap.loadMs;
    await context.close();
  }

  await browser.close();

  try {
    const { execSync } = await import('node:child_process');
    execSync(`cp -r "${outDir}/." "${mirrorDir}/"`, { stdio: 'inherit' });
  } catch {
    /* mirror optional */
  }

  console.log('report', JSON.stringify(report, null, 2));
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
