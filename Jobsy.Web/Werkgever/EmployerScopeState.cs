using Jobsy.Web.Navigation;

namespace Jobsy.Web.Werkgever;

public enum EmployerScopeKind
{
    Organisation,
    Region,
    Vestiging
}

public sealed record EmployerScopeOption(
    EmployerScopeKind Kind,
    Guid? Id,
    string Label,
    string? SubLabel = null)
{
    public string Key => Kind switch
    {
        EmployerScopeKind.Organisation => "org",
        EmployerScopeKind.Region => $"region:{(Id ?? Guid.Empty):D}",
        EmployerScopeKind.Vestiging => $"vestiging:{(Id ?? Guid.Empty):D}",
        _ => "org"
    };

    public static bool TryParse(string? raw, out EmployerScopeKind kind, out Guid? id)
    {
        kind = EmployerScopeKind.Organisation;
        id = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (string.Equals(raw, "org", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var parts = raw.Split(':', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var parsed))
        {
            return false;
        }

        if (string.Equals(parts[0], "region", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmployerScopeKind.Region;
            id = parsed;
            return true;
        }

        if (string.Equals(parts[0], "vestiging", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmployerScopeKind.Vestiging;
            id = parsed;
            return true;
        }

        return false;
    }
}

/// <summary>
/// Per-circuit employer scope (D3). Narrows UI filters; server endpoints still re-check access.
/// </summary>
public sealed class EmployerScopeState
{
    public EmployerRole? Role { get; private set; }
    public IReadOnlyList<EmployerScopeOption> AvailableScopes { get; private set; } = [];
    public EmployerScopeOption? Current { get; private set; }
    public string Label => Current?.Label ?? "";
    public string? SubLabel => Current?.SubLabel;
    public IReadOnlyList<Guid> CompanyIds { get; private set; } = [];
    public bool ScopeDeniedToast { get; private set; }
    public bool IsReadOnly => Role == EmployerRole.Regiomanager;
    /// <summary>D20: gold lock on Kandidaatinzichten nav while current scope is not full.</summary>
    public bool InsightsLocked { get; private set; } = true;

    public event Action? Changed;

    private IReadOnlyDictionary<string, IReadOnlyList<Guid>> _scopeCompanyMap =
        new Dictionary<string, IReadOnlyList<Guid>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Company ids for a scope key (org / region:… / vestiging:…).</summary>
    public IReadOnlyList<Guid> CompanyIdsFor(string? scopeKey)
    {
        if (string.IsNullOrWhiteSpace(scopeKey))
        {
            return [];
        }

        return _scopeCompanyMap.TryGetValue(scopeKey, out var ids) ? ids : [];
    }

    public void Initialize(
        EmployerRole role,
        IReadOnlyList<EmployerScopeOption> available,
        IReadOnlyDictionary<string, IReadOnlyList<Guid>> scopeCompanyMap,
        string? requestedScopeKey = null)
    {
        Role = role;
        AvailableScopes = available;
        _scopeCompanyMap = scopeCompanyMap;
        ScopeDeniedToast = false;

        EmployerScopeOption? selected = null;
        if (!string.IsNullOrWhiteSpace(requestedScopeKey)
            && EmployerScopeOption.TryParse(requestedScopeKey, out _, out _))
        {
            selected = available.FirstOrDefault(a =>
                string.Equals(a.Key, requestedScopeKey, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                ScopeDeniedToast = true;
            }
        }

        selected ??= DefaultFor(role, available);
        SetCurrent(selected);
    }

    public void Select(EmployerScopeOption option)
    {
        if (AvailableScopes.All(a => a.Key != option.Key))
        {
            ScopeDeniedToast = true;
            option = DefaultFor(Role ?? EmployerRole.Bedrijfsmanager, AvailableScopes) ?? option;
        }

        SetCurrent(option);
    }

    public void SetInsightsLocked(bool locked)
    {
        if (InsightsLocked == locked)
        {
            return;
        }

        InsightsLocked = locked;
        Changed?.Invoke();
    }

    public void ClearDeniedToast() => ScopeDeniedToast = false;

    private void SetCurrent(EmployerScopeOption? option)
    {
        Current = option;
        if (option is not null
            && _scopeCompanyMap.TryGetValue(option.Key, out var ids))
        {
            CompanyIds = ids;
        }
        else
        {
            CompanyIds = [];
        }

        Changed?.Invoke();
    }

    public static EmployerScopeOption? DefaultFor(
        EmployerRole role,
        IReadOnlyList<EmployerScopeOption> available)
    {
        if (available.Count == 0)
        {
            return null;
        }

        return role switch
        {
            EmployerRole.Bedrijfsmanager =>
                available.FirstOrDefault(a => a.Kind == EmployerScopeKind.Organisation) ?? available[0],
            EmployerRole.Regiomanager =>
                available.FirstOrDefault(a => a.Kind == EmployerScopeKind.Region) ?? available[0],
            EmployerRole.Vestigingsmanager =>
                available.FirstOrDefault(a => a.Kind == EmployerScopeKind.Vestiging) ?? available[0],
            _ => available[0]
        };
    }
}
