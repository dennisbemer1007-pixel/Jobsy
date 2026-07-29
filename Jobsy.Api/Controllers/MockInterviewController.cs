using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
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
    private readonly JobsyDbContext _db;
    private readonly IMockInterviewService _interviews;

    public MockInterviewController(JobsyDbContext db, IMockInterviewService interviews)
    {
        _db = db;
        _interviews = interviews;
    }

    /// <summary>
    /// Continues a vacancy-specific practice interview. Candidates only (OpenAI cost control).
    /// Language follows X-Jobsy-Language / ?lang= (same as vacancy translation).
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

        var language = ResolveLanguage();
        var disclaimer = language switch
        {
            "en" => "This is a practice chat with AI/scripted help — not a real interview with the employer. No job offer. Do not share SSN, bank details or passwords. See the terms of use (chatbot disclaimer).",
            "pl" => "To rozmowa treningowa z pomocą AI/skryptu — nie prawdziwa rozmowa z pracodawcą. Bez obietnicy pracy. Nie podawaj PESEL, danych bankowych ani haseł. Zobacz regulamin (zastrzeżenie chatbota).",
            "ro" => "Aceasta este o conversație de exercițiu cu AI/script — nu un interviu real cu angajatorul. Fără ofertă de job. Nu partaja CNP, date bancare sau parole. Vezi termenii (disclaimer chatbot).",
            "ar" => "هذه محادثة تدريبية بمساعدة الذكاء الاصطناعي وليست مقابلة حقيقية مع صاحب العمل. لا وعد بالتوظيف. لا تشارك بيانات حساسة أو كلمات مرور. راجع شروط الاستخدام.",
            _ => "Dit is een oefengesprek met AI/scripted hulp — geen echt gesprek met de werkgever. Geen toezegging van werk. Deel geen BSN, bankgegevens of wachtwoorden. Zie de gebruiksvoorwaarden (chatbot-disclaimer)."
        };

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

        var result = await _interviews.ContinueAsync(context, history, language, cancellationToken);
        return Ok(new MockInterviewResponseDto(result.Reply, result.UsedAi, disclaimer));
    }

    private string ResolveLanguage()
    {
        if (Request.Query.TryGetValue("lang", out var langQuery) && JobsyLanguages.IsSupported(langQuery.ToString()))
        {
            return JobsyLanguages.Normalize(langQuery.ToString());
        }

        if (Request.Headers.TryGetValue("X-Jobsy-Language", out var langHeader)
            && JobsyLanguages.IsSupported(langHeader.ToString()))
        {
            return JobsyLanguages.Normalize(langHeader.ToString());
        }

        return JobsyLanguages.Default;
    }
}
