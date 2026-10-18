using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Interfaces;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<ContextualTipsStateDto?> GetContextualTipsStateAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/me/contextual-tips", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContextualTipsStateDto>(ApiJson, ct);
    }

    public async Task<ContextualTipsStateDto?> HideContextualTipAsync(string tipId, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/me/contextual-tips/hide", new { tipId }, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContextualTipsStateDto>(ApiJson, ct);
    }

    public async Task<ContextualTipsStateDto?> RestoreHiddenContextualTipsAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/me/contextual-tips/restore-hidden", null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContextualTipsStateDto>(ApiJson, ct);
    }

    public async Task<ContextualTipsStateDto?> UpdateContextualTipsSettingsAsync(
        UpdateContextualTipsSettingsRequest body,
        CancellationToken ct = default)
    {
        var response = await PutApiJsonAsync("api/me/contextual-tips/settings", body, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContextualTipsStateDto>(ApiJson, ct);
    }
}
