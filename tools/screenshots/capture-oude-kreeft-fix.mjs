import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const root = path.resolve(import.meta.dirname, '../..');
const outDir = '/opt/cursor/artifacts/oude-kreeft';
fs.mkdirSync(outDir, { recursive: true });

const appCss = fs.readFileSync(path.join(root, 'Jobsy.Web/wwwroot/css/app.css'), 'utf8');
const passportCss = fs.readFileSync(path.join(root, 'Jobsy.Web/wwwroot/css/features/mijn-paspoort.css'), 'utf8');
const kompasCss = fs.readFileSync(path.join(root, 'Jobsy.Web/wwwroot/css/features/kompas-illustrated.css'), 'utf8');

function html() {
  return `<!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><style>
:root { --app-header-height: 56px; --bg: #f5f2ee; --surface: #fffcfa; --border: #ddd5cc; --brand: #0f2d5c; --z-sticky: 20; --space-1: 0.25rem; --space-2: 0.5rem; --space-3: 0.75rem; --bottom-nav-h: 64px; --gold: #c9a227; --gold-ink: #5c4a0f; --radius-pill: 999px; }
body { margin: 0; background: var(--bg); font-family: system-ui, sans-serif; }
${appCss}
${passportCss}
${kompasCss}
</style></head><body>
<header style="position:fixed;top:0;left:0;right:0;height:var(--app-header-height);background:#0f2d5c;z-index:50"></header>
<div class="app-shell has-bottom-nav" style="padding-top:var(--app-header-height)">
<main class="app-main" style="min-height:140vh;overflow:auto">
<div class="panel-page passport-page passport-page--illustrated">
<div class="passport-layout"><div class="passport-layout__main">
<section class="passport-overview" style="background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:1rem;margin:1rem 0">
<div class="passport-overview__ring-block">
<svg class="passport-dna-ring" viewBox="0 0 120 120" width="96" height="96"><circle cx="60" cy="60" r="34" fill="var(--surface)" stroke="#e4c65a" stroke-width="1.5" stroke-dasharray="3 3"/><image href="/images/brand/lobsy-128.webp" x="36" y="36" width="48" height="48"/></svg>
<div><h2 class="passport-overview__title">Dit ben jij</h2><p class="passport-overview__stage">Jouw kreeft · nog in het ei</p></div>
</div>
<button type="button" style="display:block;width:100%;margin-top:0.75rem;padding:0.75rem;background:var(--brand);color:#fff;border:0;border-radius:8px">Naar de tests</button>
</section>
<div class="passport-tabs-shell">
<div class="passport-tabs" role="tablist">
<button type="button" class="passport-tab passport-tab--active">Wie ik ben</button>
<button type="button" class="passport-tab">Mijn tests <span class="passport-tabs__pill">4</span></button>
<button type="button" class="passport-tab">Past bij?</button>
<button type="button" class="passport-tab">Carrière</button>
<button type="button" class="passport-tab">Bewijzen</button>
</div></div>
<article style="background:var(--surface);border:1px solid var(--border);border-radius:12px;padding:1rem;margin:1rem 0;min-height:360px">Tab panel</article>
</div></div></div></main>
<nav class="bottom-nav" style="position:fixed;bottom:0;left:0;right:0;height:var(--bottom-nav-h);background:#fff;border-top:1px solid var(--border)"></nav>
</div></body></html>`;
}

const browser = await chromium.launch({ headless: true });
for (const [name, w, h] of [
  ['paspoort-tabs-390-after', 390, 844],
  ['paspoort-tabs-1440-after', 1440, 900],
  ['paspoort-dna-ring-after', 390, 844],
]) {
  const page = await browser.newPage({ viewport: { width: w, height: h } });
  await page.setContent(html(), { waitUntil: 'load' });
  if (name.includes('1440')) {
    await page.evaluate(() => window.scrollTo(0, 280));
  }
  await page.screenshot({ path: path.join(outDir, `${name}.png`), fullPage: false });
  await page.close();
}
await browser.close();
console.log('saved to', outDir);
