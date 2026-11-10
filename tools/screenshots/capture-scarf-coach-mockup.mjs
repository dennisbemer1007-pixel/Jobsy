/**
 * Coach-voorkeuren mockups — strakke paspoort-layout + gevulde Past bij? + vergelijking.
 */
import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const outArtifacts = process.env.OUT_DIR || '/opt/cursor/artifacts/scarf-coach-mockup';
const outRepo = path.join(root, 'docs/design/scarf-coach-mockup/screenshots');
const previewsDir = path.join(root, 'docs/design/scarf-coach-mockup/previews');

const lobsterB64 = fs.readFileSync(path.join(root, 'Jobsy.Web/wwwroot/images/brand/lobsy-128.webp')).toString('base64');
const lobsterSrc = `data:image/webp;base64,${lobsterB64}`;

function readCss(...parts) {
  return parts.map((p) => fs.readFileSync(path.join(root, p), 'utf8')).join('\n');
}

const styles = readCss(
  'Jobsy.Web/wwwroot/css/tokens.css',
  'Jobsy.Web/wwwroot/css/app.css',
  'Jobsy.Web/wwwroot/css/features/mijn-paspoort.css',
  'Jobsy.Web/wwwroot/css/features/kompas-illustrated.css',
  'Jobsy.Web/wwwroot/css/features/choice-tiles.css',
  'Jobsy.Web/wwwroot/css/features/form-fields.css',
  'Jobsy.Web/wwwroot/css/features/lobsy-coach-pill.css',
  'Jobsy.Web/wwwroot/css/features/lobsy-assistant-panel.css',
  'Jobsy.Web/wwwroot/css/features/candidate-illustrated-app.css',
  'docs/design/scarf-coach-mockup/scarf-mockup-base.css',
);

const ICONS = {
  future: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M13 6l6 6-6 6"/></svg>',
  warn: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="9"/><path d="M12 8v5M12 16h.01"/></svg>',
  clock: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>',
  people: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M16 11c1.7 0 3-1.3 3-3S17.7 5 16 5s-3 1.3-3 3 1.3 3 3 3zM8 13c1.7 0 3-1.3 3-3S9.7 7 8 7 5 8.3 5 10s1.3 3 3 3z"/><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/></svg>',
  ok: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 12l2 2 4-4"/><circle cx="12" cy="12" r="9"/></svg>',
  step: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/></svg>',
  hand: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M7 11V7a2 2 0 0 1 4 0v4M11 9V5a2 2 0 0 1 4 0v6M15 11V8a2 2 0 0 1 4 0v8a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5v-3"/></svg>',
};

function head(title, vp) {
  return `<!DOCTYPE html><html lang="nl"><head>
<meta charset="utf-8"><title>${title}</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
:root { --font-display: "Segoe UI", system-ui, sans-serif; --r-md: 12px; --r-sm: 8px; --r-lg: 20px;
  --bottom-nav-h: 64px; --app-header-height: 56px; --lobsy-coach-fab-size: 56px; }
body { margin: 0; background: var(--bg, #f5f2ee); font-family: "Segoe UI", system-ui, sans-serif; color: var(--text); }
${styles}
@media (max-width: 1023px) {
  .passport-layout__card--desktop { display: none !important; }
  .passport-layout__card--mobile { display: block !important; }
}
@media (min-width: 1024px) {
  .passport-layout { display: grid; grid-template-columns: minmax(280px, 330px) minmax(0, 1fr); gap: 1.25rem; align-items: start; }
  .passport-layout__card--desktop { display: block !important; position: sticky; top: calc(var(--app-header-height) + 0.5rem); }
  .passport-layout__card--mobile { display: none !important; }
}
${vp === 'mobile' ? `.passport-tabs { flex-wrap: nowrap; overflow-x: auto; -webkit-overflow-scrolling: touch; }` : ''}
</style></head>`;
}

function pageChrome(label) {
  return `<header class="mockup-header" aria-label="Pagina"><p class="mockup-header__title">Mijn paspoort</p></header>
<div class="mockup-only-banner">${label}</div>`;
}

