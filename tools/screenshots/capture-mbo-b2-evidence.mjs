/**
 * MBO B2 mockup evidence (welkom, vraag, overgang, paspoort) @ 390 + 1440.
 * Requires CI stack :5201 + ConnectionStrings for PupilCodeService seed (run via dotnet test helper).
 *
 * LOBSY_E2E_BASE_URL=http://127.0.0.1:5201 node tools/screenshots/capture-mbo-b2-evidence.mjs
 */
import { chromium } from 'playwright';
import { mkdirSync } from 'fs';
import path from 'path';

const baseUrl = (process.env.JOBSY_E2E_BASE_URL || 'http://127.0.0.1:5201').replace(/\/$/, '');
const outDir = process.env.JOBSY_MBO_EVIDENCE_DIR || '/opt/cursor/artifacts/mbo-b2';
mkdirSync(outDir, { recursive: true });

const schoolId = process.env.JOBSY_MBO_SCHOOL_ID;
const classId = process.env.JOBSY_MBO_CLASS_ID;
const plainCode = process.env.JOBSY_MBO_PLAIN_CODE;

if (!schoolId || !classId || !plainCode) {
  console.error('Set JOBSY_MBO_SCHOOL_ID, JOBSY_MBO_CLASS_ID, JOBSY_MBO_PLAIN_CODE (from SeedFreshMboPupil).');
  process.exit(1);
}

async function pupilLogin(page) {
  const loginUrl = `${baseUrl}/leerling?schoolId=${schoolId}&classId=${classId}`;
  await page.goto(loginUrl, { waitUntil: 'domcontentloaded' });
  await page.evaluate(
    ([id, code]) =>
      new Promise((resolve, reject) => {
        const deadline = performance.now() + 30000;
        const tick = () => {
          const select = document.querySelector('select[name=classId]');
          const input = document.querySelector('input[name=code]');
          const option = select && [...select.options].find((o) => o.value === id);
          if (select && input && option) {
            option.selected = true;
            select.value = id;
            input.value = code;
            const form = input.closest('form');
            if (!form) {
              reject(new Error('no form'));
              return;
            }
            if (typeof form.requestSubmit === 'function') form.requestSubmit();
            else form.submit();
            resolve(true);
            return;
          }
          if (performance.now() > deadline) reject(new Error('login timeout'));
          else setTimeout(tick, 50);
        };
        tick();
      }),
    [classId, plainCode]
  );
  await page.waitForURL(
    (url) => !url.includes('/leerling?') || url.includes('error='),
    { timeout: 90000 }
  );
  if (page.url().includes('error=')) throw new Error('pupil login error');
}

async function shot(page, name, w, h) {
  await page.setViewportSize({ width: w, height: h });
  await page.waitForTimeout(400);
  const file = path.join(outDir, `${name}-${w}.png`);
  await page.screenshot({ path: file, fullPage: false });
  console.log('wrote', file);
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ locale: 'nl-NL' });
  const page = await context.newPage();
  page.setDefaultTimeout(120000);
  await pupilLogin(page);

  const screens = [
    ['welkom', '/leerling/mbo-hbo/uitleg'],
    ['vraag', '/leerling/mbo-hbo/test/werkwaarden'],
    ['overgang', '/leerling/mbo-hbo/uitkomst/vakken'],
    ['paspoort', '/leerling/mbo-hbo/paspoort'],
  ];

  for (const [name, route] of screens) {
    await page.goto(`${baseUrl}${route}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(600);
    for (const w of [390, 1440]) {
      await shot(page, name, w, w === 390 ? 844 : 900);
    }
  }

  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});