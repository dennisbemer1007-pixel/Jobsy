using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/mock-interview")]
public sealed class MockInterviewController : ControllerBase
{
    public const string ChatDisclaimer =
        "Dit is een oefengesprek met AI/scripted hulp — geen echt gesprek met de werkgever. " +
        "Geen toezegging van werk. Deel geen BSN, bankgegevens of wachtwoorden. " +
        "Zie de gebruiksvoorwaarden (chatbot-disclaimer).";

    private readonly JobsyDbContext _db;
    private readonly IMockInterviewService _interviews;

    public MockInterviewController(JobsyDbContext db, IMockInterviewService interviews)
    {
        _db = db;
        _interviews = interviews;
    }

    /// <summary>
    /// Continues a vacancy-specific practice interview. Candidates only (OpenAI cost control).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<MockInterviewResponseDto>> Continue(
        [FromBody] MockInterviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VacancyId == Guid.Empty)
        {
            return BadRequest(new { message = "Vacature ontbreekt." });
        }

        if (request.Messages.Count > MockInterviewService.MaxHistoryMessages)
        {
            return BadRequest(new { message = "Het gesprek is te lang. Start een nieuw oefengesprek." });
        }

        foreach (var msg in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(msg.Content))
            {
                return BadRequest(new { message = "Lege berichten zijn niet toegestaan." });
            }

            if (msg.Content.Length > MockInterviewService.MaxMessageChars)
            {
                return BadRequest(new { message = $"Een bericht mag max. {MockInterviewService.MaxMessageChars} tekens zijn." });
            }
        }

        var vacancy = await _db.Vacancies
            .AsNoTracking()
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == request.VacancyId, cancellationToken);

        if (vacancy is null)
        {
            return NotFound(new { message = "Vacature niet gevonden." });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!VacancyVisibilityRules.IsPubliclyVisible(vacancy, today))
        {
            return NotFound(new { message = "Vacature niet gevonden." });
        }

        var context = new MockInterviewVacancyContext(
            vacancy.Id,
            vacancy.Title,
            vacancy.Description,
            vacancy.Company.Name,
            vacancy.Company.Address,
            vacancy.StartDate,
            TransportLabels.Expand(vacancy.RequiredTransport),
            HourlyWage: null,
            WorkTypeLabels.ResolveLabels(vacancy.WorkTypes, vacancy.WorkTypeLabels));

        var history = request.Messages
            .Select(m => new MockInterviewMessage(m.Role, m.Content))
            .ToList();

        var result = await _interviews.ContinueAsync(context, history, cancellationToken);
        return Ok(new MockInterviewResponseDto(result.Reply, result.UsedAi, ChatDisclaimer));
    }
}