function sidebarCard(compact) {
  const wrap = compact
    ? 'passport-layout__card passport-layout__card--mobile'
    : 'passport-layout__sidebar passport-layout__card passport-layout__card--desktop';
  return `<div class="${wrap}"><aside class="passport-card mockup-passport-card${compact ? ' passport-card--compact' : ''}">
  <div class="passport-card__banner"><div class="passport-card__banner-text">
    <p class="passport-card__banner-title">MIJN PASPOORT</p>
    <p class="passport-card__member">Lid sinds 2025</p>
  </div></div>
  <div class="passport-card__identity">
    <div class="passport-card__avatar" aria-hidden="true">SM</div>
    <div class="passport-card__identity-main">
      <h2 class="passport-card__name">Sam de Wit</h2>
      <p class="passport-card__one-liner">Ik wil graag iets met mensen doen.</p>
    </div>
  </div>
  <div class="passport-card__layers" role="progressbar" aria-valuenow="3" aria-valuemin="0" aria-valuemax="6">
    <div class="passport-card__layers-bars" aria-hidden="true">
      <span class="passport-card__layer passport-card__layer--1"></span>
      <span class="passport-card__layer passport-card__layer--2"></span>
      <span class="passport-card__layer passport-card__layer--3"></span>
      <span class="passport-card__layer"></span><span class="passport-card__layer"></span><span class="passport-card__layer"></span>
    </div>
    <p class="passport-card__layers-text">3 van 6 schalen – jouw kreeft groeit mee</p>
  </div>
</aside></div>`;
}

function tabs(active) {
  const t = (label, on, pill) => {
    const cls = on ? 'passport-tab passport-tab--active' : 'passport-tab';
    return `<button type="button" class="${cls}" role="tab" aria-selected="${on}">${label}${pill ? ` <span class="passport-tabs__pill">${pill}</span>` : ''}</button>`;
  };
  return `<div class="passport-tabs-shell"><div class="passport-tabs" role="tablist">
    ${t('Wie ik ben', active === 'dna')}
    ${t('Mijn tests', active === 'tests', '4')}
    ${t('Past bij?', active === 'fit')}
    ${t('Carrière', active === 'career')}
    ${t('Gegevens', active === 'data')}
  </div></div>`;
}

function overviewStrip() {
  return `<section class="passport-overview" aria-label="Overzicht">
  <div class="passport-overview__ring-block">
    <svg class="passport-dna-ring" viewBox="0 0 120 120" width="88" height="88" aria-hidden="true">
      <circle cx="60" cy="60" r="34" fill="var(--surface)" stroke="var(--gold-light)" stroke-width="1.5" stroke-dasharray="3 3"/>
      <image href="${lobsterSrc}" x="34" y="34" width="52" height="52"/>
    </svg>
    <div>
      <h2 class="passport-overview__title">Dit ben jij</h2>
      <p class="passport-overview__stage">Jouw kreeft · schaal 3</p>
    </div>
  </div>
</section>`;
}

function tile(text, icon, selected) {
  const sel = selected ? ' is-selected' : '';
  const pressed = selected ? 'true' : 'false';
  return `<button type="button" class="lobsy-choice-tile coach-prefs__tile${sel}" aria-pressed="${pressed}">
    <span class="coach-prefs__tile-icon" aria-hidden="true">${icon}</span>
    <span class="coach-prefs__tile-text">${text}</span>
  </button>`;
}

