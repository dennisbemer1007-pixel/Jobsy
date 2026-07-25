using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationHealthService _health;

    public IntegrationsController(IIntegrationHealthService health)
    {
        _health = health;
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
}
