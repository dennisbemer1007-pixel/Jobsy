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

    public async Task<OccupationDayGenerateResult?> GenerateOccupationDaysAsync(int limit, string? ids = null, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            "api/admin/occupation-day-in-life/generate",
            new OccupationDayRunRequest { Limit = limit, Ids = ids },
            ct);
        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<OccupationDayGenerateResult>(response.Content, ct);
    }

    public async Task<OccupationDayStartResult?> StartOccupationDayBatchAsync(int? limit = null, string? ids = null, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            "api/admin/occupation-day-in-life/start",
            new OccupationDayRunRequest { Limit = limit, Ids = ids },
            ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new OccupationDayStartResult { Busy = true, Message = "Er loopt al een vulling." };
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await ReadApiJsonAsync<OccupationDayProblem>(response.Content, ct);
            return new OccupationDayStartResult { Message = problem?.Message };
        }

        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<OccupationDayStartResult>(response.Content, ct);
    }

    public async Task<OccupationDayProbeResult?> ProbeOccupationDayAsync(string query, CancellationToken ct = default)
    {
        var response = await PostApiJsonAsync(
            "api/admin/occupation-day-in-life/probe",
            new OccupationDayRunRequest { Ids = query },
            ct);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await ReadApiJsonAsync<OccupationDayProblem>(response.Content, ct);
            return new OccupationDayProbeResult { Error = problem?.Message };
        }

        response.EnsureSuccessStatusCode();
        return await ReadApiJsonAsync<OccupationDayProbeResult>(response.Content, ct);
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

public sealed class OccupationDayRunRequest
{
    public int? Limit { get; set; }
    public string? Ids { get; set; }
}

public sealed class OccupationDayStartResult
{
    public bool Started { get; set; }
    public bool Busy { get; set; }
    public string? Message { get; set; }
    public List<string> Titles { get; set; } = [];
}

public sealed class OccupationDayProbeResult
{
    public bool Ok { get; set; }
    public string? EscoId { get; set; }
    public string? TitleNl { get; set; }
    public string? Error { get; set; }
    public string? Model { get; set; }
}

public sealed class OccupationDayProblem
{
    public string? Message { get; set; }
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
