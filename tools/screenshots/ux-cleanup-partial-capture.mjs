/**
 * Retake selected ux-cleanup after shots (register, lancering, onboarding step 8).
 * Requires API :5200 + Web :5201 in Development.
 */
import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const api = (process.env.API || 'http://127.0.0.1:5200').replace(/\/$/, '');
const outRoot = process.env.OUT || '/cursor/stores/self/ux-cleanup/after';
const variantQuery = process.env.LANDING_VARIANT === 'on' ? '?_variant=on' : '';

const consentInitScript = `
  try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
  document.documentElement.classList.add('cookie-consent-known');
`;

async function fetchEmailCode(email, staleCode = null) {
  for (let i = 0; i < 40; i++) {
    const res = await fetch(`${api}/api/e2e/last-email-code?email=${encodeURIComponent(email)}`);
    if (res.ok) {
      const data = await res.json();
      const code = data?.code ?? data?.Code;
      if (code && code !== staleCode) return String(code);
    }
    await new Promise((r) => setTimeout(r, 250));
  }
  throw new Error(`No email code for ${email}`);
}

async function dismissAssistant(page) {
  await page.evaluate(() => {
    document.querySelector('.lobsy-coach-dock__close')?.click();
    document.querySelector('.lobsy-assistant-tab__close')?.click();
  }).catch(() => {});
}

async function clickOnboardingNext(page) {
  await dismissAssistant(page);
  await page.locator('[data-testid=onboarding-next]').click({ force: true, timeout: 15_000 });
  await page.waitForTimeout(350);
}

async function completeMiniStep(page) {
  for (let q = 0; q < 5; q++) {
    await page.locator('.test-likert input[value="4"]').first().click({ force: true });
    await page.waitForTimeout(450);
    const next = page.locator('button.test-flow__next:not([disabled])').first();
    if (await next.count()) {
      await next.click({ force: true });
      await page.waitForTimeout(550);
    } else {
      await page.waitForTimeout(900);
    }
  }
}

async function reachOnboardingStep8(page) {
  const email = `ux.step8.${Date.now()}@jobsy.local`;
  await page.goto(`${base}/account-maken`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await page.locator('input[name=email], input#email').fill(email);
  await page.locator('input[name=firstName]').fill('Sanne');
  await page.locator('button.au-submit, button.login-submit').first().click({ timeout: 60_000 });
  await page.waitForURL('**/code**', { timeout: 60_000 });
  const code = await fetchEmailCode(email);
  await page.locator('input[name=code]').fill(code);
  await page.locator('button.login-submit').click();
  await page.waitForURL((u) => !u.toString().includes('/code'), { timeout: 90_000 });

  await page.goto(`${base}/candidate/start`, { waitUntil: 'domcontentloaded', timeout: 90_000 });
  await page.waitForTimeout(800);
  if (await page.locator('.ob-wizard__welcome').count()) {
    await page.getByRole('button', { name: 'Beginnen' }).click();
    await page.waitForTimeout(400);
  }

  await page.locator('.ob-fields input').nth(1).fill('Test');
  await page.locator('.ob-fields input[autocomplete=postal-code], .ob-fields input[inputmode=text]').first().fill('2671GB');
  await page.waitForTimeout(400);
  const suggestion = page.locator('.ob-wizard__suggestions button').first();
  if (await suggestion.count()) await suggestion.click();

  await page.locator('[data-testid=onboarding-birth-date] input').fill('2000-01-01');
  await clickOnboardingNext(page);
  await page.locator('.ob-wizard__presets button').first().click();
  await clickOnboardingNext(page);
  await page.locator('#onboarding-field-transport .ob-wizard__transport button').first().click();
  await page.locator('.ob-wizard__transport button').filter({ hasText: /^30$/ }).first().click();
  const skip = page.locator('button.ob-wizard__ghost').filter({ hasText: /Overslaan/i }).first();
  if (await skip.count()) await skip.click({ timeout: 8_000 });
  else await clickOnboardingNext(page);

  await page.waitForTimeout(300);
  await clickOnboardingNext(page);
  await page.locator('button').filter({ hasText: /^MBO$/i }).first().click({ timeout: 8_000 });
  await clickOnboardingNext(page);
  const unknown = page.locator('button.ob-wizard__ghost').filter({ hasText: /Weet ik nog niet/i }).first();
  if (await unknown.count()) await unknown.click({ timeout: 8_000 });
  else await clickOnboardingNext(page);

  await clickOnboardingNext(page);
  for (const cb of await page.locator('.ob-wizard__consent input:not(:checked)').all()) {
    await cb.check({ force: true });
  }
  await clickOnboardingNext(page);
  await page.waitForSelector('.test-likert', { timeout: 30_000 });

  // Step 7 mini test — complete to reach step 8 at Vraag 1 van 5.
  await completeMiniStep(page);
  await page.waitForFunction(
    () => {
      const step = parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10);
      return step >= 8;
    },
    { timeout: 90_000 },
  );
  await page.waitForSelector('.ob-wizard__test-tab.is-done', { timeout: 30_000 });
  await page.waitForSelector('.ob-wizard__test-tab.is-current', { timeout: 30_000 });
  await page.waitForSelector('.test-flow__counter', { timeout: 30_000 });
  await page.waitForFunction(
    () => {
      const counter = document.querySelector('.test-flow__counter')?.textContent || '';
      return /Vraag 1 van 5/.test(counter);
    },
    { timeout: 60_000 },
  );
  await page.waitForSelector('button.test-flow__next', { timeout: 15_000 });
  await page.locator('.test-flow__counter').scrollIntoViewIfNeeded();
  await page.evaluate(() => {
    window.scrollTo(0, 0);
    document.querySelector('.app-main')?.scrollTo(0, 0);
  });
  await page.waitForTimeout(400);
}

const browser = await chromium.launch({ headless: true });
await mkdir(outRoot, { recursive: true });

for (const [name, route, w, h] of [
  ['register', `/register${variantQuery}`, 1280, 800],
  ['register', `/register${variantQuery}`, 390, 844],
  ['lancering', `/lancering${variantQuery}`, 390, 844],
]) {
  const tag = w >= 1000 ? 'desktop' : 'mobile';
  const ctx = await browser.newContext({ viewport: { width: w, height: h } });
  await ctx.addInitScript(consentInitScript);
  const page = await ctx.newPage();
  await page.goto(`${base}${route}`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  const file = path.join(outRoot, `${name}-${tag}.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log('wrote', file);
  await ctx.close();
}

{
  const ctx = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true,
    hasTouch: true,
    locale: 'nl-NL',
  });
  await ctx.addInitScript(consentInitScript);
  const page = await ctx.newPage();
  await reachOnboardingStep8(page);
  await page.evaluate(() => window.scrollTo(0, 0));
  const file = path.join(outRoot, 'onboarding-step-8-mobile.png');
  await page.screenshot({ path: file, fullPage: false });
  console.log('wrote', file);
  await ctx.close();
}

await browser.close();
