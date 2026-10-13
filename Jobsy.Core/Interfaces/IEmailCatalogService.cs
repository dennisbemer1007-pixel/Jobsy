using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Interfaces;

public interface IEmailCatalogService
{
    IReadOnlyList<EmailTemplateListItem> ListTemplates(bool ambassadorsEnabled);

    EmailCatalogTestOptions GetTestOptions();

    EmailTemplatePreview Preview(string key, string language, string theme, string publicWebBaseUrl);

    Task<EmailCatalogSendResult> SendAsync(
        string key,
        string language,
        string adminEmail,
        string? requestedTo,
        CancellationToken cancellationToken = default);

    Task<EmailCatalogSendAllAccepted> StartSendAllAsync(
        string language,
        string adminEmail,
        string? requestedTo,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    EmailCatalogSendAllStatus? GetSendAllStatus(Guid runId);
}

public sealed record EmailCatalogTestOptions(
    IReadOnlyList<string> TestRecipientAllowList,
    int TestDailyCap);

public sealed record EmailTemplateListItem(
    string Key,
    string Title,
    string Audience,
    string Description,
    string Category,
    string Kind,
    string Reason,
    bool HasMascot,
    bool Parked,
    IReadOnlyList<string> Languages,
    bool RequiresEmployers);

public sealed record EmailTemplatePreview(
    string Key,
    string Subject,
    string Preheader,
    string Html,
    string Text,
    string Kind,
    string Language,
    string Dir,
    IReadOnlyDictionary<string, string> Headers);

public sealed record EmailCatalogSendResult(
    string Key,
    string Title,
    string Category,
    string Subject,
    bool Ok,
    bool DeliveredViaProvider,
    string Message);

public sealed record EmailCatalogSendAllAccepted(Guid RunId, int Total);

public sealed record EmailCatalogSendAllStatus(
    Guid RunId,
    int Total,
    int Sent,
    int Failed,
    bool Done,
    string? Error);
