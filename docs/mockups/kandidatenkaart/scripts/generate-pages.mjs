import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const pagesDir = path.join(root, "pages");
fs.mkdirSync(pagesDir, { recursive: true });

const logoRel = "../shared/assets/lobsy-64.webp";

function mapSvg({ selectedRegion = null, locked = false }) {
  const sel = selectedRegion
    ? `<path class="heat heat--sel" d="M420 280 Q480 250 520 290 T560 360 Q530 400 470 390 T400 340 Z"/>`
    : "";
  const heats = locked
    ? `<path class="heat" d="M200 180 Q260 160 300 200 T340 280 Q300 320 240 300 T180 240 Z"/>
       <path class="heat heat--strong" d="M480 220 Q540 200 580 250 T600 330 Q560 380 500 370 T440 280 Z"/>`
    : `<path class="heat" d="M180 200 Q240 170 290 210 T330 300 Q280 350 220 330 T160 260 Z"/>
       <path class="heat heat--strong" d="M420 280 Q480 250 520 290 T560 360 Q530 400 470 390 T400 340 Z"/>
       <path class="heat" d="M520 140 Q580 120 620 170 T640 250 Q590 290 530 270 T480 190 Z"/>
       <path class="heat heat--strong" d="M300 380 Q360 360 400 400 T420 470 Q370 510 310 490 T260 420 Z"/>`;
  return `<svg class="kk-map-svg" viewBox="0 0 800 560" aria-hidden="true">
    <rect class="land" width="800" height="560"/>
    <path class="water" d="M0 420 Q120 400 200 430 T400 450 L0 560 Z"/>
    <g class="road"><path d="M120 80 L680 120"/><path d="M80 280 L720 300"/><path d="M200 80 L240 520"/></g>
    ${heats}
    ${sel}
    <circle class="radius" cx="400" cy="300" r="145"/>
    <circle class="employer-pin" cx="400" cy="300" r="7"/>
  </svg>`;
}

function shellStart(title) {
  return `<!DOCTYPE html>
<html lang="nl" class="vp-desktop">
<head>
  <meta charset="utf-8"/>
  <title>${title}</title>
  <link rel="stylesheet" href="../shared/kk.css"/>
</head>
<body>
<div class="wg-shell wg-has-bottom-nav">
  <header class="wg-topbar">
    <a class="wg-topbar__brand" href="#">
      <img class="wg-topbar__logo" src="${logoRel}" width="28" height="28" alt=""/>
      <span>Demo Bakkerij Noord</span>
    </a>
    <div class="wg-topbar__end">
      <span class="wg-topbar__pill">Filialen: Amersfoort</span>
      <span aria-hidden="true">DB</span>
    </div>
  </header>
  <div class="wg-shell__body">
    <aside class="wg-sidebar" aria-label="Navigatie">
      <span class="wg-sidebar__group-label">Vacatures</span>
      <a class="wg-sidebar__link" href="#">Dashboard</a>
      <a class="wg-sidebar__link" href="#">Vacatures</a>
      <a class="wg-sidebar__link" href="#">Sollicitaties</a>
      <span class="wg-sidebar__group-label">Markt &amp; talent</span>
      <a class="wg-sidebar__link" href="#">Kandidaatinzichten</a>
      <a class="wg-sidebar__link is-active" href="#">Kandidatenkaart</a>
      <a class="wg-sidebar__link" href="#">Talentpool</a>
    </aside>
    <main class="wg-main">`;
}

const shellEnd = `
    </main>
  </div>
  <nav class="wg-bottom-nav" aria-label="Mobiel menu">
    <ul class="wg-bottom-nav__list">
      <li><a class="wg-bottom-nav__link" href="#">Home</a></li>
      <li><a class="wg-bottom-nav__link" href="#">Vacatures</a></li>
      <li><a class="wg-bottom-nav__link" href="#">Sollicitaties</a></li>
      <li><a class="wg-bottom-nav__link is-active" href="#">Kaart</a></li>
      <li><a class="wg-bottom-nav__link" href="#">Meer</a></li>
    </ul>
  </nav>
</div>
</body>
</html>`;

function headerBlock(badge, badgeClass = "kk-badge--gold") {
  return `
      <nav class="kk-crumbs" aria-label="Kruimelpad">
        <span>Markt &amp; talent</span><span>/</span><span>Kandidatenkaart</span>
      </nav>
      <header class="kk-header">
        <div>
          <h1>Kandidatenkaart</h1>
          <p class="kk-header__lead">Omgekeerde banenkaart: zie waar vindbare kandidaten passen bij jouw vacature. Alleen geaggregeerde gebieden — nooit individuele personen.</p>
        </div>
        <span class="kk-badge ${badgeClass}">${badge}</span>
      </header>
      <div class="kk-privacy">
        <svg class="i" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3l8 4v5c0 5-3.5 9-8 10-4.5-1-8-5-8-10V7l8-4z"/></svg>
        <p><strong>Privacy by design.</strong> Gebieden tonen pas vanaf 30 opt-in kandidaten. Geen namen, profielen of exacte locaties — alleen tellingen en match-verdeling.</p>
      </div>`;
}

