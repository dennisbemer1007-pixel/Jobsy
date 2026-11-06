namespace Jobsy.Web.Services;

/// <summary>
/// Circuit-scoped signal so the voorlezen switch and speaker buttons stay in step.
/// Speech itself never leaves the browser.
/// </summary>
public sealed class ReadAloudCoordinator
{
    public string? ActiveKey { get; private set; }

    public event Action? ActiveChanged;

    public event Action? PreferenceChanged;

    public void SetActive(string? key)
    {
        if (string.Equals(ActiveKey, key, StringComparison.Ordinal))
        {
            return;
        }

        ActiveKey = key;
        ActiveChanged?.Invoke();
    }

    public void NotifyPreferenceChanged() => PreferenceChanged?.Invoke();
}

/// <summary>Result of the browser voice probe. No spoken text is included.</summary>
public sealed class ReadAloudProbe
{
    public bool Supported { get; set; }

    public bool Enabled { get; set; }
}
