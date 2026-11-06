const { chromium } = require('playwright-core');
const fs = require('fs');
(async () => {
  const b = await chromium.launch({ executablePath: '/opt/google/chrome/chrome' });
  const files = fs.readdirSync('src').filter((f) => f.startsWith('compare-zee-') && f.endsWith('.html'));
  const p = await b.newPage({ viewport: { width: 2880, height: 900 }, deviceScaleFactor: 1 });
  for (const f of files) {
    await p.goto('file://' + process.cwd() + '/src/' + f);
    await p.waitForLoadState('networkidle');
    await p.evaluate(() => document.fonts.ready);
    const out = 'renders/desktop/' + f.replace('.html', '.png');
    await p.screenshot({ path: out, fullPage: true });
    console.log('saved', out);
  }
  await b.close();
})();
