Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 13: Structure: split JobsyApiClient into partial files and move non-page components out of Pages/

**Goal:** make god files and folders navigable with **pure moves**: no logic changes and no renames of public types or namespaces that would ripple.

**Evidence:**
- `Jobsy.Web/Services/JobsyApiClient.cs` is 5,212 lines: 303 methods + 68 DTO classes in one file.
- There are 22 `.razor` files under `Components/Pages/**` without `@page` (panels, subnavs, actions). List them with:
  ```
  for f in $(rg --files -g '*.razor' Jobsy.Web/Components/Pages); do rg -q '^@page' $f || echo $f; done
  ```
  Examples: `AmbassadeurHomePanel`, `EmployerHomePanel`, `SalesManagerHomePanel`, `Candidate/*Panel.razor`, `CandidateSavedSubnav`, `SetUnavailableAction`, `Admin/AdminHomePanel`.

**Do:**
1. **JobsyApiClient:** make it `partial` and split it by area into `Services/ApiClient/JobsyApiClient.{Auth,Me,Vacancies,Applications,Employer,Tokens,Sales,Admin,Tests,Misc}.cs`. Move the DTO classes to `Services/ApiClient/Models/*.cs` (one file per area). **Keep the namespaces unchanged**, so no `using` churn, and keep member order within areas. Use `git mv` for the main file so history follows it.
2. **Components:** move the 22 non-page files to `Components/<Area>/` (e.g. `Components/Candidate/`, `Components/Home/`, `Components/Admin/`). Razor component namespaces follow folders, so add the needed `@using` to `_Imports.razor` of the area, or keep an `@namespace` directive with the old namespace in each moved file (**preferred: `@namespace` = old namespace**, zero ripple). Update `.cursor/rules` or docs that mention the paths.
3. Update tests that read these files by path (`File.ReadAllText(... "Pages/...")`): `rg -n "Components/Pages/.*(Panel|Subnav|Action)" Jobsy.Tests tools`.

**Do not touch:** method bodies, signatures, DTO property names (JSON), routes, CSS (scoped `.razor.css` files move together with their component, if any).

**Verify:**
- Build green with the same warning count.
- All tests green.
- `git diff -M --stat` shows renames (similarity ≥ 90%) plus small import edits.
- Playwright smoke of role homes (candidate, employer, sales, ambassadeur, admin) and the candidate kompas/dashboard panels.

**Dependency:** **wait until the SalesWalletChip/API-client PR is merged** (JobsyApiClient), and **all pending razor PRs** (VacancyCard, VacancyDetail, TestDetail/MatchPage, bottom-nav feedback, hoe-werkt-lobsy, gratis-dna). Do this after 08 and 12 so that dead and duplicated code is not moved. Best done in a quiet moment (touches many files).
