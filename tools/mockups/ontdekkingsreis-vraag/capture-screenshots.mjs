import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const variants = ['variant-a', 'variant-b'];
const viewports = [
  { suffix: 'desktop-1280x800', width: 1280, height: 800 },
  { suffix: 'mobile-390x844', width: 390, height: 844 },
];

const browser = await chromium.launch();

for (const variant of variants) {
  const htmlPath = path.join(__dirname, `${variant}.html`);
  const url = `file://${htmlPath}`;

  for (const vp of viewports) {
    const page = await browser.newPage({ viewport: { width: vp.width, height: vp.height } });
    await page.goto(url, { waitUntil: 'load' });
    await page.waitForTimeout(200);

    const scroll = await page.evaluate(() => ({
      doc: document.documentElement.scrollHeight,
      win: window.innerHeight,
      body: document.body.scrollHeight,
    }));
    if (scroll.doc > scroll.win + 1 || scroll.body > scroll.win + 1) {
      console.warn(`[warn] ${variant} ${vp.suffix}: vertical scroll detected`, scroll);
    }

    const out = path.join(__dirname, `${variant}-${vp.suffix}.png`);
    await page.screenshot({ path: out, fullPage: false });
    console.log('wrote', out);
    await page.close();
  }
}

await browser.close();
