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
  { stap: '5', name: 'ontdekkingsreis-step5-mobile-360x780.png', width: 360, height: 780 },
  { stap: '5', name: 'ontdekkingsreis-step5-mobile-390x844.png', width: 390, height: 844 },
  { stap: '6', name: 'ontdekkingsreis-step6-mobile-360x780.png', width: 360, height: 780 },
  { stap: '6', name: 'ontdekkingsreis-step6-mobile-390x844.png', width: 390, height: 844 },
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
  await page.waitForSelector('.journey-chips .journey-chip', { timeout: 60_000 });
  await page.evaluate(() => {
    window.scrollTo(0, 0);
    const stage = document.querySelector('.journey-stage');
    if (stage) stage.scrollTop = 0;
    const main = document.querySelector('.app-main');
    if (main) main.scrollTop = 0;
  });
  await page.waitForTimeout(400);
  const layout = await page.evaluate(() => {
    const chips = [...document.querySelectorAll('.journey-chips .journey-chip')].slice(0, 4);
    const rect = document.querySelector('.journey-chips')?.getBoundingClientRect();
    return chips.map((c) => {
      const r = c.getBoundingClientRect();
      return {
        text: c.textContent.trim().slice(0, 18),
        left: Math.round(r.left - (rect?.left ?? 0)),
        top: Math.round(r.top - (rect?.top ?? 0)),
        w: Math.round(r.width),
        nowrap: getComputedStyle(c).whiteSpace,
      };
    });
  });
  console.log(shot.name, layout);
  await page.screenshot({ path: path.join(outDir, shot.name), fullPage: false });
  console.log('wrote', path.join(outDir, shot.name));
  await context.close();
}
await browser.close();
