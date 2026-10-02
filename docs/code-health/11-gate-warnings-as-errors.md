# 11: Gate: TreatWarningsAsErrors, auto-fix job, green CI

- **Branch:** `cursor/code-health-11-gate`, from `cursor/code-health-10-frontend`.
- **PR:** into that branch.
- This step turns Dennis' decision of 2 Oct into enforcement. It must land with a Release build of **0 warnings** and CI **green**.

## §1 Build settings (`Directory.Build.props`)

```xml
<!-- Code health 11 (Dennis, 2 Oct 2026): warnings fail the build. Fix them in the same PR.
     The CI auto-fix job applies safe dotnet-format fixes on PR branches.
     NuGet audit warnings stay warnings: a new CVE must not break every build overnight;
     the "Vulnerable package gate" in code-quality.yml fails on High/Critical. -->
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<WarningsNotAsErrors>$(WarningsNotAsErrors);NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
<!-- Pinned so SDK/analyzer updates don't add warnings silently; raise it on purpose in its own PR. -->
<AnalysisLevel>10.0-recommended</AnalysisLevel>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

- Replace the old comment "Do not set TreatWarningsAsErrors" with the comment above.
- Remove the now-redundant `RZ10012` `WarningsAsErrors` line from `Jobsy.Web.csproj` (added in 01).
- Check that `tools/*` projects inherit the settings, and fix their warnings too.
- Docker builds (`dotnet publish -c Release`) now fail on warnings too. That is intended. Verify both Dockerfiles build.

## §2 Remove the baseline mechanism

- Delete `.github/quality/warning-baseline.txt`.
- In `code-quality.yml`, the build step simply fails on errors. Keep running `count-build-warnings.sh` for the per-code summary in the log, and add an explicit `test "$count" -eq 0`.
- Update `CONTRIBUTING.md` and [00-README](00-README.md): the standing rule now reads "0 warnings, enforced".

## §3 CI auto-fix job (`.github/workflows/code-health-autofix.yml`)

**Requirements (Dennis):** it runs on PR branches only, pushes one fix commit to the same PR and the build reruns. It never runs on `main` or `acceptatie` and never loops.

```yaml
name: Code health auto-fix
on:
  pull_request:
    types: [opened, synchronize, reopened]
concurrency:
  group: autofix-${{ github.event.pull_request.number }}
  cancel-in-progress: true
jobs:
  autofix:
    # Same-repo PRs only (no forks), never for main/acceptatie heads.
    if: >-
      github.event.pull_request.head.repo.full_name == github.repository &&
      github.head_ref != 'main' && github.head_ref != 'acceptatie'
    runs-on: ubuntu-latest
    timeout-minutes: 20
    permissions:
      contents: write
    steps:
      - name: Bot token present?
        id: token
        env:
          BOT_TOKEN: ${{ secrets.CODE_HEALTH_BOT_TOKEN }}
        run: |
          if [[ -z "$BOT_TOKEN" ]]; then
            echo "::notice::CODE_HEALTH_BOT_TOKEN not configured; auto-fix skipped (Dennis creates this secret)."
            echo "skip=true" >> "$GITHUB_OUTPUT"
          fi
      - uses: actions/checkout@v4
        if: steps.token.outputs.skip != 'true'
        with:
          ref: ${{ github.head_ref }}
          fetch-depth: 2
          token: ${{ secrets.CODE_HEALTH_BOT_TOKEN }}
      - name: Loop guard
        id: guard
        if: steps.token.outputs.skip != 'true'
        run: |
          msg="$(git log -1 --pretty=%B)"; author="$(git log -1 --pretty=%ae)"
          if [[ "$msg" == *"[code-health-autofix]"* || "$author" == "code-health-bot@users.noreply.github.com" ]]; then
            echo "skip=true" >> "$GITHUB_OUTPUT"; echo "Last commit is the bot's: skipping."
          else
            echo "skip=false" >> "$GITHUB_OUTPUT"
          fi
      - uses: actions/setup-dotnet@v4
        if: steps.token.outputs.skip != 'true' && steps.guard.outputs.skip == 'false'
        with: { dotnet-version: "10.0.x" }
      - name: dotnet format (safe fixers only)
        if: steps.token.outputs.skip != 'true' && steps.guard.outputs.skip == 'false'
        run: |
          dotnet restore Jobsy.sln
          dotnet format whitespace Jobsy.sln
          dotnet format style Jobsy.sln --severity warn --diagnostics IDE0005 IDE0055 IDE0161 IDE0040 IDE0044
          dotnet format analyzers Jobsy.sln --severity warn --diagnostics \
            CA1827 CA1828 CA1834 CA1847 CA1854 CA1860 CA1865 CA2249 CA2263
      - name: Commit and push fixes
        if: steps.token.outputs.skip != 'true' && steps.guard.outputs.skip == 'false'
        run: |
          git diff --quiet && { echo "No fixes."; exit 0; }
          git -c user.name="code-health-bot" -c user.email="code-health-bot@users.noreply.github.com" \
            commit -am "chore: dotnet format auto-fix [code-health-autofix]"
          git push origin HEAD:${{ github.head_ref }}   # never --force
```

Rules for the job:
- **The allow-list is the safety boundary.** Only fixers that don't change behaviour may be in it. `CA1826` (exception type on empty lists), `CA1512` (exception message) and `CA1822` (API shape) are deliberately **not** in it. Changing the list needs a PR with a reason. Never use `dotnet format` without `--diagnostics` for analyzers.
- **Token (Decision 2):** the job uses the secret **`CODE_HEALTH_BOT_TOKEN`**, a GitHub App token or fine-grained PAT with contents: write on this repo only. Pushes made with the default `GITHUB_TOKEN` would not retrigger the checks.
  - **Dennis creates the secret.** Agents never create, read or print it.
  - **Until the secret exists, the job skips gracefully:** the first step emits `::notice::CODE_HEALTH_BOT_TOKEN not configured; auto-fix skipped`, every later step is skipped, and the job ends **green**. It never fails or blocks the PR. There is no fallback to `GITHUB_TOKEN`.
- **Loop guard:** skip when the last commit has the `[code-health-autofix]` marker or the bot author. The job only ever adds **one** commit per push.
- **Never on main or acceptatie:** the `if:` above, plus `pull_request` events only (no `push` trigger). Branch protection on `main`/`acceptatie` stays as it is.
- Warnings that format cannot fix still fail the normal build. The author fixes them, following the standing rule.
- Pushing this file needs `workflow` scope (global rule 7).

**Test the job** on a throwaway same-repo PR (`cursor/code-health-11-autofix-probe`).
- **Without the secret** (the expected state when this PR is built): the job is green and shows the "not configured" notice. Screenshot or link it in the PR.
- **With the secret**, once Dennis has created it (otherwise list this as a follow-up): put one fixable warning in the probe PR (e.g. `"x".StartsWith("/")` → CA1865) and check:
  1. the bot commit appears
  2. the checks rerun (with the bot token) and go green
  3. a second push without warnings produces no bot commit
  4. the bot's own commit does not trigger another bot commit

Close the probe PR afterwards **without merging**.

## §4 Make CI green: fix the 49 pre-existing failing tests

- Every failure listed in Appendix A must be fixed in this step, unless an earlier step already fixed it. Mark those as "fixed in NN".
- "Fix" means: correct the code when the test describes intended behaviour, **or** update the test when the product intentionally changed. In that case, cite the commit/stack that changed it in the commit message.
- **Role/MFA/rights tests: the code follows the agreed role rules, and the tests are updated accordingly** (Decision 7, README):
  - admins MFA required, no Google sign-in for admins
  - Microsoft/Google (external) users no extra Lobsy 2FA
  - Ambassadeur paused behind its feature flag
  - BranchManager as in the agreed werkgever rights (`WerkgeverRightsMatrix`, `docs/security/roles-matrix.md`, ADR 0004/0005)

  Where the code matches these rules, update the test, and cite the rule in the commit message. Where the code breaks them, fix the code.
- **Only if a test cannot be mapped to these rules or any other documented behaviour** (privacy/anonymisation included), do not guess. Keep it failing, list it in the PR under "Questions for Dennis", and open the PR as **draft**.
- If the list gets long, the test fixes may be split into sub-PRs `11a` (authorization/privacy), `11b` (copy/links/flags) and `11c` (the rest), stacked before the gate PR. The gate PR itself (§1–§3) lands last, once CI is green.

## Acceptance

- The Release build has 0 warnings, with `TreatWarningsAsErrors` on. The NU190x audit warnings stay warnings.
- `code-quality`, `pr-tests` (unit + Playwright smoke) and `acceptatie-smoke` are all green on the PR.
- The auto-fix job has been demonstrated on the probe PR (screenshots or links in the PR).
- `warning-baseline.txt` is deleted, and the docs (CONTRIBUTING, 00-README, the Directory.Build.props comment) are updated.

---

## Appendix A: pre-existing failing tests (49, on `acceptatie` @ `3a15b0d7`, 2 Oct 2026)

- Measured with the CI unit filter from `pr-tests.yml` minus Playwright: 5118 tests, 5069 passed, **49 failed**.
- They were already failing before the merges of 2 Oct; the old baseline had 57.
- Flaky on reruns (passed when rerun, so not listed): `AccountUnsubscribe*` and `UnsubscribeFlow*` timing tests.

### A. Email copy and snapshots (fixed in 01 §2, or copy fixes)

| Test | Symptom (2 Oct, acceptatie 3a15b0d7) | Direction |
|---|---|---|
| `AccountEmailCopyTests.AccountLockout_duration_matches_login_lockout_rules (6 cases)` | 01 §2: CTA contains "nieuw wachtwoord" | Fixed in 01 |
| `AccountEmailCopyTests.No_entra_sbi_or_lobsy_subject_suffix_or_nieuw_wachtwoord` | 01 §2: banned "nieuw wachtwoord"/"new password" in CTA | Fixed in 01 |
| `EmailSnapshotTests.Structural_snapshots_for_every_registry_key` | Snapshot drift AccountLockout.* (01) + PasswordChanged.en | 01 for AccountLockout; check PasswordChanged.en copy, then regenerate |
| `CandidateEmailCopyTests.Nl_email_values_have_no_literal_minute_or_day_counts` | "De link werkt 30 minuten…": literal count | Fix the copy: format from the rule constant (`EmailFormat.Duration`) |
| `EmployerEmailCopyTests.Day_counts_follow_rule_constants_not_literals_in_values` | Same string as above | Same fix |
| `CandidateEmailCopyTests.No_stub_klik_hier_emdash_or_pushbom_brand_in_any_language` | Em dash "—" in an email value | Fix the copy (replace the em dash) |
| `AccessRequest07Tests.Access_approve_creates_membership_and_cannot_raise_role` | Expected mail subject not in the sent list | Align the test with the current subject, or restore the mail; check which is intended |

### B. Authorization / roles (could be real regressions: check the code first)

| Test | Symptom (2 Oct, acceptatie 3a15b0d7) | Direction |
|---|---|---|
| `Werkgever.ApplicationsApiTests.Bm_and_own_vm_can_react` | BranchManager gets 403 | Decision 7: follow `WerkgeverRightsMatrix`. Where the matrix grants BM (react on applications), **fix the code/policy**; where it denies, update the test |
| `Werkgever.ApplicationsApiTests.Filters_status_overdue_and_branchIds` | 403 | Same root cause |
| `Werkgever.ApplicationsApiTests.Privacy_dto_matches_LobsyCvAccessRules_for_bm_rm_vm` | 403 | Same root cause |
| `Werkgever.TokenRequestsApiTests.Checkout_vm_and_rm_forbidden_bm_not_forbidden` | BM checkout forbidden | Same root cause |
| `Werkgever.VacanciesManageApiTests.Bm_gets_non_403_on_lifecycle_endpoints` | BM 403 on lifecycle endpoints | Same root cause |
| `CandidateInsightsLockedJsonTests.Locked_json_omits_premium_values_for_bm_rm_vm` | 403 instead of 200 | Feature flag / role setup in test vs policy; check |
| `CandidateInsightsLockedJsonTests.Unlocked_json_includes_premium_sections` | 403 instead of 200 | Same |
| `CandidateInsightsUnlockApiTests.Feature_off_returns_404` | 403 instead of 404 when the feature is off | Order of the feature gate vs authorization |
| `AuthorizationMatrixReflectionTests.Mutating_actions_do_not_admit_RegionalManager_except_allow_list` | `MeEmailPreferencesController.Put`, `MfaController.RegenerateRecoveryCodes` admit RegionalManager | Self-service endpoints (own e-mail preferences, own recovery codes) are allowed for every signed-in role: add them to the allow-list with that reason |
| `Werkgever.WerkgeverRightsMatrixCompletenessTests.Matrix_covers_mutating_employer_endpoints` | Matrix is missing `api/applications/{id}/viewed`, `api/vacancies/{id}/ready` | Add the endpoints to `WerkgeverRightsMatrix` |
| `Werkgever.WerkgeverPageAuthorizeTests.Every_werkgever_page_authorize_matches_matrix` | `/werkgever/overnames` roles differ from the matrix | Align the page `[Authorize]` with the matrix |
| `AdminAuditRedesignTests.Reflection_guard_every_admin_reachable_non_get_has_audit_or_exempt` | `SalesCommercialController.RecordReferralVisit` has no `[AdminAudit]`/`[AdminAuditExempt]` | Add `[AdminAuditExempt]` with a reason (public tracking), or audit it |

### C. Login / MFA / SMTP

| Test | Symptom (2 Oct, acceptatie 3a15b0d7) | Direction |
|---|---|---|
| `LoginProtectionTests.Admin_without_mfa_is_redirected_before_admin_page_renders` | Redirect is now `/login?error=mfa-required` instead of `/account/mfa` | Decision 7: admins MFA required. The code must block the admin page before it renders and send the user into MFA enrolment/verification (ADR 0005). If the current `/login?error=mfa-required` does that (auth hotfix `46cb0511`), update the test to assert it; if not, fix the code |
| `LoginProtectionTests.Branch_manager_local_without_mfa_is_redirected` | Same | Same |
| `LoginProtectionTests.Fifth_failed_attempt_starts_fifteen_minute_lockout` | Expected 0, got 2h lockout | Test isolation (lockout history) or a changed rule; check `LoginLockoutRules` |
| `Sales.SalesFoundationUnitTests.MfaPolicy_requires_SalesManager_and_Ambassadeur` | Ambassadeur no longer requires MFA | Decision 7: Ambassadeur is paused behind its feature flag. Update the test: SalesManager requires MFA; Ambassadeur is asserted as paused (no login while the flag is off), with any MFA expectation only under flag-on, if `MfaPolicy` still contains it |
| `Sprint3CandidateTests.Smtp_resolve_requires_full_credentials` | SMTP resolves with incomplete credentials | Code fix: require full credentials |

### D. Feature flags, routes, legacy links, guards

| Test | Symptom (2 Oct, acceptatie 3a15b0d7) | Direction |
|---|---|---|
| `FeatureFlagReflectionTests.Expected_pages_carry_RequiresFeature_Employers` | Expected page list lacks `RequiresFeature(Employers)` | Update the expected list, or add the attribute |
| `Werkgever.WerkgeverFeatureGateTests.RequiresFeatureAttribute_absent_or_present_on_werkgever_pages` | `/werkgever/talentpool` has no `RequiresFeature` | Add the attribute (or update the expectation) |
| `KbLabelsCompletenessTests.KbRoutes_map_uses_fallback_home_when_banenkaart_absent` | Fallback home `/banenkaart` instead of `/` | Align with the current home rule (landing stack) |
| `LandingBunitTests.Feature_availability_prints_matches` | Assertion false | Landing feature print changed; update the test with the landing spec |
| `PlatformSettingsCatalogTests.Keys_unique_groups_ordered_one_policy_no_absent_flags` | Duplicate key / absent flag | Fix the catalog |
| `PlatformSettingsEditorBunitTests.Policy_row_has_lock_and_activation_disabled_in_productie` | No policy row with a lock | Fix the test setup or the catalog (same root cause as above) |
| `CandidateRegisterLinkGuardTests.Candidate_facing_razor_and_guides_do_not_link_to_register` | Candidate-facing `/register` links remain (RegisterToegang, RegisterVerifierenBrief, WaDone, CreateVacancy) | Replace them with the candidate routes, or allow-list the employer pages |
| `AdminLegacyHrefTests.No_internal_web_links_use_legacy_admin_urls` | `/admin/ambassadeurs`, `/admin/sales-managers` still referenced | Replace them with the new admin URLs |
| `Werkgever.WerkgeverLegacyHrefTests.No_internal_links_use_old_employer_urls` | `EmployerLinks.cs` still has `/employer/*` URLs | Replace them with the `/werkgever/*` URLs |
| `LegacyMapQueryTests.Key_list_matches_VacancyDiscovery_TryGetValue_keys` | Map query key list differs (`weergave` vs `maxMinutes`) | Sync the key list with `VacancyDiscovery` |
| `Uat.UatScenarioTests.Script(id: "UAT-0137", role: "Gast", scenario: "Open `/register/activate` (met of zonder token).")` | `/register/activate` has no `@page` | Update the UAT script (route removed) or restore the route |
| `PublicThemeCssGuardTests.Pub_classes_do_not_appear_in_MainLayout_pages` | `pub-` class used on a MainLayout page (status page `err-code`) | Use a non-`pub-` class on MainLayout pages |

### E. Candidate tests / deep analysis / Match

| Test | Symptom (2 Oct, acceptatie 3a15b0d7) | Direction |
|---|---|---|
| `CandidateProfileServiceTests.Profiel_page_redirects_to_kompas` | `StartDeepAnalysisCheckoutAsync` not on the tests page | Update the test to the current checkout flow (tests stack) |
| `RoleFunctionalRegressionTests.Candidate_deep_analysis_is_locked_per_kind_until_paid` | Upsell copy has no "beroepentest" | Update the copy or the test |
| `TestsOverviewBuilderTests.Paint_parity_marks_extended_gold_and_counts_done` | `DeepPay.Title` instead of `Test.Status.Extended` | Align with the current status mapping |
| `MobileSaasUxTests.Competency_test_discloses_privacy_and_blocks_save_after_load_failure` | `Competency.PrivacyNote` missing on `/candidate/competencies` | Restore the privacy note (a privacy disclosure, so likely a code fix) |
| `MatchDesktopBunitTests.MatchUnlockPanel_fit_gate_shows_no_percent` | `IFeatureFlags` not registered in the bUnit context | Test setup: register `IFeatureFlags` |

### F. Data / privacy / sales flows

| Test | Symptom (2 Oct, acceptatie 3a15b0d7) | Direction |
|---|---|---|
| `AccountUnsubscribeTests.Candidate_user_relation_catalog_requires_anonymize_coverage_for_every_model_entity` | New candidate entities lack anonymize coverage | **Privacy:** add the new entities to the anonymize/delete catalog (code fix) |
| `CoreFunctionalFlowE2ETests.Full_chain_company_manager_salesmanager_admin_prepaid_and_commissions` | Expected null, got a Guid | Check the flow; likely a changed default |
| `SalesManagerCommissionTests.Registration_with_tracking_code_links_supplier_and_reserves_slot` | No slot reserved | Check the tracking-code validation (see the unused `ValidateSalesOrAmbassadeurTrackingCodeAsync`, 06). SalesManager codes must reserve the slot; Ambassadeur codes only while its flag is on (Decision 7) |
