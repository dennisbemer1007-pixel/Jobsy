using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize]
public sealed class AssistantController : ControllerBase
{
    private readonly IAssistantChatService _assistant;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;

    public AssistantController(
        IAssistantChatService assistant,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users)
    {
        _assistant = assistant;
        _companyAuth = companyAuth;
        _users = users;
    }

    [HttpPost("chat")]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<AssistantChatResponseDto>> Chat(
        [FromBody] AssistantChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Messages.Count > AssistantChatService.MaxHistoryMessages)
        {
            return BadRequest(new { message = "Het gesprek is te lang. Start een nieuw chatgesprek." });
        }

        foreach (var msg in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(msg.Content))
            {
                return BadRequest(new { message = "Lege berichten zijn niet toegestaan." });
            }

            if (msg.Content.Length > AssistantChatService.MaxMessageChars)
            {
                return BadRequest(new { message = $"Een bericht mag max. {AssistantChatService.MaxMessageChars} tekens zijn." });
            }
        }

        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var role = user.Role.ToString();
        // Only candidate, employer managers, admin, salesmanager get the assistant.
        var allowed =
            role is JobsyRoles.Candidate
                or JobsyRoles.SalesManager
                or JobsyRoles.Admin
            || JobsyRoles.EmployerRoles.Contains(role);
        if (!allowed)
        {
            return Forbid();
        }

        IReadOnlyCollection<Guid>? companyIds = null;
        if (JobsyRoles.EmployerRoles.Contains(role) || role == JobsyRoles.Admin)
        {
            companyIds = await _companyAuth.GetAccessibleCompanyIdsAsync(User, cancellationToken);
            if (role == JobsyRoles.Admin)
            {
                // null = platform-wide for admin metrics
                companyIds = _companyAuth.IsAdmin(User) ? null : companyIds;
            }
        }

        var language = ResolveLanguage();
        var context = new AssistantChatContext(user.Id, role, language, companyIds);
        var history = request.Messages
            .Select(m => new AssistantChatMessage(m.Role, m.Content))
            .ToList();

        var result = await _assistant.ChatAsync(context, history, cancellationToken);
        return Ok(new AssistantChatResponseDto(
            result.Reply,
            result.UsedAi,
            result.Actions.Select(a => new AssistantChatActionDto(
                a.Type, a.Url, a.WorkType, a.Count, a.Label, a.ApplicationId, a.VacancyId)).ToList()));
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
