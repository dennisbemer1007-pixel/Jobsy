using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationHealthService _health;
    private readonly IKvkUsageCounter _kvkUsage;

    public IntegrationsController(IIntegrationHealthService health, IKvkUsageCounter kvkUsage)
    {
        _health = health;
        _kvkUsage = kvkUsage;
    }

    [HttpGet("health")]
    public async Task<ActionResult<IEnumerable<IntegrationHealthResult>>> GetHealth(
        CancellationToken cancellationToken)
        => Ok(await _health.GetAllAsync(cancellationToken));

    [HttpGet("health/{key}")]
    public async Task<ActionResult<IntegrationHealthResult>> Ping(
        IntegrationKey key,
        CancellationToken cancellationToken)
        => Ok(await _health.PingAsync(key, cancellationToken));

    [HttpPost("health/{key}/test")]
    public async Task<ActionResult<IntegrationHealthResult>> Test(
        IntegrationKey key,
        CancellationToken cancellationToken)
        => Ok(await _health.TestConnectionAsync(key, cancellationToken));

    [HttpGet("kvk/usage")]
    public async Task<ActionResult<KvkUsageResponse>> GetKvkUsage(CancellationToken cancellationToken)
    {
        var summary = await _kvkUsage.GetSummaryAsync(cancellationToken);
        var profilesToday = summary.BasisprofielToday + summary.VestigingenToday;
        return Ok(new KvkUsageResponse(
            summary.ZoekenToday,
            summary.BasisprofielToday,
            summary.VestigingenToday,
            summary.ProfileCallsThisMonth,
            summary.MonthlyProfileBudget,
            summary.BudgetWarning,
            $"Vandaag: {summary.ZoekenToday} zoekopdrachten · {profilesToday} profielen"));
    }

    [HttpPost("health/{key}/send-test")]
    public async Task<ActionResult<SendTestMailResult>> SendTestMail(
        IntegrationKey key,
        [FromBody] SendTestMailRequest request,
        CancellationToken cancellationToken)
    {
        if (key != IntegrationKey.Mail)
        {
            return BadRequest(new { message = "Testmail is alleen beschikbaar voor Mail." });
        }

        var result = await _health.SendTestMailAsync(request?.To ?? string.Empty, cancellationToken);
        if (!result.Ok && !result.SentViaSmtp && result.Message.Contains("geldig", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }
}

public sealed record SendTestMailRequest(string? To);

public sealed record KvkUsageResponse(
    int ZoekenToday,
    int BasisprofielToday,
    int VestigingenToday,
    int ProfileCallsThisMonth,
    int MonthlyProfileBudget,
    bool BudgetWarning,
    string TodaySummary);
