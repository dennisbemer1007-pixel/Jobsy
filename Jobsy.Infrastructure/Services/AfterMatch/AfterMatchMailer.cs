using Jobsy.Core;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.AfterMatch;

public sealed class AfterMatchMailer
{
    private readonly ITransactionalMailer _mailer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AfterMatchMailer> _logger;

    public AfterMatchMailer(
        ITransactionalMailer mailer,
        IConfiguration configuration,
        ILogger<AfterMatchMailer> logger)
    {
        _mailer = mailer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendScheduledConfirmationAsync(
        Interview interview,
        Application application,
        string candidateEmail,
        string employerEmail,
        CancellationToken cancellationToken)
    {
        if (interview.ChosenSlot is null)
        {
            return;
        }

        var webBase = JobsyPublicUrl.NormalizeOrigin(
            _configuration["PublicWebBaseUrl"] ?? "http://localhost:5201");
        var reschedule = $"{webBase}/candidate/applications/{application.Id}/after-match?panel=reschedule";
        var startLocal = interview.ChosenSlot.StartUtc.ToLocalTime();
        var mail = TransactionalEmails.AfterMatchInterviewScheduled(
            webBase,
            application.CandidateName,
            application.Vacancy.Company.Name,
            application.Vacancy.Title,
            startLocal,
            reschedule);

        await TrySendAsync(candidateEmail, mail, interview.Id, cancellationToken);
        await TrySendAsync(employerEmail, mail, interview.Id, cancellationToken);
    }

    private async Task TrySendAsync(string to, ComposedEmail mail, Guid interviewId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return;
        }

        try
        {
            await _mailer.SendAsync(
                mail,
                to,
                new EmailSendOptions(IdempotencyKey: $"after-match:{interviewId:N}:{to.GetHashCode():x}"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "After-match mail failed for {Recipient}", to);
        }
    }
}
