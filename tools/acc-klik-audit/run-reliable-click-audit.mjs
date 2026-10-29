#!/usr/bin/env node
/**
 * Reliable Acc click audit — reload per element, role+name selectors, elementFromPoint.
 * Output: artifacts/acc-klik/
 */
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = process.env.ACC_KLIK_OUT ?? path.join(__dirname);
const BASE = (process.env.JOBSY_E2E_BASE_URL ?? 'https://acceptatie.lobsy.nl').replace(/\/$/, '');
const DEMO_EMAIL = process.env.JOBSY_E2E_CANDIDATE_EMAIL ?? 'kandidaat@jobsy.local';
const DEMO_PW = process.env.JOBSY_E2E_CANDIDATE_PASSWORD ?? 'Jobsy123!';

const VIEWPORTS = [
  { tag: '1440', width: 1440, height: 900 },
  { tag: '390', width: 390, height: 844 },
];

const SKIP_FRAGMENTS = [
  'later verder',
  'later doen',
  'account verwijderen',
  'begin opnieuw',
  'intrekken',
  'uitloggen',
  'log out',
  'logout',
];

const SKIP_EXACT = ['Solliciteer', 'Apply'];

const PAGES = [
  { id: 'landing', title: 'Landing (/)', path: '/', auth: false },
  { id: 'login', title: 'Login', path: '/login', auth: false },
  { id: 'account-maken', title: 'Account maken', path: '/account-maken', auth: false },
  { id: 'wachtwoord-vergeten', title: 'Wachtwoord vergeten', path: '/wachtwoord-vergeten', auth: false },
  { id: 'paspoort', title: 'Paspoort', path: '/candidate/paspoort', auth: true },
  { id: 'sollicitaties', title: 'Sollicitaties', path: '/candidate/applications', auth: true },
  { id: 'banenkaart-gast', title: 'Banenkaart (gast)', path: '/banenkaart', auth: false },
  { id: 'banenkaart-ingelogd', title: 'Banenkaart (ingelogd)', path: '/banenkaart', auth: true },
];

function slug(s) {
  return s
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 60)
    .toLowerCase();
}

async function prepPage(page, mobile) {
  await page.evaluate(() => {
    try {
      localStorage.setItem('Jobsy.CookieConsent', 'necessary');
    } catch (_) {}
    document.documentElement.classList.add('cookie-consent-known');
    const banner = document.querySelector('.cookie-consent');
    if (banner) banner.style.display = 'none';
  });
  if (mobile) {
    await page.evaluate(() => {
      document.querySelector('.lobsy-assistant.is-open .share-modal__close')?.click();
      document.querySelector('.lobsy-assistant-tab.is-open')?.classList.remove('is-open');
      for (const el of document.querySelectorAll('.lobsy-assistant-tab, .lobsy-assistant-tab__btn')) {
        el.style.pointerEvents = 'none';
      }
    });
    await page.waitForTimeout(150);
  }
  await page.waitForTimeout(250);
}

async function gotoStart(page, url) {
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 120_000 });
  await prepPage(page, (page.viewportSize()?.width ?? 1440) < 900);
  if (url.includes('/banenkaart')) {
    await page
      .waitForFunction(
        () => document.querySelector('[data-banenkaart-ssr="1"]')?.dataset?.banenkaartSsrBound === '1',
        null,
        { timeout: 90_000 },
      )
      .catch(() => {});
  }
}

async function loginContext(context) {
  const page = await context.newPage();
  await page.setViewportSize({ width: 1440, height: 900 });
  await gotoStart(page, `${BASE}/login`);
  await page.locator("input[name='email'], input[type='email']").first().fill(DEMO_EMAIL);
  await page.locator("input[name='password'], input[type='password']").first().fill(DEMO_PW);
  await page.locator('button.au-submit, button.login-submit, button[type=submit]').first().click();
  await page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 90_000 });
  await page.close();
}

