import { createServer } from "node:http";
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, join, extname } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const outDir = "/opt/cursor/artifacts/paspoort-compact-mockups";

const variants = [
  { id: "a", html: "variant-a-slim-bar.html" },
  { id: "b", html: "variant-b-greeting-in-passport.html" },
  { id: "c", html: "variant-c-full-width.html" },
];

const viewports = [
  { width: 1440, height: 900, tag: "1440x900" },
  { width: 1366, height: 768, tag: "1366x768" },
  { width: 1920, height: 1080, tag: "1920x1080" },
  { width: 390, height: 844, tag: "390-mobile", mobile: true },
];

const mime = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".webp": "image/webp",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".svg": "image/svg+xml",
};

function ensurePlaywright() {
  const npx = spawnSync("npx", ["--yes", "playwright", "install", "chromium"], {
    stdio: "inherit",
  });
  if (npx.status !== 0) {
    throw new Error("playwright install failed");
  }
}

function startServer() {
  return new Promise((resolve) => {
    const server = createServer((req, res) => {
      const path = req.url?.split("?")[0] || "/";
      const rel = path === "/" ? "/index.html" : path;
      const filePath = join(root, decodeURIComponent(rel));
      try {
        const data = readFileSync(filePath);
        const ext = extname(filePath);
        res.writeHead(200, { "Content-Type": mime[ext] || "application/octet-stream" });
        res.end(data);
      } catch {
        res.writeHead(404);
        res.end("Not found");
      }
    });
    server.listen(0, "127.0.0.1", () => {
      const port = server.address().port;
      resolve({ server, baseUrl: `http://127.0.0.1:${port}` });
    });
  });
}

async function main() {
  ensurePlaywright();
  const { chromium } = await import("playwright");
  mkdirSync(outDir, { recursive: true });

  const { server, baseUrl } = await startServer();
  const browser = await chromium.launch({ headless: true });
  const rows = [];

  try {
    for (const v of variants) {
      for (const vp of viewports) {
        const context = await browser.newContext({
          viewport: { width: vp.width, height: vp.height },
          deviceScaleFactor: 1,
          locale: "nl-NL",
        });
        const page = await context.newPage();
        await page.goto(`${baseUrl}/${v.html}`, { waitUntil: "networkidle" });

        const foldY = vp.height;
        const metrics = await page.evaluate((foldYInner) => {
          const header = document.querySelector(".app-header");
          const headerH = header?.getBoundingClientRect().height ?? 0;
          const fold = foldYInner;
          const targets = [
            "passport-card",
            "overview",
            "nudge",
            "tabs",
            "tab-content",
          ];
          const result = { headerH, fold, blocks: {} };
          for (const key of targets) {
            const el = document.querySelector(`[data-fold="${key}"]`);
            if (!el) {
              result.blocks[key] = { visible: false, aboveFoldPx: 0, totalPx: 0, pct: 0 };
              continue;
            }
            const r = el.getBoundingClientRect();
            const total = r.height;
            const visibleTop = Math.max(0, Math.min(r.bottom, fold) - Math.max(r.top, 0));
            result.blocks[key] = {
              visible: r.top < fold && r.bottom > 0,
              top: Math.round(r.top),
              bottom: Math.round(r.bottom),
              aboveFoldPx: Math.round(visibleTop),
              totalPx: Math.round(total),
              pct: total > 0 ? Math.round((visibleTop / total) * 100) : 0,
            };
          }
          const lowest = Object.values(result.blocks)
            .filter((b) => b.visible)
            .reduce((m, b) => Math.max(m, b.bottom), 0);
          result.lowestVisibleBottom = Math.round(lowest);
          result.spareBelowFold = Math.round(fold - result.lowestVisibleBottom);
          return result;
        }, foldY);

        const fileName = `variant-${v.id}-${vp.tag}.png`;
        await page.screenshot({
          path: join(outDir, fileName),
          fullPage: vp.mobile ? false : false,
        });

        rows.push({
          variant: v.id.toUpperCase(),
          viewport: vp.tag,
          ...metrics.blocks,
          lowestVisibleBottom: metrics.lowestVisibleBottom,
          spareBelowFold: metrics.spareBelowFold,
          file: fileName,
        });

        await context.close();
      }
    }
  } finally {
    await browser.close();
    server.close();
  }

  const md = formatTable(rows);
  writeFileSync(join(outDir, "above-the-fold-coverage.md"), md, "utf8");
  writeFileSync(join(root, "above-the-fold-coverage.md"), md, "utf8");
  console.log(md);
}

function formatTable(rows) {
  const header =
    "| Variant | Viewport | Passport | Dit ben jij | Nudge | Tabs | Tab content top | Lowest block bottom | Spare px below fold |\n" +
    "| --- | --- | --- | --- | --- | --- | --- | --- | --- |\n";
  const lines = rows.map((r) => {
    const cell = (key) => {
      const b = r[key];
      if (!b?.visible) return "hidden";
      if (b.pct >= 98) return "full";
      if (b.pct > 0) return `${b.pct}% visible (top ${b.top}px)`;
      return "hidden";
    };
    return `| ${r.variant} | ${r.viewport} | ${cell("passport-card")} | ${cell("overview")} | ${cell("nudge")} | ${cell("tabs")} | ${cell("tab-content")} | ${r.lowestVisibleBottom}px | ${r.spareBelowFold}px |`;
  });
  return `# Paspoort compact mockups · above-the-fold coverage\n\nFake user **Alex Janssen**. Fold line = viewport height (browser chrome excluded). Header is inside the viewport.\n\n${header}${lines.join("\n")}\n`;
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
