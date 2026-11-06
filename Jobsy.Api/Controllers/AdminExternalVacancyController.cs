using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/external-vacancy-metrics")]
[Authorize(Roles = JobsyRoles.Admin)]
public sealed class AdminExternalVacancyController : ControllerBase
{
    private readonly ICandidateExternalVacancyService _service;

    public AdminExternalVacancyController(ICandidateExternalVacancyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
        => Ok(await _service.GetAdminMetricsAsync(cancellationToken));
}