async function collectCatalog(page) {
  return page.evaluate(() => {
    const SKIP_SKIPLINK = /naar de inhoud/i;
    function accName(el) {
      const aria = el.getAttribute('aria-label');
      if (aria) return aria.trim().replace(/\s+/g, ' ').slice(0, 120);
      if (el.tagName === 'INPUT') {
        const id = el.id;
        if (id) {
          const lab = document.querySelector('label[for="' + CSS.escape(id) + '"]');
          if (lab) return (lab.innerText || lab.textContent || '').trim().replace(/\s+/g, ' ').slice(0, 120);
        }
        const ph = el.getAttribute('placeholder');
        if (ph) return ph.trim();
        const n = el.getAttribute('name');
        if (n) return n;
      }
      if (el.tagName === 'SELECT') {
        const n = el.getAttribute('name') || el.getAttribute('aria-label');
        if (n) return n;
      }
      const t = (el.innerText || el.textContent || '').trim().replace(/\s+/g, ' ');
      if (t) return t.slice(0, 120);
      const href = el.getAttribute('href');
      if (href) return href.slice(0, 120);
      return el.tagName.toLowerCase();
    }
    function roleOf(el) {
      const r = el.getAttribute('role');
      if (r) return r;
      const tag = el.tagName.toLowerCase();
      if (tag === 'a') return 'link';
      if (tag === 'button') return 'button';
      if (tag === 'select') return 'combobox';
      if (tag === 'input') {
        const ty = (el.getAttribute('type') || 'text').toLowerCase();
        if (ty === 'checkbox') return 'checkbox';
        if (ty === 'radio') return 'radio';
        if (ty === 'submit' || ty === 'button') return 'button';
        return 'textbox';
      }
      return tag;
    }
    function inClosedSurface(el) {
      if (el.closest('[aria-hidden="true"]')) return true;
      if (el.closest('details:not([open])')) return true;
      const menu = el.closest('.account-menu');
      if (menu && !menu.classList.contains('is-open')) {
        if (el.closest('.account-menu__panel')) return true;
      }
      const drawer = el.closest('.filter-sheet, .kb-filter-drawer');
      if (drawer) {
        const hidden = drawer.getAttribute('aria-hidden');
        if (hidden === 'true') return true;
        if (drawer.classList.contains('is-closed')) return true;
      }
      return false;
    }
    function visibleEnough(el) {
      if (inClosedSurface(el)) return false;
      const st = getComputedStyle(el);
      if (st.visibility === 'hidden' || st.display === 'none' || st.pointerEvents === 'none') return false;
      if (el.closest('.cookie-consent, .cookie-banner')) return false;
      const r = el.getBoundingClientRect();
      if (r.width <= 0 || r.height <= 0) return false;
      if (r.bottom < 0 || r.top > innerHeight || r.right < 0 || r.left > innerWidth) return false;
      return true;
    }
    const sel = ['a[href]', 'button', 'input:not([type=hidden])', 'select', '[role=tab]', '[role=button]'].join(',');
    const raw = [];
    for (const el of document.querySelectorAll(sel)) {
      if (!visibleEnough(el)) continue;
      const name = accName(el);
      if (SKIP_SKIPLINK.test(name)) continue;
      const role = roleOf(el);
      raw.push({
        role,
        name,
        tag: el.tagName.toLowerCase(),
        inputType: el.getAttribute('type') || '',
        isSubmit:
          el.tagName === 'BUTTON'
            ? (el.getAttribute('type') || 'submit') === 'submit'
            : el.tagName === 'INPUT' && (el.getAttribute('type') || '').toLowerCase() === 'submit',
        href: el.getAttribute('href') || '',
      });
    }
    const counts = new Map();
    for (const item of raw) {
      const k = item.role + '||' + item.name;
      const idx = counts.get(k) || 0;
      item.index = idx;
      counts.set(k, idx + 1);
      item.key = k + '||' + idx;
    }
    return raw;
  });
}

function shouldSkipItem(item, pagePath) {
  const lower = item.name.toLowerCase();
  for (const f of SKIP_FRAGMENTS) {
    if (lower.includes(f)) return f;
  }
  if (SKIP_EXACT.some((ex) => item.name.trim().toLowerCase() === ex.toLowerCase())) return 'solliciteer';
  if (item.href && (item.href.includes('/apply') || item.href.toLowerCase().includes('sollicit'))) return 'sollicitatie-link';
  if (item.isSubmit && pagePath !== '/login') return 'submit';
  return null;
}

function playwrightRole(role) {
  const map = {
    link: 'link',
    button: 'button',
    textbox: 'textbox',
    combobox: 'combobox',
    checkbox: 'checkbox',
    radio: 'radio',
    tab: 'tab',
  };
  return map[role] ?? role;
}

