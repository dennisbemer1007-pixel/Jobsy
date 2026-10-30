import { chromium, devices } from 'playwright';
import crypto from 'crypto';
import fs from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL ?? 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.JOBSY_ILLUSTRATIONS_OUT ?? '/opt/cursor/artifacts/diversiteit-r19';
const repoRoot = process.env.JOBSY_REPO_ROOT ?? '/workspace';
const mobileVersion = process.env.JOBSY_MOBILE_HERO_VERSION ?? '20261018-r19';
fs.mkdirSync(outDir, { recursive: true });

function md5File(filePath) {
  return crypto.createHash('md5').update(fs.readFileSync(filePath)).digest('hex');
}

async function verifyServedMobileHero(page, step, slug) {
  const img = page.locator('[data-testid=onboarding-mobile-hero-img]');
  await img.waitFor({ state: 'visible', timeout: 60000 });
  const src = await img.getAttribute('src');
  if (!src || !src.includes(mobileVersion)) {
    throw new Error(`Step ${step} (${slug}): expected ?v=${mobileVersion} in img src, got ${src}`);
  }
  const repoJpg = path.join(repoRoot, `Jobsy.Web/wwwroot/images/onboarding/${slug}-mobile-hero.jpg`);
  const repoMd5 = md5File(repoJpg);
  const url = src.startsWith('http') ? src : `${baseUrl}${src.startsWith('/') ? '' : '/'}${src}`;
  const resp = await fetch(url, { cache: 'no-store' });
  if (!resp.ok) {
    throw new Error(`Step ${step} (${slug}): fetch ${url} -> ${resp.status}`);
  }
  const servedMd5 = crypto.createHash('md5').update(Buffer.from(await resp.arrayBuffer())).digest('hex');
  console.log(`hero-src-md5 step ${step} ${slug}`, { src, repoMd5, servedMd5, match: repoMd5 === servedMd5 });
  if (repoMd5 !== servedMd5) {
    throw new Error(`Step ${step} (${slug}): served MD5 ${servedMd5} != repo ${repoMd5} (${src})`);
  }
}

async function fetchCode(email) {
  const apiBase = baseUrl.replace(':5201', ':5200');
  for (let i = 0; i < 40; i++) {
    const r = await fetch(`${apiBase}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`);
    const j = await r.json();
    if (j.code) return j.code;
    await new Promise((res) => setTimeout(res, 500));
  }
  throw new Error('No OTP for ' + email);
}

async function dismissCookies(page) {
  const btn = page.locator('.cookie-consent button[data-consent="analytics"], .cookie-consent .btn-compact').first();
  if (await btn.count()) {
    await btn.click({ force: true }).catch(() => {});
    await page.waitForTimeout(300);
  }
}

