/**
 * Statische mockups: coach-voorkeuren (SCARF) — paspoort Gegevens + kreeft-panel.
 * Output: /opt/cursor/artifacts/scarf-coach-mockup/
 */
import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const outDir = process.env.OUT_DIR || '/opt/cursor/artifacts/scarf-coach-mockup';
const lobsterB64 = fs
  .readFileSync(path.join(root, 'Jobsy.Web/wwwroot/images/brand/lobsy-128.webp'))
  .toString('base64');
const lobsterSrc = `data:image/webp;base64,${lobsterB64}`;

function readCss(...parts) {
  return parts.map((p) => fs.readFileSync(path.join(root, p), 'utf8')).join('\n');
}

const styles = readCss(
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

const head = (title, vp) => `<!DOCTYPE html><html lang="nl"><head>
<meta charset="utf-8">
<title>${title}</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
:root {
  --font-display: "Segoe UI", system-ui, sans-serif;
  --r-md: 12px; --r-sm: 8px; --r-lg: 20px;
  --bottom-nav-h: 64px;
  --app-header-height: 56px;
}
body { margin: 0; background: var(--bg, #f5f2ee); color: var(--text); font-family: "Segoe UI", system-ui, sans-serif; }
.mockup-only-banner {
  position: sticky; top: var(--app-header-height); z-index: 45;
  padding: 0.35rem 0.75rem; text-align: center; font-size: 0.75rem; font-weight: 600;
  background: var(--gold-soft); color: var(--gold-ink); border-bottom: 1px solid var(--border);
}
${styles}
@media (max-width: 1023px) {
  .passport-layout__card--desktop { display: none !important; }
  .passport-layout__card--mobile { display: block !important; }
}
@media (min-width: 1024px) {
  .passport-layout { display: grid; grid-template-columns: minmax(280px, 330px) minmax(0, 1fr); gap: 1.25rem; }
  .passport-layout__card--desktop { display: block !important; }
  .passport-layout__card--mobile { display: none !important; }
}
${vp === 'mobile' ? `
.passport-tabs { flex-wrap: nowrap; overflow-x: auto; }
` : ''}
</style></head>`;

function sidebarCard(compact) {
  const wrap = compact
    ? 'passport-layout__card passport-layout__card--mobile'
    : 'passport-layout__sidebar passport-layout__card passport-layout__card--desktop';
  return `<div class="${wrap}"><aside class="passport-card${compact ? ' passport-card--compact' : ''}">
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

function tabs(dataActive) {
  const t = (id, label, active, pill) => {
    const cls = active ? 'passport-tab passport-tab--active' : 'passport-tab';
    const pillHtml = pill ? ` <span class="passport-tabs__pill">${pill}</span>` : '';
    return `<button type="button" class="${cls}" role="tab" aria-selected="${active}">${label}${pillHtml}</button>`;
  };
  return `<div class="passport-tabs-shell"><div class="passport-tabs" role="tablist">
    ${t('dna', 'Wie ik ben', dataActive === 'dna')}
    ${t('tests', 'Mijn tests', dataActive === 'tests', '4')}
    ${t('fit', 'Past bij?', dataActive === 'fit')}
    ${t('career', 'Carrière', dataActive === 'career')}
    ${t('data', 'Gegevens', dataActive === 'data')}
  </div></div>`;
}

function existingDataFields() {
  return `
  <div class="profile-form competency-profile-card">
    <div class="passport-data__body">
      <label><span>Voornaam</span><input type="text" value="Sam" readonly /></label>
      <label><span>Achternaam</span><input type="text" value="de Wit" readonly /></label>
      <label><span>Telefoon</span><input type="tel" value="06-12345678" readonly /></label>
      <label><span>Over mij</span><textarea rows="3" readonly>Ik wil graag iets met mensen doen. Ik werk graag in een team.</textarea></label>
    </div>
    <div class="profile-save-bar"><button type="button" class="btn btn-primary">Opslaan</button></div>
  </div>
  <div class="detail-card passport-data__item">
    <h3 class="passport-data__title">Beschikbaarheid</h3>
    <p class="passport-data__sub">Wanneer kun je werken?</p>
    <p class="muted" style="margin:0;font-size:0.875rem">Ma–vr ochtend · per 1 maart beschikbaar</p>
  </div>
  <div class="detail-card passport-data__item">
    <h3 class="passport-data__title">Werkstatus</h3>
    <p class="passport-data__sub">Zoek je nu werk?</p>
    <p class="muted" style="margin:0;font-size:0.875rem">Ik zoek werk · max. 20 min reizen</p>
  </div>`;
}

function coachPrefsSection() {
  return `
  <div class="detail-card passport-data__item passport-data__item--coach-prefs is-open" id="passport-data-coach-prefs">
    <span class="mockup-badge">Nieuw · mockup</span>
    <h3 class="passport-data__title">Hoe wil je tips van mij?</h3>
    <p class="passport-data__sub">Kies wat voor jou belangrijk is. Zo pas ik mijn uitleg aan. Je mag alles overslaan of later invullen.</p>
    <p class="coach-prefs__progress" aria-live="polite">Vraag 2 van 7</p>
    <h4 class="coach-prefs__question">Wat maakt je onzeker als je aan ander werk denkt?</h4>
    <p class="coach-prefs__hint">Je mag meer antwoorden kiezen.</p>
    <div class="lobsy-choice-grid" role="group" aria-label="Antwoordopties">
      <button type="button" class="lobsy-choice-tile is-selected" aria-pressed="true">Ik weet niet wat er daarna komt</button>
      <button type="button" class="lobsy-choice-tile" aria-pressed="false">Ik ben bang iets verkeerd te doen</button>
      <button type="button" class="lobsy-choice-tile is-selected" aria-pressed="true">Ik mis vaste uren of loon</button>
      <button type="button" class="lobsy-choice-tile" aria-pressed="false">Ik ken de mensen nog niet</button>
      <button type="button" class="lobsy-choice-tile" aria-pressed="false">Niks, dat hoeft niet</button>
    </div>
    <h4 class="coach-prefs__question" style="margin-top:1.25rem">Wat helpt jou om je zeker te voelen?</h4>
    <p class="coach-prefs__hint">Kies wat het beste past.</p>
    <div class="lobsy-choice-grid">
      <button type="button" class="lobsy-choice-tile is-selected" aria-pressed="true">Duidelijke volgende stap</button>
      <button type="button" class="lobsy-choice-tile" aria-pressed="false">Vaste afspraken</button>
      <button type="button" class="lobsy-choice-tile" aria-pressed="false">Iemand die me uitlegt</button>
      <button type="button" class="lobsy-choice-tile" aria-pressed="false">Rustig tempo</button>
    </div>
    <div class="coach-prefs__actions">
      <button type="button" class="btn btn-primary">Opslaan</button>
      <button type="button" class="btn btn-secondary">Later</button>
      <button type="button" class="btn btn-ghost">Overslaan</button>
    </div>
    <p class="passport-data__private-note" style="margin-top:0.75rem;font-size:0.8rem;color:var(--muted)">
      Alleen jij en de kreeft-coach zien dit. Het staat niet in je paspoort voor werkgevers en telt niet mee bij matching.
    </p>
  </div>`;
}

function questionsHtml(vp) {
  return `${head('Coach-voorkeuren — Gegevens', vp)}
<body class="app-shell app-shell--candidate has-bottom-nav" data-lobsy-coach style="--bottom-nav-h:64px;padding-top:var(--app-header-height)">
<header style="position:fixed;top:0;left:0;right:0;height:var(--app-header-height);background:#0f2d5c;z-index:50;display:flex;align-items:center;padding:0 1rem;color:#fff;font-weight:600;">Mijn paspoort</header>
<div class="mockup-only-banner">Mockup — coach-voorkeuren in tab Gegevens</div>
<main class="app-main">
<div class="panel-page passport-page passport-page--illustrated">
<div class="passport-layout">
${sidebarCard(false)}
<div class="passport-layout__main"><div class="passport-layout__main-flow">
${sidebarCard(true)}
<section class="passport-overview" style="background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:1rem;margin-bottom:0.75rem">
  <h2 style="margin:0 0 0.25rem;font-size:1.1rem;color:var(--brand)">Dit ben jij</h2>
  <p style="margin:0;font-size:0.875rem;color:var(--muted)">Jouw kreeft · schaal 3</p>
</section>
${tabs('data')}
<article class="passport-panel" role="tabpanel">
  <div class="passport-data">
    <p class="passport-data__intro">Jouw gegevens. Werkgevers zien pas contact na jouw toestemming.</p>
    ${existingDataFields()}
    ${coachPrefsSection()}
  </div>
</article>
</div></div>
</div>
</div>
</main>
${coachDock(false)}
<nav class="bottom-nav" style="position:fixed;bottom:0;left:0;right:0;height:var(--bottom-nav-h);background:var(--surface);border-top:1px solid var(--border);z-index:40" aria-hidden="true"></nav>
</body></html>`;
}

function coachDock(panelOpen) {
  const panel = panelOpen
    ? `<aside class="lobsy-assistant lobsy-assistant--coach lobsy-assistant--skin-warm has-bottom-nav mockup-coach-open" aria-label="Lobsy-hulp">
    <div class="lobsy-assistant__panel" id="lobsy-assistant-panel">
      <header class="lobsy-assistant__header">
        <div class="lobsy-assistant__brand">
          <img class="lobsy-assistant__avatar" src="${lobsterSrc}" width="40" height="40" alt="">
          <div>
            <h2>Lobsy-hulp</h2>
            <p class="lobsy-assistant__lead">Ik antwoord op basis van jouw gegevens.</p>
          </div>
        </div>
        <div class="lobsy-assistant__header-actions">
          <button type="button" class="lobsy-assistant__icon-btn" aria-label="Sluiten">×</button>
        </div>
      </header>
      <div class="lobsy-assistant__thread" style="overflow:auto;flex:1;min-height:0;padding:0.25rem 0">
        ${panelOpen.messages}
      </div>
      <div class="lobsy-assistant__compose">
        <textarea rows="1" placeholder="Stel je vraag…" style="width:100%;border:1px solid var(--border);border-radius:8px;padding:0.5rem;font:inherit"></textarea>
      </div>
    </div>
  </aside>`
    : '';
  return `${panel}
<div class="lobsy-coach-dock" data-lobsy-coach>
  <button type="button" id="lobsy-coach-btn" class="lobsy-coach-dock__btn lobsy-coach-dock__btn--warm" aria-label="Vraag">
    <span class="lobsy-coach-dock__medallion"><img src="${lobsterSrc}" width="40" height="40" alt=""></span>
    <span class="lobsy-coach-dock__label">Vraag</span>
  </button>
</div>`;
}

function bubble(assistant, text) {
  const cls = assistant ? 'lobsy-assistant__bubble lobsy-assistant__bubble--assistant' : 'lobsy-assistant__bubble lobsy-assistant__bubble--user';
  const inner = assistant
    ? `<img src="${lobsterSrc}" width="28" height="28" alt="" style="border-radius:6px;flex:none"><p style="margin:0">${text}</p>`
    : `<p style="margin:0">${text}</p>`;
  return `<div class="${cls}" style="display:flex;gap:0.5rem;margin-bottom:0.65rem;align-items:flex-start">${inner}</div>`;
}

const certaintyMessages = `
${bubble(false, 'Wat kan ik nu het beste doen?')}
${bubble(true, 'Jij zei dat een <strong>duidelijke volgende stap</strong> je helpt. Oké — zo pakken we het aan:<br><br>1) Kijk op de banenkaart naar vacatures dichtbij.<br>2) Kies er één die je interessant vindt.<br>3) Druk op &quot;Bewaar&quot; — dan staat die voor je klaar.<br><br>Ik weet niet welke baan het wordt. Jij kiest. Wil je dat ik stap 1 uitleg?')}`;

const autonomyMessages = `
${bubble(false, 'Wat kan ik nu het beste doen?')}
${bubble(true, 'Jij wilt <strong>zelf kiezen welke banen</strong> je bekijkt — prima. Je hoeft niet alles tegelijk.<br><br>Je mag kiezen:<br>• Banenkaart: zelf zoeken op kaart.<br>• Paspoort → Past bij?: voorstellen op basis van jouw tests.<br>• Even niets: dat mag ook.<br><br>Wat wil jij nu doen? Ik vul niks voor je in.')}`;

function coachHtml(vp, variant) {
  const messages = variant === 'zekerheid' ? certaintyMessages : autonomyMessages;
  const label = variant === 'zekerheid' ? 'Kreeft — zekerheidsvoorkeur' : 'Kreeft — autonomievoorkeur';
  const panelOpen = { messages };
  return `${head(label, vp)}
<body class="app-shell app-shell--candidate has-bottom-nav mockup-coach-page" data-lobsy-coach style="--bottom-nav-h:64px;padding-top:var(--app-header-height)">
<header style="position:fixed;top:0;left:0;right:0;height:var(--app-header-height);background:#0f2d5c;z-index:50;display:flex;align-items:center;padding:0 1rem;color:#fff;font-weight:600;">Mijn paspoort</header>
<div class="mockup-only-banner">Mockup — ${label}</div>
<main class="app-main">
<div class="panel-page passport-page passport-page--illustrated">
<div class="passport-layout">
${sidebarCard(false)}
<div class="passport-layout__main"><div class="passport-layout__main-flow">
${sidebarCard(true)}
${tabs('fit')}
<article class="passport-panel"><p class="login-lead" style="color:var(--muted)">Pagina op de achtergrond — kreeft-coach staat open.</p></article>
</div></div>
</div>
</div>
</main>
${coachDock(panelOpen)}
<nav class="bottom-nav" style="position:fixed;bottom:0;left:0;right:0;height:var(--bottom-nav-h);background:var(--surface);border-top:1px solid var(--border);z-index:40" aria-hidden="true"></nav>
</body></html>`;
}

const shots = [
  { name: '01-vragenstap-paspoort-gegevens', html: (vp) => questionsHtml(vp) },
  { name: '02-kreeft-bericht-zekerheid', html: (vp) => coachHtml(vp, 'zekerheid') },
  { name: '03-kreeft-bericht-autonomie', html: (vp) => coachHtml(vp, 'autonomie') },
];

const viewports = [
  { tag: '1440', width: 1440, height: 900 },
  { tag: '390', width: 390, height: 844 },
];

fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(path.join(root, 'docs/design/scarf-coach-mockup/previews'), { recursive: true });

for (const shot of shots) {
  for (const vp of viewports) {
    const html = shot.html(vp.tag === '390' ? 'mobile' : 'desktop');
    const htmlPath = path.join(root, 'docs/design/scarf-coach-mockup/previews', `${shot.name}-${vp.tag}.html`);
    fs.writeFileSync(htmlPath, html);
  }
}

const browser = await chromium.launch({ headless: true });

for (const shot of shots) {
  for (const vp of viewports) {
    const page = await browser.newPage({ viewport: { width: vp.width, height: vp.height } });
    const html = shot.html(vp.tag === '390' ? 'mobile' : 'desktop');
    await page.setContent(html, { waitUntil: 'load' });
    await page.waitForTimeout(400);
    const outfile = path.join(outDir, `${shot.name}-${vp.tag}.png`);
    if (shot.name.startsWith('01')) {
      await page.screenshot({ path: outfile, fullPage: true });
    } else {
      await page.screenshot({ path: outfile, fullPage: false });
    }
    console.log('saved', outfile);
    await page.close();
  }
}

await browser.close();
console.log('done →', outDir);