/** Same catalog logic in-browser; returns the DOM node for `targetKey`. */
async function elementHandleForKey(page, targetKey) {
  return page.evaluateHandle((key) => {
    const SKIP_SKIPLINK = /naar de inhoud/i;
    function accName(el) {
      const aria = el.getAttribute('aria-label');
      if (aria) return aria.trim().replace(/\s+/g, ' ').slice(0, 120);
      if (el.tagName === 'INPUT') {
        const id = el.id;
        if (id) {
          const lab = document.querySelector('label[for="' + CSS.escape(id) + '"]');
          if (lab) return (lab.innerText || lab.textContent || '').trim().replace(/\s+/g, ' ').slice(0, 120);
        }
        const ph = el.getAttribute('placeholder');
        if (ph) return ph.trim();
        const n = el.getAttribute('name');
        if (n) return n;
      }
      if (el.tagName === 'SELECT') {
        const n = el.getAttribute('name') || el.getAttribute('aria-label');
        if (n) return n;
      }
      const t = (el.innerText || el.textContent || '').trim().replace(/\s+/g, ' ');
      if (t) return t.slice(0, 120);
      const href = el.getAttribute('href');
      if (href) return href.slice(0, 120);
      return el.tagName.toLowerCase();
    }
    function roleOf(el) {
      const r = el.getAttribute('role');
      if (r) return r;
      const tag = el.tagName.toLowerCase();
      if (tag === 'a') return 'link';
      if (tag === 'button') return 'button';
      if (tag === 'select') return 'combobox';
      if (tag === 'input') {
        const ty = (el.getAttribute('type') || 'text').toLowerCase();
        if (ty === 'checkbox') return 'checkbox';
        if (ty === 'radio') return 'radio';
        if (ty === 'submit' || ty === 'button') return 'button';
        return 'textbox';
      }
      return tag;
    }
    function inClosedSurface(el) {
      if (el.closest('[aria-hidden="true"]')) return true;
      if (el.closest('details:not([open])')) return true;
      const menu = el.closest('.account-menu');
      if (menu && !menu.classList.contains('is-open')) {
        if (el.closest('.account-menu__panel')) return true;
      }
      const drawer = el.closest('.filter-sheet, .kb-filter-drawer');
      if (drawer) {
        const hidden = drawer.getAttribute('aria-hidden');
        if (hidden === 'true') return true;
        if (drawer.classList.contains('is-closed')) return true;
      }
      return false;
    }
    function visibleEnough(el) {
      if (inClosedSurface(el)) return false;
      const st = getComputedStyle(el);
      if (st.visibility === 'hidden' || st.display === 'none' || st.pointerEvents === 'none') return false;
      if (el.closest('.cookie-consent, .cookie-banner')) return false;
      const r = el.getBoundingClientRect();
      if (r.width <= 0 || r.height <= 0) return false;
      if (r.bottom < 0 || r.top > innerHeight || r.right < 0 || r.left > innerWidth) return false;
      return true;
    }
    const sel = ['a[href]', 'button', 'input:not([type=hidden])', 'select', '[role=tab]', '[role=button]'].join(',');
    const raw = [];
    for (const el of document.querySelectorAll(sel)) {
      if (!visibleEnough(el)) continue;
      const name = accName(el);
      if (SKIP_SKIPLINK.test(name)) continue;
      const role = roleOf(el);
      raw.push({ role, name, el });
    }
    const counts = new Map();
    for (const item of raw) {
      const k = item.role + '||' + item.name;
      const idx = counts.get(k) || 0;
      const fullKey = k + '||' + idx;
      counts.set(k, idx + 1);
      if (fullKey === key) return item.el;
    }
    return null;
  }, targetKey);
}

async function maskSecrets(page) {
  await page.evaluate(() => {
    for (const el of document.querySelectorAll('input[type=password]')) {
      el.value = '';
      el.setAttribute('value', '');
    }
  });
}

async function hitTestHandle(handle) {
  const hit = await handle.evaluate((el) => {
    if (!el) return 'missing';
    el.scrollIntoView({ block: 'center', inline: 'nearest', behavior: 'instant' });
    const r = el.getBoundingClientRect();
    const x = r.left + r.width / 2;
    const y = r.top + r.height / 2;
    const top = document.elementFromPoint(x, y);
    if (!top) return 'empty';
    if (el === top || el.contains(top) || top.contains(el)) return 'ok';
    return 'blocked:' + (top.tagName?.toLowerCase() || '?');
  });
  return hit;
}

