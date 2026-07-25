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

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        string? preferred = null;
        try
        {
            var state = await _authState.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated == true)
            {
                var api = _services.GetRequiredService<JobsyApiClient>();
                var profile = await api.GetMyProfileAsync();
                preferred = profile?.Preferences?.Language;
            }
        }
        catch
        {
            // Profile may be unavailable during early circuit start; fall back to cookie.
        }

        if (string.IsNullOrWhiteSpace(preferred))
        {
            try
            {
                preferred = await _js.InvokeAsync<string?>("jobsyCulture.get");
            }
            catch (JSException)
            {
                preferred = null;
            }
            catch (InvalidOperationException)
            {
                preferred = null;
            }
        }

        Apply(JobsyLanguages.Normalize(preferred));
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
}
