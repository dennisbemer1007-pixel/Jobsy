/**
 * Source contract for FIX 2: Banenkaart pins 304 + boot-map reuse.
 * Run: node Jobsy.Tests/js/jobMap-pins-304-reuse.test.mjs
 */
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import assert from "node:assert/strict";

const root = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const js = readFileSync(join(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"), "utf8");
const min = readFileSync(join(root, "Jobsy.Web", "wwwroot", "js", "jobMap.min.js"), "utf8");
const maps = readFileSync(join(root, "Jobsy.Web", "wwwroot", "js", "maps-loader.js"), "utf8");

assert.match(js, /let pinsCachedPayload\s*=\s*null/);
assert.match(js, /if\s*\(\s*res\.status\s*===\s*304\s*\)/);
assert.match(js, /return pinsCachedPayload/);
assert.match(js, /pinsCachedPayload\s*=\s*body/);

const disposeMatch = js.match(/function dispose\(\)\s*\{[\s\S]*?\n    \}/);
assert.ok(disposeMatch, "dispose() not found");
assert.match(disposeMatch[0], /pinsEtag\s*=\s*null/);
assert.match(disposeMatch[0], /pinsCachedPayload\s*=\s*null/);

assert.match(js, /function adoptMapContainer\s*\(/);
assert.match(js, /map\._container\s*=\s*host/);
assert.match(js, /mapCreateCount\s*\+=\s*1/);
assert.match(js, /__testGetMapCreateCount/);

const initMatch = js.match(
  /function init\(elementId, vacancies, options\)\s*\{[\s\S]*?\n    function locateIconHtml/
);
assert.ok(initMatch, "init() not found");
assert.match(initMatch[0], /adoptMapContainer\(el\)/);
assert.match(initMatch[0], /let live\s*=/);

assert.match(min, /304/);
assert.match(min, /__testGetMapCreateCount/);
assert.match(maps, /jobMap\.min\.js\?v=20260927-mapreuse/);

console.log("jobMap-pins-304-reuse: ok");
