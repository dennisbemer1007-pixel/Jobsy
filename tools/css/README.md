# CSS minifier

`build.sh` regenerates `Jobsy.Web/wwwroot/css/app.min.css` from `app.css` using a pinned `lightningcss-cli` (see `package.json` / lockfile).

```bash
./tools/css/build.sh
```

CI (`.github/workflows/code-quality.yml`) fails if the checked-in `app.min.css` differs from the build output. After editing `app.css`, run the script and bump `css/app.min.css?v=` in `App.razor`, then refresh `Jobsy.Tests/asset-versions.json` with `python3 Jobsy.Tests/update-asset-versions.py`.
