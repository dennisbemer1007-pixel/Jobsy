namespace Jobsy.Web.Services;

/// <summary>Page hint for the fixed pupil coach. The layout owns the bubble.</summary>
public sealed class LeerlingCoachHint
{
    public string? Hint { get; private set; }

    public event Action? Changed;

    public void Set(string? hint)
    {
        var next = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
        if (string.Equals(Hint, next, StringComparison.Ordinal))
        {
            return;
        }

        Hint = next;
        Changed?.Invoke();
    }
}