function filtersBlock(disabled = false) {
  const dis = disabled ? "disabled" : "";
  return `
      <div class="kk-filters">
        <div class="kk-field">
          <label for="vac">Vacature of profiel</label>
          <div class="kk-control kk-control--wide" id="vac" ${dis}>
            <svg class="i" viewBox="0 0 24 24"><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
            <span><strong>Productiemedewerker</strong> · Amersfoort</span>
          </div>
        </div>
        <div class="kk-field">
          <label>Min. matchscore</label>
          <div class="kk-seg" role="group" aria-label="Minimale matchscore">
            <span>60%</span><span class="is-on">70%</span><span>80%</span>
          </div>
        </div>
        <div class="kk-field">
          <label>Reisafstand</label>
          <div class="kk-seg" role="group" aria-label="Reisafstand">
            <span>15 km</span><span class="is-on">25 km</span><span>40 km</span><span>60 km</span>
          </div>
        </div>
      </div>`;
}

function areaPanel(selected = false) {
  if (!selected) {
    return `
          <div class="kk-panel">
            <h2>Gebiedsinzicht</h2>
            <p class="kk-note">Klik een rood gebied op de kaart om tellingen en match-verdeling te zien. Alleen kandidaten met <strong>Beschikbaar / vindbaar voor werkgevers</strong> tellen mee.</p>
            <div class="kk-upsell" style="margin-top:1rem">
              <b>Tip</b>
              Verhoog de matchdrempel om gerichter uit te nodigen — uitnodigingen blijven anoniem tot de kandidaat reageert.
            </div>
          </div>`;
  }
  return `
          <div class="kk-panel">
            <h2>Regio Amersfoort–Leusden</h2>
            <p class="kk-stat-line">42 kandidaten <span>· 12 boven 80% match</span></p>
            <div class="kk-bars" aria-label="Matchverdeling">
              <div class="kk-bar"><span>80%+</span><div class="kk-bar__track"><i class="kk-bar__fill kk-bar__fill--high" style="width:29%"></i></div><span>12</span></div>
              <div class="kk-bar"><span>70–79%</span><div class="kk-bar__track"><i class="kk-bar__fill" style="width:48%"></i></div><span>20</span></div>
              <div class="kk-bar"><span>60–69%</span><div class="kk-bar__track"><i class="kk-bar__fill" style="width:24%"></i></div><span>10</span></div>
            </div>
            <p class="kk-note">Geaggregeerd gebied (≥ 30 kandidaten). Geen individuele profielen zichtbaar.</p>
            <button type="button" class="kk-btn kk-btn--primary" style="width:100%;margin-top:0.75rem">Uitnodiging om te solliciteren</button>
          </div>`;
}

function areaList(selectedId = "amersfoort") {
  const items = [
    { id: "amersfoort", title: "Amersfoort – Leusden", meta: "42 kandidaten · 12 boven 80%", sel: true },
    { id: "soest", title: "Soest – Baarn", meta: "38 kandidaten · 9 boven 80%", sel: false },
    { id: "utrecht", title: "Utrecht west", meta: "56 kandidaten · 18 boven 80%", sel: false },
    { id: "hilversum", title: "Hilversum", meta: "31 kandidaten · 6 boven 80%", sel: false },
  ];
  return items
    .map(
      (it) => `<li class="kk-area-list__item${it.id === selectedId ? " is-selected" : ""}">
        <div class="kk-area-list__title">${it.title}</div>
        <div class="kk-area-list__meta">${it.meta}</div>
        <div class="kk-area-list__bands"><span class="kk-band-pill">70%+ · ${it.id === "amersfoort" ? "32" : "24"}</span></div>
      </li>`
    )
    .join("\n");
}

function mapBlock({ locked, selected }) {
  const lock = locked
    ? `<div class="kk-map-lock">
        <div class="kk-map-lock__card">
          <div class="kk-map-lock__icon" aria-hidden="true"><svg class="i" viewBox="0 0 24 24" style="stroke:#fff"><rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/></svg></div>
          <h2>Marktinzicht met kandidatenkaart</h2>
          <p>Bekijk waar vindbare kandidaten passen bij jouw vacature — rustig en geaggregeerd, zonder individuele gegevens.</p>
          <button type="button" class="kk-btn kk-btn--primary">Meer informatie</button>
          <p class="kk-map-lock__fine">Prijs en pakket volgen in overleg — placeholder, geen tarieven in deze mockup.</p>
        </div>
      </div>`
    : "";
  return `
        <div class="map-pane${locked ? " map-pane--locked" : ""}">
          <div class="map-pane__top-chrome">
            <div class="map-legend">
              <div class="map-legend__row"><span class="map-legend__swatch"></span> Vindbare kandidaten (≥ 30 per gebied)</div>
            </div>
          </div>
          <div class="map-stage">${mapSvg({ selectedRegion: selected, locked })}</div>
          ${lock}
        </div>`;
}

