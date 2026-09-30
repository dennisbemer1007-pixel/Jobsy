namespace Jobsy.Core.Email.Model;

public enum EmailKind
{
    Essential,
    Optional,
    Security
}

public enum EmailTone
{
    Peach,
    Sun,
    Sky,
    Mint
}

public enum EmailRenderMode
{
    Normal,
    PreviewDark
}

public sealed record EmailCulture(string Language, bool IsRightToLeft)
{
    public static EmailCulture Nl { get; } = new("nl", false);
    public static EmailCulture Ar { get; } = new("ar", true);

    public static EmailCulture ForLanguage(string? language)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "nl" : language.Trim().ToLowerInvariant();
        return new EmailCulture(lang, lang is "ar");
    }
}

public sealed record EmailEyebrow(string Text, EmailTone Tone = EmailTone.Peach);

public sealed record EmailCta(string Label, string AbsoluteUrl);

public sealed record EmailLink(string Label, string AbsoluteUrl);
