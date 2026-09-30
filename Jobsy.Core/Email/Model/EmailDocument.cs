namespace Jobsy.Core.Email.Model;

/// <summary>Immutable block-model document for one transactional mail.</summary>
public sealed record EmailDocument(
    string TemplateKey,
    EmailKind Kind,
    EmailCulture Culture,
    string Subject,
    string Preheader,
    string Heading,
    IReadOnlyList<EmailBlock> Blocks,
    string ReasonText,
    string SignOff,
    EmailEyebrow? Eyebrow = null,
    string? Greeting = null,
    EmailCta? Cta = null,
    bool ShowMascot = false,
    /// <summary>Tokenized one-click unsubscribe URL for kind O (filled at send time when missing).</summary>
    string? UnsubscribeUrl = null);

public abstract record EmailBlock;

public sealed record ParagraphBlock(EmailText Text) : EmailBlock;

public sealed record FactsBlock(
    IReadOnlyList<(string Label, EmailText Value)> Rows,
    EmailTone Tone = EmailTone.Sky) : EmailBlock;

public sealed record StepsBlock(string Title, IReadOnlyList<EmailText> Items) : EmailBlock;

public sealed record CodeBlock(string Digits, string ValidityText) : EmailBlock;

public sealed record NoteBlock(EmailText Text, EmailLink? Link = null) : EmailBlock;
