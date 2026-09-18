namespace Jobsy.Web.Models;

public sealed class CandidateCompetencyState
{
    public string Status { get; set; } = "Draft";
    public Dictionary<string, int> Answers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int AnsweredCount { get; set; }
    public int QuestionCount { get; set; } = 20;
    public CompetencyScoreSet? Scores { get; set; }
    public CompetencyScoreSet? PreviewScores { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<CompetencyQuestionItem> Questions { get; set; } = [];
}

public sealed class CompetencyScoreSet
{
    public int? Samenwerken { get; set; }
    public int? Resultaatgerichtheid { get; set; }
    public int? Stressbestendigheid { get; set; }
    public int? Innovatie { get; set; }

    public bool IsComplete =>
        Samenwerken is not null
        && Resultaatgerichtheid is not null
        && Stressbestendigheid is not null
        && Innovatie is not null;
}

public sealed class CompetencyQuestionItem
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public bool Reverse { get; set; }
    public string TextKey { get; set; } = "";
}

public sealed class CandidateMatchedVacancy
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public int MatchPercent { get; set; }
    public string ColorBand { get; set; } = "orange";
    public List<string> Why { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
}
