import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const baseUrl = process.env.BASE_URL ?? 'http://127.0.0.1:8888';
const outDir = process.env.OUT_DIR ?? '/opt/cursor/artifacts/g78-droombaan-fix';

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(`${baseUrl}/droombaan-preview.html`, { waitUntil: 'networkidle' });
await page.locator('[data-testid=ll-g78-job-picks]').waitFor();

for (const [name, width, height] of [
  ['droombaan-1440', 1440, 900],
  ['droombaan-390', 390, 844],
]) {
  await page.setViewportSize({ width, height });
  await page.waitForTimeout(400);
  await page.screenshot({
    path: path.join(outDir, `${name}.png`),
    fullPage: true,
  });
}

await browser.close();
console.log('Saved screenshots to', outDir);
