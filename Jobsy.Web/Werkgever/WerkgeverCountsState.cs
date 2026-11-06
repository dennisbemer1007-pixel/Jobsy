using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Web.Services;

namespace Jobsy.Web.Werkgever;

/// <summary>
/// Sidebar / bottom-nav badge counts from te-doen (refreshed on navigation, ≤ once per 60 s).
/// </summary>
public sealed class WerkgeverCountsState : IDisposable
{
    private readonly JobsyApiClient _api;
    private readonly EmployerScopeState _scope;
    private readonly IFeatureFlags _flags;
    private DateTime _lastFetchUtc = DateTime.MinValue;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public IReadOnlyDictionary<string, int> Counts { get; private set; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public event Action? Changed;

    public bool MaqqieHoursNavVisible { get; private set; }

    public WerkgeverCountsState(JobsyApiClient api, EmployerScopeState scope, IFeatureFlags flags)
    {
        _api = api;
        _scope = scope;
        _flags = flags;
    }

    public async Task RefreshIfStaleAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (DateTime.UtcNow - _lastFetchUtc < TimeSpan.FromSeconds(60) && Counts.Count > 0)
        {
            return;
        }

        await ForceRefreshAsync(ct);
    }

    public async Task ForceRefreshAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_scope.CompanyIds.Count == 0)
        {
            return;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (DateTime.UtcNow - _lastFetchUtc < TimeSpan.FromSeconds(60) && Counts.Count > 0)
            {
                return;
            }

            IReadOnlyList<WerkgeverTodoItemDto> items;
            try
            {
                items = await _api.GetWerkgeverTodoAsync(_scope.CompanyIds, take: 50, ct);
            }
            catch
            {
                return;
            }

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["todo"] = items.Sum(i => i.Count),
                ["applications"] = items
                    .Where(i => string.Equals(i.Kind, nameof(WerkgeverTodoKind.ApplicationsOverdue), StringComparison.OrdinalIgnoreCase)
                                || string.Equals(i.Kind, "ApplicationsOverdue", StringComparison.OrdinalIgnoreCase))
                    .Sum(i => i.Count),
                ["vacancies"] = items
                    .Where(i => string.Equals(i.Kind, "PublishRequests", StringComparison.OrdinalIgnoreCase))
                    .Sum(i => i.Count)
            };

            // Pending applications count for Sollicitaties badge prefers overdue; if zero keep todo-derived.
            Counts = map;
            MaqqieHoursNavVisible = false;
            if (await _flags.IsEnabledAsync(PlatformFeature.EmployerPhase2, ct))
            {
                try
                {
                    MaqqieHoursNavVisible = await _api.GetMaqqieHoursEmployerActiveAsync(ct);
                }
                catch
                {
                    MaqqieHoursNavVisible = false;
                }
            }

            _lastFetchUtc = DateTime.UtcNow;
            Changed?.Invoke();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _gate.Dispose();
    }
}
