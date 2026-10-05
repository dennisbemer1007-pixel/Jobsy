using System.Net;
using System.Text;
using Jobsy.Core.Careers;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<OccupationDayResponse?> GetOccupationDayAsync(
        Guid escoId,
        string? language = null,
        CancellationToken ct = default)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "nl" : language.Trim();
        var response = await _http.GetAsync(
            $"api/me/occupation-day-in-life/{escoId:D}?lang={Uri.EscapeDataString(lang)}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<OccupationDayResponse>(response.Content, ct);
    }

    public Task<OccupationDayAdminStatus?> GetOccupationDayStatusAsync(CancellationToken ct = default)
        => GetApiJsonAsync<OccupationDayAdminStatus>("api/admin/occupation-day-in-life/status", ct);

    public async Task<OccupationDayGenerateResult?> GenerateOccupationDaysAsync(int limit, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync($"api/admin/occupation-day-in-life/generate?limit={limit}", new { }, ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<OccupationDayGenerateResult>(response.Content, ct);
    }

    public async Task StartOccupationDayBatchAsync(int? limit = null, CancellationToken ct = default)
    {
        var path = limit is int value
            ? $"api/admin/occupation-day-in-life/start?limit={value}"
            : "api/admin/occupation-day-in-life/start";
        var response = await PostApiJsonAsync(path, new { }, ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task StopOccupationDayBatchAsync(CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync("api/admin/occupation-day-in-life/stop", new { }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DownloadOccupationDayExportAsync(IJSRuntime js, CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/admin/occupation-day-in-life/export", ct);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        await SendBrowserDownloadAsync(js, "occupation-day-in-life.nl.json", Convert.ToBase64String(bytes), "application/json");
    }

    public async Task<OccupationDayImportResult?> ImportOccupationDaysAsync(string json, CancellationToken ct = default)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("api/admin/occupation-day-in-life/import", content, ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<OccupationDayImportResult>(response.Content, ct);
    }
}

public sealed class OccupationDayResponse
{
    public bool Enabled { get; set; }
    public bool Found { get; set; }
    public string? EscoId { get; set; }
    public string? TitleNl { get; set; }
    public string? Morning { get; set; }
    public string? Midday { get; set; }
    public string? Afternoon { get; set; }
    public string? Closing { get; set; }
    public List<string> Highlights { get; set; } = [];
    public string? VariesNote { get; set; }
    public bool ThinSource { get; set; }
    public string Language { get; set; } = "nl";
    public List<OccupationDayBlock> Blocks { get; set; } = [];
    public List<string> Tasks { get; set; } = [];
    public List<string> Skills { get; set; } = [];
}
