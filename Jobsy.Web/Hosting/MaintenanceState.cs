using System.Net.Http.Json;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Hosting;

/// <summary>
/// The maintenance state the Web host believes in, refreshed from <c>api/site/status</c> every
/// 15 seconds by <see cref="MaintenancePoller"/> (errors 05 §05.3).
/// <para>
/// A request never waits on the API: it reads the last known value. When the API is unreachable
/// the previous answer is kept, and a cold start defaults to "off" so an API outage can never put
/// the public site into maintenance by itself.
/// </para>
/// </summary>
public class MaintenanceState
{
    private volatile Snapshot _current = new(false, null);

    public virtual bool IsEnabled => _current.Enabled;

    public virtual DateTime? ExpectedEndUtc => _current.ExpectedEndUtc;

    /// <summary>Seconds to put in <c>Retry-After</c> for the current state (E8).</summary>
    public virtual int RetryAfterSeconds
        => MaintenanceRules.RetryAfterSeconds(ExpectedEndUtc, DateTime.UtcNow);

    public void Apply(bool enabled, DateTime? expectedEndUtc)
        => _current = new Snapshot(
            enabled,
            expectedEndUtc is DateTime end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null);

    private sealed record Snapshot(bool Enabled, DateTime? ExpectedEndUtc);
}

/// <summary>Polls <c>api/site/status</c> and feeds <see cref="MaintenanceState"/>.</summary>
public sealed class MaintenancePoller : BackgroundService
{
    public const string StatusPath = "api/site/status";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    private readonly MaintenanceState _state;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MaintenancePoller> _logger;

    public MaintenancePoller(
        MaintenanceState state,
        IHttpClientFactory httpFactory,
        ILogger<MaintenancePoller> logger)
    {
        _state = state;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(MaintenanceRules.StatePollSeconds));
        await PollOnceAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollOnceAsync(stoppingToken);
        }
    }

    /// <summary>One poll. Public so a test can prove an API outage keeps the last state.</summary>
    public async Task PollOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(RequestTimeout);
            var http = _httpFactory.CreateClient(Jobsy.Web.Branding.PlatformBrandingState.HttpClientName);
            var status = await http.GetFromJsonAsync<SiteStatusResponse>(StatusPath, cts.Token);
            if (status is not null)
            {
                _state.Apply(status.Maintenance, status.ExpectedEndUtc);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
        catch (Exception ex)
        {
            // Keep the last known state: an API hiccup must not flip the public site either way.
            _logger.LogDebug(ex, "Maintenance status poll failed; keeping the last known state");
        }
    }

    private sealed record SiteStatusResponse(bool Maintenance, DateTime? ExpectedEndUtc);
}