function coachPrefsActive() {
  return `<section class="coach-prefs-card" aria-labelledby="coach-prefs-title">
    <span class="mockup-badge">Nieuw · mockup</span>
    <h3 id="coach-prefs-title" class="passport-data__title">Hoe wil je tips van mij?</h3>
    <p class="passport-data__sub">Kies wat voor jou belangrijk is. Zo pas ik mijn uitleg aan.</p>
    <div class="coach-prefs__why" role="note">
      <span class="coach-prefs__why-icon" aria-hidden="true">${ICONS.ok}</span>
      <p style="margin:0"><strong>Alleen voor jouw tips.</strong> Niemand anders ziet dit. Het staat niet bij werkgevers en telt niet mee bij matching.</p>
    </div>
    <div class="coach-prefs__progress-row">
      <p class="coach-prefs__progress-label">Vraag 2 van 7</p>
      <p class="coach-prefs__progress-pct" aria-hidden="true">29%</p>
    </div>
    <div class="coach-prefs__progress-track" role="progressbar" aria-valuenow="2" aria-valuemin="0" aria-valuemax="7" aria-label="Vraag 2 van 7">
      <div class="coach-prefs__progress-fill" style="width:28.5%"></div>
    </div>
    <h4 class="coach-prefs__question">Wat maakt je onzeker als je aan ander werk denkt?</h4>
    <p class="coach-prefs__hint">Je mag meer antwoorden kiezen.</p>
    <div class="lobsy-choice-grid" role="group">
      ${tile('Ik weet niet wat er daarna komt', ICONS.future, true)}
      ${tile('Ik ben bang iets verkeerd te doen', ICONS.warn, false)}
      ${tile('Ik mis vaste uren of loon', ICONS.clock, true)}
      ${tile('Ik ken de mensen nog niet', ICONS.people, false)}
      ${tile('Niks, dat hoeft niet', ICONS.ok, false)}
    </div>
    <p class="coach-prefs__hint" style="margin-top:1rem;margin-bottom:0.5rem">Volgende vraag verschijnt na &quot;Volgende&quot;.</p>
    <div class="coach-prefs__actions">
      <button type="button" class="btn btn-ghost">Overslaan</button>
      <button type="button" class="btn btn-secondary">Later</button>
      <span class="coach-prefs__actions-spacer"></span>
      <button type="button" class="btn btn-primary">Volgende</button>
    </div>
  </section>`;
}

function coachPrefsOverview() {
  return `<section class="coach-prefs-card" aria-labelledby="coach-prefs-overview">
    <span class="mockup-badge">Overzicht · mockup</span>
    <h3 id="coach-prefs-overview" class="passport-data__title">Hoe wil je tips van mij?</h3>
    <p class="passport-data__sub">Zo pas ik mijn uitleg aan. Je kunt dit altijd wijzigen.</p>
    <div class="coach-prefs__why" role="note">
      <span class="coach-prefs__why-icon" aria-hidden="true">${ICONS.ok}</span>
      <p style="margin:0">Alleen jij en de kreeft zien dit. Geen score, geen label voor werkgevers.</p>
    </div>
    <h4 class="coach-prefs__summary-title">Jouw gekozen voorkeuren</h4>
    <dl class="coach-prefs__summary-list">
      <div><dt>Onzeker bij ander werk</dt><dd>Ik weet niet wat er daarna komt · Ik mis vaste uren of loon</dd></div>
      <div><dt>Helpt je zekerheid</dt><dd>Duidelijke volgende stap · Rustig tempo</dd></div>
      <div><dt>Zelf beslissen</dt><dd>Welke banen ik bekijk · Wanneer ik solliciteer</dd></div>
      <div><dt>Eerlijk en duidelijk</dt><dd>Geen beloftes die niet kloppen</dd></div>
    </dl>
    <div class="coach-prefs__actions">
      <button type="button" class="btn btn-secondary">Aanpassen</button>
      <button type="button" class="btn btn-ghost">Wissen</button>
    </div>
  </section>`;
}

function dataStack(extraCard) {
  return `<div class="passport-data mockup-data-stack">
    <div class="passport-data__intro-card">
      <p>Jouw gegevens. Werkgevers zien pas contact na jouw toestemming.</p>
    </div>
    <div class="profile-form competency-profile-card">
      <div class="passport-data__body">
        <label><span>Voornaam</span><input type="text" value="Sam" readonly /></label>
        <label><span>Achternaam</span><input type="text" value="de Wit" readonly /></label>
        <label><span>Telefoon</span><input type="tel" value="06-12345678" readonly /></label>
        <label><span>Over mij</span><textarea rows="3" readonly>Ik wil graag iets met mensen doen. Ik werk graag in een team.</textarea></label>
      </div>
      <div class="profile-save-bar"><button type="button" class="btn btn-primary">Opslaan</button></div>
    </div>
    <div class="passport-data__accordions">
      <div class="detail-card passport-data__item">
        <h3 class="passport-data__title">Beschikbaarheid</h3>
        <p class="passport-data__sub">Wanneer kun je werken?</p>
        <p class="passport-data__sub" style="margin-top:0.5rem;color:var(--text)">Ma–vr ochtend · per 1 maart</p>
      </div>
      <div class="detail-card passport-data__item">
        <h3 class="passport-data__title">Werkstatus</h3>
        <p class="passport-data__sub">Zoek je nu werk?</p>
        <p class="passport-data__sub" style="margin-top:0.5rem;color:var(--text)">Ik zoek werk · max. 20 min reizen</p>
      </div>
    </div>
    ${extraCard}
  </div>`;
}

