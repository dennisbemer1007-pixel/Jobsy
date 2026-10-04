namespace Jobsy.Web.Services;

/// <summary>
/// Page hint for the fixed pupil coach. Dismissal lasts for this circuit
/// and is also stored in sessionStorage by <c>LeerlingCoach</c>.
/// </summary>
public sealed class LeerlingCoachHint
{
    private readonly HashSet<string> _dismissed = new(StringComparer.Ordinal);

    public string? Hint { get; private set; }

    public bool StorageLoaded { get; private set; }

    public event Action? Changed;

    public bool ShouldShow(string? hint)
    {
        if (!StorageLoaded || string.IsNullOrWhiteSpace(hint))
        {
            return false;
        }

        return !_dismissed.Contains(hint.Trim());
    }

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

    public void RememberDismissed(IEnumerable<string>? texts)
    {
        if (texts is not null)
        {
            foreach (var text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _dismissed.Add(text.Trim());
                }
            }
        }

        StorageLoaded = true;
        Changed?.Invoke();
    }

    public void MarkLoaded()
    {
        if (StorageLoaded)
        {
            return;
        }

        StorageLoaded = true;
        Changed?.Invoke();
    }

    public void Dismiss(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return;
        }

        if (_dismissed.Add(hint.Trim()))
        {
            Changed?.Invoke();
        }
    }
}
