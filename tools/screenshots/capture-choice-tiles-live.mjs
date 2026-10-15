import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import path from 'path';

const base = process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5201';
const outDir = process.env.OUT_DIR || '/opt/cursor/artifacts/screenshots';
const password = process.env.JOBSY_E2E_PASSWORD || 'Jobsy123!';
const email = process.env.JOBSY_E2E_EMAIL || 'kandidaat@jobsy.local';

const conn =
  process.env.ConnectionStrings__JobsyDb ||
  'Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres';

function sql(query) {
  if (!conn.includes('127.0.0.1') && !conn.includes('localhost')) {
    return;
  }
  execSync(
    `PGPASSWORD=postgres psql -h 127.0.0.1 -U postgres -d JobsyCi -v ON_ERROR_STOP=1 -c "${query.replace(/"/g, '\\"')}"`,
    { stdio: 'inherit' },
  );
}

function prepareDiscoveryStep(step) {
  sql(`
UPDATE "Users" SET "TestAiConsentAt"=COALESCE("TestAiConsentAt", NOW()), "TestAiConsentVersion"='2026-09-26'
WHERE lower("Email")=lower('${email.replace(/'/g, "''")}');
`);
  const completed = Math.max(0, step - 1);
  // Minimal steps json: mark prior steps completed via analytics is complex; discovery URL stap= works with journey state API from seeded user.
  sql(`
UPDATE "CandidateOnboardings" o SET "CurrentStep"=${step}, "FinishReached"=false, "CompletedAtUtc"=NULL
FROM "Users" u WHERE o."UserId"=u."Id" AND lower(u."Email")=lower('${email.replace(/'/g, "''")}');
`);
}

function prepareOnboardingStep(step) {
  const esc = email.replace(/'/g, "''");
  sql(`
UPDATE "Users" SET "TestAiConsentAt"=COALESCE("TestAiConsentAt", NOW()), "TestAiConsentVersion"='2026-09-26'
WHERE lower("Email")=lower('${esc}');
UPDATE "CandidateOnboardings" o SET "CurrentStep"=${step}, "FinishReached"=false, "CompletedAtUtc"=NULL,
  "StartedAtUtc"=COALESCE("StartedAtUtc", NOW()), "WizardVersion"=3
FROM "Users" u WHERE o."UserId"=u."Id" AND lower(u."Email")=lower('${esc}');
`);
}

async function login(page) {
  await page.goto(`${base}/login`, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  if (await page.locator('input[name="email"]').count()) {
    await page.fill('input[name="email"]', email);
    await page.fill('input[name="password"]', password);
    await Promise.all([
      page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 60_000 }),
      page.click('button.login-submit'),
    ]);
  }
}

async function waitJourneyReady(page) {
  await page.waitForFunction(
    () => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready',
    null,
    { timeout: 90_000 },
  );
}

async function resetScroll(page) {
  await page.evaluate(() => {
    window.scrollTo(0, 0);
    const stage = document.querySelector('.journey-stage');
    if (stage) stage.scrollTop = 0;
    const main = document.querySelector('.app-main');
    if (main) main.scrollTop = 0;
  });
  await page.waitForTimeout(350);
}

async function measureFit(page) {
  return page.evaluate(() => {
    const stage = document.querySelector('.journey-stage');
    const foot = document.querySelector('.journey-step__foot .btn-primary, .journey-step__foot [data-testid="onboarding-next"]');
    const footBox = foot?.getBoundingClientRect();
    const vh = window.innerHeight;
    return {
      stageScrollOverflow: stage ? stage.scrollHeight - stage.clientHeight : null,
      docScrollOverflow: document.documentElement.scrollHeight - vh,
      footBottom: footBox ? Math.round(footBox.bottom) : null,
      viewportHeight: vh,
      footVisible: footBox ? footBox.bottom <= vh && footBox.top >= 0 : false,
      ellipsisTiles: [...document.querySelectorAll('.lobsy-choice-tile__label')].filter((el) => {
        const s = getComputedStyle(el);
        return s.textOverflow === 'ellipsis' && el.scrollWidth > el.clientWidth + 1;
      }).length,
    };
  });
}

async function assertNoEllipsis(page) {
  const bad = await page.evaluate(() =>
    [...document.querySelectorAll('.lobsy-choice-tile__label, .lobsy-choice-tile__stack strong')]
      .filter((el) => el.scrollWidth > el.clientWidth + 1)
      .map((el) => el.textContent?.trim().slice(0, 24)),
  );
  if (bad.length) {
    console.warn('Possible clipped labels:', bad);
  }
}