function fitPanel() {
  const cell = (label, state) => `<li class="passport-fit__cell${state === 'warn' ? ' passport-fit__cell--warn' : ''}">
    <span class="passport-fit__cell-icon" aria-hidden="true">${state === 'warn' ? ICONS.warn : ICONS.ok}</span>
    <span class="passport-fit__cell-label">${label}</span>
  </li>`;
  return `<div class="passport-fit mockup-fit-fill">
    <div class="passport-fit__layout">
      <div class="detail-card passport-fit__card">
        <div class="passport-fit__head"><h3 class="passport-fit__h">Zelf een beroep proberen</h3></div>
        <p class="passport-fit__sub">
          <span class="passport-fit__sub-icon" aria-hidden="true">${ICONS.step}</span>
          <span>Gebaseerd op jouw tests. Geen belofte over een baan.</span>
        </p>
        <form class="passport-fit__form">
          <input class="passport-fit__input" type="text" value="Verkoper / winkel" readonly aria-label="Beroep" />
          <button type="button" class="btn btn-primary">Check</button>
        </form>
        <p class="passport-fit__last-line"><strong>Laatste check:</strong> verkoper · gisteren</p>
        <ul class="passport-fit__grid">
          ${cell('Past bij jouw Social-type', 'ok')}
          ${cell('Reistijd: oké met fiets', 'ok')}
          ${cell('Loon: kijk goed naar functie', 'warn')}
          ${cell('Diploma: geen harde eis', 'ok')}
        </ul>
      </div>
      <div class="detail-card passport-fit__vacancies">
        <h3 class="passport-fit__h">Vacatures die kunnen passen</h3>
        <p class="passport-fit__sub" style="margin-bottom:0.5rem">3 in de buurt · op basis van jouw voorkeuren</p>
        <ul class="passport-fit__similar">
          <li class="btn btn-secondary" style="font-size:0.85rem">Hema · Verkoper</li>
          <li class="btn btn-secondary" style="font-size:0.85rem">Kruidvat · Vulploeg</li>
          <li class="btn btn-secondary" style="font-size:0.85rem">Action · Winkelmedewerker</li>
        </ul>
        <p class="passport-fit__pct" style="margin-top:0.75rem;color:var(--muted)">Functiefit is een hulp voor jou — geen score voor de werkgever.</p>
      </div>
    </div>
  </div>`;
}

function passportLayout(tab, panelHtml) {
  return `<div class="panel-page passport-page passport-page--illustrated">
  <div class="passport-layout">
    ${sidebarCard(false)}
    <div class="passport-layout__main"><div class="passport-layout__main-flow">
      ${sidebarCard(true)}
      ${overviewStrip()}
      ${tabs(tab)}
      <article class="passport-panel" role="tabpanel">${panelHtml}</article>
    </div></div>
  </div>
</div>`;
}

function bubble(user, html) {
  const role = user ? 'lobsy-assistant__bubble--user' : 'lobsy-assistant__bubble--assistant';
  const inner = user
    ? `<p style="margin:0">${html}</p>`
    : `<img src="${lobsterSrc}" width="30" height="30" alt="" style="border-radius:6px;flex:none"><p style="margin:0">${html}</p>`;
  return `<div class="lobsy-assistant__bubble ${role}" style="display:flex;gap:0.5rem;align-items:flex-start;margin-bottom:0.65rem">${inner}</div>`;
}

const MSG_ZEKERHEID = bubble(
  false,
  'Jij zei dat een <strong>duidelijke volgende stap</strong> je helpt. Oké — zo pakken we het aan:<br><br><strong>1)</strong> Kijk op de banenkaart naar vacatures dichtbij.<br><strong>2)</strong> Kies er één die je interessant vindt.<br><strong>3)</strong> Druk op Bewaar — dan staat die klaar.<br><br>Ik weet niet welke baan het wordt. Jij kiest. Wil je dat ik stap 1 uitleg?',
);

