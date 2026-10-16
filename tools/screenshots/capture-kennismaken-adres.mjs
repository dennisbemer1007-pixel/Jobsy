import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../..');
const storeRoot =
  process.env.KENNISMAKEN_STORE || '/cursor/stores/self/kennismaken-adres';
const accBase = (process.env.ACCEPTATIE_BASE_URL || 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const email = process.env.JOBSY_E2E_EMAIL || 'kandidaat@jobsy.local';
const password = process.env.JOBSY_E2E_PASSWORD || 'Jobsy123!';

const viewports = [
  { w: 390, h: 844, tag: 'mobile' },
  { w: 1280, h: 800, tag: 'desktop' },
];

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
  await page.waitForFunction(
    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready',
    null,
    { timeout: 90_000 },
  );
}

async function gotoOnboardingStep1(page, base) {
  await page.goto(`${base}/candidate/start`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await waitCircuitReady(page);
  await page.waitForSelector('.ob-fields h1, .ob-wizard__welcome', { timeout: 90_000 });
  const welcome = page.locator('.ob-wizard__welcome button, .ob-wizard__welcome .btn-primary').first();
  if (await welcome.isVisible().catch(() => false)) {
    await welcome.click();
    await page.waitForSelector('.ob-fields', { timeout: 60_000 });
    await waitCircuitReady(page);
  }
  await page.waitForSelector('.ob-fields', { timeout: 60_000 });
  await page.locator('.ob-fields h1').first().waitFor({ timeout: 60_000 });
  await page.waitForFunction(
    () => !document.body?.textContent?.includes('Laden…') && !document.body?.textContent?.includes('Laden...'),
    null,
    { timeout: 90_000 },
  ).catch(() => {});
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
    // Acceptatie (pre-PR): postcode-only field; geocoder resolves inline (no dropdown).
    await input.click();
    await input.fill('2671GB');
    await page.waitForTimeout(2000);
    await page.locator('.ob-fields h1').waitFor({ state: 'visible', timeout: 60_000 });
    return { legacy: true };
  }
  await input.fill('');
  await input.fill('Kerkstraat Naaldwijk');
  await page.waitForTimeout(900);
  const list = page.locator('.address-suggestions');
  await list.waitFor({ state: 'visible', timeout: 20_000 });
  if ((await page.locator('.address-suggestions__item').count()) < 1) {
    throw new Error('Expected address suggestions to be visible');
  }
  return { legacy: false };
}

async function captureAcceptatieBefore(outDir) {
  ensureDir(outDir);
  const browser = await chromium.launch({ headless: true });
  for (const vp of viewports) {
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
    await login(page, accBase);
    // Suggesties first: on mobile acceptatie, a prior screenshot + postcode fill drops the circuit.
    await gotoOnboardingStep1(page, accBase);
    const { legacy } = await prepareAddressSuggestionsShot(page);
    await screenshotStep(page, path.join(outDir, `even-kennismaken-suggesties-${vp.tag}.png`), {
      suggestions: !legacy,
    });
    await gotoOnboardingStep1(page, accBase);
    await screenshotStep(page, path.join(outDir, `even-kennismaken-${vp.tag}.png`));
    await context.close();
  }
  await browser.close();
}

function startStaticServer(wwwroot, port) {
  const server = http.createServer((req, res) => {
    const urlPath = decodeURIComponent((req.url || '/').split('?')[0]);
    const filePath = path.join(wwwroot, urlPath === '/' ? 'index.html' : urlPath);
    if (!filePath.startsWith(wwwroot)) {
      res.writeHead(403);
      res.end();
      return;
    }
    fs.readFile(filePath, (err, data) => {
      if (err) {
        res.writeHead(404);
        res.end('not found');
        return;
      }
      const ext = path.extname(filePath);
      const type =
        ext === '.css'
          ? 'text/css'
          : ext === '.html'
            ? 'text/html'
            : 'application/octet-stream';
      res.writeHead(200, { 'Content-Type': type });
      res.end(data);
    });
  });
  return new Promise((resolve, reject) => {
    server.listen(port, '127.0.0.1', () => resolve(server));
    server.on('error', reject);
  });
}

function dotnetExportHtml(exportDir, staticBase) {
  const env = {
    ...process.env,
    PATH: `${process.env.HOME}/.dotnet:${process.env.PATH}`,
    PERSONAL_STEP_HTML_DIR: exportDir,
    PERSONAL_STEP_STATIC_BASE: staticBase,
  };
  execSync(
    'dotnet test Jobsy.Tests/Jobsy.Tests.csproj --filter FullyQualifiedName~PersonalStepScreenshotHarnessTests.Export_current --no-build',
    { cwd: repoRoot, env, stdio: 'inherit' },
  );
}

async function captureStyledAfter(outDir, port = 8765) {
  ensureDir(outDir);
  const wwwroot = path.join(repoRoot, 'Jobsy.Web/wwwroot');
  const exportDir = path.join(wwwroot, 'kennismaken-export');
  ensureDir(exportDir);
  const staticBase = `http://127.0.0.1:${port}`;
  execSync('dotnet build Jobsy.sln -v q', {
    cwd: repoRoot,
    env: { ...process.env, PATH: `${process.env.HOME}/.dotnet:${process.env.PATH}` },
    stdio: 'inherit',
  });
  dotnetExportHtml(exportDir, staticBase);
  const server = await startStaticServer(wwwroot, port);
  const browser = await chromium.launch({ headless: true });
  try {
    for (const vp of viewports) {
      const context = await browser.newContext({
        viewport: { width: vp.w, height: vp.h },
        isMobile: vp.w < 600,
        hasTouch: vp.w < 600,
      });
      const page = await context.newPage();
      await page.goto(`${staticBase}/kennismaken-export/personal-step.html`, {
        waitUntil: 'networkidle',
      });
      await page.waitForSelector('.kb-address__field input', { timeout: 30_000 });
      await screenshotStep(page, path.join(outDir, `even-kennismaken-${vp.tag}.png`));
      await page.goto(`${staticBase}/kennismaken-export/personal-step-suggestions.html`, {
        waitUntil: 'networkidle',
      });
      await page.waitForSelector('.address-suggestions', { state: 'visible', timeout: 30_000 });
      await screenshotStep(page, path.join(outDir, `even-kennismaken-suggesties-${vp.tag}.png`), {
        suggestions: true,
      });
      await context.close();
    }
  } finally {
    await browser.close();
    server.close();
  }
}

const mode = process.env.CAPTURE_MODE || 'all';
if (mode === 'before' || mode === 'all') {
  await captureAcceptatieBefore(path.join(storeRoot, 'before'));
}
if (mode === 'after' || mode === 'all') {
  await captureStyledAfter(path.join(storeRoot, 'after'));
}
