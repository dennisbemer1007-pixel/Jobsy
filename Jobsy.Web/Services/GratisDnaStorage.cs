using System.Text.Json;
using Jobsy.Core.Rules;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

/// <summary>JS interop for <c>jobsy.gratisDna.v1</c> browser storage.</summary>
public sealed class GratisDnaStorage(IJSRuntime js)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private bool _moduleReady;

    public async Task EnsureModuleAsync()
    {
        if (_moduleReady)
        {
            return;
        }

        await js.InvokeVoidAsync("jobsyEnsureGratisDna");
        _moduleReady = true;
    }

    public async Task<GratisDnaStoragePayload?> LoadAsync()
    {
        await EnsureModuleAsync();
        var raw = await js.InvokeAsync<string?>("jobsyGratisDna.load");
        return GratisDnaStorageValidator.TryParseAndValidate(raw, DateTime.UtcNow);
    }

    public async Task SaveAsync(GratisDnaStoragePayload payload)
    {
        await EnsureModuleAsync();
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await js.InvokeVoidAsync("jobsyGratisDna.save", json);
    }

    public async Task ClearAsync()
    {
        await EnsureModuleAsync();
        await js.InvokeVoidAsync("jobsyGratisDna.clear");
    }

    public Task<GratisDnaStoragePayload> CreateEmptyAsync(string consentVersion)
    {
        var now = DateTime.UtcNow;
        return Task.FromResult(new GratisDnaStoragePayload
        {
            V = GratisDnaStoragePayload.SchemaVersion,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(GratisDnaStoragePayload.RetentionDays),
            AgeBand = GratisDnaStoragePayload.AgeBand16Plus,
            Consent = new GratisDnaStoredConsent
            {
                Version = consentVersion,
                AtUtc = now
            },
            Answers = new GratisDnaStoredAnswers()
        });
    }
}
