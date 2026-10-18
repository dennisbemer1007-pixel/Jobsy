import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.LOBSY_BASE_URL || 'http://127.0.0.1:5080';
const outDir = process.env.OUT_DIR || '/opt/cursor/artifacts/candidate-illustrated-pages';

const pages = [
  { name: 'login', path: '/login', auth: false },
  { name: 'start', path: '/', auth: false },
  { name: 'kompas', path: '/candidate/paspoort', auth: true },
  { name: 'banenkaart', path: '/banenkaart', auth: true },
];

const viewports = [
  { tag: '1440', width: 1440, height: 900 },
  { tag: '390', width: 390, height: 844 },
];

async function disableSticky(page) {
  await page.addStyleTag({
    content: `
      .pub-header, .jobsy-chrome__header, header.panel-header, .bottom-nav, .candidate-bottom-nav,
      .saved-gate__header, .pub-coach-dock, .lobsy-coach-dock { position: static !important; }
      .passport-layout__card--desktop { position: static !important; }
    `,
  });
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch({ headless: true });

  for (const vp of viewports) {
    const context = await browser.newContext({
      viewport: { width: vp.width, height: vp.height },
      locale: 'nl-NL',
    });
    const page = await context.newPage();

    for (const spec of pages) {
      if (spec.auth) {
        // Local dev: rely on test cookie / seeded session when available.
        await page.goto(`${baseUrl}/login`, { waitUntil: 'networkidle', timeout: 120_000 });
      }
      await page.goto(`${baseUrl}${spec.path}`, { waitUntil: 'networkidle', timeout: 120_000 });
      await page.waitForTimeout(1500);
      await disableSticky(page);
      const file = path.join(outDir, `${spec.name}-${vp.tag}.png`);
      await page.screenshot({ path: file, fullPage: true });
      console.log('saved', file);
    }

    await context.close();
  }

  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
