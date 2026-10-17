import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const pagesDir = path.join(root, "pages");
fs.mkdirSync(pagesDir, { recursive: true });

const logoRel = "../shared/assets/lobsy-64.webp";
const mapSvg = fs.readFileSync(path.join(root, "shared", "map-amersfoort.svg"), "utf8");

function mapBlock({ locked, selected, belowMin = false, sparseSoest = false }) {
  let stageClass = "";
  if (sparseSoest) stageClass = " map-has-sparse-soest";
  else if (selected) stageClass = " map-has-selection";
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
            <div class="map-legend">
              <div class="map-legend__row"><span class="map-legend__swatch"></span> ≥ 30 kandidaten</div>
            </div>
          </div>
          <div class="map-stage${stageClass}">${mapSvg}</div>
          ${lock}
        </div>`;
}

function filterVacancy(disabled) {
  const dis = disabled ? "disabled" : "";
  return `<div class="kk-field">
    <label for="vac">Vacature of profiel</label>
    <div class="kk-control kk-control--wide" id="vac" ${dis}>
      <svg class="i" viewBox="0 0 24 24"><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
      <span><strong>Productiemedewerker</strong> · Amersfoort</span>
    </div>
  </div>`;
}

function filterMatch(disabled) {
  const dis = disabled ? ' aria-disabled="true"' : "";
  return `<div class="kk-field">
    <label>Min. matchscore</label>
    <div class="kk-seg" role="group" aria-label="Minimale matchscore"${dis}>
      <span>60%</span><span class="is-on">70%</span><span>80%</span>
    </div>
  </div>`;
}

function filterDistance(disabled) {
  const dis = disabled ? ' aria-disabled="true"' : "";
  return `<div class="kk-field">
    <label>Reisafstand</label>
    <div class="kk-seg" role="group" aria-label="Reisafstand"${dis}>
      <span>15 km</span><span class="is-on">25 km</span><span>40 km</span><span>60 km</span>
    </div>
  </div>`;
}

function filterStartDate(disabled) {
  const dis = disabled ? ' aria-disabled="true"' : "";
  return `<div class="kk-field">
    <label>Startdatum</label>
    <div class="kk-seg kk-seg--wrap" role="group" aria-label="Startdatum"${dis}>
      <span class="is-on">Per direct</span><span>Binnen 1 maand</span><span>Later</span>
    </div>
  </div>`;
}

function filterHours(disabled) {
  const dis = disabled ? ' aria-disabled="true"' : "";
  return `<div class="kk-field">
    <label>Uren per week</label>
    <div class="kk-seg kk-seg--wrap" role="group" aria-label="Uren per week"${dis}>
      <span>&lt;16</span><span>16–24</span><span class="is-on">24–32</span><span>32+</span>
    </div>
  </div>`;
}

function filtersDesktopRow(disabled, popoverOpen) {
  return `<div class="kk-filters kk-filters--desktop">
        ${filterVacancy(disabled)}
        ${filterMatch(disabled)}
        ${filterDistance(disabled)}
        <div class="kk-field kk-more-filters">
          <label>Meer</label>
          <button type="button" class="kk-btn kk-btn--more${popoverOpen ? " is-active" : ""}">Meer filters</button>
          <div class="kk-filter-popover${popoverOpen ? " is-open" : ""}" role="dialog" aria-label="Extra filters">
            ${filterStartDate(disabled)}
            ${filterHours(disabled)}
          </div>
        </div>
      </div>`;
}

function filtersMobileSheetContent(disabled) {
  return `<div class="kk-filter-sheet__body">
    ${filterVacancy(disabled)}
    ${filterMatch(disabled)}
    ${filterDistance(disabled)}
    ${filterStartDate(disabled)}
    ${filterHours(disabled)}
    <p class="kk-filter-sheet__hint">Gebieden op de kaart tonen pas vanaf 30 kandidaten na filtering.</p>
    <button type="button" class="kk-btn kk-btn--primary kk-filter-sheet__apply">Toepassen</button>
  </div>`;
}

function mobileFiltersOverlay(disabled) {
  return `
      <div class="kk-filter-scrim" aria-hidden="true"></div>
      <div class="kk-filter-sheet" role="dialog" aria-labelledby="filter-sheet-title">
        <div class="kk-filter-sheet__head">
          <h2 id="filter-sheet-title">Filters</h2>
          <button type="button" class="kk-btn kk-btn--ghost">Sluiten</button>
        </div>
        ${filtersMobileSheetContent(disabled)}
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

function activeCandidatesChip() {
  return `<p class="kk-active-chip">
      <svg class="i" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3l8 4v5c0 5-3.5 9-8 10-4.5-1-8-5-8-10V7l8-4z"/></svg>
      <span>Alleen actieve kandidaten op de kaart</span>
    </p>`;
}

function activityPill() {
  return `<span class="kk-activity-pill">Actief deze week</span>`;
}

function chromeBlock(badge, badgeClass, filtersDisabled, filtersOpen = false, preview = false, inviteStatus = false) {
  const filterFab = preview
    ? `<button type="button" class="kk-filter-fab kk-filter-fab--disabled" disabled aria-disabled="true">Filters</button>`
    : `<button type="button" class="kk-filter-fab">Filters</button>`;
  return `
      <div class="kk-page${filtersOpen ? " kk-page--filters-open" : ""}${preview ? " kk-page--preview" : ""}${inviteStatus ? " kk-page--invite-status" : ""}">
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
      ${filtersDesktopRow(filtersDisabled, filtersOpen)}
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
          ${filterFab}
          <span class="kk-mobile-summary">70% · 25 km · Per direct · 24–32 u</span>
        </div>
      </div>`;
}

function areaSparseRegionPanel() {
  return `<div class="kk-panel">
            <h2>Regio Soest en Baarn</h2>
            <div class="kk-below-min">
              <p>Minder dan 30 kandidaten in dit gebied met deze filters.</p>
              <p class="kk-note">We tonen alleen gebieden met minimaal 30 matches na filtering. Verlaag de matchdrempel, kies ruimere uren of vergroot de reisafstand om dit gebied terug te zien.</p>
            </div>
          </div>`;
}

function areaPanelBelowMin() {
  return `
          ${activeCandidatesBlock()}
          ${areaSparseRegionPanel()}`;
}

function areaPanel(selected = false) {
  if (!selected) {
    return `
          ${activeCandidatesBlock()}
          <div class="kk-panel">
            <h2>Gebiedsinzicht</h2>
            <p class="kk-note">Tik een rood gebied voor tellingen en matchverdeling. Alleen kandidaten die vindbaar zijn voor werkgevers tellen mee.</p>
            <p class="kk-note kk-note--tip"><strong>Tip.</strong> Verhoog de matchdrempel om gerichter uit te nodigen. Uitnodigingen blijven anoniem tot iemand reageert.</p>
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

function mobileSheetContent(state, variant, locked) {
  if (locked) {
    return `<div class="kk-mobile-sheet__body">${activeCandidatesBlock()}<div class="kk-upsell"><b>Preview</b> Volledige filters en gebieden zodra marktinzicht actief is.</div></div>`;
  }
  if (state === "area-sparse") {
    return `<div class="kk-mobile-sheet__body kk-mobile-sheet__body--sparse">${areaSparseRegionPanel()}</div>`;
  }
  if (state === "invite-status" || state === "invite-status-vol") {
    return `<div class="kk-mobile-sheet__body kk-mobile-sheet__body--invite-status">${inviteStatusPanel(state === "invite-status-vol")}</div>`;
  }
  if (state === "area" || state === "invite" || state === "invite-extended") {
    return `<div class="kk-mobile-sheet__body">${areaPanel(true)}</div>`;
  }
  if (variant === "b") {
    return `<div class="kk-mobile-sheet__body kk-mobile-sheet__body--list"><ul class="kk-area-list kk-area-list--compact">${areaList()}</ul></div>`;
  }
  return `<div class="kk-mobile-sheet__peek">Veeg omhoog voor gebieden</div>`;
}

function previewSampleRows() {
  return `<ul class="kk-area-list kk-area-list--sample" aria-hidden="true">
    <li class="kk-area-list__item"><span class="kk-sample-row__title">Amersfoort en Leusden</span><span class="kk-sample-row__meta">42 kandidaten</span></li>
    <li class="kk-area-list__item"><span class="kk-sample-row__title">Soest en Baarn</span><span class="kk-sample-row__meta">38 kandidaten</span></li>
    <li class="kk-area-list__item"><span class="kk-sample-row__title">Utrecht west</span><span class="kk-sample-row__meta">56 kandidaten</span></li>
  </ul>`;
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

const areaSelectedStates = new Set([
  "area",
  "invite",
  "invite-extended",
  "invite-status",
  "invite-status-vol",
]);

function resolveState(state) {
  return {
    locked: state === "preview",
    selected: areaSelectedStates.has(state),
    sparseSoest: state === "area-sparse",
    belowMin: state === "area-sparse",
    filtersOpen: state === "filters-open",
    inviteStatus: state === "invite-status" || state === "invite-status-vol",
    inviteVol: state === "invite-status-vol",
  };
}

function mobileSheetMarkup(state, variant, locked) {
  if (locked) return "";
  let extraClass = "";
  if (state === "area-sparse") extraClass = " kk-mobile-sheet--sparse";
  if (state === "invite-status" || state === "invite-status-vol") extraClass = " kk-mobile-sheet--invite-status";
  return `<div class="kk-mobile-sheet kk-mobile-sheet--${variant}${extraClass}">${mobileSheetContent(state, variant, locked)}</div>`;
}

function layoutA(state) {
  const { locked, selected, belowMin, sparseSoest, filtersOpen } = resolveState(state);
  let aside;
  if (locked) {
    aside = `${activeCandidatesBlock()}<div class="kk-panel kk-upsell"><b>Preview</b> Filters en gebieden worden vrijgegeven zodra marktinzicht op jullie vestiging actief is.</div>`;
  } else if (belowMin) {
    aside = areaPanelBelowMin();
  } else if (areaSelectedStates.has(state) && state !== "invite-status" && state !== "invite-status-vol") {
    aside = areaPanel(true);
  } else if (state === "invite-status" || state === "invite-status-vol") {
    aside = `${activeCandidatesBlock()}${inviteStatusPanel(state === "invite-status-vol")}`;
  } else {
    aside = areaPanel(false);
  }

  const filterOverlay = filtersOpen ? mobileFiltersOverlay(locked) : "";
  return `
      <div class="kk-layout-a">
        <div class="kk-layout-a__map">${mapBlock({ locked, selected, belowMin, sparseSoest })}</div>
        <aside class="kk-layout-a__aside">${aside}</aside>
      </div>
      ${mobileSheetMarkup(state, "a", locked)}
      ${filterOverlay}`;
}

function layoutB(state) {
  const { locked, selected, belowMin, sparseSoest, filtersOpen, inviteStatus } = resolveState(state);
  let listPanel;
  if (locked) {
    listPanel = `${activeCandidatesBlock()}${previewSampleRows()}`;
  } else if (belowMin) {
    listPanel = areaPanelBelowMin();
  } else if (inviteStatus) {
    listPanel = `${activeCandidatesChip()}${inviteStatusPanel(state === "invite-status-vol")}`;
  } else {
    listPanel = `${activeCandidatesBlock()}<ul class="kk-area-list">${areaList("amersfoort")}</ul>
       <button type="button" class="kk-btn kk-btn--primary" style="width:100%">Uitnodiging voor geselecteerd gebied</button>`;
  }
  const filterOverlay = filtersOpen ? mobileFiltersOverlay(locked) : "";
  const listClass = inviteStatus ? " kk-layout-b__list--invite-status" : "";
  return `
      <div class="kk-layout-b">
        <div class="kk-layout-b__list${listClass}">${listPanel}</div>
        <div class="kk-layout-b__map">${mapBlock({ locked, selected, belowMin, sparseSoest })}</div>
      </div>
      ${mobileSheetMarkup(state, "b", locked)}
      ${filterOverlay}`;
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

function inviteOverlayExtended() {
  return `
      <div class="kk-scrim" role="dialog" aria-modal="true" aria-labelledby="invite-ext-title">
        <div class="kk-modal kk-modal--invite-extended">
          <h2 id="invite-ext-title">Uitnodiging om te solliciteren</h2>
          <div class="kk-modal__anon">
            <svg class="i" viewBox="0 0 24 24"><path d="M12 3l8 4v5c0 5-3.5 9-8 10-4.5-1-8-5-8-10V7l8-4z"/></svg>
            <p>Anoniem naar kandidaten in <strong>Regio Amersfoort en Leusden</strong>. Zij zien jouw vacature en kiezen zelf of ze reageren.</p>
          </div>
          <p class="kk-modal__vac">Vacature: <strong>Productiemedewerker · Amersfoort</strong></p>
          <div class="kk-invite-slots">
            <span class="kk-invite-slots__label">Aantal plekken</span>
            <strong class="kk-invite-slots__value">2</strong>
          </div>
          <div class="kk-invite-graduated">
            <p>We nodigen in kleine rondes uit, eerst de beste matches. Komen er te weinig reacties, dan volgt automatisch de volgende groep.</p>
            <p class="kk-invite-round"><strong>Ronde 1:</strong> 12 kandidaten met 80%+ match</p>
          </div>
          <ul class="kk-invite-rules">
            <li>Eén uitnodigingsronde per gebied per vacature.</li>
            <li>Uitnodigingen alleen voor kandidaten met minimaal 70% match.</li>
          </ul>
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

function inviteStatusPanel(vacancyFull) {
  const volBadge = vacancyFull
    ? `<span class="kk-invite-status__badge">Vacature vol</span>`
    : "";
  const round2 = vacancyFull
    ? `<li class="kk-round kk-round--muted"><span class="kk-round__title">Ronde 2</span><span class="kk-round__meta">Geannuleerd — alle plekken ingevuld</span></li>`
    : `<li class="kk-round kk-round--pending"><span class="kk-round__title">Ronde 2</span><span class="kk-round__meta">Start automatisch bij te weinig reacties (70–79% match)</span></li>`;
  const footnote = vacancyFull
    ? `<p class="kk-note kk-note--status">Open uitnodigingen zijn automatisch ingetrokken. Er gaan geen nieuwe rondes meer uit voor deze vacature in dit gebied.</p>`
    : `<p class="kk-note kk-note--status">Je ziet reacties zodra kandidaten reageren. Ronde 2 volgt alleen als ronde 1 onvoldoende oplevert.</p>`;
  const plekkenLine = vacancyFull
    ? `<span class="kk-round__meta">5 reacties · <strong>2 van 2 plekken</strong> ingevuld</span>`
    : `<span class="kk-round__meta">5 reacties · 1 van 2 plekken ingevuld</span>`;

  return `<div class="kk-panel kk-invite-status">
            <div class="kk-invite-status__head">
              <h2>Uitnodigingen · Amersfoort en Leusden</h2>
              ${volBadge}
            </div>
            <p class="kk-invite-status__vac">Productiemedewerker · Amersfoort · <strong>2 plekken</strong></p>
            <ol class="kk-rounds" aria-label="Uitnodigingsrondes">
              <li class="kk-round kk-round--done">
                <span class="kk-round__title">Ronde 1</span>
                <span class="kk-round__meta">Verstuurd · 12 uitnodigingen</span>
                ${plekkenLine}
              </li>
              ${round2}
            </ol>
            ${footnote}
          </div>`;
}

function writePage(variant, state) {
  const isA = variant === "a";
  const { locked, filtersOpen, inviteStatus } = resolveState(state);
  const titles = {
    preview: `Kandidatenkaart ${variant.toUpperCase()} — preview`,
    full: `Kandidatenkaart ${variant.toUpperCase()} — volledig`,
    area: `Kandidatenkaart ${variant.toUpperCase()} — gebied`,
    invite: `Kandidatenkaart ${variant.toUpperCase()} — uitnodiging`,
    "invite-extended": `Kandidatenkaart B — uitnodiging (getrapt)`,
    "invite-status": `Kandidatenkaart B — status uitnodigingen`,
    "invite-status-vol": `Kandidatenkaart B — vacature vol`,
    "filters-open": `Kandidatenkaart ${variant.toUpperCase()} — filters open`,
    "area-sparse": `Kandidatenkaart ${variant.toUpperCase()} — onder drempel`,
  };
  const badge = state === "preview" ? "Preview · add-on" : "Marktinzicht actief";
  const badgeClass = state === "preview" ? "kk-badge--free" : "kk-badge--gold";
  let body = chromeBlock(badge, badgeClass, locked, filtersOpen, locked, inviteStatus);
  body += isA ? layoutA(state) : layoutB(state);
  body += `</div>`;
  if (state === "invite-extended") body += inviteOverlayExtended();
  else if (state === "invite") body += inviteOverlay();

  const html = shellStart(titles[state] || titles.full) + body + shellEnd;
  fs.writeFileSync(path.join(pagesDir, `${variant}-${state}.html`), html, "utf8");
  console.log("wrote", `${variant}-${state}.html`);
}

fs.mkdirSync(path.join(root, "shared", "assets"), { recursive: true });
const srcLogo = path.join(root, "..", "onboarding-ontwerp", "shared", "assets", "lobsy-64.webp");
const dstLogo = path.join(root, "shared", "assets", "lobsy-64.webp");
if (fs.existsSync(srcLogo) && !fs.existsSync(dstLogo)) {
  fs.copyFileSync(srcLogo, dstLogo);
}

const employerStates = ["preview", "full", "area", "invite", "filters-open", "area-sparse"];
const bOnlyStates = ["invite-extended", "invite-status", "invite-status-vol"];

for (const s of employerStates) {
  writePage("a", s);
}
for (const s of employerStates) {
  writePage("b", s);
}
for (const s of bOnlyStates) {
  writePage("b", s);
}
