namespace Jobsy.Core.Passport;

/// <summary>Display model for the 2-page DNA-paspoort. Words and facts only: no scores, no date of birth.</summary>
public sealed record PassportPdfModel(
    string Language,
    string FullName,
    string Initials,
    string? Tagline,
    string? Region,
    bool OpenForWork,
    string? PassportNumber,
    string? AvailableFrom,
    string? Hours,
    IReadOnlyList<PassportShiftChip> Shifts,
    string? ShiftNote,
    string? TransportLine,
    string? TravelLine,
    string? OwnCarLine,
    string? Email,
    string? Phone,
    bool WhatsApp,
    IReadOnlyList<PassportTextLine> Languages,
    string? DutchNote,
    IReadOnlyList<PassportTextLine> WorkPreferences,
    IReadOnlyList<string> Strengths,
    string? StrengthSource,
    string? SoughtLine,
    IReadOnlyList<PassportExperienceLine> Experience,
    int ExperienceMore,
    IReadOnlyList<string> Certificates,
    int CertificatesMore,
    IReadOnlyList<string> Educations,
    IReadOnlyList<PassportDnaCard> Dna,
    IReadOnlyList<string> HowIWork,
    string? OwnWords,
    string? Motivation,
    bool ShowBadge,
    string? BadgeDetail,
    string TestsPlainStatus,
    IReadOnlyList<string> Checked,
    IReadOnlyList<string> NotChecked,
    string GeneratedLabel,
    DateTime GeneratedAtUtc,
    string? Story,
    string? HeroMeta,
    IReadOnlyList<PassportChip> Chips,
    IReadOnlyList<PassportGlossLine> Traits,
    IReadOnlyList<string> HomeLines,
    IReadOnlyList<PassportGlossLine> ValueLines,
    IReadOnlyList<PassportJobFit> JobFits,
    string? PracticalLine,
    string? PracticalSeek,
    IReadOnlyList<PassportReferenceQuote> Quotes,
    IReadOnlyList<PassportDirectionStep> Direction);

public sealed record PassportChip(string Text, string Tone);

public sealed record PassportGlossLine(string Label, string Gloss);

public sealed record PassportJobFit(string Title, string Why);

public sealed record PassportReferenceQuote(string Attribution, string Quote);

public sealed record PassportDirectionStep(string Title, string State);

public sealed record PassportShiftChip(string Label, string Status, PassportShiftKind Kind);

public enum PassportShiftKind
{
    Yes,
    Consult,
    No
}

public sealed record PassportTextLine(string Label, string Value);

public sealed record PassportExperienceLine(
    string Title,
    string? Meta,
    string? Detail,
    IReadOnlyList<string>? Duties = null,
    string? Period = null,
    string? Place = null);

public sealed record PassportDnaCard(string Title, string Body, string? When, bool Present = true);

/// <summary>One DNA layer as words. Percents never leave the reader.</summary>
public sealed record PassportDnaLayerFact(
    string Key,
    bool Done,
    DateTime? CompletedAtUtc,
    IReadOnlyList<string> Words,
    IReadOnlyList<string>? Codes = null);

public static class PassportDnaLayer
{
    public const string Competence = "competence";
    public const string Career = "career";
    public const string Culture = "culture";
    public const string Values = "values";

    public static readonly string[] Keys = [Competence, Career, Culture, Values];

    public static IReadOnlyList<PassportDnaLayerFact> None()
        => Keys.Select(key => new PassportDnaLayerFact(key, false, null, [])).ToArray();
}

public sealed record PassportExperienceFact(
    string Employer,
    string? Role,
    string? StartMonth,
    string? EndMonth,
    int? Years,
    string? Description);

public sealed record PassportPaperFact(string Name, int? Year);