async function auditElement(page, pageDef, vp, item, problems) {
  const startUrl = `${BASE}${pageDef.path}`;
  await page.setViewportSize({ width: vp.width, height: vp.height });
  await gotoStart(page, startUrl);

  const skip = shouldSkipItem(item, pageDef.path);
  if (skip) return { status: 'skipped', effect: skip };

  const catalog = await collectCatalog(page);
  const found = catalog.find((c) => c.key === item.key);
  if (!found) return { status: 'missing', effect: 'not-in-dom' };

  const handle = await elementHandleForKey(page, item.key);
  const isNull = await handle.evaluate((el) => el === null);
  if (isNull) {
    await handle.dispose();
    return { status: 'missing', effect: 'handle-null' };
  }

  await page.waitForTimeout(80);
  const hit = await hitTestHandle(handle);
  if (hit !== 'ok') {
    const status = hit.startsWith('blocked') ? 'blocked' : 'hit:' + hit;
    if (hit.startsWith('blocked')) {
      await maskSecrets(page);
      const shot = path.join(OUT, 'problems', `${pageDef.id}-${vp.tag}-${slug(item.name)}.png`);
      await handle.screenshot({ path: shot }).catch(async () => page.screenshot({ path: shot }));
      problems.push({ page: pageDef.title, viewport: vp.tag, element: item.name, issue: status, shot });
    }
    await handle.dispose();
    return { status, effect: hit };
  }

  const before = await handle.evaluate((el) => ({
    url: location.href,
    expanded: el.getAttribute('aria-expanded'),
    pressed: el.getAttribute('aria-pressed'),
    checked: el.getAttribute('aria-checked'),
    tag: el.tagName.toLowerCase(),
  }));

  try {
    await handle.click({ timeout: 10_000 });
  } catch (e) {
    if (page.url() !== before.url) {
      await handle.dispose().catch(() => {});
      return { status: 'ok', effect: 'navigate' };
    }
    await handle.dispose().catch(() => {});
    const msg = e.message?.slice(0, 80) || 'click-fail';
    return { status: 'error', effect: msg };
  }

  const inputLike = before.tag === 'input' || before.tag === 'textarea' || before.tag === 'select';
  if (inputLike) {
    let focused = false;
    try {
      focused = await handle.evaluate((el) => document.activeElement === el || el.contains(document.activeElement));
    } catch {
      focused = page.url() !== before.url;
    }
    await handle.dispose().catch(() => {});
    if (focused) return { status: 'ok', effect: 'focus' };
    await maskSecrets(page);
    const shot = path.join(OUT, 'problems', `${pageDef.id}-${vp.tag}-${slug(item.name)}-noeffect.png`);
    await page.screenshot({ path: shot });
    problems.push({ page: pageDef.title, viewport: vp.tag, element: item.name, issue: 'no-focus', shot });
    return { status: 'no-effect', effect: 'no-focus' };
  }

  await page.waitForTimeout(350);
  if (page.url() !== before.url) {
    await handle.dispose().catch(() => {});
    return { status: 'ok', effect: 'navigate' };
  }

  let after = { expanded: null, pressed: null, checked: null, active: false };
  try {
    after = await handle.evaluate((el) => ({
      expanded: el.getAttribute('aria-expanded'),
      pressed: el.getAttribute('aria-pressed'),
      checked: el.getAttribute('aria-checked'),
      active:
        el.classList.contains('is-active') || el.classList.contains('active') || el.matches(':checked'),
    }));
  } catch {
    await handle.dispose().catch(() => {});
    return { status: 'ok', effect: 'navigate' };
  }
  await handle.dispose().catch(() => {});
  if (after.expanded !== before.expanded) return { status: 'ok', effect: 'aria-expanded' };
  if (after.pressed !== before.pressed) return { status: 'ok', effect: 'aria-pressed' };
  if (after.checked !== before.checked) return { status: 'ok', effect: 'aria-checked' };
  if (after.active) return { status: 'ok', effect: 'active-class' };

  await maskSecrets(page);
  const shotPath = path.join(OUT, 'problems', `${pageDef.id}-${vp.tag}-${slug(item.name)}-noeffect.png`);
  await page.screenshot({ path: shotPath });
  problems.push({ page: pageDef.title, viewport: vp.tag, element: item.name, issue: 'no-effect', shot: shotPath });
  return { status: 'no-effect', effect: 'no-effect' };
}

async function catalogForPage(page, pageDef, vp) {
  await page.setViewportSize({ width: vp.width, height: vp.height });
  await gotoStart(page, `${BASE}${pageDef.path}`);
  const catalog = await collectCatalog(page);
  return catalog.filter((c) => !shouldSkipItem(c, pageDef.path));
}

