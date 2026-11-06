import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const base = process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5201';
const outDir = process.env.OUT_DIR || '/opt/cursor/artifacts/screenshots';
const password = process.env.JOBSY_E2E_PASSWORD || 'Jobsy123!';
const email = process.env.JOBSY_E2E_EMAIL || 'kandidaat@jobsy.local';
const stap = process.env.JOBSY_STAP || '8';

const conn =
  process.env.ConnectionStrings__JobsyDb ||
  'Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres';

function prepareDb() {
  if (!conn.includes('127.0.0.1') && !conn.includes('localhost')) {
    console.warn('Skipping DB fixture (non-local ConnectionStrings__JobsyDb).');
    return;
  }
  // Local DB fixture (matches CandidateJourneyE2e helpers).
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
  { name: 'ontdekkingsreis-vraag-desktop-1280x800.png', width: 1280, height: 800, mobile: false },
  { name: 'ontdekkingsreis-vraag-desktop-1440x900.png', width: 1440, height: 900, mobile: false },
  { name: 'ontdekkingsreis-vraag-mobile-390x844.png', width: 390, height: 844, mobile: true },
];

const browser = await chromium.launch();
for (const shot of shots) {
  const context = await browser.newContext({
    viewport: { width: shot.width, height: shot.height },
    isMobile: shot.mobile,
    hasTouch: shot.mobile,
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
  await page.goto(`${base}/candidate/ontdekkingsreis?stap=${stap}`, {
    waitUntil: 'domcontentloaded',
    timeout: 120_000,
  });
  await page.waitForFunction(
    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready',
    null,
    { timeout: 90_000 },
  );
  await page.waitForSelector('.journey-step .test-flow--compact', { timeout: 60_000 });
  await page.waitForTimeout(400);
  await page.screenshot({ path: path.join(outDir, shot.name), fullPage: false });
  console.log('wrote', path.join(outDir, shot.name));
  await context.close();
}
await browser.close();
