#!/usr/bin/env node
import { readFile } from "node:fs/promises";
import { join } from "node:path";

const liveDir = process.argv[2] ?? "/opt/cursor/artifacts/bedrijvenkaart-build-r5/live";
const manifestPath = process.argv[3] ?? join(liveDir, "capture-manifest.json");

const manifest = JSON.parse(await readFile(manifestPath, "utf8"));
const bad = manifest.filter((e) => !e.ok);
if (bad.length) {
  console.error("Manifest has failed entries:", bad);
  process.exit(1);
}

console.log(`Verified ${manifest.length} captures from manifest (all ok).`);
