namespace Jobsy.Core.Passport;

/// <summary>Display model for HBO/student DNA-paspoort (2 pages, hogeschool mockup layout).</summary>
public sealed record StudentPassportPdfModel(
    string FullName,
    string Initials,
    string StudyLine,
    string YearLine,
    string? Tagline,
    string? Region,
    IReadOnlyList<StudentPassportDnaCard> DnaCards,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<StudentPassportVacancyCard> Vacancies,
    IReadOnlyList<StudentPassportExperienceLine> Experience,
    IReadOnlyList<string> LearningGoals,
    IReadOnlyList<StudentPassportDirectionStep> DirectionSteps,
    string? OwnWords,
    string? Motivation,
    IReadOnlyList<string> HowIWork,
    IReadOnlyList<string> Certificates,
    string FooterNote,
    bool Sparse);

public sealed record StudentPassportDnaCard(string IconKey, string Label, string Value, string? Sub);

public sealed record StudentPassportVacancyCard(
    string Title,
    string Org,
    IReadOnlyList<string> MetaPills,
    IReadOnlyList<string> Bullets);

public sealed record StudentPassportExperienceLine(string Title, string Period, string? Detail);

public sealed record StudentPassportDirectionStep(int Number, string Title, string Detail, bool IsGoal);