async function selectStep6Dislikes(page) {
  for (const label of ['Nachtdiensten', 'Lawaai']) {
    const tile = page.locator('.journey-chips .lobsy-choice-tile', { hasText: label }).first();
    const pressed = await tile.getAttribute('aria-pressed');
    if (pressed !== 'true') {
      await tile.click();
    }
  }
  const selected = await page.$$eval('.journey-chips .lobsy-choice-tile[aria-pressed="true"]', (els) =>
    els.map((el) => el.textContent?.trim().split('\n')[0]),
  );
  console.log('step6-selected', JSON.stringify(selected));
}

async function gotoOnboardingStep(page, step) {
  prepareOnboardingStep(step);
  await page.goto(`${base}/candidate/start`, { waitUntil: 'networkidle' });
  if (!(await page.locator('.ob-wizard__presets .lobsy-choice-tile, .ob-wizard__start-grid .lobsy-choice-tile, .work-status-chip').count())) {
    await page.reload({ waitUntil: 'networkidle' });
  }
  await page.waitForTimeout(600);
}

async function measureOnboardingTileWidths(page) {
  const result = await page.evaluate(() => {
    const check = (selector) => {
      const grid = document.querySelector(selector);
      if (!grid) return { error: `missing ${selector}` };
      const gr = grid.getBoundingClientRect();
      const gs = getComputedStyle(grid);
      const gap = parseFloat(gs.gap) || 8;
      const minExpected = (gr.width - gap) / 2 - 2;
      const tiles = [...grid.querySelectorAll(':scope > .lobsy-choice-tile')].map((t) => ({
        label: t.textContent?.trim().split('\n')[0],
        width: Math.round(t.getBoundingClientRect().width),
      }));
      return {
        gridWidth: Math.round(gr.width),
        gap,
        gridTemplateColumns: gs.gridTemplateColumns,
        minExpectedTileWidth: Math.round(minExpected * 10) / 10,
        tiles,
        pass: tiles.every((t) => t.width >= minExpected - 1),
      };
    };
    return {
      presets: check('.ob-wizard__presets'),
      start: check('.ob-wizard__start-grid'),
    };
  });
  console.log('onboarding-tile-widths', JSON.stringify(result));
  for (const key of ['presets', 'start']) {
    if (!result[key]?.pass) {
      throw new Error(`Onboarding ${key} tile width assertion failed: ${JSON.stringify(result[key])}`);
    }
  }
  return result;
}

async function shot(page, name, fitLabel, options = {}) {
  if (!options.skipReset) {
    await resetScroll(page);
  }
  const fit = await measureFit(page);
  if (fitLabel) {
    console.log(fitLabel, JSON.stringify(fit));
  }
  await assertNoEllipsis(page);
  const file = path.join(outDir, name);
  await page.screenshot({ path: file, fullPage: options.fullPage === true });
  console.log('wrote', file);
}

