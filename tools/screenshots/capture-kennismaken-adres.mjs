import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../..');
const storeRoot =
  process.env.KENNISMAKEN_STORE || '/cursor/stores/self/kennismaken-adres';
const accBase = (process.env.ACCEPTATIE_BASE_URL || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const afterBase = (process.env.AFTER_BASE_URL || process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const email = process.env.JOBSY_E2E_EMAIL || 'kandidaat@jobsy.local';
const password = process.env.JOBSY_E2E_PASSWORD || 'Jobsy123!';

const allViewports = [
  { w: 390, h: 844, tag: 'mobile' },
  { w: 1280, h: 800, tag: 'desktop' },
];
const viewportFilter = (process.env.CAPTURE_VIEWPORTS || '')
  .split(',')
  .map((s) => s.trim())
  .filter(Boolean);
const viewports =
  viewportFilter.length > 0
    ? allViewports.filter((vp) => viewportFilter.includes(vp.tag))
    : allViewports;

function ensureDir(p) {
  fs.mkdirSync(p, { recursive: true });
}

async function acceptCookies(page) {
  const btn = page.locator('button.cookie-consent__accept, .cookie-consent button.btn-compact--primary').first();
  if (await btn.count()) {
    await btn.click({ timeout: 5000 }).catch(() => {});
    await page.waitForTimeout(200);
  }
  await page.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (_) {}
  });
}

async function login(page, base) {
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await acceptCookies(page);
  if (await page.locator('input[name="email"]').count()) {
    await page.fill('input[name="email"]', email);
    await page.fill('input[name="password"]', password);
    await Promise.all([
      page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 90_000 }),
      page.click('button.login-submit'),
    ]);
  }
}

async function waitCircuitReady(page) {
  await page.locator('html[data-lobsy-circuit="ready"]').waitFor({ timeout: 90_000 });
}

async function waitWizardWithoutLoadError(page, { strict = true } = {}) {
  await page.getByText(/^Laden[….]{1,3}$/).waitFor({ state: 'hidden', timeout: 90_000 }).catch(() => {});
  const err = page.locator('.ob-wizard__content .state-message.error');
  if (await err.count()) {
    const text = await err.first().textContent();
    const message = `Wizard load/save error banner visible: ${text?.trim()}`;
    if (strict) {
      throw new Error(message);
    }
    console.warn(message);
  }
}

async function gotoOnboardingStep1(page, base, { strictNoError = true } = {}) {
  await page.goto(`${base}/candidate/start`, { waitUntil: 'networkidle', timeout: 120_000 });
  await waitCircuitReady(page);
  await page.waitForSelector('.ob-fields h1, .ob-wizard__welcome, .ob-wizard__welcome .ob-wizard__cta', {
    timeout: 90_000,
  });
  if ((await page.locator('.ob-fields h1').count()) === 0) {
    const welcome = page.locator('.ob-wizard__welcome .ob-wizard__cta, .ob-wizard__welcome .btn-primary').first();
    if (await welcome.count()) {
      await welcome.click();
    } else {
      const cta = page.getByRole('button', { name: /Start|Begin|startprofiel|profiel/i }).first();
      if (await cta.count()) {
        await cta.click();
      }
    }
    await page.waitForSelector('.ob-fields', { timeout: 60_000 });
    await waitCircuitReady(page);
  }
  await page.waitForSelector('.ob-fields', { timeout: 60_000 });
  await page.locator('.ob-fields h1').first().waitFor({ timeout: 60_000 });
  await waitWizardWithoutLoadError(page, { strict: strictNoError });
  await page.waitForSelector('.ob-wizard__footer .ob-wizard__cta, .ob-wizard__footer button', {
    timeout: 60_000,
  }).catch(() => {});
}

async function scrollStepTop(page) {
  await page.evaluate(() => {
    window.scrollTo(0, 0);
    document.querySelector('.ob-wizard__content')?.scrollTo?.(0, 0);
  });
  await page.waitForTimeout(250);
}

async function scrollAddressSuggestionsIntoView(page) {
  const list = page.locator('.address-suggestions');
  if (await list.count()) {
    await list.scrollIntoViewIfNeeded();
    await page.waitForTimeout(200);
    return;
  }
  const { input } = await addressInput(page);
  await input.scrollIntoViewIfNeeded();
  await page.waitForTimeout(200);
}

