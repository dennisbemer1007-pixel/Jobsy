using Jobsy.Core.Localization;

namespace Jobsy.Tests;

/// <summary>
/// Enables the full UI catalog (nl,en,pl,ro,ar) for tests that assert localized copy or RTL.
/// Production default remains nl,en only via <see cref="JobsyLanguages.ResetEnabledForTests"/>.
/// Serialized so parallel tests do not reset each other's <see cref="JobsyLanguages"/> static state.
/// </summary>
internal sealed class EnabledCulturesScope : IDisposable
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private bool _held;

    public EnabledCulturesScope(string enabledCsv = "nl,en,pl,ro,ar")
    {
        Gate.Wait();
        _held = true;
        JobsyLanguages.ConfigureEnabledCultures(enabledCsv);
    }

    public void Dispose()
    {
        if (!_held)
        {
            return;
        }

        try
        {
            JobsyLanguages.ResetEnabledForTests();
        }
        finally
        {
            _held = false;
            Gate.Release();
        }
    }
}
