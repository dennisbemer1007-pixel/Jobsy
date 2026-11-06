using System.Globalization;
using Jobsy.Core.Localization;

namespace Jobsy.Tests;

/// <summary>
/// <see cref="Jobsy.Web.Localization.CultureState"/> switches the culture for the whole process, so a test
/// that renders an ar or en page would otherwise leave that culture behind for every test that runs after it.
/// Capture the ambient culture before rendering and restore it when the test class is done.
/// Also enables the full legal-page language set for <c>?lang=</c> resolution in bUnit.
/// </summary>
internal sealed class AmbientCultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

    public AmbientCultureScope()
    {
        JobsyLanguages.ConfigureEnabledCultures("nl,en,pl,ro,ar");
    }

    public void Dispose()
    {
        JobsyLanguages.ResetEnabledForTests();
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