const shots = [
  {
    name: 'choice-tiles-discovery-stap6-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare() {
      prepareDiscoveryStep(6);
    },
    async run(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=6`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
      await selectStep6Dislikes(page);
      await shot(page, this.name, 'fit-stap6-390');
    },
  },
  {
    name: 'choice-tiles-discovery-stap6-360x740.png',
    width: 360,
    height: 740,
    mobile: true,
    async prepare() {
      prepareDiscoveryStep(6);
    },
    async run(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=6`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
      await selectStep6Dislikes(page);
      await shot(page, this.name, 'fit-stap6-360');
    },
  },
  {
    name: 'choice-tiles-discovery-stap6-1440x900.png',
    width: 1440,
    height: 900,
    mobile: false,
    async prepare() {
      prepareDiscoveryStep(6);
    },
    async run(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=6`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
      await selectStep6Dislikes(page);
      await shot(page, this.name, 'fit-stap6-desktop');
    },
  },
  {
    name: 'choice-tiles-discovery-stap5-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    optional: true,
    async prepare() {
      prepareDiscoveryStep(5);
    },
    async run(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=5`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
      await shot(page, this.name);
    },
  },
  {
    name: 'choice-tiles-discovery-stap3-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    optional: true,
    async prepare() {
      prepareDiscoveryStep(3);
    },
    async run(page) {
      await page.goto(`${base}/candidate/ontdekkingsreis?stap=3`, { waitUntil: 'domcontentloaded' });
      await waitJourneyReady(page);
      await page.waitForSelector('.lobsy-choice-tile', { timeout: 60_000 });
      await shot(page, this.name);
    },
  },
  {
    name: 'choice-tiles-onboarding-step2-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare() {
      prepareOnboardingStep(2);
    },
    async run(page) {
      await page.goto(`${base}/candidate/start`, { waitUntil: 'domcontentloaded' });
      await page.waitForSelector('.ob-wizard__presets .lobsy-choice-tile', { timeout: 60_000 });
      await page.waitForSelector('text=Kies wat bij je past', { timeout: 60_000 });
      await resetScroll(page);
      await page.locator('.ob-wizard__start-options').scrollIntoViewIfNeeded();
      await page.waitForTimeout(300);
      await shot(page, this.name, null, { fullPage: true, skipReset: true });
    },
  },
  {
    name: 'choice-tiles-onboarding-education-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    optional: true,
    async prepare() {
      prepareOnboardingStep(5);
    },
    async run(page) {
      await page.goto(`${base}/candidate/start`, { waitUntil: 'domcontentloaded' });
      await page.waitForSelector('.ob-wizard__education-grid .lobsy-choice-tile', { timeout: 60_000 });
      await shot(page, this.name);
    },
  },
  {
    name: 'choice-tiles-onboarding-workstatus-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare() {
      prepareOnboardingStep(1);
    },
    async run(page) {
      await page.goto(`${base}/candidate/start`, { waitUntil: 'domcontentloaded' });
      await page.waitForSelector('.work-status-chip', { timeout: 60_000 });
      await shot(page, this.name);
    },
  },
  {
    name: 'choice-tiles-onboarding-presets-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    round5: true,
    async run(page) {
      await gotoOnboardingStep(page, 2);
      await page.waitForSelector('.ob-wizard__presets .lobsy-choice-tile', { timeout: 60_000 });
      await measureOnboardingTileWidths(page);
      await resetScroll(page);
      await page.locator('.ob-wizard__presets').scrollIntoViewIfNeeded();
      await shot(page, this.name, null, { skipReset: true });
    },
  },
  {
    name: 'choice-tiles-onboarding-start-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    round5: true,
    async run(page) {
      await gotoOnboardingStep(page, 2);
      await page.waitForSelector('.ob-wizard__start-grid .lobsy-choice-tile', { timeout: 60_000 });
      await measureOnboardingTileWidths(page);
      await page.locator('.ob-wizard__start-options').scrollIntoViewIfNeeded();
      await page.waitForTimeout(300);
      await shot(page, this.name, null, { skipReset: true });
    },
  },
  {
    name: 'choice-tiles-paspoort-coach-motivation-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async run(page) {
      await page.goto(`${base}/candidate/paspoort?tab=data`, { waitUntil: 'domcontentloaded' });
      await page.waitForFunction(
        () => {
          const panel = document.querySelector('#passport-panel-data');
          return panel && !panel.hasAttribute('hidden');
        },
        null,
        { timeout: 60_000 },
      );
      await page.waitForSelector('.passport-data', { timeout: 60_000 });
      const motivationToggle = page
        .locator('button.passport-data__toggle', { hasText: /motivatie/i })
        .first();
      await page.waitForSelector('.passport-data__accordions', { timeout: 60_000 });
      await motivationToggle.click({ timeout: 60_000 });
      await page.waitForSelector('#passport-data-panel-motivation', { timeout: 60_000 });
      await page.locator('#passport-data-panel-motivation textarea').first().scrollIntoViewIfNeeded();
      await page.waitForTimeout(450);
      await shot(page, this.name, null, { skipReset: true });
    },
  },
  {
    name: 'choice-tiles-paspoort-bottom-export-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async run(page) {
      await page.goto(`${base}/candidate/paspoort?tab=data`, { waitUntil: 'domcontentloaded' });
      await page.waitForSelector('#passport-panel-data:not([hidden])', { timeout: 60_000 });
      await page.waitForSelector('.passport-data__accordions', { timeout: 60_000 });
      const exportRow = page.locator('[data-testid="passport-download-data"]').first();
      await exportRow.scrollIntoViewIfNeeded();
      await page.waitForTimeout(400);
      const coachVisible = await page.locator('.lobsy-coach-dock__btn').isVisible().catch(() => false);
      console.log('paspoort-bottom-coach-visible', coachVisible);
      await shot(page, this.name, null, { skipReset: true });
    },
  },
  {
    name: 'choice-tiles-paspoort-tabs-coach-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    round5: true,
    async run(page) {
      await page.goto(`${base}/candidate/paspoort?tab=dna`, { waitUntil: 'domcontentloaded' });
      await page.waitForSelector('.passport-tabs', { timeout: 60_000 });
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await page.waitForTimeout(400);
      await shot(page, this.name, null, { skipReset: true });
    },
  },
  {
    name: 'choice-tiles-profile-dislikes-390x844.png',
    width: 390,
    height: 844,
    mobile: true,
    async prepare() {
      prepareDiscoveryStep(6);
    },
    async run(page) {
      await page.goto(`${base}/candidate/paspoort?tab=data`, { waitUntil: 'domcontentloaded' });
      await page.waitForFunction(
        () => {
          const panel = document.querySelector('#passport-panel-data');
          return panel && !panel.hasAttribute('hidden');
        },
        null,
        { timeout: 60_000 },
      );
      await page.waitForSelector('.passport-data', { timeout: 60_000 });
      const dislikesToggle = page.locator('#passport-data-btn-dislikes, button.passport-data__toggle', {
        hasText: /niet van/i,
      }).first();
      await dislikesToggle.scrollIntoViewIfNeeded();
      await dislikesToggle.click();
      await page.waitForSelector('#passport-data-panel-dislikes .profile-chip-grid .lobsy-choice-tile', {
        timeout: 60_000,
      });
      await page.evaluate(() => {
        const panel = document.querySelector('#passport-data-panel-dislikes');
        if (!panel) return;
        const y = panel.getBoundingClientRect().top + window.scrollY - 72;
        window.scrollTo(0, Math.max(0, y));
      });
      await page.waitForTimeout(400);
      await shot(page, this.name, null, { skipReset: true });
    },
  },
];

