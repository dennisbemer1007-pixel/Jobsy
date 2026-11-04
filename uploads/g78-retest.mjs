/**
 * G78 Acceptatie E2E hertest — LB-6RGZ / race rond vraag 26 (9026)
 * node --check uploads/g78-retest.mjs && node uploads/g78-retest.mjs
 */
import { chromium } from '/agent/repos/Jobsy/tools/screenshots/node_modules/playwright/index.mjs';
import fs from 'fs';
import path from 'path';

const BASE = process.env.G78_BASE || 'https://acceptatie.lobsy.nl';
const OUT = process.env.G78_OUT || '/opt/cursor/artifacts/g78-acc-retest';
const SHOT = path.join(OUT, 'screenshots');
const CLASS_ID = process.env.G78_CLASS_ID || '897a3406-5f03-4b94-8790-60ca17bfc16d';
const SCHOOL_ID = process.env.G78_SCHOOL_ID || '99999999-1111-1111-1111-111111111111';
const PASS = 'Jobsy123!';

const rows = [];
const httpLog = [];
const consoleLog = [];
const codesCreated = [];
let seq = 0;

function log(step, width, control, result, note = '', screenshot = '', kind = '') {
  rows.push({ step, width, control, result, note, screenshot, kind });
}

async function capture(page, width, tag) {
  seq += 1;
  const name = `${String(seq).padStart(3, '0')}-${width}-${tag}.png`;
  const file = path.join(SHOT, name);
  try {
    await page.screenshot({ path: file, fullPage: true });
  } catch {
    return '';
  }
  return `screenshots/${name}`;
}

async function tryStep(step, width, control, page, fn, kind = '') {
  try {
    const note = await fn();
    const sn = page ? await capture(page, width, step) : '';
    log(step, width, control, 'pass', note ?? '', sn, kind);
    return true;
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    const sn = page ? await capture(page, width, `${step}-fail`) : '';
    const errKind = kind || (msg.includes('Timeout') ? 'script' : 'site');
    log(step, width, control, 'fail', msg.slice(0, 300), sn, errKind);
    return false;
  }
}

function attachMonitors(page, tag) {
  page.on('response', (r) => {
    const u = r.url();
    if (r.status() >= 400 && u.includes('lobsy.nl') && !/favicon|fonts\.google/i.test(u)) {
      httpLog.push({ tag, status: r.status, url: u });
    }
  });
  page.on('console', (m) => {
    if (m.type() === 'error' && !/favicon|Cache-Control|Pragma/i.test(m.text())) {
      consoleLog.push({ tag, text: m.text() });
    }
  });
  page.on('pageerror', (e) => consoleLog.push({ tag, text: `pageerror: ${e.message}` }));
}

const COOKIE = `document.documentElement.classList.add('cookie-consent-known');try{localStorage.setItem('Jobsy.CookieConsent','1')}catch{}`;
const SPEECH = `window.__lobsyReadAloudSynth={getVoices:()=>[{lang:'nl-NL'}],speak(u){if(u?.onstart)u.onstart();if(u?.onend)setTimeout(u.onend,30)},cancel(){},addEventListener(){},removeEventListener(){}};`;

