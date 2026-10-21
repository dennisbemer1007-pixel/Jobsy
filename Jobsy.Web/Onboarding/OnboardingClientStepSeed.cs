namespace Jobsy.Web.Onboarding;

/// <summary>Server-provided seed data for client-only onboarding steps (render once, no circuit input).</summary>
public sealed class OnboardingClientStepSeed
{
    public IReadOnlyList<MiniQuestionSeed> MiniQuestions { get; init; } = [];
    public bool HasConsent { get; init; }
    public bool ConsentRequested { get; init; }
    public IReadOnlyList<string> DreamTitles { get; init; } = [];
    public string? DreamCurrent { get; init; }
    public bool NoWorkExperience { get; init; }
    public IReadOnlyList<JobSeed> Jobs { get; init; } = [];
    public string? Education { get; init; }
    public IReadOnlyDictionary<int, int> MiniAnswers { get; init; } = new Dictionary<int, int>();

    public sealed record MiniQuestionSeed(int Id, string Text);

    public sealed record JobSeed(string Employer, string? StartMonth, string? EndMonth, bool IsCurrent);
}
