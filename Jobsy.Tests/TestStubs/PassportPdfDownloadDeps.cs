using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

internal static class PassportPdfDownloadDeps
{
    internal static ReferenceVerificationService ReferenceVerification(JobsyDbContext db)
        => new(
            db,
            new NoopMailer(),
            new UserNotificationService(db),
            new NoopPush(),
            new NlLanguage(),
            new RefVerificationFeaturesOff(),
            new EmptyDnaReader(),
            new EmptyCompetencies());

    internal static IPlatformFeatureService FeaturesOff() => new RefVerificationFeaturesOff();

    private sealed class NoopMailer : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(false, false, null, EmailDeliveryKind.Provider));
    }

    private sealed class NoopPush : IPushNotificationService
    {
        public Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NlLanguage : IEmailLanguageResolver
    {
        public Task<EmailCulture> ResolveAsync(EmailRecipient recipient, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailCulture.Nl);
    }

    private sealed class RefVerificationFeaturesOff : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, false, "http://localhost:5201", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class EmptyCompetencies : ICandidateCompetencyService
    {
        public Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateCompetencyStateDto(
                CandidateCompetencyStatuses.Draft, new Dictionary<int, int>(), 0, 0, null, null, null, null, [], [], "", 1, 0, false));

        public Task<CandidateCompetencyStateDto> SaveAsync(
            Guid userId,
            IReadOnlyDictionary<int, int> answers,
            bool complete,
            CancellationToken cancellationToken = default)
            => GetAsync(userId, cancellationToken);

        public Task<CompetencyScores?> GetCompletedScoresAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<CompetencyScores?>(null);

        public Task<IReadOnlyList<CandidateMatchedVacancyDto>> GetTopMatchesAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CandidateMatchedVacancyDto>>([]);
    }

    private sealed class EmptyDnaReader : IPassportDnaReader
    {
        public Task<IReadOnlyList<PassportDnaLayerFact>> ReadAsync(
            Guid userId,
            string language,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PassportDnaLayerFact>>(PassportDnaLayer.None());
    }
}
