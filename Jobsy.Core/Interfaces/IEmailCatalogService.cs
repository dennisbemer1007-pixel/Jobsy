using Jobsy.Core.Email;

namespace Jobsy.Core.Interfaces;

public interface IEmailCatalogService
{
    IReadOnlyList<EmailTemplateInfo> ListTemplates();

    Task<EmailCatalogSendResult> SendAsync(
        string key,
        string to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailCatalogSendResult>> SendAllAsync(
        string to,
        CancellationToken cancellationToken = default);
}

public sealed record EmailCatalogSendResult(
    string Key,
    string Title,
    string Category,
    string Subject,
    bool Ok,
    bool DeliveredViaProvider,
    string Message);
