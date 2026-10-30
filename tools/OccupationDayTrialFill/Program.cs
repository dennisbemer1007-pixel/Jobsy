using System.Text.Json;
using Jobsy.Core.Careers;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.OpenAi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var replace = args.Contains("--replace", StringComparer.OrdinalIgnoreCase);
var filteredArgs = args.Where(a => !string.Equals(a, "--replace", StringComparison.OrdinalIgnoreCase)).ToArray();
var manifestPath = filteredArgs.Length > 0
    ? filteredArgs[0]
    : Path.Combine(root, "docs/mockups/leerling/werelden/data/trial-20-manifest.json");
var outDir = Path.Combine(root, "docs/mockups/leerling/werelden/data");
Directory.CreateDirectory(outDir);
var exportPath = Path.Combine(outDir, "occupation-day-in-life.nl.json");
var costPath = Path.Combine(outDir, "trial-20-cost.json");

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine("Manifest missing: " + manifestPath);
    return 1;
}

var manifest = JsonSerializer.Deserialize<TrialManifest>(
    await File.ReadAllTextAsync(manifestPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
if (manifest?.Occupations is not { Count: > 0 } list)
{
    Console.Error.WriteLine("Empty manifest.");
    return 1;
}

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set OPENAI_API_KEY or OpenAI__ApiKey for the approved trial.");
    return 2;
}

var dbName = "trial-" + Guid.NewGuid().ToString("N");
var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole().SetMinimumLevel(LogLevel.Warning));
services.AddHttpClient(OccupationDayInLifeOpenAiWriter.HttpClientName);
services.AddSingleton<IIntegrationCredentialService>(_ => new EnvCredentials(apiKey));
services.AddSingleton<IOptions<OpenAiOptions>>(_ => Options.Create(new OpenAiOptions { ApiKey = apiKey, Model = "gpt-4o" }));
services.AddSingleton<IOptions<OccupationDayInLifeOptions>>(_ => Options.Create(new OccupationDayInLifeOptions
{
    Model = "gpt-4o",
    DelayMilliseconds = 300
}));
services.AddSingleton<IOptions<AiOptions>>(_ => Options.Create(new AiOptions()));
services.AddSingleton<IOpenAiEndpointResolver, OpenAiEndpointResolver>();
services.AddSingleton<OccupationDayInLifeOpenAiWriter>();
services.AddSingleton<UsageTrackingWriter>();
services.AddSingleton<IOccupationDayInLifeWriter>(sp => sp.GetRequiredService<UsageTrackingWriter>());
services.AddSingleton<OccupationDayInLifeTranslator>();
services.AddSingleton<IOccupationDayInLifeTranslator>(sp => sp.GetRequiredService<OccupationDayInLifeTranslator>());
services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(dbName));
services.AddSingleton<OccupationDayInLifeGenerator>();

await using var provider = services.BuildServiceProvider();
var tracker = provider.GetRequiredService<UsageTrackingWriter>();
var generator = provider.GetRequiredService<OccupationDayInLifeGenerator>();

var only = list.Select(o => o.EscoId).ToHashSet(StringComparer.OrdinalIgnoreCase);
var started = DateTime.UtcNow;
var result = await generator.GenerateMissingAsync(
    Math.Min(20, only.Count),
    null,
    only,
    replaceExisting: replace,
    CancellationToken.None);
var export = await generator.ExportAsync();
await File.WriteAllTextAsync(exportPath, JsonSerializer.Serialize(export, OccupationDayJson.Options));

var cost = OccupationDayBatchPlan.EstimateFromUsage(tracker.PromptTokens, tracker.CompletionTokens);
await File.WriteAllTextAsync(costPath, JsonSerializer.Serialize(new
{
    startedUtc = started,
    finishedUtc = DateTime.UtcNow,
    generated = result.Generated,
    failed = result.Failed,
    failures = result.Failures,
    usage = new { tracker.PromptTokens, tracker.CompletionTokens, tracker.Calls },
    costUsd = cost
}, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine(exportPath);
Console.WriteLine($"generated={result.Generated} failed={result.Failed} usd={cost:F4}");
return result.Failed > 0 && result.Generated == 0 ? 3 : 0;

file sealed class TrialManifest
{
    public List<TrialOcc> Occupations { get; set; } = [];
}

file sealed class TrialOcc
{
    public string EscoId { get; set; } = "";
}

file sealed class EnvCredentials(string apiKey) : IIntegrationCredentialService
{
    public Task<string?> GetRawApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(key == IntegrationKey.OpenAI ? apiKey : null);

    public Task<IntegrationCredentialView?> GetAsync(IntegrationKey key, CancellationToken cancellationToken = default)
        => Task.FromResult<IntegrationCredentialView?>(null);

    public Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<IntegrationCredentialView>>([]);

    public Task<IntegrationCredentialView> UpsertAsync(IntegrationKey key, IntegrationCredentialUpdate update, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task SavePingResultAsync(IntegrationKey key, bool ok, string message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task<IntegrationCredentialSecrets?> GetSecretsAsync(IntegrationKey key, CancellationToken cancellationToken = default)
        => Task.FromResult<IntegrationCredentialSecrets?>(null);
}

file sealed class UsageTrackingWriter(OccupationDayInLifeOpenAiWriter inner) : IOccupationDayInLifeWriter
{
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }
    public int Calls { get; private set; }

    public async Task<OccupationDayWriteResult> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        Calls++;
        var result = await inner.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
        PromptTokens += result.PromptTokens;
        CompletionTokens += result.CompletionTokens;
        return result;
    }
}
