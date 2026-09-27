/**
 * Fails if jobMap.js still uses MapLibre callback-style cluster APIs.
 * MapLibre GL JS 5.x is promise-only for getClusterLeaves.
 * Cluster tap must open the pager (openClusterList) and must NOT easeTo/zoom.
 *
 * Run: node Jobsy.Tests/js/jobMap-cluster-promise-api.test.mjs
 */
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import assert from "node:assert/strict";

const root = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const js = readFileSync(join(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"), "utf8");

assert.doesNotMatch(
  js,
  /getClusterExpansionZoom\s*\(\s*[^)]*,\s*function/,
  "callback-style getClusterExpansionZoom is broken on MapLibre 5"
);
assert.doesNotMatch(
  js,
  /getClusterLeaves\s*\(\s*[^)]*,\s*[^)]*,\s*[^)]*,\s*function/,
  "callback-style getClusterLeaves is broken on MapLibre 5"
);
assert.match(js, /await\s+source\.getClusterLeaves/);
assert.match(js, /async function onClusterClick/);

const onClusterMatch = js.match(
  /async function onClusterClick\s*\([^)]*\)\s*\{[\s\S]*?\n    \}/
);
assert.ok(onClusterMatch, "onClusterClick function body not found");
const onClusterBody = onClusterMatch[0];
assert.match(onClusterBody, /openClusterList\s*\(/);
assert.doesNotMatch(onClusterBody, /\.easeTo\s*\(/);
assert.doesNotMatch(onClusterBody, /getClusterExpansionZoom/);

console.log("jobMap-cluster-promise-api: ok");
