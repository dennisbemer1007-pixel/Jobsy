const ICON={
bell:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"/><path d="M10.3 21a1.94 1.94 0 0 0 3.4 0"/></svg>',
user:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="8" r="4"/><path d="M4 21c1.5-4 4.5-6 8-6s6.5 2 8 6"/></svg>',
reis:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5z"/></svg>',
zoek:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>',
soll:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4h6v3H9z"/><path d="m9 14 2 2 4-4"/></svg>',
car:'<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>'};
function header(){return `<header class="app-header"><a class="brand" href="#"><img src="../../lobsy-merkteken.svg" alt="">Lobsy</a>
<nav class="nav"><a href="#">Reis</a><a href="#">Paspoort</a><a href="#" class="is-active">Zoeken</a><a href="#">Sollicitaties</a><a href="#">Carrière</a></nav>
<div class="utils"><button class="util">i</button><button class="util">${ICON.bell}</button><span class="lang"><img src="nl.svg" alt="">NL<span class="car">▾</span></span><span class="user"><span>Roland van der Velde</span><button class="util">${ICON.user}</button></span></div></header>`}
function hero(m){return `<div class="kstrip" style="--dh:225px;--mh:105px" aria-hidden="true"><img class="desk" src="../../strips/bedrijvenkaart-desk.jpg" alt=""><img class="mob" src="../../strips/bedrijvenkaart-mob.jpg" alt=""></div>`}
function footer(){return `<footer class="footer"><nav><a href="#">Privacyverklaring</a><a href="#">Algemene voorwaarden</a><a href="#">Gebruiksvoorwaarden</a><a href="#">Wie zijn wij</a><a href="#">Toegankelijkheid</a><a href="#">Bronnen</a></nav><span>© 2026 Lobsy</span></footer>
<nav class="bottom-nav"><a href="#">${ICON.reis}Reis</a><a href="#">${ICON.user}Paspoort</a><a href="#" class="is-active">${ICON.zoek}Zoeken</a><a href="#">${ICON.soll}Sollicitaties</a><a href="#">${ICON.car}Carrière</a></nav>`}
