using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Admin.Shell;

/// <summary>Per-circuit open/closed state for admin sidebar groups (no JS).</summary>
public sealed class AdminSidebarState
{
    private readonly HashSet<string> _openKeys = new(StringComparer.OrdinalIgnoreCase);
    private string? _forcedOpenKey;

    public event Action? Changed;

    public bool IsOpen(string groupKey)
    {
        if (_forcedOpenKey is not null
            && string.Equals(_forcedOpenKey, groupKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _openKeys.Contains(groupKey);
    }

    public void Toggle(string groupKey)
    {
        if (!_openKeys.Add(groupKey))
        {
            _openKeys.Remove(groupKey);
        }

        Changed?.Invoke();
    }

    public void EnsureActiveGroupOpen(string? groupKey)
    {
        _forcedOpenKey = groupKey;
        if (groupKey is not null)
        {
            _openKeys.Add(groupKey);
        }

        Changed?.Invoke();
    }
}
