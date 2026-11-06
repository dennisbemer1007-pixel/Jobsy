using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Jobsy.Tests;

/// <summary>
/// Smoke coverage for the Sentry.AspNetCore 6 bump: CaptureException reaches a
/// custom transport with the configured environment and without default PII.
/// </summary>
public class SentryAspNetCore6SmokeTests
{
    [Fact]
    public async Task CaptureException_reaches_transport_with_environment_and_no_default_pii()
    {
        var transport = new RecordingTransport();
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = "https://public@example.com/1";
            o.SendDefaultPii = false;
            o.TracesSampleRate = 0;
            o.Environment = "code-health-08a";
            o.Transport = transport;
            o.AutoSessionTracking = false;
        });

        SentrySdk.CaptureException(new InvalidOperationException("sentry-6-smoke"));
        await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));

        Assert.NotEmpty(transport.Envelopes);
        var json = await EnvelopeToStringAsync(transport.Envelopes[0]);
        Assert.Contains("sentry-6-smoke", json, StringComparison.Ordinal);
        Assert.Contains("code-health-08a", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"email\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Api_and_web_keep_SendDefaultPii_false()
    {
        var api = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Api", "Program.cs"));
        var web = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "Program.cs"));
        Assert.Contains("options.SendDefaultPii = false", api, StringComparison.Ordinal);
        Assert.Contains("options.SendDefaultPii = false", web, StringComparison.Ordinal);
        Assert.Contains("Sentry.AspNetCore", File.ReadAllText(Path.Combine(RepoRoot(), "Directory.Packages.props")));
        Assert.Contains("Version=\"6.", File.ReadAllText(Path.Combine(RepoRoot(), "Directory.Packages.props")));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private static async Task<string> EnvelopeToStringAsync(Envelope envelope)
    {
        await using var ms = new MemoryStream();
        await envelope.SerializeAsync(ms, null!, CancellationToken.None);
        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    private sealed class RecordingTransport : ITransport
    {
        public List<Envelope> Envelopes { get; } = [];

        public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            Envelopes.Add(envelope);
            return Task.CompletedTask;
        }
    }
}
