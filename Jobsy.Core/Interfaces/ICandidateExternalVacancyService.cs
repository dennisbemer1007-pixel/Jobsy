using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ICandidateExternalVacancyService
{
    Task<ExternalVacancyDetailDto> ImportAsync(Guid candidateUserId, string url, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalVacancyListItemDto>> ListAsync(Guid candidateUserId, CancellationToken cancellationToken = default);

    Task<ExternalVacancyDetailDto?> GetDetailAsync(Guid candidateUserId, Guid id, CancellationToken cancellationToken = default);

    Task<ExternalVacancyApplyResultDto> ApplyAsync(
        Guid candidateUserId,
        Guid id,
        ExternalVacancyApplyRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]?> BuildApplicationLetterPdfAsync(
        Guid candidateUserId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ExternalVacancyAdminMetricsDto> GetAdminMetricsAsync(CancellationToken cancellationToken = default);
}

public interface IExternalVacancyUrlFetchService
{
    Task<(string Html, string VisibleText)?> FetchAsync(Uri url, CancellationToken cancellationToken = default);
}

public interface IExternalVacancyExtractionService
{
    Task<ExternalVacancyExtractionResult?> ExtractAsync(string visibleText, CancellationToken cancellationToken = default);
}

public interface IExternalVacancyMatchService
{
    Task<ExternalVacancyMatchInsightsDto> BuildMatchInsightsAsync(
        Guid candidateUserId,
        IReadOnlyDictionary<string, string> structuredFacts,
        CancellationToken cancellationToken = default);
}

public interface IExternalVacancyContactFinder
{
    Task<IReadOnlyList<string>> FindEmployerEmailsAsync(Uri vacancyUrl, string companyName, CancellationToken cancellationToken = default);
}

public interface IExternalVacancyEmployerInviteService
{
    Task<ExternalVacancyEmployerInviteDto?> ResolveInviteAsync(string token, CancellationToken cancellationToken = default);
}

public interface IExternalVacancyApplicationLetterPdfBuilder
{
    byte[] Build(
        string candidateName,
        string vacancyTitle,
        string companyName,
        string motivation,
        IReadOnlyList<(string Label, string Value)> sharedFacts);
}

public interface IExternalVacancySuppressionService
{
    Task<bool> IsSuppressedAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task SuppressAsync(string normalizedEmail, string reason, CancellationToken cancellationToken = default);
    string CreateUnsubscribeToken(string normalizedEmail);
    bool TryValidateUnsubscribeToken(string? token, out string normalizedEmail);
}
