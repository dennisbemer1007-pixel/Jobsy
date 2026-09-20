namespace Jobsy.Web.Models;

public sealed class CandidateCompetencyState
{
    public string Status { get; set; } = "Draft";
    public Dictionary<string, int> Answers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int AnsweredCount { get; set; }
    public int QuestionCount { get; set; } = 25;
    public CompetencyScoreSet? Scores { get; set; }
    public CompetencyScoreSet? PreviewScores { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<CompetencyQuestionItem> Questions { get; set; } = [];
    public List<string> MatchTags { get; set; } = [];
    public string DeepAnalysisUpsellCopy { get; set; } = "";
}

public sealed class CandidateCareerInterestState
{
    public string Status { get; set; } = "Draft";
    public Dictionary<string, int> Answers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int AnsweredCount { get; set; }
    public int QuestionCount { get; set; } = 25;
    public RiasecScoreSet? Scores { get; set; }
    public RiasecScoreSet? PreviewScores { get; set; }
    public string HollandCode { get; set; } = "";
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<CompetencyQuestionItem> Questions { get; set; } = [];
    public List<string> RiasecTags { get; set; } = [];
    public List<string> MatchTags { get; set; } = [];
    public CareerCompassModel Compass { get; set; } = new();
    public string DeepAnalysisUpsellCopy { get; set; } = "";
    public List<CandidateMatchedVacancy> TopVacancies { get; set; } = [];
}

public sealed class CompetencyScoreSet
{
    public int? Samenwerken { get; set; }
    public int? Resultaatgerichtheid { get; set; }
    public int? Stressbestendigheid { get; set; }
    public int? Innovatie { get; set; }
    public int? Extraversie { get; set; }

    public bool IsComplete =>
        Samenwerken is not null
        && Resultaatgerichtheid is not null
        && Stressbestendigheid is not null
        && Innovatie is not null;
}

public sealed class RiasecScoreSet
{
    public int? Realistic { get; set; }
    public int? Investigative { get; set; }
    public int? Artistic { get; set; }
    public int? Social { get; set; }
    public int? Enterprising { get; set; }
    public int? Conventional { get; set; }

    public bool IsComplete =>
        Realistic is not null
        && Investigative is not null
        && Artistic is not null
        && Social is not null
        && Enterprising is not null
        && Conventional is not null;
}

public sealed class CompetencyQuestionItem
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public bool Reverse { get; set; }
    public string TextKey { get; set; } = "";
    public bool IsRiasec { get; set; }
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

public sealed class CareerOccupationMatchModel
{
    public string Title { get; set; } = "";
    public int Percent { get; set; }
    public string Band { get; set; } = "";
    public string Why { get; set; } = "";
    public List<string> Keys { get; set; } = [];
    public List<string> SearchKeys { get; set; } = [];
}

public sealed class CareerCompassModel
{
    public List<string> Strengths { get; set; } = [];
    public List<CareerOccupationMatchModel> SuperMatches { get; set; } = [];
    public List<CareerOccupationMatchModel> StrongChoices { get; set; } = [];
    public List<CareerOccupationMatchModel> Broadening { get; set; } = [];
    public List<string> PracticalNotes { get; set; } = [];
    public bool FromDeepAnalysis { get; set; }
    public bool FromOpenAi { get; set; }

    public bool HasOccupations =>
        SuperMatches.Count > 0 || StrongChoices.Count > 0 || Broadening.Count > 0;
}

public sealed class DeepAnalysisState
{
    public string Kind { get; set; } = "Competence";
    public string Status { get; set; } = "Locked";
    public bool IsUnlocked { get; set; }
    public bool IsCompleted { get; set; }
    public int AnsweredCount { get; set; }
    public int QuestionCount { get; set; } = 150;
    public decimal PriceEuro { get; set; } = 2.99m;
    public List<string> Tags { get; set; } = [];
    public DateTime? UnlockedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ReportGeneratedAtUtc { get; set; }
    public Dictionary<string, int> Answers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<DeepAnalysisQuestionItem> Questions { get; set; } = [];
    public string UpsellCopy { get; set; } = "";
}

public sealed class DeepAnalysisQuestionItem
{
    public int Id { get; set; }
    public string Family { get; set; } = "";
    public string Domain { get; set; } = "";
    public bool Reverse { get; set; }
    public string PromptNl { get; set; } = "";
}

public sealed class DeepAnalysisCheckout
{
    public Guid CheckoutId { get; set; }
    public string PaymentId { get; set; } = "";
    public string CheckoutUrl { get; set; } = "";
    public decimal AmountEuro { get; set; }
    public bool IsStub { get; set; }
    public string Kind { get; set; } = "Competence";
}

public sealed class RoleFitCheckState
{
    public bool IsUnlocked { get; set; }
    public bool CompetenceQuickScanCompleted { get; set; }
    public bool CareerQuickScanCompleted { get; set; }
    public bool DeepAnalysisCompleted { get; set; }
    public decimal DeepAnalysisPriceEuro { get; set; }
    public string LockMessage { get; set; } = "";
    public string DeepUpsellCopy { get; set; } = "";
    public RoleFitCheckResult? LastResult { get; set; }
}

public sealed class RoleFitCheckResult
{
    public string JobTitle { get; set; } = "";
    public int MatchPercent { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
    public List<string> ActionSteps { get; set; } = [];
    public List<string> SearchKeys { get; set; } = [];
    public string MapHref { get; set; } = "/";
    public bool FromDeepAnalysis { get; set; }
    public bool FromOpenAi { get; set; }
    public bool ShowDeepUpsell { get; set; }
    public List<TrainingOfferCard> TrainingOffers { get; set; } = [];
}

public sealed class TrainingOfferCard
{
    public Guid OfferId { get; set; }
    public string Title { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Network { get; set; } = "";
    public string Region { get; set; } = "";
    public string CtaLabel { get; set; } = "";
    public string Advice { get; set; } = "";
}

public sealed class TrainingTrackedLink
{
    public Guid ClickId { get; set; }
    public string Url { get; set; } = "";
    public string CandidateHash { get; set; } = "";
}

public sealed class TrainingProviderAdmin
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Network { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string FieldsCsv { get; set; } = "";
    public string Region { get; set; } = "";
    public decimal? CplEuro { get; set; }
    public decimal? CpaEuro { get; set; }
    public decimal? IntakeFeeEuro { get; set; }
    public decimal? StartFeeEuro { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public List<TrainingOfferAdmin> Offers { get; set; } = [];
}

public sealed class TrainingOfferAdmin
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string Title { get; set; } = "";
    public string FieldsCsv { get; set; } = "";
    public string KeysCsv { get; set; } = "";
    public string? ExternalPath { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