async function register(page, email) {
  await page.goto(`${baseUrl}/account-maken`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[type=email]').fill(email);
  await page.locator('input[name=firstName]').fill('Illus');
  await page.locator('button.login-submit, button.au-submit[type=submit]').first().click();
  await page.waitForURL('**/code**', { timeout: 60000 });
  const code = await fetchCode(email);
  await page.locator('input[name=code]').fill(code);
  await page.locator('button.login-submit').click();
  await page.waitForURL((u) => !u.toString().includes('/code'), { timeout: 90000 });
  await dismissCookies(page);
}

async function beginWizard(page) {
  await page.goto(`${baseUrl}/candidate/start`, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('.ob-wizard__welcome, [data-testid^=onboarding-step-], .ob-wizard__loading', {
    timeout: 120000,
  });
  await page.waitForFunction(
    () =>
      document.querySelector('.ob-wizard__welcome') ||
      document.querySelector('[data-testid^="onboarding-step-"]'),
    { timeout: 120000 },
  );
  if (await page.locator('.ob-wizard__welcome').count()) {
    await page.getByRole('button', { name: 'Beginnen' }).click({ force: true });
    await page.waitForTimeout(800);
  }
  await page.waitForSelector('[data-testid=onboarding-step-1-illustrated]', { timeout: 90000 });
  await dismissCookies(page);
}

async function fillStep1(page) {
  await page.locator('.ob-fields input').first().fill('Illus');
  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('#onboarding-home-address').fill('2671GB');
  await page.waitForTimeout(500);
  const s = page.locator('.kb-address__suggest button, .address-suggestions button').first();
  if (await s.count()) await s.click();
  const birthText = page.locator('[data-testid=onboarding-birth-date] .jobsy-date-field__text').first();
  if (await birthText.count()) await birthText.fill('15-06-2000');
  else {
    const birthDate = page.locator('[data-testid=onboarding-birth-date] input[type=date]').first();
    if (await birthDate.count()) await birthDate.fill('2000-06-15');
  }
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
  await page.waitForSelector('[data-testid=onboarding-step-2-illustrated]', { timeout: 90000 });
}

async function fillStep2(page) {
  await page.locator('.ob-wizard__presets .lobsy-choice-tile').first().click();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
}

async function fillStep3(page) {
  await page.locator('#onboarding-field-transport .lobsy-choice-tile').first().click();
  await page.locator('.ob-wizard__travel-times .lobsy-choice-tile').filter({ hasText: '30' }).first().click();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
  if (await page.locator('[data-testid=early-reward-nearby]').count()) {
    await page.getByTestId('onboarding-next').click({ force: true });
    await page.waitForTimeout(800);
  }
}

async function fillStep4(page) {
  const job = page.locator('[data-testid=onboarding-work-job]').first();
  await job.locator('input').first().fill('Verkoopmedewerker');
  await job.locator('input').nth(1).fill('Bakkerij Jansen');
  await job.locator('.ob-job-dates__current input[type=checkbox]').check();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
}

async function fillStep5(page) {
  await page.locator('.ob-wizard__education-grid .lobsy-choice-tile').filter({ hasText: 'HBO' }).first().click();
  await dismissCookies(page);
  await page.getByTestId('onboarding-next').click({ force: true });
  await page.waitForTimeout(800);
}

const mobileSteps = [
  [1, 'kennismaken', null],
  [2, 'beschikbaarheid', fillStep1],
  [3, 'vervoer', fillStep2],
  [4, 'werkervaring', fillStep3],
  [5, 'opleiding', fillStep4],
  [6, 'droombaan', fillStep5],
];

async function assertHeroLayout(page, step, slug, viewportWidth) {
  const audit = await page.evaluate(async () => {
    const pearl = [245, 242, 238];
    const hero = document.querySelector('[data-testid=onboarding-mobile-hero]');
    const copy = hero?.querySelector('.ob-illustrated-step__mobile-cover-copy');
    const img = hero?.querySelector('[data-testid=onboarding-mobile-hero-img]');
    if (!hero || !copy || !img) {
      return { ok: false, reason: 'missing-hero-copy-or-img' };
    }
    if (!img.complete) {
      await new Promise((res, rej) => {
        img.onload = () => res(null);
        img.onerror = () => rej(new Error('img-load'));
      });
    }
    const hr = hero.getBoundingClientRect();
    const textNodes = copy.querySelectorAll(
      'h1, .ob-illustrated-step__mobile-lead, .ob-illustrated-step__mobile-eyebrow, .ob-wizard__badge',
    );
    let textRight = hr.left;
    let textTop = hr.bottom;
    let textBottom = hr.top;
    textNodes.forEach((el) => {
      const rects = el.getClientRects();
      for (const tr of rects) {
        if (tr.width <= 0) continue;
        textRight = Math.max(textRight, tr.right);
        textTop = Math.min(textTop, tr.top);
        textBottom = Math.max(textBottom, tr.bottom);
      }
    });
    const copyMaxRight = hr.left + hr.width * 0.48;
    if (textRight > copyMaxRight + 2) {
      return {
        ok: false,
        reason: `copy-width textRight=${textRight.toFixed(1)} max=${copyMaxRight.toFixed(1)}`,
      };
    }
    const cw = Math.round(hr.width);
    const ch = Math.round(hr.height);
    if (cw < 200 || ch < 80) {
      return { ok: false, reason: `hero-size ${cw}x${ch}` };
    }
    const canvas = document.createElement('canvas');
    canvas.width = cw;
    canvas.height = ch;
    const ctx = canvas.getContext('2d');
    if (!ctx) return { ok: false, reason: 'no-canvas' };
    ctx.drawImage(img, 0, 0, cw, ch);
    const data = ctx.getImageData(0, 0, cw, ch).data;
    const figStartPx = Math.floor(cw * 0.55);
    const isBg = (r, g, b) => Math.hypot(r - pearl[0], g - pearl[1], b - pearl[2]) < 26;
    const copyLeft = Math.max(0, Math.round(hr.left - hr.left));
    const copyRight = Math.min(cw, Math.round(textRight - hr.left));
    const copyTop = Math.max(0, Math.round(textTop - hr.top));
    const copyBottom = Math.min(ch, Math.round(textBottom - hr.top));
    let minFigInRightBand = cw;
    for (let y = Math.floor(ch * 0.12); y < Math.floor(ch * 0.96); y++) {
      for (let x = figStartPx; x < cw; x++) {
        const i = (y * cw + x) * 4;
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];
        if (!isBg(r, g, b)) {
          minFigInRightBand = Math.min(minFigInRightBand, x);
        }
      }
    }
    if (minFigInRightBand < figStartPx - 2) {
      return { ok: false, reason: `figure-band minFigX=${minFigInRightBand} need>=${figStartPx}` };
    }
    const overlapX0 = Math.max(figStartPx, copyLeft);
    if (copyRight > overlapX0 + 1) {
      for (let y = copyTop; y < copyBottom; y++) {
        for (let x = overlapX0; x < copyRight; x++) {
          const i = (y * cw + x) * 4;
          const r = data[i];
          const g = data[i + 1];
          const b = data[i + 2];
          if (!isBg(r, g, b)) {
            return { ok: false, reason: `copy-figure-overlap at ${x},${y}` };
          }
        }
      }
    }
    return { ok: true, reason: '', minFigInRightBand, figStartPx, copyRight: textRight - hr.left };
  });
  if (!audit.ok) {
    throw new Error(`Hero layout step ${step} (${slug}) @${viewportWidth}px: ${audit.reason}`);
  }
  console.log(`layout-ok step ${step} ${slug} @${viewportWidth}`, audit);
}

async function captureHeroCloseup(page, step, slug) {
  const hero = page.locator('[data-testid=onboarding-mobile-hero]');
  await hero.waitFor({ timeout: 60000 });
  const file = path.join(outDir, `${String(step).padStart(2, '0')}-${slug}-hero-closeup-2x.png`);
  await hero.screenshot({ path: file, scale: 'device' });
  console.log('saved', file);
}

async function captureMobileFlow(browser, viewportWidth) {
  const ctx = await browser.newContext({
    ...(viewportWidth === 390 ? devices['Pixel 5'] : {}),
    viewport: { width: viewportWidth, height: 844 },
    locale: 'nl-NL',
    isMobile: viewportWidth < 768,
    hasTouch: viewportWidth < 768,
  });
  const page = await ctx.newPage();
  await register(page, `illus.r19.${viewportWidth}.${Date.now()}@jobsy.local`);
  await beginWizard(page);
  for (const [step, slug, prep] of mobileSteps) {
    if (prep) await prep(page);
    await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`, { timeout: 90000 });
    await page.locator('[data-testid=onboarding-mobile-hero]').waitFor({ state: 'visible', timeout: 60000 });
    await page.waitForFunction(
      () => {
        const h = document.querySelector('[data-testid=onboarding-mobile-hero]');
        return h != null && h.getBoundingClientRect().width > 200;
      },
      { timeout: 30000 },
    );
    await verifyServedMobileHero(page, step, slug);
    await assertHeroLayout(page, step, slug, viewportWidth);
    const suffix = `mobiel-${viewportWidth}`;
    const file = path.join(outDir, `${String(step).padStart(2, '0')}-${slug}-${suffix}.png`);
    await page.screenshot({ path: file, fullPage: false });
    console.log('saved', file);
    if (viewportWidth === 390) {
      await captureHeroCloseup(page, step, slug);
    }
  }
  await ctx.close();
}

async function captureDesktopStep(browser, step, slug, prepChain) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'nl-NL' });
  const page = await ctx.newPage();
  await register(page, `illus.r15.d${step}.${Date.now()}@jobsy.local`);
  await beginWizard(page);
  for (const prep of prepChain) {
    if (prep) await prep(page);
  }
  await page.waitForSelector(`[data-testid=onboarding-step-${step}-illustrated]`, { timeout: 90000 });
  const file = path.join(outDir, `${String(step).padStart(2, '0')}-${slug}-desktop-1440.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log('saved', file);
  await ctx.close();
}

const browser = await chromium.launch({ headless: true });
for (const w of [390, 360]) {
  await captureMobileFlow(browser, w);
}
await captureDesktopStep(browser, 1, 'kennismaken', []);
await captureDesktopStep(browser, 6, 'droombaan', [fillStep1, fillStep2, fillStep3, fillStep4, fillStep5]);
await browser.close();
console.log('diversiteit-r19 capture + layout audit + hero md5 passed');