async function screenshotStep(page, outPath, { suggestions = false } = {}) {
  if (suggestions) {
    await scrollAddressSuggestionsIntoView(page);
  } else {
    await scrollStepTop(page);
    const openList = page.locator('.address-suggestions');
    if (await openList.count()) {
      throw new Error('Expected no open address suggestions on baseline screenshot');
    }
  }
  await page.screenshot({ path: outPath, fullPage: false });
  console.log('wrote', outPath);
}

async function addressInput(page) {
  const kb = page.locator('#onboarding-home-address').first();
  if (await kb.count()) {
    return { input: kb, legacy: false };
  }
  const pc = page.locator('.ob-fields input[autocomplete=postal-code]').first();
  if (await pc.count()) {
    return { input: pc, legacy: true };
  }
  throw new Error('No address/postcode input found on step 1');
}

async function prepareAddressSuggestionsShot(page) {
  const { input, legacy } = await addressInput(page);
  if (legacy) {
    await input.click();
    await input.fill('2671GB');
    await page.waitForTimeout(2000);
    return { legacy: true };
  }
  await input.click();
  await input.fill('');
  await input.fill('Kerkstraat Naaldwijk');
  await page.waitForTimeout(900);
  const list = page.locator('.address-suggestions');
  await list.waitFor({ state: 'visible', timeout: 20_000 });
  await page.locator('.address-suggestions__status:has-text("Laden")').waitFor({ state: 'hidden', timeout: 20_000 }).catch(() => {});
  await page.locator('.address-suggestions__item').first().waitFor({ state: 'visible', timeout: 20_000 });
  if ((await page.locator('.address-suggestions__item').count()) < 1) {
    throw new Error('Expected address suggestions to be visible');
  }
  return { legacy: false };
}

async function captureOneShot(browser, base, vp, outPath, { strictNoError, prepareSuggestions, allowLegacy, suggestionsFrame }) {
  const context = await browser.newContext({
    viewport: { width: vp.w, height: vp.h },
    isMobile: vp.w < 600,
    hasTouch: vp.w < 600,
  });
  await context.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (_) {}
  });
  const page = await context.newPage();
  page.setDefaultTimeout(90_000);
  try {
    await login(page, base);
    await gotoOnboardingStep1(page, base, { strictNoError });
    if (!prepareSuggestions) {
      const { input, legacy } = await addressInput(page);
      if (!legacy && (await input.inputValue()).trim().length > 0) {
        await input.fill('');
        await input.blur();
        await page.waitForTimeout(400);
      }
    }
    let showSuggestionsFrame = suggestionsFrame;
    if (prepareSuggestions) {
      const { legacy } = await prepareAddressSuggestionsShot(page);
      if (!allowLegacy && legacy) {
        throw new Error('Expected KbAddressField on after capture');
      }
      showSuggestionsFrame = !legacy;
    }
    await screenshotStep(page, outPath, { suggestions: showSuggestionsFrame });
  } finally {
    await context.close();
  }
}

async function captureLiveWizard(outDir, base, { allowLegacy = true, strictNoError = true } = {}) {
  ensureDir(outDir);
  const browser = await chromium.launch({ headless: true });
  for (const vp of viewports) {
    await captureOneShot(browser, base, vp, path.join(outDir, `even-kennismaken-${vp.tag}.png`), {
      strictNoError,
      prepareSuggestions: false,
      allowLegacy,
      suggestionsFrame: false,
    });
    await captureOneShot(
      browser,
      base,
      vp,
      path.join(outDir, `even-kennismaken-suggesties-${vp.tag}.png`),
      {
        strictNoError,
        prepareSuggestions: true,
        allowLegacy,
        suggestionsFrame: true,
      },
    );
  }
  await browser.close();
}

const mode = process.env.CAPTURE_MODE || 'all';
if (mode === 'before' || mode === 'all') {
  await captureLiveWizard(path.join(storeRoot, 'before'), accBase, {
    allowLegacy: true,
    strictNoError: false,
  });
}
if (mode === 'after' || mode === 'all') {
  await captureLiveWizard(path.join(storeRoot, 'after'), afterBase, {
    allowLegacy: false,
    strictNoError: true,
  });
}
