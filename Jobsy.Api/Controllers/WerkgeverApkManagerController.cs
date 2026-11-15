using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Infrastructure.WerkgeverApk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/werkgever-apk/manager")]
[WerkgeverApkFeatureGate]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class WerkgeverApkManagerController : ControllerBase
{
    private readonly IWerkgeverApkManagerPortalService _portal;

    public WerkgeverApkManagerController(IWerkgeverApkManagerPortalService portal) => _portal = portal;

    private async Task<IActionResult?> EnsureEnterpriseManagerAsync(CancellationToken ct)
    {
        if (!User.IsInRole(JobsyRoles.EnterpriseManager) && !User.IsInRole(JobsyRoles.Admin))
        {
            return Forbid();
        }

        var (_, error, status) = await _portal.ResolveManagerCompanyAsync(User, ct);
        return error is not null ? StatusCode(status, error) : null;
    }

    [HttpGet("teams")]
    public async Task<IActionResult> Teams(CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.ListTeamsAsync(User, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam([FromBody] WerkgeverApkTeamCreateRequest request, CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.CreateTeamAsync(User, request, ct);
        return ok is null ? StatusCode(status, error) : StatusCode(status, ok);
    }

    [HttpPut("teams/{teamId:guid}")]
    public async Task<IActionResult> RenameTeam(
        Guid teamId,
        [FromBody] WerkgeverApkTeamCreateRequest request,
        CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.RenameTeamAsync(User, teamId, request, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpDelete("teams/{teamId:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid teamId, CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.DeleteTeamAsync(User, teamId, ct);
        return ok ? Ok() : StatusCode(status, error);
    }

    [HttpPost("teams/{teamId:guid}/codes")]
    public async Task<IActionResult> GenerateCodes(
        Guid teamId,
        [FromBody] WerkgeverApkGenerateCodesRequest request,
        CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.GenerateCodesAsync(User, teamId, request, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("teams/{teamId:guid}/codes")]
    public async Task<IActionResult> ListCodes(Guid teamId, CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.ListCodesAsync(User, teamId, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpDelete("codes/{codeId:guid}")]
    public async Task<IActionResult> RevokeCode(Guid codeId, CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.RevokeCodeAsync(User, codeId, ct);
        return ok ? Ok() : StatusCode(status, error);
    }

    [HttpGet("teams/{teamId:guid}/overview")]
    public async Task<IActionResult> TeamOverview(Guid teamId, CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.GetTeamOverviewAsync(User, teamId, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("codes/{codeId:guid}/label")]
    public async Task<IActionResult> SetLabel(
        Guid codeId,
        [FromBody] WerkgeverApkUpdateEmployeeLabelRequest request,
        CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.SetCodeLabelAsync(User, codeId, request, ct);
        return ok ? Ok() : StatusCode(status, error);
    }

    [HttpGet("questions")]
    public async Task<IActionResult> Questions(CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.ListManagerQuestionsAsync(ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("codes/{codeId:guid}/sheet")]
    public async Task<IActionResult> Sheet(Guid codeId, CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.GetManagerSheetAsync(User, codeId, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpPut("codes/{codeId:guid}/answers")]
    public async Task<IActionResult> SaveAnswer(
        Guid codeId,
        [FromBody] WerkgeverApkManagerAnswerRequest request,
        CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.SaveManagerAnswerAsync(User, codeId, request, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }

    [HttpGet("teams/{teamId:guid}/insights")]
    public async Task<IActionResult> Insights(Guid teamId, CancellationToken ct)
    {
        var gate = await EnsureEnterpriseManagerAsync(ct);
        if (gate is not null)
        {
            return gate;
        }

        var (ok, error, status) = await _portal.GetGroupInsightsAsync(User, teamId, ct);
        return ok is null ? StatusCode(status, error) : Ok(ok);
    }
}