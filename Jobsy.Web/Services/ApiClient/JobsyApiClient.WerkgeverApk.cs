using Jobsy.Core.Contracts.WerkgeverApk;

namespace Jobsy.Web.Services;

public sealed partial class JobsyApiClient
{
    public async Task WerkgeverApkEmployeeWelcomeAsync(CancellationToken ct = default)
        => await _http.PostAsync("api/werkgever-apk/employee/welcome", null, ct);

    public async Task<IReadOnlyList<WerkgeverApkEmployeeCompanyOptionDto>> GetWerkgeverApkLoginOptionsAsync(
        CancellationToken ct = default)
        => await _http.GetFromJsonAsync<IReadOnlyList<WerkgeverApkEmployeeCompanyOptionDto>>(
               "api/werkgever-apk/employee/login-options", ct)
           ?? [];

    public async Task<IReadOnlyList<WerkgeverApkTeamDto>> GetWerkgeverApkTeamsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<IReadOnlyList<WerkgeverApkTeamDto>>(
               "api/werkgever-apk/manager/teams", ct)
           ?? [];

    public async Task UpdateCompanyLoopbaanApkAsync(Guid companyId, bool enabled, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/companies/{companyId:D}/loopbaan-apk",
            new UpdateLoopbaanApkRequest(enabled),
            ct);
        response.EnsureSuccessStatusCode();
    }
}