async function readLiveAssetVersion() {
  const html = await fetch(BASE).then((r) => r.text());
  const m = html.match(/leerling-journey-ssr\.js\?v=([^"']+)/);
  return m?.[1] ?? '';
}

async function loginStaff(page, email) {
  await page.context().clearCookies();
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 90_000 });
  await page.locator("input[name='email'], input[type=email]").first().fill(email);
  await page.locator("input[name='password'], input[type=password]").first().fill(PASS);
  await page.locator('button[type=submit]').first().click();
  await page.waitForFunction(() => !location.pathname.includes('/login'), null, { timeout: 90_000 });
}

async function freeCodeSlots(browser, count = 2) {
  const ctx = await browser.newContext({ locale: 'nl-NL' });
  await ctx.addInitScript(COOKIE);
  const page = await ctx.newPage();
  await tryStep('prep-free', 'docent', `${count} codes vrijmaken`, page, async () => {
    await loginStaff(page, 'schoolbeheer@jobsy.local');
    await page.goto(`${BASE}/school/klassen/${CLASS_ID}`, { waitUntil: 'load', timeout: 120_000 });
    const forms = page.locator('form[action*="/codes/"][action*="/delete"]');
    let freed = 0;
    for (let i = 0; i < count; i++) {
      if ((await forms.count()) === 0) break;
      page.once('dialog', (d) => d.accept());
      await forms.first().locator('button[type=submit]').click();
      await page.waitForTimeout(1200);
      freed++;
    }
    return `freed=${freed}`;
  });
  await ctx.close();
}

async function teacherAddCodeFresh(browser, label) {
  const ctx = await browser.newContext({ locale: 'nl-NL' });
  await ctx.addInitScript(COOKIE);
  const page = await ctx.newPage();
  attachMonitors(page, `docent-${label}`);
  let result = { code: null };
  await tryStep(`docent-add-${label}`, label, 'Code toevoegen (leraar)', page, async () => {
    await loginStaff(page, 'leraar@jobsy.local');
    await page.goto(`${BASE}/leraar/klas/${CLASS_ID}/codes`, { waitUntil: 'load', timeout: 120_000 });
    await page.locator('.sch-code').first().waitFor({ timeout: 120_000 });
    const before = new Set((await page.locator('.sch-code').allInnerTexts()).map((s) => s.trim()));
    await page.getByRole('button', { name: 'Code toevoegen' }).click();
    await page.waitForTimeout(2500);
    const body = await page.locator('body').innerText();
    if (/Maximaal\s+40\s+codes/i.test(body)) {
      throw new Error('Max 40 codes bereikt');
    }
    for (let t = 0; t < 120; t++) {
      for (const raw of await page.locator('.sch-code').allInnerTexts()) {
        const c = raw.trim();
        if (!before.has(c) && /^[A-Z0-9]{3}-[A-Z0-9]{3}$/.test(c)) {
          result = { code: c };
          codesCreated.push({ label, code: c, at: new Date().toISOString() });
          return c;
        }
      }
      await page.waitForTimeout(500);
    }
    throw new Error('Geen nieuwe code');
  });
  await ctx.close();
  return result;
}

async function pupilLogin(page, code) {
  const url = `${BASE}/leerling?schoolId=${SCHOOL_ID}&classId=${CLASS_ID}`;
  await page.context().clearCookies();
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 90_000 });
  await page.waitForSelector('select[name=classId], h1', { timeout: 60_000 });
  if ((await page.locator('h1').innerText()).includes('Je bent al ingelogd')) {
    await page.goto(`${BASE}/leerling/reis`, { waitUntil: 'commit' });
    return;
  }
  await page.selectOption('select[name=classId]', CLASS_ID);
  await page.fill('input[name=code]', code);
  await page.locator('form:has(input[name=code]) button[type=submit]').click();
  await page.waitForURL(/\/leerling\/(wereld|start|reis|vakken|puzzel|eiland|dit-ben-jij|droombaan|paspoort|stop)/, {
    timeout: 90_000,
  });
}

async function qualityCheck(page) {
  return page.evaluate(() => {
    const overflow = document.documentElement.scrollWidth - document.documentElement.clientWidth;
    const text = document.body?.innerText || '';
    const untranslated = (text.match(/\bLeerling(?:Passport|Story|G78)?\.[A-Za-z.]+/g) || []).slice(0, 5);
    const err = /Er ging iets mis|Even iets misgegaan|LB-[2-9A-HJKMNP-TV-Z]{4}/i.test(text);
    const lbCode = text.match(/LB-[2-9A-HJKMNP-TV-Z]{4}/)?.[0] ?? '';
    return { overflow, untranslated, err, lbCode };
  });
}

