const { chromium } = require(process.env.PWC || 'playwright-core');
const fs = require('fs'), path = require('path');
const dir = __dirname, only = process.argv[2];
(async () => {
  const b = await chromium.launch({ executablePath: '/usr/bin/google-chrome', args: ['--no-sandbox'] });
  const files = fs.readdirSync(path.join(dir, 'html')).filter(f => f.endsWith('.html') && (!only || f.includes(only)));
  for (const f of files) {
    const m = f.includes('-m');
    const isM = /^au-m/.test(f);
    const ctx = await b.newContext({ viewport: isM ? { width: 390, height: 844 } : { width: 1440, height: 900 }, deviceScaleFactor: isM ? 2 : 1 });
    const p = await ctx.newPage();
    await p.goto('file://' + path.join(dir, 'html', f));
    await p.evaluate(() => document.fonts.ready);
    await p.screenshot({ path: path.join(dir, f.replace('.html', '.png')), fullPage: true });
    await ctx.close();
  }
  await b.close();
  console.log('rendered', files.length);
})();