const browser = await chromium.launch();
const context = await browser.newContext({
  viewport: { width: 390, height: 844 },
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
await login(page);

const round4Names = new Set([
  'choice-tiles-discovery-stap6-390x844.png',
  'choice-tiles-discovery-stap6-360x740.png',
  'choice-tiles-discovery-stap6-1440x900.png',
  'choice-tiles-onboarding-step2-390x844.png',
  'choice-tiles-profile-dislikes-390x844.png',
  'choice-tiles-onboarding-workstatus-390x844.png',
]);

const round5Names = new Set([
  'choice-tiles-discovery-stap6-390x844.png',
  'choice-tiles-discovery-stap6-360x740.png',
  'choice-tiles-onboarding-presets-390x844.png',
  'choice-tiles-onboarding-start-390x844.png',
  'choice-tiles-profile-dislikes-390x844.png',
  'choice-tiles-paspoort-tabs-coach-390x844.png',
]);

const polishNames = new Set([
  'choice-tiles-discovery-stap6-390x844.png',
  'choice-tiles-discovery-stap6-360x740.png',
]);

const polish2Names = new Set([
  'choice-tiles-discovery-stap6-390x844.png',
  'choice-tiles-discovery-stap6-360x740.png',
  'choice-tiles-paspoort-coach-motivation-390x844.png',
]);

const polish3Names = new Set([
  'choice-tiles-paspoort-coach-motivation-390x844.png',
  'choice-tiles-paspoort-bottom-export-390x844.png',
]);

const activeShots =
  process.env.SHOT_SET === 'polish3'
    ? shots.filter((s) => polish3Names.has(s.name))
    : process.env.SHOT_SET === 'polish2'
      ? shots.filter((s) => polish2Names.has(s.name))
      : process.env.SHOT_SET === 'polish'
      ? shots.filter((s) => polishNames.has(s.name))
      : process.env.SHOT_SET === 'round5'
      ? shots.filter((s) => round5Names.has(s.name))
      : process.env.SHOT_SET === 'round4'
        ? shots.filter((s) => round4Names.has(s.name))
        : shots.filter((s) => !s.optional || process.env.FULL_SHOTS === '1');

for (const s of activeShots) {
  if (s.prepare && !s.round5) {
    s.prepare();
  }
  await page.setViewportSize({ width: s.width, height: s.height });
  await s.run(page);
}
await context.close();
await browser.close();
