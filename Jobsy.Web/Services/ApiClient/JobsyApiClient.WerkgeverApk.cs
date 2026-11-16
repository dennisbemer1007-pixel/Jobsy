using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task WerkgeverApkEmployeeWelcomeAsync(CancellationToken ct = default)
        => await _http.PostAsync("api/werkgever-apk/employee/welcome", null, ct);

    public async Task<WerkgeverApkEmployeeQuestionDto?> GetWerkgeverApkAbQuestionAsync(
        string? itemId = null,
        CancellationToken ct = default)
    {
        var path = "api/werkgever-apk/employee/question";
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            path += $"?itemId={Uri.EscapeDataString(itemId)}";
        }

        var response = await _http.GetAsync(path, ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<WerkgeverApkEmployeeQuestionDto>(ApiJson, ct)
            : null;
    }

    public async Task<(WerkgeverApkEmployeeAnswerResponse? Ok, int StatusCode, string? ErrorBody)> SaveWerkgeverApkAbAnswerAsync(
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/werkgever-apk/employee/answers/{Uri.EscapeDataString(itemId)}",
            new WerkgeverApkEmployeeAnswerRequest(value),
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return (null, (int)response.StatusCode, body);
        }

        var ok = await JsonSerializerTry<WerkgeverApkEmployeeAnswerResponse>(response.Content);
        return (ok, (int)response.StatusCode, null);
    }

    public async Task<WerkgeverApkPauseDto?> GetWerkgeverApkPauseAsync(int afterBlock, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkPauseDto>(
            $"api/werkgever-apk/employee/pause?after={afterBlock}", ApiJson, ct);

    public async Task<WerkgeverApkEmployeeQuestionDto?> GetWerkgeverApkQuestionAsync(
        string blockKey,
        string? itemId = null,
        CancellationToken ct = default)
    {
        var path = $"api/werkgever-apk/employee/blocks/{Uri.EscapeDataString(blockKey)}/question";
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            path += $"?itemId={Uri.EscapeDataString(itemId)}";
        }

        var response = await _http.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<WerkgeverApkEmployeeQuestionDto>(ApiJson, ct);
    }

    public async Task<(WerkgeverApkEmployeeAnswerResponse? Ok, int StatusCode, string? ErrorBody)> SaveWerkgeverApkAnswerAsync(
        string blockKey,
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/werkgever-apk/employee/blocks/{Uri.EscapeDataString(blockKey)}/answers/{Uri.EscapeDataString(itemId)}",
            new WerkgeverApkEmployeeAnswerRequest(value),
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return (null, (int)response.StatusCode, body);
        }

        var ok = await JsonSerializerTry<WerkgeverApkEmployeeAnswerResponse>(response.Content);
        return (ok, (int)response.StatusCode, null);
    }

    public async Task<WerkgeverApkSharePreviewDto?> GetWerkgeverApkSharePreviewAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkSharePreviewDto>("api/werkgever-apk/employee/share-preview", ApiJson, ct);

    public async Task<(WerkgeverApkShareWithManagerResponse? Ok, int StatusCode, string? ErrorBody)> SetWerkgeverApkShareAsync(
        bool share,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/werkgever-apk/employee/share",
            new WerkgeverApkShareWithManagerRequest(share),
            ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return (null, (int)response.StatusCode, body);
        }

        var ok = await JsonSerializerTry<WerkgeverApkShareWithManagerResponse>(response.Content);
        return (ok, (int)response.StatusCode, null);
    }

    public async Task<WerkgeverApkEmployeeComparisonResponse?> GetWerkgeverApkComparisonAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkEmployeeComparisonResponse>(
            "api/werkgever-apk/employee/comparison", ApiJson, ct);

    public async Task<WerkgeverApkEmployeeProgressDto?> GetWerkgeverApkProgressAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkEmployeeProgressDto>("api/werkgever-apk/employee/progress", ApiJson, ct);

    public async Task<IReadOnlyList<WerkgeverApkTeamDto>> GetWerkgeverApkTeamsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<IReadOnlyList<WerkgeverApkTeamDto>>(
               "api/werkgever-apk/manager/teams", ApiJson, ct)
           ?? [];

    public async Task<WerkgeverApkTeamOverviewDto?> GetWerkgeverApkTeamOverviewAsync(Guid teamId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkTeamOverviewDto>(
            $"api/werkgever-apk/manager/teams/{teamId:D}/overview", ApiJson, ct);

    public async Task<WerkgeverApkGroupInsightsDto?> GetWerkgeverApkGroupInsightsAsync(Guid teamId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkGroupInsightsDto>(
            $"api/werkgever-apk/manager/teams/{teamId:D}/insights", ApiJson, ct);

    public async Task<IReadOnlyList<WerkgeverApkManagerQuestionDto>> GetWerkgeverApkManagerQuestionsAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<IReadOnlyList<WerkgeverApkManagerQuestionDto>>(
               "api/werkgever-apk/manager/questions", ApiJson, ct)
           ?? [];

    public async Task<WerkgeverApkManagerSheetDto?> GetWerkgeverApkManagerSheetAsync(Guid codeId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkManagerSheetDto>(
            $"api/werkgever-apk/manager/codes/{codeId:D}/sheet", ApiJson, ct);

    public async Task<WerkgeverApkEmployeeFunctieResponse?> GetWerkgeverApkFunctieAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<WerkgeverApkEmployeeFunctieResponse>(
            "api/werkgever-apk/employee/functie", ApiJson, ct);

    public async Task<WerkgeverApkFunctiePickDto?> GetWerkgeverApkFunctiePickAsync(
        Guid teamId,
        Guid codeId,
        string? query,
        CancellationToken ct = default)
    {
        var path = $"api/werkgever-apk/manager/teams/{teamId:D}/codes/{codeId:D}/functie-pick";
        if (!string.IsNullOrWhiteSpace(query))
        {
            path += $"?q={Uri.EscapeDataString(query)}";
        }

        return await _http.GetFromJsonAsync<WerkgeverApkFunctiePickDto>(path, ApiJson, ct);
    }

    public async Task<WerkgeverApkFunctieGroupInsightsDto?> GetWerkgeverApkFunctieInsightsAsync(
        Guid teamId,
        string? escoOccupationId,
        CancellationToken ct = default)
    {
        var path = $"api/werkgever-apk/manager/teams/{teamId:D}/functie-insights";
        if (!string.IsNullOrWhiteSpace(escoOccupationId))
        {
            path += $"?esco={Uri.EscapeDataString(escoOccupationId)}";
        }

        return await _http.GetFromJsonAsync<WerkgeverApkFunctieGroupInsightsDto>(path, ApiJson, ct);
    }

    public async Task<(bool Ok, int StatusCode)> SetWerkgeverApkOccupationAsync(
        Guid codeId,
        string escoOccupationId,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/werkgever-apk/manager/codes/{codeId:D}/occupation",
            new WerkgeverApkSetEmployeeOccupationRequest(escoOccupationId),
            ct);
        return (response.IsSuccessStatusCode, (int)response.StatusCode);
    }

    private static async Task<T?> JsonSerializerTry<T>(HttpContent content)
    {
        try
        {
            return await content.ReadFromJsonAsync<T>(ApiJson);
        }
        catch
        {
            return default;
        }
    }
}