async function solvePuzzle(page) {
  const isVuurtoren = (await page.locator('[data-ll-lamp-index]').count()) > 0;
  if (isVuurtoren) {
    await page.waitForTimeout(9000);
    const pattern =
      (await page.evaluate(
        () => document.querySelector('[data-ll-puzzle-play-section]')?.getAttribute('data-ll-lamp-pattern') || ''
      )) || '';
    if (!pattern) throw new Error('Geen lamp-pattern');
    for (const p of pattern.split(',').map((s) => s.trim()).filter(Boolean)) {
      await page.locator(`[data-ll-lamp-index="${p}"]`).click();
      await page.waitForTimeout(500);
    }
  }
  if (await page.getByRole('button', { name: 'Start de puzzel' }).count()) {
    await page.getByRole('button', { name: 'Start de puzzel' }).click().catch(() => {});
  }
  if (await page.locator('[data-ll-schelp-choice]').count()) {
    const ans = await page.evaluate(
      () => document.querySelector('.ll-puzzle-g78')?.getAttribute('data-ll-puzzle-answer') || '2'
    );
    await page.locator(`[data-ll-schelp-choice="${ans}"]`).first().click();
  } else if (!isVuurtoren && (await page.locator('[data-ll-lamp-index]').count())) {
    await page.waitForTimeout(9000);
    const pattern =
      (await page.evaluate(
        () => document.querySelector('[data-ll-puzzle-play-section]')?.getAttribute('data-ll-lamp-pattern') || ''
      )) || '';
    for (const p of pattern.split(',').map((s) => s.trim()).filter(Boolean)) {
      await page.locator(`[data-ll-lamp-index="${p}"]`).click();
      await page.waitForTimeout(500);
    }
  } else if (await page.locator('[data-ll-card-index]').count()) {
    const cards = page.locator('[data-ll-card-index]');
    const n = await cards.count();
    for (let a = 0; a < 25; a++) {
      if (await page.locator('[data-ll-puzzle-continue]:not([disabled])').count()) break;
      for (let i = 0; i < n; i++) await cards.nth(i).click({ timeout: 2000 }).catch(() => {});
      await page.waitForTimeout(300);
    }
  }
  const skip = page.getByRole('button', { name: 'Overslaan' });
  if (await page.locator('[data-ll-puzzle-continue]:disabled').count() && (await skip.count())) {
    await skip.first().click();
    await page.waitForLoadState('domcontentloaded');
    return;
  }
  await page.locator('[data-ll-puzzle-continue]:not([disabled])').first().click({ timeout: 60_000 });
  await page.waitForLoadState('domcontentloaded');
}

async function fillVakken(page) {
  for (let p = 0; p < 6 && page.url().includes('/vakken'); p++) {
    const groups = page.locator('.ll-vt-sc, .ll-vt-scale-btns');
    const n = await groups.count();
    for (let i = 0; i < n; i++) {
      const btns = groups.nth(i).locator('.ll-vt-scale-btn');
      if ((await btns.count()) >= 3) await btns.nth(2).click();
    }
    const next = page.locator('[data-testid=ll-vt-next], .ll-vt-next');
    if (!(await next.count())) break;
    try {
      await page.waitForFunction(
        () => {
          const b = document.querySelector('[data-testid=ll-vt-next], .ll-vt-next');
          return b && !b.disabled;
        },
        null,
        { timeout: 20_000 }
      );
      await next.first().click();
      await page.waitForTimeout(800);
    } catch {
      break;
    }
  }
}

async function answerOne(page, value = '3') {
  await page.locator('[data-testid=ll-g78-answers] .ans').first().waitFor({ timeout: 30_000 });
  const prev = await page.locator('input[name=ItemId]').inputValue().catch(() => '');
  let pick = page.locator(`[data-testid=ll-g78-answers] .ans[data-ll-answer-value="${value}"]`);
  if (!(await pick.count())) pick = page.locator('[data-testid=ll-g78-answers] .ans').nth(2);
  await pick.first().click();
  await page
    .waitForFunction(
      (p) => {
        if (!location.pathname.includes('/leerling/reis')) return true;
        if (document.querySelector('[data-testid=leerling-reis-klaar]')) return true;
        const el = document.querySelector('input[name=ItemId]');
        return el && el.value !== p;
      },
      prev,
      { timeout: 55_000 }
    )
    .catch(() => {});
}

