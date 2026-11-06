using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

/// <summary>Who a mail is for — used to pick the recipient language.</summary>
public abstract record EmailRecipient
{
    public sealed record User(Guid UserId) : EmailRecipient;

    /// <summary>Requester language from X-Jobsy-Language (anonymous OTP / registration / takeover / API contact).</summary>
    public sealed record Requester(string? Language) : EmailRecipient;

    /// <summary>Parent/guardian receives mail in the child's language (README D11).</summary>
    public sealed record ParentOf(Guid ChildUserId) : EmailRecipient;

    public sealed record Address(string Email) : EmailRecipient;
}

public interface IEmailLanguageResolver
{
    Task<EmailCulture> ResolveAsync(EmailRecipient recipient, CancellationToken cancellationToken = default);
}
