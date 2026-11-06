using Jobsy.Web.Services;

namespace Jobsy.Web.Components.Admin.Shell;

/// <summary>
/// Circuit-scoped sidebar count pills. Loaded once + on <see cref="AdminTodoChanged"/>, not on every navigation.
/// </summary>
public sealed class AdminTodoCountsStore
{
    private IReadOnlyDictionary<string, int> _counts =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public event Action? Changed;

    public IReadOnlyDictionary<string, int> Counts => _counts;

    public int Get(string? countKey)
    {
        if (string.IsNullOrWhiteSpace(countKey))
        {
            return 0;
        }

        return _counts.TryGetValue(countKey, out var n) ? n : 0;
    }

    public int GroupSum(IEnumerable<string?> countKeys)
        => countKeys.Sum(Get);

    public void Apply(IReadOnlyDictionary<string, int>? counts)
    {
        _counts = counts is null
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : new Dictionary<string, int>(counts, StringComparer.Ordinal);
        Changed?.Invoke();
    }

    public void Apply(AdminTodoResponseItem? response)
        => Apply(response?.CountsByNavKey);
}

/// <summary>Raised after a relevant admin write so sidebar/dashboard refresh todo counts.</summary>
public sealed class AdminTodoChanged
{
    public event Func<Task>? Handlers;

    public async Task NotifyAsync()
    {
        var handlers = Handlers;
        if (handlers is null)
        {
            return;
        }

        foreach (var d in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            await d();
        }
    }
}
