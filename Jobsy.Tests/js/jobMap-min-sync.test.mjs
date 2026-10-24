/**
 * Ensures wwwroot/js/jobMap.min.js is the terser output of jobMap.js (acc never serves stale min).
 * Run: node Jobsy.Tests/js/jobMap-min-sync.test.mjs
 */
import { execSync } from "node:child_process";
import { createHash } from "node:crypto";
import { readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import assert from "node:assert/strict";

const root = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const srcPath = join(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js");
const minPath = join(root, "Jobsy.Web", "wwwroot", "js", "jobMap.min.js");
const tmpPath = join(tmpdir(), `jobMap.min.${process.pid}.js`);

execSync(
  `npx --yes terser@5 "${srcPath}" --compress --mangle -o "${tmpPath}"`,
  { cwd: root, stdio: "pipe", env: process.env },
);

const committed = readFileSync(minPath);
const generated = readFileSync(tmpPath);
try {
  assert.equal(
    committed.compare(generated),
    0,
    "jobMap.min.js is stale — regenerate with: npx terser@5 Jobsy.Web/wwwroot/js/jobMap.js --compress --mangle -o Jobsy.Web/wwwroot/js/jobMap.min.js",
  );
} finally {
  try {
    unlinkSync(tmpPath);
  } catch {
    /* ignore */
  }
}

const js = readFileSync(srcPath, "utf8");
const min = readFileSync(minPath, "utf8");
assert.match(js, /function lagenPinSymbolsRenderable/);
assert.match(js, /if \(lagenLayout\) \{\s*return "";\s*\}/);
assert.match(js, /pushPinData\(\)/);
assert.match(js, /icon-ignore-placement": true/);
assert.match(js, /lagenUnclusteredIconLayout[\s\S]*?"icon-ignore-placement": true/);
const hash = createHash("sha256").update(committed).digest("hex");
const manifest = JSON.parse(
  readFileSync(join(root, "Jobsy.Tests", "asset-versions.json"), "utf8"),
);
const entry = manifest["js/jobMap.min.js"];
assert.ok(entry?.sha256, "asset-versions.json must list js/jobMap.min.js sha256");
assert.equal(
  entry.sha256,
  hash,
  "asset-versions.json sha256 for js/jobMap.min.js is stale",
);

console.log("jobMap-min-sync: ok sha256=" + hash.slice(0, 12) + "…");