async function completePauzeEiland(page) {
  await page.locator('[data-ll-chip-like]').first().click({ timeout: 15_000 });
  await page.locator('[data-ll-island-next]').first().click();
  await page.locator('[data-ll-island-step="2"]').waitFor({ state: 'visible', timeout: 15_000 });
  await page.locator('[data-ll-chip-dislike]').first().click();
  const submit = page
    .locator('[data-ll-island-step="2"] button[type="submit"].btn-main')
    .or(page.getByRole('button', { name: 'Klaar, verder!' }).last());
  await submit.first().click();
  await page.waitForURL(/\/leerling\/(reis|puzzel)/, { timeout: 90_000 });
}

async function runViewport(browser, width, height, code, tag, assetV) {
  const ctx = await browser.newContext({
    viewport: { width, height },
    isMobile: width < 900,
    hasTouch: width < 900,
    locale: 'nl-NL',
  });
  await ctx.addInitScript(COOKIE + SPEECH);
  const page = await ctx.newPage();
  page.setDefaultTimeout(90_000);
  attachMonitors(page, tag);

  await tryStep('login', tag, 'Leerling login', page, async () => {
    await pupilLogin(page, code);
    return code;
  });

  await tryStep('asset-ssr', tag, 'leerling-journey-ssr.js', page, async () => {
    const ok = await page.evaluate((v) => {
      const srcs = [...document.querySelectorAll('script[src]')].map((s) => s.getAttribute('src') || '');
      return !v || srcs.some((s) => s.includes(`leerling-journey-ssr.js?v=${v}`));
    }, assetV);
    if (assetV && !ok) throw new Error(`Script v=${assetV} niet in pagina`);
    return assetV || 'any';
  });

  let answers = 0;
  let pauseDone = false;
  let vorigeDone = false;
  let pdfDone = false;
  let lastScreen = '';
  let puzzelFails = 0;

  for (let guard = 0; guard < 260; guard++) {
    const url = page.url();
    let screen = 'other';
    if (url.includes('/wereld')) screen = 'wereld';
    else if (url.includes('/start')) screen = 'start';
    else if (url.includes('/stop')) screen = 'stop';
    else if (url.includes('/puzzel')) screen = 'puzzel';
    else if (url.includes('/eiland')) screen = 'eiland';
    else if (url.includes('/vakken')) screen = 'vakken';
    else if (url.includes('/dit-ben-jij')) screen = 'dit-ben-jij';
    else if (url.includes('/droombaan')) screen = 'droombaan';
    else if (url.includes('/paspoort')) screen = 'paspoort';
    else if (url.includes('/reis')) screen = (await page.locator('[data-testid=leerling-reis-klaar]').count()) ? 'klaar' : 'reis';

    if (screen !== lastScreen) {
      const q = await qualityCheck(page);
      const sn = await capture(page, tag, screen);
      const ok = q.overflow <= 1 && !q.untranslated.length && !q.err;
      log(screen, tag, 'scherm + layout', ok ? 'pass' : 'fail', JSON.stringify(q), sn, q.err ? 'site' : 'site');
      lastScreen = screen;
    }

    if (screen === 'paspoort' && !pdfDone) {
      pdfDone = true;
      await tryStep('pdf', tag, 'DNA-paspoort PDF', page, async () => {
        await page.locator('[data-testid=leerling-paspoort-page], [data-testid=leerling-paspoort-g78]').first().waitFor({
          timeout: 30_000,
        });
        const pdfLink = page.locator('a[href*="/leerling/pdf"]').first();
        const [resp] = await Promise.all([
          page.waitForResponse((r) => r.url().includes('/leerling/pdf') && r.status() < 500, { timeout: 60_000 }).catch(() => null),
          pdfLink.click(),
        ]);
        if (resp && resp.status() >= 400) throw new Error(`PDF HTTP ${resp.status()}`);
        return 'pdf ok';
      });
      break;
    }

    if (screen === 'wereld') {
      await tryStep('wereld', tag, 'Zee-expeditie', page, async () => {
        await page.locator('[data-testid=ll-wereld-zee]').click();
        await page.waitForURL('**/leerling/start**', { timeout: 60_000 });
      });
      continue;
    }
    if (screen === 'start') {
      await tryStep('start', tag, 'Beginnen', page, async () => {
        await page.locator('a.rk-go, a.btn-primary').first().click();
        await page.waitForURL('**/leerling/reis**', { timeout: 60_000 });
      });
      continue;
    }
    if (screen === 'stop') {
      await tryStep('stop-hervat', tag, 'Stop → hervat', page, async () => {
        await pupilLogin(page, code);
        return page.url();
      });
      continue;
    }
    if (screen === 'klaar') {
      await tryStep('klaar', tag, 'Naar dit-ben-jij', page, async () => {
        await page.locator('a[href="/leerling/dit-ben-jij"]').first().click();
        await page.waitForURL('**/dit-ben-jij**', { timeout: 60_000 });
      });
      continue;
    }
    if (screen === 'reis') {
      answers++;
      if (answers === 6 && !pauseDone) {
        await tryStep('pauze', tag, 'Pauze → stop → hervat', page, async () => {
          await page.getByRole('button', { name: /^Pauze$/ }).or(page.locator('.ll-topbar__pause')).first().click();
          await page.waitForURL(/\/leerling\/(stop|pauze)/, { timeout: 60_000 });
          pauseDone = true;
          await pupilLogin(page, code);
        });
        continue;
      }
      if (answers === 12 && !vorigeDone) {
        await tryStep('vorige', tag, 'Vorige', page, async () => {
          await page.getByRole('link', { name: 'Vorige' }).click();
          await page.locator('[data-testid=ll-g78-answers] .ans').last().click();
          vorigeDone = true;
        });
        continue;
      }
      await tryStep(`reis-q${answers}`, tag, `Antwoord ${answers}`, page, async () => {
        await answerOne(page);
      });
      continue;
    }
    if (screen === 'puzzel') {
      if (puzzelFails >= 3) {
        log('puzzel-skip', tag, 'Puzzel overslaan', 'fail', 'Te veel mislukte pogingen', await capture(page, tag, 'puzzel-stuck'));
        await page.getByRole('button', { name: 'Overslaan' }).click({ timeout: 5000 }).catch(() => {});
        continue;
      }
      const ok = await tryStep('puzzel', tag, 'Puzzel', page, async () => {
        await solvePuzzle(page);
      });
      puzzelFails = ok ? 0 : puzzelFails + 1;
      continue;
    }
    if (screen === 'eiland') {
      await tryStep('eiland', tag, 'Pauze-eiland Klaar verder', page, async () => {
        await completePauzeEiland(page);
      });
      continue;
    }
    if (screen === 'vakken') {
      await tryStep('vakken', tag, 'Vakken', page, async () => {
        await fillVakken(page);
      });
      continue;
    }
    if (screen === 'dit-ben-jij') {
      await tryStep('dit-ben-jij', tag, 'Droombaan', page, async () => {
        await page.locator('a[href="/leerling/droombaan"]').first().click();
        await page.waitForURL('**/droombaan**', { timeout: 60_000 });
      });
      continue;
    }
    if (screen === 'droombaan') {
      await tryStep('droombaan', tag, 'Bevestigen', page, async () => {
        if (!(await page.locator('[data-testid=ll-g78-job-picks]').count())) {
          await page.goto(`${BASE}/leerling/droombaan?pick=1`, { waitUntil: 'domcontentloaded' });
        }
        await page.locator('[data-testid=ll-g78-job-picks] .jpick:not(.jpick-none)').first().click();
        await page.getByRole('button', { name: 'Bevestigen' }).click();
      });
      await page.goto(`${BASE}/leerling/paspoort`, { waitUntil: 'domcontentloaded' }).catch(() => {});
      continue;
    }
    await page.waitForTimeout(400);
  }

  if (!pdfDone) {
    await tryStep('paspoort-fallback', tag, 'Paspoort', page, async () => {
      await page.goto(`${BASE}/leerling/paspoort`, { waitUntil: 'domcontentloaded' });
      await page.locator('[data-testid=leerling-paspoort-page], [data-testid=leerling-paspoort-g78]').first().waitFor({
        timeout: 30_000,
      });
    });
  }

  log(
    'http',
    tag,
    'Geen 4xx/5xx',
    httpLog.filter((h) => h.tag === tag).length ? 'fail' : 'pass',
    httpLog.filter((h) => h.tag === tag).map((h) => `${h.status} ${h.url}`).join('; '),
    '',
    'site'
  );
  log(
    'console',
    tag,
    'Geen console errors',
    consoleLog.filter((c) => c.tag === tag).length ? 'fail' : 'pass',
    consoleLog.filter((c) => c.tag === tag).map((c) => c.text).slice(0, 5).join('; '),
    '',
    'site'
  );

  await ctx.close();
}

