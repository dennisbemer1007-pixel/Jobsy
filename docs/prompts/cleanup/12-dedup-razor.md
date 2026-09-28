Branch from `acceptatie`. ONE PR into `acceptatie`. Do not merge, do not deploy, do not use rule 123.

# 12: Razor deduplication: questionnaire shell, payout stub, culture-aware base component, CircuitInterop

**Goal:** remove copied Razor blocks. Rendering and behaviour stay exactly the same.

**Evidence (code-review.md §2.3, 2.4, 2.8, 2.9):**
- **Questionnaire pages:** `Candidate/CultureScan.razor:152-241` ≡ `Candidate/ValuesScan.razor:151-240` ≡ `Candidate/CareerTest.razor:211-300`; `CompetencyTest.razor:201-236` is similar. `Components/Shared/Questionnaire/QuestionnaireShell.razor` + `LikertScaleQuestion.razor` already exist; check what they cover.
- **Payout stubs:** `Pages/Ambassadeur/PayoutCheckoutStub.razor` ≡ `Pages/SalesManager/PayoutCheckoutStub.razor` (+ similar `Employer/PartnerSalesPayoutCheckoutStub.razor`).
- **Culture boilerplate:** 83 components have `Culture.Changed += ...; InvokeAsync(StateHasChanged)` + `Dispose` with `-=`.
- **Empty catches:** 166 empty `catch {}` blocks around JS interop, while `Jobsy.Web/Hosting/CircuitInterop.cs` `InvokeVoidIgnoredAsync` exists and is unused.

**Do:**
1. **Questionnaire:** extend the existing `QuestionnaireShell` (or add `QuestionnairePageBody`) with the shared progress/question/navigation markup and parameters (questions, answers, onNext/onPrev, labels). Use it in CultureScan, ValuesScan and CareerTest. Do CompetencyTest only if it fits without special cases.
2. **Payout stub:** add `Components/Shared/PayoutCheckoutStubView.razor` with parameters (back URL, API call delegate, texts). The two or three pages keep their `@page` routes and `[Authorize]` roles and only render the view.
3. **Culture:** add `Components/Shared/CultureAwareComponentBase.cs` (inherits `ComponentBase`, `IDisposable`; subscribes in `OnInitialized`, unsubscribes in `Dispose`, virtual `OnCultureChanged`). Migrate **only components not touched by pending PRs** (leave VacancyDiscovery, VacancyCard, VacancyDetail, BottomNav, TestDetail, MatchPage, SalesWalletChip, CookieConsentBanner, HowLobsyWorks, GratisDna*). Up to ~20 components in this PR; list the rest as follow-up.
4. **CircuitInterop:** in the same migrated components, replace `try { await Js.InvokeVoidAsync(...) } catch { }` with `CircuitInterop.InvokeVoidIgnoredAsync`, only where the catch was empty and meant to ignore disconnects.

**Do not touch:** any file changed by pending PRs (list above); CSS; route or authorization attributes; UiStrings keys.

**Verify:**
- Build and tests green; bUnit/render tests where they exist.
- Playwright: complete culture scan, values scan and career test (answer all questions, check the result page appears and is saved); open both payout stub pages as the demo sales manager/ambassadeur; switch language on a migrated page and confirm the texts update without reload.
- Line count removed reported in the PR.

**Dependency:** **wait until the TestDetail/MatchPage persisted-state PR is merged** (questionnaire flow overlaps), and the SalesWalletChip PR (sales pages). After 08 (deleted components are gone).