async function auditPage(browser, pageDef, problems) {
  const authContext = await browser.newContext();
  if (pageDef.auth) await loginContext(authContext);
  const page = await authContext.newPage();

  const catalog = await catalogForPage(page, pageDef, VIEWPORTS[0]);
  const keys = new Map();
  for (const c of catalog) keys.set(c.key, c);

  const narrowCat = await catalogForPage(page, pageDef, VIEWPORTS[1]);
  for (const c of narrowCat) keys.set(c.key, c);

  const items = [...keys.values()].sort((a, b) => a.name.localeCompare(b.name));
  console.log(`  ${pageDef.id}: ${items.length} elementen`);
  const rows = [];
  let i = 0;
  for (const item of items) {
    i++;
    if (i % 5 === 1) console.log(`  ${pageDef.id}: ${i}/${items.length}`);
    const row = { element: item.name, role: item.role, key: item.key, r1440: '', r390: '', effect1440: '', effect390: '' };
    for (const vp of VIEWPORTS) {
      try {
        const r = await auditElement(page, pageDef, vp, item, problems);
        if (vp.tag === '1440') {
          row.r1440 = r.status;
          row.effect1440 = r.effect;
        } else {
          row.r390 = r.status;
          row.effect390 = r.effect;
        }
      } catch (err) {
        const msg = err.message?.slice(0, 60) || 'err';
        if (vp.tag === '1440') row.r1440 = 'error';
        else row.r390 = 'error';
      }
    }
    rows.push(row);
  }

  await authContext.close();
  return rows;
}

function writePageReport(pageDef, rows) {
  const mdPath = path.join(OUT, `${pageDef.id}.md`);
  const jsonPath = path.join(OUT, `${pageDef.id}.json`);
  let md = `# Kliktest — ${pageDef.title}\n\n`;
  md += `URL: ${BASE}${pageDef.path}\n\n`;
  md += '| Element | 1440 | 390 | Effect (1440 / 390) |\n| --- | --- | --- | --- |\n';
  for (const r of rows) {
    const eff = `${r.effect1440 || '—'} / ${r.effect390 || '—'}`;
    md += `| ${r.element.replace(/\|/g, '\\|')} | ${r.r1440} | ${r.r390} | ${eff} |\n`;
  }
  fs.writeFileSync(mdPath, md);
  fs.writeFileSync(jsonPath, JSON.stringify({ page: pageDef.title, path: pageDef.path, rows }, null, 2));
}

async function main() {
  fs.mkdirSync(path.join(OUT, 'problems'), { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const problems = [];
  const meta = { base: BASE, started: new Date().toISOString(), pages: [] };

  for (const pageDef of PAGES) {
    const mdPath = path.join(OUT, `${pageDef.id}.md`);
    if (process.env.ACC_KLIK_RESUME === '1' && fs.existsSync(mdPath)) {
      console.log('Skip (done)', pageDef.id);
      meta.pages.push({ id: pageDef.id, elements: 'skipped-resume' });
      continue;
    }
    console.log('Auditing', pageDef.id, '…');
    const rows = await auditPage(browser, pageDef, problems);
    writePageReport(pageDef, rows);
    meta.pages.push({ id: pageDef.id, elements: rows.length });
  }

  await browser.close();

  let summary = `# Acceptatie kliktest — ${BASE}\n\n`;
  summary += `Gestart: ${meta.started}\n\n`;
  summary += `Commit (verwacht door opdracht): \`15bbaf4d\` — live SHA niet via HTTP bevestigd.\n\n`;
  summary += `## Pagina-overzicht\n\n| Pagina | Elementen getest |\n| --- | --- |\n`;
  for (const p of meta.pages) summary += `| ${p.id} | ${p.elements} |\n`;

  if (problems.length) {
    summary += `\n## Echte problemen (${problems.length})\n\n`;
    for (const p of problems) {
      summary += `- **${p.page}** @ ${p.viewport}px — “${p.element}”: ${p.issue} ([screenshot](problems/${path.basename(p.shot)}))\n`;
    }
  } else {
    summary += `\n## Echte problemen\n\nGeen geblokkeerde hits of klikken zonder effect.\n`;
  }

  fs.writeFileSync(path.join(OUT, 'SUMMARY.md'), summary);
  fs.writeFileSync(path.join(OUT, 'problems.json'), JSON.stringify(problems, null, 2));
  fs.writeFileSync(path.join(OUT, 'meta.json'), JSON.stringify(meta, null, 2));
  console.log('Done. Problems:', problems.length);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
