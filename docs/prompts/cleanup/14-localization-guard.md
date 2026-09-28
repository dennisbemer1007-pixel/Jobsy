Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 14: Localization guard: parity report, user-visible "Jobsy" strings, candidate-unused keys

**Goal:** make translation gaps visible, and fix the brand name where users see it. No bulk deletion of keys.

**Evidence (code-review.md §3.6, §4.1):**
- `Jobsy.Web/Localization/UiStrings.cs` (3,394 lines) has 2,109 keys × 5 languages (nl, en, pl, ro, ar). All languages have all keys, but **pl/ro/ar each have 683 values identical to nl** (untranslated copies).
- 238 keys have no literal reference (`docs/review/data/uistrings-keys-without-literal-reference.txt`). Some are used dynamically (e.g. `$"status.{x}"`), so **verify before removing anything**.
- 23 UiStrings lines show the old brand "Jobsy" to users (`rg -n "Jobsy" Jobsy.Web/Localization/UiStrings.cs`, excluding code identifiers). Keep "Jobsy" as the code namespace, DB, headers, storage keys and repo name (see the ADR in prompt 15).

**Do:**
1. Add a test `LocalizationParityReportTests` that:
   - asserts every key exists in all languages (hard fail);
   - **reports** (Output/Console, no fail) the count of values identical to nl per language, with a committed baseline file `docs/i18n/untranslated-baseline.txt`, and fails only if the count **increases**. Exempt legit identical values (brand names, "OK", numbers) via an allow-list.
2. Replace user-visible "Jobsy" with "Lobsy" in UiStrings values (all languages). Leave keys and code identifiers unchanged. Check the email templates too (`rg -n "Jobsy" Jobsy.Infrastructure -g '*.html' -g '*Email*'`) and list them in the PR; change them only if they are user-visible text.
3. Write `docs/i18n/candidate-unused-keys.md`: the 238 keys from the data file, each marked `dynamic-used` / `unused` / `unknown` after checking for interpolated key usage. **Do not delete keys in this PR.**
4. Optional: a small `tools/i18n-export` that writes a CSV for translators (key, nl, en, pl, ro, ar) for the identical-to-nl values.

**Do not touch:** the UiStrings structure/format; translations beyond the brand-name fix; pages with pending PRs (only UiStrings values change, which is safe).

**Verify:**
- Build and tests green.
- The new test passes with the baseline.
- Manual: switch to en/pl and see "Lobsy" in the header/footer/help texts that previously said "Jobsy".
- `rg -n "Jobsy" UiStrings.cs` → only keys/identifiers remain.

**Dependency:** after 01. Watch UiStrings merge conflicts with pending PRs that add keys (rebase; conflicts are line-local).
