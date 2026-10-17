import { chromium } from "playwright";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const pagesDir = path.join(root, "pages");
const outDir = root;
const artifactsDir = "/opt/cursor/artifacts/kandidatenkaart";

const states = ["preview", "full", "area", "invite", "filters-open", "area-sparse"];
const bOnlyStates = ["invite-extended", "invite-status", "invite-status-vol"];

const candidatePages = [
  { id: "reminder-notificatie", file: "kandidaat/reminder-notificatie.html", label: "Herinnering in-app" },
  { id: "reminder-email", file: "kandidaat/reminder-email.html", label: "Herinnering e-mail" },
  { id: "inactivity-email", file: "kandidaat/inactivity-email.html", label: "Activiteitscheck e-mail" },
  { id: "inactivity-notificatie", file: "kandidaat/inactivity-notificatie.html", label: "Activiteitscheck in-app" },
  { id: "weer-beschikbaar-popup", file: "kandidaat/weer-beschikbaar-popup.html", label: "Weer beschikbaar popup" },
  { id: "uitnodiging-kaart", file: "kandidaat/uitnodiging-kaart.html", label: "Uitnodiging kaart" },
  { id: "plek-vergeven", file: "kandidaat/plek-vergeven.html", label: "Plek vergeven" },
  { id: "uitnodigingen-digest-email", file: "kandidaat/uitnodigingen-digest-email.html", label: "Uitnodigingen digest" },
  { id: "instellingen-uitnodigingen", file: "kandidaat/instellingen-uitnodigingen.html", label: "Instellingen uitnodigingen" },
];

const viewports = [
  { key: "desktop", width: 1440, height: 900, deviceScaleFactor: 1, htmlClass: "vp-desktop" },
  { key: "mobile", width: 390, height: 844, deviceScaleFactor: 2, htmlClass: "vp-mobile" },
];

function pngName(prefix, vp) {
  return vp.key === "desktop"
    ? `${prefix}-desktop-1440x900.png`
    : `${prefix}-mobile-390x844@2x.png`;
}

function shotCards(prefix, labelPrefix, stateList) {
  return stateList
    .flatMap((s) =>
      viewports.map((vp) => {
        const file = pngName(`${prefix}-${s}`, vp);
        return `<figure class="shot"><figcaption>${labelPrefix} · ${s} · ${vp.key}</figcaption><img src="${file}" alt="" loading="lazy"/></figure>`;
      })
    )
    .join("\n");
}

function overviewHtml() {
  const employerA = `<section><h2>Werkgever — variant A (legacy)</h2><div class="grid">${shotCards("a", "Werkgever A", states)}</div></section>`;
  const employerBBase = `<section><h2>Werkgever — variant B (basis)</h2><div class="grid">${shotCards("b", "Werkgever B", states)}</div></section>`;
  const employerBInvite = `<section><h2>Werkgever — variant B (uitnodigingsregels)</h2><div class="grid">${shotCards("b", "Werkgever B", bOnlyStates)}</div></section>`;

  const candidateCards = candidatePages
    .flatMap((p) =>
      viewports.map((vp) => {
        const file = pngName(`k-${p.id}`, vp);
        return `<figure class="shot"><figcaption>Kandidaat · ${p.label} · ${vp.key}</figcaption><img src="${file}" alt="" loading="lazy"/></figure>`;
      })
    )
    .join("\n");

  const groups = `${employerA}
  ${employerBBase}
  ${employerBInvite}
  <section><h2>Kandidaat — uitnodiging &amp; beschikbaarheid</h2><div class="grid">${candidateCards}</div></section>`;

  return `<!DOCTYPE html>
<html lang="nl">
<head>
  <meta charset="utf-8"/>
  <title>Kandidatenkaart — overzicht mockups</title>
  <style>
    * { box-sizing: border-box; }
    body { margin: 0; padding: 2rem 1.5rem 3rem; font-family: "Segoe UI", Arial, sans-serif; background: #e8e4de; color: #122033; }
    h1 { margin: 0 0 0.5rem; font-size: 1.5rem; }
    p.lead { margin: 0 0 2rem; color: #5a6a7d; max-width: 40rem; }
    section { margin-bottom: 2.5rem; }
    section h2 { font-size: 1.1rem; margin: 0 0 1rem; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1rem; }
    .shot { margin: 0; }
    .shot figcaption { font-size: 0.75rem; font-weight: 600; margin-bottom: 0.35rem; color: #5a6a7d; }
    .shot img { width: 100%; height: auto; border-radius: 10px; border: 1px solid #ddd5cc; box-shadow: 0 8px 24px rgba(15,45,92,.12); background: #fffcfa; }
  </style>
</head>
<body>
  <h1>Kandidatenkaart — mockup overzicht</h1>
  <p class="lead">Werkgever B (focus): getrapt uitnodigen, status rondes, vacature vol. Kandidaat: uitnodigingskaart, plek vergeven, weekly digest, instellingen. Desktop 1440×900 · mobiel 390×844 @2x.</p>
  ${groups}
</body>
</html>`;
}

fs.mkdirSync(artifactsDir, { recursive: true });

async function capturePage(browser, htmlFile, prefix) {
  for (const vp of viewports) {
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height },
      deviceScaleFactor: vp.deviceScaleFactor,
    });
    const page = await context.newPage();
    await page.goto("file://" + htmlFile, { waitUntil: "load" });
    await page.evaluate((cls) => {
      document.documentElement.classList.remove("vp-desktop", "vp-mobile");
      document.documentElement.classList.add(cls);
    }, vp.htmlClass);

    const name = pngName(prefix, vp);
    const dest = path.join(outDir, name);
    await page.screenshot({ path: dest, fullPage: false });
    fs.copyFileSync(dest, path.join(artifactsDir, name));
    console.log("captured", name);
    await context.close();
  }
}

const browser = await chromium.launch();

for (const state of states) {
  await capturePage(browser, path.join(pagesDir, `a-${state}.html`), `a-${state}`);
}
for (const state of states) {
  await capturePage(browser, path.join(pagesDir, `b-${state}.html`), `b-${state}`);
}
for (const state of bOnlyStates) {
  await capturePage(browser, path.join(pagesDir, `b-${state}.html`), `b-${state}`);
}

for (const cp of candidatePages) {
  const file = path.join(pagesDir, cp.file);
  await capturePage(browser, file, `k-${cp.id}`);
}

fs.writeFileSync(path.join(root, "overview.html"), overviewHtml(), "utf8");

const overviewPng = path.join(outDir, "overview-sheet.png");
{
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();
  await page.goto("file://" + path.join(root, "overview.html"), { waitUntil: "load" });
  await page.screenshot({ path: overviewPng, fullPage: true });
  fs.copyFileSync(overviewPng, path.join(artifactsDir, "overview-sheet.png"));
  await context.close();
}

await browser.close();
console.log("Done. Artifacts:", artifactsDir);
