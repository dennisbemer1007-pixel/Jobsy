using System.Globalization;
using Jobsy.Core.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Jobsy.Web.Localization;

/// <summary>
/// Circuit-scoped culture state. Persists via cookie for everyone and PreferencesJson for signed-in users.
/// </summary>
public sealed class CultureState
{
    public const string CookieName = "Jobsy.Culture";

    private readonly IJSRuntime _js;
    private readonly IServiceProvider _services;
    private readonly AuthenticationStateProvider _authState;
    private bool _initialized;

    public CultureState(
        IJSRuntime js,
        IServiceProvider services,
        AuthenticationStateProvider authState)
    {
        _js = js;
        _services = services;
        _authState = authState;
    }

    public string Language { get; private set; } = JobsyLanguages.Default;

    public LanguageOption Current => JobsyLanguages.Get(Language);

    public bool IsRightToLeft => Current.IsRightToLeft;

    public event Action? Changed;

    public string this[string key] => UiStrings.Get(key, Language);

    public string Format(string key, params object[] args)
        => string.Format(CultureInfo.InvariantCulture, this[key], args);

    /// <summary>
    /// SSR/prerender culture from the request: <c>?lang=</c> → cookie → nl.
    /// Marks the state initialized so <see cref="InitializeAsync"/> keeps this value.
    /// </summary>
    public void InitializeFromRequest(HttpContext? http)
    {
        if (_initialized)
        {
            return;
        }

        Apply(CultureRequest.ResolveLanguage(http));
        _initialized = true;
    }

    /// <summary>
    /// Pins the culture for a static SSR render that resolved the language itself
    /// (error pages use cookie → Accept-Language → nl without touching the API).
    /// </summary>
    public void InitializeFromLanguage(string language)
    {
        if (_initialized)
        {
            return;
        }

        Apply(language);
        _initialized = true;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        string? cookie = null;
        try
        {
            cookie = await _js.InvokeAsync<string?>("jobsyCulture.get");
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        string? profileLanguage = null;
        JobsyApiClient? api = null;
        try
        {
            var state = await _authState.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated == true)
            {
                api = _services.GetRequiredService<JobsyApiClient>();
                var profile = await api.GetMyProfileAsync();
                profileLanguage = profile?.Preferences?.Language;
            }
        }
        catch
        {
            // Profile may be unavailable during early circuit start; fall back to cookie.
        }

        // An explicit language cookie wins over a stale profile, so the picker matches the page.
        var chosen = !string.IsNullOrWhiteSpace(cookie)
            ? JobsyLanguages.Normalize(cookie)
            : JobsyLanguages.Normalize(profileLanguage);
        Apply(chosen);

        if (api is not null
            && !string.IsNullOrWhiteSpace(cookie)
            && !string.IsNullOrWhiteSpace(profileLanguage)
            && !JobsyLanguages.AreSame(chosen, profileLanguage))
        {
            try
            {
                await api.UpdateMyLanguageAsync(chosen);
            }
            catch
            {
                // Cookie still holds the choice.
            }
        }
    }

    public async Task SetLanguageAsync(string language)
    {
        var normalized = JobsyLanguages.Normalize(language);
        if (JobsyLanguages.AreSame(Language, normalized))
        {
            return;
        }

        Apply(normalized);
        await PersistCookieAsync(Language);

        var auth = await _authState.GetAuthenticationStateAsync();
        if (auth.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var api = _services.GetRequiredService<JobsyApiClient>();
                await api.UpdateMyLanguageAsync(normalized);
            }
            catch
            {
                // Cookie still holds the choice; profile sync can retry on next visit.
            }
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Show <paramref name="language"/> for this circuit without writing the cookie or profile.
    /// Dispose restores the previous language so a Dutch-only surface does not stick.
    /// </summary>
    public IDisposable PushDisplayLanguage(string language)
    {
        var previous = Language;
        var normalized = JobsyLanguages.Normalize(language);
        if (!JobsyLanguages.AreSame(previous, normalized))
        {
            Apply(normalized);
            Changed?.Invoke();
        }

        return new DisplayLanguageScope(this, previous);
    }

    private void Apply(string language)
    {
        Language = JobsyLanguages.Normalize(language);
        var culture = new CultureInfo(JobsyLanguages.ToCultureName(Language));
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private async Task PersistCookieAsync(string language)
    {
        try
        {
            await _js.InvokeVoidAsync("jobsyCulture.set", language);
            await _js.InvokeVoidAsync("jobsyCulture.applyDocument", language, IsRightToLeft);
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    public async Task SyncDocumentAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("jobsyCulture.applyDocument", Language, IsRightToLeft);
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private sealed class DisplayLanguageScope : IDisposable
    {
        private readonly CultureState _state;
        private readonly string _previous;
        private bool _disposed;

        public DisplayLanguageScope(CultureState state, string previous)
        {
            _state = state;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (JobsyLanguages.AreSame(_state.Language, _previous))
            {
                return;
            }

            _state.Apply(_previous);
            _state.Changed?.Invoke();
        }
    }
}