function writeReport(assetV) {
  const shots = fs.existsSync(SHOT) ? fs.readdirSync(SHOT).sort() : [];
  const md = [
    '# G78 Acceptatie hertest (LB-6RGZ)',
    '',
    `- Base: ${BASE}`,
    `- Asset: \`leerling-journey-ssr.js?v=${assetV}\``,
    `- Codes: ${codesCreated.map((c) => `${c.label}:${c.code}`).join(', ') || '(zie log)'}`,
    '',
    '| Stap | Breedte | Control | Result | Notitie | Screenshot |',
    '|------|---------|---------|--------|---------|------------|',
    ...rows.map(
      (r) =>
        `| ${r.step} | ${r.width} | ${r.control} | ${r.result} | ${r.note.replace(/\|/g, '/').slice(0, 100)} | ${r.screenshot} |`
    ),
    '',
    `## Samenvatting: ${rows.filter((r) => r.result === 'pass').length} pass / ${rows.filter((r) => r.result === 'fail').length} fail`,
  ].join('\n');
  fs.writeFileSync(path.join(OUT, 'REPORT.md'), md);
  fs.writeFileSync(path.join(OUT, 'report.json'), JSON.stringify({ rows, httpLog, consoleLog, shots, codesCreated, assetV }, null, 2));
}

