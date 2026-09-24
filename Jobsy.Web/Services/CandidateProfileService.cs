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
                new CandidateProfileTestCard
                {
                    Id = "competence",
                    Title = "Competentieanalyse",
                    Summary = "Big Five / OCEAN Quick-Scan (25) afgerond.",
                    Completed = true,
                    StatusBadge = "Afgerond",
                    ActionLabel = "Bekijk of herhaal",
                    ActionHref = "/candidate/competencies"
                },
                new CandidateProfileTestCard
                {
                    Id = "career",
                    Title = "Beroepentest",
                    Summary = "RIASEC-interesses en Beroepen-kompas beschikbaar.",
                    Completed = true,
                    StatusBadge = "Afgerond",
                    ActionLabel = "Open kompas",
                    ActionHref = "/candidate/profile?tab=career"
                },
                new CandidateProfileTestCard
                {
                    Id = "culture",
                    Title = "Cultuurfit / DISC",
                    Summary = "Gedragsstijl ingevuld; weegt mee in Functiefit.",
                    Completed = true,
                    StatusBadge = "Afgerond",
                    ActionLabel = "Bekijk of herhaal",
                    ActionHref = "/candidate/culture"
                },
                new CandidateProfileTestCard
                {
                    Id = "fit",
                    Title = "Functiefit checker",
                    Summary = "Nog geen recente functie getoetst dit week.",
                    Completed = false,
                    StatusBadge = "Open",
                    ActionLabel = "Start Functiefit",
                    ActionHref = "/candidate/profile?tab=fit"
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
                Completed = t.Completed,
                StatusBadge = t.StatusBadge,
                ActionLabel = t.ActionLabel,
                ActionHref = t.ActionHref
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
