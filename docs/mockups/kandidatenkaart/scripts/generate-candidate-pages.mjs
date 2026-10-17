import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const pagesDir = path.join(root, "pages", "kandidaat");
fs.mkdirSync(pagesDir, { recursive: true });

const logoRel = "../../shared/assets/lobsy-64.webp";
const dashRel = "../../shared/candidate-home-bg.svg";
const cssRel = "../../shared/kandidaat.css";

function head(title) {
  return `<!DOCTYPE html>
<html lang="nl" class="vp-desktop">
<head>
  <meta charset="utf-8"/>
  <title>${title}</title>
  <link rel="stylesheet" href="${cssRel}"/>
</head>`;
}

function candidateShell({ activeNotif = false, behindOverlay = false } = {}) {
  const shellClass = behindOverlay ? " k-shell--behind-overlay" : "";
  return `
<body>
<div class="k-shell${shellClass}">
  <header class="k-header">
    <a class="k-brand" href="#"><img src="${logoRel}" width="44" height="44" alt=""/> Lobsy</a>
    <div class="k-header__actions">
      <button type="button" class="k-icon-btn${activeNotif ? " k-icon-btn--active" : ""}" aria-label="Meldingen">
        <svg class="i" viewBox="0 0 24 24"><path d="M15 17h5l-1.4-1.4A2 2 0 0 1 18 14.2V11a6 6 0 1 0-12 0v3.2c0 .5-.2 1-.6 1.4L4 17h5"/><path d="M10 20a2 2 0 0 0 4 0"/></svg>
        ${activeNotif ? '<span class="k-badge-dot" aria-hidden="true"></span>' : ""}
      </button>
    </div>
  </header>
  <div class="k-dashboard" aria-hidden="true">
    <img class="k-dashboard__art" src="${dashRel}" width="390" height="700" alt=""/>
  </div>
  <nav class="k-bottom-nav" aria-label="Hoofdmenu">
    <a class="is-on" href="#">Home</a>
    <a href="#">Sollicitaties</a>
    <a href="#">Profiel</a>
    <a href="#">Meer</a>
  </nav>`;
}

function shellEnd() {
  return `</div></body></html>`;
}

const reminderCopy =
  "Je hebt een uitnodiging om te solliciteren. Reageer je niet binnen 2 dagen, dan zetten we je tijdelijk op <strong>niet beschikbaar</strong>. Je staat dan niet meer op de kandidatenkaart voor werkgevers.";

const declineFine =
  "Afwijzen telt als reactie. Daar hoort geen waarschuwing of andere consequentie bij.";

const notifPage = `${head("Kandidaat — herinnering uitnodiging (in-app)")}
${candidateShell({ activeNotif: true, behindOverlay: true })}
  <div class="k-notif-scrim" aria-hidden="true"></div>
  <aside class="k-notif-panel" role="dialog" aria-labelledby="notif-title">
    <div class="k-notif-panel__head">
      <h2 id="notif-title">Meldingen</h2>
      <button type="button" class="k-btn k-btn--ghost" aria-label="Sluiten">Sluiten</button>
    </div>
    <ul class="k-notif-list">
      <li class="k-notif-item k-notif-item--highlight">
        <div class="k-notif-item__tag">
          <svg class="i" viewBox="0 0 24 24"><path d="M22 11.1V12a10 10 0 1 1-5.9-9.1"/><path d="M22 4 12 14.1l-3-3"/></svg>
          Herinnering
        </div>
        <p>${reminderCopy}</p>
        <div class="k-notif-item__actions">
          <button type="button" class="k-btn k-btn--primary">Bekijk uitnodiging</button>
          <button type="button" class="k-btn">Afwijzen</button>
        </div>
        <p class="k-notif-item__fine">${declineFine}</p>
      </li>
      <li class="k-notif-item">
        <div class="k-notif-item__tag">Eerder</div>
        <p style="font-size:var(--text-sm);margin:0;color:var(--muted)">Nieuwe uitnodiging: Productiemedewerker bij Demo Bakkerij Noord.</p>
      </li>
    </ul>
  </aside>
${shellEnd()}`;

fs.writeFileSync(path.join(pagesDir, "reminder-notificatie.html"), notifPage, "utf8");

const emailPage = `${head("Kandidaat — herinnering uitnodiging (e-mail)")}
<body>
<div class="k-email-wrap">
  <article class="k-email">
    <div class="k-email__meta">
      <div><strong>Van:</strong> Lobsy &lt;hallo@lobsy.nl&gt;</div>
      <div><strong>Aan:</strong> jij@example.com</div>
      <div><strong>Onderwerp:</strong> Even checken: je uitnodiging wacht nog</div>
    </div>
    <div class="k-email__body">
      <div class="k-email__logo">
        <img src="${logoRel}" width="40" height="40" alt=""/>
        <span>Lobsy</span>
      </div>
      <h1>Je uitnodiging wacht nog op je</h1>
      <p>Hoi,</p>
      <p>Je hebt een uitnodiging om te solliciteren via Lobsy. Even vriendelijk ter herinnering: reageer je niet binnen <strong>2 dagen</strong>, dan zetten we je tijdelijk op niet beschikbaar. Je telt dan niet meer mee op de kandidatenkaart, totdat je weer aangeeft open te staan voor werk.</p>
      <p>Dat is bedoeld om de kaart actueel te houden, niet om je te corrigeren.</p>
      <div class="k-email__cta">
        <a class="k-btn k-btn--primary" href="#" style="text-decoration:none">Uitnodiging bekijken</a>
      </div>
      <p style="font-size:var(--text-xs);color:var(--muted)">${declineFine}</p>
      <div class="k-email__footer">
        <p>Je ontvangt deze mail omdat je vindbaar bent voor werkgevers op Lobsy.</p>
      </div>
    </div>
  </article>
</div>
</body></html>`;

fs.writeFileSync(path.join(pagesDir, "reminder-email.html"), emailPage, "utf8");

const popupPage = `${head("Kandidaat — weer beschikbaar popup")}
${candidateShell({ behindOverlay: true })}
  <div class="k-popup-scrim" role="presentation">
    <div class="k-popup" role="dialog" aria-labelledby="popup-title" aria-describedby="popup-desc">
      <div class="k-popup__icon" aria-hidden="true">
        <svg class="i" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>
      </div>
      <h2 id="popup-title">Weer beschikbaar voor werk?</h2>
      <p id="popup-desc">Je stond op <strong>niet beschikbaar</strong> omdat er geen reactie kwam op een uitnodiging. Geen probleem. Zodra je weer vindbaar bent, kun je opnieuw meedoen op de kandidatenkaart, als jij dat wilt.</p>
      <div class="k-popup__actions">
        <button type="button" class="k-btn k-btn--primary">Ja, ik ben weer vindbaar</button>
        <button type="button" class="k-btn">Later</button>
      </div>
      <p class="k-popup__note">Je kunt dit altijd wijzigen in Instellingen, onder Beschikbaarheid.</p>
    </div>
  </div>
${shellEnd()}`;

fs.writeFileSync(path.join(pagesDir, "weer-beschikbaar-popup.html"), popupPage, "utf8");

console.log("Wrote kandidaat pages in", path.relative(root, pagesDir));
