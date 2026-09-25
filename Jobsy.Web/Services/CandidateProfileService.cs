using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Mock candidate profile hub data for <c>/profiel</c> so the UI is interactive without live APIs.
/// </summary>
public sealed class CandidateProfileService
{
    private CandidateProfileHubModel _model = CreateDefault();

    public CandidateProfileHubModel GetProfile() => Clone(_model);

    public CandidateProfileHubModel UpdateSettings(CandidateProfileSettings settings)
    {
        _model.Settings = new CandidateProfileSettings
        {
            EmailNotifications = settings.EmailNotifications,
            PushNotifications = settings.PushNotifications,
            ShareTalentPool = settings.ShareTalentPool,
            HideContactUntilMatch = settings.HideContactUntilMatch
        };
        return GetProfile();
    }

    public CandidateProfileHubModel SetOpenForWork(bool openForWork)
    {
        _model.Basics.OpenForWork = openForWork;
        return GetProfile();
    }

    private static CandidateProfileHubModel CreateDefault()
        => new()
        {
            ProfileCompletenessPercent = 72,
            Basics = new CandidateProfileBasics
            {
                DisplayName = "Sam de Vries",
                Email = "sam.devries@example.nl",
                Phone = "06 12 34 56 78",
                HomeArea = "Den Haag · Escamp",
                MaxTravelMinutes = 35,
                TransportLabel = "E-bike / OV",
                OpenForWork = true,
                CvStatusLabel = "Lobsy-profiel klaar · eigen CV geüpload",
                CvReady = true,
                CurrentRoleTitle = "Magazijnmedewerker"
            },
            Tests =
            [
                // Scenario C — diepteanalyse gedaan
                new CandidateProfileTestCard
                {
                    Id = "competence",
                    Title = "Competentieanalyse",
                    Summary = "Diepteanalyse afgerond; scores en tags zijn bijgewerkt.",
                    Stage = CandidateDnaTestStage.DeepCompleted,
                    SupportsDeepAnalysis = true,
                    StatusBadge = "Diepteanalyse",
                    FreeTestHref = "/candidate/competencies",
                    DeepAnalysisHref = "/candidate/deep-analysis/competence"
                },
                // Scenario A — nog niets
                new CandidateProfileTestCard
                {
                    Id = "career",
                    Title = "Beroepentest",
                    Summary = "Nog niet ingevuld. Start gratis of ga direct voor de diepteanalyse.",
                    Stage = CandidateDnaTestStage.NotStarted,
                    SupportsDeepAnalysis = true,
                    StatusBadge = "Nog te doen",
                    FreeTestHref = "/candidate/profile?tab=career",
                    DeepAnalysisHref = "/candidate/deep-analysis/career"
                },
                // Scenario B — alleen gratis test
                new CandidateProfileTestCard
                {
                    Id = "culture",
                    Title = "Cultuurfit",
                    Summary = "Gratis cultuurscan afgerond; weegt mee in Functiefit.",
                    Stage = CandidateDnaTestStage.FreeCompleted,
                    SupportsDeepAnalysis = true,
                    StatusBadge = "Gratis test",
                    FreeTestHref = "/candidate/culture",
                    DeepAnalysisHref = "/candidate/deep-analysis/culture"
                }
            ],
            ScoreBars =
            [
                new CandidateProfileScoreBar
                {
                    Id = "samenwerken",
                    Label = "Samenwerken",
                    Percent = 82,
                    Hint = "Sterk in teamverband"
                },
                new CandidateProfileScoreBar
                {
                    Id = "resultaat",
                    Label = "Resultaatgerichtheid",
                    Percent = 74,
                    Hint = "Betrouwbaar afronden"
                },
                new CandidateProfileScoreBar
                {
                    Id = "stress",
                    Label = "Stressbestendigheid",
                    Percent = 68,
                    Hint = "Stabiel bij pieken"
                },
                new CandidateProfileScoreBar
                {
                    Id = "innovatie",
                    Label = "Innovatie",
                    Percent = 55,
                    Hint = "Ruimte om te groeien"
                },
                new CandidateProfileScoreBar
                {
                    Id = "cultuur",
                    Label = "Cultuurfit (gemiddeld)",
                    Percent = 71,
                    Hint = "Past bij rustige teams"
                }
            ],
            Settings = new CandidateProfileSettings
            {
                EmailNotifications = true,
                PushNotifications = false,
                ShareTalentPool = true,
                HideContactUntilMatch = true
            }
        };

    private static CandidateProfileHubModel Clone(CandidateProfileHubModel source)
        => new()
        {
            ProfileCompletenessPercent = source.ProfileCompletenessPercent,
            Basics = new CandidateProfileBasics
            {
                DisplayName = source.Basics.DisplayName,
                Email = source.Basics.Email,
                Phone = source.Basics.Phone,
                HomeArea = source.Basics.HomeArea,
                MaxTravelMinutes = source.Basics.MaxTravelMinutes,
                TransportLabel = source.Basics.TransportLabel,
                OpenForWork = source.Basics.OpenForWork,
                CvStatusLabel = source.Basics.CvStatusLabel,
                CvReady = source.Basics.CvReady,
                CurrentRoleTitle = source.Basics.CurrentRoleTitle
            },
            Tests = source.Tests.Select(t => new CandidateProfileTestCard
            {
                Id = t.Id,
                Title = t.Title,
                Summary = t.Summary,
                Stage = t.Stage,
                SupportsDeepAnalysis = t.SupportsDeepAnalysis,
                StatusBadge = t.StatusBadge,
                FreeTestHref = t.FreeTestHref,
                DeepAnalysisHref = t.DeepAnalysisHref
            }).ToList(),
            ScoreBars = source.ScoreBars.Select(s => new CandidateProfileScoreBar
            {
                Id = s.Id,
                Label = s.Label,
                Percent = s.Percent,
                Hint = s.Hint
            }).ToList(),
            Settings = new CandidateProfileSettings
            {
                EmailNotifications = source.Settings.EmailNotifications,
                PushNotifications = source.Settings.PushNotifications,
                ShareTalentPool = source.Settings.ShareTalentPool,
                HideContactUntilMatch = source.Settings.HideContactUntilMatch
            }
        };
}
