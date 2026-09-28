Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 09: Remove unused wwwroot assets and resolve the stale JS split sources

**Goal:** remove unused static files, and make the JS source-of-truth unambiguous. No behaviour change in the browser.

**Evidence (code-review.md §1.5, §1.6, §2.2):**
- **Unused assets:** there are 0 references in `.razor/.cs/.js/.css/.html/.json/manifest` outside the tests for these files:
  - `wwwroot/images/flags/{gb,nl,pl,ro,sa}.svg`
  - `images/mascot.png` (567 KB), `mascot-180.*`, `mascot-256.png`
  - `lobsy-180.webp`, `lobsy-256.png`
  - README files inside wwwroot
  - `wwwroot/image-cache-sw.js`: **verify** whether it is registered (`rg -n "image-cache-sw" .`, including `blazor-boot.js`, manifest and service-worker registration). Delete it only if it is truly unreferenced. If it was ever registered in production, keep a tiny self-unregistering stub instead of deleting it (clients keep old service workers).
- **Stale JS split sources:** `wwwroot/js/app-core.js` and `app-extras.js` are hand concatenations of:
  - `geo.js`, `culture.js`, `cookieConsent.js` (**drifted**), `maps-loader.js`
  - `extras-loader.js` (**drifted**), `sessionIdle.js`, `download.js`, `richtext.js`

  The app only serves the bundles (check `App.razor`/`blazor-boot.js`), but tests read the split files via `File.ReadAllText`. So tests can pass on code that production does not run.
- 68 test asserts hard-code `?v=` values (`rg -n "\?v=" Jobsy.Tests | wc -l`).

**Do:**
1. Delete the unused images and READMEs (per file, after `rg -n "<basename-without-ext>" .` returns only this doc).
2. JS: choose **one** option and write the choice in the PR:
   - **(A, preferred, simplest):** delete the 8 split files and repoint the tests that read them to the bundle file (`app-core.js`/`app-extras.js`), keeping the assert content. For drifted files, check first that the bundle contains the intended (newest) behaviour; if the split file had the newer code, **stop and report**, do not silently port it.
   - **(B):** keep the split files as the source of truth, add `tools/build-js-bundles` (plain concat in a fixed order) and a CI check that `concat == bundle`.
3. Replace the hard-coded `?v` test asserts with the asset-versions guard introduced by the pending asset-versions PR (assert "a ?v exists and matches the file hash/guard" instead of literal values).

**Do not touch:** `jobMap.js`, `jobsyMapLibre.js`, `vacancyDetailMap.js` and their `.min` files; `App.razor` ?v values; CSS files.

**Verify:**
- Build and tests green.
- Playwright smoke: `/` banenkaart (pins, popup), cookie banner accept/decline, language switch (culture.js), geolocation prompt (geo), file download (`download.js`), rich-text editor in CreateVacancy (`richtext.js`), session-idle warning (shorten the timeout via config in the test).
- Browser devtools show no 404s for deleted files on these pages.
- The size of `wwwroot` shrinks (report in the PR).

**Dependency:** **wait until these PRs are merged:** jobMap pins-once/deferred MapLibre/popup skeleton; map popup height + app-core.js/app.min.css ?v bumps; blazor-boot/lobsyPush/vacancyDetailMap ?v bumps + asset-versions guard; bottom-nav feedback navFeedback.js; CookieConsentBanner/map-height cookie PR (touches cookieConsent).
