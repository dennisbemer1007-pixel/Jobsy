const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const outDir = path.join(__dirname, 'screenshots');
fs.mkdirSync(outDir, { recursive: true });

const BASE = 'http://localhost:5201';
const PASSWORD = 'Jobsy123!';

const roles = [
  {
    id: 'kandidaat',
    email: 'kandidaat@jobsy.local',
    shots: [
      { name: '01-kandidaat-home', url: '/home' },
      { name: '01-kandidaat-banenkaart', url: '/' },
      { name: '01-kandidaat-sollicitaties', url: '/candidate/applications' },
      { name: '01-kandidaat-profiel', url: '/candidate/profile' },
    ],
  },
  {
    id: 'filiaalmanager',
    email: 'ondernemer@jobsy.local',
    shots: [
      { name: '02-filiaal-home', url: '/home' },
      { name: '02-filiaal-vacatures', url: '/employer/vacancies' },
      { name: '02-filiaal-sollicitanten', url: '/branch/applicants' },
      { name: '02-filiaal-tokens', url: '/branch/tokens' },
    ],
  },
  {
    id: 'regiomanager',
    email: 'regio@jobsy.local',
    shots: [
      { name: '03-regio-home', url: '/home' },
      { name: '03-regio-vacatures', url: '/employer/vacancies' },
      { name: '03-regio-vestigingen', url: '/regional/branches' },
    ],
  },
  {
    id: 'enterprise',
    email: 'enterprise@jobsy.local',
    shots: [
      { name: '04-enterprise-home', url: '/home' },
      { name: '04-enterprise-vacatures', url: '/employer/vacancies' },
      { name: '04-enterprise-tokens', url: '/employer/tokens' },
      { name: '04-enterprise-gebruikers', url: '/employer/users' },
      { name: '04-enterprise-salaristabellen', url: '/employer/salary-tables' },
    ],
  },
  {
    id: 'intermediair',
    email: 'intermediair@jobsy.local',
    shots: [
      { name: '05-intermediair-home', url: '/home' },
      { name: '05-intermediair-bedrijvenoverzicht', url: '/intermediary' },
      { name: '05-intermediair-tokens', url: '/employer/tokens' },
    ],
  },
  {
    id: 'admin',
    email: 'admin@jobsy.local',
    shots: [
      { name: '06-admin-home', url: '/home' },
      { name: '06-admin-bedrijven', url: '/admin/companies' },
      { name: '06-admin-financieel', url: '/admin/finance' },
      { name: '06-admin-settings', url: '/admin/settings' },
      { name: '06-admin-vacatures', url: '/admin/vacancies' },
    ],
  },
];

async function dismissGeo(page) {
  const later = page.getByRole('button', { name: 'Later' });
  if (await later.isVisible({ timeout: 2000 }).catch(() => false)) {
    await later.click().catch(() => {});
    await page.waitForTimeout(300);
  }
}

async function waitForApp(page) {
  await page.waitForFunction(() => {
    const t = (document.body?.innerText || '').trim();
    return t.length > 20 && !/^Not found$/i.test(t);
  }, { timeout: 25000 });
  await page.waitForTimeout(800);
}

async function login(page, email) {
  await page.goto(`${BASE}/account/logout`, { waitUntil: 'networkidle' });
  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
  await page.waitForSelector('input[name="email"]', { timeout: 20000 });
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', PASSWORD);
  await Promise.all([
    page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 }),
    page.click('button.login-submit'),
  ]);
  await waitForApp(page);
  console.log('logged in', email, page.url());
}

async function shot(page, name) {
  await dismissGeo(page);
  await page.waitForTimeout(400);
  await page.screenshot({ path: path.join(outDir, `${name}.png`), fullPage: false });
  console.log('saved', name);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: 'nl-NL',
  });
  const page = await context.newPage();

  await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
  await waitForApp(page);
  await dismissGeo(page);
  await shot(page, '00-banenkaart-publiek');

  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
  await page.waitForSelector('input[name="email"]', { timeout: 20000 });
  await shot(page, '00-login');

  for (const role of roles) {
    console.log('---', role.id);
    await login(page, role.email);
    for (const s of role.shots) {
      await page.goto(`${BASE}${s.url}`, { waitUntil: 'networkidle' });
      await waitForApp(page);
      await shot(page, s.name);
    }
  }

  await login(page, 'kandidaat@jobsy.local');
  await page.goto(`${BASE}/`, { waitUntil: 'networkidle' });
  await waitForApp(page);
  await dismissGeo(page);
  const firstView = page.getByRole('link', { name: 'Bekijk' }).first();
  if (await firstView.isVisible().catch(() => false)) {
    await firstView.click();
    await page.waitForLoadState('networkidle');
    await waitForApp(page);
    await shot(page, '01-kandidaat-vacature-detail');
  }

  await browser.close();
  console.log('Done.');
})().catch((err) => {
  console.error(err);
  process.exit(1);
});
