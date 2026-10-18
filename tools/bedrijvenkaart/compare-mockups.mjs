#!/usr/bin/env node
import { mkdir, readdir } from "node:fs/promises";
import { join } from "node:path";
import { execSync } from "node:child_process";

const mockDir = process.argv[2] ?? "/opt/cursor/artifacts/bedrijvenkaart-build-r1/reference";
const liveDir = process.argv[3] ?? "/opt/cursor/artifacts/bedrijvenkaart-build-r8/live";
const outDir = process.argv[4] ?? "/opt/cursor/artifacts/bedrijvenkaart-build-r8/compare";

await mkdir(outDir, { recursive: true });
const names = (await readdir(mockDir)).filter((n) => n.endsWith(".png")).sort();

for (const name of names) {
  const mock = join(mockDir, name);
  const live = join(liveDir, name);
  const out = join(outDir, name);
  try {
    execSync(
      `convert "${mock}" "${live}" +append -background white -gravity center "${out}"`,
      { stdio: "pipe" },
    );
    console.log("compare", name);
  } catch (e) {
    console.warn("skip", name, e.message?.split("\n")[0]);
  }
}

console.log("Compare dir:", outDir);
