import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const pagesDir = path.join(root, "pages");
fs.mkdirSync(pagesDir, { recursive: true });

const logoRel = "../shared/assets/lobsy-64.webp";
const mapSvg = fs.readFileSync(path.join(root, "shared", "map-amersfoort.svg"), "utf8");

function mapBlock({ locked, selected }) {
  const stageClass = selected ? " map-has-selection" : "";
  const lock = locked
    ? `<div class="kk-map-lock">
        <div class="kk-map-lock__card">
          <div class="kk-map-lock__icon" aria-hidden="true"><svg class="i" viewBox="0 0 24 24" style="stroke:#fff"><rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/></svg></div>
          <h2>Marktinzicht met kandidatenkaart</h2>
          <p>Zie waar vindbare kandidaten passen bij jouw vacature. Rustig overzicht, zonder individuele gegevens.</p>
          ${activeCandidatesBlock(true)}
          <button type="button" class="kk-btn kk-btn--primary">Meer informatie</button>
          <p class="kk-map-lock__fine">Prijs en pakket op aanvraag</p>
        </div>
      </div>`
    : "";
  return `
        <div class="map-pane${locked ? " map-pane--locked" : ""}">
          <div class="map-pane__top-chrome">
            <button type="button" class="kk-filter-fab kk-filter-fab--on-map">Filters</button>
            <div class="map-legend">
              <div class="map-legend__row"><span class="map-legend__swatch"></span> Vindbaar (≥ 30 per gebied)</div>
            </div>
          </div>
          <div class="map-stage${stageClass}">${mapSvg}</div>
          ${lock}
        </div>`;
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

function activeCandidatesBlock(compact = false) {
  if (compact) {
    return `<div class="kk-active-sell kk-active-sell--compact">
      <strong>Alleen actieve kandidaten</strong>
      <span>Herinnering na 2 dagen, maandelijkse activiteitscheck, daarna van de kaart tot iemand zich weer meldt.</span>
    </div>`;
  }
  return `<div class="kk-active-sell">
    <h3>Alleen actieve kandidaten</h3>
    <p>Wie een uitnodiging niet binnen 2 dagen beantwoordt, krijgt een herinnering en verdwijnt daarna van de kaart tot die zich weer beschikbaar meldt. Wie een maand niet actief is, bevestigt eerst dat die nog zoekt. Zo zie je alleen mensen die echt zoeken.</p>
  </div>`;
}

function activityPill() {
  return `<span class="kk-activity-pill">Actief deze week</span>`;
}

function chromeBlock(badge, badgeClass, filtersDisabled) {
  const dis = filtersDisabled ? "disabled" : "";
  return `
      <div class="kk-page">
      <div class="kk-page__chrome kk-page__chrome--desktop">
      <nav class="kk-crumbs" aria-label="Kruimelpad">
        <span>Markt &amp; talent</span><span>/</span><span>Kandidatenkaart</span>
      </nav>
      <header class="kk-header">
        <div>
          <h1>Kandidatenkaart</h1>
          <p class="kk-header__lead">Omgekeerde banenkaart: waar vindbare kandidaten passen bij jouw vacature. Alleen geaggregeerde gebieden, nooit losse personen.</p>
        </div>
        <span class="kk-badge ${badgeClass}">${badge}</span>
      </header>
      <div class="kk-privacy kk-privacy--desktop">
        <svg class="i" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3l8 4v5c0 5-3.5 9-8 10-4.5-1-8-5-8-10V7l8-4z"/></svg>
        <p><strong>Privacy.</strong> Gebieden vanaf 30 opt-in kandidaten. Geen namen of exacte adressen, wel tellingen en matchverdeling.</p>
      </div>
      <div class="kk-filters kk-filters--desktop">
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
      </div>
      </div>
      <div class="kk-mobile-top">
        <div class="kk-mobile-top__row">
          <h1 class="kk-mobile-title">Kandidatenkaart</h1>
          <span class="kk-badge ${badgeClass}">${badge}</span>
        </div>
        <button type="button" class="kk-info-chip">
          <svg class="i" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="M12 10v5M12 7h.01"/></svg>
          Geaggregeerd · min. 30 · alleen actieve kandidaten
        </button>
        <div class="kk-mobile-bar">
          <button type="button" class="kk-filter-fab">Filters</button>
          <span class="kk-mobile-summary">Productiemedewerker · 70% · 25 km</span>
        </div>
      </div>`;
}

function areaPanel(selected = false) {
  if (!selected) {
    return `
          ${activeCandidatesBlock()}
          <div class="kk-panel">
            <h2>Gebiedsinzicht</h2>
            <p class="kk-note">Tik een rood gebied voor tellingen en matchverdeling. Alleen kandidaten die vindbaar zijn voor werkgevers tellen mee.</p>
            <div class="kk-upsell" style="margin-top:1rem">
              <b>Tip</b>
              Verhoog de matchdrempel om gerichter uit te nodigen. Uitnodigingen blijven anoniem tot iemand reageert.
            </div>
          </div>`;
  }
  return `
          ${activeCandidatesBlock()}
          <div class="kk-panel">
            <div class="kk-panel__head">
              <h2>Regio Amersfoort en Leusden</h2>
              ${activityPill()}
            </div>
            <p class="kk-stat-line">42 kandidaten <span>· 12 boven 80% match</span></p>
            <p class="kk-freshness">Laatst bijgewerkt: actief deze week</p>
            <div class="kk-bars" aria-label="Matchverdeling">
              <div class="kk-bar"><span>80%+</span><div class="kk-bar__track"><i class="kk-bar__fill kk-bar__fill--high" style="width:29%"></i></div><span>12</span></div>
              <div class="kk-bar"><span>70–79%</span><div class="kk-bar__track"><i class="kk-bar__fill" style="width:48%"></i></div><span>20</span></div>
              <div class="kk-bar"><span>60–69%</span><div class="kk-bar__track"><i class="kk-bar__fill" style="width:24%"></i></div><span>10</span></div>
            </div>
            <p class="kk-note">Geaggregeerd gebied (min. 30 kandidaten). Geen individuele profielen.</p>
            <button type="button" class="kk-btn kk-btn--primary" style="width:100%;margin-top:0.75rem">Uitnodiging om te solliciteren</button>
          </div>`;
}

function mobileSheetContent(state, variant, locked, selected) {
  if (locked) {
    return `<div class="kk-mobile-sheet__body">${activeCandidatesBlock()}<div class="kk-upsell"><b>Preview</b> Volledige filters en gebieden zodra marktinzicht actief is.</div></div>`;
  }
  if (selected) {
    return `<div class="kk-mobile-sheet__body">${areaPanel(true)}</div>`;
  }
  if (variant === "b") {
    return `<div class="kk-mobile-sheet__body kk-mobile-sheet__body--list"><ul class="kk-area-list kk-area-list--compact">${areaList()}</ul></div>`;
  }
  return `<div class="kk-mobile-sheet__peek">Veeg omhoog voor gebieden · tik op de kaart voor detail</div>`;
}

function areaList(selectedId = "amersfoort") {
  const items = [
    { id: "amersfoort", title: "Amersfoort en Leusden", meta: "42 kandidaten · 12 boven 80%" },
    { id: "soest", title: "Soest en Baarn", meta: "38 kandidaten · 9 boven 80%" },
    { id: "utrecht", title: "Utrecht west", meta: "56 kandidaten · 18 boven 80%" },
    { id: "hilversum", title: "Hilversum", meta: "31 kandidaten · 6 boven 80%" },
  ];
  return items
    .map(
      (it) => `<li class="kk-area-list__item${it.id === selectedId ? " is-selected" : ""}">
        <div class="kk-area-list__row">
          <div class="kk-area-list__title">${it.title}</div>
          ${activityPill()}
        </div>
        <div class="kk-area-list__meta">${it.meta}</div>
      </li>`
    )
    .join("\n");
}

function layoutA(state) {
  const locked = state === "preview";
  const selected = state === "area" || state === "invite";
  const aside = locked
    ? `${activeCandidatesBlock()}<div class="kk-panel kk-upsell"><b>Preview</b> Filters en gebieden worden vrijgegeven zodra marktinzicht op jullie vestiging actief is.</div>`
    : areaPanel(selected);
  return `
      <div class="kk-layout-a">
        <div class="kk-layout-a__map">${mapBlock({ locked, selected })}</div>
        <aside class="kk-layout-a__aside">${aside}</aside>
      </div>
      <div class="kk-mobile-sheet kk-mobile-sheet--a">${mobileSheetContent(state, "a", locked, selected)}</div>`;
}

function layoutB(state) {
  const locked = state === "preview";
  const selected = state === "area" || state === "invite";
  const listPanel = locked
    ? `${activeCandidatesBlock()}<div class="kk-panel"><p class="kk-note">Gebiedslijst in de volledige versie. Nu kaartpreview met voorbeeldgebieden.</p></div>`
    : `${activeCandidatesBlock()}<ul class="kk-area-list">${areaList()}</ul>
       <button type="button" class="kk-btn kk-btn--primary" style="width:100%">Uitnodiging voor geselecteerd gebied</button>`;
  return `
      <div class="kk-layout-b">
        <div class="kk-layout-b__list">${listPanel}</div>
        <div class="kk-layout-b__map">${mapBlock({ locked, selected })}</div>
      </div>
      <div class="kk-mobile-sheet kk-mobile-sheet--b">${mobileSheetContent(state, "b", locked, selected)}</div>`;
}

function inviteOverlay() {
  return `
      <div class="kk-scrim" role="dialog" aria-modal="true" aria-labelledby="invite-title">
        <div class="kk-modal">
          <h2 id="invite-title">Uitnodiging om te solliciteren</h2>
          <div class="kk-modal__anon">
            <svg class="i" viewBox="0 0 24 24"><path d="M12 3l8 4v5c0 5-3.5 9-8 10-4.5-1-8-5-8-10V7l8-4z"/></svg>
            <p>Anoniem naar kandidaten in <strong>Regio Amersfoort en Leusden</strong>. Zij zien jouw vacature en kiezen zelf of ze reageren.</p>
          </div>
          <p>Vacature: <strong>Productiemedewerker · Amersfoort</strong>. Geschat 42 ontvangers binnen de filters.</p>
          <label class="kk-field"><span>Korte toelichting (optioneel)</span>
            <textarea placeholder="Bijv. ochtendteam, 24–32 uur per week."></textarea>
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
  const badge = state === "preview" ? "Preview · add-on" : "Marktinzicht actief";
  const badgeClass = state === "preview" ? "kk-badge--free" : "kk-badge--gold";
  let body = chromeBlock(badge, badgeClass, state === "preview");
  body += isA ? layoutA(state) : layoutB(state);
  body += `</div>`;
  if (state === "invite") body += inviteOverlay();

  const html = shellStart(titles[state]) + body + shellEnd;
  fs.writeFileSync(path.join(pagesDir, `${variant}-${state}.html`), html, "utf8");
  console.log("wrote", `${variant}-${state}.html`);
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
