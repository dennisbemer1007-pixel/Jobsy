import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const base = process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5201';
const outDir = process.env.OUT_DIR || '/opt/cursor/artifacts/screenshots';
const password = process.env.JOBSY_E2E_PASSWORD || 'Jobsy123!';
const email = process.env.JOBSY_E2E_EMAIL || 'kandidaat@jobsy.local';

const conn =
  process.env.ConnectionStrings__JobsyDb ||
  'Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres';

function prepareDb() {
  if (!conn.includes('127.0.0.1') && !conn.includes('localhost')) {
    console.warn('Skipping DB fixture (non-local ConnectionStrings__JobsyDb).');
    return;
  }
  execSync(
    `PGPASSWORD=postgres psql -h 127.0.0.1 -U postgres -d JobsyCi -v ON_ERROR_STOP=1 -c "
UPDATE \\"Users\\" SET \\"TestAiConsentAt\\"=COALESCE(\\"TestAiConsentAt\\", NOW()), \\"TestAiConsentVersion\\"='2026-09-26'
WHERE lower(\\"Email\\")=lower('${email.replace(/'/g, "''")}');
DELETE FROM \\"CandidateCareerInterests\\" WHERE \\"UserId\\" IN (SELECT \\"Id\\" FROM \\"Users\\" WHERE lower(\\"Email\\")=lower('${email.replace(/'/g, "''")}'));
"`,
    { stdio: 'inherit' },
  );
}

prepareDb();

const shots = [
  { stap: '6', name: 'ontdekkingsreis-step6-mobile-360x780.png', width: 360, height: 780 },
  { stap: '6', name: 'ontdekkingsreis-step6-mobile-390x844.png', width: 390, height: 844 },
  { stap: '8', name: 'ontdekkingsreis-step8-mobile-360x780.png', width: 360, height: 780 },
  { stap: '8', name: 'ontdekkingsreis-step8-mobile-390x844.png', width: 390, height: 844 },
];

const browser = await chromium.launch();
for (const shot of shots) {
  const context = await browser.newContext({
    viewport: { width: shot.width, height: shot.height },
    isMobile: true,
    hasTouch: true,
  });
  await context.addInitScript(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (e) {}
  });
  const page = await context.newPage();
  page.setDefaultTimeout(90_000);
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  if (await page.locator('input[name="email"]').count()) {
    await page.fill('input[name="email"]', email);
    await page.fill('input[name="password"]', password);
    await Promise.all([
      page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 60_000 }),
      page.click('button.login-submit'),
    ]);
  }
  await page.goto(`${base}/candidate/ontdekkingsreis?stap=${shot.stap}`, {
    waitUntil: 'domcontentloaded',
    timeout: 120_000,
  });
  await page.waitForFunction(
    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready',
    null,
    { timeout: 90_000 },
  );
  if (shot.stap === '8') {
    await page.waitForSelector('.journey-step .test-flow--compact', { timeout: 60_000 });
  } else {
    await page.waitForSelector('.journey-step__eyebrow', { timeout: 60_000 });
  }
  await page.evaluate(() => {
    window.scrollTo(0, 0);
    const stage = document.querySelector('.journey-stage');
    if (stage) stage.scrollTop = 0;
    const main = document.querySelector('.app-main');
    if (main) main.scrollTop = 0;
  });
  await page.waitForTimeout(400);
  const layout = await page.evaluate(() => {
    const mprog = document.querySelector('.journey-mprog');
    const main = document.querySelector('.app-main');
    const stage = document.querySelector('.journey-stage');
    const rect = (el) => (el ? el.getBoundingClientRect() : null);
    return {
      scrollY: window.scrollY,
      mainScroll: main?.scrollTop ?? null,
      stageScroll: stage?.scrollTop ?? null,
      mprogTop: rect(mprog)?.top ?? null,
      mprogHeight: rect(mprog)?.height ?? null,
    };
  });
  console.log(shot.name, layout);
  await page.screenshot({ path: path.join(outDir, shot.name), fullPage: false });
  console.log('wrote', path.join(outDir, shot.name));
  await context.close();
}
await browser.close();
