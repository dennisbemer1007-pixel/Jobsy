/**
 * Diagnose external vacancy load: timing, console, network.
 * LOBSY_BASE_URL=http://127.0.0.1:5201 node tools/screenshots/diagnose-ext-vac-load.mjs
 */
import { chromium } from 'playwright';

const baseUrl = (process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const id = process.env.PR755_EXT_VACANCY_ID || 'eeeeeeee-2222-2222-2222-222222222222';

const logs = [];
const failedRequests = [];

async function main() {
  const browser = await chromium.launch({ headless: true });
  for (const vp of [
    { w: 390, h: 844, tag: '390' },
    { w: 1440, h: 900, tag: '1440' },
  ]) {
    const context = await browser.newContext({ viewport: { width: vp.w, height: vp.h }, locale: 'nl-NL' });
    await context.addInitScript(() => {
      try {
        localStorage.setItem('Jobsy.CookieConsent', 'necessary');
      } catch {}
      document.documentElement.classList.add('cookie-consent-known');
    });
    const page = await context.newPage();
    page.on('console', (msg) => logs.push({ vp: vp.tag, type: msg.type(), text: msg.text() }));
    page.on('pageerror', (err) => logs.push({ vp: vp.tag, type: 'pageerror', text: String(err) }));
    page.on('requestfailed', (req) =>
      failedRequests.push({ vp: vp.tag, url: req.url(), failure: req.failure()?.errorText }),
    );

    const t0 = Date.now();
    await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded' });
    await page.locator('input[name="email"], input[type="email"]').first().fill('sanne@jobsy.local');
    await page.locator('input[name="password"], input[type="password"]').first().fill('Jobsy123!');
    await page.locator('form.au-form button[type="submit"], button[type="submit"]').first().click();
    await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 90_000 }).catch(() => {});

    const apiPattern = `**/api/candidate/external-vacancies/${id}**`;
    const navStart = Date.now();
    const responsePromise = page.waitForResponse(
      (r) => r.url().includes(`/api/candidate/external-vacancies/${id}`) && r.request().method() === 'GET',
      { timeout: 120_000 },
    ).catch((e) => ({ error: String(e) }));

    await page.goto(`${baseUrl}/candidate/external/${id}`, { waitUntil: 'domcontentloaded' });
    const circuitAt = Date.now();
    const circuit = await page
      .waitForSelector('[data-lobsy-circuit="ready"]', { timeout: 90_000 })
      .then(() => 'ready')
      .catch(() => page.getAttribute('html', 'data-lobsy-circuit'));

    const apiResp = await responsePromise;
    const apiAt = Date.now();

    let titleAt = null;
    try {
      await page.locator('.kb-ext__title').waitFor({ state: 'visible', timeout: 120_000 });
      titleAt = Date.now();
    } catch (e) {
      titleAt = `timeout: ${e.message}`;
    }

    const dom = await page.evaluate(() => ({
      loadingText: document.querySelector('.kb-ext .state-message')?.textContent?.trim(),
      title: document.querySelector('.kb-ext__title')?.textContent?.trim(),
      hasActions: !!document.querySelector('[data-testid="kb-ext-detail-actions"]'),
      circuit: document.documentElement.getAttribute('data-lobsy-circuit'),
      scrollY: window.scrollY,
    }));

    console.log(
      JSON.stringify(
        {
          viewport: vp.tag,
          ms: {
            total: Date.now() - t0,
            afterNav: apiAt - navStart,
            titleAfterNav: typeof titleAt === 'number' ? titleAt - navStart : titleAt,
          },
          circuit,
          circuitWaitMs: circuitAt - navStart,
          api:
            apiResp && 'status' in apiResp
              ? { status: apiResp.status(), ok: apiResp.ok() }
              : apiResp,
          dom,
        },
        null,
        2,
      ),
    );
    await context.close();
  }

  console.log('console', JSON.stringify(logs.slice(0, 30), null, 2));
  console.log('failedRequests', JSON.stringify(failedRequests, null, 2));
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
