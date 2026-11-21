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
var replace = false;
var model = "gpt-4o";
var manifestPath = Path.Combine(root, "docs/mockups/leerling/werelden/data/trial-20-manifest.json");
var outDir = Path.Combine(root, "docs/mockups/leerling/werelden/data");
var exportFileName = "occupation-day-in-life.nl.json";
var costFileName = "trial-20-cost.json";

var estimateOnly = false;
for (var i = 0; i < args.Length; i++)
{
    var a = args[i];
    if (string.Equals(a, "--estimate-only", StringComparison.OrdinalIgnoreCase))
    {
        estimateOnly = true;
        continue;
    }

    if (string.Equals(a, "--replace", StringComparison.OrdinalIgnoreCase))
    {
        replace = true;
        continue;
    }

    if (string.Equals(a, "--model", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        model = args[++i].Trim();
        continue;
    }

    if (string.Equals(a, "--manifest", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        manifestPath = Path.GetFullPath(args[++i]);
        continue;
    }

    if (string.Equals(a, "--out-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        outDir = Path.GetFullPath(args[++i]);
        continue;
    }

    if (string.Equals(a, "--export-out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        exportFileName = args[++i];
        continue;
    }

    if (string.Equals(a, "--cost-out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        costFileName = args[++i];
        continue;
    }

    if (a.StartsWith('-'))
    {
        Console.Error.WriteLine("Unknown option: " + a);
        return 1;
    }

    manifestPath = Path.GetFullPath(a);
}

Directory.CreateDirectory(outDir);
var exportPath = Path.GetFullPath(Path.IsPathRooted(exportFileName) ? exportFileName : Path.Combine(outDir, exportFileName));
var costPath = Path.GetFullPath(Path.IsPathRooted(costFileName) ? costFileName : Path.Combine(outDir, costFileName));

if (estimateOnly)
{
    var g4o = OccupationDayBatchPlan.EstimateCatalog("gpt-4o");
    var mini = OccupationDayBatchPlan.EstimateCatalog("gpt-4o-mini");
    var payload = new
    {
        catalogOccupations = OccupationDayBatchPlan.CatalogTotal,
        gpt4o = new { g4o.OpenAiCalls, g4o.InputTokens, g4o.OutputTokens, g4o.EstimatedUsd },
        gpt4oMini = new { mini.OpenAiCalls, mini.InputTokens, mini.OutputTokens, mini.EstimatedUsd },
        grokSkipped = "No xAI/Grok integration in Jobsy."
    };
    await File.WriteAllTextAsync(costPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}

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
    Console.Error.WriteLine("Set OPENAI_API_KEY or OpenAI__ApiKey. Grok/xAI is not wired in Jobsy integrations (skipped).");
    return 2;
}

var dbName = "trial-" + Guid.NewGuid().ToString("N");
var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole().SetMinimumLevel(LogLevel.Warning));
services.AddHttpClient(OccupationDayInLifeOpenAiWriter.HttpClientName);
services.AddSingleton<IIntegrationCredentialService>(_ => new EnvCredentials(apiKey, model));
services.AddSingleton<IOptions<OpenAiOptions>>(_ => Options.Create(new OpenAiOptions { ApiKey = apiKey, Model = model }));
services.AddSingleton<IOptions<OccupationDayInLifeOptions>>(_ => Options.Create(new OccupationDayInLifeOptions
{
    Model = model,
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

var costUsd = OccupationDayBatchPlan.EstimateFromUsage(tracker.PromptTokens, tracker.CompletionTokens, model);
var catalog3039 = OccupationDayBatchPlan.EstimateCatalog(model);
await File.WriteAllTextAsync(costPath, JsonSerializer.Serialize(new
{
    model,
    startedUtc = started,
    finishedUtc = DateTime.UtcNow,
    generated = result.Generated,
    failed = result.Failed,
    failures = result.Failures,
    usage = new { tracker.PromptTokens, tracker.CompletionTokens, tracker.Calls },
    costUsd,
    projectedCatalog3039Usd = catalog3039.EstimatedUsd,
    projectedCatalog3039Calls = catalog3039.OpenAiCalls,
    grokSkipped = "No IntegrationKey for xAI Grok in Jobsy; Acceptatie Integrations tile is OpenAI-only."
}, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine(exportPath);
Console.WriteLine($"model={model} generated={result.Generated} failed={result.Failed} usd={costUsd:F4} projected3039={catalog3039.EstimatedUsd:F2}");
return result.Failed > 0 && result.Generated == 0 ? 3 : 0;

file sealed class TrialManifest
{
    public List<TrialOcc> Occupations { get; set; } = [];
}

file sealed class TrialOcc
{
    public string EscoId { get; set; } = "";
}

file sealed class EnvCredentials(string apiKey, string? model) : IIntegrationCredentialService
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
        => Task.FromResult<string?>(key == IntegrationKey.OpenAI ? model : null);

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
