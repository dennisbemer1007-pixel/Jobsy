import { chromium } from 'playwright';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import sharp from 'sharp';

const base = (process.env.BASE || 'http://127.0.0.1:5201').replace(/\/$/, '');
const out = process.env.OUT || '/cursor/stores/self/perf2/screenshots';
const label = process.env.LABEL || 'after';

const shots = [
  { name: 'landing', path: '/', desktop: [1440, 900], mobile: [390, 844] },
  { name: 'login', path: '/login', desktop: [1440, 900], mobile: [390, 844] },
  { name: 'banenkaart', path: '/banenkaart', desktop: [1440, 900], mobile: [390, 844] },
];

async function saveJpeg(page, file) {
  const png = await page.screenshot({ fullPage: false, type: 'png' });
  await mkdir(path.dirname(file), { recursive: true });
  await sharp(png).jpeg({ quality: 88 }).toFile(file);
  console.log('wrote', file);
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  for (const s of shots) {
    for (const [w, h, tag] of [
      [...s.desktop, 'desktop'],
      [...s.mobile, 'mobile'],
    ]) {
      const ctx = await browser.newContext({ viewport: { width: w, height: h } });
      const page = await ctx.newPage();
      await page.goto(base + s.path, { waitUntil: 'domcontentloaded', timeout: 120_000 });
      await page.waitForTimeout(s.path.includes('banenkaart') ? 2500 : 800);
      await saveJpeg(page, path.join(out, `${label}-${s.name}-${tag}.jpg`));
      await ctx.close();
    }
  }
  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