function layoutA(state) {
  const locked = state === "preview";
  const selected = state === "area" || state === "invite";
  const aside = locked
    ? `<div class="kk-panel kk-upsell"><b>Preview</b> Filters en gebieden worden vrijgegeven zodra marktinzicht actief is op jullie vestiging.</div>`
    : areaPanel(selected);
  return `
      <div class="kk-layout-a">
        <div class="kk-layout-a__map">${mapBlock({ locked, selected })}</div>
        <aside class="kk-layout-a__aside">${aside}</aside>
      </div>`;
}

function layoutB(state) {
  const locked = state === "preview";
  const selected = state === "area" || state === "invite";
  const listPanel = locked
    ? `<div class="kk-panel"><p class="kk-note">Gebiedslijst beschikbaar in volledige versie. Nu alleen kaart-preview.</p></div>`
    : `<ul class="kk-area-list">${areaList()}</ul>
       <button type="button" class="kk-btn kk-btn--primary" style="width:100%">Uitnodiging voor geselecteerd gebied</button>`;
  return `
      <div class="kk-layout-b">
        <div class="kk-layout-b__list">${listPanel}</div>
        <div class="kk-layout-b__map">${mapBlock({ locked, selected: selected || !locked })}</div>
      </div>`;
}

function inviteOverlay() {
  return `
      <div class="kk-scrim" role="dialog" aria-modal="true" aria-labelledby="invite-title">
        <div class="kk-modal">
          <h2 id="invite-title">Uitnodiging om te solliciteren</h2>
          <div class="kk-modal__anon">
            <svg class="i" viewBox="0 0 24 24"><path d="M12 3l8 4v5c0 5-3.5 9-8 10-4.5-1-8-5-8-10V7l8-4z"/></svg>
            <p>Anoniem naar kandidaten in <strong>Regio Amersfoort–Leusden</strong>. Zij zien jouw vacature en kiezen zelf of ze reageren. Geen namen of contactgegevens worden gedeeld.</p>
          </div>
          <p>Vacature: <strong>Productiemedewerker · Amersfoort</strong> · geschat 42 ontvangers (voldoet aan filters).</p>
          <label class="kk-field"><span>Korte toelichting (optioneel)</span>
            <textarea placeholder="Bijv. we zoeken iemand voor het ochtendteam, 24–32 uur per week."></textarea>
          </label>
          <div class="kk-modal__actions">
            <button type="button" class="kk-btn">Annuleren</button>
            <button type="button" class="kk-btn kk-btn--primary">Uitnodiging versturen</button>
          </div>
        </div>
      </div>`;
}

function writePage(variant, state) {
  const isA = variant === "a";
  const titles = {
    preview: `Kandidatenkaart ${variant.toUpperCase()} — preview`,
    full: `Kandidatenkaart ${variant.toUpperCase()} — volledig`,
    area: `Kandidatenkaart ${variant.toUpperCase()} — gebied`,
    invite: `Kandidatenkaart ${variant.toUpperCase()} — uitnodiging`,
  };
  const badge =
    state === "preview"
      ? "Preview · add-on"
      : "Marktinzicht actief";
  const badgeClass = state === "preview" ? "kk-badge--free" : "kk-badge--gold";
  let body = headerBlock(badge, badgeClass);
  body += filtersBlock(state === "preview");
  body += isA ? layoutA(state) : layoutB(state);
  if (state === "invite") body += inviteOverlay();

  const html = shellStart(titles[state]) + body + shellEnd;
  const file = path.join(pagesDir, `${variant}-${state}.html`);
  fs.writeFileSync(file, html, "utf8");
  console.log("wrote", path.relative(root, file));
}

fs.mkdirSync(path.join(root, "shared", "assets"), { recursive: true });
const srcLogo = path.join(root, "..", "onboarding-ontwerp", "shared", "assets", "lobsy-64.webp");
const dstLogo = path.join(root, "shared", "assets", "lobsy-64.webp");
if (fs.existsSync(srcLogo) && !fs.existsSync(dstLogo)) {
  fs.copyFileSync(srcLogo, dstLogo);
}

for (const v of ["a", "b"]) {
  for (const s of ["preview", "full", "area", "invite"]) {
    writePage(v, s);
  }
}