const MSG_AUTONOMIE = bubble(
  false,
  'Jij wilt <strong>zelf kiezen welke banen</strong> je bekijkt — prima.<br><br>Je mag kiezen:<br>• <strong>Banenkaart</strong> — zelf zoeken.<br>• <strong>Past bij?</strong> — voorstellen uit jouw tests.<br>• <strong>Even niets</strong> — dat mag ook.<br><br>Wat wil jij nu doen? Ik vul niks voor je in.',
);

function coachThread(variant) {
  const msg = variant === 'zekerheid' ? MSG_ZEKERHEID : MSG_AUTONOMIE;
  return `${bubble(true, 'Wat kan ik nu het beste doen?')}${msg}`;
}

function coachPanel(messages, inline) {
  const pos = inline
    ? `position:relative;inset:auto;max-width:100%;width:100%;align-items:stretch;pointer-events:auto`
    : '';
  return `<aside class="lobsy-assistant lobsy-assistant--coach lobsy-assistant--skin-warm has-bottom-nav mockup-coach-open" style="${pos}" aria-label="Lobsy-hulp">
  <div class="lobsy-assistant__panel">
    <header class="lobsy-assistant__header">
      <div class="lobsy-assistant__brand">
        <img class="lobsy-assistant__avatar" src="${lobsterSrc}" width="40" height="40" alt="">
        <div><h2>Lobsy-hulp</h2><p class="lobsy-assistant__lead">Ik antwoord op basis van jouw gegevens.</p></div>
      </div>
      <div class="lobsy-assistant__header-actions"><button type="button" class="lobsy-assistant__icon-btn" aria-label="Sluiten">×</button></div>
    </header>
    <div class="lobsy-assistant__thread">${messages}</div>
    <div class="lobsy-assistant__compose">
      <textarea rows="2" placeholder="Stel je vraag…" style="width:100%;border:1px solid var(--border);border-radius:8px;padding:0.5rem;font:inherit;min-height:2.5rem"></textarea>
    </div>
  </div>
</aside>`;
}

function coachDockFab() {
  return `<div class="lobsy-coach-dock" data-lobsy-coach>
    <button type="button" class="lobsy-coach-dock__btn lobsy-coach-dock__btn--warm" aria-label="Vraag">
      <span class="lobsy-coach-dock__medallion"><img src="${lobsterSrc}" width="40" height="40" alt=""></span>
      <span class="lobsy-coach-dock__label">Vraag</span>
    </button>
  </div>`;
}

function shell(label, bodyClass, inner) {
  return `${head(label, 'desktop')}
<body class="app-shell app-shell--candidate has-bottom-nav ${bodyClass || ''}" data-lobsy-coach style="--bottom-nav-h:64px">
${pageChrome(label)}
<main class="app-main mockup-main">${inner}</main>
<nav class="mockup-bottom-nav" aria-hidden="true"></nav>
${bodyClass?.includes('mockup-coach-page') ? `${coachDockFab()}` : coachDockFab()}
</body></html>`;
}

function htmlQuestions(vp) {
  const mobile = vp === 'mobile';
  return `${head('Vragenstap Gegevens', mobile ? 'mobile' : 'desktop')}
<body class="app-shell app-shell--candidate has-bottom-nav" data-lobsy-coach style="--bottom-nav-h:64px">
${pageChrome('Mockup — vragenstap in tab Gegevens')}
<main class="app-main mockup-main">${passportLayout('data', dataStack(coachPrefsActive()))}</main>
<nav class="mockup-bottom-nav" aria-hidden="true"></nav>
${coachDockFab()}
</body></html>`;
}

function htmlOverview(vp) {
  return `${head('Overzicht na invullen', vp === 'mobile' ? 'mobile' : 'desktop')}
<body class="app-shell app-shell--candidate has-bottom-nav" data-lobsy-coach style="--bottom-nav-h:64px">
${pageChrome('Mockup — overzicht coach-voorkeuren')}
<main class="app-main mockup-main">${passportLayout('data', dataStack(coachPrefsOverview()))}</main>
<nav class="mockup-bottom-nav" aria-hidden="true"></nav>
${coachDockFab()}
</body></html>`;
}

