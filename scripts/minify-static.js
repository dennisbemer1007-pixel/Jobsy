#!/usr/bin/env node
"use strict";

const { spawnSync } = require("child_process");
const path = require("path");

const root = path.resolve(__dirname, "..");
const web = path.join(root, "Jobsy.Web", "wwwroot");

function run(cmd, args) {
    const result = spawnSync(cmd, args, { stdio: "inherit", cwd: root });
    if (result.status !== 0) {
        process.exit(result.status || 1);
    }
}

const npx = process.platform === "win32" ? "npx.cmd" : "npx";

run(npx, ["--yes", "terser@5", path.join(web, "js", "app-core.js"), "-c", "-m", "-o", path.join(web, "js", "app-core.min.js")]);
run(npx, ["--yes", "terser@5", path.join(web, "js", "jobMap.js"), "-c", "-m", "-o", path.join(web, "js", "jobMap.min.js")]);
run(npx, ["--yes", "terser@5", path.join(web, "js", "jobsyMapLibre.js"), "-c", "-m", "-o", path.join(web, "js", "jobsyMapLibre.min.js")]);
run(npx, ["--yes", "terser@5", path.join(web, "js", "vacancyDetailMap.js"), "-c", "-m", "-o", path.join(web, "js", "vacancyDetailMap.min.js")]);
run(npx, ["--yes", "clean-css-cli@5", "-o", path.join(web, "css", "app.min.css"), path.join(web, "css", "app.css")]);

console.log("Minified app-core, jobMap, jobsyMapLibre, vacancyDetailMap, and app.css");
