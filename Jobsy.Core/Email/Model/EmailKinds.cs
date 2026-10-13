using Jobsy.Core.Localization;

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
    public static EmailCulture Nl { get; } = ForLanguage(JobsyLanguages.Default);
    public static EmailCulture En { get; } = ForLanguage("en");
    public static EmailCulture Pl { get; } = ForLanguage("pl");
    public static EmailCulture Ro { get; } = ForLanguage("ro");
    public static EmailCulture Ar { get; } = ForLanguage("ar");

    public static EmailCulture ForLanguage(string? language)
    {
        var lang = JobsyLanguages.Normalize(language);
        var opt = JobsyLanguages.Get(lang);
        return new EmailCulture(opt.Code, opt.IsRightToLeft);
    }
}

public sealed record EmailEyebrow(string Text, EmailTone Tone = EmailTone.Peach);

public sealed record EmailCta(string Label, string AbsoluteUrl);

public sealed record EmailLink(string Label, string AbsoluteUrl);