function htmlCoach(vp, variant) {
  const title = variant === 'zekerheid' ? 'Mockup — Past bij? + kreeft (zekerheid)' : 'Mockup — Past bij? + kreeft (autonomie)';
  return `${head(title, vp === 'mobile' ? 'mobile' : 'desktop')}
<body class="app-shell app-shell--candidate has-bottom-nav mockup-coach-page" data-lobsy-coach style="--bottom-nav-h:64px">
${pageChrome(title)}
<main class="app-main mockup-main">${passportLayout('fit', fitPanel())}</main>
<nav class="mockup-bottom-nav" aria-hidden="true"></nav>
${coachPanel(coachThread(variant), false)}
${coachDockFab()}
</body></html>`;
}

function htmlCompare() {
  return `${head('Vergelijking kreeft-toon', 'desktop')}
<body class="app-shell mockup-coach-page" style="--bottom-nav-h:64px;background:var(--bg)">
${pageChrome('Mockup — zekerheid vs autonomie (zelfde vraag)')}
<main class="app-main mockup-main" style="padding-top:calc(var(--app-header-height) + 2rem)">
<div class="mockup-compare">
  <div class="mockup-compare__col">
    <p class="mockup-compare__label">Voorkeur: duidelijke volgende stap</p>
    ${coachPanel(`${bubble(true, 'Wat kan ik nu het beste doen?')}${MSG_ZEKERHEID}`, true)}
  </div>
  <div class="mockup-compare__col">
    <p class="mockup-compare__label">Voorkeur: zelf banen kiezen</p>
    ${coachPanel(`${bubble(true, 'Wat kan ik nu het beste doen?')}${MSG_AUTONOMIE}`, true)}
  </div>
</div>
<p style="text-align:center;font-size:0.85rem;color:var(--muted);max-width:52rem;margin:1rem auto 2rem">Achtergrond op de echte pagina: tab <strong>Past bij?</strong> met functie-check en vacaturetegels (zie mockups 03 en 04).</p>
</main>
</body></html>`;
}

const shots = [
  { name: '01-vragenstap-paspoort-gegevens', html: (vp) => htmlQuestions(vp) },
  { name: '02-overzicht-na-invullen', html: (vp) => htmlOverview(vp) },
  { name: '03-kreeft-past-bij-zekerheid', html: (vp) => htmlCoach(vp, 'zekerheid') },
  { name: '04-kreeft-past-bij-autonomie', html: (vp) => htmlCoach(vp, 'autonomie') },
  { name: '05-kreeft-vergelijking', html: () => htmlCompare(), desktopOnly: true },
];

const viewports = [
  { tag: '1440', width: 1440, height: 900 },
  { tag: '390', width: 390, height: 844 },
];

for (const d of [outArtifacts, outRepo, previewsDir]) fs.mkdirSync(d, { recursive: true });

for (const shot of shots) {
  const vps = shot.desktopOnly ? viewports.filter((v) => v.tag === '1440') : viewports;
  for (const vp of vps) {
    const html = shot.html(vp.tag === '390' ? 'mobile' : 'desktop');
    const base = `${shot.name}-${vp.tag}`;
    fs.writeFileSync(path.join(previewsDir, `${base}.html`), html);
  }
}

const browser = await chromium.launch({ headless: true });
for (const shot of shots) {
  const vps = shot.desktopOnly ? viewports.filter((v) => v.tag === '1440') : viewports;
  for (const vp of vps) {
    const page = await browser.newPage({ viewport: { width: vp.width, height: vp.height } });
    const html = shot.html(vp.tag === '390' ? 'mobile' : 'desktop');
    await page.setContent(html, { waitUntil: 'load' });
    await page.waitForTimeout(500);
    const base = `${shot.name}-${vp.tag}.png`;
    const pngPath = path.join(outArtifacts, base);
    await page.screenshot({ path: pngPath, fullPage: true });
    fs.copyFileSync(pngPath, path.join(outRepo, base));
    console.log('saved', pngPath);
    await page.close();
  }
}
await browser.close();
console.log('done →', outArtifacts, '+', outRepo);