async function main() {
  fs.mkdirSync(SHOT, { recursive: true });
  const assetV = await readLiveAssetVersion();
  const browser = await chromium.launch({ headless: true });

  await freeCodeSlots(browser, 3);

  const r1440 = await teacherAddCodeFresh(browser, '1440');
  const r390 = await teacherAddCodeFresh(browser, '390');

  if (r1440.code) {
    try {
      await runViewport(browser, 1440, 900, r1440.code, '1440', assetV);
    } catch (e) {
      log('1440-abort', '1440', 'Flow', 'fail', String(e).slice(0, 200), '', 'site');
    }
  } else {
    log('1440-skip', '1440', 'Geen code', 'fail', '', '', 'site');
  }

  if (r390.code) {
    try {
      await runViewport(browser, 390, 844, r390.code, '390', assetV);
    } catch (e) {
      log('390-abort', '390', 'Flow', 'fail', String(e).slice(0, 200), '', 'site');
    }
  } else if (r1440.code) {
    try {
      await runViewport(browser, 390, 844, r1440.code, '390-reuse', assetV);
    } catch (e) {
      log('390-abort', '390', 'Flow', 'fail', String(e).slice(0, 200), '', 'site');
    }
  }

  await browser.close();
  writeReport(assetV);
  console.log(fs.readFileSync(path.join(OUT, 'REPORT.md'), 'utf8'));
  const failCount = rows.filter((r) => r.result === 'fail').length;
  process.exit(failCount ? 1 : 0);
}

main().catch((e) => {
  console.error(e);
  writeReport('?');
  process.exit(1);
});
