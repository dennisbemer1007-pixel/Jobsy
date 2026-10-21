# Client-side forms (Blazor Server islands)

This document describes the pattern used to move **interactive candidate forms off the Blazor Server circuit** so typing, taps, and tile clicks stay in the browser. Onboarding (start → steps 1–10 → Kompas) is the reference implementation.

## Why Blazor Server froze onboarding

On Blazor Server, most UI events (`@onclick`, `@bind`, `@bind:event="oninput"`) are sent to the server over the SignalR circuit. The handler runs on the server; only then does the UI update.

### Evidence: address suggest blocked the circuit

Step 1 used `PersonalStep.SuggestAddressesAsync`, which **awaits** `IGeocodingClient.SuggestAsync` on the circuit thread:

```438:503:Jobsy.Web/Components/Candidate/Onboarding/PersonalStep.razor
    private async Task SuggestAddressesAsync(string query)
    {
        // ...
        _suggesting = true;
        try
        {
            await Task.Delay(300, token);
            using var geocodeTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            geocodeTimeout.CancelAfter(TimeSpan.FromSeconds(3));
            var results = await Geocoder.SuggestAsync(trimmed, geocodeTimeout.Token);
            // ...
        }
        finally
        {
            if (generation == _suggestGeneration)
            {
                _suggesting = false;
                await SafeStateHasChangedAsync();
            }
        }
    }
```

If the geocoder HTTP call **hangs** or ignores cancellation, the handler never completes. `_suggesting` stays true (“Laden…”), and **later events on the same circuit queue behind it**—chips and Volgende feel dead while the WebSocket stays connected (no console error). Acc sessions after #842 still hit this path because suggest remained circuit-bound.

### Amplifiers (not always sole cause)

- **Per-keystroke `@bind` + `StateHasChanged`** on large steps.
- **`OnboardingWizardProfileAutosave`**: serialized saves (`SemaphoreSlim`) + debounced `PUT api/me/profile` on every draft change.
- **Mini-test `AnswerAsync`**: autosave + re-render per Likert click.

The fix is not “more debouncing on the server” but **no server round-trip per click/keystroke** for step inputs.

## Pattern overview

| Layer | Responsibility |
|--------|----------------|
| **Razor island** | One render: static HTML shell + `data-initial` JSON. `ShouldRender()` → `false` after JS mount. |
| **`onboarding-client-form.js`** | Mount listeners, local state, DOM updates, `collect(step)` JSON. |
| **`OnboardingClientStepInitialState`** | Prefill JSON from `OnboardingProfileDraft` + seed (questions, dream titles, labels). |
| **`OnboardingClientStepBridge`** | On Volgende only: deserialize JSON → apply to draft / mini answers. |
| **Wizard** | `TryImportClientStepAsync()` → validate → **one** `SaveProfileAsync` / mini flush / consent API. |

Assets: `Jobsy.Web/wwwroot/js/onboarding-client-form.js`, registered on `/candidate/start` via `PageAssetCatalog`.

## Shared JS helper (`jobsyOnboardingClient`)

```javascript
jobsyOnboardingClient.mount(rootElement, stepNumber);
jobsyOnboardingClient.collect(stepNumber); // JSON string
jobsyOnboardingClient.unmount(rootElement);
```

- **WeakMap** per root for step state (presets, matrix, suggest generation, mini answers).
- **No Blazor calls** during typing/tapping.
- **Mobile**: use the same markup/CSS as illustrated steps (`lobsy-choice-tile`, `availability-matrix__cell--btn`, range inputs with `input` events). Touch taps are plain `click` listeners (no `@onclick`).

## Prefill when the user returns

1. Server loads profile + onboarding progress into `OnboardingProfileDraft`.
2. Island sets `data-initial="@OnboardingClientStepInitialState.Build(...)"` once.
3. On mount, JS reads `data-initial` and sets inputs, selected tiles, matrix cells, mini answers.
4. Revisiting a step remounts the island (step change) with fresh JSON from the draft—no autosave needed to repopulate.

## Save on Volgende only

Flow in `OnboardingWizard.NextAsync`:

1. `TryImportClientStepAsync()` → `jobsyOnboardingClient.collect(_step)` → `OnboardingClientStepBridge.Apply`.
2. Step-specific server work (e.g. step 1 `EnsureAddressResolvedAsync` geocode once if lat/lng missing).
3. `GetValidationIssues()` on the draft (unchanged rules in `OnboardingWizardValidation`).
4. On failure: `_showValidation = true`, scroll to anchor; **no** navigation.
5. On success: `FlushBeforeLeavingStepAsync()` → single profile PUT and/or mini block flush / dream save / consent accept.
6. Advance step via `SaveMyOnboardingProgressAsync`.

Autosave is **disabled** while `_step <= 10` on client steps (`ScheduleProfileSave` early return; mini answers not sent per click).

Errors from API persist as today (`_error`, `_saveError`, rustig save status).

## Address suggest (browser fetch)

- **Minimum length 3** — no `fetch` while the trimmed query has fewer than three characters (input handler and `fetchSuggest` both enforce this; the API returns `[]` for `q` shorter than 3 as a backstop).
- Endpoint: `GET /api/onboarding-client/address-suggest?q=…` (candidate auth, server timeout 4s).
- JS: debounce **300ms**, `AbortController` **4.5s**, `credentials: "same-origin"`.
- Stale generation ignored (`suggestGen`).
- Empty/error: show fallback copy (NL: *Geen suggesties, typ je adres verder*), not an infinite “Laden…”.

This keeps geocode latency **off** the SignalR circuit.

## Checklist: convert another candidate page

1. **Identify hot path** — inputs that `@bind` or call `StateHasChanged` / autosave on every event.
2. **Add a static shell** — one Razor mount or HTML template with `data-*` hooks; no `@bind` on hot fields.
3. **Add `data-initial` JSON** — map server model → camelCase DTO; include UI strings for JS.
4. **Add JS module** — mount/unmount/collect; local state; fetch for any slow IO (debounce + timeout + fallback).
5. **Add bridge** — `Apply(draft, json)` called only from Volgende/submit.
6. **Wire page** — import before validate/save; disable autosave for that section.
7. **Tests** — Playwright: rapid tap/type, assert DOM + values, **count network saves** (exactly one per submit); run at **390×844**, `IsMobile` + `HasTouch`.
8. **Mobile pass** — hit targets, matrix/slider touch, keyboard open on inputs; no reliance on hover.

## Related files

- `Jobsy.Web/Components/Candidate/Onboarding/OnboardingClientStepIsland.razor`
- `Jobsy.Web/Onboarding/OnboardingClientStepBridge.cs`
- `Jobsy.Web/Onboarding/OnboardingClientStepInitialState.cs`
- `Jobsy.Web/Hosting/OnboardingClientEndpoints.cs`
- `Jobsy.Tests/OnboardingClientStepsSaveOncePlaywrightTests.cs` (+ mobile fixture)
