using System.Net.Http.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task<EmployerPhase2ApplicationContextItem?> GetEmployerPhase2ContextAsync(
        Guid applicationId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<EmployerPhase2ApplicationContextItem>(
            $"api/employer-phase2/applications/{applicationId:D}/context",
            ct);

    public async Task<EmployerPhase2AcceptResult?> AcceptEmployerPhase2Async(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/employer-phase2/applications/{applicationId:D}/accept",
            content: null,
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployerPhase2AcceptResult>(cancellationToken: ct);
    }

    public async Task<EmployerPhase2PlacementResult?> SetEmployerPhase2EmploymentModeAsync(
        Guid applicationId,
        PlacementEmploymentMode mode,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/employer-phase2/applications/{applicationId:D}/employment-mode",
            new { mode },
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployerPhase2PlacementResult>(cancellationToken: ct);
    }

    public async Task<EmployerPhase2WalletDto?> GetEmployerPhase2WalletAsync(
        Guid companyId,
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<EmployerPhase2WalletDto>(
            $"api/employer-phase2/wallet?companyId={companyId:D}",
            ct);

    public async Task<bool> GetMaqqieHoursActiveAsync(CancellationToken ct = default)
    {
        var dto = await _http.GetFromJsonAsync<MaqqieActiveItem>("api/maqqie-hours/active", ct);
        return dto?.Active == true;
    }

    public async Task<MaqqieHoursOverviewDto?> GetMaqqieHoursOverviewAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<MaqqieHoursOverviewDto>("api/maqqie-hours/overview", ct);

    public async Task<MaqqieHoursWeekDto?> UpsertMaqqieHoursWeekAsync(
        DateOnly weekStart,
        IReadOnlyDictionary<int, decimal> dailyHours,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            "api/maqqie-hours/weeks",
            new { weekStart, dailyHours },
            ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MaqqieHoursWeekDto>(cancellationToken: ct);
    }

    public async Task SubmitMaqqieHoursWeekAsync(Guid weekId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/maqqie-hours/weeks/{weekId:D}/submit", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveMaqqieHoursWeekAsync(
        Guid applicationId,
        Guid weekId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/maqqie-hours/applications/{applicationId:D}/weeks/{weekId:D}/approve",
            null,
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReturnMaqqieHoursWeekAsync(
        Guid applicationId,
        Guid weekId,
        string note,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/maqqie-hours/applications/{applicationId:D}/weeks/{weekId:D}/return",
            new { note },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> GetMaqqieHoursEmployerActiveAsync(CancellationToken ct = default)
    {
        var dto = await _http.GetFromJsonAsync<MaqqieEmployerActiveItem>("api/maqqie-hours/employer/active", ct);
        return dto?.Active == true;
    }

    public async Task<MaqqieHoursEmployerOverviewDto?> GetMaqqieHoursEmployerOverviewAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<MaqqieHoursEmployerOverviewDto>("api/maqqie-hours/employer/overview", ct);
}

public sealed class EmployerPhase2ApplicationContextItem
{
    public Guid ApplicationId { get; set; }
    public ApplicationStatus Status { get; set; }
    public bool IsStaffingAgencyVacancy { get; set; }
    public bool HasPlacement { get; set; }
    public PlacementEmploymentMode? EmploymentMode { get; set; }
    public decimal? AcceptCostTokens { get; set; }
    public List<EmployerPhase2FactItem> Facts { get; set; } = [];
}

public sealed class EmployerPhase2FactItem
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public bool FitsWell { get; set; }
}

public sealed class MaqqieActiveItem
{
    public bool Active { get; set; }
}

public sealed class MaqqieEmployerActiveItem
{
    public bool Active { get; set; }
}
